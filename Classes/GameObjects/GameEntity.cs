using System;
using CasinoRoyale.Classes.GameObjects.Interfaces;
using CasinoRoyale.Classes.Networking;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;

namespace CasinoRoyale.Classes.GameObjects;

public abstract class GameEntity : IObject, IHitbox, INetworkObject
{
    private event EventHandler<EntityMovementEventArgs> MovementEvent;
    public event Action<string, INetSerializable> OnChanged;
    private readonly NetworkComponent _network;
    private uint _networkObjectId;
    public uint NetworkObjectId
    {
        get => _networkObjectId;
        set => _networkObjectId = value;
    }

    private bool awake;
    private bool destroyed = false;
    public bool Destroyed
    {
        get => destroyed;
        set
        {
            destroyed = value;
            MarkAsChanged();
        }
    }

    private Vector2 _coords;
    public Vector2 Coords
    {
        get => _coords;
        set
        {
            if (_coords == value)
                return;
            _coords = value;
            OnMovement(new EntityMovementEventArgs { coords = _coords });
            MarkAsChanged();
        }
    }

    private Vector2 _velocity;
    public Vector2 Velocity
    {
        get => _velocity;
        set
        {
            if (_velocity == value)
                return;
            _velocity = value;
            MarkAsChanged();
        }
    }

    private Rectangle _hitbox;
    public Rectangle Hitbox
    {
        get => _hitbox;
        set
        {
            if (_hitbox == value)
                return;
            _hitbox = value;
            MarkAsChanged();
        }
    }

    private float mass;
    public float Mass
    {
        get => mass;
        set
        {
            if (Math.Abs(mass - value) < float.Epsilon)
                return;
            mass = value;
            MarkAsChanged();
        }
    }

    private bool _hasChanged = false;
    public bool HasChanged
    {
        get => _hasChanged;
        set => _hasChanged = value;
    }

    public GameEntity(
        Vector2 coords,
        Vector2 velocity,
        Rectangle hitbox,
        bool awake,
        float mass = 1.0f
    )
    {
        this.awake = awake;
        // Initialize backing fields directly to avoid emitting change events during construction
        _coords = coords;
        _velocity = velocity;
        _hitbox = hitbox;
        this.mass = mass;
        MovementEvent += UpdateHitbox;
        _network = new NetworkComponent(this);
    }

    public void Move(float dt)
    {
        Coords += Velocity * dt * Mass;
    }

    protected virtual void OnMovement(EntityMovementEventArgs e)
    {
        MovementEvent?.Invoke(this, e);
    }

    private void UpdateHitbox(object s, EntityMovementEventArgs e)
    {
        var newRect = new Rectangle(
            (int)Math.Round(e.coords.X),
            (int)Math.Round(e.coords.Y),
            Hitbox.Width,
            Hitbox.Height
        );
        if (_hitbox == newRect)
            return;
        // Assign backing field directly to avoid re-triggering change notification
        _hitbox = newRect;
    }

    public void AwakenEntity()
    {
        awake = true;
        MarkAsChanged();
    }

    public void SleepEntity()
    {
        awake = false;
        MarkAsChanged();
    }

    public void DestroyEntity()
    {
        destroyed = true;
        MarkAsChanged();
    }

    public void MarkAsChanged()
    {
        HasChanged = true;
        OnChanged?.Invoke("state", null);
    }

    public void ClearChangedFlag()
    {
        HasChanged = false;
    }

    public bool CollidedWith(IHitbox c)
    {
        return Hitbox.Intersects(c.Hitbox);
    }

    public GameEntityState GetEntityState()
    {
        return new GameEntityState
        {
            awake = awake,
            coords = Coords,
            velocity = Velocity,
            mass = Mass,
        };
    }
}

public struct GameEntityState
{
    public bool awake;
    public Vector2 coords;
    public Vector2 velocity;
    public float mass;
}

// Wrapper to make GameEntityState INetSerializable for networking
public class GameEntityStatePacket : INetSerializable
{
    public GameEntityState state;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(state.awake);
        writer.Put(state.coords.X);
        writer.Put(state.coords.Y);
        writer.Put(state.velocity.X);
        writer.Put(state.velocity.Y);
        writer.Put(state.mass);
    }

    public void Deserialize(NetDataReader reader)
    {
        state = new GameEntityState
        {
            awake = reader.GetBool(),
            coords = new Vector2(reader.GetFloat(), reader.GetFloat()),
            velocity = new Vector2(reader.GetFloat(), reader.GetFloat()),
            mass = reader.GetFloat(),
        };
    }
}

public class EntityMovementEventArgs : EventArgs
{
    public Vector2 coords { get; set; }
}
