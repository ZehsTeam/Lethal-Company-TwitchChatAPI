using TwitchChatAPI.Enums;
using TwitchChatAPI.Helpers;
using TwitchChatAPI.MonoBehaviours;
using TwitchChatAPI.Objects;
using System;
using System.Collections.Generic;

namespace TwitchChatAPI;

/// <summary>
/// API for TwitchChatAPI.
/// </summary>
public static class API
{
    /// <summary>
    /// The current Twitch channel username.
    /// </summary>
    public static string Channel => TwitchChat.Channel;

    /// <summary>
    /// Represents the current connection state to Twitch chat.
    /// </summary>
    public static ConnectionState ConnectionState => TwitchChat.ConnectionState;

    /// <summary>
    /// Invoked when the connection state to Twitch chat is changed.
    /// </summary>
    public static event Action<ConnectionState> OnConnectionStateChanged;

    /// <summary>
    /// Invoked when the connection to Twitch chat is established.
    /// </summary>
    public static event Action OnConnect;

    /// <summary>
    /// Invoked when the connection to Twitch chat is lost or disconnected.
    /// </summary>
    public static event Action OnDisconnect;

    /// <summary>
    /// Invoked when a new chat message is received.
    /// </summary>
    public static event Action<TwitchMessage> OnMessage;

    /// <summary>
    /// Invoked when a user sends a cheer.
    /// </summary>
    public static event Action<TwitchCheerEvent> OnCheer;

    /// <summary>
    /// Invoked when a user subscribes or gifts subscriptions.
    /// </summary>
    public static event Action<TwitchSubEvent> OnSub;

    /// <summary>
    /// Invoked when a user raids the channel.
    /// </summary>
    public static event Action<TwitchRaidEvent> OnRaid;

    /// <summary>
    /// Invoked when the Twitch chat room state updates.
    /// </summary>
    public static event Action<TwitchRoomState> OnRoomStateUpdate;

    /// <summary>
    /// Gets the list of all Twitch users that have had activity in the chat since this mod has been connected.
    /// </summary>
    public static IReadOnlyCollection<TwitchUser> Users => UserHelper.Users.Values;

    /// <summary>
    /// Connect to Twitch chat. Uses the channel specified in the config settings.
    /// </summary>
    public static void Connect()
    {
        TwitchChat.Connect();
    }

    /// <summary>
    /// Connect to Twitch chat. Uses the channel specified. This will update the channel in the config settings.
    /// </summary>
    /// <param name="channel"></param>
    public static void Connect(string channel)
    {
        TwitchChat.Connect(channel);
    }

    /// <summary>
    /// Disconnect from Twitch chat.
    /// </summary>
    public static void Disconnect()
    {
        TwitchChat.Disconnect();
    }

    /// <summary>
    /// Attempts to retrieve a Twitch user by their username or display name.
    /// </summary>
    /// <param name="username">The username or display name (case insensitive).</param>
    /// <param name="twitchUser">The found TwitchUser, or default if not found.</param>
    /// <returns>True if the user was found, otherwise false.</returns>
    public static bool TryGetUserByUsername(string username, out TwitchUser twitchUser)
    {
        return UserHelper.TryGetUserByUsername(username, out twitchUser);
    }

    /// <summary>
    /// Attempts to retrieve a Twitch user by their user ID.
    /// </summary>
    /// <param name="userId">The unique user ID.</param>
    /// <param name="twitchUser">The found TwitchUser, or default if not found.</param>
    /// <returns>True if the user was found, otherwise false.</returns>
    public static bool TryGetUserByUserId(string userId, out TwitchUser twitchUser)
    {
        return UserHelper.TryGetUserByUserId(userId, out twitchUser);
    }

    /// <summary>
    /// Returns an array of Twitch users who were last seen within the given time span.
    /// </summary>
    /// <param name="timeSpan">The time span to filter users by.</param>
    /// <returns>An array of TwitchUsers who were last seen within the given time span.</returns>
    public static TwitchUser[] GetUsersSeenWithin(TimeSpan timeSpan)
    {
        return UserHelper.GetUsersSeenWithin(timeSpan);
    }

    #region Internal Event Invocation
    internal static void InvokeOnConnectionStateChanged(ConnectionState state)
    {
        MainThreadDispatcher.Enqueue(() => OnConnectionStateChanged?.Invoke(state));
    }

    internal static void InvokeOnConnect()
    {
        MainThreadDispatcher.Enqueue(() => OnConnect?.Invoke());
    }

    internal static void InvokeOnDisconnect()
    {
        MainThreadDispatcher.Enqueue(() => OnDisconnect?.Invoke());
    }

    internal static void InvokeOnMessage(TwitchMessage message)
    {
        MainThreadDispatcher.Enqueue(() => OnMessage?.Invoke(message));
    }

    internal static void InvokeOnSub(TwitchSubEvent subEvent)
    {
        MainThreadDispatcher.Enqueue(() => OnSub?.Invoke(subEvent));
    }

    internal static void InvokeOnCheer(TwitchCheerEvent cheerEvent)
    {
        MainThreadDispatcher.Enqueue(() => OnCheer?.Invoke(cheerEvent));
    }

    internal static void InvokeOnRaid(TwitchRaidEvent raidEvent)
    {
        MainThreadDispatcher.Enqueue(() => OnRaid?.Invoke(raidEvent));
    }

    internal static void InvokeOnRoomStateUpdate(TwitchRoomState roomState)
    {
        MainThreadDispatcher.Enqueue(() => OnRoomStateUpdate?.Invoke(roomState));
    }
    #endregion
}
