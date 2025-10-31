using System;
using System.Collections.Generic;
using System.Linq;
using CasinoRoyale.Classes.GameObjects.Items;
using CasinoRoyale.Classes.GameObjects.Items.Coin;
using CasinoRoyale.Classes.GameObjects.Items.Sword;
using CasinoRoyale.Classes.GameObjects.Platforms;
using CasinoRoyale.Classes.GameObjects.WorldGrid;
using CasinoRoyale.Classes.GameUtilities;
using CasinoRoyale.Classes.Networking;
using CasinoRoyale.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.GameSystems;

public partial class GameWorld(
    Properties properties,
    ContentManager content,
    SpriteBatch spriteBatch,
    MainCamera camera,
    Vector2 ratio
)
{
    public Rectangle GameArea { get; private set; }
    private readonly Artist artist = new(content, spriteBatch, camera, ratio);
    private readonly Texture2D _floorTexture = content.Load<Texture2D>(
        properties.get("casinoFloor.image.1", "CasinoFloor1")
    );
    private readonly Texture2D _groundTexture = content.Load<Texture2D>(
        properties.get("ground.image", "Ground")
    );
    private readonly Texture2D _casinoMachineTexture = content.Load<Texture2D>(
        properties.get("casinoMachine.image.1", "CasinoMachine1")
    );
    private readonly ItemManager itemManager = new(content, properties);
    private readonly GridManager gridManager = new(
        int.Parse(properties.get("gameArea.width", "4000")),
        int.Parse(properties.get("gameArea.height", "4000")),
        16
    );

    private readonly Properties gameProperties = properties;
    private readonly ContentManager gameContent = content;

    // Host
    public void InitializeGameWorld(Vector2 playerOrigin, Rectangle gameArea = default)
    {
        if (gameArea == default)
            LoadGameArea();
        else
            GameArea = gameArea;

        // Set the world offset in GridManager so it can handle coordinate conversion internally
        gridManager.SetWorldOffset(new Vector2(GameArea.X, GameArea.Y));

        GenerateGameWorld(GameArea, playerOrigin);

        PhysicsSystem.Initialize(gameProperties);
    }

    // Client
    public void InitializeGameWorldFromState(JoinAcceptPacket joinAccept)
    {
        GameArea = joinAccept.gameArea;

        // Set the world offset in GridManager so it can handle coordinate conversion internally
        gridManager.SetWorldOffset(new Vector2(GameArea.X, GameArea.Y));

        GenerateGameWorldFromState(joinAccept);

        // Initialize physics system for client
        PhysicsSystem.Initialize(gameProperties);
    }

    private void LoadGameArea()
    {
        if (gameProperties == null) return;

        int gameAreaX = int.Parse(gameProperties.get("gameArea.x", "-2000"));
        int gameAreaY = int.Parse(gameProperties.get("gameArea.y", "0"));
        int gameAreaWidth = int.Parse(gameProperties.get("gameArea.width", "4000"));
        int gameAreaHeight = int.Parse(gameProperties.get("gameArea.height", "4000"));

        GameArea = new Rectangle(gameAreaX, gameAreaY, gameAreaWidth, gameAreaHeight);
    }

    public void DrawGameObjects()
    {
        artist.DrawGridTiles(gridManager.GetAllTiles());
        artist.DrawItems(AllItems);
    }

    // Calculates player origin based on game area and texture dimensions
    // Searches for an empty spawn position near the center at the bottom of the world
    public Vector2 CalculatePlayerOrigin(int playerTextureHeight, int playerTextureWidth = 0)
    {
        if (GameArea == Rectangle.Empty) return Vector2.Zero;

        // Default to height if width not provided (assume roughly square)
        if (playerTextureWidth == 0)
            playerTextureWidth = playerTextureHeight;

        // Start at the center bottom of the world
        float baseY = GameArea.Y + GameArea.Height - playerTextureHeight;
        float centerX = GameArea.X + (GameArea.Width / 2f) - (playerTextureWidth / 2f);
        
        Vector2 spawnPos = new Vector2(centerX, baseY);
        
        // Check if the default position is empty
        Rectangle spawnRect = new Rectangle((int)spawnPos.X, (int)spawnPos.Y, playerTextureWidth, playerTextureHeight);
        if (!gridManager.IsPositionOccupied(spawnRect))
        {
            return spawnPos;
        }

        // If occupied, search for an empty spot nearby
        // Try positions left and right in a spiral pattern
        // Search up to half the game area width to find an empty spawn
        int searchRadius = GameArea.Width / 2; // pixels to search
        int stepSize = 32; // step size for searching
        
        for (int offset = stepSize; offset <= searchRadius; offset += stepSize)
        {
            // Try left
            Vector2 leftPos = new Vector2(centerX - offset, baseY);
            // Ensure position is within game area bounds
            if (leftPos.X >= GameArea.X && leftPos.X + playerTextureWidth <= GameArea.X + GameArea.Width)
            {
                Rectangle leftRect = new Rectangle((int)leftPos.X, (int)leftPos.Y, playerTextureWidth, playerTextureHeight);
                if (!gridManager.IsPositionOccupied(leftRect))
                {
                    return leftPos;
                }
            }

            // Try right
            Vector2 rightPos = new Vector2(centerX + offset, baseY);
            // Ensure position is within game area bounds
            if (rightPos.X >= GameArea.X && rightPos.X + playerTextureWidth <= GameArea.X + GameArea.Width)
            {
                Rectangle rightRect = new Rectangle((int)rightPos.X, (int)rightPos.Y, playerTextureWidth, playerTextureHeight);
                if (!gridManager.IsPositionOccupied(rightRect))
                {
                    return rightPos;
                }
            }
        }

        // If no empty spot found, return the default position anyway
        Logger.Warning($"Could not find empty spawn position, using default position");
        return spawnPos;
    }

    // New: unified world update (items only; grid is static unless modified by state)
    public void Update(float dt, bool isHost)
    {
        itemManager.UpdateItems(dt, GameArea, GetPlatformTileHitboxes());
    }

    // Host-only generation hook (currently no-op)
    public void GenerateGameWorld(Rectangle gameArea, Vector2 playerOrigin)
    {
        try
        {
            if (_groundTexture == null)
            {
                Logger.Error("Ground texture is null");
                return;
            }
            
            int groundPatches = 10; // Number of ground patches to spawn (5 on each side)
            
            // Place ground at the bottom of the game area, centered near the player
            // Ground tiles should be positioned so they sit at the bottom edge
            float groundY = gameArea.Y + gameArea.Height - _groundTexture.Height;
            
            for (int i = -groundPatches / 2; i < groundPatches / 2; i++)
            {
                // Calculate world position - centered around player's X position at the bottom
                Vector2 worldPos = new(
                    playerOrigin.X + (i * _groundTexture.Width), 
                    groundY // Bottom of the game area
                );
                
                // GridManager handles coordinate conversion internally
                gridManager.AddGround(_groundTexture, worldPos);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to generate ground tiles: {ex.Message}");
            throw;
        }
    }

    // Client: rebuild from state
    public void GenerateGameWorldFromState(JoinAcceptPacket joinAccept)
    {
        if (joinAccept.itemStates != null)
        {
            itemManager.ProcessItemStates(joinAccept.itemStates);
        }

        if (joinAccept.gridTiles != null)
        {
            foreach (var t in joinAccept.gridTiles)
            {
                var texture = ResolveTextureForTileType(t.type);
                if (texture != null)
                {
                    gridManager.PlaceTile(t.type, t.hitbox, texture, t.source, t.isSolid);
                }
            }
        }
    }

    // Physics helpers
    public IEnumerable<Rectangle> GetPlatformTileHitboxes() => gridManager.GetSolidHitboxes();

    // Item access
    public List<Coin> Coins => itemManager.GetItemsOfType<Coin>();
    public List<Sword> Swords => itemManager.GetItemsOfType<Sword>();
    public List<Item> AllItems => itemManager.GetAllItems();

    public void SpawnItem(
        ItemType itemType,
        Vector2 position,
        Vector2 velocity = default,
        float mass = 10.0f,
        float elasticity = 0.5f
    ) => itemManager.SpawnItem(itemType, position, velocity, mass, elasticity);

    public void RemoveItemById(uint id) => itemManager.RemoveItemById(id);

    public void ProcessItemStates(ItemState[] itemStates) =>
        itemManager.ProcessItemStates(itemStates);

    public void AddItem(Item item) => itemManager.AddItem(item);

    public ItemState[] GetItemStates() => itemManager.GetAllItemStates();

    public ItemState[] GetChangedItemStates() => itemManager.GetChangedItemStates();

    public Item GetItemById(uint itemId) => AllItems.FirstOrDefault(item => item.ItemId == itemId);

    public GridTileState[] GetGridTileStates()
    {
        var tiles = gridManager.GetAllTiles();
        var list = new List<GridTileState>();
        foreach (var tile in tiles)
        {
            list.Add(
                new GridTileState
                {
                    type = tile.Type,
                    hitbox = tile.Hitbox,
                    source = tile.Source,
                    isSolid = tile.IsSolid,
                }
            );
        }
        var result = list.ToArray();
        Logger.Info($"GetGridTileStates: returning {result.Length} tiles");
        return result;
    }

    private Texture2D ResolveTextureForTileType(GridTileType type)
    {
        return type switch
        {
            GridTileType.GROUND => _groundTexture,
            GridTileType.CASINOMACHINE => _casinoMachineTexture,
            GridTileType.PLATFORM => _floorTexture,
            GridTileType.WOODPLANK => _floorTexture,
            GridTileType.ESCALATOR => _floorTexture,
            GridTileType.WALL => _floorTexture,
            _ => _floorTexture // Default fallback
        };
    }
}

// Observer handlers for item packets
partial class GameWorld
    : CasinoRoyale.Classes.Networking.AsyncPacketProcessor.INetworkObserver<ItemUpdatePacket>,
        CasinoRoyale.Classes.Networking.AsyncPacketProcessor.INetworkObserver<ItemRemovedPacket>,
        CasinoRoyale.Classes.Networking.AsyncPacketProcessor.INetworkObserver<GameWorldInitPacket>,
        CasinoRoyale.Classes.Networking.AsyncPacketProcessor.INetworkObserver<JoinAcceptPacket>
{
    public void OnPacket(ItemUpdatePacket packet)
    {
        if (packet?.itemStates != null)
        {
            itemManager.ProcessItemStates(packet.itemStates);
        }
    }

    public void OnPacket(ItemRemovedPacket packet)
    {
        RemoveItemById(packet.itemId);
    }

    public void OnPacket(GameWorldInitPacket packet)
    {
        // Initialize client game world from a lightweight init packet
        if (packet.gameArea != Rectangle.Empty)
        {
            GameArea = packet.gameArea;
        }
        if (packet.itemStates != null)
        {
            itemManager.ProcessItemStates(packet.itemStates);
        }
        if (packet.gridTiles != null)
        {
            foreach (var t in packet.gridTiles)
            {
                var texture = ResolveTextureForTileType(t.type);
                if (texture != null)
                {
                    gridManager.PlaceTile(t.type, t.hitbox, texture, t.source, t.isSolid);
                }
            }
        }
    }

    public void OnPacket(JoinAcceptPacket packet)
    {
        // Initialize the world from the join accept packet (this also initializes PhysicsSystem)
        InitializeGameWorldFromState(packet);
    }
}
