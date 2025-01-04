using com.github.zehsteam.TwitchChatAPI.Enums;
using com.github.zehsteam.TwitchChatAPI.MonoBehaviours;
using com.github.zehsteam.TwitchChatAPI.Objects;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace com.github.zehsteam.TwitchChatAPI;

internal static class TwitchChat
{
    public const string ServerIP = "irc.chat.twitch.tv";
    public const int ServerPort = 6667;

    public static bool Enabled => Plugin.ConfigManager.TwitchChat_Enabled.Value;
    public static string Channel => $"#{Plugin.ConfigManager.TwitchChat_Channel.Value}".Trim();
    public static ConnectionState ConnectionState => _connectionState;

    private static TcpClient _client;
    private static NetworkStream _stream;
    private static StreamReader _reader;
    private static StreamWriter _writer;
    private static CancellationTokenSource _cts;

    private static ConnectionState _connectionState = ConnectionState.None;
    private static bool _isReconnecting;
    private static Task _reconnectTask;
    private static CancellationTokenSource _reconnectCts;
    private static int _reconnectDelay = 5000; // 5 seconds
    private static bool _explicitDisconnect;

    private static readonly object _lock = new object();

    public static void Connect()
    {
        Task.Run(ConnectAsync);
    }

    public static async Task ConnectAsync()
    {
        lock (_lock)
        {
            if (_isReconnecting)
            {
                CancelReconnect();
            }

            _explicitDisconnect = false;
            _isReconnecting = false;
        }

        if (!Enabled)
        {
            Plugin.Logger.LogError("Failed to connect to Twitch chat. Twitch chat has been disabled in the config settings.");
            return;
        }

        if (_connectionState == ConnectionState.Connecting)
        {
            Plugin.Logger.LogWarning("Twitch chat is already connecting.");
            return;
        }

        if (_connectionState == ConnectionState.Connected)
        {
            Disconnect();
        }
        
        if (string.IsNullOrWhiteSpace(Channel) || Channel == "#")
        {
            Plugin.Logger.LogWarning("Failed to start Twitch chat connection: Invalid or empty channel name.");
            return;
        }

        Plugin.Logger.LogInfo("Establishing connection to Twitch chat...");

        _connectionState = ConnectionState.Connecting;
        PluginCanvas.Instance?.UpdateSettingsWindowConnectionStatus();

        try
        {
            _cts = new CancellationTokenSource();

            _client = new TcpClient();
            await _client.ConnectAsync(ServerIP, ServerPort);

            _stream = _client.GetStream();
            _reader = new StreamReader(_stream);
            _writer = new StreamWriter(_stream) { AutoFlush = true };

            // Authenticate and join channel
            await _writer.WriteLineAsync($"NICK justinfan123");
            await _writer.WriteLineAsync("CAP REQ :twitch.tv/tags"); // Request metadata tags
            await _writer.WriteLineAsync("CAP REQ :twitch.tv/commands"); // Request events
            await _writer.WriteLineAsync($"JOIN {Channel}");

            _connectionState = ConnectionState.Connected;
            PluginCanvas.Instance?.UpdateSettingsWindowConnectionStatus();
            _explicitDisconnect = false;

            Plugin.Logger.LogInfo($"Successfully connected to Twitch chat {Channel}.");

            API.InvokeOnConnect();

            await Task.Run(ListenAsync, _cts.Token);
        }
        catch (System.Exception ex)
        {
            _connectionState = ConnectionState.Disconnected;
            PluginCanvas.Instance?.UpdateSettingsWindowConnectionStatus();
            _explicitDisconnect = false;

            Plugin.Logger.LogError($"Failed to connect to Twitch chat {Channel}. {ex}");

            ScheduleReconnect();
        }
    }

    public static void Disconnect()
    {
        lock (_lock)
        {
            _explicitDisconnect = true;
            CancelReconnect();

            if (_connectionState != ConnectionState.Connected && _connectionState != ConnectionState.Connecting)
            {
                Plugin.Logger.LogInfo("Twitch chat is not connected or already disconnecting.");
                return;
            }

            _connectionState = ConnectionState.Disconnecting;
            PluginCanvas.Instance?.UpdateSettingsWindowConnectionStatus();

            _cts?.Cancel();

            try
            {
                _writer?.Dispose();
                _reader?.Dispose();
                _stream?.Dispose();
                _client?.Close();
            }
            finally
            {
                _writer = null;
                _reader = null;
                _stream = null;
                _client = null;

                Plugin.Logger.LogInfo("Twitch chat connection stopped.");
            }

            _connectionState = ConnectionState.Disconnected;
            PluginCanvas.Instance?.UpdateSettingsWindowConnectionStatus();

            API.InvokeOnDisconnect();
        }
    }

