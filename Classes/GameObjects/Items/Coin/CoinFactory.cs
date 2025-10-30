using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.GameObjects.Items.Coin;

public class CoinFactory(Texture2D coinTex) : IItemFactory<Item>
{
    private readonly Texture2D coinTex = coinTex;


    // IItemFactory<Item> implementation
    public Item CreateItem(uint id, Vector2 position, Vector2 velocity, float mass = 10.0f, float elasticity = 0.5f)
    {
        return new Coin(id, coinTex, position, velocity, mass, elasticity);
    }
    
    public Texture2D GetTexture() => coinTex;
    
    public ItemType GetItemType() => ItemType.COIN;
    
    public Item CreateFromState(ItemState state)
    {
        if (state.itemType == ItemType.COIN)
        {
            return new Coin(state.itemId, coinTex, 
                state.gameEntityState.coords, 
                state.gameEntityState.velocity, 
                state.gameEntityState.mass);
        }
        return null;
    }
}