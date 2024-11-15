using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Player : GameEntity, IDrawable
{
    private uint pid;
    private string username;
    private Texture2D tex;

    public Player(uint pid, string username, Texture2D tex, Vector2 coords, Vector2 velocity, Rectangle hitbox, bool awake)
    : base(coords, velocity, hitbox, awake)
    {
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