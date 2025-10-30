using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using CasinoRoyale.Utils;
using Microsoft.Xna.Framework;
using CasinoRoyale.Classes.GameObjects;
using CasinoRoyale.Classes.GameObjects.Player;
using CasinoRoyale.Classes.GameObjects.Platforms;
using CasinoRoyale.Classes.GameObjects.Items;
using CasinoRoyale.Classes.Networking.SerializingExtensions;

namespace CasinoRoyale.Classes.Networking;

// Asynchronous packet processor that handles network packets on a background thread
// to prevent blocking the main game thread, with main thread dispatch for game events
public class AsyncPacketProcessor : IDisposable
{
    private readonly ConcurrentQueue<PacketData> _packetQueue = new();
    private readonly ConcurrentQueue<MainThreadEvent> _mainThreadEventQueue = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _processingTask;
    private readonly SemaphoreSlim _semaphore = new(0);
    private readonly NetPacketProcessor _packetProcessor;
    private readonly object _eventHandlerLock = new();
    
    // Packet pooling for memory efficiency
    private readonly ConcurrentQueue<byte[]> _packetPool = new();
    private const int MaxPooledPackets = 100;
    
    // Packet size limits to prevent buffer overflow
    private const int MaxPacketSize = 8192; // 8KB max packet size
    private const int MaxQueueSize = 1000; // Maximum packets in queue
    
    // Events for packet processing results
    public event EventHandler<PacketReceivedEventArgs<PlayerSendUpdatePacket>> PlayerUpdateReceived;
    public event EventHandler<PacketReceivedEventArgs<JoinPacket>> JoinRequestReceived;
    public event EventHandler<PacketReceivedEventArgs<JoinAcceptPacket>> JoinAcceptReceived;
    public event EventHandler<PacketReceivedEventArgs<GameWorldInitPacket>> GameWorldInitReceived;
    // Platform packets removed
    public event EventHandler<PacketReceivedEventArgs<PlayerReceiveUpdatePacket>> PlayerStatesUpdateReceived;
    // Platform packets removed
    public event EventHandler<PacketReceivedEventArgs<ItemUpdatePacket>> ItemSpawnedReceived;
    public event EventHandler<PacketReceivedEventArgs<ItemRemovedPacket>> ItemRemovedReceived;
    public event EventHandler<PacketReceivedEventArgs<PlayerJoinedGamePacket>> PlayerJoinedGameReceived;
    public event EventHandler<PacketReceivedEventArgs<PlayerLeftGamePacket>> PlayerLeftGameReceived;

    public AsyncPacketProcessor()
    {
        _packetProcessor = new NetPacketProcessor();
        RegisterPacketHandlers();
        
        // Pre-populate packet pool
        for (int i = 0; i < 20; i++)
        {
            _packetPool.Enqueue(new byte[4096]); // 4KB packets
        }
        
        _processingTask = Task.Run(ProcessPacketsAsync, _cancellationTokenSource.Token);
    }

