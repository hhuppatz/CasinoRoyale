using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using CasinoRoyale.Classes.GameObjects.Player;
using CasinoRoyale.Classes.GameObjects.Items;
using CasinoRoyale.Classes.GameSystems;
using CasinoRoyale.Utils;

namespace CasinoRoyale.Classes.Networking;

/// <summary>
/// Singleton handler for inventory-related network packets
/// Manages client requests and host broadcasts for item pickup/drop/use
/// </summary>
public class InventoryNetworkHandler
{
    private static InventoryNetworkHandler _instance;
    public static InventoryNetworkHandler Instance => _instance;
    
    private NetPeer _relayPeer;
    private AsyncPacketProcessor _packetProcessor;
    private Dictionary<uint, PlayableCharacter> _players;
    private GameWorld _gameWorld;
    private bool _isHost;
    
    private InventoryNetworkHandler() { }
    
    /// <summary>
    /// Initialize the inventory network handler
    /// Must be called before use
    /// </summary>
    public static void Initialize(
        NetPeer relayPeer,
        AsyncPacketProcessor packetProcessor,
        Dictionary<uint, PlayableCharacter> players,
        GameWorld gameWorld,
        bool isHost)
    {
        if (_instance == null)
        {
            _instance = new InventoryNetworkHandler();
        }
        
        _instance._relayPeer = relayPeer;
        _instance._packetProcessor = packetProcessor;
        _instance._players = players;
        _instance._gameWorld = gameWorld;
        _instance._isHost = isHost;
        
        // Register packet handlers
        _instance.RegisterPacketHandlers();
        
        Logger.LogNetwork("INVENTORY", $"Initialized as {(isHost ? "Host" : "Client")}");
    }
    
    private void RegisterPacketHandlers()
    {
        if (_isHost)
        {
            // Host handles requests from clients
            _packetProcessor.SubscribeReusable<ItemPickupRequestPacket, ulong>(OnPickupRequest);
            _packetProcessor.SubscribeReusable<ItemDropRequestPacket, ulong>(OnDropRequest);
            _packetProcessor.SubscribeReusable<ItemUseRequestPacket, ulong>(OnUseRequest);
        }
        else
        {
            // Clients handle broadcasts from host
            _packetProcessor.SubscribeReusable<ItemPickupBroadcastPacket, ulong>(OnPickupBroadcast);
            _packetProcessor.SubscribeReusable<ItemDropBroadcastPacket, ulong>(OnDropBroadcast);
            _packetProcessor.SubscribeReusable<ItemUseBroadcastPacket, ulong>(OnUseBroadcast);
        }
    }
    
    // ==================== CLIENT -> HOST REQUESTS ====================
    
    /// <summary>
    /// Send pickup request to host (called by client)
    /// </summary>
    public void SendPickupRequest(uint playerId, uint itemId)
    {
        if (_isHost) return; // Host doesn't send requests to itself
        
        var request = new ItemPickupRequestPacket
        {
            playerId = playerId,
            itemId = itemId
        };
        
        var writer = new NetDataWriter();
        _packetProcessor.Write(writer, request);
        _relayPeer?.Send(writer, DeliveryMethod.ReliableOrdered);
        
        Logger.LogNetwork("INVENTORY", $"Client sent pickup request: player={playerId}, item={itemId}");
    }
    
    /// <summary>
    /// Send drop request to host (called by client)
    /// </summary>
    public void SendDropRequest(uint playerId, ItemType itemType, Microsoft.Xna.Framework.Vector2 dropPosition, Microsoft.Xna.Framework.Vector2 dropVelocity)
    {
        if (_isHost) return; // Host doesn't send requests to itself
        
        var request = new ItemDropRequestPacket
        {
            playerId = playerId,
            itemType = (byte)itemType,
            dropPosition = dropPosition,
            dropVelocity = dropVelocity
        };
        
        var writer = new NetDataWriter();
        _packetProcessor.Write(writer, request);
        _relayPeer?.Send(writer, DeliveryMethod.ReliableOrdered);
        
        Logger.LogNetwork("INVENTORY", $"Client sent drop request: player={playerId}, itemType={itemType}");
    }
    
