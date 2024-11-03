using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Player : GameEntity
{
    private NetPeer peer;
    private PlayerState playerState;
    private readonly Texture2D tex;

    public Player(NetPeer peer, PlayerState playerState, Texture2D tex, Vector2 maxBaseVelocity)
    : base(true, playerState.ges.coords, playerState.ges.velocity)
    {
        this.peer = peer;
        this.tex = tex;
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