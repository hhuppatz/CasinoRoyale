using System.Collections.Generic;
using System.Linq;
using CasinoRoyale.Classes.GameObjects.Items;
using CasinoRoyale.Classes.GameObjects.Items.Coin;
using CasinoRoyale.Classes.GameObjects.Items.Sword;
using CasinoRoyale.Classes.GameObjects.Platforms;
using CasinoRoyale.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.GameSystems;

// Generic item manager that handles all item types through a unified interface
// Single source of truth for all items - no redundant storage in factories
public class ItemManager
{
    private readonly Dictionary<ItemType, IItemFactory<Item>> _factories = [];
    private readonly List<Item> _allItems = [];
    private uint _nextItemId = 0;

    // Item factories - moved from GameWorldObjects for better encapsulation
    private readonly CoinFactory _coinFactory;
    private readonly SwordFactory _swordFactory;

    // Constructor - initialize item factories and register them
    public ItemManager(ContentManager content, Properties properties)
    {
        // Initialize item factories
        _coinFactory = new CoinFactory(
            content.Load<Texture2D>(properties.get("coin.image", "Coin"))
        );
        _swordFactory = new SwordFactory(
            content.Load<Texture2D>(properties.get("sword.image", "Sword"))
        );

        // Register factories with the item manager
        RegisterFactory(ItemType.COIN, _coinFactory);
        RegisterFactory(ItemType.SWORD, _swordFactory);
    }

    // Register a factory for a specific item type
    public void RegisterFactory(ItemType itemType, IItemFactory<Item> factory)
    {
        _factories[itemType] = factory;
    }

    // Spawn a new item of the specified type
    public void SpawnItem(
        ItemType itemType,
        Vector2 position,
        Vector2 velocity = default,
        float mass = 10.0f,
        float elasticity = 0.5f
    )
    {
        if (!_factories.TryGetValue(itemType, out var factory))
        {
            Logger.Error($"No factory registered for item type: {itemType}");
            return;
        }

        var item = factory.CreateItem(_nextItemId++, position, velocity, mass, elasticity);
        item.MarkAsChanged(); // Mark new item as changed for networking
        _allItems.Add(item);
    }

    // Update all items directly - no need to delegate to factories
    public void UpdateItems(float dt, Rectangle gameArea, IEnumerable<Rectangle> tileRects)
    {
        // Remove items that have fallen off the world or been destroyed
        _allItems.RemoveAll(item => item.Coords.Y > gameArea.Bottom || item.Destroyed);

        // Update remaining items
        foreach (var item in _allItems)
        {
            item.Update(dt, gameArea, tileRects);
        }
    }

    // Get all changed item states directly from items
    public ItemState[] GetChangedItemStates()
    {
        var changedStates = new List<ItemState>();

        foreach (var item in _allItems)
        {
            if (item.HasChanged)
            {
                changedStates.Add(item.GetState());
            }
        }

        return [.. changedStates];
    }

    // Get all item states directly from items
    public ItemState[] GetAllItemStates()
    {
        var allStates = new List<ItemState>();

        foreach (var item in _allItems)
        {
            allStates.Add(item.GetState());
        }

        return [.. allStates];
    }

    // Process item states from network (recreate items)
    public void ProcessItemStates(ItemState[] itemStates)
    {
        if (itemStates == null)
            return;

        // Clear existing items
        _allItems.Clear();

        // Track the highest item ID to avoid conflicts
        uint maxItemId = 0;

        foreach (var itemState in itemStates)
        {
            if (_factories.TryGetValue(itemState.itemType, out var factory))
            {
                var item = factory.CreateFromState(itemState);
                if (item != null)
                {
                    item.MarkAsChanged(); // Mark recreated items as changed
                    _allItems.Add(item);

                    // Track the highest item ID
                    if (itemState.itemId >= maxItemId)
                    {
                        maxItemId = itemState.itemId + 1;
                    }
                }
            }
        }

        // Update next item ID to avoid conflicts
        if (maxItemId > _nextItemId)
        {
            _nextItemId = maxItemId;
        }
    }

    // Remove an item by ID
    public void RemoveItemById(uint itemId)
    {
        var item = _allItems.FirstOrDefault(i => i.ItemId == itemId);
        if (item != null)
        {
            _allItems.Remove(item);
        }
    }

    // Add an item directly (for items created from network states)
    public void AddItem(Item item)
    {
        _allItems.Add(item);

        // Update next item ID to avoid conflicts
        if (item.ItemId >= _nextItemId)
        {
            _nextItemId = item.ItemId + 1;
        }
    }

    // Get all items of a specific type
    public List<T> GetItemsOfType<T>()
        where T : Item
    {
        return _allItems.OfType<T>().ToList();
    }

    // Get all items
    public List<Item> GetAllItems()
    {
        return [.. _allItems];
    }

    // Clear all changed flags for all items
    public void ClearAllChangedFlags()
    {
        foreach (var item in _allItems)
        {
            item.ClearChangedFlag();
        }
    }

    // Get the next available item ID (for external use)
    public uint GetNextItemId()
    {
        return _nextItemId++;
    }

    // Get coin factory texture (for casino machine spawning)
    public Texture2D GetCoinTexture()
    {
        return _coinFactory.GetTexture();
    }
}
