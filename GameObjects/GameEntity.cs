using System;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;

public class GameEntity : ICollidable
{
    private bool awake;
    private Vector2 velocity;
    private event EventHandler<EntityMovementEventArgs> MovementEvent;
    private Vector2 coords;
    private Rectangle _hitbox;
    public Rectangle Hitbox { get => _hitbox; set => _hitbox = value; }

    public GameEntity(Vector2 coords, Vector2 velocity, Rectangle hitbox, bool awake) {
        this.awake = awake;
        this.coords = coords;
        this.velocity = velocity;
        Hitbox = hitbox;
        MovementEvent += UpdateHitbox;
    }

    protected virtual void OnMovement(EntityMovementEventArgs e)
    {
        MovementEvent?.Invoke(this, e);
    }

    private void UpdateHitbox(object s, EntityMovementEventArgs e)
    {
        Hitbox = new Rectangle(e.coords.ToPoint(), Hitbox.Size);
    }

    // setters
    public void SetCoords(Vector2 coords)
    {
        if (!this.coords.Equals(coords))
        {
            this.coords = coords;
            OnMovement(new EntityMovementEventArgs { coords = coords });
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

public class EntityMovementEventArgs : EventArgs
{
    public Vector2 coords { get; set; }
}
