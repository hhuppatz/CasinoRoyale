using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using CasinoRoyale.Classes.MonogameMethodExtensions;
using CasinoRoyale.Classes.Networking;
using CasinoRoyale.Utils;
using CasinoRoyale.Classes.GameSystems;
using CasinoRoyale.Classes.GameStates.Interfaces;
using CasinoRoyale.Classes.GameObjects.Player;
using CasinoRoyale.Classes.Networking.Players;

namespace CasinoRoyale.Classes.GameStates;

// Game state for hosting a game
public class HostGameState : GameState
{
    // Game world and player
    protected GameWorld GameWorld { get; private set; }
    protected PlayableCharacter LocalPlayer { get; set; }
    protected Texture2D PlayerTexture { get; set; }
    protected Vector2 PlayerOrigin { get; set; }

    // Network fields
    private float _serverUpdateTimer = 0f;
    private float _platformUpdateTimer = 0f; // Separate timer for platform updates
    private float _gameTime = 0f; // Track game time for state buffering
    private readonly uint _maxPlayers = 6;
    private readonly PlayerIDs _playerIDs;
    private int _nextSpawnOffset = 0;
    private string _currentLobbyCode;
    
    // Host-specific fields
    private readonly string _username = "HAZZA";
        
    public HostGameState(Game game, IGameStateManager stateManager) : base(game, stateManager)
    {
        _playerIDs = new PlayerIDs(_maxPlayers);
    }
        
    public override void Initialize()
    {
        base.Initialize();
        
        // Note: GameWorld will be initialized in LoadContent() after SpriteBatch is created
        
        // Initialize new singleton NetworkManager with host role
        NetworkManager.Instance.Initialize(true);
    }
        
    public override void LoadContent()
    {
        try
        {
            base.LoadContent();
            
            // Initialize GameWorld now that SpriteBatch is available
            GameWorld = new GameWorld(GameProperties, Content, SpriteBatch, MainCamera, Resolution.ratio);
            
            string playerImageName = GameProperties.get("player.image", "ball");
            PlayerTexture = Content.Load<Texture2D>(playerImageName);

            GameWorld.InitializeGameWorld(PlayerOrigin);
            
            // Calculate player spawn buffer using GameWorld
            if (PlayerTexture != null)
            {
                PlayerOrigin = GameWorld.CalculatePlayerOrigin(PlayerTexture.Height);
            }
            
            // Create local player using texture and origin
            CreateLocalPlayer();
            
            // Debug: Print player hitbox info
            
            InitializeCamera();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error in HostGameState.LoadContent(): {ex.Message}");
            Logger.Error($"Stack trace: {ex.StackTrace}");
        }
    }
    
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _serverUpdateTimer += deltaTime;
        _platformUpdateTimer += deltaTime;
        this._gameTime += deltaTime; // Track total game time
        
        // Networking events are handled by networked objects; no polling here

        // Common update logic
        if (LocalPlayer != null)
        {
            LocalPlayer.TryMovePlayer(KeyboardState, PreviousKeyboardState, deltaTime, GameWorld);
            MainCamera.MoveToFollowPlayer(LocalPlayer);
        }
        
        // Don't process multiplayer logic if game isn't fully initialized
        if (LocalPlayer == null || GameWorld == null) return;
        
