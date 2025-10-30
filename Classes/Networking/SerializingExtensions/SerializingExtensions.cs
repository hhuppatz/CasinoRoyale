using CasinoRoyale.Classes.GameObjects;
using CasinoRoyale.Classes.GameObjects.Items;
using CasinoRoyale.Classes.GameObjects.Platforms;
using CasinoRoyale.Classes.GameObjects.Player;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;

namespace CasinoRoyale.Classes.Networking.SerializingExtensions;

public static class SerializingExtensions
{
    public static void SerializeVector2(NetDataWriter writer, Vector2 vector)
    {
        writer.Put(vector.X);
        writer.Put(vector.Y);
    }

    private static Vector2 DeserializeVector2(NetDataReader reader)
    {
        return new Vector2(reader.GetFloat(), reader.GetFloat());
    }

    private static void SerializeRectangle(NetDataWriter writer, Rectangle rectangle)
    {
        writer.Put(rectangle.X);
        writer.Put(rectangle.Y);
        writer.Put(rectangle.Width);
        writer.Put(rectangle.Height);
    }

    private static Rectangle DeserializeRectangle(NetDataReader reader)
    {
        return new Rectangle(reader.GetInt(), reader.GetInt(), reader.GetInt(), reader.GetInt());
    }

    private static void SerializeGameEntityState(NetDataWriter writer, GameEntityState ges)
    {
        writer.Put(ges.awake);
        SerializeVector2(writer, ges.coords);
        SerializeVector2(writer, ges.velocity);
        writer.Put(ges.mass);
    }

    private static GameEntityState DeserializeGameEntityState(NetDataReader reader)
    {
        return new GameEntityState
        {
            awake = reader.GetBool(),
            coords = DeserializeVector2(reader),
            velocity = DeserializeVector2(reader),
            mass = reader.GetFloat(),
        };
    }

    private static void SerializePlayerState(NetDataWriter writer, PlayerState playerState)
    {
        writer.Put((byte)playerState.objectType);
        writer.Put(playerState.pid);
        writer.Put(playerState.username);
        SerializeGameEntityState(writer, playerState.ges);
        writer.Put(playerState.initialJumpVelocity);
        writer.Put(playerState.maxRunSpeed);
    }

    private static PlayerState DeserializePlayerState(NetDataReader reader)
    {
        return new PlayerState
        {
            objectType = (ObjectType)reader.GetByte(),
            pid = reader.GetUInt(),
            username = reader.GetString(),
            ges = DeserializeGameEntityState(reader),
            initialJumpVelocity = reader.GetFloat(),
            maxRunSpeed = reader.GetFloat(),
        };
    }

    private static void SerializeItemState(NetDataWriter writer, ItemState itemState)
    {
        writer.Put((byte)itemState.objectType);
        writer.Put(itemState.itemId);
        writer.Put((byte)itemState.itemType);
        SerializeGameEntityState(writer, itemState.gameEntityState);
    }

    private static ItemState DeserializeItemState(NetDataReader reader)
    {
        return new ItemState
        {
            objectType = (ObjectType)reader.GetByte(),
            itemId = reader.GetUInt(),
            itemType = (ItemType)reader.GetByte(),
            gameEntityState = DeserializeGameEntityState(reader),
        };
    }
}
