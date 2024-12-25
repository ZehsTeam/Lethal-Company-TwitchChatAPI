using com.github.zehsteam.TwitchChatAPI.Enums;
using com.github.zehsteam.TwitchChatAPI.Objects;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace com.github.zehsteam.TwitchChatAPI;

internal static class TwitchChat
{
    public const string ServerIP = "irc.chat.twitch.tv";
    public const int ServerPort = 6667;

    public static bool Enabled => Plugin.ConfigManager.TwitchChat_Enabled.Value;
    public static string Channel => $"#{Plugin.ConfigManager.TwitchChat_Channel.Value}".Trim().Replace(" ", "_");

    private static TcpClient _client;
    private static NetworkStream _stream;
    private static StreamReader _reader;
    private static StreamWriter _writer;
    private static CancellationTokenSource _cts;

    private static ConnectionState _connectionState = ConnectionState.None;
    private static bool _isReconnecting;
    private static int _reconnectDelay = 5000; // 5 seconds

    public static void Connect()
    {
        Task.Run(ConnectAsync);
    }

    public static async Task ConnectAsync()
    {
        _isReconnecting = false;

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
            _isReconnecting = true;
            Disconnect();
        }
        
        if (string.IsNullOrWhiteSpace(Channel) || Channel == "#")
        {
            _isReconnecting = false;
            Plugin.Logger.LogWarning("Failed to start Twitch chat connection: Invalid or empty channel name.");
            return;
        }

        Plugin.Logger.LogInfo("Establishing connection to Twitch chat...");

        _connectionState = ConnectionState.Connecting;

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
            _isReconnecting = false;

            Plugin.Logger.LogInfo($"Successfully connected to Twitch chat {Channel}.");

            API.InvokeOnConnect();

