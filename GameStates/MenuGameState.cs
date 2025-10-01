using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using CasinoRoyale.Utils;

namespace CasinoRoyale.GameStates
{
    /// <summary>
    /// Game state for the main menu
    /// </summary>
    public class MenuGameState : GameState
    {
        private KeyboardState _previousKeyboardState;
        private MouseState _previousMouseState;
        private string _lobbyCodeInput = "";
        private string _statusMessage = "";
        private Color _statusColor = Color.White;
        
        // Button rectangles for click detection
        private Rectangle _hostButtonRect;
        private Rectangle _inputBoxRect;
        
        public MenuGameState(Game game, IGameStateManager stateManager) : base(game, stateManager)
        {
        }
        
        public override void Initialize()
        {
            base.Initialize();
            Logger.Info("Menu Game State initializing...");
        }
        
        public override void LoadContent()
        {
            base.LoadContent();
            
            // Set up button rectangles for single page layout
            var centerX = GraphicsDevice.Viewport.Width / 2;
            var centerY = GraphicsDevice.Viewport.Height / 2;
            
            // Host button
            var hostText = "HOST GAME";
            var hostTextSize = Font.MeasureString(hostText);
            var hostButtonWidth = (int)hostTextSize.X + 40;
            var hostButtonHeight = (int)hostTextSize.Y + 20;
            _hostButtonRect = new Rectangle(centerX - hostButtonWidth / 2, centerY - 50, hostButtonWidth, hostButtonHeight);
            
            // Input box for lobby code
            var inputBoxWidth = 200;
            var inputBoxHeight = 40;
            _inputBoxRect = new Rectangle(centerX - inputBoxWidth / 2, centerY + 50, inputBoxWidth, inputBoxHeight);
        }
        
        public override void Update(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();
            
            HandleInput(keyboardState, mouseState);
            
            _previousKeyboardState = keyboardState;
            _previousMouseState = mouseState;
        }
        
        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkBlue);
            
