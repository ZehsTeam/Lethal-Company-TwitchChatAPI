namespace com.github.zehsteam.TwitchChatAPI.Objects;

public struct TwitchUser
{
    public string DisplayName { get; set; }
    public string Color { get; set; }
    public bool IsSubscriber { get; set; }
    public bool IsModerator { get; set; }
    public bool IsBroadcaster { get; set; }
}
