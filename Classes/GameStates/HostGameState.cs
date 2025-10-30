using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using CasinoRoyale.Classes.GameObjects;
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

public class HostGameState : GameState
{
    protected GameWorld GameWorld { get; private set; }
    protected PlayableCharacter LocalPlayer { get; set; }
    protected Texture2D PlayerTexture { get; set; }
    protected Vector2 PlayerOrigin { get; set; }

    private float _serverUpdateTimer = 0f;
    private float _platformUpdateTimer = 0f; // Separate timer for platform updates
    private float _gameTime = 0f;
    private readonly uint _maxPlayers = 6;
    private readonly PlayerIDs _playerIDs;
    private int _nextSpawnOffset = 0;
    private string _currentLobbyCode;

    private readonly string _username = "HAZZA";

    private LiteNetRelayManager _relayManager;
    private AsyncPacketProcessor _packetProcessor;
    private Dictionary<uint, PlayableCharacter> _players;
    private RelayGameEventListener _gameEventListener;
    private AsyncPacketProcessor.INetworkEventBus _eventBus;

    public HostGameState(Game game, IGameStateManager stateManager)
        : base(game, stateManager)
    {
        _playerIDs = new PlayerIDs(_maxPlayers);
    }

    public override void Initialize()
    {
        base.Initialize();

        NetworkManager.Instance.Initialize(true);

        _players = new Dictionary<uint, PlayableCharacter>();
    }

    private void InitializeNetworking()
    {
        try
        {
            Logger.LogNetwork("HOST", "Initializing network components...");

            _packetProcessor = new AsyncPacketProcessor();
            _eventBus = new AsyncPacketProcessor.NetworkEventBus();
            _packetProcessor.AttachEventBus(_eventBus);

            _gameEventListener = new RelayGameEventListener(
                _packetProcessor,
                _players,
                LocalPlayer,
                onClientConnected: () =>
                {
                    try
                    {
                        BroadcastWorldInit();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error broadcasting world init: {ex.Message}");
                    }
                }
            );

            // Read relay endpoint from app.properties (fallback to localhost if missing)
            string relayAddress = GameProperties.get("relay.server.address", "127.0.0.1");
            int relayPort = 9051;
            {
                string portString = GameProperties.get("relay.server.port", "9051"); // UDP
                if (!int.TryParse(portString, out relayPort))
                {
                    relayPort = 9051;
                }
            }
            Logger.LogNetwork("HOST", $"Using relay endpoint {relayAddress}:{relayPort}");
            _relayManager = new LiteNetRelayManager(_gameEventListener, relayAddress, relayPort);
            // Forward raw game packets from relay to our async packet processor
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
                    Logger.Error($"Error enqueueing relay packet: {ex.Message}");
                }
            };
            _relayManager.OnError += (err) => Logger.Error($"Relay error: {err}");
            _relayManager.OnLobbyCodeReceived += (code) => SetLobbyCode(code);

            WireNetworkManagerEvents();

            WirePacketProcessorEvents();

            // Subscribe world to item-related packets
            if (GameWorld != null)
            {
                _eventBus.Subscribe<ItemUpdatePacket>(GameWorld);
                _eventBus.Subscribe<ItemRemovedPacket>(GameWorld);
            }

