using LiteNetLib.Utils;
using Microsoft.Xna.Framework;

public interface GameEntity : IPhysics, IHittable
{

    public void SetCoords(Vector2 coords);

    public void AwakenEntity();

    public void SleepEntity();

    // getters
    public GameEntityState GetEntityState();

    public Vector2 GetCoords();

    public Vector2 GetVelocity();
}

public struct GameEntityState : INetSerializable
{
    public bool awake;
    public Vector2 coords;
    public Vector2 velocity;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(awake);
        writer.Put(coords);
        writer.Put(velocity);
    }

    public void Deserialize(NetDataReader reader)
    {
        awake = reader.GetBool();
        coords = reader.GetVector2();
        velocity = reader.GetVector2();
    }
}
