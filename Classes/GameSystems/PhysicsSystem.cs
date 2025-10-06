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

public struct PhysicsUpdateResult
{
    public Vector2 newPosition;
    public Vector2 newVelocity;
    public bool isGrounded;
    public bool horizontalBlocked;
    public bool verticalBlocked;
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

    // Generic physics update method that works with any object that has a hitbox
    public static PhysicsUpdateResult UpdatePhysics(Rectangle gameArea, GameWorldObjects gameWorldObjects, 
        Vector2 currentPosition, Vector2 currentVelocity, Rectangle hitbox, float mass, float dt)
    {
        var result = new PhysicsUpdateResult
        {
            newPosition = currentPosition,
            newVelocity = currentVelocity,
            isGrounded = false,
            horizontalBlocked = false,
            verticalBlocked = false
        };

        // Apply gravity if not grounded
        // Create a hitbox positioned at the current position
        Rectangle positionedHitbox = new Rectangle((int)currentPosition.X, (int)currentPosition.Y, hitbox.Width, hitbox.Height);
        bool isGrounded = IsObjectGrounded(gameArea, gameWorldObjects, positionedHitbox);
        result.isGrounded = isGrounded;
        
        if (!isGrounded)
        {
            result.newVelocity += new Vector2(0, Instance.GRAVITY * mass * dt);
        }

        // Calculate attempted movement
        Vector2 attemptedMovement = result.newVelocity * dt;
        
        // Check and resolve collisions
        var collisionResult = ResolveCollisions(gameWorldObjects, currentPosition, positionedHitbox, attemptedMovement);
        result.newPosition += collisionResult.movement;
        result.horizontalBlocked = collisionResult.horizontalBlocked;
        result.verticalBlocked = collisionResult.verticalBlocked;
        
        // Handle velocity changes based on collision response
        if (collisionResult.horizontalBlocked && Math.Abs(result.newVelocity.X) > 0.1f)
        {
            result.newVelocity = new Vector2(0, result.newVelocity.Y);
        }
        
        if (collisionResult.verticalBlocked)
        {
            if (result.newVelocity.Y < 0 && Math.Abs(collisionResult.movement.Y) < Math.Abs(attemptedMovement.Y))
            {
                result.newVelocity = new Vector2(result.newVelocity.X, 0);
            }
            else if (result.newVelocity.Y > 0 && Math.Abs(collisionResult.movement.Y) < Math.Abs(attemptedMovement.Y))
            {
                result.newVelocity = new Vector2(result.newVelocity.X, 0);
                
                // Ensure object is positioned above any platform it landed on
                result.newPosition = EnsureObjectAbovePlatforms(gameWorldObjects, result.newPosition, positionedHitbox);
            }
        }
        
        // Keep object within game area bounds
        result.newPosition = ClampToGameArea(gameArea, result.newPosition, positionedHitbox);
        
        return result;
    }

    // Legacy method for backward compatibility with PlayableCharacter
    public static void EnforceMovementRules(Rectangle gameArea, GameWorldObjects gameWorldObjects, PlayableCharacter player, float dt)
    {
        var physicsResult = UpdatePhysics(gameArea, gameWorldObjects, player.Coords, player.Velocity, player.Hitbox, player.Mass, dt);
        
        // Apply the physics results to the player
        player.Coords = physicsResult.newPosition;
        player.Velocity = physicsResult.newVelocity;
    }
    