            _ = _relayManager
                .StartAsHostAsync()
                .ContinueWith(task =>
                {
                    if (task.Result)
                    {
                        Logger.LogNetwork("HOST", "Successfully started as host");
                        SetLobbyCode(_relayManager.CurrentLobbyCode);
                    }
                    else
                    {
                        Logger.Error("Failed to start as host");
                    }
                });
        }
        catch (Exception ex)
        {
            Logger.Error($"Error initializing network components: {ex.Message}");
            Logger.Error($"Stack trace: {ex.StackTrace}");
        }
    }

    private void BroadcastWorldInit()
    {
        if (GameWorld == null) return;
        var packet = new GameWorldInitPacket
        {
            targetClientId = 0, // broadcast; clients do not filter this currently
            gameArea = GameWorld.GameArea,
            worldVersion = 1,
            playerStates = BuildPlayerStatesSnapshot(),
            itemStates = GameWorld.GetItemStates(),
            gridTiles = GameWorld.GetGridTileStates()
        };
        SendPacketToRelay(packet);
        Logger.LogNetwork("HOST", "Broadcasted GameWorldInitPacket to lobby");
    }

    private PlayerState[] BuildPlayerStatesSnapshot()
    {
        try
        {
            var list = new List<PlayerState>();
            if (LocalPlayer != null)
            {
                list.Add(LocalPlayer.GetPlayerState());
            }
            if (_players != null)
            {
                foreach (var kv in _players)
                {
                    var p = kv.Value;
                    if (p != null && (LocalPlayer == null || p.GetID() != LocalPlayer.GetID()))
                    {
                        list.Add(p.GetPlayerState());
                    }
                }
            }
            return list.ToArray();
        }
        catch
        {
            return Array.Empty<PlayerState>();
        }
    }

    private void WireNetworkManagerEvents()
    {
        NetworkManager.Instance.HostBroadcastPacket += (packet) =>
        {
            if (_relayManager?.RelayServerPeer?.ConnectionState == ConnectionState.Connected)
            {
                SendPacketToRelay(packet);
            }
        };

        NetworkManager.Instance.HostSendToClient += (packet, clientId) =>
        {
            if (_relayManager?.RelayServerPeer?.ConnectionState == ConnectionState.Connected)
            {
                SendPacketToRelay(packet);
            }
        };

        NetworkManager.Instance.HostBroadcastObjectChangeRequested += (
            objectId,
            propertyName,
            newValue
        ) =>
        {
            if (_relayManager?.RelayServerPeer?.ConnectionState == ConnectionState.Connected)
            {
                SendObjectChangeBroadcast(objectId, propertyName, newValue);
            }
        };

        NetworkManager.Instance.ClientSpawnRequest += (prefabKey, position, initialState) =>
        {
            if (_relayManager?.RelayServerPeer?.ConnectionState == ConnectionState.Connected)
            {
                HandleSpawnRequest(prefabKey, position, initialState);
            }
        };

        NetworkManager.Instance.HostSpawnBroadcast += (
            objectId,
            prefabKey,
            position,
            initialState
        ) =>
        {
            if (_relayManager?.RelayServerPeer?.ConnectionState == ConnectionState.Connected)
            {
                SendSpawnBroadcast(objectId, prefabKey, position, initialState);
            }
        };
    }

    private void SendObjectChangeBroadcast(
        uint objectId,
        string propertyName,
        INetSerializable newValue
    )
    {
        try
        {
            var updatePacket = new NetworkObjectUpdatePacket
            {
                objectId = objectId,
                coords = GetCoordsFromValue(newValue),
                velocity = GetVelocityFromValue(newValue),
            };

            SendPacketToRelay(updatePacket);
            Logger.LogNetwork(
                "EVENT_NETWORK",
                $"Broadcast object change: id={objectId}, property={propertyName}"
            );
        }
        catch (Exception ex)
        {
            Logger.Error($"Error sending object change broadcast: {ex.Message}");
        }
    }

    private Vector2 GetCoordsFromValue(INetSerializable value)
    {
        if (value is GameEntityStatePacket statePacket)
        {
            return statePacket.state.coords;
        }
        return Vector2.Zero;
    }

    private Vector2 GetVelocityFromValue(INetSerializable value)
    {
        if (value is GameEntityStatePacket statePacket)
        {
            return statePacket.state.velocity;
        }
        return Vector2.Zero;
    }

    private void HandleSpawnRequest(
        string prefabKey,
        Vector2 position,
        INetSerializable initialState
    )
    {
        uint newObjectId = GetNextObjectId();

        NetworkManager.Instance.HostApproveAndBroadcastSpawn(
            newObjectId,
            prefabKey,
            position,
            initialState
        );
        Logger.LogNetwork("EVENT_NETWORK", $"Approved spawn: id={newObjectId}, prefab={prefabKey}");
    }

    private void SendSpawnBroadcast(
        uint objectId,
        string prefabKey,
        Vector2 position,
        INetSerializable initialState
    )
    {
        try
        {
            var spawnPacket = new NetworkObjectSpawnPacket
            {
                objectId = objectId,
                prefabKey = prefabKey,
                position = position,
            };

            SendPacketToRelay(spawnPacket);
            Logger.LogNetwork(
                "EVENT_NETWORK",
                $"Broadcast spawn: id={objectId}, prefab={prefabKey}"
            );
        }
        catch (Exception ex)
        {
            Logger.Error($"Error sending spawn broadcast: {ex.Message}");
        }
    }

    private uint _nextObjectId = 100; // Start at 100 to avoid conflicts with player IDs

    private uint GetNextObjectId()
    {
        return _nextObjectId++;
    }

    private void WirePacketProcessorEvents()
    {
        _packetProcessor.NetworkObjectSpawnReceived += OnNetworkObjectSpawnReceived;
        _packetProcessor.NetworkObjectUpdateReceived += OnNetworkObjectUpdateReceived;
        _packetProcessor.JoinRequestReceived += (sender, args) =>
        {
            try
            {
                var join = args.Packet;
                Logger.LogNetwork("HOST", $"JoinPacket received with nonce {join.clientNonce}");
                if (_playerIDs == null || !_playerIDs.RoomForNextPlayer())
                {
                    Logger.Error("HOST: No room for next player");
                    return;
                }
                uint newId = _playerIDs.GetNextID();

                var accept = new JoinAcceptPacket
                {
                    targetClientId = newId,
                    clientNonce = join.clientNonce,
                    gameArea = GameWorld != null ? GameWorld.GameArea : Rectangle.Empty,
                };
                SendPacketToRelay(accept);
                Logger.LogNetwork("HOST", $"JoinAccept sent for nonce {join.clientNonce} with id {newId}");
            }
            catch (Exception ex)
            {
                Logger.Error($"HOST: error handling join request: {ex.Message}");
            }
        };
    }

    private void OnNetworkObjectSpawnReceived(
        object sender,
        PacketReceivedEventArgs<NetworkObjectSpawnPacket> e
    )
    {
        Logger.LogNetwork(
            "EVENT_NETWORK",
            $"Received spawn: id={e.Packet.objectId}, prefab={e.Packet.prefabKey}"
        );
    }

    private void OnNetworkObjectUpdateReceived(
        object sender,
        PacketReceivedEventArgs<NetworkObjectUpdatePacket> e
    )
    {
        var packet = e.Packet;
        Logger.LogNetwork("EVENT_NETWORK", $"Received update: id={packet.objectId}");
    }

    private void SendPacketToRelay(INetSerializable packet)
    {
        try
        {
            if (packet == null || _relayManager?.RelayServerPeer == null)
                return;

            var writer = new NetDataWriter();
            // IMPORTANT: Use NetPacketProcessor to include type info so receiver can deserialize
            if (!TryWriteWithProcessor(writer, packet))
            {
                // Fallback: raw serialize (may not be decodable by ReadAllPackets)
                Logger.Warning(
                    $"Falling back to raw serialize for packet type {packet.GetType().Name}"
                );
                packet.Serialize(writer);
            }
            _relayManager.RelayServerPeer.Send(writer, DeliveryMethod.ReliableOrdered);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error sending packet to relay: {ex.Message}");
        }
    }

    private bool TryWriteWithProcessor(NetDataWriter writer, INetSerializable packet)
    {
        if (_packetProcessor == null)
            return false;
        switch (packet)
        {
            case NetworkObjectUpdatePacket p:
                _packetProcessor.PacketProcessor.Write(writer, p);
                return true;
            case NetworkObjectSpawnPacket p2:
                _packetProcessor.PacketProcessor.Write(writer, p2);
                return true;
            case ItemUpdatePacket p3:
                _packetProcessor.PacketProcessor.Write(writer, p3);
                return true;
            case ItemRemovedPacket p4:
                _packetProcessor.PacketProcessor.Write(writer, p4);
                return true;
            case PlayerReceiveUpdatePacket p5:
                _packetProcessor.PacketProcessor.Write(writer, p5);
                return true;
            case PlayerJoinedGamePacket p6:
                _packetProcessor.PacketProcessor.Write(writer, p6);
                return true;
            case PlayerLeftGamePacket p7:
                _packetProcessor.PacketProcessor.Write(writer, p7);
                return true;
            case GameWorldInitPacket p8:
                _packetProcessor.PacketProcessor.Write(writer, p8);
                return true;
            case JoinAcceptPacket p9:
                _packetProcessor.PacketProcessor.Write(writer, p9);
                return true;
            default:
                return false;
        }
    }

    public override void LoadContent()
    {
        try
        {
            base.LoadContent();

            GameWorld = new GameWorld(
                GameProperties,
                Content,
                SpriteBatch,
                MainCamera,
                Resolution.ratio
            );

            string playerImageName = GameProperties.get("player.image", "ball");
            PlayerTexture = Content.Load<Texture2D>(playerImageName);

            GameWorld.InitializeGameWorld(PlayerOrigin);

            if (PlayerTexture != null)
            {
                PlayerOrigin = GameWorld.CalculatePlayerOrigin(PlayerTexture.Height);
            }

            CreateLocalPlayer();

            if (LocalPlayer != null)
            {
                _players[LocalPlayer.GetID()] = LocalPlayer;
                // Reserve host ID (0) in allocator so clients start at 1+
                if (_playerIDs != null)
                {
                    _playerIDs.GetNextID();
                }
            }

            InitializeCamera();

            InitializeNetworking();
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

        _relayManager?.PollEvents();
        _packetProcessor?.ProcessMainThreadEvents();

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _serverUpdateTimer += deltaTime;
        _platformUpdateTimer += deltaTime;
        this._gameTime += deltaTime;

        if (LocalPlayer != null)
        {
            LocalPlayer.TryMovePlayer(KeyboardState, PreviousKeyboardState, deltaTime, GameWorld);
            MainCamera.MoveToFollowPlayer(LocalPlayer);
        }

        if (LocalPlayer == null || GameWorld == null)
            return;

        GameWorld.Update(deltaTime, NetworkManager.Instance.IsHost);
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkMagenta);

        if (SpriteBatch == null || MainCamera == null || GameWorld == null)
            return;

        Vector2 ratio = Resolution.ratio;
        MainCamera.ApplyRatio(ratio);

        SpriteBatch.Begin();

        GameWorld.DrawGameObjects();

        SpriteBatch.DrawEntity(MainCamera, LocalPlayer);

        if (!string.IsNullOrEmpty(_currentLobbyCode) && Font != null)
        {
            DrawLobbyCode();
        }

        SpriteBatch.End();
    }

    protected virtual void InitializeCamera()
    {
        if (LocalPlayer == null)
            return;
        MainCamera.InitMainCamera(Window, LocalPlayer);
    }

    private void CreateLocalPlayer()
    {
        if (PlayerTexture == null)
            return;

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
            new Rectangle(
                PlayerOrigin.ToPoint(),
                new Point(PlayerTexture.Bounds.Width, PlayerTexture.Bounds.Height)
            ),
            true
        );
        LocalPlayer.MarkAsNewPlayer();

        LocalPlayer.InitializeTargets();
    }

    #region Network Event Handlers

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

        Vector2 clientSpawnPosition = PlayerOrigin + new Vector2(_nextSpawnOffset * 200, 0);

        _nextSpawnOffset++;

        return clientSpawnPosition;
    }

    private void DrawLobbyCode()
    {
        if (Font == null || GraphicsDevice == null)
        {
            Logger.Error("Font or GraphicsDevice is null in DrawLobbyCode()!");
            return;
        }

        var lobbyText = $"LOBBY: {_currentLobbyCode}";
        var scale = 0.7f;

        var textSize = Font.MeasureString(lobbyText) * scale;
        var backgroundRect = new Rectangle(10, 10, (int)textSize.X + 20, (int)textSize.Y + 10);

        var backgroundTexture = new Texture2D(GraphicsDevice, 1, 1);
        backgroundTexture.SetData([Color.Black]);
        SpriteBatch.Draw(backgroundTexture, backgroundRect, Color.Black * 0.8f);

        var borderRect = new Rectangle(
            backgroundRect.X - 1,
            backgroundRect.Y - 1,
            backgroundRect.Width + 2,
            backgroundRect.Height + 2
        );
        var borderTexture = new Texture2D(GraphicsDevice, 1, 1);
        borderTexture.SetData([Color.White]);
        SpriteBatch.Draw(borderTexture, borderRect, Color.White);

        var textPosition = new Vector2(backgroundRect.X + 10, backgroundRect.Y + 5);
        SpriteBatch.DrawString(
            Font,
            lobbyText,
            textPosition,
            Color.Yellow,
            0f,
            Vector2.Zero,
            scale,
            Microsoft.Xna.Framework.Graphics.SpriteEffects.None,
            0f
        );

        backgroundTexture.Dispose();
        borderTexture.Dispose();
    }

    public override void Dispose()
    {
        _relayManager?.Dispose();
        _packetProcessor?.Dispose();

        base.Dispose();
    }
    #endregion
}

