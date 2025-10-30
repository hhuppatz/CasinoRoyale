# Inventory Networking Integration Guide

## Overview

The inventory system is fully network-compatible using a **client-server architecture** where:
- **Clients send requests** to the host for inventory actions
- **Host processes requests authoritatively** and validates them
- **Host broadcasts results** to all clients including the requestor

## Architecture Flow

### Pickup Flow
```
Client: Press E near sword
  ↓
Client: Send ItemPickupRequestPacket → Host
  ↓
Host: Validate & Process pickup
  ↓
Host: Add to inventory & destroy item
  ↓
Host: Broadcast ItemPickupBroadcastPacket → All Clients
  ↓
All Clients: Update inventory & remove item from world
```

### Drop Flow
```
Client: Press Q to drop item
  ↓
Client: Send ItemDropRequestPacket → Host
  ↓
Host: Validate & Process drop
  ↓
Host: Remove from inventory & spawn item
  ↓
Host: Broadcast ItemDropBroadcastPacket → All Clients
  ↓
All Clients: Update inventory & spawn item in world
```

### Use Flow
```
Client: Press I to use item
  ↓
Client: Send ItemUseRequestPacket → Host
  ↓
Host: Validate & Execute use strategy
  ↓
Host: Broadcast ItemUseBroadcastPacket → All Clients
  ↓
All Clients: Execute use strategy
```

## Network Packets

### Request Packets (Client → Host)

#### ItemPickupRequestPacket
```csharp
{
    uint playerId;
    uint itemId;
}
```

#### ItemDropRequestPacket
```csharp
{
    uint playerId;
    byte itemType;
    Vector2 dropPosition;
    Vector2 dropVelocity;
}
```

#### ItemUseRequestPacket
```csharp
{
    uint playerId;
    byte itemType;
}
```

### Broadcast Packets (Host → All Clients)

#### ItemPickupBroadcastPacket
```csharp
{
    uint playerId;
    uint itemId;
    byte itemType;
    bool success;  // false if inventory was full
}
```

#### ItemDropBroadcastPacket
```csharp
{
    uint playerId;
    byte itemType;
    uint newItemId;
    ItemState newItemState;  // Full state for spawning
}
```

#### ItemUseBroadcastPacket
```csharp
{
    uint playerId;
    byte itemType;
}
```

## Integration Steps

### Step 1: Initialize InventoryNetworkHandler

In your `HostGameState.cs` or `ClientGameState.cs`, after establishing network connection:

```csharp
// In HostGameState or ClientGameState after network setup
InventoryNetworkHandler.Initialize(
    relayPeer: _relayManager.RelayServerPeer,
    packetProcessor: _packetProcessor,
    players: _players,  // Dictionary<uint, PlayableCharacter>
    gameWorld: GameWorld,
    isHost: NetworkManager.Instance.IsHost
);
```

### Step 2: Register Packet Handlers

The `InventoryNetworkHandler.Initialize()` method automatically registers:
- **Host**: Handlers for request packets
- **Client**: Handlers for broadcast packets

### Step 3: Verify Player Dictionary

Ensure your GameState has a player dictionary accessible to the handler:

```csharp
private Dictionary<uint, PlayableCharacter> _players = new();
```

## How It Works

### Host Authority Model

1. **All inventory actions are validated by the host**
   - Inventory full checks
   - Item existence verification
   - Player existence verification

2. **Host is the source of truth**
   - Host processes all requests
   - Host broadcasts results
   - Clients never directly modify inventory for remote players

3. **Broadcasts include the requesting client**
   - Client sees their own action reflected via broadcast
   - Ensures consistency across all clients
   - Prevents desyncs

### Network Handler Methods

#### For Clients (called by PlayableCharacter)
- `SendPickupRequest(playerId, itemId)`
- `SendDropRequest(playerId, itemType, position, velocity)`
- `SendUseRequest(playerId, itemType)`