    private void RegisterPacketHandlers()
    {
        // Register custom serializers for XNA Framework types
        _packetProcessor.RegisterNestedType<Vector2>(SerializeVector2, DeserializeVector2);
        _packetProcessor.RegisterNestedType<Rectangle>(SerializeRectangle, DeserializeRectangle);
        
        // Register custom serializers for game object types
        _packetProcessor.RegisterNestedType<GameEntityState>(SerializeGameEntityState, DeserializeGameEntityState);
        _packetProcessor.RegisterNestedType<PlayerState>(SerializePlayerState, DeserializePlayerState);
        // Platform state serializer removed with grid migration
        _packetProcessor.RegisterNestedType<ItemState>(SerializeItemState, DeserializeItemState);
        _packetProcessor.RegisterNestedType<GridTileState>(SerializeGridTileState, DeserializeGridTileState);
        
        Logger.LogNetwork("PACKET_PROCESSOR", "Registered custom serializers for XNA and game object types");
        
        // Note: State types (PlayerState, etc.) implement INetSerializable
        // and handle their own serialization, including Vector2 properties.
        
        Logger.LogNetwork("PACKET_PROCESSOR", "Registered packet handlers for INetSerializable types");
        
        // Register packet handlers for INetSerializable packet classes
        _packetProcessor.SubscribeReusable<PlayerSendUpdatePacket, NetPeer>(OnPlayerStateReceived);
        _packetProcessor.SubscribeReusable<JoinPacket, NetPeer>(OnJoinRequestReceived);
        _packetProcessor.SubscribeReusable<JoinAcceptPacket, NetPeer>(OnJoinAcceptReceived);
        _packetProcessor.SubscribeReusable<GameWorldInitPacket, NetPeer>(OnGameWorldInitReceived);
        // Platform packets removed
        _packetProcessor.SubscribeReusable<PlayerReceiveUpdatePacket, NetPeer>(OnPlayerStatesUpdateReceived);
        // Platform packets removed
        _packetProcessor.SubscribeReusable<ItemUpdatePacket, NetPeer>(OnItemSpawnedReceived);
        _packetProcessor.SubscribeReusable<ItemRemovedPacket, NetPeer>(OnItemRemovedReceived);
        _packetProcessor.SubscribeReusable<PlayerJoinedGamePacket, NetPeer>(OnPlayerJoinedGameReceived);
        _packetProcessor.SubscribeReusable<PlayerLeftGamePacket, NetPeer>(OnPlayerLeftGameReceived);
    }

    // Custom serializers for XNA Framework types
    public static void SerializeVector2(NetDataWriter writer, Vector2 vector)
    {
        writer.Put(vector.X);
        writer.Put(vector.Y);
    }
    
    public static Vector2 DeserializeVector2(NetDataReader reader)
    {
        return new Vector2(reader.GetFloat(), reader.GetFloat());
    }
    
    public static void SerializeRectangle(NetDataWriter writer, Rectangle rectangle)
    {
        writer.Put(rectangle.X);
        writer.Put(rectangle.Y);
        writer.Put(rectangle.Width);
        writer.Put(rectangle.Height);
    }
    
    public static Rectangle DeserializeRectangle(NetDataReader reader)
    {
        return new Rectangle(reader.GetInt(), reader.GetInt(), reader.GetInt(), reader.GetInt());
    }
    
    // Custom serializers for game object types
    public static void SerializeGameEntityState(NetDataWriter writer, GameEntityState ges)
    {
        // Add validation to prevent serialization issues
        if (float.IsNaN(ges.coords.X) || float.IsNaN(ges.coords.Y) || 
            float.IsInfinity(ges.coords.X) || float.IsInfinity(ges.coords.Y))
        {
            Logger.Error($"Invalid coordinates in GameEntityState: {ges.coords}");
            writer.Put(false); // awake
            writer.Put(0f); // coords.X
            writer.Put(0f); // coords.Y
            writer.Put(0f); // velocity.X
            writer.Put(0f); // velocity.Y
            writer.Put(ges.mass);
            return;
        }
        
        if (float.IsNaN(ges.velocity.X) || float.IsNaN(ges.velocity.Y) || 
            float.IsInfinity(ges.velocity.X) || float.IsInfinity(ges.velocity.Y))
        {
            Logger.Error($"Invalid velocity in GameEntityState: {ges.velocity}");
            writer.Put(ges.awake);
            writer.Put(ges.coords.X);
            writer.Put(ges.coords.Y);
            writer.Put(0f); // velocity.X
            writer.Put(0f); // velocity.Y
            writer.Put(ges.mass);
            return;
        }
        
        writer.Put(ges.awake);
        writer.Put(ges.coords.X);
        writer.Put(ges.coords.Y);
        writer.Put(ges.velocity.X);
        writer.Put(ges.velocity.Y);
        writer.Put(ges.mass);
    }
    
