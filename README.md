# TwitchChatAPI

[![GitHub](https://img.shields.io/badge/GitHub-TwitchChatAPI-brightgreen?style=for-the-badge&logo=GitHub)](https://github.com/ZehsTeam/TwitchChatAPI)
[![Thunderstore Version](https://img.shields.io/thunderstore/v/Zehs/TwitchChatAPI?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/Zehs/TwitchChatAPI/)
[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/Zehs/TwitchChatAPI?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/Zehs/TwitchChatAPI/)
[![NuGet Version](https://img.shields.io/nuget/v/zehs.twitchchatapi?style=for-the-badge&logo=nuget)](https://www.nuget.org/packages/Zehs.TwitchChatAPI)

**Add Twitch chat integration to your Unity game mods! Subscribe to events like Messages, Cheers, Subs, and Raids. No Twitch authentication or connections required.**

**<ins>Who needs this mod installed for it to work?</ins> Only you!**

## <img src="https://i.imgur.com/TpnrFSH.png" width="20px"> Download

Download [TwitchChatAPI](https://thunderstore.io/c/lethal-company/p/Zehs/TwitchChatAPI/) on Thunderstore.

## API Usage

<details><summary>Click to Expand</summary>

<br>

Reference TwitchChatAPI in your project's `.csproj` file.

Add TwitchChatAPI as a dependency to your plugin class.

```cs
[BepInDependency(TwitchChatAPI.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
```

```cs
[BepInPlugin("You.YourMod", "YourMod", "1.0.0")]
[BepInDependency(TwitchChatAPI.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
public class YourMod : BaseUnityPlugin
{
    // ...
}
```

### API

https://github.com/ZehsTeam/TwitchChatAPI/blob/main/TwitchChatAPI/API.cs

```cs
namespace TwitchChatAPI;

public static class API
{
    public static string Channel { get; }

    public static ConnectionState ConnectionState { get; }
    public static event Action<ConnectionState> OnConnectionStateChanged;

    public static event Action OnConnect;
    public static event Action OnDisconnect;
    public static event Action<TwitchMessage> OnMessage;
    public static event Action<TwitchCheerEvent> OnCheer;
    public static event Action<TwitchSubEvent> OnSub;
    public static event Action<TwitchRaidEvent> OnRaid;
    public static event Action<TwitchRoomState> OnRoomStateUpdate;

    public static IReadOnlyCollection<TwitchUser> Users { get; }

    public static void Connect();
    public static void Connect(string channel);

    public static void Disconnect();    

    public static bool TryGetUserByUsername(string username, out TwitchUser twitchUser);
    public static bool TryGetUserByUserId(string userId, out TwitchUser twitchUser);
    public static TwitchUser[] GetUsersSeenWithin(TimeSpan timeSpan);
}
```

### TwitchUser

https://github.com/ZehsTeam/TwitchChatAPI/blob/main/TwitchChatAPI/Objects/TwitchUser.cs

### TwitchMessage

https://github.com/ZehsTeam/TwitchChatAPI/blob/main/TwitchChatAPI/Objects/TwitchMessage.cs

### TwitchEvents (Cheer, Sub, Raid)

https://github.com/ZehsTeam/TwitchChatAPI/blob/main/TwitchChatAPI/Objects/TwitchEvents.cs

### Example

```cs
using TwitchChatAPI;
using TwitchChatAPI.Enums;
using TwitchChatAPI.Objects;
using UnityEngine;

public class TwitchChatExample : MonoBehaviour
{
    private void OnEnable()
    {
        // Subscribe to Twitch events
        API.OnMessage += HandleMessage;
        API.OnCheer += HandleCheer;
        API.OnSub += HandleSub;
        API.OnRaid += HandleRaid;
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid memory leaks
        API.OnMessage -= HandleMessage;
        API.OnCheer -= HandleCheer;
        API.OnSub -= HandleSub;
        API.OnRaid -= HandleRaid;
    }

    private void HandleMessage(TwitchMessage message)
    {
        Debug.Log($"[{message.User.DisplayName}]: {message.Message}");
    }

    private void HandleCheer(TwitchCheerEvent cheer)
    {
        Debug.Log($"{cheer.User.DisplayName} cheered {cheer.CheerAmount} bits!");
    }

    private void HandleSub(TwitchSubEvent sub)
    {
        //...
    }

    private void HandleRaid(TwitchRaidEvent raid)
    {
        Debug.Log($"Raid incoming! {raid.User.DisplayName} is raiding with {raid.ViewerCount} viewers!");
    }
}
```

</details>

## Developer Contact

**Report bugs, suggest features, or provide feedback:**

| **Discord Server** | **Forum** | **Post** |  
|--------------------|-----------|----------|  
| [Lethal Company Modding](https://discord.gg/XeyYqRdRGC) | `#mod-releases` | [TwitchChatAPI](https://discord.com/channels/1168655651455639582/1324949317030772838) |  
| [Unofficial Lethal Company Community](https://discord.gg/nYcQFEpXfU) | `#mod-releases` | [TwitchChatAPI](https://discord.com/channels/1169792572382773318/1324949327453356145) |  

- **GitHub Issues Page:** [TwitchChatAPI](https://github.com/ZehsTeam/TwitchChatAPI/issues)
- **My Links:** https://solo.to/crithaxxog

[<img src="https://i.imgur.com/duJZQTS.png" width="200px">](https://ko-fi.com/zehsteam)
