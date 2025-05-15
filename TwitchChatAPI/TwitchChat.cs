using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TwitchChatAPI.Enums;
using TwitchChatAPI.Helpers;
using UnityEngine;

namespace TwitchChatAPI;

internal static class TwitchChat
{
    public const string ServerIP = "irc.chat.twitch.tv";
    public const int ServerPort = 6667;

    public static bool Enabled => ConfigManager.TwitchChat_Enabled.Value;
    public static string Channel => ConfigManager.TwitchChat_Channel.Value.Trim();

    public static ConnectionState ConnectionState
    {
        get => _connectionState;
        private set
        {
            if (_connectionState != value)
            {
                _connectionState = value;
                API.InvokeOnConnectionStateChanged(_connectionState);
            }
        }
    }

    private static ConnectionState _connectionState = ConnectionState.None;

    private static TcpClient _client;
    private static NetworkStream _stream;
    private static StreamReader _reader;
    private static StreamWriter _writer;
    private static CancellationTokenSource _cts;

    private static bool _isReconnecting;
    private static Task _reconnectTask;
    private static CancellationTokenSource _reconnectCts;
    private static int _reconnectDelay = 5000; // 5 seconds
    private static bool _explicitDisconnect;

    private static readonly object _connectionLock = new object();
    private static readonly SemaphoreSlim _readLock = new SemaphoreSlim(1, 1);

    public static void Initialize()
    {
        Application.quitting += OnApplicationQuit;

        if (Enabled)
        {
            Connect();
        }
    }
    
    public static void Connect()
    {
        Task.Run(ConnectAsync);
    }

    public static void Connect(string channel)
    {
        ConfigManager.TwitchChat_Channel.Value = channel;
        Connect();
    }

    public static async Task ConnectAsync()
    {
        lock (_connectionLock)
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
            Logger.LogError("Failed to connect to Twitch chat. Twitch chat has been disabled in the config settings.");
            return;
        }

        if (ConnectionState == ConnectionState.Connecting)
        {
            Logger.LogWarning("Twitch chat is already connecting.");
            return;
        }

        if (ConnectionState == ConnectionState.Connected)
        {
            Disconnect();
        }
        
        if (!UserHelper.IsValidUsername(Channel))
        {
            Logger.LogWarning("Failed to start Twitch chat connection: Invalid or empty channel name.");
            return;
        }

        Logger.LogInfo("Establishing connection to Twitch chat...");

        ConnectionState = ConnectionState.Connecting;

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
            //await _writer.WriteLineAsync("CAP REQ :twitch.tv/membership"); // Request join and part messages
            await _writer.WriteLineAsync($"JOIN #{Channel}");

            ConnectionState = ConnectionState.Connected;
            _explicitDisconnect = false;

            Logger.LogInfo($"Successfully connected to Twitch chat {Channel}.");

            API.InvokeOnConnect();

            await Task.Run(ListenAsync, _cts.Token);
        }
        catch (Exception ex)
        {
            ConnectionState = ConnectionState.Disconnected;
            _explicitDisconnect = false;

            Logger.LogError($"Failed to connect to Twitch chat {Channel}. {ex}");

            ScheduleReconnect();
        }
    }

    public static void Disconnect()
    {
        lock (_connectionLock)
        {
            _explicitDisconnect = true;
            CancelReconnect();

            if (ConnectionState != ConnectionState.Connected && ConnectionState != ConnectionState.Connecting)
            {
                Logger.LogInfo("Twitch chat is not connected or already disconnecting.");
                return;
            }

            ConnectionState = ConnectionState.Disconnecting;

            _cts?.Cancel();

            DisconnectStreams();

            ConnectionState = ConnectionState.Disconnected;

            Logger.LogInfo("Twitch chat connection stopped.");

            API.InvokeOnDisconnect();
        }
    }

    private static void DisconnectStreams()
    {
        _writer?.Dispose();
        _reader?.Dispose();
        _stream?.Dispose();
        _client?.Close();

        _writer = null;
        _reader = null;
        _stream = null;
        _client = null;
    }

    private static void ScheduleReconnect()
    {
        lock (_connectionLock)
        {
            if (!Enabled || _explicitDisconnect || _isReconnecting)
            {
                return;
            }

            Logger.LogInfo($"Reconnection to Twitch chat will be attempted in {_reconnectDelay / 1000} seconds.");

            _isReconnecting = true;

            _reconnectCts = new CancellationTokenSource();
            _reconnectTask = Task.Delay(_reconnectDelay, _reconnectCts.Token).ContinueWith(async task =>
            {
                if (task.IsCanceled) return;

                lock (_connectionLock)
                {
                    _isReconnecting = false;
                }

                if (!Enabled) return;

                Logger.LogInfo("Attempting to reconnect to Twitch chat...");
                await ConnectAsync();
            });
        }
    }

    private static void CancelReconnect()
    {
        lock (_connectionLock)
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
        if (ConnectionState != ConnectionState.Connected || _reader == null)
        {
            return;
        }

        try
        {
            while (_cts != null && !_cts.Token.IsCancellationRequested)
            {
                lock (_connectionLock) // Ensure no overlapping access to _reader
                {
                    if (_reader == null) break;
                }

                string message = await SafeReadLineAsync(_cts.Token);
                if (message == null) continue;

                if (message.StartsWith("PING"))
                {
                    Logger.LogInfo("Received PING, sending PONG...", extended: true);
                    await (_writer?.WriteLineAsync("PONG :tmi.twitch.tv") ?? Task.CompletedTask).ConfigureAwait(false);
                }
                else
                {
                    MessageHelper.ProcessMessage(message);
                }
            }
        }
        catch (TaskCanceledException)
        {
            Logger.LogInfo("Twitch chat listen task canceled.");
        }
        catch (OperationCanceledException)
        {
            Logger.LogInfo("Twitch chat listen task canceled.");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Twitch chat listen task failed. {ex}");

            ScheduleReconnect();
        }
        finally
        {
            lock (_connectionLock)
            {
                ConnectionState = ConnectionState.Disconnected;
            }
        }
    }

    private static async Task<string> SafeReadLineAsync(CancellationToken cancellationToken)
    {
        await _readLock.WaitAsync(cancellationToken); // Await lock with cancellation support

        try
        {
            // Wrap ReadLineAsync in a cancellable task
            var readTask = _reader.ReadLineAsync();

            // Wait for either the read to complete or the token to be canceled
            var completedTask = await Task.WhenAny(readTask, Task.Delay(Timeout.Infinite, cancellationToken)).ConfigureAwait(false);

            if (completedTask == readTask)
            {
                // Read operation completed successfully
                return await readTask.ConfigureAwait(false);
            }
            else
            {
                // Task.Delay was completed, meaning cancellation was requested
                throw new OperationCanceledException(cancellationToken);
            }
        }
        finally
        {
            _readLock.Release();
        }
    }

    private static void OnApplicationQuit()
    {
        Logger.LogInfo("Application is quitting. Disconnecting Twitch chat...");
        Disconnect();
    }

    public static void HandleEnabledChanged()
    {
        if (Enabled)
        {
            Connect();
        }
        else
        {
            Disconnect();
        }
    }

    public static void HandleChannelChanged()
    {
        if (Enabled)
        {
            Connect();
        }
    }
}
