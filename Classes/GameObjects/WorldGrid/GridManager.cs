using System;
using System.Collections.Generic;
using CasinoRoyale.Classes.GameObjects.Platforms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.GameObjects.WorldGrid;

public class GridManager
{
    private readonly GridTile[][] gridTiles;
    private readonly int tileSize;
    private Vector2 worldOffset = Vector2.Zero;
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

    public void SetWorldOffset(Vector2 offset)
    {
        worldOffset = offset;
    }

    // General method to split a texture into grid-aligned tiles and store per-cell source rectangles
    // coords: world coordinates - will be converted to grid-relative internally
    public void AddTiledTexture(GridTileType type, Texture2D texture, Vector2 worldCoords, bool isSolid)
    {
        if (texture == null) return;

        // Convert world coordinates to grid-relative coordinates
        Vector2 gridCoords = new (
            worldCoords.X - worldOffset.X,
            worldCoords.Y - worldOffset.Y
        );

        Rectangle rect = new ((int)gridCoords.X, (int)gridCoords.Y, texture.Width, texture.Height);

        int startTileX = (int)Math.Floor(gridCoords.X / tileSize);
        int startTileY = (int)Math.Floor(gridCoords.Y / tileSize);
        int endTileX = (int)Math.Floor(((gridCoords.X + texture.Width - 1) / tileSize));
        int endTileY = (int)Math.Floor(((gridCoords.Y + texture.Height - 1) / tileSize));

        startTileX = Math.Max(0, startTileX);
        startTileY = Math.Max(0, startTileY);
        endTileX = Math.Min(gridTiles.Length - 1, endTileX);
        endTileY = Math.Min(gridTiles[0].Length - 1, endTileY);

        for (int tx = startTileX; tx <= endTileX; tx++)
        {
            for (int ty = startTileY; ty <= endTileY; ty++)
            {
                Rectangle cellRect = new (tx * tileSize, ty * tileSize, tileSize, tileSize);
                Rectangle overlap = Rectangle.Intersect(cellRect, rect);
                if (overlap.Width <= 0 || overlap.Height <= 0) continue;

                Rectangle source = new (
                    overlap.X - rect.X,
                    overlap.Y - rect.Y,
                    overlap.Width,
                    overlap.Height
                );

                // Convert hitbox back to world coordinates by adding the offset
                Rectangle worldHitbox = new (
                    overlap.X + (int)worldOffset.X,
                    overlap.Y + (int)worldOffset.Y,
                    overlap.Width,
                    overlap.Height
                );

                gridTiles[tx][ty] = new GridTile(type, worldHitbox, texture, source, isSolid);
                Console.WriteLine($"Added tile at {tx}, {ty} with type {type}");
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

    // Convenience: add a casino machine as non-solid tiles (for occupancy/visuals only)
    public void AddCasinoMachine(Texture2D texture, Vector2 coords)
    {
        AddTiledTexture(GridTileType.CASINOMACHINE, texture, coords, false);
    }

    public void AddGround(Texture2D texture, Vector2 worldCoords)
    {
        AddTiledTexture(GridTileType.GROUND, texture, worldCoords, true);
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
    // dest: world coordinates (will be converted to grid-relative internally)
    public void PlaceTile(
        GridTileType type,
        Rectangle dest,
        Texture2D texture,
        Rectangle source,
        bool isSolid
    )
    {
        // Convert world coordinates to grid-relative coordinates
        Vector2 gridCoords = new(
            dest.X - worldOffset.X,
            dest.Y - worldOffset.Y
        );

        int tx = (int)Math.Floor(gridCoords.X / tileSize);
        int ty = (int)Math.Floor(gridCoords.Y / tileSize);
        if (tx < 0 || ty < 0 || tx >= gridTiles.Length || ty >= gridTiles[0].Length)
            return;
        gridTiles[tx][ty] = new GridTile(type, dest, texture, source, isSolid);
    }

    public int[] CoordsToGrid(Vector2 coords)
    {
        return [(int)coords.X / tileSize, (int)coords.Y / tileSize];
    }

    // Check if a world position (rectangle) overlaps with any solid tiles
    public bool IsPositionOccupied(Rectangle worldRect)
    {
        // Convert world coordinates to grid-relative coordinates
        Vector2 gridCoords = new(
            worldRect.X - worldOffset.X,
            worldRect.Y - worldOffset.Y
        );

        int startTileX = (int)Math.Floor(gridCoords.X / tileSize);
        int startTileY = (int)Math.Floor(gridCoords.Y / tileSize);
        int endTileX = (int)Math.Floor(((gridCoords.X + worldRect.Width - 1) / tileSize));
        int endTileY = (int)Math.Floor(((gridCoords.Y + worldRect.Height - 1) / tileSize));

        startTileX = Math.Max(0, startTileX);
        startTileY = Math.Max(0, startTileY);
        endTileX = Math.Min(gridTiles.Length - 1, endTileX);
        endTileY = Math.Min(gridTiles[0].Length - 1, endTileY);

        for (int tx = startTileX; tx <= endTileX; tx++)
        {
            for (int ty = startTileY; ty <= endTileY; ty++)
            {
                var tile = gridTiles[tx][ty];
                if (tile != null && tile.IsSolid)
                {
                    // Check if the tile's hitbox overlaps with the requested position
                    if (tile.Hitbox.Intersects(worldRect))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // Check if a world position is empty (no solid tiles)
    public bool IsPositionEmpty(Vector2 worldPos, int width, int height)
    {
        Rectangle rect = new((int)worldPos.X, (int)worldPos.Y, width, height);
        return !IsPositionOccupied(rect);
    }
}
