using com.github.zehsteam.TwitchChatAPI.Enums;
using com.github.zehsteam.TwitchChatAPI.Objects;
using System;

namespace com.github.zehsteam.TwitchChatAPI.Helpers;

internal static class SaveHelper
{
    private static JsonSave _modpackSave;
    private static JsonSave _globalSave;

    static SaveHelper()
    {
        _modpackSave = new JsonSave(directoryPath: Utils.GetConfigDirectoryPath(), fileName: $"{MyPluginInfo.PLUGIN_NAME}_Save");
        _globalSave = new JsonSave(directoryPath: Utils.GetGlobalConfigDirectoryPath(), fileName: "GlobalSave");
    }

    public static bool KeyExists(string key, SaveLocation saveLocation)
    {
        try
        {
            var fullKey = GetKey(key, saveLocation);
            switch (saveLocation)
            {
                case SaveLocation.CurrentSave:
                    return ES3.KeyExists(fullKey, GetCurrentSaveFilePath());
                case SaveLocation.GeneralSave:
                    return ES3.KeyExists(fullKey, GetGeneralSaveFilePath());
                case SaveLocation.Modpack:
                    return _modpackSave.KeyExists(fullKey);
                case SaveLocation.Global:
                    return _globalSave.KeyExists(fullKey);
                default:
                    Plugin.Logger.LogWarning($"KeyExists: Unknown SaveLocation: {saveLocation}");
                    return false;
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"KeyExists Error: Key: \"{key}\", SaveLocation: {saveLocation}. Exception: {ex.Message}");
            return false;
        }
    }

    public static T LoadValue<T>(string key, SaveLocation saveLocation, T defaultValue = default)
    {
        if (TryLoadValue(key, saveLocation, out T value))
        {
            return value;
        }

        return defaultValue;
    }

    public static bool TryLoadValue<T>(string key, SaveLocation saveLocation, out T value)
    {
        value = default;

        if (!KeyExists(key, saveLocation))
        {
            return false;
        }

        try
        {
            var fullKey = GetKey(key, saveLocation);
            switch (saveLocation)
            {
                case SaveLocation.CurrentSave:
                    value = ES3.Load<T>(fullKey, GetCurrentSaveFilePath(), defaultValue: default);
                    return true;
                case SaveLocation.GeneralSave:
                    value = ES3.Load<T>(fullKey, GetGeneralSaveFilePath(), defaultValue: default);
                    return true;
                case SaveLocation.Modpack:
                    return _modpackSave.TryLoadValue(fullKey, out value);
                case SaveLocation.Global:
                    return _globalSave.TryLoadValue(fullKey, out value);
                default:
                    Plugin.Logger.LogWarning($"LoadValue: Unknown SaveLocation: {saveLocation}");
                    return false;
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"LoadValue Error: Key: \"{key}\", SaveLocation: {saveLocation}. Exception: {ex.Message}");
            return false;
        }
    }

    public static bool SaveValue<T>(string key, T value, SaveLocation saveLocation)
    {
        if (string.IsNullOrWhiteSpace(key) || value == null)
        {
            Plugin.Logger.LogError("SaveValue: Invalid key or value.");
            return false;
        }

        try
        {
            var fullKey = GetKey(key, saveLocation);
            switch (saveLocation)
            {
                case SaveLocation.CurrentSave:
                    ES3.Save(fullKey, value, GetCurrentSaveFilePath());
                    return true;
                case SaveLocation.GeneralSave:
                    ES3.Save(fullKey, value, GetGeneralSaveFilePath());
                    return true;
                case SaveLocation.Modpack:
                    return _modpackSave.SaveValue(fullKey, value);
                case SaveLocation.Global:
                    return _globalSave.SaveValue(fullKey, value);
                default:
                    Plugin.Logger.LogWarning($"SaveValue: Unknown SaveLocation: {saveLocation}");
                    return false;
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"SaveValue Error: Key: \"{key}\", SaveLocation: {saveLocation}. Exception: {ex.Message}");
            return false;
        }
    }

    private static string GetKey(string key, SaveLocation saveLocation)
    {
        return saveLocation switch
        {
            SaveLocation.CurrentSave or SaveLocation.GeneralSave => $"{MyPluginInfo.PLUGIN_GUID}.{key}",
            _ => key
        };
    }

    private static string GetCurrentSaveFilePath()
    {
        if (GameNetworkManager.Instance == null)
        {
            Plugin.Logger.LogWarning("GetCurrentSaveFilePath: GameNetworkManager instance is null. Returning an empty string.");
            return string.Empty;
        }

        return GameNetworkManager.Instance.currentSaveFileName ?? string.Empty;
    }

    private static string GetGeneralSaveFilePath()
    {
        return GameNetworkManager.generalSaveDataName;
    }
}
