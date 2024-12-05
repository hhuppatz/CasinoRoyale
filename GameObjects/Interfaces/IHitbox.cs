using Microsoft.Xna.Framework;

public interface IHitbox
{
    Rectangle Hitbox { get; set; }

    public bool CollidedWith(IHitbox c)
    {
        return Hitbox.Intersects(c.Hitbox);
    }

}