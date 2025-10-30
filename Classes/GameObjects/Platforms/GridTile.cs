using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CasinoRoyale.Classes.GameObjects.Interfaces;
using IDrawable = CasinoRoyale.Classes.GameObjects.Interfaces.IDrawable;

namespace CasinoRoyale.Classes.GameObjects.Platforms;

public class GridTile(GridTileType type, Rectangle hitbox, Texture2D texture, Rectangle source, bool isSolid) : IDrawable
{
    public bool IsSolid { get; } = isSolid;
    public GridTileType Type { get; } = type;
    public Rectangle Hitbox { get; } = hitbox;
    public Rectangle Source { get; } = source;
    private Texture2D texture = texture;
    public Texture2D Texture { get => texture; set => texture = value; }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(Texture, Hitbox, Source, Color.White);
    }
}

public enum GridTileType
{
    PLATFORM,
    WOODPLANK,
    ESCALATOR,
    WALL,
    CASINOMACHINE,
}