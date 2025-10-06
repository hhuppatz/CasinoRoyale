using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using CasinoRoyale.Classes.GameObjects;
using CasinoRoyale.Utils;
using CasinoRoyale.Classes.GameObjects.CasinoMachines;
using CasinoRoyale.Classes.GameObjects.Platforms;

namespace CasinoRoyale.Classes.GameSystems
{
public struct CollisionResult(Vector2 movement, bool horizontalBlocked, bool verticalBlocked)
{
    public Vector2 movement = movement;
    public bool horizontalBlocked = horizontalBlocked;
    public bool verticalBlocked = verticalBlocked;
}

public class PhysicsSystem
{
    public readonly float GRAVITY = 9.8f;
    private static PhysicsSystem _instance;
    
    public static PhysicsSystem Instance 
    { 
        get 
        { 
            if (_instance == null)
                throw new InvalidOperationException("CollisionSystem has not been initialized. Call Initialize() first.");
            return _instance; 
        } 
    }

    private readonly Properties _gameProperties;
    
    private PhysicsSystem(Properties gameProperties)
    {
        _gameProperties = gameProperties;
        GRAVITY = float.Parse(_gameProperties.get("gravity"));
    }

    public static void Initialize(Properties gameProperties)
    {
        _instance = new PhysicsSystem(gameProperties);
    }

    // Coordinate system has 0,0 in top left and grows more positive to the right and down
    public static void EnforceMovementRules(Rectangle gameArea, GameWorldObjects gameWorldObjects, PlayableCharacter player, float dt)
    {
        EnforcePlayerGravity(gameArea, gameWorldObjects, player, dt);

        // Calculate attempted movement
        Vector2 attemptedMovement = player.Velocity * dt;
        
        // Check and resolve collisions, getting the maximum allowed movement and collision info
        var collisionResult = ResolveCollisions(gameWorldObjects, player, attemptedMovement);
        Vector2 realMovement = collisionResult.movement;
        
        // Apply the resolved movement to the player
        player.Coords += realMovement;
        
        // Handle velocity changes based on collision response
        if (collisionResult.horizontalBlocked && Math.Abs(player.Velocity.X) > 0.1f)
        {
            // Stop horizontal velocity when hitting a wall
            player.Velocity = new Vector2(0, player.Velocity.Y);
        }
        
        if (collisionResult.verticalBlocked)
        {
            // Handle vertical collision response
            if (player.Velocity.Y < 0 && Math.Abs(realMovement.Y) < Math.Abs(attemptedMovement.Y))
            {
                // Player was moving upward and hit ceiling/platform from below - stop upward velocity
                player.Velocity = new Vector2(player.Velocity.X, 0);
            }
            else if (player.Velocity.Y > 0 && Math.Abs(realMovement.Y) < Math.Abs(attemptedMovement.Y))
            {
                // Player was moving downward and hit ground/platform from above - stop downward velocity
                player.Velocity = new Vector2(player.Velocity.X, 0);
                
                // Ensure player is positioned above any platform they landed on
                EnsurePlayerAbovePlatforms(gameWorldObjects, player);
            }
        }
        
        // Keep player's hitbox within game area bounds
        Rectangle playerHitbox = player.Hitbox;
        
        // Calculate the maximum allowed coordinates to keep the entire hitbox within bounds
        float maxX = gameArea.Right - playerHitbox.Width;
        float maxY = gameArea.Bottom - playerHitbox.Height;
        float minX = gameArea.Left;
        float minY = gameArea.Top;
        
        // Clamp coordinates to ensure hitbox stays within game area
        player.Coords = Vector2.Min(Vector2.Max(player.Coords, new Vector2(minX, minY)), new Vector2(maxX, maxY));        
    }
    
    private static CollisionResult ResolveCollisions(GameWorldObjects gameWorldObjects, PlayableCharacter player, Vector2 attemptedMovement)
    {
        // Simple and reliable collision resolution using step-by-step movement
        Vector2 resolvedMovement = Vector2.Zero;
        bool horizontalBlocked = false;
        bool verticalBlocked = false;
        
        // Test if full diagonal movement is safe
        Rectangle diagonalTestHitbox = new(
            player.Hitbox.X + (int)attemptedMovement.X,
            player.Hitbox.Y + (int)attemptedMovement.Y,
            player.Hitbox.Width,
            player.Hitbox.Height
        );
        
        // Check if diagonal movement causes any collision
        bool hasDiagonalCollision = false;
        foreach (var platform in gameWorldObjects.Platforms ?? [])
        {
            if (diagonalTestHitbox.Intersects(platform.Hitbox))
            {
                hasDiagonalCollision = true;
                break;
            }
        }
        
        if (!hasDiagonalCollision)
        {
            // No collision, allow full movement
            return new CollisionResult(attemptedMovement, false, false);
        }
        
        // Resolve horizontal movement first (no vertical movement during horizontal test)
        if (attemptedMovement.X != 0)
        {
            float originalX = attemptedMovement.X;
            resolvedMovement.X = ResolveAxisMovement(gameWorldObjects, player, attemptedMovement.X, 0f, true);
            
            // Check if horizontal movement was blocked
            if (Math.Abs(resolvedMovement.X) < Math.Abs(originalX))
            {
                horizontalBlocked = true;
            }
        }
        
        // Resolve vertical movement using the RESOLVED horizontal position
        if (attemptedMovement.Y != 0)
        {
            float originalY = attemptedMovement.Y;
            resolvedMovement.Y = ResolveAxisMovement(gameWorldObjects, player, resolvedMovement.X, attemptedMovement.Y, false);
            
            // Check if vertical movement was blocked
            if (Math.Abs(resolvedMovement.Y) < Math.Abs(originalY))
            {
                verticalBlocked = true;
            }
        }
        
        return new CollisionResult(resolvedMovement, horizontalBlocked, verticalBlocked);
    }
    
