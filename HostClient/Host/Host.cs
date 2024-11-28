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

namespace CSharpFirstPerson;

public class Host : Game, INetEventListener
{
    // Server fields
    private int PORT_NUM = 12345;
    private NetManager server;
    public Vector2 initialPosition = new Vector2(0, 0);
    private NetDataWriter writer;
    private NetPacketProcessor packetProcessor;
    private Dictionary<uint, NetworkPlayer> networkPlayers = new Dictionary<uint, NetworkPlayer>();
    private float serverUpdatetimer = 0f;
    private int MAX_PLAYERS = 6;

    // Game fields
    Properties _gameProperties;
    MainCamera _mainCamera = MainCamera.Instance;
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Rectangle gameArea;
    private List<Platform> platforms;
    private List<CasinoMachine> casinoMachines;
    private CasinoMachineFactory casinoMachineFactory;

    // Player loading info fields
    private Texture2D playerTex;
    private Vector2 playerOrigin;
    private Vector2 playerBaseVelocity;

    // Host player
    private string username = "PENIS";
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
        networkPlayers = new Dictionary<uint, NetworkPlayer>();

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

        // game world initialisation
        platforms = PlatformLayout.GenerateStandardRandPlatLayout(Content.Load<Texture2D>(_gameProperties.get("casinoFloor.image.1")),
                                                                gameArea,
                                                                50,
                                                                200,
                                                                50,
                                                                100,
                                                                70);

        // player initialisation
        playerTex = Content.Load<Texture2D>(_gameProperties.get("player.image"));
        playerOrigin = new Vector2(0, 0);
        playerBaseVelocity = new Vector2(float.Parse(_gameProperties.get("playerMaxVelocity.x")), float.Parse(_gameProperties.get("playerMaxVelocity.y")));

        // casino machine generation
        casinoMachines = new List<CasinoMachine>();
        casinoMachineFactory = new CasinoMachineFactory(Content.Load<Texture2D>(_gameProperties.get("casinoMachine.image.1")));
        
        casinoMachines = casinoMachineFactory.SpawnCasinoMachines();

        player1 = new PlayableCharacter(0,
                            username,
                            playerTex,
                            playerOrigin,
                            playerBaseVelocity,
                            new Rectangle(playerOrigin.ToPoint(), new Point(playerTex.Bounds.Width, playerTex.Bounds.Height)),
                            true);
                            
