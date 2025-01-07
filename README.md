# TwitchChatAPI
#### Add Twitch chat integration to your Lethal Company mods! Subscribe to events like Messages, Cheers, Subs, and Raids! No authentication required.

#### <ins>This mod is fully client-side.</ins>

## <img src="https://i.imgur.com/TpnrFSH.png" width="20px"> Download

Download [TwitchChatAPI](https://thunderstore.io/c/lethal-company/p/Zehs/TwitchChatAPI/) on Thunderstore.

## Usage
<details><summary>Click to Expand</summary>

### API
https://github.com/ZehsTeam/Lethal-Company-TwitchChatAPI/blob/main/TwitchChatAPI/API.cs

```cs
namespace com.github.zehsteam.TwitchChatAPI;

public static class API
{
    public static ConnectionState ConnectionState { get; }
    public static event Action OnConnect;
    public static event Action OnDisconnect;
    public static event Action<TwitchMessage> OnMessage;
    public static event Action<TwitchCheerEvent> OnCheer;
    public static event Action<TwitchSubEvent> OnSub;
    public static event Action<TwitchRaidEvent> OnRaid;
    public static event Action<TwitchRoomState> OnRoomStateUpdate;
}
```

### TwitchUser
https://github.com/ZehsTeam/Lethal-Company-TwitchChatAPI/blob/main/TwitchChatAPI/Objects/TwitchUser.cs

### TwitchMessage
https://github.com/ZehsTeam/Lethal-Company-TwitchChatAPI/blob/main/TwitchChatAPI/Objects/TwitchMessage.cs

### TwitchEvents (Cheer, Sub, Raid)
https://github.com/ZehsTeam/Lethal-Company-TwitchChatAPI/blob/main/TwitchChatAPI/Objects/TwitchEvents.cs

### Example
```cs
using com.github.zehsteam.TwitchChatAPI;
using com.github.zehsteam.TwitchChatAPI.Enums;
using com.github.zehsteam.TwitchChatAPI.Objects;
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
#### Report bugs, suggest features, or provide feedback:  
- **GitHub Issues Page:** [TwitchChatAPI](https://github.com/ZehsTeam/Lethal-Company-TwitchChatAPI/issues)  

| **Discord Server** | **Forum** | **Post** |  
|--------------------|-----------|----------|  
| [Lethal Company Modding](https://discord.gg/XeyYqRdRGC) | `#mod-releases` | [TwitchChatAPI](https://discord.com/channels/1168655651455639582/1324949317030772838) |  
| [Unofficial Lethal Company Community](https://discord.gg/nYcQFEpXfU) | `#mod-releases` | [TwitchChatAPI](https://discord.com/channels/1169792572382773318/1324949327453356145) |  

- **Email:** crithaxxog@gmail.com  
- **Twitch:** [CritHaxXoG](https://www.twitch.tv/crithaxxog)  
- **YouTube:** [Zehs](https://www.youtube.com/channel/UCb4VEkc-_im0h8DKXlwmIAA)