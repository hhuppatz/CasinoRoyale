using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using CasinoRoyale.Classes.GameObjects;
using CasinoRoyale.Classes.MonogameMethodExtensions;
using CasinoRoyale.Classes.Networking;
using CasinoRoyale.Utils;
using CasinoRoyale.Classes.GameSystems;
using CasinoRoyale.Classes.GameObjects.CasinoMachines;
using CasinoRoyale.Classes.GameObjects.Platforms;

namespace CasinoRoyale.Classes.GameStates
{
    // Game state for joining a game as a client
    public class ClientGameState(Game game, IGameStateManager stateManager, string lobbyCode) : GameState(game, stateManager), INetEventListener
    {
        // Game world and player
        protected GameWorld GameWorld { get; private set; }
        protected PlayableCharacter LocalPlayer { get; set; }
        protected Texture2D PlayerTexture { get; set; }
        protected Vector2 PlayerOrigin { get; set; }

        // Network fields
        private LiteNetRelayManager relayManager;
        private NetDataWriter writer;
        private NetPacketProcessor packetProcessor;
        private readonly List<PlayableCharacter> otherPlayers = [];
        private NetPeer server;
        private bool connected = false;
        private readonly string lobbyCode = lobbyCode;
        private float gameTime = 0f; // Track game time for state buffering
        
        // Client-specific fields
        private readonly string username = "HH";
        private readonly PlayerIDs playerIDs = new(6);
        
        
        // Handle casino machine interaction - set spawnedCoin flag and let host handle the rest
        public void TryInteractWithCasinoMachine()
        {
            if (LocalPlayer == null || GameWorld?.WorldObjects == null) return;
            
            Logger.Info($"CLIENT DEBUG: TryInteractWithCasinoMachine called - Player hitbox: {LocalPlayer.Hitbox}, Casino machines count: {GameWorld.WorldObjects.CasinoMachines.Count}");
            
            // Check if player is colliding with any casino machine
            foreach (var machine in GameWorld.WorldObjects.CasinoMachines)
            {
                Logger.Info($"CLIENT DEBUG: Checking machine {machine.GetState().machineNum} - Hitbox: {machine.Hitbox}, Intersects: {LocalPlayer.Hitbox.Intersects(machine.Hitbox)}");
                
                if (LocalPlayer.Hitbox.Intersects(machine.Hitbox))
                {
                    // Check if machine hasn't already spawned a coin
                    if (!machine.SpawnedCoin)
                    {
                        // Set the spawnedCoin flag to request coin spawning from host
                        machine.SpawnedCoin = true;
                        machine.MarkAsChanged();
                        
                        Logger.Info($"CLIENT DEBUG: Set spawnedCoin=true for machine {machine.GetState().machineNum}, will be sent to host");
                    }
                    else
                    {
                        Logger.Info($"CLIENT DEBUG: Machine {machine.GetState().machineNum} already spawned a coin");
                    }
                    break; // Only interact with one machine at a time
                }
            }
        }
        
        
        // Get changed casino machine states (no longer need request IDs)
        private CasinoMachineState[] GetChangedCasinoMachineStates()
        {
            var changedMachines = GameWorld.WorldObjects.CasinoMachines.Where(m => m.HasChanged).ToList();
            var casinoMachineStates = new List<CasinoMachineState>();
            
            Logger.Info($"CLIENT DEBUG: GetChangedCasinoMachineStates - Found {changedMachines.Count} changed machines out of {GameWorld.WorldObjects.CasinoMachines.Count} total");
            
            foreach (var machine in changedMachines)
            {
                var state = machine.GetState();
                Logger.Info($"CLIENT DEBUG: Machine {state.machineNum} - spawnedCoin={state.spawnedCoin}");
                casinoMachineStates.Add(state);
            }
            
            Logger.Info($"CLIENT DEBUG: Returning {casinoMachineStates.Count} casino machine states");
            return casinoMachineStates.ToArray();
        }

