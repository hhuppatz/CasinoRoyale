using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class PlatformFactory
{
    private Texture2D platTex;
    private List<Platform> platforms;

    public PlatformFactory(Rectangle gameArea, float minLen, float maxLen, float verticalDistApart, float horizontalDistApart)
    {
        GenerateNewPlatformLayout(gameArea, minLen, maxLen);
    }

    public List<Platform> GetPlatforms()
    {
        return platforms;
    }

    public void GenerateNewPlatformLayout(Rectangle gameArea, float minLen, float maxLen)
    {
        var rand = new Random();
        platforms = new List<Platform>();

        for (int i = gameArea.X; i < gameArea.X + gameArea.Width; i++)
        {
            for (int j = gameArea.Y; j < gameArea.Y + gameArea.Height; j++)
            {

            }
        }
    }

    private Platform GeneratePlatform(Rectangle platArea, float minLen, float maxLen, Random rand)
    {
        Vector2 L = new Vector2((float)rand.NextDouble() * platArea.Width - platArea.X, (float)rand.NextDouble() * platArea.Height - platArea.Y);
        Vector2 R = new Vector2(L.X + (float)rand.NextDouble() * (maxLen - minLen) + minLen, L.Y);

        return new Platform(platTex, L, R);
    }

}