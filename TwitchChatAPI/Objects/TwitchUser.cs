using System;

namespace TwitchChatAPI.Objects;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public struct TwitchUser : IEquatable<TwitchUser>
{
    public string UserId { get; set; }

    /// <summary>
    /// Same as <see cref="DisplayName"/> but in all lowercase.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Same as <see cref="Username"/> but with capitalization.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// The name color. HEX color code that starts with a # (Example: #FFFFFF)
    /// </summary>
    public string Color { get; set; }
    public bool IsVIP { get; set; }
    public bool IsSubscriber { get; set; }
    public bool IsModerator { get; set; }
    public bool IsBroadcaster { get; set; }

    public override bool Equals(object obj)
    {
        return obj is TwitchUser other && Equals(other);
    }

    public bool Equals(TwitchUser other)
    {
        return string.Equals(UserId, other.UserId, StringComparison.Ordinal);
    }

    public override int GetHashCode()
    {
        return UserId?.GetHashCode() ?? 0;
    }

    public static bool operator ==(TwitchUser left, TwitchUser right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TwitchUser left, TwitchUser right)
    {
        return !left.Equals(right);
    }
}
