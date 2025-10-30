using System;
using Microsoft.Xna.Framework;
using CasinoRoyale.Classes.GameObjects.Interfaces;
using CasinoRoyale.Classes.Networking;
using LiteNetLib.Utils;

namespace CasinoRoyale.Classes.GameObjects;

public abstract class GameEntity : IObject, IHitbox, INetworkObject
{
    private event EventHandler<EntityMovementEventArgs> MovementEvent;
    public event Action<string, INetSerializable> OnChanged;
    private readonly NetworkComponent _network;
    private uint _networkObjectId;
    public uint NetworkObjectId { get => _networkObjectId; set => _networkObjectId = value; }

    private bool awake;
    private bool destroyed = false;
    public bool Destroyed { get => destroyed; set { destroyed = value; MarkAsChanged(); } }
    
    private Vector2 _coords;
    public Vector2 Coords { get => _coords; set { _coords = value; MarkAsChanged(); OnMovement(new EntityMovementEventArgs { coords = _coords });}  }
    
    private Vector2 _velocity;
    public Vector2 Velocity { get => _velocity; set { _velocity = value; MarkAsChanged(); } }

    private Rectangle _hitbox;
    public Rectangle Hitbox { get => _hitbox; set { _hitbox = value; MarkAsChanged(); } }

    private float mass;
    public float Mass { get => mass; set { mass = value; MarkAsChanged(); } }

    private bool _hasChanged = false;
    public bool HasChanged { get => _hasChanged; set => _hasChanged = value; }

    public GameEntity(Vector2 coords, Vector2 velocity, Rectangle hitbox, bool awake, float mass = 1.0f) {
        this.awake = awake;
        Coords = coords;
        Velocity = velocity;
        Hitbox = hitbox;
        Mass = mass;
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
        Hitbox = new Rectangle((int)Math.Round(e.coords.X), (int)Math.Round(e.coords.Y), Hitbox.Width, Hitbox.Height);
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
        return new GameEntityState {
            awake = awake,
            coords = Coords,
            velocity = Velocity,
            mass = Mass
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

public class EntityMovementEventArgs : EventArgs
{
    public Vector2 coords { get; set; }
}