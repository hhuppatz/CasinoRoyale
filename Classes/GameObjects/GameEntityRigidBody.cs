using System;
using Microsoft.Xna.Framework;
using CasinoRoyale.Classes.GameObjects.Interfaces;

namespace CasinoRoyale.Classes.GameObjects
{
    public class GameEntityRigidBody : IRigidBody, IHitbox, IMovement
{
    private bool awake;
    private event EventHandler<EntityMovementEventArgs> MovementEvent;
    private Vector2 _coords;
    public Vector2 Coords { get => _coords; set { _coords = value; OnMovement(new EntityMovementEventArgs { coords = _coords });}  }
    private Vector2 _coordsDash;
    public Vector2 CoordsDash { get => _coordsDash; set => _coordsDash = value; }
    private Vector2 _velocity;
    public Vector2 Velocity { get => _velocity; set => _velocity = value; }
    private Rectangle _hitbox;
    public Rectangle Hitbox { get => _hitbox; set => _hitbox = value; }
    private float _m;
    public float M { get => _m; }
    private Vector2 _g;
    public Vector2 G { get => _g; }
    private Vector2 _f;
    public Vector2 F { get => _f; set => _f = value; }

    public GameEntityRigidBody(Vector2 coords, Vector2 velocity, Rectangle hitbox, bool awake) {
        this.awake = awake;
        Coords = coords;
        Velocity = velocity;
        Hitbox = hitbox;
        MovementEvent += UpdateHitbox;
        _m = 100;
        _g = new Vector2(0, 980f);
        _f = Vector2.Zero;
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
    

    public void SatisfyConstraints(Rectangle gameArea)
    {
        Coords = Vector2.Min(Vector2.Max(Coords, new Vector2(gameArea.Left, gameArea.Top)), new Vector2(gameArea.Right, gameArea.Bottom));
    }

    public void VerletMove(float dt)
    {
        Vector2 m_NewCoords = 2 * Coords - CoordsDash + (F/M + G) * dt * dt;
        CoordsDash = Coords;
        Coords = m_NewCoords;
    }
}
}