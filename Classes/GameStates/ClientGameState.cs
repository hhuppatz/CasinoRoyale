using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using CasinoRoyale.Classes.MonogameMethodExtensions;
using CasinoRoyale.Classes.Networking;
using CasinoRoyale.Utils;
using CasinoRoyale.Classes.GameSystems;
using CasinoRoyale.Classes.GameStates.Interfaces;
using CasinoRoyale.Classes.GameObjects.Player;
using CasinoRoyale.Classes.GameObjects.Platforms;
using CasinoRoyale.Classes.Networking.Players;
using System.Threading;

namespace CasinoRoyale.Classes.GameStates;

// Game state for joining a game as a client
public class ClientGameState(Game game, IGameStateManager stateManager, string lobbyCode) : GameState(game, stateManager)
{
    // Game world and player
    protected GameWorld GameWorld { get; private set; }
    protected PlayableCharacter LocalPlayer { get; set; }
    protected Texture2D PlayerTexture { get; set; }
    protected Vector2 PlayerOrigin { get; set; }

    // Network fields
    private readonly List<PlayableCharacter> _otherPlayers = [];
    private bool _connected = false;
    // Player initialized flag no longer needed
    private readonly string _lobbyCode = lobbyCode;
    private float _gameTime = 0f; // Track game time for state buffering
    
    private uint _clientId = 0; // Track this client's ID to filter packets
    
    
    // Connection handling
    private float _connectionTimeout = 10f; // 10 seconds timeout
    private bool _connectionAttempted = false;
    private bool _contentLoaded = false; // Track if game content has been loaded
    private bool _disposed = false; // Track if the object is being disposed
    
    
    
    // Client-specific fields
    
    private readonly PlayerIDs _playerIDs = new(6);

    public override void Initialize()
    {
        base.Initialize();
        
        Logger.LogNetwork("CLIENT", $"ClientGameState.Initialize() called with lobby code: {_lobbyCode}");
        
        // Initialize new singleton NetworkManager with client role
        NetworkManager.Instance.Initialize(false);
    }
    
    public override void LoadContent()
    {
        try
        {
            base.LoadContent();
            
            // Load only the player texture for now (needed for connection status display)
            string playerImageName = GameProperties.get("player.image", "ball");
            PlayerTexture = Content.Load<Texture2D>(playerImageName);
            
            Logger.LogNetwork("CLIENT", "Basic content loaded, waiting for connection...");
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
        this._gameTime += deltaTime; // Track total game time
        
        // Platform initialization removed; grid is now authoritative
        
        // Networking is handled by networked objects; no polling here
        
        if (!_connected || !_contentLoaded || LocalPlayer == null) return;

        LocalPlayer.TryMovePlayer(KeyboardState, PreviousKeyboardState, deltaTime, GameWorld);
        MainCamera.MoveToFollowPlayer(LocalPlayer);
        
        // Don't process multiplayer logic if game isn't fully initialized
        if (GameWorld == null) return;
        
        // Update game world (including items)
        GameWorld.Update(deltaTime, NetworkManager.Instance.IsHost);
        
        // Process buffered states and update interpolation for other players
        foreach (var otherPlayer in _otherPlayers)
        {
            otherPlayer.ProcessBufferedStates(deltaTime);
            otherPlayer.UpdateInterpolation(deltaTime);
        }

        // No manual network updates; networked components handle their own events
    }
    
    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkMagenta);
        
        // Show connection status if not connected or content not loaded
        if (!_connected || !_contentLoaded)
        {
            if (SpriteBatch != null && MainCamera != null)
            {
                SpriteBatch.Begin();
                string statusText;
                if (!_connected)
                {
                    statusText = _connectionAttempted ? 
                        $"Connection failed after {_connectionTimeout}s" : 
                        $"Connecting to lobby: {_lobbyCode}...";
                }
                else
                {
                    statusText = "Loading game content...";
                }
                SpriteBatch.DrawString(Font, statusText, new Vector2(100, 100), Color.White);
                SpriteBatch.End();
            }
            return;
        }
        
