using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

public sealed class MainCamera
{
    private Vector2 coords;
    private MainCamera() {
        coords = new Vector2(0, 0);
    }

    // Only allow one instance of Main Camera to exist
    private static readonly Lazy<MainCamera> lazy = new Lazy<MainCamera>(() => new MainCamera());
    public static MainCamera Instance
    {
        get
        {
            return lazy.Value;
        }
    }

    public void MoveToFollowPlayer(Player player)
    {
        coords.X = player.GetCoords().X;
    }

    public Vector2 TransformToView(Vector2 vec2)
    {
        return Vector2.Subtract(vec2, coords);
    }
}