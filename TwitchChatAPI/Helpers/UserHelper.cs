using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TwitchChatAPI.Objects;
using UnityEngine;

namespace TwitchChatAPI.Helpers;

internal static class UserHelper
{
    /// <summary>
    /// Stores Twitch users, keyed by their unique user ID, allowing for O(1) lookups.
    /// </summary>
    public static Dictionary<string, TwitchUser> Users { get; private set; } = [];

    /// <summary>
    /// Maps usernames to user IDs for O(1) lookup by username.
    /// </summary>
    public static Dictionary<string, string> UsernameToUserId { get; private set; } = [];

    /// <summary>
    /// Tracks the last activity time (Time.realtimeSinceStartup) of users.
    /// </summary>
    public static Dictionary<string, float> TimeLastSeen { get; private set; } = [];

    /// <summary>
    /// Attempts to retrieve a Twitch user by their username or display name.
    /// </summary>
    /// <param name="username">The username or display name (case insensitive).</param>
    /// <param name="twitchUser">The found TwitchUser, or default if not found.</param>
    /// <returns>True if the user was found, otherwise false.</returns>
    public static bool TryGetUserByUsername(string username, out TwitchUser twitchUser)
    {
        if (UsernameToUserId.TryGetValue(username.ToLower(), out string userId) && Users.TryGetValue(userId, out twitchUser))
        {
            return true;
        }

        twitchUser = default;
        return false;
    }

    /// <summary>
    /// Attempts to retrieve a Twitch user by their user ID.
    /// </summary>
    /// <param name="userId">The unique user ID.</param>
    /// <param name="twitchUser">The found TwitchUser, or default if not found.</param>
    /// <returns>True if the user was found, otherwise false.</returns>
    public static bool TryGetUserByUserId(string userId, out TwitchUser twitchUser)
    {
        return Users.TryGetValue(userId, out twitchUser);
    }

    /// <summary>
    /// Returns an array of Twitch users who were last seen within the given time span.
    /// </summary>
    /// <param name="timeSpan">The time span to filter users by.</param>
    /// <returns>An array of TwitchUsers who were last seen within the given time span.</returns>
    public static TwitchUser[] GetUsersSeenWithin(TimeSpan timeSpan)
    {
        float currentTime = Time.realtimeSinceStartup;
        float minTime = currentTime - (float)timeSpan.TotalSeconds;

        return TimeLastSeen
            .Where(entry => entry.Value >= minTime)  // Only users seen within the timespan
            .Select(entry => Users.TryGetValue(entry.Key, out TwitchUser user) ? user : default)
            .Where(user => user != null)  // Filter out any default/null values
            .ToArray();
    }

    /// <summary>
    /// Adds or updates a Twitch user in the system, ensuring data consistency.
    /// </summary>
    /// <param name="twitchUser">The TwitchUser to add or update.</param>
    public static void UpdateUser(TwitchUser twitchUser)
    {
        if (twitchUser.Equals(default)) return;

        // Update or add user in Users dictionary
        Users[twitchUser.UserId] = twitchUser;

        // Update username-to-userId mapping (case insensitive keys)
        string usernameKey = twitchUser.Username.ToLower();

        if (UsernameToUserId.TryGetValue(usernameKey, out string existingUserId))
        {
            // Remove the old mapping if the user ID has changed
            if (existingUserId != twitchUser.UserId)
            {
                UsernameToUserId.Remove(usernameKey);
            }
        }

        // Update the mapping
        UsernameToUserId[usernameKey] = twitchUser.UserId;

        // Update last seen time
        TimeLastSeen[twitchUser.UserId] = Time.realtimeSinceStartup;
    }

    public static bool IsValidUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        // Must be 3–25 characters long
        // Can contain uppercase/lowercase letters, numbers, underscores
        // Cannot start with an underscore
        var regex = new Regex(@"^[A-Za-z0-9][A-Za-z0-9_]{2,24}$");
        return regex.IsMatch(username) && !username.StartsWith("_");
    }
}
