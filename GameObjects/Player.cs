using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Player : GameEntity, IDrawable
{
    private NetPeer peer;
    private uint pid;
    private string username;
    private Texture2D tex;

    public Player(uint pid, string username, Texture2D tex, bool awake, Vector2 coords, Vector2 velocity)
    : base(coords, velocity, new Rectangle(coords.ToPoint() - new Point(tex.Bounds.Width/2, tex.Bounds.Height/2), new Point(tex.Bounds.Width, tex.Bounds.Height)), awake)
    {
        peer = null;
        this.tex = tex;
        this.pid = pid;
        this.username = username;
    }

    public Player(NetPeer peer, uint pid, string username, Texture2D tex, bool awake, Vector2 coords, Vector2 velocity)
    : base(coords, velocity, new Rectangle(coords.ToPoint() - new Point(tex.Bounds.Width/2, tex.Bounds.Height/2), new Point(tex.Bounds.Width, tex.Bounds.Height)), awake)
    {
        this.peer = peer;
        this.tex = tex;
        this.pid = pid;
        this.username = username;
    }

    // setters
    public void SetTex(Texture2D tex)
    {
        this.tex = tex;
    }

    public void SetPlayerState(PlayerState playerState)
    {
        SetCoords(playerState.ges.coords);
        SetVelocity(playerState.ges.velocity);
        if (playerState.ges.awake)
            AwakenEntity();
        else
            SleepEntity();
    }

    // getters
    public string GetUsername()
    {
        return username;
    }
    public Texture2D GetTex()
    {
        return tex;
    }

    public PlayerState GetPlayerState()
    {
        return new PlayerState {
            pid = pid,
            username = username,
            ges = GetEntityState()
        };
    }

    public NetPeer GetPeer()
    {
        return peer;
    }

    public uint GetID()
    {
        return pid;
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