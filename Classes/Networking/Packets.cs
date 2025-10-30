// Packet classes
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using CasinoRoyale.Classes.GameObjects.Platforms;
using CasinoRoyale.Classes.GameObjects.Items;
using CasinoRoyale.Classes.GameObjects.Player;
using CasinoRoyale.Classes.Networking.SerializingExtensions;

namespace CasinoRoyale.Classes.Networking;
public enum ObjectType : byte
{
    PLATFORM = 0,
    ITEM = 1,
    PLAYABLECHARACTER = 2
}
// Generic object spawn request (client -> host)
public class NetworkObjectSpawnRequestPacket : INetSerializable
{
    public string prefabKey { get; set; }
    public Vector2 position { get; set; }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(prefabKey);
        AsyncPacketProcessor.SerializeVector2(writer, position);
    }

    public void Deserialize(NetDataReader reader)
    {
        prefabKey = reader.GetString();
        position = AsyncPacketProcessor.DeserializeVector2(reader);
    }
}

// Generic object spawn broadcast (host -> all)
public class NetworkObjectSpawnPacket : INetSerializable
{
    public uint objectId { get; set; }
    public string prefabKey { get; set; }
    public Vector2 position { get; set; }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(objectId);
        writer.Put(prefabKey);
        AsyncPacketProcessor.SerializeVector2(writer, position);
    }

    public void Deserialize(NetDataReader reader)
    {
        objectId = reader.GetUInt();
        prefabKey = reader.GetString();
        position = AsyncPacketProcessor.DeserializeVector2(reader);
    }
}

// Generic object transform update using id (client -> host, host -> others)
public class NetworkObjectUpdatePacket : INetSerializable
{
    public uint objectId { get; set; }
    public Vector2 coords { get; set; }
    public Vector2 velocity { get; set; }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(objectId);
        AsyncPacketProcessor.SerializeVector2(writer, coords);
        AsyncPacketProcessor.SerializeVector2(writer, velocity);
    }

    public void Deserialize(NetDataReader reader)
    {
        objectId = reader.GetUInt();
        coords = AsyncPacketProcessor.DeserializeVector2(reader);
        velocity = AsyncPacketProcessor.DeserializeVector2(reader);
    }
}

public class JoinPacket : INetSerializable
{
    public string username { get; set; }
    public float playerMass { get; set; }
    public float playerInitialJumpVelocity { get; set; }
    public float playerStandardSpeed { get; set; }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(username);
        writer.Put(playerMass);
        writer.Put(playerInitialJumpVelocity);
        writer.Put(playerStandardSpeed);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        username = reader.GetString();
        playerMass = reader.GetFloat();
        playerInitialJumpVelocity = reader.GetFloat();
        playerStandardSpeed = reader.GetFloat();
    }
}

// Sent to new player on accceptance
public class JoinAcceptPacket : INetSerializable {
    public uint targetClientId { get; set; }  // Add target client ID to prevent cross-client packet processing
    public Rectangle gameArea { get; set; }
    public Rectangle playerHitbox { get; set; }
    public PlayerState playerState { get; set; }
    public Vector2 playerVelocity { get; set; }
    public PlayerState[] otherPlayerStates { get; set; }
    public ItemState[] itemStates { get; set; }
    public GridTileState[] gridTiles { get; set; }
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(targetClientId);  // Serialize target client ID first
        AsyncPacketProcessor.SerializeRectangle(writer, gameArea);
        AsyncPacketProcessor.SerializeRectangle(writer, playerHitbox);
        AsyncPacketProcessor.SerializePlayerState(writer, playerState);
        AsyncPacketProcessor.SerializeVector2(writer, playerVelocity);
        
        // Serialize arrays with length prefix and null safety
        writer.Put((ushort)(otherPlayerStates?.Length ?? 0));
        if (otherPlayerStates != null)
        {
            foreach (var state in otherPlayerStates)
                AsyncPacketProcessor.SerializePlayerState(writer, state);
        }
            
        writer.Put((ushort)(itemStates?.Length ?? 0));
        if (itemStates != null)
        {
            foreach (var state in itemStates)
                AsyncPacketProcessor.SerializeItemState(writer, state);
        }
        
