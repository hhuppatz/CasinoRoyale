using System;
using System.Threading.Tasks;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using CasinoRoyale.Utils;

namespace CasinoRoyale.Classes.Networking;

// Relay manager using LiteNetLib
// Connects to a LiteNetLib relay server for lobby-based multiplayer
public class LiteNetRelayManager : IDisposable
{
    private readonly NetManager _netManager;
    private readonly INetEventListener _gameEventListener;
    private NetPeer _relayServerPeer;
    private readonly string _relayServerAddress;
    private readonly int _relayServerPort;
    
    // Events
    public event Action<string> OnLobbyCodeReceived;
    public event Action<string> OnError;
    public event Action<byte[]> OnGamePacketReceived;
    
    // State
    public string CurrentLobbyCode { get; private set; }
    public bool IsConnected => _relayServerPeer?.ConnectionState == ConnectionState.Connected;
    public bool IsHost { get; private set; }
    public NetPeer RelayServerPeer => _relayServerPeer;
        
    public LiteNetRelayManager(INetEventListener gameEventListener, string relayServerAddress = "127.0.0.1", int relayServerPort = 9051)
    {
        _gameEventListener = gameEventListener;
        _relayServerAddress = relayServerAddress;
        _relayServerPort = relayServerPort;
        
        // Create NetManager that wraps the game's event listener
        _netManager = new NetManager(new RelayEventListener(this, gameEventListener))
        {
            DisconnectTimeout = 10000, // 10 seconds timeout
            ReconnectDelay = 1000,
            MaxConnectAttempts = 5,
            PingInterval = 2000 // Send ping every 2 seconds
        };
        _netManager.Start();
    }
        
    public async Task<bool> StartAsHostAsync()
    {
        try
        {
            var connected = await ConnectToRelayServer();
            if (!connected) return false;
            
            // Send host registration
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
            if (!connected) return false;
            
            // Send join request
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
    
    /// <summary>
    /// Shared connection logic to eliminate duplication
    /// </summary>
    private async Task<bool> ConnectToRelayServer()
    {
        try
        {
            Logger.LogNetwork("RELAY_MGR", $"Attempting to connect to relay server at {_relayServerAddress}:{_relayServerPort}");
            
            // Connect to relay server
            _relayServerPeer = _netManager.Connect(_relayServerAddress, _relayServerPort, "");
            
            if (_relayServerPeer == null)
            {
                Logger.Error("Failed to create connection to relay server");
                return false;
            }
            
            // Use improved polling with better timing
            var connectionTcs = new TaskCompletionSource<bool>();
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15)); // Increased timeout
            
            // Set up timeout
            timeoutCts.Token.Register(() => 
            {
                Logger.Warning("Connection timeout reached");
                connectionTcs.TrySetResult(false);
            });
            
            // Start polling in background with improved timing
            var pollingTask = Task.Run(async () =>
            {
                try
                {
                    int pollCount = 0;
                    while (!connectionTcs.Task.IsCompleted && !timeoutCts.Token.IsCancellationRequested)
                    {
                        _netManager.PollEvents();
                        pollCount++;
                        
                        // Check if we're connected with more frequent checks
                        if (_relayServerPeer?.ConnectionState == ConnectionState.Connected)
                        {
                            Logger.LogNetwork("RELAY_MGR", $"Connected to relay server after {pollCount} polls");
                            connectionTcs.TrySetResult(true);
                            break;
                        }
                        
                        // Log progress every 100 polls (roughly every 500ms)
                        if (pollCount % 100 == 0)
                        {
                            Logger.LogNetwork("RELAY_MGR", $"Still connecting... poll {pollCount}, state: {_relayServerPeer?.ConnectionState}");
                        }
                        
                        await Task.Delay(5, timeoutCts.Token); // Reduced delay for faster detection
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when timeout occurs
                }
            });
            
            // Wait for connection or timeout
            var connected = await connectionTcs.Task;
            
            // Clean up
            timeoutCts.Cancel();
            try
            {
                await pollingTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            timeoutCts.Dispose();
            
            if (!connected)
            {
                Logger.Error($"Failed to connect to relay server after 15 seconds. State: {_relayServerPeer?.ConnectionState}");
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
        
    /// <summary>
    /// Event listener wrapper that intercepts relay server control messages
    /// </summary>
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
                // Forward the connection event to the game listener
                _gameListener.OnPeerConnected(peer);
            }
            
            public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
            {
                _manager.OnError?.Invoke($"Relay connection lost: {disconnectInfo.Reason}");
            }
            
            public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
            {
                Logger.Error($"Relay network error: {socketError}");
            }
            
            public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
            {
                try
                {
                    // Get all remaining bytes
                    var bytes = reader.GetRemainingBytes();
                    
                    if (bytes == null || bytes.Length == 0)
                    {
                        Logger.Warning("Received empty packet from relay server");
                        reader.Recycle(); // Recycle the reader
                        return;
                    }
                    
                    // Skip very small packets that are likely control messages
                    if (bytes.Length < 4)
                    {
                        Logger.LogNetwork("RELAY_LISTENER", $"Skipping small packet ({bytes.Length} bytes) - likely control message");
                        reader.Recycle(); // Recycle the reader
                        return;
                    }
                    
                    // Try to read as string to check for control messages
                    var dataReader = new NetDataReader(bytes);
                    
                    try
                    {
                        var message = dataReader.GetString();
                        
                        // Check if it's a control message (contains ':' or is a known control command)
                        if (IsControlMessage(message))
                        {
                            Logger.LogNetwork("RELAY_LISTENER", $"Received control message: {message}");
                            HandleControlMessage(message);
                            reader.Recycle(); // Recycle the reader
                            return;
                        }
                    }
                    catch (Exception)
                    {
                        // Not a string message, treat as game packet
                    }
                    
                    // Forward as game packet
                    Logger.LogNetwork("RELAY_LISTENER", $"Forwarding game packet with {bytes.Length} bytes");
                    
                    // Forward the raw bytes through an event to the NetworkManager
                    _manager.OnGamePacketReceived?.Invoke(bytes);
                    
                    // Recycle the NetPacketReader as recommended by LiteNetLib docs
                    reader.Recycle();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error in RelayEventListener.OnNetworkReceive: {ex.Message}");
                    // Still recycle the reader even on error
                    try { reader.Recycle(); } catch { }
                }
            }
            
            private bool IsControlMessage(string message)
            {
                // Check for known control message patterns
                return message.Contains(":") || 
                       message == "PONG" || 
                       message.StartsWith("LOBBY_") ||
                       message.StartsWith("CLIENT_") ||
                       message.StartsWith("HOST_") ||
                       message.StartsWith("ERROR") ||
                       message.StartsWith("JOINED_");
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
                            // Trigger OnPeerConnected to start the join process
                            _gameListener.OnPeerConnected(_manager._relayServerPeer);
                        }
                        break;
                        
                    case "CLIENT_JOINED":
                        if (parts.Length > 1)
                        {
                            var clientId = parts[1];
                            // For host: a new client connected
                            // We can't create a real NetPeer for them, but we signal the connection
                            _gameListener.OnPeerConnected(_manager._relayServerPeer);
                        }
                        break;
                        
                    case "HOST_DISCONNECTED":
                        _gameListener.OnPeerDisconnected(_manager._relayServerPeer, new DisconnectInfo());
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
                        // Keep-alive response
                        break;
                }
            }
            
            public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
            {
                // Not used
            }
            
            public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
            {
                // Can be used for monitoring
            }
            
            public void OnConnectionRequest(ConnectionRequest request)
            {
                // Server-side only
            }
        }
    }