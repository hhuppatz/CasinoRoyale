using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using CSharpFirstPerson;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CSharpFirstPerson;

public class Server : Game, INetEventListener 
{
    // Server fields
    private int PORT_NUM = 12345;
    private NetManager server;
    private EventBasedNetListener listener;
    public Vector2 initialPosition = new Vector2(0, 0);
    private NetDataWriter writer;
    private NetPacketProcessor packetProcessor;
    private Dictionary<uint, ServerPlayer> players = new Dictionary<uint, ServerPlayer>();

    // Game logic fields
    Properties _gameProperties;
    MainCamera _mainCamera = MainCamera.Instance;
    private GraphicsDeviceManager _graphics;
    private KeyboardState lastKeyboardState;
    private SpriteBatch _spriteBatch;
    private Rectangle gameArea;
    private List<Platform> platforms;
    private CasinoMachineFactory casinoMachineFactory;
    private Player player1;

    public Server()
    {
        listener = new EventBasedNetListener();
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
        server = new NetManager(this) {
            AutoRecycle = true,
        };
        writer = new NetDataWriter();
        packetProcessor = new NetPacketProcessor();
        packetProcessor.RegisterNestedType((w, v) => w.Put(v), reader => reader.GetVector2());
        packetProcessor.RegisterNestedType<PlayerState>();
        packetProcessor.SubscribeReusable<JoinPacket, NetPeer>(OnJoinReceived);
        Console.WriteLine("Starting server");
        server.Start(PORT_NUM);

        lastKeyboardState = Keyboard.GetState();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        gameArea = new Rectangle(int.Parse(_gameProperties.get("gameFloor.x")),
                                 -int.Parse(_gameProperties.get("gameFloor.height")),
                                 int.Parse(_gameProperties.get("gameFloor.width")),
                                 int.Parse(_gameProperties.get("gameFloor.height")));

        // game world initialisation
        platforms = PlatformLayout.GenerateStandardRandPlatLayout(Content.Load<Texture2D>(_gameProperties.get("casinoFloor.image.1")),
                                                                gameArea,
                                                                50,
                                                                200,
                                                                50,
                                                                100,
                                                                70);

        // player initialisation
        player1 = new Player(Content.Load<Texture2D>(_gameProperties.get("player.image")),
                            new Vector2(0, 0),
                            new Vector2(float.Parse(_gameProperties.get("playerMaxVelocity.x")), float.Parse(_gameProperties.get("playerMaxVelocity.y"))));

        // casino machine generation
        casinoMachineFactory = new CasinoMachineFactory(Content.Load<Texture2D>(_gameProperties.get("casinoMachine.image.1")));
        casinoMachineFactory.SpawnCasinoMachine();

        // main camera initialisation
        _mainCamera.InitMainCamera(Window, player1);
    }

    protected override void Update(GameTime gameTime)
    {
        // delta time and current keyboard state
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        KeyboardState ks = Keyboard.GetState();

        // Check for client packets
        server.PollEvents();

        // player logic
        player1.Move(ks, deltaTime);

        // camera logic
        _mainCamera.MoveToFollowPlayer(player1);
        
        // game window logic (move)
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // if f11 was just pressed, go fullscreen borderless
        if (Keyboard.GetState().IsKeyDown(Keys.F11) && !lastKeyboardState.IsKeyDown(Keys.F11))
        {
            Resolution.ToggleBorderless(Window, _graphics);
        }

        base.Update(gameTime);

        // saving for next update call
        lastKeyboardState = Keyboard.GetState();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkMagenta);

        Vector2 ratio = Resolution.ratio;
        _mainCamera.ApplyRatio(ratio);

        // drawing sprites
        _spriteBatch.Begin();

        foreach (CasinoMachine machine in casinoMachineFactory.GetCasinoMachines())
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
        _spriteBatch.Draw(player1.GetTex(),
                        _mainCamera.TransformToView(player1.GetCoords()),
                        null,
                        Color.White,
                        0.0f,
                        new Vector2(player1.GetTex().Bounds.Width/2, player1.GetTex().Bounds.Height/2),
                        ratio,
                        0,
                        0);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    // Server functionality methods, structs and classes
    public void SendPacket<T>(T packet, NetPeer peer, DeliveryMethod deliveryMethod) where T : class, new() {
        if (peer != null) {
            writer.Reset();
            packetProcessor.Write(writer, packet);
            peer.Send(writer, deliveryMethod);
        }
    }

    public void OnJoinReceived(JoinPacket packet, NetPeer peer) {
        Console.WriteLine($"Received join from {packet.username} (pid: {(uint)peer.Id})");

        ServerPlayer newPlayer = players[(uint)peer.Id] = new ServerPlayer {
            peer = peer,
            state = new PlayerState {
                pid = (uint)peer.Id,
                position = initialPosition,
            },
            username = packet.username,
        };

        SendPacket(new JoinAcceptPacket { state = newPlayer.state }, peer, DeliveryMethod.ReliableOrdered);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod) {
        packetProcessor.ReadAllPackets(reader, peer);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
        if (peer.Tag != null) {
            players.Remove((uint)peer.Id);
        }
    }

    public class JoinPacket {
        public string username { get; set; }
    }

    public class JoinAcceptPacket {
        public PlayerState state { get; set; }
    }

    public struct PlayerState : INetSerializable {
        public uint pid;
        public Vector2 position;

        public void Serialize(NetDataWriter writer) {
            writer.Put(pid);
            writer.Put(position);
        }

        public void Deserialize(NetDataReader reader) {
            pid = reader.GetUInt();
            position = reader.GetVector2();
        }
    }

    public class ClientPlayer {
        public PlayerState state;
        public string username;
    }

    public class ServerPlayer {
        public NetPeer peer;
        public PlayerState state;
        public string username;
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        Console.WriteLine($"Incoming connection from {request.RemoteEndPoint}");
        request.Accept();
        ((INetEventListener)listener).OnConnectionRequest(request);
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        ((INetEventListener)listener).OnNetworkError(endPoint, socketError);
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        ((INetEventListener)listener).OnNetworkLatencyUpdate(peer, latency);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        ((INetEventListener)listener).OnNetworkReceive(peer, reader, channelNumber, deliveryMethod);
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        ((INetEventListener)listener).OnNetworkReceiveUnconnected(remoteEndPoint, reader, messageType);
    }

    public void OnPeerConnected(NetPeer peer)
    {
        ((INetEventListener)listener).OnPeerConnected(peer);
    }
}

public static class SerializingExtensions {
    public static void Put(this NetDataWriter writer, Vector2 vector) {
        writer.Put(vector.X);
        writer.Put(vector.Y);
    }

    public static Vector2 GetVector2(this NetDataReader reader) {
        return new Vector2(reader.GetFloat(), reader.GetFloat());
    }
}