using Microsoft.Xna.Framework;

namespace CasinoRoyale.Classes.GameObjects.Interfaces;

public interface IHitbox
{
    Rectangle Hitbox { get; set; }

    public bool CollidedWith(IHitbox c);
}