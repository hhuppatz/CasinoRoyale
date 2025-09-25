using System;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using CasinoRoyale.GameObjects.Interfaces;
using CasinoRoyale.Players.Common.Networking;

namespace CasinoRoyale.GameObjects
{
    public class GameEntity : IObject, IHitbox, IMovement
{
    private bool awake;
    private event EventHandler<EntityMovementEventArgs> MovementEvent;
    private Vector2 _coords;
    public Vector2 Coords { get => _coords; set { _coords = value; OnMovement(new EntityMovementEventArgs { coords = _coords });}  }
    private Vector2 _velocity;
    public Vector2 Velocity { get => _velocity; set => _velocity = value; }
    private Rectangle _hitbox;
    public Rectangle Hitbox { get => _hitbox; set => _hitbox = value; }

    public GameEntity(Vector2 coords, Vector2 velocity, Rectangle hitbox, bool awake) {
        this.awake = awake;
        Coords = coords;
        Velocity = velocity;
        Hitbox = hitbox;
        MovementEvent += UpdateHitbox;
    }

    public void Move(float dt)
    {
        Coords += Velocity * dt;
    }

    protected virtual void OnMovement(EntityMovementEventArgs e)
    {
        MovementEvent?.Invoke(this, e);
    }

    private void UpdateHitbox(object s, EntityMovementEventArgs e)
    {
        Hitbox = new Rectangle(e.coords.ToPoint(), Hitbox.Size);
    }

    public void AwakenEntity()
    {
        awake = true;
    }

    public void SleepEntity()
    {
        awake = false;
    }

    public GameEntityState GetEntityState()
    {
        return new GameEntityState {
            awake = awake,
            coords = Coords,
            velocity = Velocity
        };
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
}