    private static float ResolveAxisMovement(GameWorldObjects gameWorldObjects, PlayableCharacter player, float xMovement, float yMovement, bool isHorizontal)
    {
        float movementDistance = isHorizontal ? xMovement : yMovement;
        
        // If no movement, return 0
        if (Math.Abs(movementDistance) < 0.001f) return 0f;
        
        // Get all potential colliders (platforms)
        var platforms = gameWorldObjects.Platforms;
        if (platforms == null || platforms.Count == 0) return movementDistance;
        
        // Check if the full movement is safe first
        Rectangle fullMovementHitbox;
        if (isHorizontal)
        {
            fullMovementHitbox = new(
                player.Hitbox.X + (int)movementDistance,
                player.Hitbox.Y + (int)yMovement,
                player.Hitbox.Width,
                player.Hitbox.Height
            );
        }
        else
        {
            fullMovementHitbox = new(
                player.Hitbox.X + (int)xMovement,
                player.Hitbox.Y + (int)movementDistance,
                player.Hitbox.Width,
                player.Hitbox.Height
            );
        }
        
        // Check if full movement causes any collision
        bool hasFullMovementCollision = false;
        foreach (var platform in platforms)
        {
            if (fullMovementHitbox.Intersects(platform.Hitbox))
            {
                hasFullMovementCollision = true;
                break;
            }
        }
        
        // If no collision with full movement, allow it
        if (!hasFullMovementCollision) return movementDistance;
        
        // Use step-by-step movement instead of binary search for more reliable results
        float stepSize = Math.Sign(movementDistance); // 1 or -1
        float currentMovement = 0f;
        
        // Step through the movement one pixel at a time
        while (Math.Abs(currentMovement) < Math.Abs(movementDistance))
        {
            float nextMovement = currentMovement + stepSize;
            
            // Create test hitbox for next position
            Rectangle testHitbox;
            if (isHorizontal)
            {
                testHitbox = new(
                    player.Hitbox.X + (int)nextMovement,
                    player.Hitbox.Y + (int)yMovement,
                    player.Hitbox.Width,
                    player.Hitbox.Height
                );
            }
            else
            {
                testHitbox = new(
                    player.Hitbox.X + (int)xMovement,
                    player.Hitbox.Y + (int)nextMovement,
                    player.Hitbox.Width,
                    player.Hitbox.Height
                );
            }
            
            // Check if this step would cause a collision
            bool hasCollision = false;
            foreach (var platform in platforms)
            {
                if (testHitbox.Intersects(platform.Hitbox))
                {
                    hasCollision = true;
                    break;
                }
            }
            
            if (hasCollision)
            {
                // Collision detected, stop at current position
                break;
            }
            
            // No collision, continue to next step
            currentMovement = nextMovement;
        }
        
        return currentMovement;
    }

    public static void EnforcePlayerGravity(Rectangle gameArea, GameWorldObjects gameWorldObjects, PlayableCharacter player, float dt)
    {
        if (!IsPlayerGrounded(gameArea, gameWorldObjects, player))
        {
            player.Velocity += new Vector2(0, CasinoRoyale.Classes.GameSystems.PhysicsSystem.Instance.GRAVITY * player.Mass * dt);
        }
    }
    
    public static bool IsPlayerGrounded(Rectangle gameArea, GameWorldObjects gameWorldObjects, PlayableCharacter player)
    {
        // Check if player is at the bottom of the game area (ground)
        bool atBottom = player.Hitbox.Bottom >= gameArea.Bottom;
        if (atBottom)
        {
            return true;
        }
        
        Rectangle belowPlayerHitbox = new(
            player.Hitbox.X,
            player.Hitbox.Y + player.Hitbox.Height,
            player.Hitbox.Width,
            8  // Increased from 2 to 8 pixels for more reliable ground detection
        );
        
        // Check if player is grounded on a platform
        foreach (Platform platform in gameWorldObjects.Platforms)
        {
            if (belowPlayerHitbox.Intersects(platform.Hitbox))
            {
                return true;
            }
        }
        
        return false;
    }
    
    private static void EnsurePlayerAbovePlatforms(GameWorldObjects gameWorldObjects, PlayableCharacter player)
    {
        // Check all platforms and ensure player is positioned above any that they intersect with
        foreach (var platform in gameWorldObjects.Platforms)
        {
            if (player.Hitbox.Intersects(platform.Hitbox))
            {
                // Player is overlapping with platform - push them above it
                float newY = platform.Hitbox.Y - player.Hitbox.Height - 1; // 1 pixel gap
                player.Coords = new Vector2(player.Coords.X, newY);
                break; // Only adjust for the first overlapping platform
            }
        }
    }
    
}
}