using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using CasinoRoyale.Classes.GameObjects;
using CasinoRoyale.Classes.MonogameMethodExtensions;
using CasinoRoyale.Utils;

namespace CasinoRoyale.Classes.GameStates
{
    /// <summary>
    /// Base class for all game states (Menu, Host, Client)
    /// </summary>
    public abstract class GameState(Game game, IGameStateManager stateManager)

    {
        protected Game Game { get; } = game;
        protected IGameStateManager StateManager { get; } = stateManager;
        protected GraphicsDevice GraphicsDevice => Game.GraphicsDevice;
        protected ContentManager Content => Game.Content;
        protected GameWindow Window => Game.Window;
        
        // Common fields for rendering
        protected SpriteBatch SpriteBatch { get; private set; }
        protected SpriteFont Font { get; private set; }
        protected MainCamera MainCamera { get; private set; } = MainCamera.Instance;
        protected Properties GameProperties { get; private set; } = new Properties("app.properties");

        // Keyboard States
        protected KeyboardState KeyboardState { get; set; }
        protected KeyboardState PreviousKeyboardState { get; set; }


        public virtual void Initialize()
        {
            // Common initialization
        }
        
        public virtual void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            Font = Content.Load<SpriteFont>("Arial");
        }
        
        public virtual void Update(GameTime gameTime)
        {
            PreviousKeyboardState = KeyboardState;
            KeyboardState = Keyboard.GetState();
        }
        
        public virtual void Draw(GameTime gameTime) {}
        
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
