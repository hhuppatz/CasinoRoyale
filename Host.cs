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
    private Player player1;

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

        player1 = new Player(0, username, playerTex, playerOrigin, playerBaseVelocity, new Rectangle(), true);
        _mainCamera.SetCoords(player1.GetCoords());
    }

    protected override void Update(GameTime gameTime)
    {
        // delta time and current keyboard state
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        serverUpdatetimer += deltaTime;

        if (serverUpdatetimer > 1/90f) {
            // Check for client packets
            server.PollEvents();

            // Check player states and update according to received messages
            PlayerState[] player_states = new PlayerState[networkPlayers.Count];
            for (int i = 0; i < networkPlayers.Count; i++)
            {
                PlayerState player_state = networkPlayers.Values.ToArray()[i].Player.GetPlayerState();
                player_states[i] = new PlayerState{
                    pid = player_state.pid,
                    username = player_state.username,
                    ges = player_state.ges
                };
            }

            CasinoMachineState[] casino_machine_states = new CasinoMachineState[casinoMachines.Count];
            for (int i = 0; i < casinoMachines.Count; i++)
            {
                casino_machine_states[i] = casinoMachines[i].GetState();
            }
            foreach (KeyValuePair<uint, NetworkPlayer> entry in networkPlayers)
            {
                SendPacket(new PlayerReceiveUpdatePacket{ playerStates = player_states,
                                                        casinoMachineStates = casino_machine_states },
                                                        entry.Value.Peer,
                                                        DeliveryMethod.Unreliable);
            }
            serverUpdatetimer = 0f;
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
            int platL = (int)platform.GetLCoords().X;
            int platTexWidth = platform.GetTex().Bounds.Width;
            int platWidth = platform.GetWidth();
            int i = platL;
            while (i < platL + platWidth)
            {
                _spriteBatch.Draw(platform.GetTex(),
                                _mainCamera.TransformToView(new Vector2(i + platTexWidth/2, platform.GetCoords().Y)),
                                null,
                                Color.White,
                                0.0f,
                                new Vector2(platTexWidth/2, platTexWidth/2),
                                ratio,
                                0,
                                0);
                i += platTexWidth;
            }
        }

        foreach (KeyValuePair<uint, NetworkPlayer> entry in networkPlayers)
        {
            _spriteBatch.DrawEntity(_mainCamera, entry.Value.Player);
        }
        _spriteBatch.DrawEntity(_mainCamera, player1);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    // Server functionality methods
    public void OnJoinReceived(JoinPacket packet, NetPeer peer) {
        Console.WriteLine($"Received join from {packet.username} (pid: {(uint)peer.Id})");

        PlatformState[] platform_states = new PlatformState[platforms.Count];
        for (int i = 0; i < platforms.Count; i++)
        {
            platform_states[i] = platforms[i].GetState();
        }
        CasinoMachineState[] casino_machine_states = new CasinoMachineState[casinoMachines.Count];
        for (int i = 0; i < casinoMachines.Count; i++)
        {
            casino_machine_states[i] = casinoMachines[i].GetState();
        }
        PlayerState[] other_player_states = new PlayerState[networkPlayers.Count];
        for (uint i = 0; i < networkPlayers.Count; i++)
        {
            other_player_states[(int)i] = networkPlayers[i].Player.GetPlayerState();
        }

        NetworkPlayer m_NewNetPlayer = networkPlayers[(uint)peer.Id] = new NetworkPlayer(
            peer,
            new Player( 
            (uint)peer.Id,
            packet.username,
            playerTex,
            initialPosition,
            playerBaseVelocity,
            new Rectangle(),
            true)
        );

        SendPacket(new JoinAcceptPacket { playerState = m_NewNetPlayer.Player.GetPlayerState(),
                                        playerHitbox = new Rectangle(m_NewNetPlayer.Player.GetCoords().ToPoint() - new Point(playerTex.Bounds.Width/2, playerTex.Bounds.Height/2),
                                                                        new Point(playerTex.Bounds.Width, playerTex.Bounds.Height)),
                                        playerBaseVelocity = playerBaseVelocity,
                                        platformStates = platform_states,
                                        otherPlayerStates = other_player_states,
                                        casinoMachineStates = casino_machine_states},
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
                    new_player_hitbox = new Rectangle(m_NewNetPlayer.Player.GetCoords().ToPoint() - new Point(playerTex.Bounds.Width/2, playerTex.Bounds.Height/2),
                                                                        new Point(playerTex.Bounds.Width, playerTex.Bounds.Height))
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
}

