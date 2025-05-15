using System;

namespace com.github.zehsteam.TwitchChatAPI.Enums;

[Obsolete(API.ObsoleteMessage, error: true)]
public enum SubType
{
    Sub,
    Resub,
    SubGift,
    SubMysteryGift
}