    private static void ScheduleReconnect()
    {
        lock (_lock)
        {
            if (!Enabled || _explicitDisconnect || _isReconnecting)
            {
                return;
            }

            Plugin.Logger.LogInfo($"Reconnection to Twitch chat will be attempted in {_reconnectDelay / 1000} seconds.");

            _isReconnecting = true;

            _reconnectCts = new CancellationTokenSource();
            _reconnectTask = Task.Delay(_reconnectDelay, _reconnectCts.Token).ContinueWith(async t =>
            {
                if (t.IsCanceled) return;

                lock (_lock)
                {
                    _isReconnecting = false;
                }

                if (!Enabled) return;

                Plugin.Logger.LogInfo("Attempting to reconnect to Twitch chat...");
                await ConnectAsync();
            });
        }
    }

    private static void CancelReconnect()
    {
        lock (_lock)
        {
            if (_reconnectTask != null && !_reconnectTask.IsCompleted)
            {
                _reconnectCts?.Cancel();
                _reconnectTask = null;
            }

            _isReconnecting = false;
        }
    }

    private static async Task ListenAsync()
    {
        try
        {
            if (_reader == null) return; // Ensure the reader is not null

            while (_cts != null && !_cts.Token.IsCancellationRequested)
            {
                string message = await _reader.ReadLineAsync();
                if (message == null) continue;

                if (message.StartsWith("PING"))
                {
                    await _writer?.WriteLineAsync("PONG :tmi.twitch.tv");
                    Plugin.Instance.LogInfoExtended("Received PING, sending PONG...");
                }
                else
                {
                    ProcessMessage(message);
                }
            }
        }
        catch (TaskCanceledException)
        {
            // Expected during shutdown
            Plugin.Logger.LogInfo("Twitch chat listen task canceled.");
        }
        catch (System.Exception ex)
        {
            if (Enabled && !_explicitDisconnect)
            {
                Plugin.Logger.LogError($"Twitch chat listen task failed. {ex}");
            }

            ScheduleReconnect();
        }
        finally
        {
            lock (_lock)
            {
                _connectionState = ConnectionState.Disconnected;
                PluginCanvas.Instance?.UpdateSettingsWindowConnectionStatus();
            }
        }
    }

