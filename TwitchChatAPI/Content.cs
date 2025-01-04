using System.IO;
using UnityEngine;

namespace com.github.zehsteam.TwitchChatAPI;

internal static class Content
{
    // Prefabs
    public static GameObject PluginCanvasPrefab { get; private set; }

    public static void Load()
    {
        LoadAssetsFromAssetBundle();
    }

    private static void LoadAssetsFromAssetBundle()
    {
        AssetBundle assetBundle = LoadAssetBundle("twitchchatapi_assets");
        if (assetBundle == null) return;

        // Prefabs
        PluginCanvasPrefab = LoadAssetFromAssetBundle<GameObject>("TwitchChatAPICanvas", assetBundle);

        Plugin.Logger.LogInfo("Successfully loaded assets from AssetBundle!");
    }

    private static AssetBundle LoadAssetBundle(string fileName)
    {
        try
        {
            var dllFolderPath = Path.GetDirectoryName(Plugin.Instance.Info.Location);
            var assetBundleFilePath = Path.Combine(dllFolderPath, fileName);
            return AssetBundle.LoadFromFile(assetBundleFilePath);
        }
        catch (System.Exception e)
        {
            Plugin.Logger.LogError($"Failed to load AssetBundle \"{fileName}\". {e}");
        }

        return null;
    }

    private static T LoadAssetFromAssetBundle<T>(string name, AssetBundle assetBundle) where T : Object
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Plugin.Logger.LogError($"Failed to load asset of type \"{typeof(T).Name}\" from AssetBundle. Name is null or whitespace.");
            return null;
        }

        if (assetBundle == null)
        {
            Plugin.Logger.LogError($"Failed to load asset of type \"{typeof(T).Name}\" with name \"{name}\" from AssetBundle. AssetBundle is null.");
            return null;
        }

        T asset = assetBundle.LoadAsset<T>(name);

        if (asset == null)
        {
            Plugin.Logger.LogError($"Failed to load asset of type \"{typeof(T).Name}\" with name \"{name}\" from AssetBundle. No asset found with that type and name.");
            return null;
        }

        return asset;
    }
}
