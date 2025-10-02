using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.GameObjects
{
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
    public static List<Platform> GenerateStandardRandPlatLayout(Texture2D platTex, Rectangle gameArea, int minLen, int maxLen, int horizontalDistApart, int verticalDistApart, int platSpawnChance, int playerSpawnBuffer = 200)
    {
        uint platNum = 0;
        Random rand = new Random();
        PlatformFactory platformFactory = new PlatformFactory(platTex);
        List<Platform> platforms = new List<Platform>();

        // Start platform generation above the player spawn area
        int i = gameArea.Y + playerSpawnBuffer;
        int j = gameArea.X;
        
        // Only generate platforms in the upper portion, leaving bottom clear for player
        while (i < gameArea.Y + gameArea.Height - playerSpawnBuffer)
        {
            while (j < gameArea.X + gameArea.Width)
            {
                // Generate platform at current rectangle
                if (rand.NextInt64(0, 100) < platSpawnChance)
                {
                    Platform platform = platformFactory.GeneratePlatform(platNum, new Vector2(j, i), minLen, maxLen);
                    platforms.Add(platform);
                    platNum++;
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
}