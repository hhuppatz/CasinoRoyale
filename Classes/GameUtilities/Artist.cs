using System.Collections.Generic;
using CasinoRoyale.Classes.GameObjects;
using CasinoRoyale.Classes.GameObjects.Items;
using CasinoRoyale.Classes.GameObjects.Platforms;
using CasinoRoyale.Classes.GameObjects.Player;
using CasinoRoyale.Classes.MonogameMethodExtensions;
using CasinoRoyale.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.GameUtilities;

public class Artist(
    ContentManager content,
    SpriteBatch spriteBatch,
    MainCamera camera,
    Vector2 ratio
)
{
    private readonly ContentManager content = content;
    private readonly SpriteBatch spriteBatch = spriteBatch;
    private readonly MainCamera camera = camera;
    private readonly Vector2 ratio = ratio;

    // Draw grid tiles (new system)
    public void DrawGridTiles(IEnumerable<GridTile> tiles)
    {
        if (spriteBatch == null || camera == null || tiles == null)
            return;

        foreach (var tile in tiles)
        {
            if (tile?.Texture == null)
                continue;
            // Draw each tile using its source and destination
            var dest = camera.TransformToView(tile.Hitbox.Location.ToVector2());
            var destRect = new Rectangle(
                dest.ToPoint(),
                new Point(tile.Hitbox.Width, tile.Hitbox.Height)
            );
            spriteBatch.Draw(tile.Texture, destRect, tile.Source, Color.White);
        }
    }

    // Casino machines are visuals via grid tiles; no separate draw method

    // Draws all items using the provided SpriteBatch and camera
    public void DrawItems(IEnumerable<Item> Items)
    {
        // Safety check: ensure spriteBatch and camera are initialized
        if (spriteBatch == null || camera == null)
        {
            return;
        }

        // Safety check: ensure items collection is not null
        if (Items == null)
        {
            return;
        }

        foreach (var item in Items)
        {
            if (item.Texture != null)
            {
                spriteBatch.DrawEntity(camera, item);
            }
        }
    }
}
