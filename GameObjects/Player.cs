using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class Player
{
    private readonly Texture2D tex;
    private readonly Vector2 maxBaseVelocity;
    private readonly Vector2 maxBaseAcceleration;
    private Vector2 coords;
    private Vector2 velocity;
    private Vector2 acceleration;

    public Player(Texture2D tex, Vector2 coords, Vector2 maxBaseVelocity, Vector2 maxBaseAcceleration)
    {
        this.tex = tex;
        this.coords = coords;
        this.maxBaseVelocity = maxBaseVelocity;
        this.maxBaseAcceleration = maxBaseAcceleration;

        velocity = new Vector2(maxBaseVelocity.X, 0);
        acceleration = maxBaseAcceleration;
    }

    public Texture2D GetTex()
    {
        return tex;
    }

    public Vector2 GetCoords()
    {
        return coords;
    }

    // Only use provided input KeyboardState to keep control of input
    // exclusively in Game1.cs
    public void Move(KeyboardState ks, float deltaTime)
    {
        if (ks.IsKeyDown(Keys.A) || ks.IsKeyDown(Keys.Left))
        {
            coords = Vector2.Add(GetCoords(), new Vector2(-velocity.X, 0) * deltaTime);
        }

        if (ks.IsKeyDown(Keys.D) || ks.IsKeyDown(Keys.Right))
        {
            coords = Vector2.Add(GetCoords(), new Vector2(velocity.X, 0) * deltaTime);
        }
    }
}