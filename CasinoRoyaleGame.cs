using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CasinoRoyale.GameStates;
using CasinoRoyale.Utils;

namespace CasinoRoyale
{
    /// <summary>
    /// Unified game class that manages different game states
    /// </summary>
    public class CasinoRoyaleGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private GameState _currentState;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;
        
        public CasinoRoyaleGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.Title = "Casino Royale";
        }
        
        protected override void Initialize()
        {
            Logger.Initialize();
            Logger.Info("Casino Royale Game initializing...");
            
            base.Initialize();
        }
        
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("Arial");
            
            // Start with menu state
            SetState(new MenuGameState(this));
        }
        
        protected override void Update(GameTime gameTime)
        {
            _currentState?.Update(gameTime);
            base.Update(gameTime);
        }
        
        protected override void Draw(GameTime gameTime)
        {
            _currentState?.Draw(gameTime);
            base.Draw(gameTime);
        }
        
        /// <summary>
        /// Sets the current game state
        /// </summary>
        public void SetState(GameState newState)
        {
            // Dispose current state
            _currentState?.Dispose();
            
            // Set new state
            _currentState = newState;
            
            // Initialize and load content for new state
            _currentState?.Initialize();
            _currentState?.LoadContent();
        }
        
        /// <summary>
        /// Starts hosting a game
        /// </summary>
        public void StartHost()
        {
            Logger.Info("Starting Host game...");
            var hostState = new HostGameState(this);
            SetState(hostState);
        }
        
        /// <summary>
        /// Joins a game with the specified lobby code
        /// </summary>
        public void JoinGame(string lobbyCode)
        {
            Logger.Info($"Joining game with lobby code: {lobbyCode}");
            var clientState = new ClientGameState(this, lobbyCode);
            SetState(clientState);
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _currentState?.Dispose();
                _spriteBatch?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