        writer.Put((ushort)(gridTiles?.Length ?? 0));
        if (gridTiles != null)
        {
            foreach (var t in gridTiles)
                AsyncPacketProcessor.SerializeGridTileState(writer, t);
        }
    }
    
    public void Deserialize(NetDataReader reader)
    {
        targetClientId = reader.GetUInt();  // Deserialize target client ID first
        gameArea = AsyncPacketProcessor.DeserializeRectangle(reader);
        playerHitbox = AsyncPacketProcessor.DeserializeRectangle(reader);
        playerState = AsyncPacketProcessor.DeserializePlayerState(reader);
        playerVelocity = AsyncPacketProcessor.DeserializeVector2(reader);
        
        // Deserialize arrays with length prefix and null safety
        ushort otherPlayerCount = reader.GetUShort();
        otherPlayerStates = otherPlayerCount > 0 ? new PlayerState[otherPlayerCount] : null;
        if (otherPlayerStates != null)
        {
            for (int i = 0; i < otherPlayerCount; i++)
                otherPlayerStates[i] = AsyncPacketProcessor.DeserializePlayerState(reader);
        }

        ushort itemCount = reader.GetUShort();
        itemStates = itemCount > 0 ? new ItemState[itemCount] : null;
        if (itemStates != null)
        {
            for (int i = 0; i < itemCount; i++)
                itemStates[i] = AsyncPacketProcessor.DeserializeItemState(reader);
        }
        
        ushort tileCount = reader.GetUShort();
        gridTiles = tileCount > 0 ? new GridTileState[tileCount] : null;
        if (gridTiles != null)
        {
            for (int i = 0; i < tileCount; i++)
                gridTiles[i] = AsyncPacketProcessor.DeserializeGridTileState(reader);
        }
    }
}   

public class PlayerSendUpdatePacket : INetSerializable {
    public uint playerId { get; set; }  // Add player ID for relay identification
    public Vector2 coords { get; set; }
    public Vector2 velocity { get; set; }
    public float dt { get; set; }
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(playerId);  // Serialize player ID first
        writer.Put(coords.X);
        writer.Put(coords.Y);
        writer.Put(velocity.X);
        writer.Put(velocity.Y);
        writer.Put(dt);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        playerId = reader.GetUInt();  // Deserialize player ID first
        coords = new Vector2(reader.GetFloat(), reader.GetFloat());
        velocity = new Vector2(reader.GetFloat(), reader.GetFloat());
        dt = reader.GetFloat();
    }
}

public class PlayerReceiveUpdatePacket : INetSerializable {
    public PlayerState[] playerStates { get; set; }
    
    public void Serialize(NetDataWriter writer)
    {
        // Serialize player states array with length prefix and null safety
        writer.Put((ushort)(playerStates?.Length ?? 0));
        if (playerStates != null)
        {
            foreach (var state in playerStates)
                AsyncPacketProcessor.SerializePlayerState(writer, state);
        }
    }
    
    public void Deserialize(NetDataReader reader)
    {
        // Deserialize player states array with length prefix and null safety
        ushort playerCount = reader.GetUShort();
        playerStates = playerCount > 0 ? new PlayerState[playerCount] : null;
        if (playerStates != null)
        {
            for (int i = 0; i < playerCount; i++)
                playerStates[i] = AsyncPacketProcessor.DeserializePlayerState(reader);
        }
    }
}

public class PlayerJoinedGamePacket : INetSerializable {
    public string new_player_username{ get; set; }
    public PlayerState new_player_state { get; set; }
    public Rectangle new_player_hitbox { get; set; }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(new_player_username);
        AsyncPacketProcessor.SerializePlayerState(writer, new_player_state);
        AsyncPacketProcessor.SerializeRectangle(writer, new_player_hitbox);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        new_player_username = reader.GetString();
        new_player_state = AsyncPacketProcessor.DeserializePlayerState(reader);
        new_player_hitbox = AsyncPacketProcessor.DeserializeRectangle(reader);
    }
}

public class PlayerLeftGamePacket : INetSerializable {
    public uint pid { get; set; }
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(pid);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        pid = reader.GetUInt();
    }
}

public class ItemUpdatePacket : INetSerializable {
    public ItemState[] itemStates { get; set; }
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put((ushort)(itemStates?.Length ?? 0));
        if (itemStates != null)
        {
            foreach (var state in itemStates)
                AsyncPacketProcessor.SerializeItemState(writer, state);
        }
    }
    
    public void Deserialize(NetDataReader reader)
    {
        ushort itemCount = reader.GetUShort();
        itemStates = itemCount > 0 ? new ItemState[itemCount] : null;
        if (itemStates != null)
        {
            for (int i = 0; i < itemCount; i++)
                itemStates[i] = AsyncPacketProcessor.DeserializeItemState(reader);
        }
    }
}

// Individual object update packets

// Casino machine update packet removed

public class ItemRemovedPacket : INetSerializable
{
    public uint itemId { get; set; }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(itemId);
    }

    public void Deserialize(NetDataReader reader)
    {
        itemId = reader.GetUInt();
    }
}

// Dedicated packet for game world initialization - contains only basic world info
public class GameWorldInitPacket : INetSerializable
{
    public uint targetClientId { get; set; }  // Target client ID for packet filtering
    public Rectangle gameArea { get; set; }
    public ItemState[] itemStates { get; set; }
    public GridTileState[] gridTiles { get; set; }
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(targetClientId);  // Serialize target client ID first
        AsyncPacketProcessor.SerializeRectangle(writer, gameArea);
        