public class RelayGameEventListener : LiteNetLib.INetEventListener
{
    private readonly AsyncPacketProcessor _packetProcessor;
    private readonly Dictionary<uint, PlayableCharacter> _players;
    private readonly PlayableCharacter _localPlayer;
    private readonly Action _onClientConnected;

    public RelayGameEventListener(
        AsyncPacketProcessor packetProcessor,
        Dictionary<uint, PlayableCharacter> players,
        PlayableCharacter localPlayer,
        Action onClientConnected = null
    )
    {
        _packetProcessor = packetProcessor;
        _players = players;
        _localPlayer = localPlayer;
        _onClientConnected = onClientConnected;
    }

    public void OnPeerConnected(LiteNetLib.NetPeer peer)
    {
        Logger.LogNetwork("GAME_EVENT_LISTENER", $"Peer connected: {peer.Id}");
        _onClientConnected?.Invoke();
    }

    public void OnPeerDisconnected(
        LiteNetLib.NetPeer peer,
        LiteNetLib.DisconnectInfo disconnectInfo
    )
    {
        Logger.LogNetwork(
            "GAME_EVENT_LISTENER",
            $"Peer disconnected: {peer.Id}, Reason: {disconnectInfo.Reason}"
        );
    }

    public void OnNetworkError(
        System.Net.IPEndPoint endPoint,
        System.Net.Sockets.SocketError socketError
    )
    {
        Logger.Error($"Network error: {socketError}");
    }

    public void OnNetworkReceive(
        NetPeer peer,
        NetPacketReader reader,
        byte channel,
        DeliveryMethod deliveryMethod
    )
    {
        _packetProcessor.EnqueuePacket(peer, reader, channel, deliveryMethod);
    }

    public void OnNetworkReceiveUnconnected(
        IPEndPoint remoteEndPoint,
        NetPacketReader reader,
        UnconnectedMessageType messageType
    ) { }

    public void OnNetworkLatencyUpdate(LiteNetLib.NetPeer peer, int latency) { }

    public void OnConnectionRequest(LiteNetLib.ConnectionRequest request)
    {
        request.Accept();
    }
}

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
