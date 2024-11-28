using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CSharpFirstPerson;

public class Client : Game, INetEventListener {
    // Client fields
    private string username = "HH";
    private int PORT_NUM = 12345;
    private NetDataWriter writer;
    private NetPacketProcessor packetProcessor;
    private PlayableCharacter player1;
    private List<PlayableCharacter> otherPlayers;
    private NetManager client;
    private NetPeer server;
    private bool connected = false;
    
    // Game fields
    MainCamera _mainCamera = MainCamera.Instance;
    private GraphicsDeviceManager _graphics;
    private KeyboardState lastKeyboardState;
    private SpriteBatch _spriteBatch;
    private Rectangle gameArea;
    private List<Platform> platforms;
    private List<CasinoMachine> casinoMachines;
    private Texture2D platformTex;
    private Texture2D casinoMachineTex;

    // Player fields
    private Texture2D playerTex;
    private Vector2 playerBaseVelocity;

    public Client()
    {
        _graphics = new GraphicsDeviceManager(this);
        Window.AllowUserResizing = false;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // Initialise Client player (this)
        otherPlayers = new List<PlayableCharacter>();

        writer = new NetDataWriter();
        packetProcessor = new NetPacketProcessor();

        packetProcessor.RegisterNestedType((w, v) => w.Put(v), reader => reader.GetVector2());
        packetProcessor.RegisterNestedType((w, v) => w.Put(v), reader => reader.GetGES());
        packetProcessor.RegisterNestedType((w, v) => w.Put(v), reader => reader.GetRectangle());
        packetProcessor.RegisterNestedType<PlayerState>();
        packetProcessor.RegisterNestedType<PlatformState>();
        packetProcessor.RegisterNestedType<CasinoMachineState>();

        packetProcessor.SubscribeReusable<JoinAcceptPacket>(OnJoinAccept);
        packetProcessor.SubscribeReusable<PlayerReceiveUpdatePacket>(OnReceiveUpdate);
        packetProcessor.SubscribeReusable<PlayerJoinedGamePacket>(OnPlayerJoin);
        packetProcessor.SubscribeReusable<PlayerLeftGamePacket>(OnPlayerLeave);

        // Initialise gameobject storage
        casinoMachines = new List<CasinoMachine>();
        platforms = new List<Platform>();
        gameArea = new Rectangle();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // texture loading
        playerTex = Content.Load<Texture2D>("ball");
        platformTex = Content.Load<Texture2D>("CasinoFloor1");
        casinoMachineTex = Content.Load<Texture2D>("CasinoMachine1");

        JoinServer();

        // main camera initialisation
        _mainCamera.InitMainCamera(Window, player1);
    }

    protected override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // left is negative x, right positive
        if (Keyboard.GetState().IsKeyDown(Keys.A))
        {
            player1.SetCoords(player1.GetCoords() + new Vector2(-player1.GetVelocity().X,0) * deltaTime);
        }
        if (Keyboard.GetState().IsKeyDown(Keys.D))
        {
            player1.SetCoords(player1.GetCoords() + new Vector2(player1.GetVelocity().X,0) * deltaTime);
        }

        if (client != null) {
            client.PollEvents();
            if (!player1.Equals(null)) {
                SendPacket(new PlayerSendUpdatePacket { coords = player1.GetCoords(),
                                                        velocity = player1.GetVelocity(),
                                                        dt = deltaTime },
                                                        DeliveryMethod.Unreliable);
            }
        }

        // TODO: Add your update logic here
        _mainCamera.MoveToFollowPlayer(player1);

        if (Keyboard.GetState().IsKeyDown(Keys.F11))
            Resolution.ToggleFullscreen(Window, _graphics);

        base.Update(gameTime);

