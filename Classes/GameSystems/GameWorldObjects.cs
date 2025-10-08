using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using CasinoRoyale.Utils;
using CasinoRoyale.Classes.GameObjects.CasinoMachines;
using CasinoRoyale.Classes.GameObjects.Platforms;
using CasinoRoyale.Classes.GameObjects;

namespace CasinoRoyale.Classes.GameSystems
{
    // Manages game world objects (platforms and casino machines) for both Host and Client
    public class GameWorldObjects(Properties properties, ContentManager content)
    {
        public List<Platform> Platforms { get; private set; } = [];
        public List<CasinoMachine> CasinoMachines { get; private set; } = [];
        
        private readonly CasinoMachineFactory casinoMachineFactory = new(content.Load<Texture2D>(properties.get("casinoMachine.image.1", "CasinoMachine1")));
        private readonly Properties gameProperties = properties;

        private readonly Texture2D coinTex = content.Load<Texture2D>(properties.get("coin.image", "Coin"));
        public List<Coin> Coins { get; private set; } = [];
        private uint nextCoinId = 0;
        private readonly Dictionary<uint, uint> processedRequests = new(); // machineNum -> lastProcessedRequestId

        //
        // Update methods
        //

        public void Update(float dt, Rectangle gameArea)
        {
            UpdateCoins(dt, gameArea);
        }
        
        public void UpdateCoins(float dt, Rectangle gameArea)
        {
            // Remove coins that have fallen off the world or been stationary for too long
            Coins.RemoveAll(coin => 
                coin.Coords.Y > gameArea.Bottom || 
                (Math.Abs(coin.Velocity.X) < 0.01f && Math.Abs(coin.Velocity.Y) < 0.01f && coin.Coords.Y > gameArea.Height * 0.9f));
                
            // Update remaining coins
            foreach (var coin in Coins)
            {
                coin.Update(dt, gameArea, this);
            }
        }
        
        //
        // Draw methods
        //

        // Draws all coins using the provided SpriteBatch and camera
        public void DrawCoins(SpriteBatch spriteBatch, MainCamera camera, Vector2 ratio)
        {
            foreach (var coin in Coins)
            {
                if (coin.GetTexture() != null)
                {
                    spriteBatch.Draw(coin.GetTexture(),
                        camera.TransformToView(coin.Coords),
                        null, Color.White, 0.0f, Vector2.Zero, ratio, 0, 0);
                }
            }
        }

        // Draws all platforms using the provided SpriteBatch and camera
        public void DrawPlatforms(SpriteBatch spriteBatch, MainCamera camera, Vector2 ratio)
        {
            foreach (var platform in Platforms)
            {
                if (platform?.GetTex() != null)
                {
                    int platformLeft = (int)platform.GetLCoords().X;
                    int platformTexWidth = platform.GetTex().Bounds.Width;
                    int platformWidth = platform.GetWidth();
                    
                    // Safety check: ensure platform has positive width
                    if (platformWidth <= 0)
                    {
                        Console.WriteLine($"Warning: Platform {platform.GetState().platNum} has zero or negative width ({platformWidth})");
                        continue; // Skip rendering this platform
                    }
                    
                    int i = platformLeft;
                    
                    // Draw platform tiles from left to right
                    while (i < platformLeft + platformWidth)
                    {
                        spriteBatch.Draw(platform.GetTex(),
                            camera.TransformToView(new Vector2(i + platformTexWidth / 2, platform.GetCoords().Y)),
                            null, Color.White, 0.0f,
                            new Vector2(platformTexWidth / 2, platformTexWidth / 2),
                            ratio, 0, 0);
                        i += platformTexWidth;
                    }
                }
                else
                {
                    Console.WriteLine($"Warning: Platform {platform?.GetState().platNum} has null texture");
                }
            }
        }
        
        // Draws all casino machines using the provided SpriteBatch and camera
        public void DrawCasinoMachines(SpriteBatch spriteBatch, MainCamera camera, Vector2 ratio)
        {
            foreach (var casinoMachine in CasinoMachines)
            {
                if (casinoMachine?.GetTex() != null)
                {
                    spriteBatch.Draw(casinoMachine.GetTex(),
                        camera.TransformToView(casinoMachine.Coords),
                        null, Color.White, 0.0f, Vector2.Zero, ratio, 0, 0);
                }
            }
        }

