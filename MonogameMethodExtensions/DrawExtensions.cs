using CSharpFirstPerson;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public static class DrawExtensions
{
    public static void DrawEntity<T>(this SpriteBatch _spriteBatch, MainCamera _mainCamera, T drawable)
    where T : IObject, IDrawable
    {
        _spriteBatch.Draw(drawable.GetTex(),
                            _mainCamera.TransformToView(drawable.Coords),
                            null,
                            Color.White,
                            0.0f,
                            Vector2.Zero,
                            //new Vector2(drawable.GetTex().Bounds.Width/2, drawable.GetTex().Bounds.Height/2),
                            Resolution.ratio,
                            0,
                            0);
    }
}