using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using CasinoRoyale.Classes.Networking;
using CasinoRoyale.Classes.Networking.SerializingExtensions;
using CasinoRoyale.Classes.GameSystems;
using CasinoRoyale.Classes.GameObjects.Interfaces;

namespace CasinoRoyale.Classes.GameObjects.Player;

public class PlayableCharacter(uint pid, string username, Texture2D tex, Vector2 coords, Vector2 velocity, float mass, float initialJumpVelocity, float standardSpeed, Rectangle hitbox, bool awake)
    : GameEntity(coords, velocity, hitbox, awake, mass),
    CasinoRoyale.Classes.GameObjects.Interfaces.IDrawable,
    IJump
{
    // PlayableCharacter
    private readonly uint pid = pid;
    private readonly string username = username;
    private Texture2D tex = tex;
    public Texture2D Texture { get => tex; set => tex = value;}

    private float standardSpeed = standardSpeed;
    public float StandardSpeed { get => standardSpeed; set => standardSpeed = value; }

    // Movement interpolation fields for inbetween network updates
    private Vector2 targetCoords;
    private Vector2 targetVelocity;
    private readonly float interpolationSpeed = 8.0f; // How fast to interpolate to target position
    
    // State buffer for delayed interpolation for responsiveness
    private struct BufferedState
    {
        public Vector2 coords;
        public Vector2 velocity;
        public float timestamp;
    }
    
    private readonly Queue<BufferedState> stateBuffer = new(); // Queue for the state buffer
    private float currentTime = 0f;

    // IJump
    private bool inJump = false;
    public bool InJump { get => inJump; set => inJump = value; }
    public bool InJumpSquat { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    private float initialJumpVelocity = initialJumpVelocity;
    public float InitialJumpVelocity { get => initialJumpVelocity; set => initialJumpVelocity = value; }

    // Method to mark new player as changed (call after construction)
    public void MarkAsNewPlayer()
    {
        MarkAsChanged();
    }

    // Include previous keyboard state to check for key releases
    public void TryMovePlayer(KeyboardState ks, KeyboardState previousKs, float dt, GameWorld gameWorld)
    {
        bool m_playerAttemptedJump = false;
        
        // Reset horizontal velocity each frame (for responsive controls)
        Velocity = new Vector2(0, Velocity.Y);
        
        if (ks.IsKeyDown(Keys.A)) // Left, Can be held
        {
            Velocity = new Vector2(-StandardSpeed, Velocity.Y);
        }
        if (ks.IsKeyDown(Keys.D)) // Right, Can be held
        {
            Velocity = new Vector2(StandardSpeed, Velocity.Y);
        }
        if ((ks.GetPressedKeys().Contains(Keys.W) && !previousKs.GetPressedKeys().Contains(Keys.W)) ||
            (ks.GetPressedKeys().Contains(Keys.Space) && !previousKs.GetPressedKeys().Contains(Keys.Space)))
        {
            m_playerAttemptedJump = true;
        }
        if (ks.IsKeyDown(Keys.LeftShift)) // Sprint, Can be held
        {
            Velocity = new Vector2(Velocity.X * 1.5f, Velocity.Y);
        }
        
        // Casino machine interaction removed

        // Update velocity according to forces and movement requests
        UpdateJump(m_playerAttemptedJump, gameWorld);

        // Enforce movement rules using grid tiles (new system)
        PhysicsSystem.EnforceMovementRules(gameWorld.GameArea, gameWorld.GetPlatformTileHitboxes(), this, dt);
    }

    public void UpdateJump(bool m_playerAttemptedJump, GameWorld gameWorld)
    {
        // Deal with player jumping - only apply jump velocity once when jump starts
        if (m_playerAttemptedJump
            && !InJump
            && PhysicsSystem.IsPlayerGrounded(gameWorld.GameArea, gameWorld.GetPlatformTileHitboxes(), this))
        {
            // Start jump with initial velocity
            InJump = true;
            Velocity = new Vector2(0, -InitialJumpVelocity);
        }
        // Check if player has landed after falling (end jump state)
        else if (InJump
                && Velocity.Y >= 0
                && PhysicsSystem.IsPlayerGrounded(gameWorld.GameArea, gameWorld.GetPlatformTileHitboxes(), this))
        {
            InJump = false;
        }

    }
    
    // Casino machine interaction removed

    public void SetPlayerState(PlayerState playerState)
    {
        Coords = playerState.ges.coords;
        Velocity = playerState.ges.velocity;
        Mass = playerState.ges.mass;
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
        
        // Don't apply immediately - let the interpolation system handle smooth movement
        // SetTargetPosition(newCoords, newVelocity);
    }
    
    // Process buffered states and apply delayed ones
    public void ProcessBufferedStates(float dt)
    {
        currentTime += dt;
        
        // Clean up old states from buffer and apply the most recent valid state
        BufferedState? mostRecentState = null;
        
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
                // Keep the most recent state
                mostRecentState = oldestState;
                break;
            }
        }
        
        // Apply the most recent valid state if we have one
        if (mostRecentState.HasValue)
        {
            SetTargetPosition(mostRecentState.Value.coords, mostRecentState.Value.velocity);
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

    public string GetUsername()
    {
        return username;
    }

    public PlayerState GetPlayerState()
    {
        return new PlayerState {
            objectType = ObjectType.PLAYABLECHARACTER,
            pid = pid,
            username = username,
            ges = GetEntityState(),
            initialJumpVelocity = initialJumpVelocity,
            maxRunSpeed = standardSpeed
        };
    }

    public uint GetID()
    {
        return pid;
    }

}

public struct PlayerState
{
    public ObjectType objectType;
    public uint pid;
    public string username;
    public GameEntityState ges;
    public float initialJumpVelocity;
    public float maxRunSpeed;
}