        //
        // Collision detection methods
        //

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

        //
        // Generation/creation methods
        //

        // Generates the complete game world with platforms and casino machines
        public void GenerateGameWorld(ContentManager content, Rectangle gameArea, Vector2 playerOrigin)
        {
            Logger.Info($"GENERATION DEBUG: GenerateGameWorld called - GameArea: {gameArea}, PlayerOrigin: {playerOrigin}");
            
            // Clear any existing coins from previous runs
            Coins.Clear();
            nextCoinId = 0;
            
            // Generate platforms
            Logger.Info("GENERATION DEBUG: Generating platforms...");
            GeneratePlatforms(content, gameArea, playerOrigin);
            Logger.Info($"GENERATION DEBUG: Platform generation complete - Created {Platforms.Count} platforms");
            
            // Generate casino machines
            Logger.Info("GENERATION DEBUG: Generating casino machines...");
            GenerateCasinoMachines();
            Logger.Info($"GENERATION DEBUG: Casino machine generation complete - Created {CasinoMachines.Count} casino machines");
        }
        
        // Generates platforms using the same logic as HostGameState

        private void GeneratePlatforms(ContentManager content, Rectangle gameArea, Vector2 playerOrigin)
        {
            // Calculate player spawn buffer - this should be the height of the bottom safe zone
            // where we don't want to spawn platforms (to keep area around player clear)
            // Use the same calculation as GameWorld.CalculatePlayerOrigin: playerTextureHeight
            int playerTextureHeight = 64; // ball.png texture height
            int playerSpawnBuffer = (int)(playerTextureHeight * 1.25f); // 64 pixels - matches player spawn position
            
            // Generate platforms using PlatformLayout
            Platforms = PlatformLayout.GenerateStandardRandPlatLayout(
                content.Load<Texture2D>(gameProperties.get("casinoFloor.image.1", "CasinoFloor1")),
                gameArea,
                64,    // minLen (minimum platform width)
                6,   // maxLengthMultiple (dictates maximum platform width)
                128,    // horizontalDistApart
                96,   // verticalDistApart
                65,    // platSpawnChance (60% chance to spawn)
                playerSpawnBuffer
            );
        }

        // Generates casino machines randomly on platforms
        private void GenerateCasinoMachines()
        {
            CasinoMachines = [];
            Random rand = new();
            
            // Casino machine spawn parameters from properties
            int casinoMachineSpawnChance = int.Parse(gameProperties.get("casinoMachine.spawnChance", "15"));
            int casinoMachineSpacing = int.Parse(gameProperties.get("casinoMachine.minSpacing", "64"));
            int minPlatformWidth = int.Parse(gameProperties.get("casinoMachine.minPlatformWidth", "128"));
            
            Logger.Info($"GENERATION DEBUG: Starting casino machine generation - Platforms: {Platforms.Count}, Spawn chance: {casinoMachineSpawnChance}%, Min platform width: {minPlatformWidth}");
            
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
                    Logger.Info($"GENERATION DEBUG: Platform {platformsChecked} too narrow: {platform.GetWidth()} < {minPlatformWidth}");
                    continue; // Skip narrow platforms
                }
                
                platformsWideEnough++;
                Logger.Info($"GENERATION DEBUG: Platform {platformsChecked} is wide enough: {platform.GetWidth()} >= {minPlatformWidth}");
                
                // Check if we should spawn a casino machine on this platform
                int roll = rand.Next(0, 100);
                spawnAttempts++;
                Logger.Info($"GENERATION DEBUG: Spawn attempt {spawnAttempts}: rolled {roll}, need < {casinoMachineSpawnChance}");
                
                if (roll < casinoMachineSpawnChance)
                {
                    // Calculate spawn position on the platform
                    Vector2 spawnPosition = CalculateCasinoMachinePosition(platform, rand, casinoMachineSpacing);
                    
                    // Create casino machine at the calculated position
                    var machine = new CasinoMachine((uint)CasinoMachines.Count, casinoMachineFactory.GetTexture(), spawnPosition);
                    CasinoMachines.Add(machine);
                    successfulSpawns++;
                    
                    Logger.Info($"GENERATION DEBUG: Successfully created casino machine {machine.GetState().machineNum} at {spawnPosition}");
                }
            }
            
