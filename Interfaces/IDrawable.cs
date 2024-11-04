using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public interface IDrawable
{
    public void SetCoords(Vector2 coords);
    public void SetTex(Texture2D tex);
    public Vector2 GetCoords();
    public Texture2D GetTex();

}