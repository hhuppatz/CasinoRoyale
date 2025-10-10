using System;
using System.Collections.Generic;
using System.Linq;
using CasinoRoyale.Classes.GameObjects.Platforms;
using CasinoRoyale.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.GameObjects.CasinoMachines
{
public class CasinoMachineFactory(Texture2D machineTex)
{
    public List<CasinoMachine> CasinoMachines { get; private set; } = [];
    private readonly Texture2D machineTex = machineTex;

    public Texture2D GetTexture() => machineTex;

    public List<CasinoMachine> SpawnCasinoMachines()
    {
        uint id = 0;
        List<CasinoMachine> machines = [new CasinoMachine(id, machineTex, new Vector2(200,0))];
        id++;
        return machines;
    }

    // Generates casino machines randomly on platforms
    public void GenerateCasinoMachines(Properties gameProperties, List<Platform> Platforms)
    {
        CasinoMachines = [];
        Random rand = new();
        
        // Casino machine spawn parameters from properties
        int casinoMachineSpawnChance = int.Parse(gameProperties.get("casinoMachine.spawnChance", "15"));
        int casinoMachineSpacing = int.Parse(gameProperties.get("casinoMachine.minSpacing", "64"));
        int minPlatformWidth = int.Parse(gameProperties.get("casinoMachine.minPlatformWidth", "128"));
        
        int platformsChecked = 0;
        int platformsWideEnough = 0;
        int spawnAttempts = 0;
        int successfulSpawns = 0;
        
        foreach (var platform in Platforms)
        {
            platformsChecked++;
            
            // Only spawn casino machines on platforms that are wide enough
            if (platform.GetWidth() < minPlatformWidth) 
            {
                continue; // Skip narrow platforms
            }
            
            platformsWideEnough++;
            
            // Check if we should spawn a casino machine on this platform
            int roll = rand.Next(0, 100);
            spawnAttempts++;
            
            if (roll < casinoMachineSpawnChance)
            {
                // Calculate spawn position on the platform
                Vector2 spawnPosition = CalculateCasinoMachinePosition(platform, rand, casinoMachineSpacing);
                
                // Create casino machine at the calculated position
                var machine = new CasinoMachine((uint)CasinoMachines.Count, machineTex, spawnPosition);
                CasinoMachines.Add(machine);
                successfulSpawns++;    
            }
        }
    }

    // Recreates casino machines from casino machine states (used by Client when receiving world data)
    public void RecreateCasinoMachinesFromStates(CasinoMachineState[] casinoMachineStates)
    {
        CasinoMachines = [];
        foreach (var casinoMachineState in casinoMachineStates ?? [])
        {
            var casinoMachine = new CasinoMachine(
                casinoMachineState.machineNum,
                machineTex,
                casinoMachineState.coords);
            CasinoMachines.Add(casinoMachine);
        }
    }
    
    // Calculates appropriate spawn position for casino machine on a platform
    private Vector2 CalculateCasinoMachinePosition(Platform platform, Random rand, int minSpacing)
    {
        // Get platform bounds
        float platformLeft = platform.GetLCoords().X;
        float platformRight = platformLeft + platform.GetWidth();
        float platformTop = platform.GetCoords().Y;
        
        // Casino machine texture dimensions (assuming square texture)
        int machineWidth = machineTex.Width;
        int machineHeight = machineTex.Height;
        
        // Calculate available spawn area (leave some margin from edges)
        float spawnAreaLeft = platformLeft + machineWidth / 2;
        float spawnAreaRight = platformRight - machineWidth / 2;
        
        // If platform is too narrow, center the machine
        if (spawnAreaRight <= spawnAreaLeft)
        {
            return new Vector2(platformLeft + platform.GetWidth() / 2, platformTop - machineHeight);
        }
        
        // Try to find a position that doesn't overlap with existing machines
        int maxAttempts = 10;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Random X position within spawn area
            float randomX = (float)(rand.NextDouble() * (spawnAreaRight - spawnAreaLeft) + spawnAreaLeft);
            
            // Check if this position conflicts with existing machines on this platform
            Vector2 candidatePosition = new Vector2(randomX, platformTop - machineHeight);
            bool hasConflict = false;
            
            foreach (var existingMachine in CasinoMachines)
            {
                float distance = Math.Abs(existingMachine.Coords.X - candidatePosition.X);
                if (distance < minSpacing)
                {
                    hasConflict = true;
                    break;
                }
            }
            
            if (!hasConflict)
            {
                return candidatePosition;
            }
        }
        
        // If we couldn't find a non-conflicting position, use a random position anyway
        float fallbackX = (float)(rand.NextDouble() * (spawnAreaRight - spawnAreaLeft) + spawnAreaLeft);
        return new Vector2(fallbackX, platformTop - machineHeight);
    }

    // Gets casino machine states for networking (used by Host to send to Client)
    public CasinoMachineState[] GetCasinoMachineStates()
    {
        var casinoMachineStates = new CasinoMachineState[CasinoMachines.Count];
        for (int i = 0; i < CasinoMachines.Count; i++)
        {
            casinoMachineStates[i] = CasinoMachines[i].GetState();
        }
        return casinoMachineStates;
    }

    // Gets only changed casino machine states for networking
    public CasinoMachineState[] GetChangedCasinoMachineStates()
    {
        var changedMachines = CasinoMachines.Where(m => m.HasChanged).ToList();
        var casinoMachineStates = new CasinoMachineState[changedMachines.Count];
        for (int i = 0; i < changedMachines.Count; i++)
        {
            casinoMachineStates[i] = changedMachines[i].GetState();
        }
        return casinoMachineStates;
    }

    public List<(uint id, CasinoMachineState state)> GetChangedCasinoMachineUpdates()
    {
        var updates = new List<(uint, CasinoMachineState)>();
        for (int i = 0; i < CasinoMachines.Count; i++)
        {
            if (CasinoMachines[i].HasChanged)
            {
                updates.Add(((uint)i, CasinoMachines[i].GetState()));
            }
        }
        return updates;
    }

    public void UpdateCasinoMachineById(uint id, CasinoMachineState state)
    {
        if (id < CasinoMachines.Count)
        {
            CasinoMachines[(int)id].SetState(state);
        }
    }

    // Checks if a rectangle intersects with any casino machine (for collision detection)
    public bool CheckCasinoMachineCollision(Rectangle hitbox)
    {
        foreach (var casinoMachine in CasinoMachines)
        {
            if (casinoMachine.Hitbox.Intersects(hitbox))
            {
                return true;
            }
        }
        return false;
    }
}
}