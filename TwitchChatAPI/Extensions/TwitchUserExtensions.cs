using System;
using TwitchChatAPI.Objects;

namespace TwitchChatAPI.Extensions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public static class TwitchUserExtensions
{
    public static string GetDisplayNameWithColor(this TwitchUser twitchUser)
    {
        return $"<color={twitchUser.Color}>{twitchUser.DisplayName}</color>";
    }

    public static string GetDisplayNameWithColor(this TwitchUser twitchUser, Func<string, string> colorParser)
    {
        string newColor;

        if (colorParser == null)
        {
            newColor = twitchUser.Color;
        }
        else
        {
            newColor = colorParser(twitchUser.Color);
        }

        return $"<color={newColor}>{twitchUser.DisplayName}</color>";
    }
}
