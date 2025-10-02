using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using CasinoRoyale.Classes.Networking;

namespace CasinoRoyale.Classes.GameObjects
{
    public class PlayableCharacter(uint pid, string username, Texture2D tex, Vector2 coords, Vector2 velocity, float mass, float initialJumpVelocity, float maxRunSpeed, Rectangle hitbox, bool awake) : GameEntity(coords, velocity, hitbox, awake), CasinoRoyale.Classes.GameObjects.Interfaces.IDrawable
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

    // Simple movement interpolation fields
    private Vector2 targetCoords;
    private Vector2 targetVelocity;
    private float interpolationSpeed = 8.0f; // How fast to interpolate to target position
    
    // State buffer for delayed interpolation
    private struct BufferedState
    {
        public Vector2 coords;
        public Vector2 velocity;
        public float timestamp;
    }
    
    private readonly Queue<BufferedState> stateBuffer = new();
    private float currentTime = 0f;

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
        CasinoRoyale.Classes.GameSystems.PhysicsSystem.Instance.EnforceMovementRules(this, dt);
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
        if (InJump && Velocity.Y >= 0 && CasinoRoyale.Classes.GameSystems.PhysicsSystem.Instance.IsPlayerGrounded(this)) InJump = false;

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

    // Set target position for interpolation (used for network updates)
    public void SetTargetPosition(Vector2 newCoords, Vector2 newVelocity)
    {
        targetCoords = newCoords;
        targetVelocity = newVelocity;
    }
    
    // Add state to buffer for delayed application
    public void AddBufferedState(Vector2 newCoords, Vector2 newVelocity, float timestamp)
    {
        var bufferedState = new BufferedState
        {
            coords = newCoords,
            velocity = newVelocity,
            timestamp = timestamp
        };
        
        stateBuffer.Enqueue(bufferedState);
        
        // Keep buffer size reasonable (max 5 states)
        while (stateBuffer.Count > 5)
        {
            stateBuffer.Dequeue();
        }
        
        // Apply immediately for responsiveness, but also keep in buffer for smoothing
        SetTargetPosition(newCoords, newVelocity);
    }
    
    // Process buffered states and apply delayed ones
    public void ProcessBufferedStates(float dt)
    {
        currentTime += dt;
        
        // Clean up old states from buffer
        while (stateBuffer.Count > 0)
        {
            var oldestState = stateBuffer.Peek();
            
            // Remove states older than 200ms
            if (currentTime - oldestState.timestamp > 0.2f)
            {
                stateBuffer.Dequeue();
            }
            else
            {
                break;
            }
        }
    }
    
    // Initialize target coordinates (call this after player creation)
    public void InitializeTargets()
    {
        targetCoords = Coords;
        targetVelocity = Velocity;
        currentTime = 0f;
    }

    // Update position using interpolation towards target
    public void UpdateInterpolation(float dt)
    {
        // Only interpolate if we have a target set
        if (targetCoords != Vector2.Zero || Vector2.Distance(Coords, targetCoords) > 0.1f)
        {
            // Smooth interpolation towards target position
            Coords = Vector2.Lerp(Coords, targetCoords, interpolationSpeed * dt);
            
            // Smooth interpolation towards target velocity
            Velocity = Vector2.Lerp(Velocity, targetVelocity, interpolationSpeed * dt);
        }
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