        public override void Initialize()
        {
            base.Initialize();
            
            // Initialize GameWorld
            GameWorld = new GameWorld(GameProperties, Content);
            
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
            packetProcessor.RegisterNestedType<CoinState>();
            
            // Set up packet processing - register both with and without NetPeer for relay compatibility
            packetProcessor.SubscribeReusable<PlayerReceiveUpdatePacket, NetPeer>(OnPlayerStatesUpdateReceived);
            packetProcessor.SubscribeReusable<PlayerReceiveUpdatePacket>(p => OnPlayerStatesUpdateReceived(p, null));
            
            packetProcessor.SubscribeReusable<JoinAcceptPacket, NetPeer>(OnJoinAcceptReceived);
            packetProcessor.SubscribeReusable<JoinAcceptPacket>(p => OnJoinAcceptReceived(p, null));
            
            packetProcessor.SubscribeReusable<PlatformUpdatePacket, NetPeer>(OnPlatformUpdateReceived);
            packetProcessor.SubscribeReusable<PlatformUpdatePacket>(p => OnPlatformUpdateReceived(p, null));
            
            packetProcessor.SubscribeReusable<CasinoMachineUpdatePacket, NetPeer>(OnCasinoMachineUpdateReceived);
            packetProcessor.SubscribeReusable<CasinoMachineUpdatePacket>(p => OnCasinoMachineUpdateReceived(p, null));
            
            
            packetProcessor.SubscribeReusable<CoinRemovedPacket, NetPeer>(OnCoinRemovedReceived);
            packetProcessor.SubscribeReusable<CoinRemovedPacket>(p => OnCoinRemovedReceived(p, null));
            
            
            packetProcessor.SubscribeReusable<CoinSpawnedPacket, NetPeer>(OnCoinSpawned);
            packetProcessor.SubscribeReusable<CoinSpawnedPacket>(p => OnCoinSpawned(p, null));
            
            
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
            try
            {
                base.LoadContent();

                // Load player texture using GameWorld
                if (GameWorld == null)
                {
                    Logger.Error("GameWorld is null in LoadContent()!");
                    return;
                }
                
                string playerImageName = GameProperties.get("player.image", "ball");
                Logger.Info($"Loading player texture: {playerImageName}");
                PlayerTexture = Content.Load<Texture2D>(playerImageName);
                
                Logger.Info($"CLIENT DEBUG: Before first InitializeGameWorld - Coins: {GameWorld.WorldObjects.Coins.Count}");
                GameWorld.InitializeGameWorld(Content, PlayerOrigin);
                Logger.Info($"CLIENT DEBUG: After first InitializeGameWorld - Coins: {GameWorld.WorldObjects.Coins.Count}");
                
                // Create local player
                CreateLocalPlayer();
                
                InitializeCamera();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in ClientGameState.LoadContent(): {ex.Message}");
                Logger.Error($"Stack trace: {ex.StackTrace}");
            }
        }
        
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            this.gameTime += deltaTime; // Track total game time
            
            // Always poll events, even when not connected (to receive JOINED_LOBBY, etc.)
            relayManager.PollEvents();
            
            if (!connected || LocalPlayer == null) 
            {
                if (!connected) Logger.Info("CLIENT DEBUG: Not connected, skipping update");
                if (LocalPlayer == null) Logger.Info("CLIENT DEBUG: LocalPlayer is null, skipping update");
                return;
            }

            // Check for casino machine interaction (H key) before movement
            bool hKeyPressed = KeyboardState.GetPressedKeys().Contains(Keys.H);
            bool hKeyWasPressed = PreviousKeyboardState.GetPressedKeys().Contains(Keys.H);

            // Debug: Show H key state when it changes
            if (hKeyPressed != hKeyWasPressed)
            {
                Logger.Info($"CLIENT DEBUG: H key state changed - Current: {hKeyPressed}, Previous: {hKeyWasPressed}");
            }
            
            if (hKeyPressed && !hKeyWasPressed)
            {
                Logger.Info("CLIENT DEBUG: H key pressed - calling TryInteractWithCasinoMachine");
                TryInteractWithCasinoMachine();
            }
            
            LocalPlayer.TryMovePlayer(KeyboardState, PreviousKeyboardState, deltaTime, GameWorld);
            MainCamera.MoveToFollowPlayer(LocalPlayer);
            
            // Don't process multiplayer logic if game isn't fully initialized
            if (GameWorld == null || GameWorld.WorldObjects == null) return;
            
            // Update game world objects (including coins)
            GameWorld.WorldObjects.Update(deltaTime, GameWorld.GameArea);
            
            // Process buffered states and update interpolation for other players
            foreach (var otherPlayer in otherPlayers)
            {
                otherPlayer.ProcessBufferedStates(deltaTime);
                otherPlayer.UpdateInterpolation(deltaTime);
            }
            
