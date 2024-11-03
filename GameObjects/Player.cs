using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Player : GameEntity
{
    private NetPeer peer;
    private PlayerState playerState;
    private Texture2D tex;

    public Player(PlayerState playerState, Texture2D tex)
    {
        peer = null;
        this.tex = tex;
        this.playerState = playerState;
    }

    public Player(NetPeer peer, PlayerState playerState, Texture2D tex)
    {
        this.peer = peer;
        this.tex = tex;
        this.playerState = playerState;
    }

    // setters
    public void SetTex(Texture2D tex)
    {
        this.tex = tex;
    }

    public void SetState(PlayerState playerState)
    {
        this.playerState = playerState;
    }

    // getters
    public string GetUsername()
    {
        return playerState.username;
    }
    public Texture2D GetTex()
    {
        return tex;
    }

    public PlayerState GetState()
    {
        return playerState;
    }

    public NetPeer GetPeer()
    {
        return peer;
    }

    public uint GetID()
    {
        return playerState.pid;
    }

    public void SetCoords(Vector2 coords)
    {
        playerState.ges.coords = coords;
    }

    public void AwakenEntity()
    {
        playerState.ges.awake = true;
    }

    public void SleepEntity()
    {
        playerState.ges.awake = false;
    }

    public GameEntityState GetEntityState()
    {
        return playerState.ges;
    }

    public Vector2 GetCoords()
    {
        return playerState.ges.coords;
    }

    public Vector2 GetVelocity()
    {
        return playerState.ges.velocity;
    }
}

public struct PlayerState : INetSerializable
{
    public uint pid;
    public string username;
    public GameEntityState ges;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(pid);
        writer.Put(username);
        writer.Put(ges);
    }

    public void Deserialize(NetDataReader reader)
    {
        pid = reader.GetUInt();
        username = reader.GetString();
        ges = reader.GetGES();
    }
}