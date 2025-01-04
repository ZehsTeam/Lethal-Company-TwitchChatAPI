using BepInEx.Bootstrap;

namespace com.github.zehsteam.TwitchChatAPI.Dependencies;

internal static class MoreCompanyProxy
{
    public const string PLUGIN_GUID = "me.swipez.melonloader.morecompany";
    public static bool Enabled
    {
        get
        {
            _enabled ??= Chainloader.PluginInfos.ContainsKey(PLUGIN_GUID);
            return _enabled.Value;
        }
    }

    private static bool? _enabled;
}
