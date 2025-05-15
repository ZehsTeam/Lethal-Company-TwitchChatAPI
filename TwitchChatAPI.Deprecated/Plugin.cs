using BepInEx;
using System;
using NewMyPluginInfo = TwitchChatAPI.MyPluginInfo;

namespace com.github.zehsteam.TwitchChatAPI;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(NewMyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
internal class Plugin : BaseUnityPlugin
{
    internal static Plugin Instance { get; private set; }

    #pragma warning disable IDE0051 // Remove unused private members
    private void Awake()
    #pragma warning restore IDE0051 // Remove unused private members
    {
        Instance = this;

        TwitchChatAPI.Logger.Initialize(BepInEx.Logging.Logger.CreateLogSource(MyPluginInfo.PLUGIN_GUID));
        TwitchChatAPI.Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} has awoken!");

        #pragma warning disable CS0612 // Type or member is obsolete
        InitializeAPI();
        #pragma warning restore CS0612 // Type or member is obsolete
    }

    [Obsolete]
    private void InitializeAPI()
    {
        API.Initialize();
    }
}
