using BepInEx;
using BepInEx.Configuration;
using TwitchChatAPI.MonoBehaviours;
using TwitchChatAPI.Objects;

namespace TwitchChatAPI;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; }

    internal static new ConfigFile Config { get; private set; }

    //internal static JsonSave GlobalSave { get; private set; }

    #pragma warning disable IDE0051 // Remove unused private members
    private void Awake()
    #pragma warning restore IDE0051 // Remove unused private members
    {
        Instance = this;

        TwitchChatAPI.Logger.Initialize(BepInEx.Logging.Logger.CreateLogSource(MyPluginInfo.PLUGIN_GUID));
        TwitchChatAPI.Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} has awoken!");

        Config = Utils.CreateGlobalConfigFile(this);

        //GlobalSave = new JsonSave(Utils.GetPluginPersistentDataPath(), "GlobalSave");

        ConfigManager.Initialize(Config);
        MainThreadDispatcher.Initialize();
        TwitchChat.Initialize();
    }
}
