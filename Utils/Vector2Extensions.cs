using Microsoft.Xna.Framework;

namespace CasinoRoyale.Utils;

public static class Vector2Extensions
    {
        public static Point ToPoint(this Vector2 vec)
        {
            return new Point((int)vec.X, (int)vec.Y);
        }
    }