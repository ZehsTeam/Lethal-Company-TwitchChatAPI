using System;

namespace com.github.zehsteam.TwitchChatAPI.Enums;

[Obsolete(API.ObsoleteMessage, error: true)]
public enum ConnectionState
{
    None,
    Connecting,
    Connected,
    Disconnecting,
    Disconnected,
}