        // Serialize item states
        writer.Put((ushort)(itemStates?.Length ?? 0));
        if (itemStates != null)
        {
            foreach (var state in itemStates)
                AsyncPacketProcessor.SerializeItemState(writer, state);
        }
        
        writer.Put((ushort)(gridTiles?.Length ?? 0));
        if (gridTiles != null)
        {
            foreach (var t in gridTiles)
                AsyncPacketProcessor.SerializeGridTileState(writer, t);
        }
    }
    
    public void Deserialize(NetDataReader reader)
    {
        targetClientId = reader.GetUInt();  // Deserialize target client ID first
        gameArea = AsyncPacketProcessor.DeserializeRectangle(reader);
        
        // Deserialize item states
        ushort itemCount = reader.GetUShort();
        itemStates = itemCount > 0 ? new ItemState[itemCount] : null;
        if (itemStates != null)
        {
            for (int i = 0; i < itemCount; i++)
                itemStates[i] = AsyncPacketProcessor.DeserializeItemState(reader);
        }
        
        ushort tileCount = reader.GetUShort();
        gridTiles = tileCount > 0 ? new GridTileState[tileCount] : null;
        if (gridTiles != null)
        {
            for (int i = 0; i < tileCount; i++)
                gridTiles[i] = AsyncPacketProcessor.DeserializeGridTileState(reader);
        }
    }
}

public struct GridTileState
{
    public GridTileType type { get; set; }
    public Rectangle hitbox { get; set; }
    public Rectangle source { get; set; }
    public bool isSolid { get; set; }
}

// ==================== INVENTORY PACKETS ====================

// Client -> Host: Request to pick up an item
public class ItemPickupRequestPacket : INetSerializable
{
    public uint playerId { get; set; }
    public uint itemId { get; set; }
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(playerId);
        writer.Put(itemId);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        playerId = reader.GetUInt();
        itemId = reader.GetUInt();
    }
}

// Host -> All: Broadcast that a player picked up an item
public class ItemPickupBroadcastPacket : INetSerializable
{
    public uint playerId { get; set; }
    public uint itemId { get; set; }
    public byte itemType { get; set; }  // ItemType enum as byte
    public bool success { get; set; }  // Whether pickup was successful
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(playerId);
        writer.Put(itemId);
        writer.Put(itemType);
        writer.Put(success);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        playerId = reader.GetUInt();
        itemId = reader.GetUInt();
        itemType = reader.GetByte();
        success = reader.GetBool();
    }
}

// Client -> Host: Request to drop an item
public class ItemDropRequestPacket : INetSerializable
{
    public uint playerId { get; set; }
    public byte itemType { get; set; }  // ItemType enum as byte
    public Vector2 dropPosition { get; set; }
    public Vector2 dropVelocity { get; set; }
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(playerId);
        writer.Put(itemType);
        AsyncPacketProcessor.SerializeVector2(writer, dropPosition);
        AsyncPacketProcessor.SerializeVector2(writer, dropVelocity);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        playerId = reader.GetUInt();
        itemType = reader.GetByte();
        dropPosition = AsyncPacketProcessor.DeserializeVector2(reader);
        dropVelocity = AsyncPacketProcessor.DeserializeVector2(reader);
    }
}

// Host -> All: Broadcast that a player dropped an item (and a new item was spawned)
public class ItemDropBroadcastPacket : INetSerializable
{
    public uint playerId { get; set; }
    public byte itemType { get; set; }  // ItemType enum as byte
    public uint newItemId { get; set; }  // ID of the newly spawned item in world
    public ItemState newItemState { get; set; }  // Full state of the new item
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(playerId);
        writer.Put(itemType);
        writer.Put(newItemId);
        AsyncPacketProcessor.SerializeItemState(writer, newItemState);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        playerId = reader.GetUInt();
        itemType = reader.GetByte();
        newItemId = reader.GetUInt();
        newItemState = AsyncPacketProcessor.DeserializeItemState(reader);
    }
}

// Client -> Host: Request to use an item
public class ItemUseRequestPacket : INetSerializable
{
    public uint playerId { get; set; }
    public byte itemType { get; set; }  // ItemType enum as byte
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(playerId);
        writer.Put(itemType);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        playerId = reader.GetUInt();
        itemType = reader.GetByte();
    }
}

// Host -> All: Broadcast that a player used an item
public class ItemUseBroadcastPacket : INetSerializable
{
    public uint playerId { get; set; }
    public byte itemType { get; set; }  // ItemType enum as byte
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(playerId);
        writer.Put(itemType);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        playerId = reader.GetUInt();
        itemType = reader.GetByte();
    }
}

// Dedicated packet for platform initialization - removed; grid is authoritative