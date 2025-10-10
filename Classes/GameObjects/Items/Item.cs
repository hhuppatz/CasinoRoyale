using System;
using System.Collections.Generic;
using CasinoRoyale.Classes.GameObjects.Platforms;
using CasinoRoyale.Classes.GameSystems;
using CasinoRoyale.Classes.Networking;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.GameObjects.Items;

public enum ItemType
{
    COIN,
    OTHER
}

public class Item(uint itemId, ItemType itemType, Texture2D tex, Vector2 coords, Vector2 startVelocity, float mass = 4.0f) : GameEntity(coords, startVelocity, new Rectangle(coords.ToPoint(), new Point(tex.Bounds.Width, tex.Bounds.Height)), true, mass)
{
    private readonly uint itemId = itemId;
    private readonly ItemType itemType = itemType;
    private float lifetime = 0;
    public ItemType ItemType => itemType;
    private readonly Texture2D tex = tex;
    private readonly float elasticity = 0.5f;
    // Items handle their own physics deterministically - no change tracking needed
    
    public uint ItemId => itemId;
    public Texture2D GetTexture() => tex;
    
    public void Update(float dt, Rectangle gameArea, List<Platform> platforms)
    {
        // Use the new generic physics system
        var physicsResult = PhysicsSystem.UpdatePhysics(gameArea, platforms, Coords, Velocity, Hitbox, Mass, dt);
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

        switch (itemType)
        {
            case ItemType.COIN:
                if (lifetime > 10)
                {
                    DestroyEntity();
                }
            break;
            case ItemType.OTHER:

            break;
        }

        lifetime += dt;
    }
    
    public ItemState GetState()
    {
        return new ItemState
        {
            objectType = ObjectType.ITEM,
            itemType = itemType,
            itemId = itemId,
            gameEntityState = GetEntityState()
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

public struct ItemState : INetSerializable
{
    public ObjectType objectType;
    public ItemType itemType;
    public GameEntityState gameEntityState;
    public uint itemId;
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)objectType);
        writer.Put((byte)itemType);
        writer.Put(itemId);
        gameEntityState.Serialize(writer);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        objectType = (ObjectType)reader.GetByte();
        itemType = (ItemType)reader.GetByte();
        itemId = reader.GetUInt();
        gameEntityState = new GameEntityState();
        gameEntityState.Deserialize(reader);
    }
}