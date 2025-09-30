using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CasinoRoyale.GameObjects;
using CasinoRoyale.Extensions;
using CasinoRoyale.Players.Common;
using CasinoRoyale.Players.Common.Networking;
using CasinoRoyale.Players.Host;
using CasinoRoyale.Utils;

namespace CasinoRoyale.GameStates
{
    /// <summary>
    /// Game state for hosting a game
    /// </summary>
    public class HostGameState : GameState, INetEventListener
    {
        // Network fields
        private LiteNetRelayManager relayManager;
        private NetDataWriter writer;
        private NetPacketProcessor packetProcessor;
        private Dictionary<uint, NetworkPlayer> networkPlayers = new();
        private float serverUpdateTimer = 0f;
        private readonly uint MAX_PLAYERS = 6;
        private PlayerIDs playerIDs;
        private int nextSpawnOffset = 0;
        private string currentLobbyCode;
        
        // Host-specific fields
        private string username = "HAZZA";
        
        public HostGameState(Game game) : base(game)
        {
            playerIDs = new PlayerIDs(MAX_PLAYERS);
        }
        
        public override void Initialize()
        {
            base.Initialize();
            
            // Initialize relay manager (using LiteNetLib relay)
            string relayAddress = GetStringProperty("relay.server.address", "127.0.0.1");
            int relayPort = GetIntProperty("relay.server.port", 9051);
            Logger.Info($"Initializing relay manager with server: {relayAddress}:{relayPort}");
            relayManager = new LiteNetRelayManager(this, relayAddress, relayPort);
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
            packetProcessor.SubscribeReusable<PlayerSendUpdatePacket, NetPeer>(OnPlayerStateReceived);
            packetProcessor.SubscribeReusable<PlayerSendUpdatePacket>(p => OnPlayerStateReceived(p, null));
            
            packetProcessor.SubscribeReusable<JoinPacket, NetPeer>(OnJoinRequestReceived);
            packetProcessor.SubscribeReusable<JoinPacket>(p => OnJoinRequestReceived(p, null));
            
            // Set up relay manager events
            relayManager.OnLobbyCodeReceived += (lobbyCode) =>
            {
                currentLobbyCode = lobbyCode;
                Logger.Info($"🎮 LOBBY CODE: {lobbyCode}");
                Logger.Info("Share this code with other players to join your game!");
            };

            relayManager.OnError += OnRelayError;
            relayManager.OnGamePacketReceived += OnRelayGamePacketReceived;
            
            // Start as host through relay server asynchronously
            Logger.LogNetwork("HOST", "Starting relay server connection...");
            _ = Task.Run(async () =>
            {
                try
                {
                    var hostStarted = await relayManager.StartAsHostAsync();
                    if (!hostStarted)
                    {
                        Logger.Error("Failed to start as host through relay server");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error starting host: {ex.Message}");
                }
            });
        }
        
        public override void LoadContent()
        {
            base.LoadContent();
            
            // Load player texture (same as original Host)
            string playerImageName = GetStringProperty("player.image", "ball");
            Logger.Info($"Loading player texture: {playerImageName}");
            
            // Generate game world
            GenerateGameWorld();
            
            // Create local player
            CreateLocalPlayer();
            
            // Debug: Print player hitbox info (same as original Host)
            Logger.Info($"Player texture dimensions: {PlayerTexture.Width}x{PlayerTexture.Height}");
            Logger.Info($"Player {LocalPlayer.GetID()} {LocalPlayer.GetUsername()} created at {LocalPlayer.Coords}");
            
            // Initialize physics and camera
            InitializePhysics();
            InitializeCamera();
        }
        
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            serverUpdateTimer += deltaTime;
            
            // Check for client packets through relay
            relayManager.PollEvents();
            
            // Check for player collisions
            CheckPlayerCollisions();
            
            // Send state updates to clients
            SendClientsUpdate();
        }
        
        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkMagenta);
            
            // Check if essential components are initialized
            if (SpriteBatch == null)
            {
                Logger.Error("SpriteBatch is null in HostGameState.Draw() - skipping draw");
                return;
            }
            
            if (MainCamera == null)
            {
                Logger.Error("MainCamera is null in HostGameState.Draw() - skipping draw");
                return;
            }
            
            Vector2 ratio = Resolution.ratio;
            MainCamera.ApplyRatio(ratio);
            
            SpriteBatch.Begin();
            