    public static GameEntityState DeserializeGameEntityState(NetDataReader reader)
    {
        return new GameEntityState 
        { 
            awake = reader.GetBool(), 
            coords = new Vector2(reader.GetFloat(), reader.GetFloat()), 
            velocity = new Vector2(reader.GetFloat(), reader.GetFloat()), 
            mass = reader.GetFloat() 
        };
    }
    
    public static void SerializePlayerState(NetDataWriter writer, PlayerState playerState)
    {
        writer.Put((byte)playerState.objectType);
        writer.Put(playerState.pid);
        writer.Put(playerState.username);
        SerializeGameEntityState(writer, playerState.ges);
        writer.Put(playerState.initialJumpVelocity);
        writer.Put(playerState.maxRunSpeed);
    }
    
    public static PlayerState DeserializePlayerState(NetDataReader reader)
    {
        return new PlayerState
        {
            objectType = (ObjectType)reader.GetByte(),
            pid = reader.GetUInt(),
            username = reader.GetString(),
            ges = DeserializeGameEntityState(reader),
            initialJumpVelocity = reader.GetFloat(),
            maxRunSpeed = reader.GetFloat()
        };
    }
    
    // Platform serializers removed with grid migration
    
    // Casino machine serializers removed

    public static void SerializeGridTileState(NetDataWriter writer, GridTileState tile)
    {
        writer.Put((byte)tile.type);
        SerializeRectangle(writer, tile.hitbox);
        SerializeRectangle(writer, tile.source);
        writer.Put(tile.isSolid);
    }
    
    public static GridTileState DeserializeGridTileState(NetDataReader reader)
    {
        return new GridTileState
        {
            type = (GridTileType)reader.GetByte(),
            hitbox = DeserializeRectangle(reader),
            source = DeserializeRectangle(reader),
            isSolid = reader.GetBool()
        };
    }
    
    public static void SerializeItemState(NetDataWriter writer, ItemState itemState)
    {
        writer.Put((byte)itemState.objectType);
        writer.Put(itemState.itemId);
        writer.Put((byte)itemState.itemType);
        SerializeGameEntityState(writer, itemState.gameEntityState);
    }
    
    public static ItemState DeserializeItemState(NetDataReader reader)
    {
        return new ItemState
        {
            objectType = (ObjectType)reader.GetByte(),
            itemId = reader.GetUInt(),
            itemType = (ItemType)reader.GetByte(),
            gameEntityState = DeserializeGameEntityState(reader)
        };
    }

    // Get the packet processor for writing packets (used by NetworkManager)
    public NetPacketProcessor PacketProcessor => _packetProcessor;

    // Enqueue a packet for asynchronous processing from byte array
    public void EnqueuePacket(NetPeer peer, byte[] packetData, byte channel, DeliveryMethod deliveryMethod)
    {
        try
        {
            if (packetData == null || packetData.Length == 0)
                return;
            
            // Check packet size limit
            if (packetData.Length > MaxPacketSize)
            {
                Logger.Warning($"Packet too large ({packetData.Length} bytes), dropping packet");
                return;
            }
            
            // Check queue size limit
            if (_packetQueue.Count >= MaxQueueSize)
            {
                Logger.Warning($"Packet queue full ({_packetQueue.Count} packets), dropping packet");
                return;
            }
                
            // Get pooled buffer or create new one
            byte[] packetBuffer;
            if (!_packetPool.TryDequeue(out packetBuffer) || packetBuffer.Length < packetData.Length)
            {
                packetBuffer = new byte[Math.Max(packetData.Length, 1024)];
            }
            
            // Copy data to pooled buffer
            Array.Copy(packetData, packetBuffer, packetData.Length);
            
            var data = new PacketData
            {
                Peer = peer,
                Data = packetBuffer,
                DataLength = packetData.Length,
                Channel = channel,
                DeliveryMethod = deliveryMethod
            };
            
            _packetQueue.Enqueue(data);
            _semaphore.Release();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error enqueuing packet: {ex.Message}");
        }
    }

