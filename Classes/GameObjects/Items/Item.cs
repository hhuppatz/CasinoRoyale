using System;
using System.Collections.Generic;
using CasinoRoyale.Classes.GameSystems;
using CasinoRoyale.Classes.Networking;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IDrawable = CasinoRoyale.Classes.GameObjects.Interfaces.IDrawable;

namespace CasinoRoyale.Classes.GameObjects.Items;

public enum ItemType
{
    COIN,
    SWORD,
}

// Abstract base class for all items
public abstract class Item(
    uint itemId,
    ItemType itemType,
    Texture2D tex,
    Vector2 coords,
    Vector2 startVelocity,
    float mass = 10.0f,
    float elasticity = 0.5f
)
    : GameEntity(
        coords,
        startVelocity,
        new Rectangle(coords.ToPoint(), new Point(tex.Bounds.Width, tex.Bounds.Height)),
        true,
        mass
    ),
        IDrawable
{
    // Item
    private readonly uint itemId = itemId;
    public uint ItemId
    {
        get => itemId;
    }
    private readonly ItemType itemType = itemType;
    public ItemType ItemType => itemType;
    private readonly float elasticity = elasticity;
    private float lifetime = 0;
    public float Lifetime
    {
        get => lifetime;
    }

    // IDrawable
    private Texture2D tex = tex;
    public Texture2D Texture
    {
        get => tex;
        set => tex = value;
    }

    public virtual void Update(float dt, Rectangle gameArea, IEnumerable<Rectangle> tileRects)
    {
        // Use the new generic physics system
        var physicsResult = PhysicsSystem.UpdatePhysics(
            gameArea,
            tileRects,
            Coords,
            Velocity,
            Hitbox,
            Mass,
            dt
        );
        Coords = physicsResult.newPosition;
        Velocity = physicsResult.newVelocity;

        // Apply elasticity when grounded (bounce with reduced velocity)
        if (physicsResult.isGrounded)
        {
            // Only apply elasticity if the coin is moving downward (has positive Y velocity)
            if (Velocity.Y > 0)
            {
                Velocity = new Vector2(Velocity.X * elasticity, -Math.Abs(Velocity.Y) * elasticity);
            }
        }

        lifetime += dt;
    }

    // Destroy item is same process as destroying any entity
    public void Destroy() => DestroyEntity();

    public abstract void Collect();

    public ItemState GetState()
    {
        return new ItemState
        {
            objectType = ObjectType.ITEM,
            itemType = itemType,
            itemId = itemId,
            gameEntityState = GetEntityState(),
        };
    }

    public void SetState(ItemState state)
    {
        // Update base properties directly - no change tracking needed
        base.Coords = state.gameEntityState.coords;
        base.Velocity = state.gameEntityState.velocity;
        base.Mass = state.gameEntityState.mass;
        if (state.gameEntityState.awake)
            AwakenEntity();
        else
            SleepEntity();

        ClearChangedFlag(); // Clear changed flag since we're setting the state
    }
}

public struct ItemState
{
    public ObjectType objectType;
    public ItemType itemType;
    public GameEntityState gameEntityState;
    public uint itemId;
}
