using com.github.zehsteam.TwitchChatAPI.Enums;
using com.github.zehsteam.TwitchChatAPI.MonoBehaviours;
using com.github.zehsteam.TwitchChatAPI.Objects;
using System;

namespace com.github.zehsteam.TwitchChatAPI;

public static class API
{
    public static ConnectionState ConnectionState => TwitchChat.ConnectionState;
    public static event Action OnConnect;
    public static event Action OnDisconnect;
    public static event Action<TwitchMessage> OnMessage;
    public static event Action<TwitchCheerEvent> OnCheer;
    public static event Action<TwitchSubEvent> OnSub;
    public static event Action<TwitchRaidEvent> OnRaid;
    public static event Action<TwitchRoomState> OnRoomStateUpdate;

    #region Internal
    internal static void InvokeOnConnect()
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            OnConnect?.Invoke();
        });
    }

    internal static void InvokeOnDisconnect()
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            OnDisconnect?.Invoke();
        });
    }

    internal static void InvokeOnMessage(TwitchMessage message)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            OnMessage?.Invoke(message);
        });
    }

    internal static void InvokeOnSub(TwitchSubEvent subEvent)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            OnSub?.Invoke(subEvent);
        });
    }

    internal static void InvokeOnCheer(TwitchCheerEvent cheerEvent)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            OnCheer?.Invoke(cheerEvent);
        });
    }

    internal static void InvokeOnRaid(TwitchRaidEvent raidEvent)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            OnRaid?.Invoke(raidEvent);
        });
    }

    internal static void InvokeOnRoomStateUpdate(TwitchRoomState roomState)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            OnRoomStateUpdate?.Invoke(roomState);
        });
    }
    #endregion
}
