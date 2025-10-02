using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CasinoRoyale.Classes.GameStates;
using CasinoRoyale.Utils;

namespace CasinoRoyale
{
    // Unified game class that manages different game states
    public class CasinoRoyaleGame : Game, IGameStateManager
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
            TransitionToState(new MenuGameState(this, this));
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
        
        // Transitions to a new game state (implements IGameStateManager)
        public void TransitionToState(GameState newState)
        {
            // Dispose current state
            _currentState?.Dispose();
            
            // Set new state
            _currentState = newState;
            
            // Initialize and load content for new state
            _currentState?.Initialize();
            _currentState?.LoadContent();
        }
        
        // Returns to the main menu (implements IGameStateManager)
        public void ReturnToMenu()
        {
            Logger.Info("Returning to main menu...");
            TransitionToState(new MenuGameState(this, this));
        }
        
        // Sets the current game state (legacy method - use TransitionToState instead)
        [Obsolete("Use TransitionToState instead")]
        public void SetState(GameState newState)
        {
            TransitionToState(newState);
        }
        
        // Starts hosting a game (convenience method for external use)
        public void StartHost()
        {
            Logger.Info("Starting Host game...");
            var hostState = new HostGameState(this, this);
            TransitionToState(hostState);
        }
        
        // Joins a game with the specified lobby code (convenience method for external use)
        public void JoinGame(string lobbyCode)
        {
            Logger.Info($"Joining game with lobby code: {lobbyCode}");
            var clientState = new ClientGameState(this, this, lobbyCode);
            TransitionToState(clientState);
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
