using com.github.zehsteam.TwitchChatAPI.Dependencies;
using com.github.zehsteam.TwitchChatAPI.Enums;
using com.github.zehsteam.TwitchChatAPI.Helpers;
using Newtonsoft.Json.Bson;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace com.github.zehsteam.TwitchChatAPI.MonoBehaviours;

public class PluginCanvas : MonoBehaviour
{
    public static PluginCanvas Instance { get; private set; }

    [Header("Windows")]
    [Space(5f)]
    public GameObject SettingsWindowObject;
    public GameObject MainMenuObject;
    public GameObject QuickMenuObject;

    [Header("Settings Window Properties")]
    [Space(5f)]
    public TextMeshProUGUI ConnectionStatusText;
    public Toggle EnabledToggle;
    public TMP_InputField ChannelInputField;

    [Header("MainMenu Properties")]
    [Space(5f)]
    public RectTransform MainMenuSettingsButtonTransform;

    [Header("QuickMenu Properties")]
    [Space(5f)]
    public RectTransform QuickMenuSettingsButtonTransform;

    public bool IsSettingsWindowOpen { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        CloseSettingsWindow();
        OpenSettingsWindowFirstTimeOnly();
        UpdateSettingsButton();
    }

    private void OpenSettingsWindowFirstTimeOnly()
    {
        if (!SaveHelper.LoadValue("OpenedSettingsWindowFirstTime", SaveLocation.Global, defaultValue: false))
        {
            SaveHelper.SaveValue("OpenedSettingsWindowFirstTime", value: true, SaveLocation.Global);
            OpenSettingsWindow();
        }
    }

    public void OpenSettingsWindow()
    {
        if (SettingsWindowObject == null || IsSettingsWindowOpen)
        {
            return;
        }

        if (QuickMenuManagerHelper.Instance != null && !QuickMenuManagerHelper.IsMenuOpen)
        {
            return;
        }

        IsSettingsWindowOpen = true;
        SettingsWindowObject.SetActive(true);

        UpdateSettingsWindowUI();
        UpdateSettingsButton();
    }

    public void CloseSettingsWindow()
    {
        if (SettingsWindowObject == null)
        {
            return;
        }

        IsSettingsWindowOpen = false;
        SettingsWindowObject.SetActive(false);

        UpdateSettingsButton();
    }

    public void OnApplyButtonClicked()
    {
        if (EnabledToggle != null)
        {
            Plugin.ConfigManager.TwitchChat_Enabled.Value = EnabledToggle.isOn;
        }

        if (ChannelInputField != null)
        {
            Plugin.ConfigManager.TwitchChat_Channel.Value = ChannelInputField.text;
        }
    }

    public void OnCloseButtonClicked()
    {
        CloseSettingsWindow();
    }

    private void UpdateSettingsWindowUI()
    {
        if (EnabledToggle != null)
        {
            EnabledToggle.isOn = Plugin.ConfigManager.TwitchChat_Enabled.Value;
        }

        if (ChannelInputField != null)
        {
            ChannelInputField.text = Plugin.ConfigManager.TwitchChat_Channel.Value;
        }

        UpdateSettingsWindowConnectionStatus();
    }

    public void UpdateSettingsWindowConnectionStatus()
    {
        if (ConnectionStatusText == null)
        {
            return;
        }

        string stateColor = TwitchChat.ConnectionState switch
        {
            ConnectionState.Connecting => "#00FF00",
            ConnectionState.Connected => "#00FF00",
            ConnectionState.Disconnecting => "#FF0000",
            ConnectionState.Disconnected => "#FF0000",
            _ => string.Empty,
        };

        string state = string.IsNullOrEmpty(stateColor) ? Utils.GetEnumName(TwitchChat.ConnectionState) : $"<color={stateColor}>{Utils.GetEnumName(TwitchChat.ConnectionState)}</color>";
        
        ConnectionStatusText.text = $"Connection Status: {state}";
    }

    public void OnSettingsButtonClicked()
    {
        OpenSettingsWindow();
    }

    public void UpdateSettingsButton()
    {
        UpdateMainMenuUI();
        UpdateQuickMenuUI();
    }

    private void UpdateMainMenuUI()
    {
        if (MainMenuObject == null)
        {
            return;
        }

        MainMenuObject.SetActive(MenuManagerHelper.Instance != null);

        if (MainMenuSettingsButtonTransform == null)
        {
            return;
        }

        MainMenuSettingsButtonTransform.gameObject.SetActive(!IsSettingsWindowOpen);

        if (MoreCompanyProxy.Enabled)
        {
            MainMenuSettingsButtonTransform.anchoredPosition = new Vector2(-145f, 48f);
        }
        else
        {
            MainMenuSettingsButtonTransform.anchoredPosition = new Vector2(-24f, 48f);
        }
    }

    private void UpdateQuickMenuUI()
    {
        if (QuickMenuObject == null)
        {
            return;
        }

        QuickMenuObject.SetActive(QuickMenuManagerHelper.IsMenuOpen);

        if (QuickMenuSettingsButtonTransform == null)
        {
            return;
        }

        QuickMenuSettingsButtonTransform.gameObject.SetActive(!IsSettingsWindowOpen);

        if (MoreCompanyProxy.Enabled)
        {
            QuickMenuSettingsButtonTransform.anchoredPosition = new Vector2(-169, 63f);
        }
        else
        {
            QuickMenuSettingsButtonTransform.anchoredPosition = new Vector2(-52f, 63f);
        }
    }
}
