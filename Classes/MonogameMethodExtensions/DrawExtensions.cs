using CasinoRoyale.GameObjects;
using CasinoRoyale.GameObjects.Interfaces;
using CasinoRoyale.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.MonogameMethodExtensions
{
    public static class DrawExtensions
    {
        public static void DrawEntity<T>(this SpriteBatch _spriteBatch, MainCamera _mainCamera, T drawable)
        where T : CasinoRoyale.GameObjects.Interfaces.IObject, CasinoRoyale.GameObjects.Interfaces.IDrawable
        {
            _spriteBatch.Draw(drawable.GetTex(),
                                _mainCamera.TransformToView(drawable.Coords),
                                null,
                                Color.White,
                                0.0f,
                                Vector2.Zero,
                                //new Vector2(drawable.GetTex().Bounds.Width/2, drawable.GetTex().Bounds.Height/2),
                                CasinoRoyale.Utils.Resolution.ratio,
                                0,
                                0);
        }
    }
}