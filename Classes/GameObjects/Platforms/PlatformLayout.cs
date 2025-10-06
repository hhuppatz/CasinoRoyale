using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.GameObjects.Platforms
{
    public static class PlatformLayout
{
    /*
        Returns randomly generated platform layout as follows:
        Generates platforms from bottom to top, ensuring consistent player buffer at bottom:
        X00000  <- Top row
        0X0000
        000000
        X00000  <- Bottom row (above player buffer)
        ------  <- Player buffer zone (no platforms)
        
        Iterates from bottom up, giving each 'rectangle' a chance to spawn platform.
        Guarantees consistent safe zone at bottom for player spawning.
    */
    public static List<Platform> GenerateStandardRandPlatLayout(Texture2D platTex, Rectangle gameArea, int minLen, int maxLengthMultiple, int horizontalDistApart, int verticalDistApart, int platSpawnChance, int playerSpawnBuffer = 100)
    {
        uint platNum = 0;
        Random rand = new();
        PlatformFactory platformFactory = new(platTex);
        List<Platform> platforms = [];

        // Start platform generation from the bottom up, ensuring consistent player buffer at bottom
        int i = gameArea.Y + gameArea.Height - playerSpawnBuffer;
        int j = gameArea.X;
        
        // Generate platforms from bottom to top, leaving bottom buffer zone clear for player
        while (i >= gameArea.Y + playerSpawnBuffer)
        {
            while (j < gameArea.X + gameArea.Width)
            {
                // Generate platform at current rectangle
                if (rand.NextInt64(0, 100) < platSpawnChance)
                {
                    Platform platform = platformFactory.GeneratePlatform(platNum, new Vector2(j, i), minLen, maxLengthMultiple);
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
            i -= verticalDistApart;
        }

        return platforms;
    }

}
}