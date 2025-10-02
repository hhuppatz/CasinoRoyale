using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.GameObjects.Interfaces
{
    public interface IDrawable
{
    public void SetTex(Texture2D tex);
    public Texture2D GetTex();

}
}