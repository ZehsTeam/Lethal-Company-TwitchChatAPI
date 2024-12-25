using com.github.zehsteam.TwitchChatAPI.Objects;
using System;

namespace com.github.zehsteam.TwitchChatAPI;

public static class API
{
    public static event Action OnConnect;
    public static event Action OnDisconnect;
    public static event Action<TwitchMessage> OnMessage;
    public static event Action<TwitchSubEvent> OnSub;
    public static event Action<TwitchCheerEvent> OnCheer;
    public static event Action<TwitchRaidEvent> OnRaid;
    public static event Action<TwitchRoomState> OnRoomStateUpdate;

    #region Internal
    internal static void InvokeOnConnect()
    {
        OnConnect?.Invoke();
    }

    internal static void InvokeOnDisconnect()
    {
        OnDisconnect?.Invoke();
    }

    internal static void InvokeOnMessage(TwitchMessage message)
    {
        OnMessage?.Invoke(message);
    }

    internal static void InvokeOnSub(TwitchSubEvent subEvent)
    {
        OnSub?.Invoke(subEvent);
    }

    internal static void InvokeOnCheer(TwitchCheerEvent cheerEvent)
    {
        OnCheer?.Invoke(cheerEvent);
    }

    internal static void InvokeOnRaid(TwitchRaidEvent raidEvent)
    {
        OnRaid?.Invoke(raidEvent);
    }

    internal static void InvokeOnRoomStateUpdate(TwitchRoomState roomState)
    {
        OnRoomStateUpdate?.Invoke(roomState);
    }
    #endregion
}
