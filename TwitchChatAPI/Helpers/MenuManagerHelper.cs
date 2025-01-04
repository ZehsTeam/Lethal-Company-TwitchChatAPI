namespace com.github.zehsteam.TwitchChatAPI.Helpers;

internal static class MenuManagerHelper
{
    public static MenuManager Instance { get; private set; }

    public static void SetInstance(MenuManager menuManager)
    {
        Instance = menuManager;
    }
}