        // Update game world (including items)
        GameWorld.Update(deltaTime, NetworkManager.Instance.IsHost);
    }
    
    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkMagenta);
        
        // Check if essential components are initialized
        if (SpriteBatch == null || MainCamera == null || GameWorld == null) return;
        
        Vector2 ratio = Resolution.ratio;
        MainCamera.ApplyRatio(ratio);
        
        SpriteBatch.Begin();
        
        // Draw objects in game world
        GameWorld.DrawGameObjects();
        
        // Draw local player
        SpriteBatch.DrawEntity(MainCamera, LocalPlayer);
        
        // Draw other players
        DrawOtherPlayers();
        
        // Draw lobby code in top-left corner (debugging)
        if (!string.IsNullOrEmpty(_currentLobbyCode) && Font != null)
        {
            DrawLobbyCode();
        }
        
        SpriteBatch.End();
    }

    protected virtual void InitializeCamera()
    {
        if (LocalPlayer == null) return;
        MainCamera.InitMainCamera(Window, LocalPlayer);
    }
    
    protected void DrawOtherPlayers()
    {
        // Other players are drawn by their own networked components in the new system
    }
    
    private void CreateLocalPlayer()
    {
        if (PlayerTexture == null) return;
        
        // For the host, use a reserved ID (0) for the local player
        // This ensures the host always has a consistent ID regardless of netPeer
        uint hostPlayerId = 0;
        
        LocalPlayer = new PlayableCharacter(
            hostPlayerId,
            _username,
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
    }
    
    private void SendClientsUpdate() { }

    // Casino machine handling removed

    #region Network Event Handlers

    // Old network event handlers are no longer needed with the new networking system
    
    // Public methods for lobby code access
    public void SetLobbyCode(string lobbyCode)
    {
        _currentLobbyCode = lobbyCode;
    }
    
    public string GetLobbyCode()
    {
        return _currentLobbyCode;
    }
    
    private Vector2 CalculateSpawnPosition()
    {
        if (PlayerTexture == null)
        {
            Logger.Error("PlayerTexture is null in CalculateSpawnPosition()!");
            return PlayerOrigin;
        }
        
        // Calculate spawn position for this client (offset from host spawn)
        // Use a larger offset to avoid collision with casino machines and other objects
        Vector2 clientSpawnPosition = PlayerOrigin + new Vector2(_nextSpawnOffset * 200, 0); // 200 pixels apart horizontally
        
        // Casino machine collision avoidance removed
        
        _nextSpawnOffset++; // Increment for next client
        
        // Debug: Log spawn positions
        
        return clientSpawnPosition;
    }
    
    private uint GenerateUniquePlayerId() { return 0; }
    
    private void ProcessJoinRequest(JoinPacket joinPacket, NetPeer peer, uint newPlayerId) { }
    
    private void DrawLobbyCode()
    {
        if (Font == null || GraphicsDevice == null)
        {
            Logger.Error("Font or GraphicsDevice is null in DrawLobbyCode()!");
            return;
        }
        
        // Draw lobby code in top-left corner with smaller, compact text
        var lobbyText = $"LOBBY: {_currentLobbyCode}";
        var scale = 0.7f; // Smaller scale for corner display
        
        // Create background rectangle
        var textSize = Font.MeasureString(lobbyText) * scale;
        var backgroundRect = new Rectangle(10, 10, (int)textSize.X + 20, (int)textSize.Y + 10);
        
        // Draw semi-transparent background
        var backgroundTexture = new Texture2D(GraphicsDevice, 1, 1);
        backgroundTexture.SetData([Color.Black]);
        SpriteBatch.Draw(backgroundTexture, backgroundRect, Color.Black * 0.8f);
        
        // Draw border
        var borderRect = new Rectangle(backgroundRect.X - 1, backgroundRect.Y - 1, backgroundRect.Width + 2, backgroundRect.Height + 2);
        var borderTexture = new Texture2D(GraphicsDevice, 1, 1);
        borderTexture.SetData([Color.White]);
        SpriteBatch.Draw(borderTexture, borderRect, Color.White);
        
        // Draw lobby code text - smaller and compact
        var textPosition = new Vector2(backgroundRect.X + 10, backgroundRect.Y + 5);
        SpriteBatch.DrawString(Font, lobbyText, textPosition, Color.Yellow, 0f, Vector2.Zero, scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
        
        // Clean up textures
        backgroundTexture.Dispose();
        borderTexture.Dispose();
    }
    
    public override void Dispose()
    {
        base.Dispose();
    }
    #endregion
}

// Data structure for pending join requests
public struct PendingJoinRequest
{
    public JoinPacket JoinPacket { get; set; }
    public NetPeer Peer { get; set; }
    
    public PendingJoinRequest(JoinPacket joinPacket, NetPeer peer)
    {
        JoinPacket = joinPacket;
        Peer = peer;
    }
}