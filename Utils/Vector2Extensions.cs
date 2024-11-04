using Microsoft.Xna.Framework;

public static class Vector2Extensions
{
    public static Point ToPoint(this Vector2 vec)
    {
        return new Point((int)vec.X, (int)vec.Y);
    }
}