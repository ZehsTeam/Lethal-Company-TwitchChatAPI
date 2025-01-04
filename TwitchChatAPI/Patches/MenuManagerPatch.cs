using com.github.zehsteam.TwitchChatAPI.Helpers;
using HarmonyLib;

namespace com.github.zehsteam.TwitchChatAPI.Patches;

[HarmonyPatch(typeof(MenuManager))]
internal static class MenuManagerPatch
{
    [HarmonyPatch(nameof(MenuManager.Awake))]
    [HarmonyPrefix]
    private static void AwakePatch(MenuManager __instance)
    {
        MenuManagerHelper.SetInstance(__instance);
    }

    [HarmonyPatch(nameof(MenuManager.Start))]
    [HarmonyPostfix]
    private static void StartPatch(MenuManager __instance)
    {
        if (__instance.isInitScene) return;

        Plugin.Instance.SpawnPluginCanvas();
    }
}
