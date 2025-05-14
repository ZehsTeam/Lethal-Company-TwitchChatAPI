using System;
using System.Collections.Generic;
using TwitchChatAPI.Enums;

namespace TwitchChatAPI.Objects;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public abstract class TwitchEvent
{
    public string Channel { get; set; }
    public TwitchUser User { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string> Tags { get; set; } // Raw tags for extensibility

    public TwitchEvent RemoveTags()
    {
        Tags = [];
        return this;
    }
}

public class TwitchSubEvent : TwitchEvent
{
    public SubType Type { get; set; }
    public SubTier Tier { get; set; }
    public int CumulativeMonths { get; set; }
    public string RecipientUser { get; set; }
    public int GiftCount { get; set; }

    [Obsolete("Use Type instead.", true)]
    public SubType SubType => Type;

    [Obsolete("Use CumulativeMonths instead.", true)]
    public int Months => CumulativeMonths;

    [Obsolete("Use SubTier.Prime instead.", true)]
    public bool IsPrime => Tier == SubTier.Prime;
}

public class TwitchCheerEvent : TwitchEvent
{
    public int CheerAmount { get; set; }
}

public class TwitchRaidEvent : TwitchEvent
{
    public int ViewerCount { get; set; }
}
