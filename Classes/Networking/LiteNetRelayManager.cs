using System;
using System.Threading;
using System.Threading.Tasks;
using CasinoRoyale.Utils;
using LiteNetLib;
using LiteNetLib.Utils;

namespace CasinoRoyale.Classes.Networking;

public class LiteNetRelayManager : IDisposable
{
    private readonly NetManager _netManager;
    private readonly INetEventListener _gameEventListener;
    private NetPeer _relayServerPeer;
    private readonly string _relayServerAddress;
    private readonly int _relayServerPort;

    public event Action<string> OnLobbyCodeReceived;
    public event Action<string> OnError;
    public event Action<byte[]> OnGamePacketReceived;

    public string CurrentLobbyCode { get; private set; }
    public bool IsConnected => _relayServerPeer?.ConnectionState == ConnectionState.Connected;
    public bool IsHost { get; private set; }
    public NetPeer RelayServerPeer => _relayServerPeer;

    public LiteNetRelayManager(
        INetEventListener gameEventListener,
        string relayServerAddress = "127.0.0.1",
        int relayServerPort = 9051
    )
    {
        _gameEventListener = gameEventListener;
        _relayServerAddress = relayServerAddress;
        _relayServerPort = relayServerPort;

        _netManager = new NetManager(new RelayEventListener(this, gameEventListener))
        {
            DisconnectTimeout = 10000,
            ReconnectDelay = 1000,
            MaxConnectAttempts = 5,
            PingInterval = 2000,
        };
        _netManager.Start();
    }

