using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class PlatformFactory
{
    private Texture2D platTex;
    private Random rand;
    public PlatformFactory(Texture2D platTex)
    {
        this.platTex = platTex;
        rand = new Random();
    }

    public Platform GeneratePlatform(uint platNum, Vector2 leftSide, int minLen, int maxLen)
    {
        Vector2 L = new Vector2(leftSide.X, leftSide.Y);
        Vector2 R = new Vector2(L.X + (int)(rand.NextDouble() * (maxLen - minLen)) + minLen, L.Y);

        return new Platform(platNum, platTex, L, R);
    }
}