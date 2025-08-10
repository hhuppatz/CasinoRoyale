using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class PlatformFactory(Texture2D platTex)
{
    private readonly Texture2D platTex = platTex;
    private readonly Random rand = new();

    public Platform GeneratePlatform(uint platNum, Vector2 topLeft, int minLen, int maxLen)
    {
        Vector2 botRight = new(topLeft.X + (int)(rand.NextDouble() * (maxLen - minLen)) + minLen, topLeft.Y);

        return new Platform(platNum, platTex, topLeft, botRight);
    }
}