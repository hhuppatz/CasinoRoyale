using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.GameObjects.Items.Coin;

public class Coin(uint itemId, Texture2D tex, Vector2 coords, Vector2 startVelocity, float mass = 4.0f, float elasticity = 0.5f)
: Item(itemId, ItemType.COIN, tex, coords, startVelocity, mass, elasticity)
{
    public override void Update(float dt, Rectangle gameArea, IEnumerable<Rectangle> tileRects)
    {
        base.Update(dt, gameArea, tileRects);
        if (Lifetime > 5)
        {
            DestroyEntity();
        }
    }

    public override void Collect()
    {

    }
}