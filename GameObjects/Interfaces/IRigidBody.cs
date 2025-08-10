using Microsoft.Xna.Framework;

// Taking inspiration of Verlet integration scheme and iterative methods
// from "Advanced Character Physics" by Thomas Jakobsen of IO Interactive

// Utilising "velocity-less" bodies
public interface IRigidBody : IObject
{
    public Vector2 CoordsDash { get; set; }
    public float M { get;}
    public Vector2 G { get;}
    public Vector2 F { get;}

    public void VerletMove(float dt);
    public void SatisfyConstraints(Rectangle gameArea);
}