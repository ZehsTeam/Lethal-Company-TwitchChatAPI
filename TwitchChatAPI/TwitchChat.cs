using com.github.zehsteam.TwitchChatAPI.Enums;
using com.github.zehsteam.TwitchChatAPI.Objects;
using Newtonsoft.Json;
using System;
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
        catch (Exception ex)
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
        catch (Exception ex)
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
                ProcessPRIVMSG(message);
            }
            else if (message.StartsWith("@") && message.Contains("USERNOTICE"))
            {
                ProcessUSERNOTICE(message);
            }
            else if (message.StartsWith("@") && message.Contains("ROOMSTATE"))
            {
                ProcessROOMSTATE(message);
            }
            else
            {
                Plugin.Instance.LogInfoExtended($"Unhandled RAW message: {message}");
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to process message: {message} Error: {ex}");
        }
    }

    private static void ProcessPRIVMSG(string message)
    {
        // Parse metadata tags
        var tagsSection = message.Split(' ')[0].Substring(1); // Remove leading '@'
        var tags = tagsSection.Split(';').ToDictionary(
            tag => tag.Split('=')[0],
            tag => tag.Contains('=') ? tag.Split('=')[1] : ""
        );

        // Extract user and message details
        var contentSection = message.Split("PRIVMSG")[1];
        var contentParts = contentSection.Split(':');
        var userName = message.Split('!')[0].Split(':')[1];
        var channel = contentParts[0].Trim();
        var chatMessage = contentParts[1].Trim();

        // Determine broadcaster status
        var channelName = Channel.TrimStart('#').ToLower();
        var isBroadcaster = userName.ToLower() == channelName;

        // Populate TwitchMessage
        var twitchMessage = new TwitchMessage
        {
            Channel = channel,
            DisplayName = tags.TryGetValue("display-name", out var displayName) ? displayName : userName,
            Username = userName,
            Message = chatMessage,
            Color = tags.TryGetValue("color", out var color) ? color : "#FFFFFF",
            IsBroadcaster = isBroadcaster,
            IsModerator = tags.TryGetValue("mod", out var mod) && mod == "1",
            IsSubscriber = tags.TryGetValue("subscriber", out var sub) && sub == "1",
            Tags = tags // Retain raw tags for extensibility
        };

        Plugin.Instance.LogInfoExtended($"[{twitchMessage.DisplayName}] {twitchMessage.Message}");

        API.InvokeOnMessage(twitchMessage);
    }

    private static void ProcessUSERNOTICE(string message)
    {
        // Parse metadata tags
        string tagsSection = message.Split(' ')[0].Substring(1); // Remove leading '@'
        var tags = tagsSection.Split(';').ToDictionary(
            tag => tag.Split('=')[0],
            tag => tag.Contains('=') ? tag.Split('=')[1] : ""
        );

        // Extract channel
        string channel = message.Split("USERNOTICE")[1];
        string channelName = channel.TrimStart('#').ToLower();

        string msgId = tags.GetValueOrDefault("msg-id", defaultValue: string.Empty);

        if (string.IsNullOrWhiteSpace(msgId))
        {
            Plugin.Logger.LogError($"Failed to process USERNOTICE message: {message}");
            return;
        }

        string displayName = tags.GetValueOrDefault("display-name", defaultValue: string.Empty);
        string color = tags.GetValueOrDefault("color", defaultValue: "#FFFFFF");

        // Handle Subscription and Resubscription
        if (msgId == "sub" || msgId == "resub")
        {
            var subEvent = new TwitchSubEvent
            {
                Channel = channel,
                SubscriberName = displayName,
                CumulativeMonths = int.Parse(tags["msg-param-cumulative-months"]),
                IsResub = msgId == "resub",
                GifterName = tags.ContainsKey("msg-param-gifter-name") ? tags["msg-param-gifter-name"] : null,
                Tags = tags
            };

            Plugin.Instance.LogInfoExtended($"RAW subscription message: {message}");
            Plugin.Instance.LogInfoExtended($"Subscription event: {subEvent.SubscriberName} subscribed for {subEvent.CumulativeMonths} months! Gifter: {subEvent.GifterName ?? "None"}");

            API.InvokeOnSub(subEvent);
        }

        // Handle Raid event
        if (msgId == "raid")
        {
            var raidEvent = new TwitchRaidEvent
            {
                Channel = channel,
                RaiderName = tags["msg-param-displayName"],
                ViewerCount = int.Parse(tags["msg-param-viewerCount"])
            };

            Plugin.Instance.LogInfoExtended($"RAW raid message: {message}");
            Plugin.Instance.LogInfoExtended($"Raid detected: {raidEvent.RaiderName} is raiding with {raidEvent.ViewerCount} viewers!");

            API.InvokeOnRaid(raidEvent);
        }

        // Handle Cheer event
        if (msgId == "cheer")
        {
            var cheerEvent = new TwitchCheerEvent
            {
                Channel = channel,
                CheerUserName = twitchMessage.DisplayName,
                CheerAmount = int.Parse(tags["msg-param-currency"]),
            };

            Plugin.Instance.LogInfoExtended($"RAW cheer message: {message}");
            Plugin.Instance.LogInfoExtended($"Cheer event: {cheerEvent.CheerUserName} cheered {cheerEvent.CheerAmount} bits!");

            API.InvokeOnCheer(cheerEvent);
        }
    }

    private static void ProcessROOMSTATE(string message)
    {
        try
        {
            // Parse metadata tags
            var tagsSection = message.Split(' ')[0].Substring(1); // Remove leading '@'
            var tags = tagsSection.Split(';').ToDictionary(
                tag => tag.Split('=')[0],
                tag => tag.Contains('=') ? tag.Split('=')[1] : ""
            );

            // Extract channel
            var channel = message.Split("USERNOTICE")[1];
            var channelName = channel.TrimStart('#').ToLower();

            var roomState = new TwitchRoomState
            {
                Channel = channelName,
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
            Plugin.Logger.LogError($"Failed to process ROOMSTATE message. {ex}");
        }
    }
}