    /// <summary>
    /// Send use request to host (called by client)
    /// </summary>
    public void SendUseRequest(uint playerId, ItemType itemType)
    {
        if (_isHost) return; // Host doesn't send requests to itself
        
        var request = new ItemUseRequestPacket
        {
            playerId = playerId,
            itemType = (byte)itemType
        };
        
        var writer = new NetDataWriter();
        _packetProcessor.Write(writer, request);
        _relayPeer?.Send(writer, DeliveryMethod.ReliableOrdered);
        
        Logger.LogNetwork("INVENTORY", $"Client sent use request: player={playerId}, itemType={itemType}");
    }
    
    // ==================== HOST REQUEST HANDLERS ====================
    
    /// <summary>
    /// Host handles pickup request from client
    /// </summary>
    private void OnPickupRequest(ItemPickupRequestPacket request, ulong clientId)
    {
        if (!_isHost) return;
        
        Logger.LogNetwork("INVENTORY", $"Host received pickup request: player={request.playerId}, item={request.itemId}");
        
        // Find the player and item
        if (!_players.TryGetValue(request.playerId, out var player))
        {
            Logger.Warning($"Player {request.playerId} not found for pickup request");
            return;
        }
        
        var item = _gameWorld.GetItemById(request.itemId);
        if (item == null)
        {
            Logger.Warning($"Item {request.itemId} not found for pickup request");
            return;
        }
        
        // Process pickup on host
        bool success = player.ProcessItemPickup(item, _gameWorld);
        
        // Broadcast result to all clients
        BroadcastPickup(request.playerId, request.itemId, item.ItemType, success);
    }
    
    /// <summary>
    /// Host handles drop request from client
    /// </summary>
    private void OnDropRequest(ItemDropRequestPacket request, ulong clientId)
    {
        if (!_isHost) return;
        
        Logger.LogNetwork("INVENTORY", $"Host received drop request: player={request.playerId}, itemType={request.itemType}");
        
        // Find the player
        if (!_players.TryGetValue(request.playerId, out var player))
        {
            Logger.Warning($"Player {request.playerId} not found for drop request");
            return;
        }
        
        var itemType = (ItemType)request.itemType;
        
        // Process drop on host
        bool success = player.ProcessItemDrop(itemType, request.dropPosition, request.dropVelocity, _gameWorld);
        
        if (success)
        {
            // Get the newly spawned item
            var newItem = _gameWorld.AllItems[_gameWorld.AllItems.Count - 1]; // Last spawned item
            
            // Broadcast drop to all clients
            BroadcastDrop(request.playerId, itemType, newItem);
        }
    }
    
    /// <summary>
    /// Host handles use request from client
    /// </summary>
    private void OnUseRequest(ItemUseRequestPacket request, ulong clientId)
    {
        if (!_isHost) return;
        
        Logger.LogNetwork("INVENTORY", $"Host received use request: player={request.playerId}, itemType={request.itemType}");
        
        // Find the player
        if (!_players.TryGetValue(request.playerId, out var player))
        {
            Logger.Warning($"Player {request.playerId} not found for use request");
            return;
        }
        
        var itemType = (ItemType)request.itemType;
        
        // Process use on host
        player.ProcessItemUse(itemType);
        
        // Broadcast use to all clients
        BroadcastUse(request.playerId, itemType);
    }
    
    // ==================== HOST -> ALL BROADCASTS ====================
    
    /// <summary>
    /// Host broadcasts pickup result to all clients
    /// </summary>
    public void BroadcastPickup(uint playerId, uint itemId, ItemType itemType, bool success)
    {
        if (!_isHost) return;
        
        var broadcast = new ItemPickupBroadcastPacket
        {
            playerId = playerId,
            itemId = itemId,
            itemType = (byte)itemType,
            success = success
        };
        
        var writer = new NetDataWriter();
        _packetProcessor.Write(writer, broadcast);
        _relayPeer?.Send(writer, DeliveryMethod.ReliableOrdered);
        
        Logger.LogNetwork("INVENTORY", $"Host broadcast pickup: player={playerId}, item={itemId}, success={success}");
    }
    
