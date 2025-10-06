using System;
using CasinoRoyale.Classes.GameSystems;
using CasinoRoyale.Classes.Networking;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.GameObjects
{
public class Coin(uint coinId, Texture2D tex, Vector2 coords, Vector2 startVelocity, float mass = 4.0f) : GameEntity(coords, startVelocity, new Rectangle(coords.ToPoint(), new Point(tex.Bounds.Width, tex.Bounds.Height)), true, mass)
{
    private readonly uint coinId = coinId;
    private readonly Texture2D tex = tex;
    private readonly float elasticity = 0.5f;
    
    public uint CoinId => coinId;
    public Texture2D GetTexture() => tex;

        public void Update(float dt, Rectangle gameArea, GameWorldObjects gameWorldObjects)
    {
        // Use the new generic physics system
        var physicsResult = PhysicsSystem.UpdatePhysics(gameArea, gameWorldObjects, Coords, Velocity, Hitbox, Mass, dt);
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
    }
    
    public CoinState GetState()
    {
        return new CoinState
        {
            coinId = coinId,
            coords = Coords,
            velocity = Velocity
        };
    }
}

public struct CoinState : INetSerializable
{
    public uint coinId;
    public Vector2 coords;
    public Vector2 velocity;
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(coinId);
        writer.Put(coords);
        writer.Put(velocity);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        coinId = reader.GetUInt();
        coords = reader.GetVector2();
        velocity = reader.GetVector2();
    }
}
}