using com.github.zehsteam.TwitchChatAPI.Enums;
using com.github.zehsteam.TwitchChatAPI.Helpers;
using com.github.zehsteam.TwitchChatAPI.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NewAPI = TwitchChatAPI.API;
using NewConnectionState = TwitchChatAPI.Enums.ConnectionState;
using NewTwitchUser = TwitchChatAPI.Objects.TwitchUser;
using NewTwitchMessage = TwitchChatAPI.Objects.TwitchMessage;
using NewTwitchCheerEvent = TwitchChatAPI.Objects.TwitchCheerEvent;
using NewTwitchSubEvent = TwitchChatAPI.Objects.TwitchSubEvent;
using NewTwitchRaidEvent = TwitchChatAPI.Objects.TwitchRaidEvent;
using NewTwitchRoomState = TwitchChatAPI.Objects.TwitchRoomState;

namespace com.github.zehsteam.TwitchChatAPI;

[Obsolete(ObsoleteMessage, error: true)]
public static class API
{
    internal const string ObsoleteMessage = "You are using the old TwitchChatAPI namespace! Switch to \"using TwitchChatAPI;\".";

    public static ConnectionState ConnectionState => Converter.Convert<ConnectionState>(NewAPI.ConnectionState);

    public static event Action OnConnect;
    public static event Action OnDisconnect;

    public static event Action<TwitchMessage> OnMessage;
    public static event Action<TwitchCheerEvent> OnCheer;
    public static event Action<TwitchSubEvent> OnSub;
    public static event Action<TwitchRaidEvent> OnRaid;
    public static event Action<TwitchRoomState> OnRoomStateUpdate;

    public static IReadOnlyCollection<TwitchUser> Users => (IReadOnlyCollection<TwitchUser>)Converter.ConvertList<NewTwitchUser, TwitchUser>(NewAPI.Users);

    internal static void Initialize()
    {
        NewAPI.OnConnect += InvokeOnConnect;
        NewAPI.OnDisconnect += InvokeOnDisconnect;
        NewAPI.OnMessage += InvokeOnMessage;
        NewAPI.OnCheer += InvokeOnCheer;
        NewAPI.OnSub += InvokeOnSub;
        NewAPI.OnRaid += InvokeOnRaid;
        NewAPI.OnRoomStateUpdate += InvokeOnRoomStateUpdate;

        Application.quitting += () =>
        {
            NewAPI.OnConnect -= InvokeOnConnect;
            NewAPI.OnDisconnect -= InvokeOnDisconnect;
            NewAPI.OnMessage -= InvokeOnMessage;
            NewAPI.OnCheer -= InvokeOnCheer;
            NewAPI.OnSub -= InvokeOnSub;
            NewAPI.OnRaid -= InvokeOnRaid;
            NewAPI.OnRoomStateUpdate -= InvokeOnRoomStateUpdate;
        };
    }

    public static bool TryGetUserByUsername(string username, out TwitchUser twitchUser)
    {
        if (NewAPI.TryGetUserByUsername(username, out var newTwitchUser))
        {
            twitchUser = Converter.Convert<TwitchUser>(newTwitchUser);
            return true;
        }
        else
        {
            twitchUser = default;
            return false;
        }
    }

    public static bool TryGetUserByUserId(string userId, out TwitchUser twitchUser)
    {
        if (NewAPI.TryGetUserByUserId(userId, out var newTwitchUser))
        {
            twitchUser = Converter.Convert<TwitchUser>(newTwitchUser);
            return true;
        }
        else
        {
            twitchUser = default;
            return false;
        }
    }

    public static TwitchUser[] GetUsersSeenWithin(TimeSpan timeSpan)
    {
        return Converter.ConvertList<NewTwitchUser, TwitchUser>(NewAPI.GetUsersSeenWithin(timeSpan)).ToArray();
    }

    #region Internal Event Invocation
    internal static void InvokeOnConnect()
    {
        OnConnect?.Invoke();
    }

    internal static void InvokeOnDisconnect()
    {
        OnDisconnect?.Invoke();
    }

    internal static void InvokeOnMessage(NewTwitchMessage message)
    {
        OnMessage?.Invoke(Converter.Convert<TwitchMessage>(message));
    }

    internal static void InvokeOnCheer(NewTwitchCheerEvent cheerEvent)
    {
        OnCheer?.Invoke(Converter.Convert<TwitchCheerEvent>(cheerEvent));
    }

    internal static void InvokeOnSub(NewTwitchSubEvent subEvent)
    {
        OnSub?.Invoke(Converter.Convert<TwitchSubEvent>(subEvent));
    }

    internal static void InvokeOnRaid(NewTwitchRaidEvent raidEvent)
    {
        OnRaid?.Invoke(Converter.Convert<TwitchRaidEvent>(raidEvent));
    }

    internal static void InvokeOnRoomStateUpdate(NewTwitchRoomState roomState)
    {
        OnRoomStateUpdate?.Invoke(Converter.Convert<TwitchRoomState>(roomState));
    }
    #endregion
}
