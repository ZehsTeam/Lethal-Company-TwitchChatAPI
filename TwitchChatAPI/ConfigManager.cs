using BepInEx.Configuration;
using com.github.zehsteam.TwitchChatAPI.Helpers;

namespace com.github.zehsteam.TwitchChatAPI;

internal class ConfigManager
{
    // General
    public ConfigEntry<bool> ExtendedLogging { get; private set; }

    // Twitch Chat
    public ConfigEntry<bool> TwitchChat_Enabled { get; private set; }
    public ConfigEntry<string> TwitchChat_Channel { get; private set; }
    
    public ConfigManager()
    {
        BindConfigs();
        ConfigHelper.ClearUnusedEntries();
    }

    private void BindConfigs()
    {
        ConfigHelper.SkipAutoGen();

        // General
        ExtendedLogging = ConfigHelper.Bind("General", "ExtendedLogging", defaultValue: false, "Enable extended logging.");

        // Twitch Chat
        TwitchChat_Enabled = ConfigHelper.Bind("Twitch Chat", "Enabled", defaultValue: true, "Enable/Disable the connection to Twitch.");
        TwitchChat_Channel = ConfigHelper.Bind("Twitch Chat", "Channel", defaultValue: "", "Your Twitch channel username.");
        ConfigHelper.AddButton("Twitch Chat", "Refresh Connection", "Refresh the connection to Twitch.", "Refresh", TwitchChat_Refresh_Clicked);
        TwitchChat_Enabled.SettingChanged += (object sender, System.EventArgs e) => TwitchChat_Enabled_SettingChanged();
        TwitchChat_Channel.SettingChanged += (object sender, System.EventArgs e) => TwitchChat_Channel_SettingChanged();
    }

    private void TwitchChat_Enabled_SettingChanged()
    {
        if (TwitchChat_Enabled.Value)
        {
            TwitchChat.Connect();
        }
        else
        {
            TwitchChat.Disconnect();
        }
    }

    private void TwitchChat_Channel_SettingChanged()
    {
        TwitchChat_Refresh_Clicked();
    }

    private void TwitchChat_Refresh_Clicked()
    {
        if (TwitchChat_Enabled.Value)
        {
            TwitchChat.Connect();
        }
    }
}
