using com.github.zehsteam.TwitchChatAPI.Enums;
using System;
using System.Collections.Generic;

namespace com.github.zehsteam.TwitchChatAPI.Objects;

[Obsolete(API.ObsoleteMessage, error: true)]
public abstract class TwitchEvent
{
    public string Channel { get; set; }
    public TwitchUser User { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string> Tags { get; set; } // Raw tags for extensibility
}

[Obsolete(API.ObsoleteMessage, error: true)]
public class TwitchSubEvent : TwitchEvent
{
    public SubType SubType { get; set; }
    public bool IsPrime { get; set; }
    public SubTier Tier { get; set; }
    public int CumulativeMonths { get; set; }
    public string RecipientUser { get; set; }
    public int GiftCount { get; set; }

    [Obsolete("Use CumulativeMonths instead.", true)]
    public int Months => CumulativeMonths;
}

[Obsolete(API.ObsoleteMessage, error: true)]
public class TwitchCheerEvent : TwitchEvent
{
    public int CheerAmount { get; set; }
}

[Obsolete(API.ObsoleteMessage, error: true)]
public class TwitchRaidEvent : TwitchEvent
{
    public int ViewerCount { get; set; }
}
