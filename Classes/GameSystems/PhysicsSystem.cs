using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Xna.Framework;
using CasinoRoyale.Classes.GameObjects;
using CasinoRoyale.Utils;
using CasinoRoyale.Classes.GameObjects.Player;
using System.Linq;

namespace CasinoRoyale.Classes.GameSystems;

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

    // (platform-based overload removed)

    // Grid-based overloads using tile rectangles instead of Platform list
    public static PhysicsUpdateResult UpdatePhysics(Rectangle gameArea, IEnumerable<Rectangle> tileRects,
        Vector2 currentPosition, Vector2 currentVelocity, Rectangle hitbox, float mass, float dt)
    {
        // Convert to list once for multiple passes
        var tiles = tileRects?.ToList() ?? new List<Rectangle>();

        var result = new PhysicsUpdateResult
        {
            newPosition = currentPosition,
            newVelocity = currentVelocity,
            isGrounded = false,
            horizontalBlocked = false,
            verticalBlocked = false
        };

        Rectangle positionedHitbox = new((int)currentPosition.X, (int)currentPosition.Y, hitbox.Width, hitbox.Height);
        bool isGrounded = IsObjectGrounded(gameArea, tiles, positionedHitbox);
        result.isGrounded = isGrounded;

        if (!isGrounded)
        {
            result.newVelocity += new Vector2(0, Instance.GRAVITY * mass * dt);
        }

        Vector2 attemptedMovement = result.newVelocity * dt;

        var collisionResult = ResolveCollisions(tiles, currentPosition, positionedHitbox, attemptedMovement);
        result.newPosition += collisionResult.movement;
        result.horizontalBlocked = collisionResult.horizontalBlocked;
        result.verticalBlocked = collisionResult.verticalBlocked;

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
                result.newPosition = EnsureObjectAboveTiles(tiles, result.newPosition, positionedHitbox);
            }
        }

        result.newPosition = ClampToGameArea(gameArea, result.newPosition, positionedHitbox);

        return result;
    }

    public static void EnforceMovementRules(Rectangle gameArea, IEnumerable<Rectangle> tileRects, PlayableCharacter player, float dt)
    {
        var physicsResult = UpdatePhysics(gameArea, tileRects, player.Coords, player.Velocity, player.Hitbox, player.Mass, dt);
        player.Coords = physicsResult.newPosition;
        player.Velocity = physicsResult.newVelocity;
    }

    // Parallel physics update for multiple players - significantly improves performance with many players
    public static void UpdatePhysicsParallel(Rectangle gameArea, List<PlayableCharacter> players, float dt, IEnumerable<Rectangle> tileRects)
    {
        if (players == null || players.Count == 0) return;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Min(players.Count, Environment.ProcessorCount)
        };

        Parallel.ForEach(players, parallelOptions, player =>
        {
            try
            {
                var physicsResult = UpdatePhysics(gameArea, tileRects, player.Coords, player.Velocity, player.Hitbox, player.Mass, dt);
                
                // Thread-safe updates using lock to prevent race conditions
                lock (player)
                {
                    player.Coords = physicsResult.newPosition;
                    player.Velocity = physicsResult.newVelocity;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in parallel physics update for player {player.GetID()}: {ex.Message}");
            }
        });
    }

    // Parallel physics update for multiple objects with custom physics logic
    public static void UpdatePhysicsParallel<T>(Rectangle gameArea, IEnumerable<Rectangle> tileRects, List<T> objects, float dt, 
        Func<T, Vector2> getPosition, Func<T, Vector2> getVelocity, Func<T, Rectangle> getHitbox, 
        Func<T, float> getMass, Action<T, Vector2, Vector2> setPositionAndVelocity) where T : class
    {
        if (objects == null || objects.Count == 0) return;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Min(objects.Count, Environment.ProcessorCount)
        };

        Parallel.ForEach(objects, parallelOptions, obj =>
        {
            try
            {
                var position = getPosition(obj);
                var velocity = getVelocity(obj);
                var hitbox = getHitbox(obj);
                var mass = getMass(obj);
                
                var physicsResult = UpdatePhysics(gameArea, tileRects, position, velocity, hitbox, mass, dt);
                
                // Thread-safe updates
                lock (obj)
                {
                    setPositionAndVelocity(obj, physicsResult.newPosition, physicsResult.newVelocity);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in parallel physics update for object {obj}: {ex.Message}");
            }
        });
    }
    
    private static CollisionResult ResolveCollisions(List<Rectangle> tiles, Vector2 currentPosition, Rectangle hitbox, Vector2 attemptedMovement)
    {
        Vector2 resolvedMovement = Vector2.Zero;
        bool horizontalBlocked = false;
        bool verticalBlocked = false;

        Rectangle diagonalTestHitbox = new(
            hitbox.X + (int)attemptedMovement.X,
            hitbox.Y + (int)attemptedMovement.Y,
            hitbox.Width,
            hitbox.Height
        );

        bool hasDiagonalCollision = tiles.Any(t => diagonalTestHitbox.Intersects(t));
        if (!hasDiagonalCollision)
        {
            return new CollisionResult(attemptedMovement, false, false);
        }

        if (attemptedMovement.X != 0)
        {
            float originalX = attemptedMovement.X;
            resolvedMovement.X = ResolveAxisMovement(tiles, currentPosition, hitbox, attemptedMovement.X, 0f, true);
            if (Math.Abs(resolvedMovement.X) < Math.Abs(originalX)) horizontalBlocked = true;
        }

        if (attemptedMovement.Y != 0)
        {
            float originalY = attemptedMovement.Y;
            resolvedMovement.Y = ResolveAxisMovement(tiles, currentPosition, hitbox, resolvedMovement.X, attemptedMovement.Y, false);
            if (Math.Abs(resolvedMovement.Y) < Math.Abs(originalY)) verticalBlocked = true;
        }

        return new CollisionResult(resolvedMovement, horizontalBlocked, verticalBlocked);
    }
    
    private static float ResolveAxisMovement(List<Rectangle> tiles, Vector2 currentPosition, Rectangle hitbox, float xMovement, float yMovement, bool isHorizontal)
    {
        float movementDistance = isHorizontal ? xMovement : yMovement;
        if (Math.Abs(movementDistance) < 0.001f) return 0f;
        if (tiles == null || tiles.Count == 0) return movementDistance;

        Rectangle fullMovementHitbox = isHorizontal
            ? new Rectangle(hitbox.X + (int)movementDistance, hitbox.Y + (int)yMovement, hitbox.Width, hitbox.Height)
            : new Rectangle(hitbox.X + (int)xMovement, hitbox.Y + (int)movementDistance, hitbox.Width, hitbox.Height);

        bool hasFullMovementCollision = tiles.Any(t => fullMovementHitbox.Intersects(t));
        if (!hasFullMovementCollision) return movementDistance;

        float stepSize = Math.Sign(movementDistance);
        float currentMovement = 0f;
        while (Math.Abs(currentMovement) < Math.Abs(movementDistance))
        {
            float nextMovement = currentMovement + stepSize;
            Rectangle testHitbox = isHorizontal
                ? new Rectangle(hitbox.X + (int)nextMovement, hitbox.Y + (int)yMovement, hitbox.Width, hitbox.Height)
                : new Rectangle(hitbox.X + (int)xMovement, hitbox.Y + (int)nextMovement, hitbox.Width, hitbox.Height);

            bool hasCollision = tiles.Any(t => testHitbox.Intersects(t));
            if (hasCollision) break;
            currentMovement = nextMovement;
        }
        return currentMovement;
    }

    public static bool IsObjectGrounded(Rectangle gameArea, List<Rectangle> tiles, Rectangle hitbox)
    {
        if (hitbox.Bottom >= gameArea.Bottom) return true;

        Rectangle belowObjectHitbox = new(
            hitbox.X,
            hitbox.Y + hitbox.Height,
            hitbox.Width,
            8
        );

        foreach (var tile in tiles)
        {
            if (belowObjectHitbox.Intersects(tile)) return true;
        }
        return false;
    }
    
    private static Vector2 EnsureObjectAboveTiles(List<Rectangle> tiles, Vector2 position, Rectangle hitbox)
    {
        Vector2 newPosition = position;
        foreach (var tile in tiles)
        {
            Rectangle testHitbox = new((int)newPosition.X, (int)newPosition.Y, hitbox.Width, hitbox.Height);
            if (testHitbox.Intersects(tile))
            {
                newPosition.Y = tile.Y - hitbox.Height - 1;
                break;
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
    
    public static bool IsPlayerGrounded(Rectangle gameArea, IEnumerable<Rectangle> tileRects, PlayableCharacter player)
    {
        var tiles = tileRects?.ToList() ?? new List<Rectangle>();
        return IsObjectGrounded(gameArea, tiles, player.Hitbox);
    }
}