            // Draw casino machines (only if they exist)
            if (CasinoMachines != null)
            {
                foreach (var casinoMachine in CasinoMachines)
                {
                    if (casinoMachine?.GetTex() != null)
                    {
                        SpriteBatch.Draw(casinoMachine.GetTex(),
                            MainCamera.TransformToView(casinoMachine.Coords),
                            null, Color.White, 0.0f, Vector2.Zero, ratio, 0, 0);
                    }
                }
            }
            
            // Draw platforms (only if they exist)
            if (Platforms != null)
            {
                foreach (var platform in Platforms)
                {
                    if (platform?.GetTex() != null)
                    {
                        int platformLeft = (int)platform.GetLCoords().X;
                        int platformTexWidth = platform.GetTex().Bounds.Width;
                        int platformWidth = platform.GetWidth();
                        int i = platformLeft;
                        
                        while (i < platformLeft + platformWidth)
                        {
                            SpriteBatch.Draw(platform.GetTex(),
                                MainCamera.TransformToView(new Vector2(i + platformTexWidth / 2, platform.GetCoords().Y)),
                                null, Color.White, 0.0f,
                                new Vector2(platformTexWidth / 2, platformTexWidth / 2),
                                ratio, 0, 0);
                            i += platformTexWidth;
                        }
                    }
                }
            }
            
            // Draw local player
            if (LocalPlayer != null)
            {
                SpriteBatch.DrawEntity(MainCamera, LocalPlayer);
            }
            
            // Draw other players
            DrawOtherPlayers();
            
            // Draw lobby code in top-left corner (same as original Host)
            if (!string.IsNullOrEmpty(currentLobbyCode))
            {
                DrawLobbyCode();
            }
            
