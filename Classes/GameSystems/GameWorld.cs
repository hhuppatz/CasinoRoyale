using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using CasinoRoyale.Classes.GameObjects;
using CasinoRoyale.Utils;
using CasinoRoyale.Classes.Networking;

namespace CasinoRoyale.Classes.GameSystems;

public class GameWorld(Properties properties, ContentManager content, SpriteBatch spriteBatch, MainCamera camera, Vector2 ratio)
{
    public Rectangle GameArea { get; private set; }
    public GameWorldObjects WorldObjects { get; private set; } = new GameWorldObjects(properties, content, spriteBatch, camera, ratio);

    private readonly Properties gameProperties = properties;
    private readonly ContentManager gameContent = content;

    public void InitializeGameWorld(Vector2 playerOrigin, Rectangle gameArea = default)
    {   
        if (gameArea == default) LoadGameArea();
        else GameArea = gameArea;

        WorldObjects.GenerateGameWorld(GameArea, playerOrigin);
        
        PhysicsSystem.Initialize(gameProperties);
    }

    public void InitializeGameWorldFromState(JoinAcceptPacket joinAccept)
    {
        GameArea = joinAccept.gameArea;

        WorldObjects.GenerateGameWorldFromState(joinAccept);
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

    public void DrawGameObjects()
    {
        WorldObjects.DrawGameObjects();
    }
    
    // Calculates player origin based on game area and texture height
    public Vector2 CalculatePlayerOrigin(int playerTextureHeight)
    {
        if (GameArea == Rectangle.Empty)
        {
            Logger.Error("GameArea is not initialized in CalculatePlayerOrigin()!");
            return Vector2.Zero;
        }
        
        // Player spawns at the exact bottom of the world
        return new Vector2(0, GameArea.Y + GameArea.Height - playerTextureHeight);
    }
}