    private static void ProcessMessage(string message)
    {
        try
        {
            if (message.StartsWith("@") && message.Contains("PRIVMSG"))
            {
                ProcessMessage_PRIVMSG(message);
            }
            else if (message.StartsWith("@") && message.Contains("USERNOTICE"))
            {
                ProcessMessage_USERNOTICE(message);
            }
            else if (message.StartsWith("@") && message.Contains("ROOMSTATE"))
            {
                ProcessMessage_ROOMSTATE(message);
            }
            else
            {
                Plugin.Instance.LogInfoExtended($"Unhandled RAW message: {message}");
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to process message:\n\n{message}\n\nError: {ex}");
        }
    }

    private static void ProcessMessage_PRIVMSG(string message)
    {
        try
        {
            string tagsSection = message.Split(' ')[0].Substring(1); // Remove leading '@'
            var tags = tagsSection.Split(';').ToDictionary(
                tag => tag.Split('=')[0],
                tag => tag.Contains('=') ? tag[(tag.IndexOf('=') + 1)..] : string.Empty
            );

            string contentSection = message.Split("PRIVMSG")[1];
            string channel = contentSection.Split(' ', System.StringSplitOptions.RemoveEmptyEntries)[0].Trim().TrimStart('#');
            string chatMessage = string.Empty;

            int indexOfColon = contentSection.IndexOf(':');
            if (indexOfColon != -1)
            {
                chatMessage = contentSection[(indexOfColon + 1)..].Trim(); // Extract chat message
            }

            TwitchUser twitchUser = GetTwitchUser(channel, tags);
            if (twitchUser.Equals(default)) return;

            if (tags.ContainsKey("bits"))
            {
                ProcessMessage_PRIVMSG_Cheer(message, channel, twitchUser, chatMessage, tags);
                return;
            }

            var twitchMessage = new TwitchMessage
            {
                Channel = channel,
                User = twitchUser,
                Message = chatMessage,
                Tags = tags // Retain raw tags for extensibility
            };

            //Plugin.Instance.LogInfoExtended($"\n{JsonConvert.SerializeObject(twitchMessage, Formatting.Indented)}");

            API.InvokeOnMessage(twitchMessage);
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to process PRIVMSG message:\n\n{message}\n\nError: {ex}");
        }
    }

    private static void ProcessMessage_PRIVMSG_Cheer(string message, string channel, TwitchUser twitchUser, string chatMessage, Dictionary<string, string> tags)
    {
        try
        {
            var cheerTypes = new string[]
            {
                "Cheer", "cheerwhal", "Corgo", "uni", "ShowLove", "Party", "SeemsGood", "Pride",
                "Kappa", "FrankerZ", "HeyGuys", "DansGame", "EleGiggle", "TriHard", "Kreygasm",
                "4Head", "SwiftRage", "NotLikeThis", "FailFish", "VoHiYo", "PJSalt", "MrDestructoid",
                "bday", "RIPCheer", "Shamrock"
            };

            string pattern = @"\b(" + string.Join("|", cheerTypes) + @")\d+\b";

            chatMessage = Regex.Replace(chatMessage, pattern, string.Empty, RegexOptions.IgnoreCase).Trim();

            var cheerEvent = new TwitchCheerEvent
            {
                Channel = channel,
                User = twitchUser,
                Message = chatMessage,
                Tags = tags,
                CheerAmount = int.Parse(tags.GetValueOrDefault("bits", "0"))
            };

            Plugin.Instance.LogInfoExtended($"RAW cheer message: {message}");
            Plugin.Instance.LogInfoExtended($"[!] Cheer event: {cheerEvent.User.DisplayName} cheered {cheerEvent.CheerAmount} bits!\n{JsonConvert.SerializeObject(cheerEvent, Formatting.Indented)}");

            API.InvokeOnCheer(cheerEvent);
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to process PRIVMSG message:\n\n{message}\n\nError: {ex}");
        }
    }

    private static void ProcessMessage_USERNOTICE(string message)
    {
        try
        {
            string tagsSection = message.Split(' ')[0].Substring(1); // Remove leading '@'
            var tags = tagsSection.Split(';').ToDictionary(
                tag => tag.Split('=')[0],
                tag => tag.Contains('=') ? tag[(tag.IndexOf('=') + 1)..] : string.Empty
            );

            string msgId = tags.GetValueOrDefault("msg-id", defaultValue: string.Empty);

            if (string.IsNullOrEmpty(msgId))
            {
                Plugin.Logger.LogError($"Failed to process USERNOTICE message:\n\n{message}");
                return;
            }

            string contentSection = message.Split("USERNOTICE")[1];
            string channel = contentSection.Split(' ', System.StringSplitOptions.RemoveEmptyEntries)[0].Trim().TrimStart('#');
            string chatMessage = string.Empty;

            int indexOfColon = contentSection.IndexOf(':');
            if (indexOfColon != -1)
            {
                chatMessage = contentSection[(indexOfColon + 1)..].Trim(); // Extract chat message
            }

            TwitchUser twitchUser = GetTwitchUser(channel, tags);
            if (twitchUser.Equals(default)) return;

            if (msgId == "sub" || msgId == "resub" || msgId == "subgift" || msgId == "submysterygift")
            {
                ProcessMessage_USERNOTICE_Sub(message, channel, twitchUser, chatMessage, tags);
            }
            else if (msgId == "raid")
            {
                ProcessMessage_USERNOTICE_Raid(message, channel, twitchUser, tags);
            }
            else
            {
                Plugin.Instance.LogInfoExtended($"Unhandled USERNOTICE message: {message}");
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to process USERNOTICE message:\n\n{message}\n\nError: {ex}");
        }
    }

    private static void ProcessMessage_USERNOTICE_Sub(string message, string channel, TwitchUser twitchUser, string chatMessage, Dictionary<string, string> tags)
    {
        try
        {
            string msgId = tags.GetValueOrDefault("msg-id", defaultValue: string.Empty);

            if (string.IsNullOrEmpty(msgId))
            {
                Plugin.Logger.LogError($"Failed to process USERNOTICE message: {message}");
                return;
            }

            SubType subType = SubType.Sub;

            switch (msgId)
            {
                case "resub":
                    subType = SubType.Resub;
                    break;
                case "subgift":
                    subType = SubType.SubGift;
                    break;
                case "submysterygift":
                    subType = SubType.SubMysteryGift;
                    break;
            }

            if (subType == SubType.SubGift && tags.ContainsKey("msg-param-community-gift-id"))
            {
                Plugin.Instance.LogInfoExtended($"Skipping subgift since it originates from a submysterygift. Message: {message}");
                return;
            }

            bool isPrime = false;
            int tier = 1;

            if (tags.TryGetValue("msg-param-sub-plan", out string subPlan) && !string.IsNullOrEmpty(subPlan))
            {
                if (subPlan == "Prime")
                {
                    isPrime = true;
                }
                else if (subPlan == "2000")
                {
                    tier = 2;
                }
                else if (subPlan == "3000")
                {
                    tier = 3;
                }
            }

            var subEvent = new TwitchSubEvent
            {
                Channel = channel,
                User = twitchUser,
                Message = chatMessage,
                Tags = tags,
                SubType = subType,
                IsPrime = isPrime,
                Tier = tier,
                Months = int.Parse(tags.GetValueOrDefault("msg-param-cumulative-months", defaultValue: "0")),
                RecipientUser = tags.GetValueOrDefault("msg-param-recipient-display-name", defaultValue: string.Empty),
                GiftCount = int.Parse(tags.GetValueOrDefault("msg-param-mass-gift-count", defaultValue: "0"))
            };

            Plugin.Instance.LogInfoExtended($"RAW subscription message: {message}");
            Plugin.Instance.LogInfoExtended($"[!] Subscription event: \n{JsonConvert.SerializeObject(subEvent, Formatting.Indented)}");

            API.InvokeOnSub(subEvent);
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to process USERNOTICE message:\n\n{message}\n\nError: {ex}");
        }
    }

    private static void ProcessMessage_USERNOTICE_Raid(string message, string channel, TwitchUser twitchUser, Dictionary<string, string> tags)
    {
        try
        {
            var raidEvent = new TwitchRaidEvent
            {
                Channel = channel,
                User = twitchUser,
                Message = string.Empty,
                Tags = tags,
                ViewerCount = int.Parse(tags.GetValueOrDefault("msg-param-viewerCount", "0"))
            };

            Plugin.Instance.LogInfoExtended($"RAW raid message: {message}");
            Plugin.Instance.LogInfoExtended($"[!] Raid detected: {raidEvent.User.DisplayName} is raiding with {raidEvent.ViewerCount} viewers!\n{JsonConvert.SerializeObject(raidEvent, Formatting.Indented)}");

            API.InvokeOnRaid(raidEvent);
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to process USERNOTICE message:\n\n{message}\n\nError: {ex}");
        }
    }

    private static void ProcessMessage_ROOMSTATE(string message)
    {
        try
        {
            string tagsSection = message.Split(' ')[0].Substring(1); // Remove leading '@'
            var tags = tagsSection.Split(';').ToDictionary(
                tag => tag.Split('=')[0],
                tag => tag.Contains('=') ? tag.Split('=')[1] : ""
            );

            string channel = message.Split("ROOMSTATE")[1].Trim().TrimStart('#');

            var roomState = new TwitchRoomState
            {
                Channel = channel,
                IsEmoteOnly = tags.ContainsKey("emote-only") && tags["emote-only"] == "1",
                IsFollowersOnly = tags.ContainsKey("followers-only") && tags["followers-only"] != "-1",
                IsR9K = tags.ContainsKey("r9k") && tags["r9k"] == "1",
                IsSlowMode = tags.ContainsKey("slow") && tags["slow"] != "0",
                IsSubsOnly = tags.ContainsKey("subs-only") && tags["subs-only"] == "1",
                Tags = tags
            };

            Plugin.Instance.LogInfoExtended($"RAW roomstate message: {message}");
            Plugin.Instance.LogInfoExtended($"[!] Room state change detected: \n{JsonConvert.SerializeObject(roomState, Formatting.Indented)}");

            API.InvokeOnRoomStateUpdate(roomState);
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to process ROOMSTATE message:\n\n{message}\n\nError: {ex}");
        }
    }

    private static TwitchUser GetTwitchUser(string channel, Dictionary<string, string> tags)
    {
        try
        {
            string displayName = tags.GetValueOrDefault("display-name", "Anonymous");

            return new TwitchUser
            {
                UserId = tags.GetValueOrDefault("user-id", defaultValue: "0"),
                DisplayName = displayName,
                Color = tags.GetValueOrDefault("color", "#FFFFFF"),
                IsVIP = tags.TryGetValue("vip", out var vip) && vip == "1",
                IsSubscriber = tags.TryGetValue("subscriber", out var sub) && sub == "1",
                IsModerator = tags.TryGetValue("mod", out var mod) && mod == "1",
                IsBroadcaster = displayName.Equals(channel, System.StringComparison.OrdinalIgnoreCase)
            };
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to get TwitchUser: {ex}");
            return default;
        }
    }
}
