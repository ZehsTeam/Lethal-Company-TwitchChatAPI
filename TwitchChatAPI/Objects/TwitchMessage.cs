using System.Collections.Generic;

namespace TwitchChatAPI.Objects;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public struct TwitchMessage
{
    public string Channel { get; set; }
    public TwitchUser User { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string> Tags { get; set; } // Raw tags for extensibility

    public TwitchMessage RemoveTags()
    {
        var twitchMessage = this;
        twitchMessage.Tags = [];
        return twitchMessage;
    }
}
