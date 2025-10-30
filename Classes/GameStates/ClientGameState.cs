using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CasinoRoyale.Classes.GameObjects.Platforms;
using CasinoRoyale.Classes.GameObjects.Player;
using CasinoRoyale.Classes.GameStates.Interfaces;
using CasinoRoyale.Classes.GameSystems;
using CasinoRoyale.Classes.MonogameMethodExtensions;
using CasinoRoyale.Classes.Networking;
using CasinoRoyale.Classes.Networking.Players;
using CasinoRoyale.Utils;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CasinoRoyale.Classes.GameStates;

public partial class ClientGameState(Game game, IGameStateManager stateManager, string lobbyCode)
    : GameState(game, stateManager)
{
    protected GameWorld GameWorld { get; private set; }
    protected PlayableCharacter LocalPlayer { get; set; }
    protected Texture2D PlayerTexture { get; set; }
    protected Vector2 PlayerOrigin { get; set; }

    private readonly List<PlayableCharacter> _otherPlayers = [];
    private bool _connected = false;
    private readonly string _lobbyCode = lobbyCode;
    private float _gameTime = 0f;

    private uint _clientId = 0;
    private string _clientNonce = string.Empty;

    private float _connectionTimeout = 10f;
    private bool _connectionAttempted = false;
    private bool _contentLoaded = false;
    private bool _disposed = false;
    private bool _worldReady = false;
    private bool _localPlayerInitialized = false;

    private readonly PlayerIDs _playerIDs = new(6);

    private LiteNetRelayManager _relayManager;
    private AsyncPacketProcessor _packetProcessor;
    private INetEventListener _gameEventListener;
    private AsyncPacketProcessor.INetworkEventBus _eventBus;

    public override void Initialize()
    {
        base.Initialize();

        Logger.LogNetwork(
            "CLIENT",
            $"ClientGameState.Initialize() called with lobby code: {_lobbyCode}"
        );

        NetworkManager.Instance.Initialize(false);

        _packetProcessor = new AsyncPacketProcessor();
        _eventBus = new AsyncPacketProcessor.NetworkEventBus();
        _packetProcessor.AttachEventBus(_eventBus);

        _gameEventListener = new ClientGameEventListener(
            onConnected: () =>
            {
                _connected = true;
                Logger.LogNetwork("CLIENT", "Connected to relay lobby; loading content");
                LoadGameContent();
                TrySendJoinPacket();
                if (GameWorld != null)
                {
                    _eventBus.Subscribe<ItemUpdatePacket>(GameWorld);
                    _eventBus.Subscribe<ItemRemovedPacket>(GameWorld);
                    _eventBus.Subscribe<GameWorldInitPacket>(GameWorld);
                    _eventBus.Subscribe<JoinAcceptPacket>(GameWorld);
                }
            },
            onDisconnected: (reason) =>
            {
                _connected = false;
                Logger.Warning($"Disconnected from relay: {reason}");
            }
        );

        // Read relay endpoint
        string relayAddress = GameProperties.get("relay.server.address", "127.0.0.1");
        int relayPort = int.TryParse(GameProperties.get("relay.server.port", "9051"), out var rp)
            ? rp
            : 9051;

        _relayManager = new LiteNetRelayManager(_gameEventListener, relayAddress, relayPort);
        _relayManager.OnGamePacketReceived += (bytes) =>
        {
            try
            {
                _packetProcessor?.EnqueuePacket(
                    _relayManager.RelayServerPeer,
                    bytes,
                    0,
                    DeliveryMethod.ReliableOrdered
                );
            }
            catch (Exception ex)
            {
                Logger.Error($"CLIENT: Error enqueueing relay packet: {ex.Message}");
            }
        };
        _relayManager.OnError += (err) => Logger.Error($"CLIENT Relay error: {err}");

        _ = _relayManager
            .JoinAsClientAsync(_lobbyCode)
            .ContinueWith(t =>
            {
                if (!t.Result)
                {
                    _connectionAttempted = true;
                    Logger.Error("CLIENT: Failed to start client join flow");
                }
            });

        // Subscribe world to item-related packets once created after connect
        // Also subscribe client state to init/accept/player updates
        _eventBus.Subscribe<GameWorldInitPacket>(this);
        _eventBus.Subscribe<JoinAcceptPacket>(this);
        _eventBus.Subscribe<PlayerReceiveUpdatePacket>(this);
        _eventBus.Subscribe<NetworkObjectUpdatePacket>(this);
    }

    public override void LoadContent()
    {
        try
        {
            base.LoadContent();

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

        _relayManager?.PollEvents();
        _packetProcessor?.ProcessMainThreadEvents();

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        this._gameTime += deltaTime;

        // Defer local player initialization until world is ready (game area set)
        if (
            _connected
            && _contentLoaded
            && !_localPlayerInitialized
            && GameWorld != null
            && GameWorld.GameArea != Rectangle.Empty
            && _clientId != 0
        )
        {
            InitializeLocalPlayerAfterContent();
            InitializeCamera();
            _localPlayerInitialized = true;
        }

        if (!_connected || !_contentLoaded || LocalPlayer == null)
            return;

        LocalPlayer.TryMovePlayer(KeyboardState, PreviousKeyboardState, deltaTime, GameWorld);
        MainCamera.MoveToFollowPlayer(LocalPlayer);

        if (GameWorld == null)
            return;

        GameWorld.Update(deltaTime, NetworkManager.Instance.IsHost);

        foreach (var otherPlayer in _otherPlayers)
        {
            otherPlayer.ProcessBufferedStates(deltaTime);
            otherPlayer.UpdateInterpolation(deltaTime);
        }
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkMagenta);

        if (!_connected || !_contentLoaded)
        {
            if (SpriteBatch != null && MainCamera != null)
            {
                SpriteBatch.Begin();
                string statusText;
                if (!_connected)
                {
                    statusText = _connectionAttempted
                        ? $"Connection failed after {_connectionTimeout}s"
                        : $"Connecting to lobby: {_lobbyCode}...";
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

        if (
            SpriteBatch == null
            || MainCamera == null
            || !_contentLoaded
            || GameWorld == null
            || LocalPlayer == null
        )
            return;

        Vector2 ratio = Resolution.ratio;
        MainCamera.ApplyRatio(ratio);

        SpriteBatch.Begin();

        GameWorld.DrawGameObjects();

        SpriteBatch.DrawEntity(MainCamera, LocalPlayer);

        foreach (var player in _otherPlayers)
        {
            SpriteBatch.DrawEntity(MainCamera, player);
        }

        SpriteBatch.End();
    }

    protected virtual void InitializeCamera()
    {
        if (LocalPlayer == null)
            return;
        MainCamera.InitMainCamera(Window, LocalPlayer);
    }

    private void LoadGameContent()
    {
        if (_contentLoaded)
            return;

        try
        {
            Logger.LogNetwork("CLIENT", "Loading game content after successful connection...");

            GameWorld = new GameWorld(
                GameProperties,
                Content,
                SpriteBatch,
                MainCamera,
                Resolution.ratio
            );

            _contentLoaded = true;
            Logger.LogNetwork("CLIENT", "Game content loaded successfully");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error loading game content: {ex.Message}");
            Logger.Error($"Stack trace: {ex.StackTrace}");
        }
    }

    public override void Dispose()
    {
        _disposed = true;
        _relayManager?.Dispose();
        _packetProcessor?.Dispose();

        base.Dispose();
    }
}

// Helper to build the local player after content and world are ready
partial class ClientGameState
{
    private void InitializeLocalPlayerAfterContent()
    {
        try
        {
            if (PlayerTexture == null)
            {
                string playerImageName = GameProperties.get("player.image", "ball");
                PlayerTexture = Content.Load<Texture2D>(playerImageName);
            }

            if (GameWorld != null && PlayerTexture != null)
            {
                if (PlayerOrigin == Vector2.Zero)
                {
                    PlayerOrigin = GameWorld.CalculatePlayerOrigin(PlayerTexture.Height);
                }

                LocalPlayer = new PlayableCharacter(
                    _clientId,
                    "CLIENT",
                    PlayerTexture,
                    PlayerOrigin,
                    Vector2.Zero,
                    GetFloatProperty("playerMass", 5.0f),
                    GetFloatProperty("playerInitialJumpVelocity", 240f),
                    GetFloatProperty("playerStandardSpeed", 240f),
                    new Rectangle(
                        PlayerOrigin.ToPoint(),
                        new Point(PlayerTexture.Bounds.Width, PlayerTexture.Bounds.Height)
                    ),
                    false
                );

                LocalPlayer.InitializeTargets();
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"CLIENT: Error initializing local player: {ex.Message}");
        }
    }
}

public class ClientGameEventListener : INetEventListener
{
    private readonly Action _onConnected;
    private readonly Action<string> _onDisconnected;

    public ClientGameEventListener(Action onConnected, Action<string> onDisconnected)
    {
        _onConnected = onConnected;
        _onDisconnected = onDisconnected;
    }

    public void OnPeerConnected(NetPeer peer)
    {
        _onConnected?.Invoke();
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _onDisconnected?.Invoke(disconnectInfo.Reason.ToString());
    }

    public void OnNetworkError(
        System.Net.IPEndPoint endPoint,
        System.Net.Sockets.SocketError socketError
    )
    {
        Logger.Error($"CLIENT network error: {socketError}");
    }

    public void OnNetworkReceive(
        NetPeer peer,
        NetPacketReader reader,
        byte channel,
        DeliveryMethod deliveryMethod
    )
    {
        // relay listener already forwards bytes to OnGamePacketReceived; nothing to do here
    }

    public void OnNetworkReceiveUnconnected(
        System.Net.IPEndPoint remoteEndPoint,
        NetPacketReader reader,
        UnconnectedMessageType messageType
    ) { }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

    public void OnConnectionRequest(ConnectionRequest request) { }
}

// Observers for world init, join accept, and player updates
partial class ClientGameState
    : AsyncPacketProcessor.INetworkObserver<GameWorldInitPacket>,
        AsyncPacketProcessor.INetworkObserver<JoinAcceptPacket>,
        AsyncPacketProcessor.INetworkObserver<PlayerReceiveUpdatePacket>,
        AsyncPacketProcessor.INetworkObserver<NetworkObjectUpdatePacket>
{
    public void OnPacket(GameWorldInitPacket packet)
    {
        // Spawn remote players from initial snapshot (ignore self once id is known)
        if (packet.playerStates != null && PlayerTexture != null)
        {
            foreach (var ps in packet.playerStates)
            {
                if (ps.pid == _clientId) continue;
                var existing = _otherPlayers.FirstOrDefault(p => p.GetID() == ps.pid);
                if (existing == null)
                {
                    var pos = ps.ges.coords;
                    var newPlayer = new PlayableCharacter(
                        ps.pid,
                        ps.username ?? $"P{ps.pid}",
                        PlayerTexture,
                        pos,
                        ps.ges.velocity,
                        ps.ges.mass,
                        ps.initialJumpVelocity,
                        ps.maxRunSpeed,
                        new Rectangle(pos.ToPoint(), new Point(PlayerTexture.Bounds.Width, PlayerTexture.Bounds.Height)),
                        ps.ges.awake
                    );
                    newPlayer.InitializeTargets();
                    _otherPlayers.Add(newPlayer);
                }
            }
        }
        _worldReady = true;
    }

    public void OnPacket(JoinAcceptPacket packet)
    {
        if (!string.IsNullOrEmpty(packet.clientNonce) && packet.clientNonce == _clientNonce)
        {
            Logger.LogNetwork(
                "CLIENT",
                $"JoinAccept matched nonce {_clientNonce}, assigned id {packet.targetClientId}"
            );
            _clientId = packet.targetClientId;
            _worldReady = true;
        }
    }

    public void OnPacket(PlayerReceiveUpdatePacket packet)
    {
        if (packet?.playerStates == null || !_connected || !_contentLoaded)
            return;

        foreach (var ps in packet.playerStates)
        {
            if (ps.pid == _clientId)
                continue; // ignore self updates

            // Find or create other player
            var existing = _otherPlayers.FirstOrDefault(p => p.GetID() == ps.pid);
            if (existing == null && PlayerTexture != null)
            {
                var startPos = ps.ges.coords;
                var newPlayer = new PlayableCharacter(
                    ps.pid,
                    ps.username ?? $"P{ps.pid}",
                    PlayerTexture,
                    startPos,
                    ps.ges.velocity,
                    ps.ges.mass,
                    ps.initialJumpVelocity,
                    ps.maxRunSpeed,
                    new Rectangle(
                        startPos.ToPoint(),
                        new Point(PlayerTexture.Bounds.Width, PlayerTexture.Bounds.Height)
                    ),
                    false
                );
                _otherPlayers.Add(newPlayer);
                existing = newPlayer;
            }

            // If existing != null, we could update state here when explicit setters are available.
        }
    }
}

partial class ClientGameState
{
    public void OnPacket(NetworkObjectUpdatePacket packet)
    {
        if (!_connected || !_contentLoaded)
            return;

        var objectId = packet.objectId;
        if (LocalPlayer != null && objectId == LocalPlayer.GetID())
            return; // ignore self

        var existing = _otherPlayers.FirstOrDefault(p => p.GetID() == objectId);
        if (existing == null && PlayerTexture != null)
        {
            var pos = packet.coords;
            var newPlayer = new PlayableCharacter(
                objectId,
                $"P{objectId}",
                PlayerTexture,
                pos,
                packet.velocity,
                GetFloatProperty("playerMass", 5.0f),
                GetFloatProperty("playerInitialJumpVelocity", 240f),
                GetFloatProperty("playerStandardSpeed", 240f),
                new Rectangle(pos.ToPoint(), new Point(PlayerTexture.Bounds.Width, PlayerTexture.Bounds.Height)),
                false
            );
            newPlayer.InitializeTargets();
            _otherPlayers.Add(newPlayer);
            existing = newPlayer;
        }

        if (existing != null)
        {
            existing.AddBufferedState(packet.coords, packet.velocity, _gameTime);
        }
    }
}

// Client-side send helpers
public partial class ClientGameState
{
    private void TrySendJoinPacket()
    {
        if (_relayManager?.RelayServerPeer == null || _packetProcessor == null)
            return;

        _clientNonce = Guid.NewGuid().ToString("N");
        Logger.LogNetwork("CLIENT", $"Sending JoinPacket with nonce {_clientNonce}");
        var jp = new JoinPacket
        {
            username = GameProperties.get("player.username", "CLIENT"),
            playerMass = GetFloatProperty("playerMass", 5.0f),
            playerInitialJumpVelocity = GetFloatProperty("playerInitialJumpVelocity", 240f),
            playerStandardSpeed = GetFloatProperty("playerStandardSpeed", 240f),
            clientNonce = _clientNonce,
        };

        var writer = new NetDataWriter();
        _packetProcessor.PacketProcessor.Write(writer, jp);
        _relayManager.RelayServerPeer.Send(writer, DeliveryMethod.ReliableOrdered);
    }
}
