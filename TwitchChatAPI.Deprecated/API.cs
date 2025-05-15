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
    internal const string ObsoleteMessage = "You are using the deprecated version of TwitchChatAPI. You need to reference the new TwitchChatAPI.dll, update your BepInDependency to use the new GUID, and use the new namespaces.";

    public static ConnectionState ConnectionState => Converter.Convert(NewAPI.ConnectionState, ConnectionState.None);

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
        if (NewAPI.TryGetUserByUsername(username, out NewTwitchUser newTwitchUser))
        {
            if (Converter.TryConvert(newTwitchUser, out twitchUser))
            {
                return true;
            }
            else
            {
                Logger.LogWarning($"[Deprecated] API: Failed to get TwitchUser by Username. Could not convert TwitchUser.", extended: true);
            }

            twitchUser = default;
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
        if (NewAPI.TryGetUserByUserId(userId, out NewTwitchUser newTwitchUser))
        {
            if (Converter.TryConvert(newTwitchUser, out twitchUser))
            {
                return true;
            }
            else
            {
                Logger.LogWarning($"[Deprecated] API: Failed to get TwitchUser by UserId. Could not convert TwitchUser.", extended: true);
            }

            twitchUser = default;
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

    internal static void InvokeOnMessage(NewTwitchMessage newMessage)
    {
        if (OnMessage == null)
        {
            return;
        }

        if (Converter.TryConvert<TwitchMessage>(newMessage, out var message))
        {
            OnMessage?.Invoke(message);
        }
        else
        {
            Logger.LogWarning($"[Deprecated] API: Failed to invoke OnMessage. Could not convert TwitchMessage.", extended: true);
        }
    }

    internal static void InvokeOnCheer(NewTwitchCheerEvent newCheerEvent)
    {
        if (OnCheer == null)
        {
            return;
        }

        if (Converter.TryConvert<TwitchCheerEvent>(newCheerEvent, out var cheerEvent))
        {
            OnCheer?.Invoke(cheerEvent);
        }
        else
        {
            Logger.LogWarning($"[Deprecated] API: Failed to invoke OnCheer. Could not convert TwitchCheerEvent.", extended: true);
        }
    }

    internal static void InvokeOnSub(NewTwitchSubEvent newSubEvent)
    {
        if (OnSub == null)
        {
            return;
        }

        if (Converter.TryConvert<TwitchSubEvent>(newSubEvent, out var subEvent))
        {
            OnSub?.Invoke(subEvent);
        }
        else
        {
            Logger.LogWarning($"[Deprecated] API: Failed to invoke OnSub. Could not convert TwitchSubEvent.", extended: true);
        }
    }

    internal static void InvokeOnRaid(NewTwitchRaidEvent newRaidEvent)
    {
        if (OnRaid == null)
        {
            return;
        }

        if (Converter.TryConvert<TwitchRaidEvent>(newRaidEvent, out var raidEvent))
        {
            OnRaid?.Invoke(raidEvent);
        }
        else
        {
            Logger.LogWarning($"[Deprecated] API: Failed to invoke OnRaid. Could not convert TwitchRaidEvent.", extended: true);
        }
    }

    internal static void InvokeOnRoomStateUpdate(NewTwitchRoomState newRoomState)
    {
        if (OnRoomStateUpdate == null)
        {
            return;
        }

        if (Converter.TryConvert<TwitchRoomState>(newRoomState, out var roomState))
        {
            OnRoomStateUpdate?.Invoke(roomState);
        }
        else
        {
            Logger.LogWarning($"[Deprecated] API: Failed to invoke OnRoomStateUpdate. Could not convert TwitchRoomState.", extended: true);
        }
    }
    #endregion
}
