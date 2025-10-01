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
    /// Main game world manager that contains game area and world objects
    /// </summary>
    public class GameWorld
    {
        public Rectangle GameArea { get; private set; }
        public GameWorldObjects WorldObjects { get; private set; }
        
        private Properties gameProperties;
        
        public GameWorld(Properties properties)
        {
            gameProperties = properties;
            WorldObjects = new GameWorldObjects(properties);
        }
        
        /// <summary>
        /// Loads game area properties and creates a Rectangle
        /// </summary>
        public Rectangle LoadGameArea()
        {
            int gameAreaX = int.Parse(gameProperties.get("gameArea.x", "-2000"));
            int gameAreaY = int.Parse(gameProperties.get("gameArea.y", "0"));
            int gameAreaWidth = int.Parse(gameProperties.get("gameArea.width", "4000"));
            int gameAreaHeight = int.Parse(gameProperties.get("gameArea.height", "4000"));
            
            GameArea = new Rectangle(gameAreaX, gameAreaY, gameAreaWidth, gameAreaHeight);
            return GameArea;
        }
        
        /// <summary>
        /// Sets the game area (used by Client when receiving from Host)
        /// </summary>
        public void SetGameArea(Rectangle gameArea)
        {
            GameArea = gameArea;
        }

        public void DrawGameObjects(SpriteBatch spriteBatch, MainCamera camera, Vector2 ratio)
        {
            WorldObjects?.DrawPlatforms(spriteBatch, camera, ratio);
            WorldObjects?.DrawCasinoMachines(spriteBatch, camera, ratio);
        }
        
        /// <summary>
        /// Calculates player origin based on game area and texture height
        /// </summary>
        public Vector2 CalculatePlayerOrigin(int playerTextureHeight)
        {
            int playerSpawnBuffer = playerTextureHeight * 2; // Keep area directly around player free
            return new Vector2(0, GameArea.Y + GameArea.Height - playerSpawnBuffer);
        }
        
        /// <summary>
        /// Generates the complete game world with platforms and casino machines
        /// </summary>
        public void GenerateGameWorld(ContentManager content, Vector2 playerOrigin)
        {
            WorldObjects.GenerateGameWorld(content, GameArea, playerOrigin);
        }

        public void InitPhysics()
        {
            PhysicsSystem.Initialize(GameArea, WorldObjects.Platforms, WorldObjects.CasinoMachines, gameProperties);
        }
        
        public Texture2D LoadPlayerTexture(ContentManager content)
        {
            string playerImageName = gameProperties.get("player.image", "ball");
            Logger.Info($"Loading player texture: {playerImageName}");
            return content.Load<Texture2D>(playerImageName);
        }
    }
}
