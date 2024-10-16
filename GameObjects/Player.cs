using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

public class Player
{
    private readonly Texture2D tex;
    private readonly Vector2 maxBaseVelocity;
    private Vector2 coords;

    public Player(Texture2D tex, Vector2 maxBaseVelocity)
    {
        this.tex = tex;
        this.maxBaseVelocity = maxBaseVelocity;
        coords = new Vector2(0, 0);
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
            coords = Vector2.Add(GetCoords(), new Vector2(-maxBaseVelocity.X, 0) * deltaTime);
        }

        if (ks.IsKeyDown(Keys.D) || ks.IsKeyDown(Keys.Right))
        {
            coords = Vector2.Add(GetCoords(), new Vector2(maxBaseVelocity.X, 0) * deltaTime);
        }
    }
}