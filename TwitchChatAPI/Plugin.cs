using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using com.github.zehsteam.TwitchChatAPI.Dependencies;
using com.github.zehsteam.TwitchChatAPI.MonoBehaviours;
using com.github.zehsteam.TwitchChatAPI.Patches;
using HarmonyLib;

namespace com.github.zehsteam.TwitchChatAPI;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(LethalConfigProxy.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(MoreCompanyProxy.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
internal class Plugin : BaseUnityPlugin
{
    private readonly Harmony _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

    internal static Plugin Instance { get; private set; }
    internal static new ManualLogSource Logger { get; private set; }
    internal static new ConfigFile Config { get; private set; }

    internal static ConfigManager ConfigManager { get; private set; }

    #pragma warning disable IDE0051 // Remove unused private members
    private void Awake()
    #pragma warning restore IDE0051 // Remove unused private members
    {
        if (Instance == null) Instance = this;

        Logger = BepInEx.Logging.Logger.CreateLogSource(MyPluginInfo.PLUGIN_GUID);
        Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} has awoken!");

        Config = Utils.CreateGlobalConfigFile();

        _harmony.PatchAll(typeof(MenuManagerPatch));
        _harmony.PatchAll(typeof(QuickMenuManagerPatch));

        Content.Load();

        ConfigManager = new ConfigManager();

        if (ConfigManager.TwitchChat_Enabled.Value)
        {
            TwitchChat.Connect();
        }
    }

    public void SpawnPluginCanvas()
    {
        if (PluginCanvas.Instance != null)
        {
            return;
        }

        Instantiate(Content.PluginCanvasPrefab);

        Logger.LogInfo("Spawned PluginCanvas.");
    }

    public void LogInfoExtended(object data)
    {
        LogExtended(LogLevel.Info, data);
    }

    public void LogWarningExtended(object data)
    {
        LogExtended(LogLevel.Warning, data);
    }

    public void LogExtended(LogLevel level, object data)
    {
        if (ConfigManager == null || ConfigManager.ExtendedLogging == null)
        {
            Logger.Log(level, data);
            return;
        }

        if (ConfigManager.ExtendedLogging.Value)
        {
            Logger.Log(level, data);
        }
    }
}
