// Packet classes
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using CasinoRoyale.Classes.GameObjects;
using CasinoRoyale.Classes.GameObjects.CasinoMachines;
using CasinoRoyale.Classes.GameObjects.Platforms;

namespace CasinoRoyale.Classes.Networking
{
    public class JoinPacket : INetSerializable
    {
        public string username { get; set; }
        public float playerMass { get; set; }
        public float playerInitialJumpVelocity { get; set; }
        public float playerMaxRunSpeed { get; set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(username);
            writer.Put(playerMass);
            writer.Put(playerInitialJumpVelocity);
            writer.Put(playerMaxRunSpeed);
        }
        
        public void Deserialize(NetDataReader reader)
        {
            username = reader.GetString();
            playerMass = reader.GetFloat();
            playerInitialJumpVelocity = reader.GetFloat();
            playerMaxRunSpeed = reader.GetFloat();
        }
    }

// Sent to new player on accceptance
public class JoinAcceptPacket : INetSerializable {
    public Rectangle gameArea { get; set; }
    public Rectangle playerHitbox { get; set; }
    public PlayerState playerState { get; set; }
    public Vector2 playerVelocity { get; set; }
    public PlayerState[] otherPlayerStates { get; set; }
    public PlatformState[] platformStates { get; set; }
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
            
        writer.Put((ushort)platformStates.Length);
        foreach (var state in platformStates)
            writer.Put(state);
            
        writer.Put((ushort)casinoMachineStates.Length);
        foreach (var state in casinoMachineStates)
            writer.Put(state);
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
            
        ushort platformCount = reader.GetUShort();
        platformStates = new PlatformState[platformCount];
        for (int i = 0; i < platformCount; i++)
            platformStates[i] = reader.Get<PlatformState>();
            
        ushort casinoMachineCount = reader.GetUShort();
        casinoMachineStates = new CasinoMachineState[casinoMachineCount];
        for (int i = 0; i < casinoMachineCount; i++)
            casinoMachineStates[i] = reader.Get<CasinoMachineState>();
    }
}   

public class PlayerSendUpdatePacket : INetSerializable {
    public Vector2 coords { get; set; }
    public Vector2 velocity { get; set; }
    public float dt { get; set; }
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(coords);
        writer.Put(velocity);
        writer.Put(dt);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        coords = reader.GetVector2();
        velocity = reader.GetVector2();
        dt = reader.GetFloat();
    }
}

public class PlayerReceiveUpdatePacket : INetSerializable {
    public PlayerState[] playerStates { get; set; }
    public CasinoMachineState[] casinoMachineStates { get; set; }
    
    public void Serialize(NetDataWriter writer)
    {
        // Serialize arrays with length prefix
        writer.Put((ushort)playerStates.Length);
        foreach (var state in playerStates)
            writer.Put(state);
            
        writer.Put((ushort)casinoMachineStates.Length);
        foreach (var state in casinoMachineStates)
            writer.Put(state);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        // Deserialize arrays with length prefix
        ushort playerCount = reader.GetUShort();
        playerStates = new PlayerState[playerCount];
        for (int i = 0; i < playerCount; i++)
            playerStates[i] = reader.Get<PlayerState>();
            
        ushort casinoMachineCount = reader.GetUShort();
        casinoMachineStates = new CasinoMachineState[casinoMachineCount];
        for (int i = 0; i < casinoMachineCount; i++)
            casinoMachineStates[i] = reader.Get<CasinoMachineState>();
    }
}

public class PlayerJoinedGamePacket : INetSerializable {
    public string new_player_username{ get; set; }
    public PlayerState new_player_state { get; set; }
    public Rectangle new_player_hitbox { get; set; }
    public float new_player_mass { get; set; }
    public float new_player_initialJumpVelocity { get; set; }
    public float new_player_maxRunSpeed { get; set; }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(new_player_username);
        writer.Put(new_player_state);
        writer.Put(new_player_hitbox);
        writer.Put(new_player_mass);
        writer.Put(new_player_initialJumpVelocity);
        writer.Put(new_player_maxRunSpeed);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        new_player_username = reader.GetString();
        new_player_state = reader.Get<PlayerState>();
        new_player_hitbox = reader.GetRectangle();
        new_player_mass = reader.GetFloat();
        new_player_initialJumpVelocity = reader.GetFloat();
        new_player_maxRunSpeed = reader.GetFloat();
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
}