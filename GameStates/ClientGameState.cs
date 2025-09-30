using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CasinoRoyale.GameObjects;
using CasinoRoyale.Extensions;
using CasinoRoyale.Players.Common.Networking;
using CasinoRoyale.Utils;

namespace CasinoRoyale.GameStates
{
    /// <summary>
    /// Game state for joining a game as a client
    /// </summary>
    public class ClientGameState : GameState, INetEventListener
    {
        // Network fields
        private LiteNetRelayManager relayManager;
        private NetDataWriter writer;
        private NetPacketProcessor packetProcessor;
        private List<PlayableCharacter> otherPlayers = new();
        private NetPeer server;
        private bool connected = false;
        private string lobbyCode;
        
        // Client-specific fields
        private string username = "HH";
        
        // Texture fields (same as original Client)
        private Texture2D platformTexture;
        private Texture2D casinoMachineTexture;
        
        public ClientGameState(Game game, string lobbyCode) : base(game)
        {
            this.lobbyCode = lobbyCode;
        }
        
        public override void Initialize()
        {
            base.Initialize();
            
            // Initialize relay manager (using LiteNetLib relay)
            relayManager = new LiteNetRelayManager(this);
            writer = new NetDataWriter();
            packetProcessor = new NetPacketProcessor();
            
            // Register nested types for packet processing
            packetProcessor.RegisterNestedType((w, v) => w.Put(v), reader => reader.GetVector2());
            packetProcessor.RegisterNestedType((w, v) => w.Put(v), reader => reader.GetGES());
            packetProcessor.RegisterNestedType((w, v) => w.Put(v), reader => reader.GetRectangle());
            packetProcessor.RegisterNestedType<PlayerState>();
            packetProcessor.RegisterNestedType<PlatformState>();
            packetProcessor.RegisterNestedType<CasinoMachineState>();
            
            // Set up packet processing - register both with and without NetPeer for relay compatibility
            packetProcessor.SubscribeReusable<PlayerReceiveUpdatePacket, NetPeer>(OnPlayerStatesUpdateReceived);
            packetProcessor.SubscribeReusable<PlayerReceiveUpdatePacket>(p => OnPlayerStatesUpdateReceived(p, null));
            
            packetProcessor.SubscribeReusable<JoinAcceptPacket, NetPeer>(OnJoinAcceptReceived);
            packetProcessor.SubscribeReusable<JoinAcceptPacket>(p => OnJoinAcceptReceived(p, null));
            
            packetProcessor.SubscribeReusable<PlayerJoinedGamePacket, NetPeer>(OnPlayerJoin);
            packetProcessor.SubscribeReusable<PlayerJoinedGamePacket>(p => OnPlayerJoin(p, null));
            
            packetProcessor.SubscribeReusable<PlayerLeftGamePacket, NetPeer>(OnPlayerLeave);
            packetProcessor.SubscribeReusable<PlayerLeftGamePacket>(p => OnPlayerLeave(p, null));
            
            // Set up relay manager events
            relayManager.OnError += OnRelayError;
            relayManager.OnGamePacketReceived += OnRelayGamePacketReceived;
            
            // Join the game
            JoinGame();
        }
        
        public override void LoadContent()
        {
            base.LoadContent();
            
            // Load textures once (same as original Client)
            platformTexture = Content.Load<Texture2D>("CasinoFloor1");
            casinoMachineTexture = Content.Load<Texture2D>("CasinoMachine1");
            
            // Client will receive game world data from host
            Logger.Info("Waiting for server to send game world data...");
        }
        
        public override void Update(GameTime gameTime)
        {
            // Always poll events, even when not connected (to receive JOINED_LOBBY, etc.)
            relayManager.PollEvents();
            
            if (!connected || LocalPlayer == null) return;
            
            base.Update(gameTime);
            
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // Send player state to host
            SendPlayerState(deltaTime);
        }
        
        public override void Draw(GameTime gameTime)
        {
            if (!connected || LocalPlayer == null) return;
            
            base.Draw(gameTime);
        }
        
        protected override void DrawOtherPlayers()
        {
            // Draw other players
            foreach (var player in otherPlayers)
            {
                SpriteBatch.DrawEntity(MainCamera, player);
            }
        }
        
