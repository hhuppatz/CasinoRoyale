using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class PlayableCharacter : GameEntity, IDrawable, IJump
{
    public float InitialJumpVelocity = 200f;
    private static float standardJumpSquatTime = 0.1f;
    private uint pid;
    private string username;
    private Texture2D tex;
    private float mass = 5f;
    public float Mass { get => mass; set => mass = value; }
    // Jump data
    private bool inJumpSquat = false;
    private float jumpSquatTimer = standardJumpSquatTime;
    private bool inJump = false;
    //private float jumpTimer = 0f;
    public bool InJumpSquat { get => inJumpSquat; }
    public float JumpSquatTimer { get => jumpSquatTimer; set => jumpSquatTimer = value; }
    public bool InJump { get => inJump;
                         set
                        { if (!inJump && inJumpSquat && value && Velocity.Y <= 0f)
                            {
                                // Reset for next jump
                                inJumpSquat = false;
                                JumpSquatTimer = standardJumpSquatTime;
                                inJump = true;
                            }
                            else if (!inJumpSquat && value && Velocity.Y <= 0f)
                            {
                                inJumpSquat = true;
                            }}}

    bool IJump.InJumpSquat { get => InJumpSquat; set => throw new System.NotImplementedException(); }

    public PlayableCharacter(uint pid, string username, Texture2D tex, Vector2 coords, Vector2 velocity, Rectangle hitbox, bool awake)
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
        Coords = playerState.ges.coords;
        Velocity = playerState.ges.velocity;
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