using LiteNetLib.Utils;
using Microsoft.Xna.Framework;

public class GameEntity : IPhysics, IHittable
{
    private GameEntityState ges;
    public GameEntity(bool awake, Vector2 coords, Vector2 initialVelocity)
    {
        ges = new GameEntityState { awake = awake, coords = coords, velocity = initialVelocity};
    }

    public void SetCoords(Vector2 coords)
    {
        ges.coords = coords;
    }

    public void AwakenEntity()
    {
        ges.awake = true;
    }

    public void SleepEntity()
    {
        ges.awake = false;
    }

    // getters
    public GameEntityState GetEntityState()
    {
        return ges;
    }

    public Vector2 GetCoords()
    {
        return ges.coords;
    }

    public Vector2 GetVelocity()
    {
        return ges.velocity;
    }
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