        private async void JoinGame()
        {
            try
            {
                Logger.LogNetwork("CLIENT", $"Attempting to join lobby: {lobbyCode}");
                var joined = await relayManager.JoinAsClientAsync(lobbyCode);
                if (joined)
                {
                    Logger.LogNetwork("CLIENT", "Successfully joined lobby");
                }
                else
                {
                    Logger.Error($"Failed to join lobby {lobbyCode}");
                    connected = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error joining game: {ex.Message}");
                connected = false;
            }
        }
        
        private void SendPlayerState(float deltaTime)
        {
            if (LocalPlayer == null) return;
            
            var playerState = new PlayerSendUpdatePacket
            {
                coords = LocalPlayer.Coords,
                velocity = LocalPlayer.Velocity,
                dt = deltaTime
            };
            
            SendPacket(playerState, DeliveryMethod.Unreliable);
        }
        
        // Network event handlers
        private void OnPlayerStatesUpdateReceived(PlayerReceiveUpdatePacket update, NetPeer peer)
        {
            if (update.playerStates == null) return;
            
            foreach (var playerState in update.playerStates)
            {
                // Skip our own player state
                if (playerState.pid == LocalPlayer?.GetID()) continue;
                
                // Find or create other player
                var otherPlayer = otherPlayers.Find(p => p.GetID() == playerState.pid);
                if (otherPlayer == null)
                {
                    // Create new player using the player state data
                    otherPlayer = new PlayableCharacter(
                        playerState.pid,
                        playerState.username,
                        PlayerTexture,
                        playerState.ges.coords,
                        playerState.ges.velocity,
                        playerState.mass,
                        playerState.initialJumpVelocity,
                        playerState.maxRunSpeed,
                        new Rectangle(playerState.ges.coords.ToPoint(), new Point(PlayerTexture.Bounds.Width, PlayerTexture.Bounds.Height)),
                        false);
                    
                    otherPlayers.Add(otherPlayer);
                    Logger.LogNetwork("CLIENT", $"Added player {otherPlayer.GetID()} ({otherPlayer.GetUsername()})");
                }
                else
                {
                    // Update existing player
                    otherPlayer.SetPlayerState(playerState);
                }
            }
            
            // Update casino machines
            if (update.casinoMachineStates != null)
            {
                foreach (var casinoMachineState in update.casinoMachineStates)
                {
                    if (casinoMachineState.machineNum < CasinoMachines.Count)
                    {
                        CasinoMachines[(int)casinoMachineState.machineNum].Coords = casinoMachineState.coords;
                    }
                }
            }
        }
        
        private void OnJoinAcceptReceived(JoinAcceptPacket joinAccept, NetPeer peer)
        {
            Logger.Info($"========== OnJoinAcceptReceived called! Platforms: {joinAccept.platformStates?.Length}, Peer: {(peer == null ? "null (relay)" : peer.Address.ToString())} ==========");
            
            // Set up game world (same as original Client)
            GameArea = joinAccept.gameArea;
            
            // Initialize physics system BEFORE creating player (same as original Client)
            InitializePhysics();
            Logger.Info($"Physics system initialized with gameArea: {GameArea}");
            
            // Create local player from player state (same as original Client)
            var playerState = joinAccept.playerState;
            LocalPlayer = new PlayableCharacter(
                playerState.pid,
                playerState.username,
                PlayerTexture,
                playerState.ges.coords,
                joinAccept.playerVelocity, // Use playerVelocity from packet
                playerState.mass,
                playerState.initialJumpVelocity,
                playerState.maxRunSpeed,
                joinAccept.playerHitbox,
                true);
            
            // Initialize camera AFTER creating player (same as original Client)
            InitializeCamera();
            Logger.Info("Player created and camera initialized!");
            
            // Recreate platforms from platform states (using pre-loaded texture)
            Platforms = new List<Platform>();
            foreach (var platformState in joinAccept.platformStates ?? new PlatformState[0])
            {
                var platform = new Platform(
                    platformState.platNum,
                    platformTexture, // Use pre-loaded texture
                    platformState.TL,
                    platformState.BR);
                Platforms.Add(platform);
            }
            
            // Recreate casino machines from casino machine states (using pre-loaded texture)
            CasinoMachines = new List<CasinoMachine>();
            foreach (var casinoMachineState in joinAccept.casinoMachineStates ?? new CasinoMachineState[0])
            {
                var casinoMachine = new CasinoMachine(
                    casinoMachineState.machineNum,
                    casinoMachineTexture, // Use pre-loaded texture
                    casinoMachineState.coords);
                CasinoMachines.Add(casinoMachine);
            }
            
            // Create other players from other player states (same as original Client)
            foreach (var otherPlayerState in joinAccept.otherPlayerStates ?? new PlayerState[0])
            {
                if (otherPlayerState.pid != LocalPlayer.GetID())
                {
                    otherPlayers.Add(new PlayableCharacter(
                        otherPlayerState.pid,
                        otherPlayerState.username,
                        PlayerTexture,
                        otherPlayerState.ges.coords,
                        joinAccept.playerVelocity, // Use playerVelocity from packet
                        otherPlayerState.mass,
                        otherPlayerState.initialJumpVelocity,
                        otherPlayerState.maxRunSpeed,
                        joinAccept.playerHitbox,
                        true));
                }
            }
            
            connected = true;
            // For relay connections, peer is null, so use the relay server peer
            server = peer ?? relayManager.RelayServerPeer;
            
            Logger.LogNetwork("CLIENT", $"Player {LocalPlayer.GetID()} ({LocalPlayer.GetUsername()}) created");
            Logger.LogNetwork("CLIENT", $"Game area: {GameArea}");
            Logger.LogNetwork("CLIENT", $"Platforms: {Platforms.Count}, Casino machines: {CasinoMachines.Count}");
            Logger.Info("Join process completed successfully - client is now fully connected!");
        }
        
        private void OnPlayerJoin(PlayerJoinedGamePacket packet, NetPeer peer)
        {
            Logger.Info($"Player (pid: {packet.new_player_state.pid}) joined the game");
            otherPlayers.Add(new PlayableCharacter(
                packet.new_player_state.pid,
                packet.new_player_username,
                PlayerTexture,
                packet.new_player_state.ges.coords,
                packet.new_player_state.ges.velocity,
                packet.new_player_mass,
                packet.new_player_initialJumpVelocity,
                packet.new_player_maxRunSpeed,
                packet.new_player_hitbox,
                true));
        }
        
        private void OnPlayerLeave(PlayerLeftGamePacket packet, NetPeer peer)
        {
            Logger.Info($"Player (pid: {packet.pid}) left the game");
            foreach (PlayableCharacter otherPlayer in otherPlayers)
            {
                if (packet.pid == otherPlayer.GetID())
                {
                    otherPlayers.Remove(otherPlayer);
                    break;
                }
            }
        }
        
        private void OnRelayError(string errorMessage)
        {
            Logger.Error($"Relay error: {errorMessage}");
        }
        
        private void OnRelayGamePacketReceived(byte[] packetData)
        {
            try
            {
                Logger.Info($"[RELAY] Received game packet: {packetData.Length} bytes");
                
                // Process the packet with our packet processor
                var reader = new NetDataReader(packetData);
                packetProcessor.ReadAllPackets(reader, null);
                
                Logger.Debug("[RELAY] Game packet processed successfully");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error processing relay game packet: {ex.Message}");
                Logger.Error($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private void SendPacket<T>(T packet, DeliveryMethod deliveryMethod) where T : class, new()
        {
            if (server != null && packetProcessor != null)
            {
                var writer = new NetDataWriter();
                packetProcessor.Write(writer, packet);
                server.Send(writer, deliveryMethod);
                
                // Only log important packets, not every frame update
                if (typeof(T) != typeof(PlayerSendUpdatePacket))
                {
                    Logger.LogNetwork("CLIENT", $"Sent packet of type {typeof(T).Name} using {deliveryMethod}");
                }
            }
            else
            {
                Logger.Warning("Cannot send packet - not connected to server");
            }
        }
        
        public override void Dispose()
        {
            base.Dispose();
            relayManager?.Dispose();
        }
        
        // INetEventListener implementation
        public void OnPeerConnected(NetPeer peer)
        {
            if (peer == null)
            {
                Logger.LogNetwork("CLIENT", "Connected to host via relay");
                // For relay connections, we don't have a NetPeer, but we can still send packets
                // The relayManager handles this internally
            }
            else
            {
                Logger.LogNetwork("CLIENT", $"Peer connected: {peer.Address}");
            }
            // For relay connections, use the relay server peer instead of null
            server = peer ?? relayManager.RelayServerPeer;
            
            // Send join packet to server
            Logger.LogNetwork("CLIENT", "Sending JoinPacket to server...");
            SendPacket(new JoinPacket { 
                username = username,
                playerMass = GetFloatProperty("playerMass", 5.0f),
                playerInitialJumpVelocity = GetFloatProperty("playerInitialJumpVelocity", 240f),
                playerMaxRunSpeed = GetFloatProperty("playerRunSpeed", 240f)
            }, DeliveryMethod.ReliableOrdered);
        }
        
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (peer == null)
            {
                Logger.LogNetwork("CLIENT", $"Peer disconnected via relay, Reason: {disconnectInfo.Reason}");
            }
            else
            {
                Logger.LogNetwork("CLIENT", $"Peer disconnected: {peer.Address}, Reason: {disconnectInfo.Reason}");
            }
        }
        
        public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        {
            Logger.Error($"Network error: {endPoint}, Error: {socketError}");
        }
        
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            packetProcessor.ReadAllPackets(reader, peer);
        }
        
        public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            Logger.LogNetwork("CLIENT", $"Received unconnected message from {remoteEndPoint}");
        }
        
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            // Optional: Handle latency updates
        }
        
        public void OnConnectionRequest(ConnectionRequest request)
        {
            Logger.LogNetwork("CLIENT", $"Connection request from {request.RemoteEndPoint}");
            request.Accept();
        }
    }
}
