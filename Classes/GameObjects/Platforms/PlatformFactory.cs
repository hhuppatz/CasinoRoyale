using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.GameObjects.Platforms
{
    public class PlatformFactory(Texture2D platTex)
{
    public List<Platform> Platforms { get; private set; } = [];
    
    private readonly Texture2D platTex = platTex;
    private readonly Random rand = new();

    public Platform GeneratePlatform(uint platNum, Vector2 topLeft, int minLen, int maxLengthMultiple = 3)
    {
        // Ensure platform width is at least minLen by using Next(1, maxLengthMultiple + 1)
        // This prevents zero-width platforms that would be invisible
        int lengthMultiple = rand.Next(1, maxLengthMultiple + 1);
        int platformWidth = lengthMultiple * minLen;
        Vector2 botRight = new(topLeft.X + platformWidth, topLeft.Y + platTex.Height);

        return new Platform(platNum, platTex, topLeft, botRight);
    }

    public void GeneratePlatforms(Rectangle gameArea, Vector2 playerOrigin)
    {
        // Calculate player spawn buffer - this should be the height of the bottom safe zone
        // where we don't want to spawn platforms (to keep area around player clear)
        // Use the same calculation as GameWorld.CalculatePlayerOrigin: playerTextureHeight
        int playerTextureHeight = 64; // ball.png texture height
        int playerSpawnBuffer = (int)(playerTextureHeight * 1.25f); // 64 pixels - matches player spawn position
        
        // Generate platforms using PlatformLayout
        Platforms = PlatformLayout.GenerateStandardRandPlatLayout(
            platTex,
            gameArea,
            64,    // minLen (minimum platform width)
            6,   // maxLengthMultiple (dictates maximum platform width)
            128,    // horizontalDistApart
            96,   // verticalDistApart
            65,    // platSpawnChance (60% chance to spawn)
            playerSpawnBuffer
        );
    }

    
    
    // Recreates platforms from platform states (used by Client when receiving world data)
    public void RecreatePlatformsFromStates(PlatformState[] platformStates)
    {
        Platforms = [];
        foreach (var platformState in platformStates ?? [])
        {
            var platform = new Platform(
                platformState.platNum,
                platTex,
                platformState.TL,
                platformState.BR);
            Platforms.Add(platform);
        }
        Console.WriteLine($"Created {Platforms.Count} platforms");
    }

    // Gets platform states for networking (used by Host to send to Client)
    public PlatformState[] GetPlatformStates()
    {
        var platformStates = new PlatformState[Platforms.Count];
        for (int i = 0; i < Platforms.Count; i++)
        {
            platformStates[i] = Platforms[i].GetState();
        }
        return platformStates;
    }

    // Gets only changed platform states for networking
    public PlatformState[] GetChangedPlatformStates()
    {
        var changedPlatforms = Platforms.Where(p => p.HasChanged).ToList();
        var platformStates = new PlatformState[changedPlatforms.Count];
        for (int i = 0; i < changedPlatforms.Count; i++)
        {
            platformStates[i] = changedPlatforms[i].GetState();
        }
        return platformStates;
    }

    // Get individual object updates with IDs for efficient networking
    public List<(uint id, PlatformState state)> GetChangedPlatformUpdates()
    {
        var updates = new List<(uint, PlatformState)>();
        for (int i = 0; i < Platforms.Count; i++)
        {
            if (Platforms[i].HasChanged)
            {
                updates.Add(((uint)i, Platforms[i].GetState()));
            }
        }
        return updates;
    }

    // Update individual objects by ID for efficient networking
    public void UpdatePlatformById(uint id, PlatformState state)
    {
        if (id < Platforms.Count)
        {
            Platforms[(int)id].SetState(state);
        }
    }

    // Checks if a rectangle intersects with any platform (for collision detection)
    public bool CheckPlatformCollision(Rectangle hitbox)
    {
        foreach (var platform in Platforms)
        {
            if (platform.Hitbox.Intersects(hitbox))
            {
                return true;
            }
        }
        return false;
    }
}
}