            SpriteBatch.Begin();
            DrawMenu();
            SpriteBatch.End();
        }
        
        private void HandleInput(KeyboardState keyboardState, MouseState mouseState)
        {
            // Handle Enter key to join game
            if (IsKeyPressed(keyboardState, Keys.Enter) && !string.IsNullOrEmpty(_lobbyCodeInput) && _lobbyCodeInput.Length == 6)
            {
                Logger.Info($"Starting as Client with lobby code: {_lobbyCodeInput}");
                StartClient(_lobbyCodeInput);
                return; // Don't process other input
            }
            
            // Handle mouse clicks
            if (IsMouseClicked(mouseState))
            {
                var mousePos = new Point(mouseState.X, mouseState.Y);
                
                if (_hostButtonRect.Contains(mousePos))
                {
                    Logger.Info("Host button clicked...");
                    StartHost();
                    return; // Don't process other input
                }
                else if (_inputBoxRect.Contains(mousePos))
                {
                    Logger.Info("Input box clicked...");
                }
            }
            
            // Handle lobby code input (typing letters/numbers)
            HandleLobbyCodeInput(keyboardState);
        }
        
        private void HandleLobbyCodeInput(KeyboardState keyboardState)
        {
            // Handle backspace
            if (IsKeyPressed(keyboardState, Keys.Back) && _lobbyCodeInput.Length > 0)
            {
                _lobbyCodeInput = _lobbyCodeInput.Substring(0, _lobbyCodeInput.Length - 1);
            }
            
            // Handle alphanumeric input (limit to 6 characters)
            foreach (var key in keyboardState.GetPressedKeys())
            {
                if (IsKeyPressed(keyboardState, key) && _lobbyCodeInput.Length < 6)
                {
                    char? character = GetCharacterFromKey(key);
                    if (character.HasValue)
                    {
                        _lobbyCodeInput += character.Value;
                    }
                }
            }
        }
        
        private bool IsKeyPressed(KeyboardState currentState, Keys key)
        {
            return currentState.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
        }
        
        private bool IsMouseClicked(MouseState currentState)
        {
            return currentState.LeftButton == ButtonState.Pressed && 
                   _previousMouseState.LeftButton == ButtonState.Released;
        }
        
        private char? GetCharacterFromKey(Keys key)
        {
            // Convert key to character (simplified version)
            if (key >= Keys.A && key <= Keys.Z)
            {
                return (char)('A' + (key - Keys.A));
            }
            else if (key >= Keys.D0 && key <= Keys.D9)
            {
                return (char)('0' + (key - Keys.D0));
            }
            return null;
        }
        
        private void StartHost()
        {
            try
            {
                _statusMessage = "Starting Host...";
                _statusColor = Color.Green;
                
                Logger.Info("Starting Host game...");
                
                // Use state manager to transition (no casting needed!)
                var hostState = new HostGameState(Game, StateManager);
                StateManager.TransitionToState(hostState);
            }
            catch (Exception ex)
            {
                _statusMessage = $"Error starting host: {ex.Message}";
                _statusColor = Color.Red;
                Logger.Error($"Error starting host: {ex.Message}");
            }
        }
        
        private void StartClient(string lobbyCode)
        {
            try
            {
                _statusMessage = $"Starting Client with lobby code: {lobbyCode}";
                _statusColor = Color.Green;
                
                Logger.Info($"Starting Client with lobby code: {lobbyCode}");
                
                // Use state manager to transition (no casting needed!)
                var clientState = new ClientGameState(Game, StateManager, lobbyCode);
                StateManager.TransitionToState(clientState);
            }
            catch (Exception ex)
            {
                _statusMessage = $"Error starting client: {ex.Message}";
                _statusColor = Color.Red;
                Logger.Error($"Error starting client: {ex.Message}");
            }
        }
        
        private void DrawMenu()
        {
            var centerX = GraphicsDevice.Viewport.Width / 2;
            var centerY = GraphicsDevice.Viewport.Height / 2;
            
            // Draw title
            var titleText = "CASINO ROYALE";
            var titleSize = Font.MeasureString(titleText);
            var titlePosition = new Vector2(centerX - titleSize.X / 2, centerY - 150);
            SpriteBatch.DrawString(Font, titleText, titlePosition, Color.Gold, 0f, Vector2.Zero, 2.0f, SpriteEffects.None, 0f);
            
            // Draw Host Game button
            DrawClickableButton("HOST GAME", _hostButtonRect, Color.LightGreen, Color.DarkGreen);
            
            // Draw lobby code input section
            var promptText = "ENTER LOBBY CODE:";
            var promptSize = Font.MeasureString(promptText);
            var promptPosition = new Vector2(centerX - promptSize.X / 2, centerY + 20);
            SpriteBatch.DrawString(Font, promptText, promptPosition, Color.White);
            
            // Draw input box
            var backgroundTexture = CreateColorTexture(Color.Black);
            SpriteBatch.Draw(backgroundTexture, _inputBoxRect, Color.Black * 0.8f);
            
            // Draw input box border
            var borderRect = new Rectangle(_inputBoxRect.X - 2, _inputBoxRect.Y - 2, _inputBoxRect.Width + 4, _inputBoxRect.Height + 4);
            var borderTexture = CreateColorTexture(Color.White);
            SpriteBatch.Draw(borderTexture, borderRect, Color.White);
            
            // Draw input text
            var displayText = _lobbyCodeInput.PadRight(6, '_');
            var inputSize = Font.MeasureString(displayText);
            var inputPosition = new Vector2(
                _inputBoxRect.X + _inputBoxRect.Width / 2 - inputSize.X / 2,
                _inputBoxRect.Y + _inputBoxRect.Height / 2 - inputSize.Y / 2
            );
            SpriteBatch.DrawString(Font, displayText, inputPosition, Color.Yellow);
            
            // Draw instructions
            var instructionsText = "PRESS ENTER TO JOIN";
            var instructionsSize = Font.MeasureString(instructionsText);
            var instructionsPosition = new Vector2(centerX - instructionsSize.X / 2, centerY + 120);
            SpriteBatch.DrawString(Font, instructionsText, instructionsPosition, Color.Gray);
            
            // Draw status message
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                var statusSize = Font.MeasureString(_statusMessage);
                var statusPosition = new Vector2(centerX - statusSize.X / 2, centerY + 150);
                SpriteBatch.DrawString(Font, _statusMessage, statusPosition, _statusColor);
            }
            
            // Clean up textures
            backgroundTexture.Dispose();
            borderTexture.Dispose();
        }
        
        private void DrawClickableButton(string text, Rectangle buttonRect, Color buttonColor, Color borderColor)
        {
            // Draw button background
            var buttonTexture = CreateColorTexture(buttonColor);
            SpriteBatch.Draw(buttonTexture, buttonRect, buttonColor);
            
            // Draw button border
            var borderRect = new Rectangle(buttonRect.X - 2, buttonRect.Y - 2, buttonRect.Width + 4, buttonRect.Height + 4);
            var borderTexture = CreateColorTexture(borderColor);
            SpriteBatch.Draw(borderTexture, borderRect, borderColor);
            
            // Draw button text centered
            var textSize = Font.MeasureString(text);
            var textPosition = new Vector2(
                buttonRect.X + buttonRect.Width / 2 - textSize.X / 2,
                buttonRect.Y + buttonRect.Height / 2 - textSize.Y / 2
            );
            SpriteBatch.DrawString(Font, text, textPosition, Color.White);
            
            // Clean up textures
            buttonTexture.Dispose();
            borderTexture.Dispose();
        }
        
        private Texture2D CreateColorTexture(Color color)
        {
            var texture = new Texture2D(GraphicsDevice, 1, 1);
            texture.SetData(new[] { color });
            return texture;
        }
    }
}