            SpriteBatch.End();
        }
        
        protected override void DrawOtherPlayers()
        {
            // Draw network players
            foreach (var entry in networkPlayers)
            {
                SpriteBatch.DrawEntity(MainCamera, entry.Value.Player);
            }
        }
        
        private async void StartAsHost()
        {
            try
            {
                Logger.LogNetwork("HOST", "Starting relay server connection...");
                var hostStarted = await relayManager.StartAsHostAsync();
                if (hostStarted)
                {
                    Logger.LogNetwork("HOST", "Successfully started as host");
                }
                else
                {
                    Logger.Error("Failed to start as host through relay server");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error starting host: {ex.Message}");
            }
        }
        
        private void GenerateGameWorld()
        {
            // Load game area properties (same as original Host)
            int gameAreaX = GetIntProperty("gameArea.x", -2000);
            int gameAreaY = GetIntProperty("gameArea.y", 0);
            int gameAreaWidth = GetIntProperty("gameArea.width", 4000);
            int gameAreaHeight = GetIntProperty("gameArea.height", 4000);
            
            GameArea = new Rectangle(gameAreaX, gameAreaY, gameAreaWidth, gameAreaHeight);
            
            // Calculate player spawn buffer (same as original Host)
            int playerSpawnBuffer = PlayerTexture.Height * 2; // Keep area directly around player free
            PlayerOrigin = new Vector2(0, GameArea.Y + GameArea.Height - playerSpawnBuffer);
            
            Logger.Debug($"Player spawn: gameArea={GameArea}, playerSpawnBuffer={playerSpawnBuffer}, playerOrigin={PlayerOrigin}");
            
            // Generate platforms (same parameters as original Host)
            Platforms = PlatformLayout.GenerateStandardRandPlatLayout(
                Content.Load<Texture2D>(GetStringProperty("casinoFloor.image.1", "CasinoFloor1")),
                GameArea,
                50,    // minPlatforms
                200,   // maxPlatforms
                50,    // minPlatformWidth
                100,   // maxPlatformWidth
                70,    // minPlatformHeight
                playerSpawnBuffer);
            
            // Debug: Print platform hitboxes (first 3 only)
            var platformTexture = Content.Load<Texture2D>(GetStringProperty("casinoFloor.image.1", "CasinoFloor1"));
            Logger.Info($"Platform texture dimensions: {platformTexture.Width}x{platformTexture.Height}");
            Logger.Info($"Generated {Platforms.Count} platforms");
            for (int i = 0; i < Math.Min(3, Platforms.Count); i++)
            {
                var platform = Platforms[i];
                Logger.Debug($"Platform {i}: Hitbox={platform.Hitbox}, Coords={platform.GetCoords()}");
            }
            
            // Generate casino machines (same as original Host)
            CasinoMachines = CasinoMachineFactory.SpawnCasinoMachines();
            
            // Debug: Log casino machine positions (first 3 only)
            Logger.Info($"Spawned {CasinoMachines.Count} casino machines");
            for (int i = 0; i < Math.Min(3, CasinoMachines.Count); i++)
            {
                var machine = CasinoMachines[i];
                Logger.Debug($"Casino machine {i}: Coords={machine.Coords}, Hitbox={machine.Hitbox}");
            }
        }
        
        private void CreateLocalPlayer()
        {
            LocalPlayer = new PlayableCharacter(
                playerIDs.GetNextID(),
                username,
                PlayerTexture,
                PlayerOrigin,
                Vector2.Zero,
                GetFloatProperty("playerMass", 5.0f),
                GetFloatProperty("playerInitialJumpVelocity", 240f),
                GetFloatProperty("playerRunSpeed", 240f),
                new Rectangle(PlayerOrigin.ToPoint(), new Point(PlayerTexture.Bounds.Width, PlayerTexture.Bounds.Height)),
                true);
            
            Logger.Info($"Player {LocalPlayer.GetID()} {LocalPlayer.GetUsername()} created at {LocalPlayer.Coords}");
        }
        
        private void CheckPlayerCollisions()
        {
            if (LocalPlayer == null) return;
            
            foreach (var entry in networkPlayers)
            {
                var otherPlayer = entry.Value.Player;
                if (otherPlayer.Hitbox.Intersects(LocalPlayer.Hitbox))
                {
                    Logger.LogCollision(LocalPlayer.GetID(), "player collision", $"with player {otherPlayer.GetID()}");
                }
            }
        }
        
        private void SendClientsUpdate()
        {
            if (serverUpdateTimer < 1.0f / 60.0f) return; // 60 FPS updates
            serverUpdateTimer = 0f;
            
            // Create player states array using GetPlayerState() method
            PlayerState[] playerStates = new PlayerState[networkPlayers.Count + 1];
            playerStates[0] = LocalPlayer.GetPlayerState();
            
            int index = 1;
            foreach (var entry in networkPlayers)
            {
                var player = entry.Value.Player;
                playerStates[index] = player.GetPlayerState();
                index++;
            }
            
            // Create casino machine states
            var casinoMachineStates = new CasinoMachineState[CasinoMachines.Count];
            for (int i = 0; i < CasinoMachines.Count; i++)
            {
                casinoMachineStates[i] = CasinoMachines[i].GetState();
            }
            
            // Send to all clients individually
            foreach (var entry in networkPlayers)
            {
                SendPacket(new PlayerReceiveUpdatePacket
                {
                    playerStates = playerStates,
                    casinoMachineStates = casinoMachineStates
                }, entry.Value.Peer, DeliveryMethod.Unreliable);
            }
        }
        
        // Network event handlers
        private void OnPlayerStateReceived(PlayerSendUpdatePacket packet, NetPeer peer)
        {
            if (peer == null)
            {
                // For relay connections, find the network player by matching coordinates or other criteria
                // For now, update the first non-local player
                foreach (var entry in networkPlayers.Values)
                {
                    if (entry.Player != LocalPlayer)
                    {
                        entry.Player.Coords = packet.coords;
                        entry.Player.Velocity = packet.velocity;
                        break;
                    }
                }
            }
            else if (networkPlayers.TryGetValue((uint)peer.Id, out NetworkPlayer networkPlayer))
            {
                networkPlayer.Player.Coords = packet.coords;
                networkPlayer.Player.Velocity = packet.velocity;
            }
        }
        
        private void OnJoinRequestReceived(JoinPacket joinPacket, NetPeer peer)
        {
            Logger.Info($"========== OnJoinRequestReceived called! Username: {joinPacket.username}, Peer: {(peer == null ? "null (relay)" : peer.Address.ToString())} ==========");
            
            if (peer == null)
            {
                Logger.LogNetwork("HOST", $"Received join from {joinPacket.username} via relay");
            }
            else
            {
                Logger.LogNetwork("HOST", $"Received join from {joinPacket.username} (peer id: {(uint)peer.Id})");
                
                // Validate peer connection state before processing
                if (peer.ConnectionState != ConnectionState.Connected)
                {
                    Logger.Error($"Cannot process join from peer {peer.Id} - not connected (state: {peer.ConnectionState})");
                    return;
                }
            }
            
            // Check if server has room for more players
            if (!playerIDs.RoomForNextPlayer())
            {
                Logger.Error($"Server is full, cannot accept join from {joinPacket.username}");
                // TODO: Send a rejection packet to the client
                return;
            }
            
            Logger.LogNetwork("HOST", $"Processing join request from {joinPacket.username}...");
            
            // Create new player for the client
            var spawnPosition = CalculateSpawnPosition();
            uint newPlayerId = playerIDs.GetNextID();
            var newPlayer = new PlayableCharacter(
                newPlayerId,
                joinPacket.username,
                PlayerTexture,
                spawnPosition,
                Vector2.Zero,
                joinPacket.playerMass,
                joinPacket.playerInitialJumpVelocity,
                joinPacket.playerMaxRunSpeed,
                new Rectangle(spawnPosition.ToPoint(), new Point(PlayerTexture.Bounds.Width, PlayerTexture.Bounds.Height)),
                false);
            
            // For relay connections, peer will be null
            if (peer == null)
            {
                Logger.LogNetwork("HOST", $"Player {newPlayer.GetID()} {newPlayer.GetUsername()} joined via relay");
                
                // Create a dummy NetworkPlayer with null peer (relay handles communication)
                var networkPlayer = new NetworkPlayer(null, newPlayer);
                networkPlayers[newPlayerId] = networkPlayer;
            }
            else
            {
                Logger.LogNetwork("HOST", $"Player {newPlayer.GetID()} {newPlayer.GetUsername()} joined from {peer.Address}");
                var networkPlayer = new NetworkPlayer(peer, newPlayer);
                networkPlayers[(uint)peer.Id] = networkPlayer;
            }
            
            // Send join acceptance
            SendJoinAccept(newPlayer, peer);
            
            // Broadcast to all other players that a new player joined
            foreach (var entry in networkPlayers)
            {
                if (entry.Value.Player.GetID() != newPlayer.GetID())
                {
                    SendPacket(new PlayerJoinedGamePacket
                    {
                        new_player_username = newPlayer.GetUsername(),
                        new_player_state = new PlayerState
                        {
                            pid = newPlayer.GetID(),
                            ges = new GameEntityState
                            {
                                awake = true,
                                coords = newPlayer.Coords,
                                velocity = newPlayer.Velocity
                            }
                        },
                        new_player_hitbox = newPlayer.Hitbox,
                        new_player_mass = joinPacket.playerMass,
                        new_player_initialJumpVelocity = joinPacket.playerInitialJumpVelocity,
                        new_player_maxRunSpeed = joinPacket.playerMaxRunSpeed
                    }, entry.Value.Peer, DeliveryMethod.ReliableOrdered);
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
        
        private void SendJoinAccept(PlayableCharacter newPlayer, NetPeer peer)
        {
            // Create platform states using GetState() method
            var platformStates = new PlatformState[Platforms.Count];
            for (int i = 0; i < Platforms.Count; i++)
            {
                platformStates[i] = Platforms[i].GetState();
            }
            
            // Create casino machine states using GetState() method
            var casinoMachineStates = new CasinoMachineState[CasinoMachines.Count];
            for (int i = 0; i < CasinoMachines.Count; i++)
            {
                casinoMachineStates[i] = CasinoMachines[i].GetState();
            }
            
            // Create player state using GetPlayerState() method
            var playerState = newPlayer.GetPlayerState();
            
            // Send join acceptance packet
            var joinAcceptPacket = new JoinAcceptPacket
            {
                gameArea = GameArea,
                playerHitbox = newPlayer.Hitbox,
                playerState = playerState,
                playerVelocity = newPlayer.Velocity,
                otherPlayerStates = new PlayerState[0], // TODO: Add other players
                platformStates = platformStates,
                casinoMachineStates = casinoMachineStates
            };
            
            SendPacket(joinAcceptPacket, peer, DeliveryMethod.ReliableOrdered);
        }
        
        // Public methods for lobby code access
        public void SetLobbyCode(string lobbyCode)
        {
            currentLobbyCode = lobbyCode;
        }
        
        public string GetLobbyCode()
        {
            return currentLobbyCode;
        }
        
        private void SendPacket<T>(T packet, NetPeer peer, DeliveryMethod deliveryMethod) where T : class, new()
        {
            if (packetProcessor != null)
            {
                var writer = new NetDataWriter();
                packetProcessor.Write(writer, packet);
                
                if (peer != null)
                {
                    // Direct connection
                    peer.Send(writer, deliveryMethod);
                    Logger.LogNetwork("HOST", $"Sent packet of type {typeof(T).Name} to peer (direct) using {deliveryMethod}");
                }
                else if (relayManager != null && relayManager.IsConnected)
                {
                    // Relay connection - send through relay server
                    relayManager.RelayServerPeer?.Send(writer, deliveryMethod);
                    Logger.LogNetwork("HOST", $"Sent packet of type {typeof(T).Name} via relay using {deliveryMethod}");
                }
            }
        }
        
        private void SendPacket<T>(T packet, DeliveryMethod deliveryMethod) where T : class, new()
        {
            // Broadcast to all network players
            if (packetProcessor != null)
            {
                var writer = new NetDataWriter();
                packetProcessor.Write(writer, packet);
                
                foreach (var entry in networkPlayers.Values)
                {
                    entry.Peer?.Send(writer, deliveryMethod);
                }
                
                Logger.LogNetwork("HOST", $"Broadcast packet of type {typeof(T).Name} using {deliveryMethod}");
            }
        }
        
        private Vector2 CalculateSpawnPosition()
        {
            // Calculate spawn position for this client (offset from host spawn)
            // Use a larger offset to avoid collision with casino machines and other objects
            Vector2 clientSpawnPosition = PlayerOrigin + new Vector2(nextSpawnOffset * 200, 0); // 200 pixels apart horizontally
            
            // Check for collisions with casino machines and adjust if necessary
            Rectangle clientHitbox = new Rectangle(clientSpawnPosition.ToPoint(), new Point(PlayerTexture.Bounds.Width, PlayerTexture.Bounds.Height));
            bool hasCollision = false;
            
            foreach (CasinoMachine machine in CasinoMachines)
            {
                if (clientHitbox.Intersects(machine.Hitbox))
                {
                    hasCollision = true;
                    Logger.LogCollision(0, "spawn collision", $"with casino machine at {machine.Coords}, adjusting spawn position");
                    break;
                }
            }
            
            // If collision detected, move spawn position further right
            if (hasCollision)
            {
                clientSpawnPosition = PlayerOrigin + new Vector2(nextSpawnOffset * 200 + 300, 0); // Extra 300 pixels offset
            }
            
            nextSpawnOffset++; // Increment for next client
            
            // Debug: Log spawn positions
            Logger.Debug($"Client spawn: offset={nextSpawnOffset-1}, position={clientSpawnPosition}, collision={hasCollision}");
            
            return clientSpawnPosition;
        }
        
        private void DrawLobbyCode()
        {
            // Draw lobby code in top-left corner with large, clear text
            var lobbyText = $"LOBBY CODE: {currentLobbyCode}";
            var scale = 2.0f; // Make it bigger!
            
            // Create background rectangle
            var textSize = Font.MeasureString(lobbyText) * scale;
            var backgroundRect = new Rectangle(10, 10, (int)textSize.X + 40, (int)textSize.Y + 20);
            
            // Draw semi-transparent background
            var backgroundTexture = new Texture2D(GraphicsDevice, 1, 1);
            backgroundTexture.SetData(new[] { Color.Black });
            SpriteBatch.Draw(backgroundTexture, backgroundRect, Color.Black * 0.8f);
            
            // Draw border
            var borderRect = new Rectangle(backgroundRect.X - 2, backgroundRect.Y - 2, backgroundRect.Width + 4, backgroundRect.Height + 4);
            var borderTexture = new Texture2D(GraphicsDevice, 1, 1);
            borderTexture.SetData(new[] { Color.White });
            SpriteBatch.Draw(borderTexture, borderRect, Color.White);
            
            // Draw lobby code text using proper font - LARGE and CLEAR
            var textPosition = new Vector2(backgroundRect.X + 20, backgroundRect.Y + 10);
            SpriteBatch.DrawString(Font, lobbyText, textPosition, Color.Yellow, 0f, Vector2.Zero, scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            
            // Clean up textures
            backgroundTexture.Dispose();
            borderTexture.Dispose();
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
                Logger.LogNetwork("HOST", "Peer connected via relay (no direct address)");
            }
            else
            {
                Logger.LogNetwork("HOST", $"Peer connected: {peer.Address}");
            }
        }
        
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (peer == null)
            {
                Logger.LogNetwork("HOST", $"Peer disconnected via relay, Reason: {disconnectInfo.Reason}");
            }
            else
            {
                Logger.LogNetwork("HOST", $"Peer disconnected: {peer.Address}, Reason: {disconnectInfo.Reason}");
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
            Logger.LogNetwork("HOST", $"Received unconnected message from {remoteEndPoint}");
        }
        
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            // Optional: Handle latency updates
        }
        
        public void OnConnectionRequest(ConnectionRequest request)
        {
            Logger.LogNetwork("HOST", $"Connection request from {request.RemoteEndPoint}");
            request.Accept();
        }
    }
}