        lastKeyboardState = Keyboard.GetState();

        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkMagenta);

        Vector2 ratio = Resolution.ratio;
        _mainCamera.ApplyRatio(ratio);

        // drawing sprites
        _spriteBatch.Begin();
        for (int j = 0; j < casinoMachines.Count; j++)
        {
            CasinoMachine machine = casinoMachines[j];
            _spriteBatch.Draw(machine.GetTex(), _mainCamera.TransformToView(machine.GetCoords()),
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
                                Vector2.Zero,
                                ratio,
                                0,
                                0);
                i += platTexWidth;
            }
        }

        // Draw other players
        for (int j = 0; j < otherPlayers.Count; j++)
        {
            _spriteBatch.DrawEntity(_mainCamera, otherPlayers[j]);
        }

        // Draw this player
        _spriteBatch.DrawEntity(_mainCamera, player1);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    // Network methods
    public void OnReceiveUpdate(PlayerReceiveUpdatePacket packet) {
        // For each player state we are sent, update local player states
        foreach (PlayerState other_player_state in packet.playerStates) {
            if (other_player_state.pid != player1.GetPlayerState().pid) {
                foreach (PlayableCharacter other_player in otherPlayers)
                {
                    if (other_player_state.pid == other_player.GetPlayerState().pid)
                    {
                        other_player.SetPlayerState(other_player_state);
                    }
                }
            }
        }
        foreach (CasinoMachineState casino_machine_state in packet.casinoMachineStates)
        {
            casinoMachines[(int)casino_machine_state.machineNum].SetState(casino_machine_state);
        }
    }

    private void JoinServer()
    {
        client = new NetManager(this) {
            AutoRecycle = true,
        };
        client.Start();
        Console.WriteLine("Connecting to server");
        client.Connect("localhost", PORT_NUM, username);

        while (!connected)
        {
            if (client != null && client.GetPeersCount(ConnectionState.Connected) > 0) client.PollEvents();
            Thread.Sleep(100);
        }
    }

    public void OnPlayerJoin(PlayerJoinedGamePacket packet) {
        Console.WriteLine($"Player '{packet.new_player_username}' (pid: {packet.new_player_state.pid}) joined the game");
        otherPlayers.Add(new PlayableCharacter(
                            packet.new_player_state.pid,
                            packet.new_player_username,
                            playerTex,
                            packet.new_player_state.ges.coords,
                            packet.new_player_state.ges.velocity,
                            packet.new_player_hitbox,
                            true
                            ));
    }

    public void OnPlayerLeave(PlayerLeftGamePacket packet) {
        Console.WriteLine($"Player (pid: {packet.pid}) left the game");
        foreach (PlayableCharacter otherPlayer in otherPlayers)
        {
            if (packet.pid == otherPlayer.GetID())
            {
                otherPlayers.Remove(otherPlayer);
                break;
            }
        };
    }

    // Initialise game objects as dictated by server
    public void OnJoinAccept(JoinAcceptPacket packet) {
        Console.WriteLine($"Join accepted by server (pid: {packet.playerState.pid})");
        gameArea = packet.gameArea;
        playerBaseVelocity = packet.playerBaseVelocity;
        player1 = new PlayableCharacter(packet.playerState.pid,
                            packet.playerState.username,
                            playerTex,
                            packet.playerState.ges.coords,
                            playerBaseVelocity,
                            packet.playerHitbox,
                            true);

        foreach (PlatformState platformState in packet.platformStates)
        {
            platforms.Add(new Platform(platformState.platNum, platformTex, platformState.L, platformState.R));
        }
        foreach (CasinoMachineState casinoMachineState in packet.casinoMachineStates)
        {
            casinoMachines.Add(new CasinoMachine(casinoMachineState.machineNum, casinoMachineTex, casinoMachineState.coords));
        }
        foreach(PlayerState other_player_state in packet.otherPlayerStates)
        {
            if (other_player_state.pid != player1.GetID())
            {
                otherPlayers.Add(new PlayableCharacter(
                    other_player_state.pid,
                    other_player_state.username,
                    playerTex,
                    other_player_state.ges.coords,
                    playerBaseVelocity,
                    packet.playerHitbox,
                    true));
                Console.WriteLine(1);
            }
            Console.WriteLine(2);
        }
        
        connected = true;
    }

    public void OnPeerConnected(NetPeer peer) {
        Console.WriteLine("Connected to server");
        Console.WriteLine(peer.Address);
        server = peer;
        SendPacket(new JoinPacket { username = username }, DeliveryMethod.ReliableOrdered);
    }

    public void SendPacket<T>(T packet, DeliveryMethod deliveryMethod) where T : class, new() {
        if (server != null) {
            writer.Reset();
            packetProcessor.Write(writer, packet);
            server.Send(writer, deliveryMethod);
        }
    }

    // INetEventListener methods, implemented through Client class
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

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Console.WriteLine("[S] NetworkError: " + disconnectInfo);
        Exit();
    }
}