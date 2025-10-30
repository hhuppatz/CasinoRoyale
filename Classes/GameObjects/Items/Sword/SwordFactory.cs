using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.GameObjects.Items.Sword;

/// <summary>
/// Factory for creating and managing Sword items using the new unified system
/// </summary>
public class SwordFactory(Texture2D swordTex) : IItemFactory<Item>
{
    private readonly Texture2D swordTex = swordTex;

    // IItemFactory<Item> implementation
    public Item CreateItem(uint id, Vector2 position, Vector2 velocity, float mass = 10.0f, float elasticity = 0.5f)
    {
        return new Sword(id, swordTex, position, velocity, mass, elasticity);
    }
    
    public Texture2D GetTexture() => swordTex;
    
    public ItemType GetItemType() => ItemType.SWORD;
    
    public Item CreateFromState(ItemState state)
    {
        if (state.itemType == ItemType.SWORD)
        {
            return new Sword(state.itemId, swordTex, 
                state.gameEntityState.coords, 
                state.gameEntityState.velocity, 
                state.gameEntityState.mass);
        }
        return null;
    }
}
