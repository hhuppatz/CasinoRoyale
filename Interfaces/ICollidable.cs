using Microsoft.Xna.Framework;

public interface ICollidable : IPhysics, IHitbox
{
    public bool CollidedWith(ICollidable c)
    {
        return Hitbox.Intersects(c.Hitbox);
    }

    public void DealWithCollision(ICollidable c)
    {
        if (CollidedWith(c))
            SetVelocity(Vector2.Zero);
    }
}