using System.Collections.Generic;

namespace com.github.zehsteam.TwitchChatAPI.Objects;

public class TwitchEvent
{
    public string Channel { get; set; }
    public TwitchUser User { get; set; }
    public Dictionary<string, string> Tags { get; set; } // Raw tags for extensibility
}

public class TwitchSubEvent : TwitchEvent
{
    public int CumulativeMonths { get; set; }
    public bool IsResub { get; set; }
    public string GifterName { get; set; } // For gifted subs
    public string Message { get; set; }
}

public class TwitchRaidEvent : TwitchEvent
{
    public int ViewerCount { get; set; }
}

public class TwitchCheerEvent : TwitchEvent
{
    public int CheerAmount { get; set; }
    public string Message { get; set; }
}