            await Task.Run(ListenAsync, _cts.Token);
        }
        catch (System.Exception ex)
        {
            _connectionState = ConnectionState.Disconnected;
            _isReconnecting = false;

            Plugin.Logger.LogError($"Failed to connect to Twitch chat {Channel}. {ex}");
        }
    }

    public static void Disconnect()
    {
        if (_connectionState != ConnectionState.Connected && _connectionState != ConnectionState.Connecting)
        {
            Plugin.Logger.LogInfo("Twitch chat is not connected or already disconnecting.");
            return;
        }

        _connectionState = ConnectionState.Disconnecting;

        // Cancel ongoing connection attempt (if any)
        _cts?.Cancel();

        // Close the connection
        _client?.Close();
        _reader?.Close();
        _writer?.Close();
        _stream?.Close();

        Plugin.Logger.LogInfo("Twitch chat connection stopped.");

        API.InvokeOnDisconnect();
    }

    private static async Task ListenAsync()
    {
        try
        {
            while (_cts != null && !_cts.Token.IsCancellationRequested && !_reader.EndOfStream)
            {
                string message = await _reader.ReadLineAsync();
                if (message == null) continue;

                if (message.StartsWith("PING"))
                {
                    await _writer.WriteLineAsync("PONG :tmi.twitch.tv");

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
            // Task was canceled
            if (!_isReconnecting)
            {
                Plugin.Logger.LogError($"Twitch chat listen task canceled.");
            }
        }
        catch (System.Exception ex)
        {
            if (Enabled && !_isReconnecting)
            {
                Plugin.Logger.LogError($"Twitch chat listen task failed. {ex}");
            }
        }
        finally
        {
            _connectionState = ConnectionState.Disconnected;
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
                tag => tag.Contains('=') ? tag.Split('=')[1] : ""
            );

            string contentSection = message.Split("PRIVMSG")[1];
            string[] contentParts = contentSection.Split(':');
            string channel = contentParts[0].Trim().TrimStart('#');
            string chatMessage = contentParts.Length > 1 ? contentParts[1].Trim() : string.Empty;

            TwitchUser twitchUser = GetTwitchUser(channel, tags);

            var twitchMessage = new TwitchMessage
            {
                Channel = channel,
                User = twitchUser,
                Message = chatMessage,
                Tags = tags // Retain raw tags for extensibility
            };

            Plugin.Instance.LogInfoExtended($"\n{JsonConvert.SerializeObject(twitchMessage, Formatting.Indented)}");

            API.InvokeOnMessage(twitchMessage);
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
                tag => tag.Contains('=') ? tag.Split('=')[1] : ""
            );

            string msgId = tags.GetValueOrDefault("msg-id", defaultValue: string.Empty);

            if (string.IsNullOrEmpty(msgId))
            {
                Plugin.Logger.LogError($"Failed to process USERNOTICE message:\n\n{message}");
                return;
            }

            string contentSection = message.Split("USERNOTICE")[1];
            string[] contentParts = contentSection.Split(':');
            string channel = contentParts[0].Trim().TrimStart('#');
            string chatMessage = contentParts.Length > 1 ? contentParts[1].Trim() : string.Empty;

            TwitchUser twitchUser = GetTwitchUser(channel, tags);
            if (twitchUser.Equals(default)) return;

            if (msgId == "sub" || msgId == "resub" || msgId == "subgift" || msgId == "submysterygift")
            {
                ProcessMessage_USERNOTICE_Sub(message, channel, twitchUser, chatMessage, tags);
            }

            if (msgId == "raid")
            {
                ProcessMessage_USERNOTICE_Cheer(message, channel, twitchUser, chatMessage, tags);
            }

            if (msgId == "cheer")
            {
                ProcessMessage_USERNOTICE_Raid(message, channel, twitchUser, chatMessage, tags);
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

            int tier = 1;

            if (tags.TryGetValue("msg-param-sub-plan", out string subPlan))
            {
                if (subPlan == "Prime")
                {
                    subType = SubType.Prime;
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

    private static void ProcessMessage_USERNOTICE_Cheer(string message, string channel, TwitchUser twitchUser, string chatMessage, Dictionary<string, string> tags)
    {
        try
        {
            var cheerEvent = new TwitchCheerEvent
            {
                Channel = channel,
                User = twitchUser,
                Message = chatMessage,
                Tags = tags,
                CheerAmount = int.Parse(tags.GetValueOrDefault("msg-param-currency", defaultValue: "0")),
            };

            Plugin.Instance.LogInfoExtended($"RAW cheer message: {message}");
            Plugin.Instance.LogInfoExtended($"[!] Cheer event: {cheerEvent.User.DisplayName} cheered {cheerEvent.CheerAmount} bits!\n{JsonConvert.SerializeObject(cheerEvent, Formatting.Indented)}");

            API.InvokeOnCheer(cheerEvent);
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to process USERNOTICE message:\n\n{message}\n\nError: {ex}");
        }
    }

    private static void ProcessMessage_USERNOTICE_Raid(string message, string channel, TwitchUser twitchUser, string chatMessage, Dictionary<string, string> tags)
    {
        try
        {
            var raidEvent = new TwitchRaidEvent
            {
                Channel = channel,
                User = twitchUser,
                Message = chatMessage,
                Tags = tags,
                ViewerCount = int.Parse(tags.GetValueOrDefault("msg-param-viewerCount", defaultValue: "0"))
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
            // Parse metadata tags
            string tagsSection = message.Split(' ')[0].Substring(1); // Remove leading '@'

            var tags = tagsSection.Split(';').ToDictionary(
                tag => tag.Split('=')[0],
                tag => tag.Contains('=') ? tag.Split('=')[1] : ""
            );

            // Extract channel
            string channel = message.Split("ROOMSTATE")[1].Trim().TrimStart('#');

            var roomState = new TwitchRoomState
            {
                Channel = channel,
                IsEmoteOnly = tags.ContainsKey("emote-only") && tags["emote-only"] == "1",
                IsFollowersOnly = tags.ContainsKey("followers-only") && tags["followers-only"] != "-1",
                IsR9K = tags.ContainsKey("r9k") && tags["r9k"] == "1",
                IsSlowMode = tags.ContainsKey("slow") && tags["slow"] != "0",
                IsSubOnly = tags.ContainsKey("subs-only") && tags["subs-only"] == "1"
            };

            Plugin.Instance.LogInfoExtended($"RAW roomstate message: {message}");
            Plugin.Instance.LogInfoExtended($"Room state change detected: \n{JsonConvert.SerializeObject(roomState, Formatting.Indented)}");

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
            string displayName = tags.GetValueOrDefault("display-name", defaultValue: "Anonymous");

            return new TwitchUser
            {
                DisplayName = displayName,
                Color = tags.GetValueOrDefault("color", defaultValue: "#FFFFFF"),
                IsSubscriber = tags.TryGetValue("subscriber", out var sub) && sub == "1",
                IsModerator = tags.TryGetValue("mod", out var mod) && mod == "1",
                IsBroadcaster = displayName.Equals(channel, System.StringComparison.OrdinalIgnoreCase)
            };
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to get TwitchUser. {ex}");
        }

        return default;
    }
}