        _mainCamera.InitMainCamera(Window, player1);
    }

    protected override void Update(GameTime gameTime)
    {
        // delta time and current keyboard state
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        serverUpdatetimer += deltaTime;

        // Check for client packets
        server.PollEvents();

        if (Keyboard.GetState().IsKeyDown(Keys.A))
        {
            player1.SetCoords(player1.GetCoords() + new Vector2(-player1.GetVelocity().X,0) * deltaTime);
        }
        if (Keyboard.GetState().IsKeyDown(Keys.D))
        {
            player1.SetCoords(player1.GetCoords() + new Vector2(player1.GetVelocity().X,0) * deltaTime);
        }

        foreach (KeyValuePair<uint, NetworkPlayer> entry in networkPlayers)
        {
            ICollidable m_other = entry.Value.Player;
            if (m_other.CollidedWith(player1))
            {
                Console.WriteLine(entry.Value.Player.GetID());
            }
        }

        // Check player states and update according to received messages
        PlayerState[] m_otherPlayerStates = new PlayerState[networkPlayers.Count + 1];
        m_otherPlayerStates[0] = new PlayerState {
                pid = player1.GetID(),
                username = player1.GetUsername(),
                ges = player1.GetEntityState()
            };
        for (int i = 1; i < networkPlayers.Count; i++)
        {
            PlayerState m_playerState = networkPlayers.Values.ToArray()[i].Player.GetPlayerState();
            m_otherPlayerStates[i] = new PlayerState {
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
            SendPacket(new PlayerReceiveUpdatePacket{ playerStates = m_otherPlayerStates,
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

        Vector2 ratio = Resolution.ratio;
        _mainCamera.ApplyRatio(ratio);

        // drawing sprites
        _spriteBatch.Begin();

        foreach (CasinoMachine machine in casinoMachines)
        {
            _spriteBatch.Draw(machine.GetTex(),
                            _mainCamera.TransformToView(machine.GetCoords()),
                            null,
                            Color.White,
                            0.0f,
                            new Vector2(machine.GetTex().Bounds.Width/2, machine.GetTex().Bounds.Height/2),
                            ratio,
                            0,
                            0);
        }
        
        foreach (Platform platform in platforms)
        {
            // TODO: Need to implement restriction on plat length so is a multiple of the length of the plat tex
            int m_platL = (int)platform.GetLCoords().X;
            int m_platTexWidth = platform.GetTex().Bounds.Width;
            int m_platWidth = platform.GetWidth();
            int i = m_platL;
            while (i < m_platL + m_platWidth)
            {
                _spriteBatch.Draw(platform.GetTex(),
                                _mainCamera.TransformToView(new Vector2(i + m_platTexWidth/2, platform.GetCoords().Y)),
                                null,
                                Color.White,
                                0.0f,
                                new Vector2(m_platTexWidth/2, m_platTexWidth/2),
                                ratio,
                                0,
                                0);
                i += m_platTexWidth;
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
        Console.WriteLine($"Received join from {packet.username} (pid: {(uint)peer.Id})");

        PlatformState[] m_platformStates = new PlatformState[platforms.Count];
        for (int i = 0; i < platforms.Count; i++)
        {
            m_platformStates[i] = platforms[i].GetState();
        }
        CasinoMachineState[]m_CasinoMachineStates = new CasinoMachineState[casinoMachines.Count];
        for (int i = 0; i < casinoMachines.Count; i++)
        {
           m_CasinoMachineStates[i] = casinoMachines[i].GetState();
        }
        PlayerState[] m_otherPlayerStates = new PlayerState[networkPlayers.Count + 1];
        m_otherPlayerStates[0] = player1.GetPlayerState();
        for (uint i = 1; i < networkPlayers.Count; i++)
        {
            m_otherPlayerStates[i] = networkPlayers[i].Player.GetPlayerState();
        }

        NetworkPlayer m_NewNetPlayer = networkPlayers[(uint)peer.Id] = new NetworkPlayer(
            peer,
            new PlayableCharacter( 
            (uint)peer.Id,
            packet.username,
            playerTex,
            initialPosition,
            playerBaseVelocity,
            new Rectangle(playerOrigin.ToPoint(), new Point(playerTex.Bounds.Width, playerTex.Bounds.Height)),
            true)
        );

        SendPacket(new JoinAcceptPacket { playerState = m_NewNetPlayer.Player.GetPlayerState(),
                                        playerHitbox = new Rectangle(m_NewNetPlayer.Player.GetCoords().ToPoint(), new Point(playerTex.Bounds.Width, playerTex.Bounds.Height)),
                                        playerBaseVelocity = playerBaseVelocity,
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
                            pid = (uint)peer.Id,
                            ges = new GameEntityState {
                                awake = true,
                                coords = initialPosition,
                                velocity = playerBaseVelocity
                            },
                        },
                    new_player_hitbox = new Rectangle(m_NewNetPlayer.Player.GetCoords().ToPoint(), new Point(playerTex.Bounds.Width, playerTex.Bounds.Height))
                }, player.Peer, DeliveryMethod.ReliableOrdered);
            }
        }
    }

    public void OnPlayerUpdate(PlayerSendUpdatePacket packet, NetPeer peer) {
        networkPlayers[(uint)peer.Id].Player.SetCoords(packet.coords);
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
        Console.WriteLine($"Player (pid: {(uint)peer.Id}) left the game");
        if (peer.Tag == null) {
            Console.WriteLine($"Player (pid: {(uint)peer.Id}) cannot be found in the game");
            return;
        }

        NetworkPlayer m_LeavingPlayer;
        if (networkPlayers.TryGetValue((uint)peer.Id, out m_LeavingPlayer)) {
            foreach (NetworkPlayer m_NetPlayer in networkPlayers.Values) {
                if (m_NetPlayer.Player.GetPlayerState().pid != m_LeavingPlayer.Player.GetPlayerState().pid) {
                    SendPacket(new PlayerLeftGamePacket { pid = m_LeavingPlayer.Player.GetPlayerState().pid }, m_NetPlayer.Peer, DeliveryMethod.ReliableOrdered);
                }
            }
            networkPlayers.Remove((uint)peer.Id);
        }
    }

    private struct PlayerIDs
    {
        private Dictionary<uint, bool> ids;

        public PlayerIDs(int MAX_PLAYERS)
        {
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                ids.Add((uint)i, false);
            }
        }

        public bool IsTaken(uint id)
        {
            return ids[id];
        }

    }
}