            // Send player state to host
            SendPlayerState(deltaTime);
        }
        
        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkMagenta);
            
            // Check if essential components are initialized
            if (SpriteBatch == null)
            {
                Logger.Error("SpriteBatch is null in ClientGameState.Draw() - skipping draw");
                return;
            }
            
            if (MainCamera == null)
            {
                Logger.Error("MainCamera is null in ClientGameState.Draw() - skipping draw");
                return;
            }
            
            if (GameWorld == null || GameWorld.WorldObjects == null)
            {
                Logger.Error("GameWorld is not initialized in ClientGameState.Draw() - skipping draw");
                return;
            }
            
            if (!connected || LocalPlayer == null) 
            {
                // Client not yet connected, skip drawing game objects
                return;
            }
            
            Vector2 ratio = Resolution.ratio;
            MainCamera.ApplyRatio(ratio);
            
            SpriteBatch.Begin();
            
            // Draw game world objects
            GameWorld.DrawGameObjects(SpriteBatch, MainCamera, ratio);
            
            // Draw local player
            if (LocalPlayer != null)
            {
                SpriteBatch.DrawEntity(MainCamera, LocalPlayer);
            }
            
            // Draw other players
            DrawOtherPlayers();
            
            SpriteBatch.End();
        }

        protected virtual void InitializeCamera()
        {
            if (LocalPlayer != null)
            {
                MainCamera.InitMainCamera(Window, LocalPlayer);
            }
        }
        
        protected void DrawOtherPlayers()
        {
            // Draw other players
            foreach (var player in otherPlayers)
            {
                SpriteBatch.DrawEntity(MainCamera, player);
            }
        }

        private void CreateLocalPlayer()
        {
            if (PlayerTexture == null)
            {
                Logger.Error("PlayerTexture is null in CreateLocalPlayer()!");
                return;
            }
            
            LocalPlayer = new PlayableCharacter(
                playerIDs.GetNextID(),
                username,
                PlayerTexture,
                PlayerOrigin,
                Vector2.Zero,
                GetFloatProperty("playerMass", 5.0f),
                GetFloatProperty("playerInitialJumpVelocity", 240f),
                GetFloatProperty("playerStandardSpeed", 240f),
                new Rectangle(PlayerOrigin.ToPoint(), new Point(PlayerTexture.Bounds.Width, PlayerTexture.Bounds.Height)),
                true);
            LocalPlayer.MarkAsNewPlayer();
            
            // Initialize interpolation targets
            LocalPlayer.InitializeTargets();
            
            Logger.Info($"Player {LocalPlayer.GetID()} {LocalPlayer.GetUsername()} created at {LocalPlayer.Coords}");
        }
        
        // Network methods //

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
            
            // Only send updates if local player has changed or if there are changed casino machines
            var changedCasinoMachineStates = GetChangedCasinoMachineStates();
            
            if (changedCasinoMachineStates.Length > 0)
            {
                Logger.Info($"CLIENT DEBUG: SendPlayerState - Sending {changedCasinoMachineStates.Length} casino machine states to host");
            }
            
            if (LocalPlayer.HasChanged || changedCasinoMachineStates.Length > 0)
            {
                var playerState = new PlayerSendUpdatePacket
                {
                    coords = LocalPlayer.Coords,
                    velocity = LocalPlayer.Velocity,
                    dt = deltaTime,
                    casinoMachineStates = changedCasinoMachineStates
                };
                
                // Use reliable delivery for coin requests to prevent packet loss
                SendPacket(playerState, DeliveryMethod.ReliableOrdered);
                
                // Clear change flags after sending
                LocalPlayer.ClearChangedFlag();
                GameWorld.WorldObjects.ClearAllChangedFlags();
            }
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
                    if (PlayerTexture == null)
                    {
                        Logger.Error("PlayerTexture is null when creating other player!");
                        continue;
                    }
                    
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
                    otherPlayer.MarkAsNewPlayer();
                    
                    // Initialize interpolation targets
                    otherPlayer.InitializeTargets();
                    
                    otherPlayers.Add(otherPlayer);
                    Logger.LogNetwork("CLIENT", $"Added player {otherPlayer.GetID()} ({otherPlayer.GetUsername()})");
                }
                else
                {
                    // Update existing player with buffered state for delayed interpolation
                    otherPlayer.AddBufferedState(playerState.ges.coords, playerState.ges.velocity, gameTime);
                }
            }
            
            // Casino machine updates are now handled by individual CasinoMachineUpdatePacket
            // This old logic is no longer needed
            
            // Coin updates are now handled by individual CoinUpdatePacket
            // This old logic is no longer needed
        }
        
        private void OnJoinAcceptReceived(JoinAcceptPacket joinAccept, NetPeer peer)
        {
            Logger.Info($"CLIENT DEBUG: Received JoinAcceptPacket - Coins: {joinAccept.coinStates?.Length ?? 0}, Casino machines: {joinAccept.casinoMachineStates?.Length ?? 0}");
            Logger.Info($"========== OnJoinAcceptReceived called! Peer: {(peer == null ? "null (relay)" : peer.Address.ToString())} ==========");
            
            // Set up game world
            Logger.Info($"CLIENT DEBUG: Before second InitializeGameWorld - Coins: {GameWorld.WorldObjects.Coins.Count}");
            GameWorld.InitializeGameWorld(Content, PlayerOrigin, joinAccept.gameArea);
            Logger.Info($"CLIENT DEBUG: After second InitializeGameWorld - Coins: {GameWorld.WorldObjects.Coins.Count}");
            
            // Create local player from player state
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
                true
            );
            LocalPlayer.MarkAsNewPlayer();

            PlayerOrigin = playerState.ges.coords;
            
            // Initialize camera AFTER creating player
            InitializeCamera();
            Logger.Info("Player created and camera initialized!");
            
            // Recreate coins from coin states
            if (joinAccept.coinStates != null)
            {
                Logger.Info($"Client recreating {joinAccept.coinStates.Length} coins from join accept");
                GameWorld.WorldObjects.RecreateCoinsFromStates(joinAccept.coinStates);
            }
            
            // Recreate casino machines from casino machine states (override generated ones)
            if (joinAccept.casinoMachineStates != null)
            {
                Logger.Info($"CLIENT DEBUG: Recreating {joinAccept.casinoMachineStates.Length} casino machines from host");
                GameWorld.WorldObjects.RecreateCasinoMachinesFromStates(Content, joinAccept.casinoMachineStates);
            }
            
            // Create other players from other player states
            foreach (var otherPlayerState in joinAccept.otherPlayerStates ?? [])
            {
                if (otherPlayerState.pid != LocalPlayer.GetID())
                {
                    var newOtherPlayer = new PlayableCharacter(
                        otherPlayerState.pid,
                        otherPlayerState.username,
                        PlayerTexture,
                        otherPlayerState.ges.coords,
                        joinAccept.playerVelocity, // Use playerVelocity from packet
                        otherPlayerState.mass,
                        otherPlayerState.initialJumpVelocity,
                        otherPlayerState.maxRunSpeed,
                        joinAccept.playerHitbox,
                        true);
                    newOtherPlayer.MarkAsNewPlayer();
                    otherPlayers.Add(newOtherPlayer);
                }
            }
            
            connected = true;
            // For relay connections, peer is null, so use the relay server peer
            server = peer ?? relayManager.RelayServerPeer;
            
            Logger.LogNetwork("CLIENT", $"Player {LocalPlayer.GetID()} ({LocalPlayer.GetUsername()}) created");
            Logger.LogNetwork("CLIENT", $"Game area: {GameWorld.GameArea}");
            Logger.LogNetwork("CLIENT", $"Platforms: {GameWorld.WorldObjects.Platforms.Count}, Casino machines: {GameWorld.WorldObjects.CasinoMachines.Count}");
            Logger.Info("Join process completed successfully - client is now fully connected!");
        }
        
        
        private void OnPlatformUpdateReceived(PlatformUpdatePacket packet, NetPeer peer)
        {
            // Only process updates if we're fully connected and initialized
            if (!connected || GameWorld?.WorldObjects == null) return;
            
            GameWorld.WorldObjects.UpdatePlatformById(packet.platformId, packet.platformState);
        }

        private void OnCasinoMachineUpdateReceived(CasinoMachineUpdatePacket packet, NetPeer peer)
        {
            // Only process updates if we're fully connected and initialized
            if (!connected || GameWorld?.WorldObjects == null) return;
            
            GameWorld.WorldObjects.UpdateCasinoMachineById(packet.machineId, packet.machineState);
        }


        private void OnCoinRemovedReceived(CoinRemovedPacket packet, NetPeer peer)
        {
            // Only process updates if we're fully connected and initialized
            if (!connected || GameWorld?.WorldObjects == null) return;
            
            GameWorld.WorldObjects.RemoveCoinById(packet.coinId);
        }
        
        
        private void OnCoinSpawned(CoinSpawnedPacket packet, NetPeer peer)
        {
            // Only process updates if we're fully connected and initialized
            if (!connected || GameWorld?.WorldObjects == null) return;
            
            Logger.Info($"CLIENT DEBUG: Received CoinSpawnedPacket - CoinId: {packet.coinState.coinId}, Coords: {packet.coinState.coords}");
            
            // Check if coin already exists (to avoid duplicates)
            var existingCoin = GameWorld.WorldObjects.Coins.FirstOrDefault(c => c.CoinId == packet.coinState.coinId);
            if (existingCoin == null)
            {
                // Add the coin to our world
                var coin = new Coin(packet.coinState.coinId, GameWorld.WorldObjects.GetCoinTexture(), packet.coinState.coords, packet.coinState.velocity);
                coin.MarkAsChanged();
                GameWorld.WorldObjects.Coins.Add(coin);
                
            }
            
            Logger.Info($"Coin {packet.coinState.coinId} added to client world, Total coins: {GameWorld.WorldObjects.Coins.Count}");
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
                packet.new_player_standardSpeed,
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
                playerStandardSpeed = GetFloatProperty("playerStandardSpeed", 240f)
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