    // Enqueue a packet for asynchronous processing
    public void EnqueuePacket(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        try
        {
            var remainingBytes = reader.GetRemainingBytes();
            if (remainingBytes == null || remainingBytes.Length == 0)
                return;
            
            // Check packet size limit
            if (remainingBytes.Length > MaxPacketSize)
            {
                Logger.Warning($"Packet too large ({remainingBytes.Length} bytes), dropping packet");
                return;
            }
            
            // Check queue size limit
            if (_packetQueue.Count >= MaxQueueSize)
            {
                Logger.Warning($"Packet queue full ({_packetQueue.Count} packets), dropping packet");
                return;
            }
                
            // Get pooled buffer or create new one
            byte[] packetBuffer;
            if (!_packetPool.TryDequeue(out packetBuffer) || packetBuffer.Length < remainingBytes.Length)
            {
                packetBuffer = new byte[Math.Max(remainingBytes.Length, 1024)];
            }
            
            // Copy data to pooled buffer
            Array.Copy(remainingBytes, packetBuffer, remainingBytes.Length);
            
            var packetData = new PacketData
            {
                Peer = peer,
                Data = packetBuffer,
                DataLength = remainingBytes.Length,
                Channel = channel,
                DeliveryMethod = deliveryMethod
            };
            
            _packetQueue.Enqueue(packetData);
            _semaphore.Release();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error enqueuing packet: {ex.Message}");
        }
    }

