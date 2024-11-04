using System;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;

public class GameEntity : IExist, IPhysics, IHittable
{
    private bool awake;
    private Vector2 velocity;
    private Rectangle hitbox;
    private event EventHandler<MovementEventArgs> MovementEvent;
    private Vector2 coords;

    protected virtual void OnMovement(MovementEventArgs e)
    {
        MovementEvent?.Invoke(this, e);
    }

    public GameEntity(Vector2 coords, Vector2 velocity, Rectangle hitbox, bool awake) {
        this.awake = awake;
        this.coords = coords;
        this.velocity = velocity;
        this.hitbox = hitbox;
        MovementEvent += UpdateHitbox;
    }

    private void UpdateHitbox(object s, MovementEventArgs e)
    {
        hitbox = new Rectangle(GetCoords().ToPoint(), hitbox.Size);
    }

    // setters
    public void SetCoords(Vector2 coords)
    {
        if (!this.coords.Equals(coords))
        {
            this.coords = coords;
            OnMovement(new MovementEventArgs { coords = coords });
        }
    }

    public void SetVelocity(Vector2 velocity)
    {
        this.velocity = velocity;
    }

    public void AwakenEntity()
    {
        awake = true;
    }

    public void SleepEntity()
    {
        awake = false;
    }

    // getters
    public GameEntityState GetEntityState()
    {
        return new GameEntityState {
            awake = awake,
            coords = coords,
            velocity = velocity
        };
    }

    public Vector2 GetCoords()
    {
        return coords;
    }

    public Vector2 GetVelocity()
    {
        return velocity;
    }

    public Rectangle GetHitbox()
    {
        return hitbox;
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

public class MovementEventArgs : EventArgs
{
    public Vector2 coords { get; set; }
}
