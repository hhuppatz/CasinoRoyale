using System;
using System.Collections.Generic;
using CasinoRoyale.Classes.GameObjects.Platforms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.GameObjects.Platforms;

public class GridManager
{
    private readonly GridTile[][] gridTiles;
    private readonly int tileSize;
    public int TileSize => tileSize;

    public GridManager(int gridWidth, int gridHeight, int tileSize)
    {
        this.tileSize = tileSize;
        int gridSizeX = gridWidth / tileSize;
        int gridSizeY = gridHeight / tileSize;
        gridTiles = new GridTile[gridSizeX][];
        for (int i = 0; i < gridSizeX; i++)
        {
            gridTiles[i] = new GridTile[gridSizeY];
        }
    }

    // General method to split a texture into grid-aligned tiles and store per-cell source rectangles
    public void AddTiledTexture(GridTileType type, Texture2D texture, Vector2 coords, bool isSolid)
    {
        if (texture == null)
        {
            return;
        }

        var rect = new Rectangle((int)coords.X, (int)coords.Y, texture.Width, texture.Height);

        int startTileX = (int)Math.Floor(coords.X / tileSize);
        int startTileY = (int)Math.Floor(coords.Y / tileSize);
        int endTileX = (int)Math.Floor(((coords.X + texture.Width - 1) / tileSize));
        int endTileY = (int)Math.Floor(((coords.Y + texture.Height - 1) / tileSize));

        startTileX = Math.Max(0, startTileX);
        startTileY = Math.Max(0, startTileY);
        endTileX = Math.Min(gridTiles.Length - 1, endTileX);
        endTileY = Math.Min(gridTiles[0].Length - 1, endTileY);

        for (int tx = startTileX; tx <= endTileX; tx++)
        {
            for (int ty = startTileY; ty <= endTileY; ty++)
            {
                var cellRect = new Rectangle(tx * tileSize, ty * tileSize, tileSize, tileSize);
                Rectangle overlap = Rectangle.Intersect(cellRect, rect);
                if (overlap.Width <= 0 || overlap.Height <= 0)
                {
                    continue;
                }

                var source = new Rectangle(
                    overlap.X - rect.X,
                    overlap.Y - rect.Y,
                    overlap.Width,
                    overlap.Height
                );

                gridTiles[tx][ty] = new GridTile(type, overlap, texture, source, isSolid);
            }
        }
    }

    public void AddPlatform(Texture2D texture, Vector2 coords)
    {
        AddTiledTexture(GridTileType.PLATFORM, texture, coords, true);
    }

    public void AddWoodPlank(Texture2D texture, Vector2 coords)
    {
        AddTiledTexture(GridTileType.WOODPLANK, texture, coords, true);
    }

    public void AddEscalator(Texture2D texture, Vector2 coords)
    {
        AddTiledTexture(GridTileType.ESCALATOR, texture, coords, true);
    }

    // Tile a base texture across an arbitrary area, wrapping the source as needed
    public void AddTiledArea(
        GridTileType type,
        Texture2D texture,
        Vector2 coords,
        int width,
        int height,
        bool isSolid
    )
    {
        if (texture == null)
            return;

        var areaRect = new Rectangle((int)coords.X, (int)coords.Y, width, height);

        int startTileX = Math.Max(0, (int)Math.Floor(coords.X / tileSize));
        int startTileY = Math.Max(0, (int)Math.Floor(coords.Y / tileSize));
        int endTileX = Math.Min(
            gridTiles.Length - 1,
            (int)Math.Floor(((coords.X + width - 1) / tileSize))
        );
        int endTileY = Math.Min(
            gridTiles[0].Length - 1,
            (int)Math.Floor(((coords.Y + height - 1) / tileSize))
        );

        for (int tx = startTileX; tx <= endTileX; tx++)
        {
            for (int ty = startTileY; ty <= endTileY; ty++)
            {
                var cellRect = new Rectangle(tx * tileSize, ty * tileSize, tileSize, tileSize);
                Rectangle overlap = Rectangle.Intersect(cellRect, areaRect);
                if (overlap.Width <= 0 || overlap.Height <= 0)
                    continue;

                // Compute source rectangle with texture wrapping
                int srcX = (overlap.X - areaRect.X) % texture.Width;
                if (srcX < 0)
                    srcX += texture.Width;
                int srcY = (overlap.Y - areaRect.Y) % texture.Height;
                if (srcY < 0)
                    srcY += texture.Height;

                var source = new Rectangle(srcX, srcY, overlap.Width, overlap.Height);
                gridTiles[tx][ty] = new GridTile(type, overlap, texture, source, isSolid);
            }
        }
    }

    // Convenience: add a casino machine as non-solid tiles (for occupancy/visuals only)
    public void AddCasinoMachine(Texture2D texture, Vector2 coords)
    {
        AddTiledTexture(GridTileType.CASINOMACHINE, texture, coords, false);
    }

    public IEnumerable<GridTile> GetAllTiles()
    {
        for (int x = 0; x < gridTiles.Length; x++)
        {
            for (int y = 0; y < gridTiles[x].Length; y++)
            {
                var tile = gridTiles[x][y];
                if (tile != null)
                    yield return tile;
            }
        }
    }

    public IEnumerable<Rectangle> GetAllHitboxes()
    {
        foreach (var tile in GetAllTiles())
        {
            yield return tile.Hitbox;
        }
    }

    // Only solid tiles' hitboxes for physics collisions
    public IEnumerable<Rectangle> GetSolidHitboxes()
    {
        foreach (var tile in GetAllTiles())
        {
            if (tile.IsSolid)
            {
                yield return tile.Hitbox;
            }
        }
    }

    // Place a single tile instance reconstructed from network/state
    public void PlaceTile(
        GridTileType type,
        Rectangle dest,
        Texture2D texture,
        Rectangle source,
        bool isSolid
    )
    {
        int tx = dest.X / tileSize;
        int ty = dest.Y / tileSize;
        if (tx < 0 || ty < 0 || tx >= gridTiles.Length || ty >= gridTiles[0].Length)
            return;
        gridTiles[tx][ty] = new GridTile(type, dest, texture, source, isSolid);
    }
}
