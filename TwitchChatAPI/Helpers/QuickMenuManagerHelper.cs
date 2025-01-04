namespace com.github.zehsteam.TwitchChatAPI.Helpers;

internal static class QuickMenuManagerHelper
{
    public static QuickMenuManager Instance { get; private set; }
    public static bool IsMenuOpen => Instance != null && Instance.isMenuOpen;

    public static void SetInstance(QuickMenuManager quickMenuManager)
    {
        Instance = quickMenuManager;
    }
}
