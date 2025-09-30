using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using CasinoRoyale.GameObjects;
using CasinoRoyale.Extensions;
using CasinoRoyale.Utils;

namespace CasinoRoyale.GameStates
{
    /// <summary>
    /// Base class for all game states (Menu, Host, Client)
    /// </summary>
    public abstract class GameState
    {
        protected Game Game { get; }
        protected GraphicsDevice GraphicsDevice => Game.GraphicsDevice;
        protected ContentManager Content => Game.Content;
        protected GameWindow Window => Game.Window;
        
        // Common game fields
        protected SpriteBatch SpriteBatch { get; private set; }
        protected SpriteFont Font { get; private set; }
        protected MainCamera MainCamera { get; private set; }
        protected Properties GameProperties { get; private set; }
        
        // Game world
        protected Rectangle GameArea { get; set; }
        protected List<Platform> Platforms { get; set; } = new();
        protected List<CasinoMachine> CasinoMachines { get; set; } = new();
        protected CasinoMachineFactory CasinoMachineFactory { get; set; }
        
        // Player
        protected PlayableCharacter LocalPlayer { get; set; }
        protected Texture2D PlayerTexture { get; private set; }
        protected Vector2 PlayerOrigin { get; set; }
        
        protected GameState(Game game)
        {
            Game = game;
            MainCamera = MainCamera.Instance;
            GameProperties = new Properties("app.properties");
        }
        
        public virtual void Initialize()
        {
            // Common initialization
        }
        
        public virtual void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            Font = Content.Load<SpriteFont>("Arial");
            
            // Load common textures
            PlayerTexture = Content.Load<Texture2D>("ball");
            var platformTexture = Content.Load<Texture2D>("CasinoFloor1");
            var casinoMachineTexture = Content.Load<Texture2D>("CasinoMachine1");
            
            // Calculate player origin
            PlayerOrigin = CalculatePlayerOrigin();
            
            // Initialize casino machine factory
            CasinoMachineFactory = new CasinoMachineFactory(casinoMachineTexture);
        }
        
        public virtual void Update(GameTime gameTime)
        {
            // Common update logic
            if (LocalPlayer != null)
            {
                float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
                LocalPlayer.TryMovePlayer(Keyboard.GetState(), deltaTime);
                MainCamera.MoveToFollowPlayer(LocalPlayer);
            }
        }
        
        public virtual void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkMagenta);
            
            // Check if essential components are initialized
            if (SpriteBatch == null)
            {
                Logger.Error("SpriteBatch is null in GameState.Draw() - skipping draw");
                return;
            }
            
            if (MainCamera == null)
            {
                Logger.Error("MainCamera is null in GameState.Draw() - skipping draw");
                return;
            }
            
            Vector2 ratio = Resolution.ratio;
            MainCamera.ApplyRatio(ratio);
            
            SpriteBatch.Begin();
            
            // Draw casino machines (only if they exist)
            if (CasinoMachines != null)
            {
                foreach (var casinoMachine in CasinoMachines)
                {
                    if (casinoMachine?.GetTex() != null)
                    {
                        SpriteBatch.Draw(casinoMachine.GetTex(),
                            MainCamera.TransformToView(casinoMachine.Coords),
                            null, Color.White, 0.0f, Vector2.Zero, ratio, 0, 0);
                    }
                }
            }
            
            // Draw platforms (only if they exist)
            if (Platforms != null)
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
                            SpriteBatch.Draw(platform.GetTex(),
                                MainCamera.TransformToView(new Vector2(i + platformTexWidth / 2, platform.GetCoords().Y)),
                                null, Color.White, 0.0f,
                                new Vector2(platformTexWidth / 2, platformTexWidth / 2),
                                ratio, 0, 0);
                            i += platformTexWidth;
                        }
                    }
                }
            }
            
            // Draw local player
            if (LocalPlayer != null)
            {
                SpriteBatch.DrawEntity(MainCamera, LocalPlayer);
            }
            
            // Draw other players (to be implemented by subclasses)
            DrawOtherPlayers();
            
            SpriteBatch.End();
        }
        
        protected abstract void DrawOtherPlayers();
        
        protected virtual Vector2 CalculatePlayerOrigin()
        {
            // Calculate player spawn position
            int playerSpawnBuffer = GetIntProperty("playerSpawnBuffer", 128);
            return new Vector2(0, GameArea.Height - playerSpawnBuffer);
        }
        
        protected virtual void InitializePhysics()
        {
            PhysicsSystem.Initialize(GameArea, Platforms, CasinoMachines, GameProperties);
        }
        
        protected virtual void InitializeCamera()
        {
            if (LocalPlayer != null)
            {
                MainCamera.InitMainCamera(Window, LocalPlayer);
            }
        }
        
        // Helper methods for properties
        protected string GetStringProperty(string key, string defaultValue = "")
        {
            return GameProperties.get(key) ?? defaultValue;
        }
        
        protected float GetFloatProperty(string key, float defaultValue = 0f)
        {
            if (float.TryParse(GameProperties.get(key), out float result))
                return result;
            return defaultValue;
        }
        
        protected int GetIntProperty(string key, int defaultValue = 0)
        {
            if (int.TryParse(GameProperties.get(key), out int result))
                return result;
            return defaultValue;
        }
        
        public virtual void Dispose()
        {
            SpriteBatch?.Dispose();
        }
    }
}
