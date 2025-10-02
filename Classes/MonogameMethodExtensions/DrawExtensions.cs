using CasinoRoyale.Classes.GameObjects;
using CasinoRoyale.Classes.GameObjects.Interfaces;
using CasinoRoyale.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.MonogameMethodExtensions
{
    public static class DrawExtensions
    {
        public static void DrawEntity<T>(this SpriteBatch _spriteBatch, MainCamera _mainCamera, T drawable)
        where T : CasinoRoyale.Classes.GameObjects.Interfaces.IObject, CasinoRoyale.Classes.GameObjects.Interfaces.IDrawable
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