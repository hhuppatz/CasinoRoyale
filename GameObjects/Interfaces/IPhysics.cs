using Microsoft.Xna.Framework;

public interface IPhysics
{
    public void SetVelocity(Vector2 velocity);
    public Vector2 GetVelocity();
}