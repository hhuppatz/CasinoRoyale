using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using CasinoRoyale.GameObjects;
using CasinoRoyale.Players.Common;
using CasinoRoyale.Players.Common.Networking;
using CasinoRoyale.Extensions;

namespace CasinoRoyale.Players.Host
{
    public class Host : Game, INetEventListener
{
    // Server fields
    private readonly int PORT_NUM = 12345;
    private NetManager server;
    private NetDataWriter writer;
    private NetPacketProcessor packetProcessor;
    private Dictionary<uint, NetworkPlayer> networkPlayers = [];
    private float serverUpdatetimer = 0f;
    private readonly uint MAX_PLAYERS = 6;
    private PlayerIDs playerIDs;

    // Game fields
    private readonly Properties _gameProperties;
    private readonly MainCamera _mainCamera = MainCamera.Instance;
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Rectangle gameArea;
    private List<Platform> platforms;
    private List<CasinoMachine> casinoMachines;
    private CasinoMachineFactory casinoMachineFactory;

    // Player loading info fields
    private Texture2D playerTex;
    private Vector2 playerOrigin;

    // Host player
    private string username = "HAZZA";
    private PlayableCharacter player1;

    public Host()
    {
        _graphics = new GraphicsDeviceManager(this);
        Window.AllowUserResizing = false;

        _gameProperties = new Properties("app.properties");
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    // For loading non-graphic related content
    protected override void Initialize()
    {
        // Initialise server
        writer = new NetDataWriter();
        packetProcessor = new NetPacketProcessor();
        networkPlayers = [];

        packetProcessor.RegisterNestedType((w, v) => w.Put(v), reader => reader.GetVector2());
        packetProcessor.RegisterNestedType((w, v) => w.Put(v), reader => reader.GetGES());
        packetProcessor.RegisterNestedType((u, x) => u.Put(x), reader => reader.GetRectangle());
        packetProcessor.RegisterNestedType<PlayerState>();
        packetProcessor.RegisterNestedType<PlatformState>();
        packetProcessor.RegisterNestedType<CasinoMachineState>();

        packetProcessor.SubscribeReusable<JoinPacket, NetPeer>(OnJoinReceived);
        packetProcessor.SubscribeReusable<PlayerSendUpdatePacket, NetPeer>(OnPlayerUpdate);

        Console.WriteLine("Starting server");
        server = new NetManager(this) {
            AutoRecycle = true,
        };

        playerIDs = new PlayerIDs(MAX_PLAYERS);
        server.Start(PORT_NUM);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        gameArea = new Rectangle(int.Parse(_gameProperties.get("gameArea.x")),
                                 int.Parse(_gameProperties.get("gameArea.y")),
                                 int.Parse(_gameProperties.get("gameArea.width")),
                                 int.Parse(_gameProperties.get("gameArea.height")));

        // player initialisation
        playerTex = Content.Load<Texture2D>(_gameProperties.get("player.image"));
        int playerSpawnBuffer = playerTex.Height * 2; // Keep area directly around player free
        playerOrigin = new Vector2(0, gameArea.Y + gameArea.Height - playerSpawnBuffer); // Spawn on the floor, above the buffer zone
        Console.WriteLine($"Player spawn: gameArea={gameArea}, playerSpawnBuffer={playerSpawnBuffer}, playerOrigin={playerOrigin}");

        // game world initialisation
        platforms = PlatformLayout.GenerateStandardRandPlatLayout(Content.Load<Texture2D>(_gameProperties.get("casinoFloor.image.1")),
                                                                gameArea,
                                                                50,
                                                                200,
                                                                50,
                                                                100,
                                                                70,
                                                                playerSpawnBuffer);
        
        // Debug: Print platform hitboxes
        var platformTexture = Content.Load<Texture2D>(_gameProperties.get("casinoFloor.image.1"));
        Console.WriteLine($"Platform texture dimensions: {platformTexture.Width}x{platformTexture.Height}");
        Console.WriteLine($"Generated {platforms.Count} platforms:");
        for (int i = 0; i < Math.Min(5, platforms.Count); i++)
        {
            var platform = platforms[i];
            Console.WriteLine($"Platform {i}: Hitbox={platform.Hitbox}, Coords={platform.GetCoords()}");
        }

        // casino machine generation
        casinoMachines = [];
        casinoMachineFactory = new CasinoMachineFactory(Content.Load<Texture2D>(_gameProperties.get("casinoMachine.image.1")));
        
        casinoMachines = casinoMachineFactory.SpawnCasinoMachines();

        player1 = new PlayableCharacter(
                            playerIDs.GetNextID(),
                            username,
                            playerTex,
                            playerOrigin,
                            Vector2.Zero,
                            new Rectangle(playerOrigin.ToPoint(), new Point(playerTex.Bounds.Width, playerTex.Bounds.Height)),
                            true);
        
        // Debug: Print player hitbox info
        Console.WriteLine($"Player texture dimensions: {playerTex.Width}x{playerTex.Height}");
        Console.WriteLine($"Player hitbox: {player1.Hitbox}, Player coords: {player1.Coords}");

        CasinoRoyale.GameObjects.PhysicsSystem.Initialize(gameArea, platforms, casinoMachines, _gameProperties);

        _mainCamera.InitMainCamera(Window, player1);
    }

    protected override void Update(GameTime gameTime)
    {
        // delta time and current keyboard state
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        serverUpdatetimer += deltaTime;

        // Check for client packets
        server.PollEvents();

        player1.TryMovePlayer(Keyboard.GetState(), deltaTime);
        // Enforce movement rules from physics system
        CasinoRoyale.GameObjects.PhysicsSystem.Instance.EnforceMovementRules(player1, Keyboard.GetState(), deltaTime);

        foreach (KeyValuePair<uint, NetworkPlayer> entry in networkPlayers)
        {
            PlayableCharacter m_other = entry.Value.Player;
            if (m_other.Hitbox.Intersects(player1.Hitbox))
            {
                Console.WriteLine(entry.Value.Player.GetID());
            }
        }

        // Check player states and update according to received messages
        
        // Host player
        PlayerState[] m_OtherPlayerStates = new PlayerState[networkPlayers.Count + 1];
        m_OtherPlayerStates[0] = new PlayerState {
                pid = player1.GetID(),
                username = player1.GetUsername(),
                ges = player1.GetEntityState()
        };

        // Connected players
        for (int i = 1; i < networkPlayers.Count + 1; i++)
        {
            PlayerState m_playerState = networkPlayers.Values.ToArray()[i - 1].Player.GetPlayerState();
            m_OtherPlayerStates[i] = new PlayerState {
                pid = m_playerState.pid,
                username = m_playerState.username,
                ges = m_playerState.ges
            };
        }

        CasinoMachineState[] m_CasinoMachineStates = new CasinoMachineState[casinoMachines.Count];
        for (int i = 0; i < casinoMachines.Count; i++)
        {
            m_CasinoMachineStates[i] = casinoMachines[i].GetState();
        }

        foreach (KeyValuePair<uint, NetworkPlayer> entry in networkPlayers)
        {
            SendPacket(new PlayerReceiveUpdatePacket{ playerStates = m_OtherPlayerStates,
                                                    casinoMachineStates = m_CasinoMachineStates },
                                                    entry.Value.Peer,
                                                    DeliveryMethod.Unreliable);
        }

        // camera logic
        _mainCamera.MoveToFollowPlayer(player1);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkMagenta);

        Vector2 ratio = CasinoRoyale.Utils.Resolution.ratio;
        _mainCamera.ApplyRatio(ratio);

        // drawing sprites
        _spriteBatch.Begin();

        foreach (CasinoMachine m_CasinoMachine in casinoMachines)
        {
            _spriteBatch.Draw(m_CasinoMachine.GetTex(),
                            _mainCamera.TransformToView(m_CasinoMachine.Coords),
                            null,
                            Color.White,
                            0.0f,
                            Vector2.Zero,
                            ratio,
                            0,
                            0);
        }
        
        foreach (Platform platform in platforms)
        {
            // TODO: Need to implement restriction on plat length so is a multiple of the length of the plat tex
            int m_PlatL = (int)platform.GetLCoords().X;
            int m_PlatTexWidth = platform.GetTex().Bounds.Width;
            int m_PlatWidth = platform.GetWidth();
            int i = m_PlatL;
            while (i < m_PlatL + m_PlatWidth)
            {
                _spriteBatch.Draw(platform.GetTex(),
                                _mainCamera.TransformToView(new Vector2(i + m_PlatTexWidth/2, platform.GetCoords().Y)),
                                null,
                                Color.White,
                                0.0f,
                                new Vector2(m_PlatTexWidth/2, m_PlatTexWidth/2),
                                ratio,
                                0,
                                0);
                i += m_PlatTexWidth;
            }
        }

        _spriteBatch.DrawEntity(_mainCamera, player1);
        foreach (KeyValuePair<uint, NetworkPlayer> entry in networkPlayers)
        {
            _spriteBatch.DrawEntity(_mainCamera, entry.Value.Player);
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    // Server functionality methods
    public void OnJoinReceived(JoinPacket packet, NetPeer peer) {
        Console.WriteLine($"Received join from {packet.username} (peer id: {(uint)peer.Id})");

        PlatformState[] m_platformStates = new PlatformState[platforms.Count];
        for (int i = 0; i < platforms.Count; i++)
        {
            m_platformStates[i] = platforms[i].GetState();
        }
        CasinoMachineState[] m_CasinoMachineStates = new CasinoMachineState[casinoMachines.Count];
        for (int i = 0; i < casinoMachines.Count; i++)
        {
           m_CasinoMachineStates[i] = casinoMachines[i].GetState();
        }
        PlayerState[] m_otherPlayerStates = new PlayerState[networkPlayers.Count + 1];
        m_otherPlayerStates[0] = player1.GetPlayerState();
        for (uint i = 1; i < networkPlayers.Count + 1; i++)
        {
            m_otherPlayerStates[i] = networkPlayers[i].Player.GetPlayerState();
        }

        NetworkPlayer m_NewNetPlayer = networkPlayers[(uint)peer.Id] = new NetworkPlayer(
            peer,
            new PlayableCharacter( 
            playerIDs.GetNextID(),
            packet.username,
            playerTex,
            playerOrigin,
            Vector2.Zero,
            new Rectangle(playerOrigin.ToPoint(), new Point(playerTex.Bounds.Width, playerTex.Bounds.Height)),
            true)
        );
        

        SendPacket(new JoinAcceptPacket { playerState = m_NewNetPlayer.Player.GetPlayerState(),
                                        playerHitbox = new Rectangle(m_NewNetPlayer.Player.Coords.ToPoint(), new Point(playerTex.Bounds.Width, playerTex.Bounds.Height)),
                                        playerVelocity = Vector2.Zero,
                                        platformStates = m_platformStates,
                                        otherPlayerStates = m_otherPlayerStates,
                                        casinoMachineStates = m_CasinoMachineStates},
                                        peer,
                                        DeliveryMethod.ReliableOrdered);

        foreach (NetworkPlayer player in networkPlayers.Values) {
            if (player.Player.GetPlayerState().pid != m_NewNetPlayer.Player.GetPlayerState().pid) {
                SendPacket(new PlayerJoinedGamePacket {
                    new_player_username = m_NewNetPlayer.Player.GetUsername(),
                    new_player_state = 
                        new PlayerState {
                            pid = m_NewNetPlayer.Player.GetPlayerState().pid,
                            ges = new GameEntityState {
                                awake = true,
                                coords = m_NewNetPlayer.Player.Coords,
                                velocity = m_NewNetPlayer.Player.Velocity
                            },
                        },
                    new_player_hitbox = new Rectangle(m_NewNetPlayer.Player.Coords.ToPoint(), new Point(playerTex.Bounds.Width, playerTex.Bounds.Height))
                }, player.Peer, DeliveryMethod.ReliableOrdered);
            }
        }
    }

    public void OnPlayerUpdate(PlayerSendUpdatePacket packet, NetPeer peer) {
        networkPlayers[(uint)peer.Id].Player.Coords = packet.coords;
    }

    public void SendPacket<T>(T packet, NetPeer peer, DeliveryMethod deliveryMethod) where T : class, new() {
        if (peer != null) {
            writer.Reset();
            packetProcessor.Write(writer, packet);
            peer.Send(writer, deliveryMethod);
        }
    }

    // INetEventListener methods, implemented through Server class
    void INetEventListener.OnConnectionRequest(ConnectionRequest request)
    {
        Console.WriteLine($"Incoming connection from {request.RemoteEndPoint}");
        request.Accept();
    }

    void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Console.WriteLine("[S] NetworkError: " + socketError);
    }

    void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        
    }

    void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        packetProcessor.ReadAllPackets(reader, peer);
    }

    void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        
    }

    void INetEventListener.OnPeerConnected(NetPeer peer)
    {
        Console.WriteLine("[S] Player connected: " + peer.Id);
    }

    // Need to rework
    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Console.WriteLine($"Player (peer id: {(uint)peer.Id}) left the game");
        if (peer.Tag == null) {
            Console.WriteLine($"Player (peer id: {(uint)peer.Id}) cannot be found in the game");
            return;
        }

        if (networkPlayers.TryGetValue((uint)peer.Id, out NetworkPlayer m_LeavingPlayer))
        {
            foreach (NetworkPlayer m_NetPlayer in networkPlayers.Values)
            {
                if (m_NetPlayer.Player.GetPlayerState().pid != m_LeavingPlayer.Player.GetPlayerState().pid)
                {
                    SendPacket(new PlayerLeftGamePacket { pid = m_LeavingPlayer.Player.GetPlayerState().pid }, m_NetPlayer.Peer, DeliveryMethod.ReliableOrdered);
                }
            }
            networkPlayers.Remove((uint)peer.Id);
        }
        }
    }
}