    public async Task<bool> StartAsHostAsync()
    {
        try
        {
            var connected = await ConnectToRelayServer();
            if (!connected)
                return false;

            var writer = new NetDataWriter();
            writer.Put("HOST_REGISTER");
            _relayServerPeer.Send(writer, DeliveryMethod.ReliableOrdered);

            IsHost = true;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error starting as host: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> JoinAsClientAsync(string lobbyCode)
    {
        try
        {
            var connected = await ConnectToRelayServer();
            if (!connected)
                return false;

            var writer = new NetDataWriter();
            writer.Put($"CLIENT_JOIN:{lobbyCode}");
            _relayServerPeer.Send(writer, DeliveryMethod.ReliableOrdered);

            IsHost = false;
            CurrentLobbyCode = lobbyCode;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error joining lobby: {ex.Message}");
            return false;
        }
    }

    public void PollEvents()
    {
        _netManager.PollEvents();
    }

    private async Task<bool> ConnectToRelayServer()
    {
        try
        {
            Logger.LogNetwork(
                "RELAY_MGR",
                $"Attempting to connect to relay server at {_relayServerAddress}:{_relayServerPort}"
            );

            _relayServerPeer = _netManager.Connect(_relayServerAddress, _relayServerPort, "");

            if (_relayServerPeer == null)
            {
                Logger.Error("Failed to create connection to relay server");
                return false;
            }

            var connectionTcs = new TaskCompletionSource<bool>();
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            timeoutCts.Token.Register(() =>
            {
                Logger.Warning("Connection timeout reached");
                connectionTcs.TrySetResult(false);
            });

            var pollingTask = Task.Run(async () =>
            {
                try
                {
                    int pollCount = 0;
                    while (
                        !connectionTcs.Task.IsCompleted && !timeoutCts.Token.IsCancellationRequested
                    )
                    {
                        _netManager.PollEvents();
                        pollCount++;

                        if (_relayServerPeer?.ConnectionState == ConnectionState.Connected)
                        {
                            Logger.LogNetwork(
                                "RELAY_MGR",
                                $"Connected to relay server after {pollCount} polls"
                            );
                            connectionTcs.TrySetResult(true);
                            break;
                        }

                        if (pollCount % 100 == 0)
                        {
                            Logger.LogNetwork(
                                "RELAY_MGR",
                                $"Still connecting... poll {pollCount}, state: {_relayServerPeer?.ConnectionState}"
                            );
                        }

                        await Task.Delay(5, timeoutCts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when timeout occurs
                }
            });

            var connected = await connectionTcs.Task;

            timeoutCts.Cancel();
            try
            {
                await pollingTask;
            }
            catch (OperationCanceledException) { }
            timeoutCts.Dispose();

            if (!connected)
            {
                Logger.Error(
                    $"Failed to connect to relay server after 15 seconds. State: {_relayServerPeer?.ConnectionState}"
                );
                return false;
            }

            Logger.LogNetwork("RELAY_MGR", "Connected to relay server successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error connecting to relay server: {ex.Message}");
            Logger.Error($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public void Dispose()
    {
        _netManager?.Stop();
    }

    private class RelayEventListener : INetEventListener
    {
        private readonly LiteNetRelayManager _manager;
        private readonly INetEventListener _gameListener;

        public RelayEventListener(LiteNetRelayManager manager, INetEventListener gameListener)
        {
            _manager = manager;
            _gameListener = gameListener;
        }

        public void OnPeerConnected(NetPeer peer)
        {
            _gameListener.OnPeerConnected(peer);
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            _manager.OnError?.Invoke($"Relay connection lost: {disconnectInfo.Reason}");
        }

        public void OnNetworkError(
            System.Net.IPEndPoint endPoint,
            System.Net.Sockets.SocketError socketError
        )
        {
            Logger.Error($"Relay network error: {socketError}");
        }

        public void OnNetworkReceive(
            NetPeer peer,
            NetPacketReader reader,
            byte channel,
            DeliveryMethod deliveryMethod
        )
        {
            try
            {
                var bytes = reader.GetRemainingBytes();

                if (bytes == null || bytes.Length == 0)
                {
                    Logger.Warning("Received empty packet from relay server");
                    reader.Recycle();
                    return;
                }

                if (bytes.Length < 4)
                {
                    Logger.LogNetwork(
                        "RELAY_LISTENER",
                        $"Skipping small packet ({bytes.Length} bytes) - likely control message"
                    );
                    reader.Recycle();
                    return;
                }

                var dataReader = new NetDataReader(bytes);

                try
                {
                    var message = dataReader.GetString();

                    if (IsControlMessage(message))
                    {
                        Logger.LogNetwork("RELAY_LISTENER", $"Received control message: {message}");
                        HandleControlMessage(message);
                        reader.Recycle();
                        return;
                    }
                }
                catch (Exception) { }

                Logger.LogNetwork(
                    "RELAY_LISTENER",
                    $"Forwarding game packet with {bytes.Length} bytes"
                );

                _manager.OnGamePacketReceived?.Invoke(bytes);

                reader.Recycle();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in RelayEventListener.OnNetworkReceive: {ex.Message}");
                try
                {
                    reader.Recycle();
                }
                catch { }
            }
        }

        private bool IsControlMessage(string message)
        {
            return message.Contains(":")
                || message == "PONG"
                || message.StartsWith("LOBBY_")
                || message.StartsWith("CLIENT_")
                || message.StartsWith("HOST_")
                || message.StartsWith("ERROR")
                || message.StartsWith("JOINED_");
        }

        private void HandleControlMessage(string message)
        {
            var parts = message.Split(':');
            var command = parts[0];

            switch (command)
            {
                case "LOBBY_CREATED":
                    if (parts.Length > 1)
                    {
                        var lobbyCode = parts[1];
                        _manager.CurrentLobbyCode = lobbyCode;
                        _manager.OnLobbyCodeReceived?.Invoke(lobbyCode);
                    }
                    break;

                case "JOINED_LOBBY":
                    if (parts.Length > 1)
                    {
                        var lobbyCode = parts[1];
                        _manager.CurrentLobbyCode = lobbyCode;
                        _gameListener.OnPeerConnected(_manager._relayServerPeer);
                    }
                    break;

                case "CLIENT_JOINED":
                    if (parts.Length > 1)
                    {
                        var clientId = parts[1];
                        _gameListener.OnPeerConnected(_manager._relayServerPeer);
                    }
                    break;

                case "HOST_DISCONNECTED":
                    _gameListener.OnPeerDisconnected(
                        _manager._relayServerPeer,
                        new DisconnectInfo()
                    );
                    break;

                case "ERROR":
                    if (parts.Length > 1)
                    {
                        var error = parts[1];
                        Logger.Error($"Relay server error: {error}");
                        _manager.OnError?.Invoke(error);
                    }
                    break;

                case "PONG":
                    break;
            }
        }

        public void OnNetworkReceiveUnconnected(
            System.Net.IPEndPoint remoteEndPoint,
            NetPacketReader reader,
            UnconnectedMessageType messageType
        ) { }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

        public void OnConnectionRequest(ConnectionRequest request) { }
    }
}
