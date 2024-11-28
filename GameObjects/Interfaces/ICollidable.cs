public interface ICollidable : IPhysics, IHitbox
{
    public bool CollidedWith(ICollidable c)
    {
        return Hitbox.Intersects(c.Hitbox);
    }
}