using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class Platform
{
    private Texture2D tex;
    private Vector2 L;
    private Vector2 R;

    public Platform(Texture2D tex, Vector2 L, Vector2 R)
    {
        this.tex = tex;
        this.L = L;
        this.R = R;
    }

    public Vector2 GetLCoords()
    {
        return L;
    }

    public Vector2 GetRCoords()
    {
        return R;
    }
}