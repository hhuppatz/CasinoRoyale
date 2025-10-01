using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using CasinoRoyale.Utils;

namespace CasinoRoyale.GameObjects
{
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

    private Rectangle _gameArea;
    public Rectangle GameArea { get => _gameArea; }
    private List<Platform> _platforms;
    public List<Platform> Platforms { get => _platforms; }
    private List<CasinoMachine> _casinoMachines;
    public List<CasinoMachine> CasinoMachines { get => _casinoMachines; }
    private Properties _gameProperties;

    private PhysicsSystem(Rectangle gameArea, List<Platform> platforms, List<CasinoMachine> casinoMachines, Properties gameProperties)
    {
        _gameArea = gameArea;
        _platforms = platforms;
        _casinoMachines = casinoMachines;
        _gameProperties = gameProperties;
        GRAVITY = float.Parse(_gameProperties.get("gravity"));
    }

    public static void Initialize(Rectangle gameArea, List<Platform> platforms, List<CasinoMachine> casinoMachines, Properties gameProperties)
    {
        _instance = new PhysicsSystem(gameArea, platforms, casinoMachines, gameProperties);
    }

    // Coordinate system has 0,0 in top left and grows more positive to the right and down
    public void EnforceMovementRules(PlayableCharacter player, float dt)
    {
        EnforcePlayerGravity(player, dt);

        // Calculate attempted movement
        Vector2 attemptedMovement = player.Velocity * dt;
        
        // Check and resolve collisions, getting the maximum allowed movement
        Vector2 realMovement = ResolveCollisions(player, attemptedMovement);
        
        // Apply the resolved movement to the player
        player.Coords += realMovement;
        
        // Keep player's hitbox within game area bounds
        Rectangle playerHitbox = player.Hitbox;
        
        // Calculate the maximum allowed coordinates to keep the entire hitbox within bounds
        float maxX = GameArea.Right - playerHitbox.Width;
        float maxY = GameArea.Bottom - playerHitbox.Height;
        float minX = GameArea.Left;
        float minY = GameArea.Top;
        
        // Clamp coordinates to ensure hitbox stays within game area
        player.Coords = Vector2.Min(Vector2.Max(player.Coords, new Vector2(minX, minY)), new Vector2(maxX, maxY));        
    }
    
    private Vector2 ResolveCollisions(PlayableCharacter player, Vector2 attemptedMovement)
    {
        Vector2 realMovement = attemptedMovement;
        
        // Case 1: Only horizontal movement
        if (attemptedMovement.X != 0 && attemptedMovement.Y == 0)
        {
            realMovement.X = ResolveHorizontalCollision(player.Hitbox, attemptedMovement).X;
            realMovement.Y = 0;
        }
        // Case 2: Only vertical movement
        else if (attemptedMovement.X == 0 && attemptedMovement.Y != 0)
        {
            realMovement.X = 0;
            realMovement.Y = ResolveVerticalCollision(player.Hitbox, attemptedMovement).Y;
        }
        // Case 3: Diagonal movement - resolve horizontal first, then vertical if needed
        else if (attemptedMovement.X != 0 && attemptedMovement.Y != 0)
        {
            // First, resolve horizontal movement
            realMovement.X = ResolveHorizontalCollision(player.Hitbox, attemptedMovement).X;
            
            // Create new hitbox after horizontal movement
            Rectangle newHitboxAfterHorizontal = new Rectangle(
                player.Hitbox.X + (int)realMovement.X,
                player.Hitbox.Y,
                player.Hitbox.Width,
                player.Hitbox.Height
            );
            
            // Check if vertical movement is still needed after horizontal resolution
            Vector2 remainingVerticalMovement = new Vector2(0, attemptedMovement.Y);
            Vector2 resolvedVertical = ResolveVerticalCollision(newHitboxAfterHorizontal, remainingVerticalMovement);
            
            // Only apply vertical movement if it's still valid
            realMovement.Y = resolvedVertical.Y;
        }
        
        return realMovement;
    }

    private Vector2 ResolveHorizontalCollision(Rectangle hitbox, Vector2 attemptedMovement)
    {
        Vector2 maxMovement = attemptedMovement;
        
        // Create the hitbox that would result from the attempted movement
        Rectangle newHitbox = new(
            hitbox.X + (int)attemptedMovement.X,
            hitbox.Y,
            hitbox.Width,
            hitbox.Height
        );
        
        // Check collision with platforms
        foreach (Platform platform in _platforms)
        {
            if (newHitbox.Intersects(platform.Hitbox))
            {
                // Calculate the maximum allowed movement in X direction
                if (attemptedMovement.X > 0) // Moving right
                {
                    int maxX = platform.Hitbox.Left - hitbox.Width;
                    maxMovement.X = Math.Min(maxMovement.X, maxX - hitbox.X);
                }
                else if (attemptedMovement.X < 0) // Moving left
                {
                    int maxX = platform.Hitbox.Right;
                    maxMovement.X = Math.Max(maxMovement.X, maxX - hitbox.X);
                }
            }
        }
        
        return new Vector2(maxMovement.X, 0);
    }
    
    private Vector2 ResolveVerticalCollision(Rectangle hitbox, Vector2 attemptedMovement)
    {
        Vector2 maxMovement = attemptedMovement;
        
        // Create the hitbox that would result from the attempted movement
        Rectangle newHitbox = new(
            hitbox.X,
            hitbox.Y + (int)attemptedMovement.Y,
            hitbox.Width,
            hitbox.Height
        );
        
        // Check collision with platforms
        foreach (Platform platform in _platforms)
        {
            if (newHitbox.Intersects(platform.Hitbox))
            {
                // Calculate the maximum allowed movement in Y direction
                if (attemptedMovement.Y > 0) // Moving down
                {
                    int maxY = platform.Hitbox.Top - hitbox.Height;
                    maxMovement.Y = Math.Min(maxMovement.Y, maxY - hitbox.Y);
                }
                else if (attemptedMovement.Y < 0) // Moving up
                {
                    int maxY = platform.Hitbox.Bottom;
                    maxMovement.Y = Math.Max(maxMovement.Y, maxY - hitbox.Y);
                }
            }
        }
        
        return new Vector2(0, maxMovement.Y);
    }

    public static void EnforcePlayerGravity(PlayableCharacter player, float dt)
    {   
        if (!CasinoRoyale.GameObjects.PhysicsSystem.Instance.IsPlayerGrounded(player))
        {   
            player.Velocity += new Vector2(0, CasinoRoyale.GameObjects.PhysicsSystem.Instance.GRAVITY * player.Mass * dt);
        }
    }
    
    public bool IsPlayerGrounded(PlayableCharacter player)
    {
        // Check if player is at the bottom of the game area (ground)
        bool atBottom = player.Hitbox.Bottom >= _gameArea.Bottom;
        if (atBottom)
        {
            return true;
        }
        
        foreach (Platform platform in Platforms)
        {
            if (player.Hitbox.Intersects(platform.Hitbox))
            {
                return true;
            }
        }
        
        return false;
    }
    
}
}