            Logger.Info($"GENERATION DEBUG: Casino machine generation complete - Platforms checked: {platformsChecked}, Wide enough: {platformsWideEnough}, Spawn attempts: {spawnAttempts}, Successful spawns: {successfulSpawns}, Total machines: {CasinoMachines.Count}");
        }
        
        // Calculates appropriate spawn position for casino machine on a platform
        private Vector2 CalculateCasinoMachinePosition(Platform platform, Random rand, int minSpacing)
        {
            // Get platform bounds
            float platformLeft = platform.GetLCoords().X;
            float platformRight = platformLeft + platform.GetWidth();
            float platformTop = platform.GetCoords().Y;
            
            // Casino machine texture dimensions (assuming square texture)
            int machineWidth = casinoMachineFactory.GetTexture().Width;
            int machineHeight = casinoMachineFactory.GetTexture().Height;
            
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
        
        // Recreates platforms from platform states (used by Client when receiving world data)
        public void RecreatePlatformsFromStates(ContentManager content, PlatformState[] platformStates)
        {
            Console.WriteLine($"RecreatePlatformsFromStates called with {platformStates?.Length ?? 0} platform states");
            Platforms = [];
            foreach (var platformState in platformStates ?? [])
            {
                var platform = new Platform(
                    platformState.platNum,
                    content.Load<Texture2D>(gameProperties.get("casinoFloor.image.1", "CasinoFloor1")),
                    platformState.TL,
                    platformState.BR);
                Platforms.Add(platform);
            }
            Console.WriteLine($"Created {Platforms.Count} platforms");
        }
        
        // Recreates casino machines from casino machine states (used by Client when receiving world data)
        public void RecreateCasinoMachinesFromStates(ContentManager content, CasinoMachineState[] casinoMachineStates)
        {
            CasinoMachines = [];
            foreach (var casinoMachineState in casinoMachineStates ?? [])
            {
                var casinoMachine = new CasinoMachine(
                    casinoMachineState.machineNum,
                    content.Load<Texture2D>(gameProperties.get("casinoMachine.image.1", "CasinoMachine1")),
                    casinoMachineState.coords);
                CasinoMachines.Add(casinoMachine);
            }
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


        // Update individual objects by ID for efficient networking
        public void UpdatePlatformById(uint id, PlatformState state)
        {
            if (id < Platforms.Count)
            {
                Platforms[(int)id].SetState(state);
            }
        }

        public void UpdateCasinoMachineById(uint id, CasinoMachineState state)
        {
            if (id < CasinoMachines.Count)
            {
                CasinoMachines[(int)id].SetState(state);
            }
        }


        public void RemoveCoinById(uint id)
        {
            // Find and remove coin by actual coinId, not array index
            var coin = Coins.FirstOrDefault(c => c.CoinId == id);
            if (coin != null)
            {
                Coins.Remove(coin);
            }
        }
        
        // Coin management methods
        public Coin CreateCoin(Vector2 position, Vector2 velocity)
        {
            var coin = new Coin(nextCoinId++, coinTex, position, velocity);
            coin.MarkAsChanged(); // Mark new coin as changed
            Coins.Add(coin);
            return coin;
        }
        
        public Coin SpawnCoinFromCasinoMachine(uint machineNum)
        {
            var machine = CasinoMachines.FirstOrDefault(m => m.GetState().machineNum == machineNum);
            if (machine != null)
            {
                var coin = machine.SpawnCoin(nextCoinId++, coinTex);
                coin.MarkAsChanged(); // Mark new coin as changed
                Coins.Add(coin);
                // Reset the spawnedCoin flag after spawning
                machine.SpawnedCoin = false;
                machine.MarkAsChanged(); // Mark machine as changed since spawnedCoin flag changed
                return coin;
            }
            return null;
        }
        
        // Process casino machine states from clients and spawn coins if requested
        public List<(uint machineNum, uint requestId, Coin coin, bool wasSuccessful)> ProcessCasinoMachineStates(CasinoMachineState[] casinoMachineStates)
        {
            var results = new List<(uint machineNum, uint requestId, Coin coin, bool wasSuccessful)>();
            
            Logger.Info($"HOST DEBUG: ProcessCasinoMachineStates called with {casinoMachineStates?.Length ?? 0} states");
            Logger.Info($"HOST DEBUG: Host has {CasinoMachines.Count} casino machines");
            
            foreach (var state in casinoMachineStates ?? [])
            {
                Logger.Info($"HOST DEBUG: Processing state for machine {state.machineNum}, spawnedCoin={state.spawnedCoin}");
                
                var machine = CasinoMachines.FirstOrDefault(m => m.GetState().machineNum == state.machineNum);
                if (machine == null)
                {
                    Logger.Info($"HOST DEBUG: Machine {state.machineNum} not found in host's casino machines!");
                    continue;
                }
                
                Logger.Info($"HOST DEBUG: Found machine {state.machineNum}, host spawnedCoin={machine.SpawnedCoin}");
                
                if (machine != null && state.spawnedCoin && !machine.SpawnedCoin)
                {
                    // Client requested coin spawn and machine hasn't spawned one yet
                    Logger.Info($"HOST DEBUG: Attempting to spawn coin from machine {state.machineNum}");
                    var coin = SpawnCoinFromCasinoMachine(state.machineNum);
                    if (coin != null)
                    {
                        results.Add((state.machineNum, 0, coin, true)); // requestId not needed anymore
                        Logger.Info($"HOST DEBUG: Successfully spawned coin {coin.CoinId} from machine {state.machineNum}");
                    }
                    else
                    {
                        results.Add((state.machineNum, 0, null, false));
                        Logger.Info($"HOST DEBUG: Failed to spawn coin from machine {state.machineNum}");
                    }
                }
                else if (machine != null && state.spawnedCoin && machine.SpawnedCoin)
                {
                    Logger.Info($"HOST DEBUG: Machine {state.machineNum} already spawned a coin, ignoring request");
                }
                else if (machine != null && !state.spawnedCoin)
                {
                    Logger.Info($"HOST DEBUG: Machine {state.machineNum} spawnedCoin=false, ignoring");
                }
            }
            return results;
        }
        
        public void RecreateCoinsFromStates(CoinState[] coinStates)
        {
            Console.WriteLine($"RecreateCoinsFromStates called with {coinStates?.Length ?? 0} coin states");
            Coins.Clear();
            uint maxCoinId = 0;
            foreach (var coinState in coinStates ?? [])
            {
                var coin = new Coin(coinState.coinId, coinTex, coinState.coords, coinState.velocity);
                coin.MarkAsChanged(); // Mark recreated coins as changed
                Coins.Add(coin);
                
                // Track the highest coin ID to avoid conflicts
                if (coinState.coinId >= maxCoinId)
                {
                    maxCoinId = coinState.coinId + 1;
                }
            }
            
            // Update nextCoinId to avoid conflicts with existing coins
            if (maxCoinId > nextCoinId)
            {
                nextCoinId = maxCoinId;
            }
            
            Console.WriteLine($"Created {Coins.Count} coins, nextCoinId set to {nextCoinId}");
        }
        
        public CoinState[] GetCoinStates()
        {
            var coinStates = new CoinState[Coins.Count];
            for (int i = 0; i < Coins.Count; i++)
            {
                coinStates[i] = Coins[i].GetState();
            }
            return coinStates;
        }

        // Gets only changed coin states for networking
        public CoinState[] GetChangedCoinStates()
        {
            var changedCoins = Coins.Where(c => c.HasChanged).ToList();
            var coinStates = new CoinState[changedCoins.Count];
            for (int i = 0; i < changedCoins.Count; i++)
            {
                coinStates[i] = changedCoins[i].GetState();
            }
            return coinStates;
        }

        // Clear change flags for all objects after sending updates
        public void ClearAllChangedFlags()
        {
            foreach (var platform in Platforms)
            {
                platform.ClearChangedFlag();
            }
            foreach (var machine in CasinoMachines)
            {
                machine.ClearChangedFlag();
            }
            // Coins handle their own physics deterministically - no change flags to clear
        }

        // Getter for coin texture (needed by clients)
        public Texture2D GetCoinTexture()
        {
            return coinTex;
        }
        
        
    }
}
