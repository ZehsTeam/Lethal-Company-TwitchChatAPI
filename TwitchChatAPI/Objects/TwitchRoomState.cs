﻿namespace com.github.zehsteam.TwitchChatAPI.Objects;

public struct TwitchRoomState
{
    public string Channel { get; set; }
    public bool IsEmoteOnly { get; set; }
    public bool IsFollowersOnly { get; set; }
    public bool IsR9K { get; set; }
    public bool IsSlowMode { get; set; }
    public bool IsSubOnly { get; set; }
}