using com.github.zehsteam.TwitchChatAPI.Enums;
using System;
using System.Collections.Generic;

namespace com.github.zehsteam.TwitchChatAPI.Objects;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

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
    public bool IsPrime { get; set; }
    public SubTier Tier { get; set; }
    public int CumulativeMonths { get; set; }
    public string RecipientUser { get; set; }
    public int GiftCount { get; set; }

    [Obsolete("Use CumulativeMonths instead.", true)]
    public int Months => CumulativeMonths;
}

public class TwitchCheerEvent : TwitchEvent
{
    public int CheerAmount { get; set; }
}

public class TwitchRaidEvent : TwitchEvent
{
    public int ViewerCount { get; set; }
}
