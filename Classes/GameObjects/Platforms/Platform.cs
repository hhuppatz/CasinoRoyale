using System;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CasinoRoyale.Classes.GameObjects.Interfaces;
using CasinoRoyale.Classes.Networking;

namespace CasinoRoyale.Classes.GameObjects
{
    public class Platform : CasinoRoyale.Classes.GameObjects.Interfaces.IDrawable, IHitbox
{
    private uint platNum;
    private Vector2 TL;
    private Vector2 BR;
    private Texture2D tex;
    private event EventHandler<PlatformMovementEventArgs> MovementEvent;
    private Rectangle _hitbox;
    public Rectangle Hitbox { get => _hitbox; set => _hitbox = value; }

    public Platform(uint platNum, Texture2D tex, Vector2 TL, Vector2 BR)
    {
        this.tex = tex;
        this.platNum = platNum;
        this.TL = TL;
        this.BR = BR;
        
        // Create hitbox using full texture dimensions
        int platformWidth = BR.ToPoint().X - TL.ToPoint().X;
        int platformHeight = tex.Height;
        
        Hitbox = new Rectangle(TL.ToPoint().X, TL.ToPoint().Y, platformWidth, platformHeight);
        MovementEvent += UpdateHitbox;
        MovementEvent += UpdateRCoords;
    }

    public PlatformState GetState()
    {
        return new PlatformState {
            platNum = platNum,
            TL = TL,
            BR = BR
        };
    }

    public Vector2 GetLCoords()
    {
        return TL;
    }

    public Vector2 GetRCoords()
    {
        return BR;
    }

    public int GetWidth()
    {
        return Math.Abs((int)TL.X - (int)BR.X);
    }

    public Texture2D GetTex()
    {
        return tex;
    }

    public Vector2 GetCoords()
    {
        return TL;
    }

    public void SetTex(Texture2D tex)
    {
        this.tex = tex;
    }

    public void SetCoords(Vector2 coords)
    {
        if (!TL.Equals(coords))
        {
            SetLCoords(coords);
        }
    }

    protected virtual void OnMovement(PlatformMovementEventArgs e)
    {
        MovementEvent?.Invoke(this, e);
    }

    private void SetLCoords(Vector2 coords)
    {
        TL = coords;
        OnMovement(new PlatformMovementEventArgs { coords = coords, length = GetWidth() });
    }

    private void UpdateRCoords(object s, PlatformMovementEventArgs e)
    {
        BR = new Vector2(e.coords.X + e.length, e.coords.Y);
    }

    private void UpdateHitbox(object s, PlatformMovementEventArgs e)
    {
        Hitbox = new Rectangle(e.coords.ToPoint(), Hitbox.Size);
    }

    // Platforms are static by default
    public void SetVelocity(Vector2 velocity) {}
    public Vector2 GetVelocity() { return Vector2.Zero; }
}

public struct PlatformState: INetSerializable
{
    public uint platNum;
    public Vector2 TL;
    public Vector2 BR;
    public void Serialize(NetDataWriter writer) {
        writer.Put(platNum);
        writer.Put(TL);
        writer.Put(BR);
    }
    public void Deserialize(NetDataReader reader) {
        platNum = reader.GetUInt();
        TL = reader.GetVector2();
        BR = reader.GetVector2();
    }
}

    public class PlatformMovementEventArgs : EventArgs
    {
        public Vector2 coords { get; set; }
        public float length { get; set; }
    }
}