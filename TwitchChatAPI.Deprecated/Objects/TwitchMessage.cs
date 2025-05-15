using System;
using System.Collections.Generic;

namespace com.github.zehsteam.TwitchChatAPI.Objects;

[Obsolete(API.ObsoleteMessage, error: true)]
public struct TwitchMessage
{
    public string Channel { get; set; }
    public TwitchUser User { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string> Tags { get; set; } // Raw tags for extensibility
}
