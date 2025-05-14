using BepInEx.Configuration;

namespace TwitchChatAPI;

internal static class ConfigManager
{
    public static ConfigFile ConfigFile { get; private set; }

    // General
    public static ConfigEntry<bool> ExtendedLogging { get; private set; }

    // Twitch Chat
    public static ConfigEntry<bool> TwitchChat_Enabled { get; private set; }
    public static ConfigEntry<string> TwitchChat_Channel { get; private set; }

    public static void Initialize(ConfigFile configFile)
    {
        ConfigFile = configFile;
        BindConfigs();
    }

    private static void BindConfigs()
    {
        // General
        ExtendedLogging = ConfigFile.Bind("General", "ExtendedLogging", defaultValue: false, "Enable extended logging.");

        // Twitch Chat
        TwitchChat_Enabled = ConfigFile.Bind("Twitch Chat", "Enabled", defaultValue: true, "Enable/Disable the connection to Twitch chat.");
        TwitchChat_Channel = ConfigFile.Bind("Twitch Chat", "Channel", defaultValue: "",   "Your Twitch channel username.");

        TwitchChat_Enabled.SettingChanged += (_, _) => TwitchChat.HandleEnabledChanged();
        TwitchChat_Channel.SettingChanged += (_, _) => TwitchChat.HandleChannelChanged();
    }
}