    private static CollisionResult ResolveCollisions(GameWorldObjects gameWorldObjects, Vector2 currentPosition, Rectangle hitbox, Vector2 attemptedMovement)
    {
        // Simple and reliable collision resolution using step-by-step movement
        Vector2 resolvedMovement = Vector2.Zero;
        bool horizontalBlocked = false;
        bool verticalBlocked = false;
        
        // Test if full diagonal movement is safe
        Rectangle diagonalTestHitbox = new(
            hitbox.X + (int)attemptedMovement.X,
            hitbox.Y + (int)attemptedMovement.Y,
            hitbox.Width,
            hitbox.Height
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
            resolvedMovement.X = ResolveAxisMovement(gameWorldObjects, currentPosition, hitbox, attemptedMovement.X, 0f, true);
            
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
            resolvedMovement.Y = ResolveAxisMovement(gameWorldObjects, currentPosition, hitbox, resolvedMovement.X, attemptedMovement.Y, false);
            
            // Check if vertical movement was blocked
            if (Math.Abs(resolvedMovement.Y) < Math.Abs(originalY))
            {
                verticalBlocked = true;
            }
        }
        
        return new CollisionResult(resolvedMovement, horizontalBlocked, verticalBlocked);
    }
    
    private static float ResolveAxisMovement(GameWorldObjects gameWorldObjects, Vector2 currentPosition, Rectangle hitbox, float xMovement, float yMovement, bool isHorizontal)
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
                hitbox.X + (int)movementDistance,
                hitbox.Y + (int)yMovement,
                hitbox.Width,
                hitbox.Height
            );
        }
        else
        {
            fullMovementHitbox = new(
                hitbox.X + (int)xMovement,
                hitbox.Y + (int)movementDistance,
                hitbox.Width,
                hitbox.Height
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
                    hitbox.X + (int)nextMovement,
                    hitbox.Y + (int)yMovement,
                    hitbox.Width,
                    hitbox.Height
                );
            }
            else
            {
                testHitbox = new(
                    hitbox.X + (int)xMovement,
                    hitbox.Y + (int)nextMovement,
                    hitbox.Width,
                    hitbox.Height
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
    
    public static bool IsObjectGrounded(Rectangle gameArea, GameWorldObjects gameWorldObjects, Rectangle hitbox)
    {
        // Check if object is at the bottom of the game area (ground)
        bool atBottom = hitbox.Bottom >= gameArea.Bottom;
        if (atBottom)
        {
            return true;
        }
        
        Rectangle belowObjectHitbox = new(
            hitbox.X,
            hitbox.Y + hitbox.Height,
            hitbox.Width,
            8
        );
        
        // Check if object is grounded on a platform
        foreach (Platform platform in gameWorldObjects.Platforms)
        {
            if (belowObjectHitbox.Intersects(platform.Hitbox))
            {
                return true;
            }
        }
        
        return false;
    }
    
    private static Vector2 EnsureObjectAbovePlatforms(GameWorldObjects gameWorldObjects, Vector2 position, Rectangle hitbox)
    {
        Vector2 newPosition = position;
        
        // Check all platforms and ensure object is positioned above any that they intersect with
        foreach (var platform in gameWorldObjects.Platforms)
        {
            Rectangle testHitbox = new((int)newPosition.X, (int)newPosition.Y, hitbox.Width, hitbox.Height);
            if (testHitbox.Intersects(platform.Hitbox))
            {
                // Object is overlapping with platform - push it above the platform
                newPosition.Y = platform.Hitbox.Y - hitbox.Height - 1; // 1 pixel gap
                break; // Only adjust for the first overlapping platform
            }
        }
        
        return newPosition;
    }
    
    private static Vector2 ClampToGameArea(Rectangle gameArea, Vector2 position, Rectangle hitbox)
    {
        // Calculate the maximum allowed coordinates to keep the entire hitbox within bounds
        float maxX = gameArea.Right - hitbox.Width;
        float maxY = gameArea.Bottom - hitbox.Height;
        float minX = gameArea.Left;
        float minY = gameArea.Top;
        
        // Clamp coordinates to ensure hitbox stays within game area
        return Vector2.Min(Vector2.Max(position, new Vector2(minX, minY)), new Vector2(maxX, maxY));
    }
    
    // Legacy method for backward compatibility
    public static bool IsPlayerGrounded(Rectangle gameArea, GameWorldObjects gameWorldObjects, PlayableCharacter player)
    {
        return IsObjectGrounded(gameArea, gameWorldObjects, player.Hitbox);
    }
    
}
}