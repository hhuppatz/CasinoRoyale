using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using CasinoRoyale.Classes.GameObjects;
using CasinoRoyale.Classes.Networking;
using CasinoRoyale.Utils;

namespace CasinoRoyale.Classes.GameSystems
{
    public class GameWorld(Properties properties)
    {
        public Rectangle GameArea { get; private set; }
        public GameWorldObjects WorldObjects { get; private set; } = new GameWorldObjects(properties);

        private readonly Properties gameProperties = properties;

        public void InitializeGameWorld(ContentManager content, Vector2 playerOrigin, Rectangle gameArea = default)
        {
            if (content == null)
            {
                Logger.Error("ContentManager is null in InitializeGameWorld()!");
                return;
            }
            
            if (WorldObjects == null)
            {
                Logger.Error("WorldObjects is null in InitializeGameWorld()!");
                return;
            }
            
            if (gameArea == default) LoadGameArea();
            else GameArea = gameArea;

            WorldObjects.GenerateGameWorld(content, GameArea, playerOrigin);
            
            if (WorldObjects.Platforms != null && WorldObjects.CasinoMachines != null)
            {
                PhysicsSystem.Initialize(GameArea, WorldObjects.Platforms, WorldObjects.CasinoMachines, gameProperties);
            }
            else
            {
                Logger.Error("Platforms or CasinoMachines are null in InitializeGameWorld()!");
            }
        }

        private void LoadGameArea()
        {
            if (gameProperties == null)
            {
                Logger.Error("gameProperties is null in LoadGameArea()!");
                return;
            }
            
            int gameAreaX = int.Parse(gameProperties.get("gameArea.x", "-2000"));
            int gameAreaY = int.Parse(gameProperties.get("gameArea.y", "0"));
            int gameAreaWidth = int.Parse(gameProperties.get("gameArea.width", "4000"));
            int gameAreaHeight = int.Parse(gameProperties.get("gameArea.height", "4000"));
            
            GameArea = new Rectangle(gameAreaX, gameAreaY, gameAreaWidth, gameAreaHeight);
        }

        public void DrawGameObjects(SpriteBatch spriteBatch, MainCamera camera, Vector2 ratio)
        {
            if (spriteBatch == null)
            {
                Logger.Error("SpriteBatch is null in DrawGameObjects()!");
                return;
            }
            
            if (camera == null)
            {
                Logger.Error("MainCamera is null in DrawGameObjects()!");
                return;
            }
            
            WorldObjects?.DrawPlatforms(spriteBatch, camera, ratio);
            WorldObjects?.DrawCasinoMachines(spriteBatch, camera, ratio);
        }
        
        // Initialize physics system
        public void InitPhysics()
        {
            if (WorldObjects?.Platforms == null || WorldObjects?.CasinoMachines == null)
            {
                Logger.Error("WorldObjects, Platforms, or CasinoMachines are null in InitPhysics()!");
                return;
            }
            
            if (GameArea == Rectangle.Empty)
            {
                Logger.Error("GameArea is not initialized in InitPhysics()!");
                return;
            }
            
            PhysicsSystem.Initialize(GameArea, WorldObjects.Platforms, WorldObjects.CasinoMachines, gameProperties);
            Logger.Info("Physics system initialized successfully");
        }
        
        // Load player texture from content manager
        public Texture2D LoadPlayerTexture(ContentManager content)
        {
            if (content == null)
            {
                Logger.Error("ContentManager is null in LoadPlayerTexture()!");
                return null;
            }
            
            if (gameProperties == null)
            {
                Logger.Error("gameProperties is null in LoadPlayerTexture()!");
                return null;
            }
            
            string playerImageName = gameProperties.get("player.image", "ball");
            Logger.Info($"Loading player texture: {playerImageName}");
            
            try
            {
                return content.Load<Texture2D>(playerImageName);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load player texture '{playerImageName}': {ex.Message}");
                return null;
            }
        }
        
        // Calculates player origin based on game area and texture height
        public Vector2 CalculatePlayerOrigin(int playerTextureHeight)
        {
            if (GameArea == Rectangle.Empty)
            {
                Logger.Error("GameArea is not initialized in CalculatePlayerOrigin()!");
                return Vector2.Zero;
            }
            
            int playerSpawnBuffer = playerTextureHeight * 2; // Keep area directly around player free
            return new Vector2(0, GameArea.Y + GameArea.Height - playerSpawnBuffer);
        }
    }
}
