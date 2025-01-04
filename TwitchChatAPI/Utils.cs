using BepInEx;
using BepInEx.Configuration;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace com.github.zehsteam.TwitchChatAPI;

internal static class Utils
{
    public static string GetEnumName<T>(T e) where T : System.Enum
    {
        return System.Enum.GetName(typeof(T), e) ?? string.Empty;
    }

    public static string GetPluginDirectoryPath()
    {
        return Path.GetDirectoryName(Plugin.Instance.Info.Location);
    }

    public static string GetConfigDirectoryPath()
    {
        return Paths.ConfigPath;
    }

    public static string GetGlobalConfigDirectoryPath()
    {
        return Path.Combine(Application.persistentDataPath, MyPluginInfo.PLUGIN_NAME);
    }

    public static ConfigFile CreateConfigFile(string directoryPath, string name = null, bool saveOnInit = false)
    {
        BepInPlugin metadata = MetadataHelper.GetMetadata(Plugin.Instance);
        name ??= metadata.GUID;
        name += ".cfg";
        return new ConfigFile(Path.Combine(directoryPath, name), saveOnInit, metadata);
    }

    public static ConfigFile CreateLocalConfigFile(string name = null, bool saveOnInit = false)
    {
        name ??= $"{MyPluginInfo.PLUGIN_GUID}-{name}";
        return CreateConfigFile(Paths.ConfigPath, name, saveOnInit);
    }

    public static ConfigFile CreateGlobalConfigFile(string name = null, bool saveOnInit = false)
    {
        name ??= "global";
        return CreateConfigFile(GetGlobalConfigDirectoryPath(), name, saveOnInit);
    }

    public static void LogStackTrace()
    {
        // Create a new StackTrace object, skipping the first few frames for cleaner output.
        StackTrace stackTrace = new StackTrace();

        // Iterate over all the frames in the stack trace (starting from 1 to skip current method)
        for (int i = 1; i < stackTrace.FrameCount; i++)
        {
            StackFrame frame = stackTrace.GetFrame(i);
            var method = frame.GetMethod();
            var callerClass = method.DeclaringType;
            var callerMethod = method.Name;

            Plugin.Instance.LogInfoExtended($"Call stack depth {i}: {callerClass}.{callerMethod}");
        }
    }
}
