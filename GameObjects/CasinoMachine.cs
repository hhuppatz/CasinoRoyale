using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CasinoRoyale.GameObjects.Interfaces;
using CasinoRoyale.Players.Common.Networking;

namespace CasinoRoyale.GameObjects
{
    public class CasinoMachine : IObject, IHitbox
{
    private readonly Texture2D tex;
    private readonly uint machineNum;
    private Vector2 _coords;
    public Vector2 Coords { get => _coords; set => _coords = value; }
    private Rectangle _hitbox;
    public Rectangle Hitbox { get => _hitbox; set => _hitbox = value; }

    public CasinoMachine(uint machineNum, Texture2D tex, Vector2 coords)
    {
        this.tex = tex;
        this.machineNum = machineNum;
        _coords = coords;
        _hitbox = new Rectangle(coords.ToPoint(), new Point(tex.Bounds.Width, tex.Bounds.Height));
    }

    public Texture2D GetTex()
    {
        return tex;
    }

    public CasinoMachineState GetState()
    {
        return new CasinoMachineState{
            machineNum = machineNum,
            coords = Coords
        };
    }
}

public struct CasinoMachineState : INetSerializable
{
    public uint machineNum;
    public Vector2 coords;
    public void Serialize(NetDataWriter writer) {
        writer.Put(machineNum);
        writer.Put(coords);
    }
    public void Deserialize(NetDataReader reader) {
        machineNum = reader.GetUInt();
        coords = reader.GetVector2();
    }
}
}