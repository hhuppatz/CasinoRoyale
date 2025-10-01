using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using CasinoRoyale.GameObjects;

namespace CasinoRoyale.Networking
{
    public static class SerializingExtensions
{
    public static void Put(this NetDataWriter writer, Vector2 vector) {
        writer.Put(vector.X);
        writer.Put(vector.Y);
    }

    public static Vector2 GetVector2(this NetDataReader reader) {
        return new Vector2(reader.GetFloat(), reader.GetFloat());
    }

    public static void Put(this NetDataWriter writer, GameEntityState ges) {
        writer.Put(ges.awake);
        writer.Put(ges.coords);
        writer.Put(ges.velocity);
    }

    public static GameEntityState GetGES(this NetDataReader reader) {
        return new GameEntityState { awake = reader.GetBool(), coords = reader.GetVector2(), velocity = reader.GetVector2() };
    }

    public static void Put(this NetDataWriter writer, Rectangle rectangle) {
        writer.Put(rectangle.X);
        writer.Put(rectangle.Y);
        writer.Put(rectangle.Width);
        writer.Put(rectangle.Height);
    }

    public static Rectangle GetRectangle(this NetDataReader reader) {
        return new Rectangle(reader.GetInt(), reader.GetInt(), reader.GetInt(), reader.GetInt());
    }
}
}