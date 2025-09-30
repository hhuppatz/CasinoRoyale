using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using CasinoRoyale.GameObjects;
using CasinoRoyale.Networking;
using CasinoRoyale.Utils;

namespace CasinoRoyale.GameObjects
{
    /// <summary>
    /// Manages game world objects (platforms and casino machines) for both Host and Client
    /// </summary>
    public class GameWorldObjects
    {
        public List<Platform> Platforms { get; private set; } = new();
        public List<CasinoMachine> CasinoMachines { get; private set; } = new();
        
        private CasinoMachineFactory casinoMachineFactory;
        private Properties gameProperties;
        
        public GameWorldObjects(Properties properties)
        {
            gameProperties = properties;
        }
        
        /// <summary>
        /// Generates the complete game world with platforms and casino machines
        /// </summary>
        public void GenerateGameWorld(ContentManager content, Rectangle gameArea, Vector2 playerOrigin)
        {
            // Generate platforms
            GeneratePlatforms(content, gameArea, playerOrigin);
            
            // Generate casino machines
            GenerateCasinoMachines(content);
        }
        
        /// <summary>
        /// Generates platforms using the same logic as HostGameState
        /// </summary>
        private void GeneratePlatforms(ContentManager content, Rectangle gameArea, Vector2 playerOrigin)
        {
            // Calculate player spawn buffer
            int playerSpawnBuffer = (int)(playerOrigin.Y - gameArea.Y);
            
            // Generate platforms using PlatformLayout
            Platforms = PlatformLayout.GenerateStandardRandPlatLayout(
                content.Load<Texture2D>(gameProperties.get("casinoFloor.image.1", "CasinoFloor1")),
                gameArea,
                50,    // minPlatforms
                200,   // maxPlatforms
                50,    // minPlatformWidth
                100,   // maxPlatformWidth
                70,    // minPlatformHeight
                playerSpawnBuffer);
            
            // Debug: Print platform hitboxes (first 3 only)
            var platformTexture = content.Load<Texture2D>(gameProperties.get("casinoFloor.image.1", "CasinoFloor1"));
            Logger.Info($"Platform texture dimensions: {platformTexture.Width}x{platformTexture.Height}");
            Logger.Info($"Generated {Platforms.Count} platforms");
            for (int i = 0; i < System.Math.Min(3, Platforms.Count); i++)
            {
                var platform = Platforms[i];
                Logger.Debug($"Platform {i}: Hitbox={platform.Hitbox}, Coords={platform.GetCoords()}");
            }
        }
        
        /// <summary>
        /// Generates casino machines using CasinoMachineFactory
        /// </summary>
        private void GenerateCasinoMachines(ContentManager content)
        {
            // Initialize casino machine factory
            var casinoMachineTexture = content.Load<Texture2D>(gameProperties.get("casinoMachine.image.1", "CasinoMachine1"));
            casinoMachineFactory = new CasinoMachineFactory(casinoMachineTexture);
            
            // Generate casino machines
            CasinoMachines = casinoMachineFactory.SpawnCasinoMachines();
            
            // Debug: Log casino machine positions (first 3 only)
            Logger.Info($"Spawned {CasinoMachines.Count} casino machines");
            for (int i = 0; i < System.Math.Min(3, CasinoMachines.Count); i++)
            {
                var machine = CasinoMachines[i];
                Logger.Debug($"Casino machine {i}: Coords={machine.Coords}, Hitbox={machine.Hitbox}");
            }
        }
        
        /// <summary>
        /// Recreates platforms from platform states (used by Client when receiving world data)
        /// </summary>
        public void RecreatePlatformsFromStates(ContentManager content, PlatformState[] platformStates)
        {
            Platforms = new List<Platform>();
            foreach (var platformState in platformStates ?? new PlatformState[0])
            {
                var platform = new Platform(
                    platformState.platNum,
                    content.Load<Texture2D>(gameProperties.get("casinoFloor.image.1", "CasinoFloor1")),
                    platformState.TL,
                    platformState.BR);
                Platforms.Add(platform);
            }
        }
        
        /// <summary>
        /// Recreates casino machines from casino machine states (used by Client when receiving world data)
        /// </summary>
        public void RecreateCasinoMachinesFromStates(ContentManager content, CasinoMachineState[] casinoMachineStates)
        {
            CasinoMachines = new List<CasinoMachine>();
            foreach (var casinoMachineState in casinoMachineStates ?? new CasinoMachineState[0])
            {
                var casinoMachine = new CasinoMachine(
                    casinoMachineState.machineNum,
                    content.Load<Texture2D>(gameProperties.get("casinoMachine.image.1", "CasinoMachine1")),
                    casinoMachineState.coords);
                CasinoMachines.Add(casinoMachine);
            }
        }
        
        /// <summary>
        /// Draws all platforms using the provided SpriteBatch and camera
        /// </summary>
        public void DrawPlatforms(SpriteBatch spriteBatch, MainCamera camera, Vector2 ratio)
        {
            foreach (var platform in Platforms)
            {
                if (platform?.GetTex() != null)
                {
                    int platformLeft = (int)platform.GetLCoords().X;
                    int platformTexWidth = platform.GetTex().Bounds.Width;
                    int platformWidth = platform.GetWidth();
                    int i = platformLeft;
                    
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
            }
        }
        
        /// <summary>
        /// Draws all casino machines using the provided SpriteBatch and camera
        /// </summary>
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
        
        /// <summary>
        /// Gets platform states for networking (used by Host to send to Client)
        /// </summary>
        public PlatformState[] GetPlatformStates()
        {
            var platformStates = new PlatformState[Platforms.Count];
            for (int i = 0; i < Platforms.Count; i++)
            {
                platformStates[i] = Platforms[i].GetState();
            }
            return platformStates;
        }
        
        /// <summary>
        /// Gets casino machine states for networking (used by Host to send to Client)
        /// </summary>
        public CasinoMachineState[] GetCasinoMachineStates()
        {
            var casinoMachineStates = new CasinoMachineState[CasinoMachines.Count];
            for (int i = 0; i < CasinoMachines.Count; i++)
            {
                casinoMachineStates[i] = CasinoMachines[i].GetState();
            }
            return casinoMachineStates;
        }
        
        /// <summary>
        /// Checks if a rectangle intersects with any platform (for collision detection)
        /// </summary>
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
        
        /// <summary>
        /// Checks if a rectangle intersects with any casino machine (for collision detection)
        /// </summary>
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
