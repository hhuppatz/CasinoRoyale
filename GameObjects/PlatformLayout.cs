using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public static class PlatformLayout
{
    /*
        Returns randomly generated plaform layout as follows:
        Iterates through given game area as such:
        X00000
        000000
        Next i:
        0X0000
        000000
        Next j:
        000000
        X00000
        Until each 'rectangle' of game area is given chance to spawn platform.
        Thuggish implementation, will likely replace later.
    */
    public static List<Platform> GenerateStandardRandPlatLayout(Texture2D platTex, Rectangle gameArea, int minLen, int maxLen, int horizontalDistApart, int verticalDistApart, int platSpawnChance)
    {
        var rand = new Random();
        PlatformFactory platformFactory = new PlatformFactory(platTex);
        List<Platform> platforms = new List<Platform>();

        int i = gameArea.Y;
        int j = gameArea.X;
        
        while (i < gameArea.Y + gameArea.Height)
        {
            while (j < gameArea.X + gameArea.Width)
            {
                // Generate platform at current rectangle
                if (rand.NextInt64(0, 100) < platSpawnChance)
                {
                    Platform platform = platformFactory.GeneratePlatform(new Vector2(j, i), minLen, maxLen);
                    platforms.Add(platform);
                    j += platform.GetWidth() + horizontalDistApart;
                }
                else
                {
                    j += minLen + horizontalDistApart;
                }           
            }

            j = gameArea.X;
            i += verticalDistApart;
        }

        return platforms;
    }

}