        // Check that essential components are initialized and content is loaded
        if (SpriteBatch == null || MainCamera == null || !_contentLoaded || GameWorld == null || LocalPlayer == null) return;
        
        Vector2 ratio = Resolution.ratio;
        MainCamera.ApplyRatio(ratio);
        
        SpriteBatch.Begin();
        
        // Draw game world objects
        GameWorld.DrawGameObjects();
        
        // Draw local player
        SpriteBatch.DrawEntity(MainCamera, LocalPlayer);
        
        // Draw other players
        foreach (var player in _otherPlayers)
        {
            SpriteBatch.DrawEntity(MainCamera, player);
        }
        
        SpriteBatch.End();
    }

    protected virtual void InitializeCamera()
    {
        if (LocalPlayer == null) return;
        MainCamera.InitMainCamera(Window, LocalPlayer);
    }
    
    private void LoadGameContent()
    {
        if (_contentLoaded) return;
        
        try
        {
            Logger.LogNetwork("CLIENT", "Loading game content after successful connection...");
            
            // Initialize GameWorld now that SpriteBatch is available and we're connected
            GameWorld = new GameWorld(GameProperties, Content, SpriteBatch, MainCamera, Resolution.ratio);
            
            _contentLoaded = true;
            Logger.LogNetwork("CLIENT", "Game content loaded successfully");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error loading game content: {ex.Message}");
            Logger.Error($"Stack trace: {ex.StackTrace}");
        }
    }
    
    #region Network Event Handlers

    private void OnConnectionEstablished(object sender, ConnectionEstablishedEventArgs e)
    {
        if (_disposed) return;
        
        Logger.LogNetwork("CLIENT", "Connection established successfully");
        _connected = true;
        _connectionAttempted = true;
    }

    private void OnPlayerStatesUpdateReceived(object sender, PacketReceivedEventArgs<PlayerReceiveUpdatePacket> e)
    {
        if (_disposed) return;
        
        var update = e.Packet;
        if (update.playerStates == null) return;
        
        Logger.LogNetwork("CLIENT", $"Received player states update with {update.playerStates.Length} players");
        
        // Don't process player states until we're fully connected and initialized
        if (!_connected || !_contentLoaded || LocalPlayer == null) 
        {
            Logger.LogNetwork("CLIENT", $"Ignoring player states update - not ready (connected: {_connected}, contentLoaded: {_contentLoaded}, localPlayer: {LocalPlayer != null})");
            return;
        }
        
        foreach (var playerState in update.playerStates)
        {
            // Skip our own player state
            if (playerState.pid == LocalPlayer?.GetID()) 
            {
                Logger.LogNetwork("CLIENT", $"Skipping own player state (ID: {playerState.pid})");
                continue;
            }
            
            Logger.LogNetwork("CLIENT", $"Processing player state for ID {playerState.pid} ({playerState.username})");
            
            // Find or create other player
            var otherPlayer = _otherPlayers.Find(p => p.GetID() == playerState.pid);
            if (otherPlayer == null)
            {
                Logger.LogNetwork("CLIENT", $"Player {playerState.pid} not found in _otherPlayers list, creating new player");
                
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
                    playerState.ges.mass,
                    playerState.initialJumpVelocity,
                    playerState.maxRunSpeed,
                    new Rectangle(playerState.ges.coords.ToPoint(), new Point(PlayerTexture.Bounds.Width, PlayerTexture.Bounds.Height)),
                    false);
                otherPlayer.MarkAsNewPlayer();
                
                // Initialize interpolation targets
                otherPlayer.InitializeTargets();
                
                _otherPlayers.Add(otherPlayer);
                Logger.LogNetwork("CLIENT", $"Created new other player with ID {otherPlayer.GetID()} ({playerState.username}). Total other players: {_otherPlayers.Count}");
            }
            else
            {
                // Update existing player with buffered state for delayed interpolation
                otherPlayer.AddBufferedState(playerState.ges.coords, playerState.ges.velocity, _gameTime);
                Logger.LogNetwork("CLIENT", $"Updated existing player {otherPlayer.GetID()} position: {playerState.ges.coords}");
            }
        }
    }
    
    private void OnJoinAcceptReceived(object sender, PacketReceivedEventArgs<JoinAcceptPacket> e)
    {
        if (_disposed) return;
        
        var joinAccept = e.Packet;
        
        // Set client ID FIRST before any filtering to ensure proper packet processing
        if (_clientId == 0)
        {
            _clientId = joinAccept.targetClientId;
            Logger.LogNetwork("CLIENT", $"Set client ID to {_clientId} from JoinAccept packet");
        }
        
        // Filter packets - only process JoinAccept packets meant for this client
        if (joinAccept.targetClientId != _clientId)
        {
            Logger.LogNetwork("CLIENT", $"Ignoring JoinAccept packet - target client ID {joinAccept.targetClientId} does not match this client's ID {_clientId}");
            return;
        }
        
        Logger.LogNetwork("CLIENT", $"OnJoinAcceptReceived called - processing JoinAccept packet for client ID {joinAccept.targetClientId}");
        
        // Load game content now that we're connected
        LoadGameContent();
        
        // Create local player from player state
        var playerState = joinAccept.playerState;
        LocalPlayer = new PlayableCharacter(
            playerState.pid,
            playerState.username,
            PlayerTexture,
            playerState.ges.coords,
            joinAccept.playerVelocity, // Use playerVelocity from packet
            playerState.ges.mass,
            playerState.initialJumpVelocity,
            playerState.maxRunSpeed,
            joinAccept.playerHitbox,
            true
        );
        LocalPlayer.MarkAsNewPlayer();

        PlayerOrigin = playerState.ges.coords;
        
        // Initialize camera AFTER creating player
        if (LocalPlayer == null) return;
        MainCamera.InitMainCamera(Window, LocalPlayer);
        
        // Mark player as properly initialized from server
        // Player ready
        
        Logger.LogNetwork("CLIENT", $"Player initialized with HOST-ASSIGNED ID: {LocalPlayer.GetID()}");
        Logger.LogNetwork("CLIENT", $"Client ID confirmed as {_clientId} for packet filtering");
        
        // Create other players from other player states
        Logger.LogNetwork("CLIENT", $"Creating other players from {joinAccept.otherPlayerStates?.Length ?? 0} other player states");
        foreach (var otherPlayerState in joinAccept.otherPlayerStates ?? [])
        {
            if (otherPlayerState.pid != LocalPlayer.GetID())
            {
                Logger.LogNetwork("CLIENT", $"Creating other player with ID {otherPlayerState.pid} ({otherPlayerState.username})");
                var newOtherPlayer = new PlayableCharacter(
                    otherPlayerState.pid,
                    otherPlayerState.username,
                    PlayerTexture,
                    otherPlayerState.ges.coords,
                    joinAccept.playerVelocity, // Use playerVelocity from packet
                    otherPlayerState.ges.mass,
                    otherPlayerState.initialJumpVelocity,
                    otherPlayerState.maxRunSpeed,
                    joinAccept.playerHitbox,
                    true);
                newOtherPlayer.MarkAsNewPlayer();
                _otherPlayers.Add(newOtherPlayer);
                Logger.LogNetwork("CLIENT", $"Added other player to list. Total other players: {_otherPlayers.Count}");
            }
        }
        
        _connected = true;
        _connectionAttempted = true;
    }
    
    private void OnGameWorldInitReceived(object sender, PacketReceivedEventArgs<GameWorldInitPacket> e)
    {
        if (_disposed) return;
        
        var gameWorldInit = e.Packet;
        
        // Set client ID if not already set (should be set by JoinAccept packet)
        if (_clientId == 0)
        {
            _clientId = gameWorldInit.targetClientId;
            Logger.LogNetwork("CLIENT", $"Set client ID to {_clientId} from GameWorldInit packet");
        }
        
        // Filter packets - only process GameWorldInit packets meant for this client
        if (gameWorldInit.targetClientId != _clientId)
        {
            Logger.LogNetwork("CLIENT", $"Ignoring GameWorldInit packet - target client ID {gameWorldInit.targetClientId} does not match this client's ID {_clientId}");
            return;
        }
        
        Logger.LogNetwork("CLIENT", $"OnGameWorldInitReceived called - processing GameWorldInit packet for client ID {gameWorldInit.targetClientId}");
        
        // Only process if we're fully connected and initialized
        if (!_connected || !_contentLoaded || GameWorld == null) 
        {
            Logger.Warning($"CLIENT: Ignoring GameWorldInit packet - not ready (connected: {_connected}, contentLoaded: {_contentLoaded}, gameWorld: {GameWorld != null})");
            return;
        }
        
        // Initialize GameWorld with the received data
        Logger.LogNetwork("CLIENT", $"Initializing GameWorld with game area: {gameWorldInit.gameArea}, items: {gameWorldInit.itemStates?.Length ?? 0}");
        
        // Initialize GameWorld using the existing method
        var tempJoinAccept = new JoinAcceptPacket
        {
            gameArea = gameWorldInit.gameArea,
            itemStates = gameWorldInit.itemStates,
            gridTiles = gameWorldInit.gridTiles
        };
        
        GameWorld.InitializeGameWorldFromState(tempJoinAccept);
        
        Logger.LogNetwork("CLIENT", $"GameWorld initialized. Items: {GameWorld.AllItems.Count}");
    }
    
    // Platform packets removed; grid is authoritative

    // Casino machine update handler removed

    private void OnItemRemovedReceived(object sender, PacketReceivedEventArgs<ItemRemovedPacket> e)
    {
        var packet = e.Packet;
        
        // Only process updates if we're fully connected and initialized
        if (!_connected || !_contentLoaded || GameWorld == null) return;
        
        GameWorld.RemoveItemById(packet.itemId);
    }
    
    private void OnItemSpawnedReceived(object sender, PacketReceivedEventArgs<ItemUpdatePacket> e)
    {
        var packet = e.Packet;
        // Only process updates if we're fully connected and initialized
        if (!_connected || !_contentLoaded || GameWorld == null) return;

        var len = packet.itemStates.Length;
        for (int i = 0; i < len; i++)
        {
            var itemState = packet.itemStates[i];
            if (GameWorld.GetItemById(itemState.itemId) != null) continue;

            GameWorld.SpawnItem(itemState.itemType, itemState.gameEntityState.coords, itemState.gameEntityState.velocity);
        }
    }
    
    private void OnPlayerJoinedGameReceived(object sender, PacketReceivedEventArgs<PlayerJoinedGamePacket> e)
    {
        var packet = e.Packet;
        
        // Don't process player joined events until we're fully connected and initialized
        if (!_connected || !_contentLoaded || PlayerTexture == null) return;
        
        _otherPlayers.Add(new PlayableCharacter(
            packet.new_player_state.pid,
            packet.new_player_username,
            PlayerTexture,
            packet.new_player_state.ges.coords,
            packet.new_player_state.ges.velocity,
            packet.new_player_state.ges.mass,
            packet.new_player_state.initialJumpVelocity,
            packet.new_player_state.maxRunSpeed,
            packet.new_player_hitbox,
            true));
    }
    
    private void OnPlayerLeftGameReceived(object sender, PacketReceivedEventArgs<PlayerLeftGamePacket> e)
    {
        var packet = e.Packet;
        foreach (PlayableCharacter otherPlayer in _otherPlayers)
        {
            if (packet.pid == otherPlayer.GetID())
            {
                _otherPlayers.Remove(otherPlayer);
                break;
            }
        }
    }
    
    private void OnNetworkError(object sender, string error) { }

    // Public method to send player left packet (called from main game disposal)
    public void SendPlayerLeftPacket() { }

    #endregion

    public override void Dispose()
    {
        _disposed = true; // Set disposed flag to prevent event handlers from running
        
        base.Dispose();
        
    }
}