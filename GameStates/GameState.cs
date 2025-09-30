using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using CasinoRoyale.GameObjects;
using CasinoRoyale.MonogameMethodExtensions;
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
        protected GameWorld GameWorld { get; private set; }
        
        // Player
        protected PlayableCharacter LocalPlayer { get; set; }
        protected Texture2D PlayerTexture { get; set; }
        protected Vector2 PlayerOrigin { get; set; }

        // Keyboard States
        protected KeyboardState KeyboardState { get; set; }
        protected KeyboardState PreviousKeyboardState { get; set; }

        protected GameState(Game game)
        {
            Game = game;
            MainCamera = MainCamera.Instance;
            GameProperties = new Properties("app.properties");
            GameWorld = new GameWorld(GameProperties);
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
            
            // Calculate player origin
            PlayerOrigin = CalculatePlayerOrigin();
        }
        
        public virtual void Update(GameTime gameTime)
        {
            PreviousKeyboardState = KeyboardState;
            KeyboardState = Keyboard.GetState();
            
            // Common update logic
            if (LocalPlayer != null)
            {
                float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
                LocalPlayer.TryMovePlayer(KeyboardState, PreviousKeyboardState, deltaTime);
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
            
            GameWorld.DrawGameObjects(SpriteBatch, MainCamera, ratio);
            
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
            return new Vector2(0, GameWorld.GameArea.Height - playerSpawnBuffer);
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
