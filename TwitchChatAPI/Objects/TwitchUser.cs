namespace com.github.zehsteam.TwitchChatAPI.Objects;

public struct TwitchUser
{
    public string UserId { get; set; }
    public string DisplayName { get; set; }
    public string Color { get; set; }
    public bool IsVIP { get; set; }
    public bool IsSubscriber { get; set; }
    public bool IsModerator { get; set; }
    public bool IsBroadcaster { get; set; }
}
