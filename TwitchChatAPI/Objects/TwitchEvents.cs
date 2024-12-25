using com.github.zehsteam.TwitchChatAPI.Enums;
using System.Collections.Generic;

namespace com.github.zehsteam.TwitchChatAPI.Objects;

public abstract class TwitchEvent
{
    public string Channel { get; set; }
    public TwitchUser User { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string> Tags { get; set; } // Raw tags for extensibility
}

public class TwitchSubEvent : TwitchEvent
{
    public SubType SubType { get; set; }
    public int Tier { get; set; }
    public int Months { get; set; }
    public string RecipientUser { get; set; }
    public int GiftCount { get; set; }
}

public class TwitchCheerEvent : TwitchEvent
{
    public int CheerAmount { get; set; }
}

public class TwitchRaidEvent : TwitchEvent
{
    public int ViewerCount { get; set; }
}