#### For Host (called automatically by handlers)
- `OnPickupRequest()` - Processes client pickup request
- `OnDropRequest()` - Processes client drop request
- `OnUseRequest()` - Processes client use request

#### For Host (called by PlayableCharacter after processing)
- `BroadcastPickup(playerId, itemId, itemType, success)`
- `BroadcastDrop(playerId, itemType, newItem)`
- `BroadcastUse(playerId, itemType)`

### Automatic Behavior in PlayableCharacter

The `PlayableCharacter` class automatically determines whether to:
- **Process locally (Host)**: Call `ProcessItemPickup()`, `ProcessItemDrop()`, etc.
- **Send request (Client)**: Call `SendPickupRequest()`, `SendDropRequest()`, etc.

This is handled via:
```csharp
if (NetworkManager.Instance.IsHost)
{
    // Host processes immediately
    ProcessItemPickup(item, gameWorld);
}
else
{
    // Client sends request
    SendPickupRequest(item.ItemId);
}
```

## Testing Checklist

### Single Player (Host)
- [ ] Can pick up items with E
- [ ] Can auto-collect coins
- [ ] Can drop items with Q
- [ ] Can use items with I
- [ ] Items spawn/despawn correctly

### Multiplayer (Host + Client)

**From Client Perspective:**
- [ ] Client can pick up items
- [ ] Client sees items disappear when picked up
- [ ] Client can drop items
- [ ] Client sees items spawn when dropped
- [ ] Client can use items
- [ ] Client sees other players' inventory actions

**From Host Perspective:**
- [ ] Host receives client requests
- [ ] Host processes requests correctly
- [ ] Host broadcasts to all clients
- [ ] Host sees client inventory actions
- [ ] Host can perform own inventory actions

**Consistency:**
- [ ] All clients see same item states
- [ ] Inventories stay synchronized
- [ ] No duplicate items
- [ ] No ghost items
- [ ] Network lag doesn't cause desyncs

## Common Issues & Solutions

### Issue: Packets not being received
**Solution**: Ensure `InventoryNetworkHandler.Initialize()` is called after network connection is established.

### Issue: Items not spawning on client
**Solution**: Check that `ItemState` serialization is working correctly in `ItemDropBroadcastPacket`.

### Issue: Inventory desyncs
**Solution**: Ensure clients never directly call `ProcessItemPickup/Drop/Use()` - they should only send requests.

### Issue: Host doesn't see client actions
**Solution**: Verify packet handlers are registered correctly and `_packetProcessor.ReadAllPackets()` is being called.

## Future Enhancements

- **Inventory synchronization packets**: Send full inventory state periodically for reconciliation
- **Latency compensation**: Predict client actions optimistically
- **Request timeouts**: Detect and handle failed requests
- **Cheat prevention**: Server-side validation of inventory operations
- **Inventory state in JoinAcceptPacket**: Send inventory state to joining players

## Code Locations

- **Packets**: `Classes/Networking/Packets.cs`
- **Network Handler**: `Classes/Networking/InventoryNetworkHandler.cs`
- **Player Integration**: `Classes/GameObjects/Player/PlayableCharacter.cs`
- **Inventory Logic**: `Classes/GameObjects/Player/Inventory.cs`

## Example: Adding to HostGameState

```csharp
public override void Initialize()
{
    base.Initialize();
    
    // ... existing network setup ...
    
    // Initialize inventory network handler
    InventoryNetworkHandler.Initialize(
        _relayManager.RelayServerPeer,
        _packetProcessor,
        _players,
        GameWorld,
        isHost: true
    );
    
    Logger.Info("Inventory networking initialized for Host");
}
```

## Example: Adding to ClientGameState

```csharp
public override void Initialize()
{
    base.Initialize();
    
    // ... existing network setup ...
    
    // Initialize inventory network handler
    InventoryNetworkHandler.Initialize(
        _relayManager.RelayServerPeer,
        _packetProcessor,
        _players,
        GameWorld,
        isHost: false
    );
    
    Logger.Info("Inventory networking initialized for Client");
}
```
