using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class CasinoMachine
{
    private readonly Texture2D tex;
    private readonly Vector2 coords;
    public CasinoMachine(Texture2D tex, Vector2 coords)
    {
        this.tex = tex;
        this.coords = coords;
    }

    public Texture2D GetTex()
    {
        return tex;
    }

    public Vector2 GetCoords()
    {
        return coords;
    }
}