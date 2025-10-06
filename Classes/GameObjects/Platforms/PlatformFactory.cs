using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.GameObjects.Platforms
{
    public class PlatformFactory(Texture2D platTex)
{
    private readonly Texture2D platTex = platTex;
    private readonly Random rand = new();

    public Platform GeneratePlatform(uint platNum, Vector2 topLeft, int minLen, int maxLengthMultiple = 3)
    {
        // Ensure platform width is at least minLen by using Next(1, maxLengthMultiple + 1)
        // This prevents zero-width platforms that would be invisible
        int lengthMultiple = rand.Next(1, maxLengthMultiple + 1);
        int platformWidth = lengthMultiple * minLen;
        Vector2 botRight = new(topLeft.X + platformWidth, topLeft.Y + platTex.Height);

        return new Platform(platNum, platTex, topLeft, botRight);
    }
}
}