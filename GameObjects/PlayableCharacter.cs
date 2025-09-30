using System;
using System.Linq;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using CasinoRoyale.GameObjects.Interfaces;
using CasinoRoyale.Networking;

namespace CasinoRoyale.GameObjects
{
    public class PlayableCharacter(uint pid, string username, Texture2D tex, Vector2 coords, Vector2 velocity, float mass, float initialJumpVelocity, float maxRunSpeed, Rectangle hitbox, bool awake) : GameEntity(coords, velocity, hitbox, awake), CasinoRoyale.GameObjects.Interfaces.IDrawable
{
    private readonly uint pid = pid;
    private readonly string username = username;
    private Texture2D tex = tex;
    private float mass = mass;
    public float Mass { get => mass; set => mass = value; }

    private float initialJumpVelocity = initialJumpVelocity;
    public float InitialJumpVelocity { get => initialJumpVelocity; set => initialJumpVelocity = value; }

    private float maxRunSpeed = maxRunSpeed;
    public float MaxRunSpeed { get => maxRunSpeed; set => maxRunSpeed = value; }

    private bool inJump = false;
    public bool InJump { get => inJump; set => inJump = value; }

    // Include previous keyboard state to check for key releases
    public void TryMovePlayer(KeyboardState ks, KeyboardState previousKs, float dt)
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
        UpdateJump(m_playerAttemptedJump);

        // Enforce movement rules from physics system
        CasinoRoyale.GameObjects.PhysicsSystem.Instance.EnforceMovementRules(this, dt);
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
            ges = GetEntityState(),
            mass = mass,
            initialJumpVelocity = initialJumpVelocity,
            maxRunSpeed = maxRunSpeed
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
    public float mass;
    public float initialJumpVelocity;
    public float maxRunSpeed;

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(pid);
        writer.Put(username);
        writer.Put(ges);
        writer.Put(mass);
        writer.Put(initialJumpVelocity);
        writer.Put(maxRunSpeed);
    }

    public void Deserialize(NetDataReader reader)
    {
        pid = reader.GetUInt();
        username = reader.GetString();
        ges = reader.GetGES();
        mass = reader.GetFloat();
        initialJumpVelocity = reader.GetFloat();
        maxRunSpeed = reader.GetFloat();
    }
}
}