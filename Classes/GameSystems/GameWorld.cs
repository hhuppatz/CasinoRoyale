using System.Collections.Generic;
using System.Linq;
using CasinoRoyale.Classes.GameObjects.Items;
using CasinoRoyale.Classes.GameObjects.Items.Coin;
using CasinoRoyale.Classes.GameObjects.Items.Sword;
using CasinoRoyale.Classes.GameObjects.Platforms;
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
    private readonly ItemManager itemManager = new(content, properties);
    private readonly GridManager gridManager = new(
        int.Parse(properties.get("gameArea.width", "4000")),
        int.Parse(properties.get("gameArea.height", "4000")),
        16
    );

    private readonly Properties gameProperties = properties;
    private readonly ContentManager gameContent = content;

    public void InitializeGameWorld(Vector2 playerOrigin, Rectangle gameArea = default)
    {
        if (gameArea == default)
            LoadGameArea();
        else
            GameArea = gameArea;

        GenerateGameWorld(GameArea, playerOrigin);

        PhysicsSystem.Initialize(gameProperties);
    }

    public void InitializeGameWorldFromState(JoinAcceptPacket joinAccept)
    {
        GameArea = joinAccept.gameArea;

        GenerateGameWorldFromState(joinAccept);

        // Initialize physics system for client
        PhysicsSystem.Initialize(gameProperties);
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
        artist.DrawGridTiles(gridManager.GetAllTiles());
        artist.DrawItems(AllItems);
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

    // New: unified world update (items only; grid is static unless modified by state)
    public void Update(float dt, bool isHost)
    {
        itemManager.UpdateItems(dt, GameArea, GetPlatformTileHitboxes());
    }

    // Host-only generation hook (currently no-op)
    public void GenerateGameWorld(Rectangle gameArea, Vector2 playerOrigin)
    {
        // Grid is populated explicitly elsewhere
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
        return list.ToArray();
    }

    private Texture2D ResolveTextureForTileType(GridTileType type)
    {
        // Minimal mapping: use floor texture for now; extend with per-type assets as needed
        return _floorTexture;
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
