using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public interface IDrawable
{
    public void SetTex(Texture2D tex);
    public Texture2D GetTex();
    public void SetCoords(Vector2 coords);
    public Vector2 GetCoords();

}