    /// <summary>
    /// Host broadcasts drop to all clients
    /// </summary>
    public void BroadcastDrop(uint playerId, ItemType itemType, Item newItem)
    {
        if (!_isHost) return;
        
        var broadcast = new ItemDropBroadcastPacket
        {
            playerId = playerId,
            itemType = (byte)itemType,
            newItemId = newItem.ItemId,
            newItemState = newItem.GetState()
        };
        
        var writer = new NetDataWriter();
        _packetProcessor.Write(writer, broadcast);
        _relayPeer?.Send(writer, DeliveryMethod.ReliableOrdered);
        
        Logger.LogNetwork("INVENTORY", $"Host broadcast drop: player={playerId}, itemType={itemType}, newItemId={newItem.ItemId}");
    }
    
    /// <summary>
    /// Host broadcasts item use to all clients
    /// </summary>
    public void BroadcastUse(uint playerId, ItemType itemType)
    {
        if (!_isHost) return;
        
        var broadcast = new ItemUseBroadcastPacket
        {
            playerId = playerId,
            itemType = (byte)itemType
        };
        
        var writer = new NetDataWriter();
        _packetProcessor.Write(writer, broadcast);
        _relayPeer?.Send(writer, DeliveryMethod.ReliableOrdered);
        
        Logger.LogNetwork("INVENTORY", $"Host broadcast use: player={playerId}, itemType={itemType}");
    }
    
    // ==================== CLIENT BROADCAST HANDLERS ====================
    
    /// <summary>
    /// Client handles pickup broadcast from host
    /// </summary>
    private void OnPickupBroadcast(ItemPickupBroadcastPacket broadcast, ulong senderId)
    {
        if (_isHost) return; // Host doesn't process its own broadcasts
        
        Logger.LogNetwork("INVENTORY", $"Client received pickup broadcast: player={broadcast.playerId}, item={broadcast.itemId}, success={broadcast.success}");
        
        if (!broadcast.success) return;
        
        // Find the player
        if (_players.TryGetValue(broadcast.playerId, out var player))
        {
            var itemType = (ItemType)broadcast.itemType;
            player.GetInventory().TryAddItem(itemType);
        }
        
        // Remove item from world
        _gameWorld.RemoveItemById(broadcast.itemId);
    }
    
    /// <summary>
    /// Client handles drop broadcast from host
    /// </summary>
    private void OnDropBroadcast(ItemDropBroadcastPacket broadcast, ulong senderId)
    {
        if (_isHost) return; // Host doesn't process its own broadcasts
        
        Logger.LogNetwork("INVENTORY", $"Client received drop broadcast: player={broadcast.playerId}, itemType={broadcast.itemType}, newItemId={broadcast.newItemId}");
        
        // Find the player
        if (_players.TryGetValue(broadcast.playerId, out var player))
        {
            var itemType = (ItemType)broadcast.itemType;
            player.GetInventory().TryRemoveItem(itemType);
        }
        
        // Create item from state
        _gameWorld.ProcessItemStates(new[] { broadcast.newItemState });
    }
    
    /// <summary>
    /// Client handles use broadcast from host
    /// </summary>
    private void OnUseBroadcast(ItemUseBroadcastPacket broadcast, ulong senderId)
    {
        if (_isHost) return; // Host doesn't process its own broadcasts
        
        Logger.LogNetwork("INVENTORY", $"Client received use broadcast: player={broadcast.playerId}, itemType={broadcast.itemType}");
        
        // Find the player
        if (_players.TryGetValue(broadcast.playerId, out var player))
        {
            var itemType = (ItemType)broadcast.itemType;
            player.ProcessItemUse(itemType);
        }
    }
}
