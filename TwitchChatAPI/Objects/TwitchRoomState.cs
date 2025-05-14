using System.Collections.Generic;

namespace TwitchChatAPI.Objects;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public struct TwitchRoomState
{
    public string Channel { get; set; }
    public bool IsEmoteOnly { get; set; }
    public bool IsFollowersOnly { get; set; }
    public bool IsR9K { get; set; }
    public bool IsSlowMode { get; set; }
    public bool IsSubsOnly { get; set; }
    public Dictionary<string, string> Tags { get; set; } // Raw tags for extensibility

    public TwitchRoomState RemoveTags()
    {
        var twitchRoomState = this;
        twitchRoomState.Tags = [];
        return twitchRoomState;
    }
}
