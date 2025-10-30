using CasinoRoyale.Classes.GameObjects.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IDrawable = CasinoRoyale.Classes.GameObjects.Interfaces.IDrawable;
using CasinoRoyale.Classes.GameUtilities;

namespace CasinoRoyale.Classes.MonogameMethodExtensions;

public static class DrawExtensions
{
    public static void DrawEntity<T>(this SpriteBatch _spriteBatch, MainCamera _mainCamera, T drawable)
    where T : IObject, IDrawable
    {
        _spriteBatch.Draw(drawable.Texture,
                            _mainCamera.TransformToView(drawable.Coords),
                            null,
                            Color.White,
                            0.0f,
                            Vector2.Zero,  // Top-left origin to match hitbox positioning
                            CasinoRoyale.Utils.Resolution.ratio,
                            0,
                            0);
    }
}