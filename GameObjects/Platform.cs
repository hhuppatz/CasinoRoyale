using System;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Platform : IDrawable, ICollidable
{
    private uint platNum;
    private Vector2 L;
    private Vector2 R;
    private Texture2D tex;
    private event EventHandler<PlatformMovementEventArgs> MovementEvent;
    private Rectangle _hitbox;
    public Rectangle Hitbox { get => _hitbox; set => _hitbox = value; }

    public Platform(uint platNum, Texture2D tex, Vector2 L, Vector2 R)
    {
        this.tex = tex;
        this.platNum = platNum;
        this.L = L;
        this.R = R;
        Hitbox = new Rectangle(L.ToPoint() - new Point(tex.Bounds.Width/2, tex.Bounds.Height/2), new Point(tex.Bounds.Width, tex.Bounds.Height));
        MovementEvent += UpdateHitbox;
        MovementEvent += UpdateRCoords;
    }

    public PlatformState GetState()
    {
        return new PlatformState {
            platNum = platNum,
            L = L,
            R = R
        };
    }

    public Vector2 GetLCoords()
    {
        return L;
    }

    public Vector2 GetRCoords()
    {
        return R;
    }

    public int GetWidth()
    {
        return Math.Abs((int)L.X - (int)R.X);
    }

    public Texture2D GetTex()
    {
        return tex;
    }

    public Vector2 GetCoords()
    {
        return L;
    }

    public void SetTex(Texture2D tex)
    {
        this.tex = tex;
    }

    public void SetCoords(Vector2 coords)
    {
        if (!L.Equals(coords))
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
        float platLen = R.X - L.X;
        L = coords;
        OnMovement(new PlatformMovementEventArgs { coords = coords, length = platLen });
    }

    private void UpdateRCoords(object s, PlatformMovementEventArgs e)
    {
        R = new Vector2(e.coords.X + e.length, e.coords.Y);
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
    public Vector2 L;
    public Vector2 R;
    public void Serialize(NetDataWriter writer) {
        writer.Put(platNum);
        writer.Put(L);
        writer.Put(R);
    }
    public void Deserialize(NetDataReader reader) {
        platNum = reader.GetUInt();
        L = reader.GetVector2();
        R = reader.GetVector2();
    }
}

public class PlatformMovementEventArgs : EventArgs
{
    public Vector2 coords { get; set; }
    public float length { get; set; }
}