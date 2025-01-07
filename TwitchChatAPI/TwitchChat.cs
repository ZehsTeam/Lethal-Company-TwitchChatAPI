using com.github.zehsteam.TwitchChatAPI.Enums;
using com.github.zehsteam.TwitchChatAPI.Helpers;
using com.github.zehsteam.TwitchChatAPI.MonoBehaviours;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace com.github.zehsteam.TwitchChatAPI;

internal static class TwitchChat
{
    public const string ServerIP = "irc.chat.twitch.tv";
    public const int ServerPort = 6667;

    public static bool Enabled => Plugin.ConfigManager.TwitchChat_Enabled.Value;
    public static string Channel => $"#{Plugin.ConfigManager.TwitchChat_Channel.Value}".Trim();

    public static ConnectionState ConnectionState
    {
        get => _connectionState;
        private set
        {
            _connectionState = value;
            PluginCanvas.Instance?.UpdateSettingsWindowConnectionStatus();
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

    static TwitchChat()
    {
        Application.quitting += OnApplicationQuit;
    }
    
    public static void Connect()
    {
        Task.Run(ConnectAsync);
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
            Plugin.Logger.LogError("Failed to connect to Twitch chat. Twitch chat has been disabled in the config settings.");
            return;
        }

        if (ConnectionState == ConnectionState.Connecting)
        {
            Plugin.Logger.LogWarning("Twitch chat is already connecting.");
            return;
        }

        if (ConnectionState == ConnectionState.Connected)
        {
            Disconnect();
        }
        
        if (string.IsNullOrWhiteSpace(Channel) || Channel == "#")
        {
            Plugin.Logger.LogWarning("Failed to start Twitch chat connection: Invalid or empty channel name.");
            return;
        }

        Plugin.Logger.LogInfo("Establishing connection to Twitch chat...");

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
            await _writer.WriteLineAsync($"JOIN {Channel}");

            ConnectionState = ConnectionState.Connected;
            _explicitDisconnect = false;

            Plugin.Logger.LogInfo($"Successfully connected to Twitch chat {Channel}.");

            API.InvokeOnConnect();

            await Task.Run(ListenAsync, _cts.Token);
        }
        catch (System.Exception ex)
        {
            ConnectionState = ConnectionState.Disconnected;
            _explicitDisconnect = false;

            Plugin.Logger.LogError($"Failed to connect to Twitch chat {Channel}. {ex}");

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
                Plugin.Logger.LogInfo("Twitch chat is not connected or already disconnecting.");
                return;
            }

            ConnectionState = ConnectionState.Disconnecting;

            _cts?.Cancel();

            DisconnectStreams();

            ConnectionState = ConnectionState.Disconnected;

            Plugin.Logger.LogInfo("Twitch chat connection stopped.");

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

            Plugin.Logger.LogInfo($"Reconnection to Twitch chat will be attempted in {_reconnectDelay / 1000} seconds.");

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

                Plugin.Logger.LogInfo("Attempting to reconnect to Twitch chat...");
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
                    Plugin.Instance.LogInfoExtended("Received PING, sending PONG...");
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
            Plugin.Logger.LogInfo("Twitch chat listen task canceled.");
        }
        catch (System.OperationCanceledException)
        {
            Plugin.Logger.LogInfo("Twitch chat listen task canceled.");
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Twitch chat listen task failed. {ex}");

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
                throw new System.OperationCanceledException(cancellationToken);
            }
        }
        finally
        {
            _readLock.Release();
        }
    }

    private static void OnApplicationQuit()
    {
        Plugin.Logger.LogInfo("Application is quitting. Disconnecting Twitch chat...");
        Disconnect();
    }
}
