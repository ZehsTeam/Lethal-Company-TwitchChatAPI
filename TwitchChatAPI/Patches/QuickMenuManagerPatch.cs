using com.github.zehsteam.TwitchChatAPI.Helpers;
using com.github.zehsteam.TwitchChatAPI.MonoBehaviours;
using HarmonyLib;

namespace com.github.zehsteam.TwitchChatAPI.Patches;

[HarmonyPatch(typeof(QuickMenuManager))]
internal static class QuickMenuManagerPatch
{
    [HarmonyPatch(nameof(QuickMenuManager.Start))]
    [HarmonyPrefix]
    private static void StartPatch(QuickMenuManager __instance)
    {
        QuickMenuManagerHelper.SetInstance(__instance);
        Plugin.Instance.SpawnPluginCanvas();
    }

    [HarmonyPatch(nameof(QuickMenuManager.OpenQuickMenu))]
    [HarmonyPostfix]
    private static void OpenQuickMenuPatch()
    {
        PluginCanvas.Instance?.UpdateSettingsButton();
    }

    [HarmonyPatch(nameof(QuickMenuManager.CloseQuickMenu))]
    [HarmonyPostfix]
    private static void CloseQuickMenuPatch()
    {
        PluginCanvas.Instance?.CloseSettingsWindow();
    }
}
