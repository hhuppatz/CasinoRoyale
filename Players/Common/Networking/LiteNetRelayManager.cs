using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using CasinoRoyale.Utils;

namespace CasinoRoyale.Players.Common.Networking
{
    /// <summary>
    /// Simplified relay manager using pure LiteNetLib
    /// Connects to a LiteNetLib relay server for lobby-based multiplayer
    /// </summary>
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
        public bool IsConnected => _relayServerPeer != null && _relayServerPeer.ConnectionState == ConnectionState.Connected;
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
            
            Logger.LogNetwork("RELAY_MGR", $"LiteNetRelayManager initialized, target: {_relayServerAddress}:{_relayServerPort}");
        }
        
        public async Task<bool> StartAsHostAsync()
        {
            try
            {
                Logger.LogNetwork("RELAY_MGR", "Connecting to relay server as host...");
                
                // Connect to relay server
                _relayServerPeer = _netManager.Connect(_relayServerAddress, _relayServerPort, "");
                
                // Wait for connection with retries
                for (int i = 0; i < 20; i++) // Try for up to 2 seconds
                {
                    await Task.Delay(100);
                    _netManager.PollEvents(); // Process connection events
                    
                    if (_relayServerPeer != null && _relayServerPeer.ConnectionState == ConnectionState.Connected)
                    {
                        Logger.LogNetwork("RELAY_MGR", "Connected to relay server successfully");
                        break;
                    }
                }
                
                if (_relayServerPeer == null || _relayServerPeer.ConnectionState != ConnectionState.Connected)
                {
                    Logger.Error($"Failed to connect to relay server after 2 seconds. State: {_relayServerPeer?.ConnectionState}");
                    return false;
                }
                
                // Send host registration
                var writer = new NetDataWriter();
                writer.Put("HOST_REGISTER");
                _relayServerPeer.Send(writer, DeliveryMethod.ReliableOrdered);
                
                IsHost = true;
                Logger.LogNetwork("RELAY_MGR", "Host registration sent");
                
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
                Logger.LogNetwork("RELAY_MGR", $"Connecting to relay server to join lobby: {lobbyCode}");
                
                // Connect to relay server
                _relayServerPeer = _netManager.Connect(_relayServerAddress, _relayServerPort, "");
                
                // Wait for connection with retries
                for (int i = 0; i < 20; i++) // Try for up to 2 seconds
                {
                    await Task.Delay(100);
                    _netManager.PollEvents(); // Process connection events
                    
                    if (_relayServerPeer != null && _relayServerPeer.ConnectionState == ConnectionState.Connected)
                    {
                        Logger.LogNetwork("RELAY_MGR", "Connected to relay server successfully");
                        break;
                    }
                }
                
                if (_relayServerPeer == null || _relayServerPeer.ConnectionState != ConnectionState.Connected)
                {
                    Logger.Error($"Failed to connect to relay server after 2 seconds. State: {_relayServerPeer?.ConnectionState}");
                    return false;
                }
                
                // Send join request
                var writer = new NetDataWriter();
                writer.Put($"CLIENT_JOIN:{lobbyCode}");
                _relayServerPeer.Send(writer, DeliveryMethod.ReliableOrdered);
                
                IsHost = false;
                CurrentLobbyCode = lobbyCode;
                Logger.LogNetwork("RELAY_MGR", $"Join request sent for lobby: {lobbyCode}");
                
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
        
        public void Dispose()
        {
            _netManager?.Stop();
            Logger.LogNetwork("RELAY_MGR", "LiteNetRelayManager disposed");
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
                Logger.LogNetwork("RELAY_MGR", $"Connected to relay server: {peer.Address}:{peer.Port}");
            }
            
            public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
            {
                Logger.LogNetwork("RELAY_MGR", $"Disconnected from relay server: {disconnectInfo.Reason}");
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
                    // Check if this is a control message from relay server
                    var bytes = reader.GetRemainingBytes();
                    Logger.Debug($"[RELAY] Received {bytes.Length} bytes from relay server");
                    
                    var dataReader = new NetDataReader(bytes);
                    
                    // Try to read as string - if it's a control message it will work
                    var position = dataReader.Position;
                    try
                    {
                        var message = dataReader.GetString();
                        Logger.Debug($"[RELAY] Decoded as string message: {message}");
                        
                        // Check if it's a control message (contains ':' or is PONG)
                        if (message.Contains(":") || message == "PONG")
                        {
                            HandleControlMessage(message);
                            return;
                        }
                        else
                        {
                            Logger.Debug($"[RELAY] String message '{message}' doesn't look like a control message, treating as game packet");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug($"[RELAY] Not a string message (error: {ex.Message}), must be a game packet");
                    }
                    
                    // Forward game packets directly to the game listener
                    // We need to create a fresh reader since the original was consumed during detection
                    // Pass null for peer since all game packets come through the relay server peer
                    Logger.Debug($"[RELAY] Forwarding packet to game listener (peer=null for relay)");
                    Logger.Debug($"[RELAY] Creating fresh NetDataReader with {bytes.Length} bytes for packet processor");
                    
                    // Create a NetDataReader and let the game state's packet processor handle it
                    var gameReader = new NetDataReader(bytes);
                    
                    // Since OnNetworkReceive expects NetPacketReader, we need a different approach
                    // Instead, we'll expose the raw bytes through an event
                    _manager.OnGamePacketReceived?.Invoke(bytes);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error in RelayEventListener.OnNetworkReceive: {ex.Message}");
                }
            }
            
            private void HandleControlMessage(string message)
            {
                Logger.Info($"[RELAY] Received control message: {message}");
                
                var parts = message.Split(':');
                var command = parts[0];
                
                switch (command)
                {
                    case "LOBBY_CREATED":
                        if (parts.Length > 1)
                        {
                            var lobbyCode = parts[1];
                            _manager.CurrentLobbyCode = lobbyCode;
                            Logger.LogNetwork("RELAY", $"Lobby created: {lobbyCode}");
                            _manager.OnLobbyCodeReceived?.Invoke(lobbyCode);
                        }
                        break;
                        
                    case "JOINED_LOBBY":
                        if (parts.Length > 1)
                        {
                            var lobbyCode = parts[1];
                            _manager.CurrentLobbyCode = lobbyCode;
                            Logger.LogNetwork("RELAY", $"Joined lobby: {lobbyCode}");
                            // Trigger OnPeerConnected to start the join process
                            _gameListener.OnPeerConnected(_manager._relayServerPeer);
                        }
                        break;
                        
                    case "CLIENT_JOINED":
                        if (parts.Length > 1)
                        {
                            var clientId = parts[1];
                            Logger.LogNetwork("RELAY", $"Client joined: {clientId}");
                            // For host: a new client connected
                            // We can't create a real NetPeer for them, but we signal the connection
                            _gameListener.OnPeerConnected(_manager._relayServerPeer);
                        }
                        break;
                        
                    case "HOST_DISCONNECTED":
                        Logger.LogNetwork("RELAY", "Host disconnected");
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
}
