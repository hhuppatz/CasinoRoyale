using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

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
    public void EnforceMovementRules(PlayableCharacter player, KeyboardState ks, float dt)
    {
        // Apply game rules to movement - only handle vertical movement
        Vector2 verticalMovement = GetVerticalMoveDistance(player, new Vector2(0, player.Velocity.Y), dt);
        player.Coords = new Vector2(player.Coords.X, player.Coords.Y + verticalMovement.Y);
        
        // Keep player within game area bounds
        player.Coords = Vector2.Min(Vector2.Max(player.Coords, new Vector2(GameArea.Left, GameArea.Top)), new Vector2(GameArea.Right, GameArea.Bottom));        
    }

    private Vector2 GetVerticalMoveDistance(PlayableCharacter player, Vector2 velocity, float dt)
    {
        Vector2 attemptedMovement = velocity * dt;
        
        // Only handle vertical movement (horizontal is handled directly)
        float maxYMovement = GetMaxMovementInDirection(player, attemptedMovement.Y, false);
        
        return new Vector2(0, maxYMovement);
    }
    
    private float GetMaxMovementInDirection(PlayableCharacter player, float attemptedMovement, bool isHorizontal)
    {
        if (attemptedMovement == 0) return 0;
        
        float currentMovement = attemptedMovement;
        Rectangle testHitbox = player.Hitbox;
        
        // Binary search to find maximum safe movement distance
        int iterations = 0;
        const int maxIterations = 8;
        
        while (iterations < maxIterations)
        {
            // Test the current movement
            if (isHorizontal) testHitbox.X = player.Hitbox.X + (int)currentMovement;
            else testHitbox.Y = player.Hitbox.Y + (int)currentMovement;
            
            // Check for collisions with platforms
            bool hasCollision = false;
            foreach (Platform platform in Platforms)
            {
                if (testHitbox.Intersects(platform.Hitbox))
                {
                    // Simple collision detection - if player is above platform, allow landing
                    if (!isHorizontal && attemptedMovement > 0 && player.Hitbox.Bottom <= platform.Hitbox.Top + 5)
                    {
                        // Player is landing on top of platform - this is allowed
                        continue;
                    }
                    
                    // If player is significantly below platform, bounce them up
                    if (!isHorizontal && attemptedMovement > 0 && player.Hitbox.Bottom > platform.Hitbox.Top + 20)
                    {
                        return 0;
                    }
                    
                    hasCollision = true;
                    // Debug: Log collision details
                    break;
                }
            }
            
            if (!hasCollision)
            {
                // No collision, we can move this distance
                return currentMovement;
            }
            
            // Collision detected, reduce movement by half
            currentMovement /= 2;
            iterations++;
        }
        
        // If we've reached max iterations, return 0 (no movement allowed)
        return 0;
    }
    
    public bool IsPlayerGrounded(PlayableCharacter player)
    {
        // Check if player is at the bottom of the game area (ground)
        if (player.Hitbox.Bottom >= _gameArea.Bottom)
        {
            return true;
        }
        
        // Check if player is standing on a platform
        // Use a small rectangle just below the player's feet
        Rectangle groundCheck = new(player.Hitbox.X, player.Hitbox.Bottom, player.Hitbox.Width, 2);
        
        foreach (Platform platform in Platforms)
        {
            if (groundCheck.Intersects(platform.Hitbox))
            {
                // Additional check: make sure player is actually on top of the platform
                // Player's bottom should be at or slightly above the platform's top
                if (player.Hitbox.Bottom <= platform.Hitbox.Top + 5)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
}
}