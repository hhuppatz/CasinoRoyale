using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using CasinoRoyale.Utils;
using CasinoRoyale.Classes.GameObjects.CasinoMachines;
using CasinoRoyale.Classes.GameObjects.Platforms;

namespace CasinoRoyale.Classes.GameObjects
{
    // Manages game world objects (platforms and casino machines) for both Host and Client
    public class GameWorldObjects(Properties properties, ContentManager content)
    {
        public List<Platform> Platforms { get; private set; } = [];
        public List<CasinoMachine> CasinoMachines { get; private set; } = [];
        
        private readonly CasinoMachineFactory casinoMachineFactory = new(content.Load<Texture2D>(properties.get("casinoMachine.image.1", "CasinoMachine1")));
        private readonly Properties gameProperties = properties;

        // Generates the complete game world with platforms and casino machines

        public void GenerateGameWorld(ContentManager content, Rectangle gameArea, Vector2 playerOrigin)
        {
            // Generate platforms
            GeneratePlatforms(content, gameArea, playerOrigin);
            
            // Generate casino machines
            GenerateCasinoMachines();
        }
        
        // Generates platforms using the same logic as HostGameState

        private void GeneratePlatforms(ContentManager content, Rectangle gameArea, Vector2 playerOrigin)
        {
            // Calculate player spawn buffer - this should be the height of the bottom safe zone
            // where we don't want to spawn platforms (to keep area around player clear)
            // Use the same calculation as GameWorld.CalculatePlayerOrigin: playerTextureHeight
            int playerTextureHeight = 64; // ball.png texture height
            int playerSpawnBuffer = (int)(playerTextureHeight * 1.3f); // 64 pixels - matches player spawn position
            
            // Generate platforms using PlatformLayout
            Platforms = PlatformLayout.GenerateStandardRandPlatLayout(
                content.Load<Texture2D>(gameProperties.get("casinoFloor.image.1", "CasinoFloor1")),
                gameArea,
                64,    // minLen (minimum platform width)
                6,   // maxLengthMultiple (dictates maximum platform width)
                128,    // horizontalDistApart
                100,   // verticalDistApart
                60,    // platSpawnChance (60% chance to spawn)
                playerSpawnBuffer
            );
        }
        
        // Generates casino machines using CasinoMachineFactory
        private void GenerateCasinoMachines()
        {
            // Generate casino machines
            CasinoMachines = casinoMachineFactory.SpawnCasinoMachines();
        }
        
        // Recreates platforms from platform states (used by Client when receiving world data)
        public void RecreatePlatformsFromStates(ContentManager content, PlatformState[] platformStates)
        {
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
    }
}
