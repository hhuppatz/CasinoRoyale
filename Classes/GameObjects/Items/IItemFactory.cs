using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.GameObjects.Items;

// Generic interface for item factories that can create items of any type
// ItemManager handles all item storage and management
public interface IItemFactory<T> where T : Item
{
    // Create a new item instance
    T CreateItem(uint id, Vector2 position, Vector2 velocity, float mass = 10.0f, float elasticity = 0.5f);
    
    // Get the texture used for this item type
    Texture2D GetTexture();
    
    // Get the item type this factory handles
    ItemType GetItemType();
    
    // Create an item from a network state (for client synchronization)
    T CreateFromState(ItemState state);
}
