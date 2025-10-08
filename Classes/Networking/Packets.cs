// Packet classes
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using CasinoRoyale.Classes.GameObjects;
using CasinoRoyale.Classes.GameObjects.CasinoMachines;
using CasinoRoyale.Classes.GameObjects.Platforms;

namespace CasinoRoyale.Classes.Networking
{
    public enum ObjectType : byte
    {
        PLATFORM = 0,
        COIN = 1,
        PLAYABLECHARACTER = 2,
        CASINOMACHINE = 3
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
    public Rectangle gameArea { get; set; }
    public Rectangle playerHitbox { get; set; }
    public PlayerState playerState { get; set; }
    public Vector2 playerVelocity { get; set; }
    public PlayerState[] otherPlayerStates { get; set; }
    public CoinState[] coinStates { get; set; }
    public CasinoMachineState[] casinoMachineStates { get; set; }
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(gameArea);
        writer.Put(playerHitbox);
        writer.Put(playerState);
        writer.Put(playerVelocity);
        
        // Serialize arrays with length prefix
        writer.Put((ushort)otherPlayerStates.Length);
        foreach (var state in otherPlayerStates)
            writer.Put(state);
            
        writer.Put((ushort)coinStates.Length);
        foreach (var state in coinStates)
            state.Serialize(writer);
            
        writer.Put((ushort)casinoMachineStates.Length);
        foreach (var state in casinoMachineStates)
            state.Serialize(writer);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        gameArea = reader.GetRectangle();
        playerHitbox = reader.GetRectangle();
        playerState = reader.Get<PlayerState>();
        playerVelocity = reader.GetVector2();
        
        // Deserialize arrays with length prefix
        ushort otherPlayerCount = reader.GetUShort();
        otherPlayerStates = new PlayerState[otherPlayerCount];
        for (int i = 0; i < otherPlayerCount; i++)
            otherPlayerStates[i] = reader.Get<PlayerState>();
            
        ushort coinCount = reader.GetUShort();
        coinStates = new CoinState[coinCount];
        for (int i = 0; i < coinCount; i++)
        {
            coinStates[i] = new CoinState();
            coinStates[i].Deserialize(reader);
        }
        
        ushort casinoMachineCount = reader.GetUShort();
        casinoMachineStates = new CasinoMachineState[casinoMachineCount];
        for (int i = 0; i < casinoMachineCount; i++)
        {
            casinoMachineStates[i] = new CasinoMachineState();
            casinoMachineStates[i].Deserialize(reader);
        }
    }
}   

public class PlayerSendUpdatePacket : INetSerializable {
    public Vector2 coords { get; set; }
    public Vector2 velocity { get; set; }
    public float dt { get; set; }
    public CasinoMachineState[] casinoMachineStates { get; set; }
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(coords);
        writer.Put(velocity);
        writer.Put(dt);
        
        // Serialize casino machine states
        writer.Put((ushort)(casinoMachineStates?.Length ?? 0));
        if (casinoMachineStates != null)
        {
            foreach (var state in casinoMachineStates)
                state.Serialize(writer);
        }
    }
    
    public void Deserialize(NetDataReader reader)
    {
        coords = reader.GetVector2();
        velocity = reader.GetVector2();
        dt = reader.GetFloat();
        
        // Deserialize casino machine states
        ushort casinoMachineCount = reader.GetUShort();
        casinoMachineStates = new CasinoMachineState[casinoMachineCount];
        for (int i = 0; i < casinoMachineCount; i++)
        {
            casinoMachineStates[i] = new CasinoMachineState();
            casinoMachineStates[i].Deserialize(reader);
        }
    }
}

public class PlayerReceiveUpdatePacket : INetSerializable {
    public PlayerState[] playerStates { get; set; }
    
    public void Serialize(NetDataWriter writer)
    {
        // Serialize player states array with length prefix
        writer.Put((ushort)playerStates.Length);
        foreach (var state in playerStates)
            writer.Put(state);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        // Deserialize player states array with length prefix
        ushort playerCount = reader.GetUShort();
        playerStates = new PlayerState[playerCount];
        for (int i = 0; i < playerCount; i++)
            playerStates[i] = reader.Get<PlayerState>();
    }
}

public class PlayerJoinedGamePacket : INetSerializable {
    public string new_player_username{ get; set; }
    public PlayerState new_player_state { get; set; }
    public Rectangle new_player_hitbox { get; set; }
    public float new_player_mass { get; set; }
    public float new_player_initialJumpVelocity { get; set; }
    public float new_player_standardSpeed { get; set; }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(new_player_username);
        writer.Put(new_player_state);
        writer.Put(new_player_hitbox);
        writer.Put(new_player_mass);
        writer.Put(new_player_initialJumpVelocity);
        writer.Put(new_player_standardSpeed);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        new_player_username = reader.GetString();
        new_player_state = reader.Get<PlayerState>();
        new_player_hitbox = reader.GetRectangle();
        new_player_mass = reader.GetFloat();
        new_player_initialJumpVelocity = reader.GetFloat();
        new_player_standardSpeed = reader.GetFloat();
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
    
    public class CoinSpawnedPacket : INetSerializable {
        public CoinState coinState { get; set; }
        
        public void Serialize(NetDataWriter writer)
        {
            coinState.Serialize(writer);
        }
        
        public void Deserialize(NetDataReader reader)
        {
            coinState = new CoinState();
            coinState.Deserialize(reader);
        }
    }


    // Individual object update packets - much more efficient
    public class PlatformUpdatePacket : INetSerializable
    {
        public uint platformId { get; set; }
        public PlatformState platformState { get; set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(platformId);
            platformState.Serialize(writer);
        }

        public void Deserialize(NetDataReader reader)
        {
            platformId = reader.GetUInt();
            platformState = new PlatformState();
            platformState.Deserialize(reader);
        }
    }

    public class CasinoMachineUpdatePacket : INetSerializable
    {
        public uint machineId { get; set; }
        public CasinoMachineState machineState { get; set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(machineId);
            machineState.Serialize(writer);
        }

        public void Deserialize(NetDataReader reader)
        {
            machineId = reader.GetUInt();
            machineState = new CasinoMachineState();
            machineState.Deserialize(reader);
        }
    }


    public class CoinRemovedPacket : INetSerializable
    {
        public uint coinId { get; set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(coinId);
        }

        public void Deserialize(NetDataReader reader)
        {
            coinId = reader.GetUInt();
        }
    }

}