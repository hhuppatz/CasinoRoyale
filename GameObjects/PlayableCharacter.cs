using System.Linq;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using CasinoRoyale.GameObjects.Interfaces;
using CasinoRoyale.Players.Common.Networking;

namespace CasinoRoyale.GameObjects
{
    public class PlayableCharacter(uint pid, string username, Texture2D tex, Vector2 coords, Vector2 velocity, Rectangle hitbox, bool awake) : GameEntity(coords, velocity, hitbox, awake), CasinoRoyale.GameObjects.Interfaces.IDrawable, IJump
{
    private static float standardJumpSquatTime = 0.1f;
    private readonly uint pid = pid;
    private readonly string username = username;
    private Texture2D tex = tex;
    private float mass = 5f;
    public float Mass { get => mass; set => mass = value; }
    private float initialJumpVelocity = 120;
    public float InitialJumpVelocity { get => initialJumpVelocity; set => initialJumpVelocity = value; }
    private float maxRunSpeed = 240f;
    public float MaxRunSpeed { get => maxRunSpeed; set => maxRunSpeed = value; }
    // Jump data
    private bool inJumpSquat = false;
    private float jumpSquatTimer = standardJumpSquatTime;
    private bool inJump = false;
    //private float jumpTimer = 0f;
    public bool InJumpSquat { get => inJumpSquat; }
    public float JumpSquatTimer { get => jumpSquatTimer; set => jumpSquatTimer = value; }
    public bool InJump { get => inJump; set => inJump = value; }

    bool IJump.InJumpSquat { get => InJumpSquat; set => throw new System.NotImplementedException(); }

    public void TryMovePlayer(KeyboardState ks, float dt)
    {
        bool m_playerAttemptedJump = false;
        
        if (ks.IsKeyDown(Keys.A)) // Can be held
        {
            // Horizontal movement should be instant (no acceleration)
            Coords = new Vector2(Coords.X - MaxRunSpeed * dt, Coords.Y);
        }
        if (ks.IsKeyDown(Keys.D)) // Can be held
        {
            // Horizontal movement should be instant (no acceleration)
            Coords = new Vector2(Coords.X + MaxRunSpeed * dt, Coords.Y);
        }
        if (ks.GetPressedKeys().Contains(Keys.W)) // Needs to be pressed
        {
            m_playerAttemptedJump = true;
        }

        // Update velocity according to forces and movement requests
        PlayerUpdateVelocity(m_playerAttemptedJump, dt);

        // Enforce movement rules from collision system
        CasinoRoyale.GameObjects.PhysicsSystem.Instance.EnforceMovementRules(this, ks, dt);
    }

    public void PlayerUpdateVelocity(bool m_playerAttemptedJump, float dt)
    {
        // Apply gravity directly to player's velocity (frame-rate independent)
        UpdateJump(m_playerAttemptedJump);
        UpdateGravity(dt);
    }

    public void UpdateJump(bool m_playerAttemptedJump)
    {
        // Deal with player jumping - only apply jump velocity once when jump starts
        if (m_playerAttemptedJump && !InJump)
        {
            // Start jump with initial velocity
            InJump = true;
            Velocity = new Vector2(0, -InitialJumpVelocity);
        }

        // Check if player has landed after falling (end jump state)
        if (InJump && Velocity.Y >= 0 && CasinoRoyale.GameObjects.PhysicsSystem.Instance.IsPlayerGrounded(this)) InJump = false;

    }

    public void UpdateGravity(float dt)
    {
        if (Velocity.Y < 0 || !CasinoRoyale.GameObjects.PhysicsSystem.Instance.IsPlayerGrounded(this))
            Velocity += new Vector2(0, CasinoRoyale.GameObjects.PhysicsSystem.Instance.GRAVITY * Mass * dt);
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
}