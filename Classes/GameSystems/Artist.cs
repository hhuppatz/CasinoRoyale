using System.Collections.Generic;
using CasinoRoyale.Classes.GameObjects;
using CasinoRoyale.Classes.GameObjects.CasinoMachines;
using CasinoRoyale.Classes.GameObjects.Items;
using CasinoRoyale.Classes.GameObjects.Platforms;
using CasinoRoyale.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class Artist(ContentManager content, SpriteBatch spriteBatch, MainCamera camera, Vector2 ratio)
{
    private readonly ContentManager content = content;
    private readonly SpriteBatch spriteBatch = spriteBatch;
    private readonly MainCamera camera = camera;
    private readonly Vector2 ratio = ratio;

    // Draws all platforms using the provided SpriteBatch and camera
    public void DrawPlatforms(List<Platform> platforms)
    {
        // Safety check: ensure spriteBatch and camera are initialized
        if (spriteBatch == null || camera == null)
        {
            return;
        }

        // Safety check: ensure platforms list is not null
        if (platforms == null)
        {
            return;
        }

        foreach (var platform in platforms)
        {
            if (platform?.GetTex() != null)
            {
                int platformLeft = (int)platform.GetLCoords().X;
                int platformTexWidth = platform.GetTex().Bounds.Width;
                int platformWidth = platform.GetWidth();
                
                // Safety check: ensure platform has positive width
                if (platformWidth <= 0)
                {
                    Logger.Warning($"Warning: Platform {platform.GetState().platNum} has zero or negative width ({platformWidth})");
                    continue; // Skip rendering this platform
                }
                
                int i = platformLeft;
                
                // Draw platform tiles from left to right
                while (i < platformLeft + platformWidth)
                {
                    spriteBatch.Draw(platform.GetTex(),
                        camera.TransformToView(new Vector2(i, platform.GetCoords().Y)),
                        null, Color.White, 0.0f,
                        Vector2.Zero,  // Top-left origin to match hitbox positioning
                        ratio, 0, 0);
                    i += platformTexWidth;
                }
            }
            else
            {
                Logger.Warning($"Warning: Platform {platform?.GetState().platNum} has null texture");
            }
        }
    }

    // Draws all casino machines using the provided SpriteBatch and camera
    public void DrawCasinoMachines(List<CasinoMachine> casinoMachines)
    {
        // Safety check: ensure spriteBatch and camera are initialized
        if (spriteBatch == null || camera == null)
        {
            return;
        }

        // Safety check: ensure casino machines list is not null
        if (casinoMachines == null)
        {
            return;
        }

        foreach (var casinoMachine in casinoMachines)
        {
            if (casinoMachine?.GetTex() != null)
            {
                spriteBatch.Draw(casinoMachine.GetTex(),
                    camera.TransformToView(casinoMachine.Coords),
                    null, Color.White, 0.0f, Vector2.Zero, ratio, 0, 0);
            }
        }
    }

     // Draws all items using the provided SpriteBatch and camera
    public void DrawItems(List<Item> Items)
    {
        // Safety check: ensure spriteBatch and camera are initialized
        if (spriteBatch == null || camera == null)
        {
            return;
        }

        // Safety check: ensure coins list is not null
        if (Items == null)
        {
            return;
        }

        foreach (var item in Items)
        {

            if (item.GetTexture() != null)
            {
                spriteBatch.Draw(item.GetTexture(),
                    camera.TransformToView(item.Coords),
                    null, Color.White, 0.0f, Vector2.Zero, ratio, 0, 0);
            }
        }
    }
}