    // Background task that processes queued packets
    private async Task ProcessPacketsAsync()
    {
        
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                await _semaphore.WaitAsync(_cancellationTokenSource.Token);
                
                while (_packetQueue.TryDequeue(out var packetData))
                {
                    ProcessPacket(packetData);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in packet processing loop: {ex.Message}");
            }
        }
        
    }

    // Process a single packet on the background thread
    private void ProcessPacket(PacketData packetData)
    {
        try
        {
            // Create a reader with the exact data length
            var dataSlice = new byte[packetData.DataLength];
            Array.Copy(packetData.Data, dataSlice, packetData.DataLength);
            
            Logger.LogNetwork("ASYNC_PROCESSOR", $"Processing packet with {packetData.DataLength} bytes from {(packetData.Peer != null ? $"peer {packetData.Peer.Id}" : "relay server")}");
            
            var reader = new NetDataReader(dataSlice);
            
            // Basic size check
            if (reader.AvailableBytes < 1)
            {
                Logger.LogNetwork("ASYNC_PROCESSOR", $"Packet too small to process: {packetData.DataLength} bytes - skipping");
                return;
            }
            
            // Additional validation for packet data
            if (packetData.DataLength < 4)
            {
                Logger.LogNetwork("ASYNC_PROCESSOR", $"Packet too small for game data: {packetData.DataLength} bytes - likely control message");
                return;
            }
            
            // Process the packet using NetPacketProcessor
            _packetProcessor.ReadAllPackets(reader, packetData.Peer);
            
            Logger.LogNetwork("ASYNC_PROCESSOR", $"Successfully processed packet with {packetData.DataLength} bytes");
            
            // Return buffer to pool
            if (_packetPool.Count < MaxPooledPackets)
            {
                _packetPool.Enqueue(packetData.Data);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error processing packet: {ex.Message}");
            Logger.Error($"Packet data length: {packetData.DataLength}, Peer: {(packetData.Peer != null ? packetData.Peer.Id.ToString() : "null")}");
            Logger.Error($"Stack trace: {ex.StackTrace}");
            
            // Return buffer to pool even on error
            if (_packetPool.Count < MaxPooledPackets)
            {
                _packetPool.Enqueue(packetData.Data);
            }
        }
    }

    #region Packet Event Handlers (called on background thread - queue for main thread)

    private void OnPlayerStateReceived(PlayerSendUpdatePacket packet, NetPeer peer)
    {
        QueueMainThreadEvent(() => PlayerUpdateReceived?.Invoke(this, new PacketReceivedEventArgs<PlayerSendUpdatePacket>(packet, peer)));
    }

    private void OnJoinRequestReceived(JoinPacket packet, NetPeer peer)
    {
        QueueMainThreadEvent(() => JoinRequestReceived?.Invoke(this, new PacketReceivedEventArgs<JoinPacket>(packet, peer)));
    }

    private void OnJoinAcceptReceived(JoinAcceptPacket packet, NetPeer peer)
    {
        Logger.LogNetwork("ASYNC_PROCESSOR", "OnJoinAcceptReceived called - processing JoinAccept packet");
        QueueMainThreadEvent(() => JoinAcceptReceived?.Invoke(this, new PacketReceivedEventArgs<JoinAcceptPacket>(packet, peer)));
    }

    private void OnGameWorldInitReceived(GameWorldInitPacket packet, NetPeer peer)
    {
        Logger.LogNetwork("ASYNC_PROCESSOR", $"OnGameWorldInitReceived called - processing GameWorldInit packet with {packet.itemStates?.Length ?? 0} items and {packet.gridTiles?.Length ?? 0} tiles");
        QueueMainThreadEvent(() => GameWorldInitReceived?.Invoke(this, new PacketReceivedEventArgs<GameWorldInitPacket>(packet, peer)));
    }

    // Platform packets removed

    private void OnPlayerStatesUpdateReceived(PlayerReceiveUpdatePacket packet, NetPeer peer)
    {
        QueueMainThreadEvent(() => PlayerStatesUpdateReceived?.Invoke(this, new PacketReceivedEventArgs<PlayerReceiveUpdatePacket>(packet, peer)));
    }

    // Platform packets removed

    // Casino machine update handler removed

    private void OnItemSpawnedReceived(ItemUpdatePacket packet, NetPeer peer)
    {
        QueueMainThreadEvent(() => ItemSpawnedReceived?.Invoke(this, new PacketReceivedEventArgs<ItemUpdatePacket>(packet, peer)));
    }

    private void OnItemRemovedReceived(ItemRemovedPacket packet, NetPeer peer)
    {
        QueueMainThreadEvent(() => ItemRemovedReceived?.Invoke(this, new PacketReceivedEventArgs<ItemRemovedPacket>(packet, peer)));
    }

    private void OnPlayerJoinedGameReceived(PlayerJoinedGamePacket packet, NetPeer peer)
    {
        QueueMainThreadEvent(() => PlayerJoinedGameReceived?.Invoke(this, new PacketReceivedEventArgs<PlayerJoinedGamePacket>(packet, peer)));
    }

    private void OnPlayerLeftGameReceived(PlayerLeftGamePacket packet, NetPeer peer)
    {
        QueueMainThreadEvent(() => PlayerLeftGameReceived?.Invoke(this, new PacketReceivedEventArgs<PlayerLeftGamePacket>(packet, peer)));
    }

    // Queue an event to be processed on the main thread
    private void QueueMainThreadEvent(Action eventAction)
    {
        _mainThreadEventQueue.Enqueue(new MainThreadEvent { Action = eventAction });
    }

    // Process all queued main thread events (call this from main thread)
    public void ProcessMainThreadEvents()
    {
        while (_mainThreadEventQueue.TryDequeue(out var mainThreadEvent))
        {
            try
            {
                mainThreadEvent.Action?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error processing main thread event: {ex.Message}");
                Logger.Error($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Logger.Error($"Inner exception: {ex.InnerException.Message}");
                    Logger.Error($"Inner stack trace: {ex.InnerException.StackTrace}");
                }
            }
        }
    }

    #endregion

    public void Dispose()
    {
        
        _cancellationTokenSource.Cancel();
        
        try
        {
            _processingTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
        {
            // Expected
        }
        
        _cancellationTokenSource?.Dispose();
        _semaphore?.Dispose();
        
    }
}

// Data structure for queued packets
public struct PacketData
{
    public NetPeer Peer;
    public byte[] Data;
    public int DataLength;
    public byte Channel;
    public DeliveryMethod DeliveryMethod;
}

// Data structure for main thread events
public struct MainThreadEvent
{
    public Action Action;
}
