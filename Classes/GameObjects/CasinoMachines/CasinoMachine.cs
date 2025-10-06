using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CasinoRoyale.Classes.GameObjects.Interfaces;
using CasinoRoyale.Classes.Networking;
using System;

namespace CasinoRoyale.Classes.GameObjects.CasinoMachines
{
    public class CasinoMachine(uint machineNum, Texture2D tex, Vector2 coords) : IObject, IHitbox
{
    private readonly Texture2D tex = tex;
    private readonly uint machineNum = machineNum;
    private Vector2 _coords = coords;
    public Vector2 Coords { get => _coords; set => _coords = value; }
    private Rectangle _hitbox = new(coords.ToPoint(), new Point(tex.Bounds.Width, tex.Bounds.Height));
    public Rectangle Hitbox { get => _hitbox; set => _hitbox = value; }
    private bool _spawnedCoin = false;
    public bool SpawnedCoin { get => _spawnedCoin; set => _spawnedCoin = value; }

        public Texture2D GetTex()
    {
        return tex;
    }

    public CasinoMachineState GetState()
    {
        return new CasinoMachineState{
            machineNum = machineNum,
            coords = Coords,
            spawnedCoin = SpawnedCoin
        };
    }

    public Coin SpawnCoin(uint coinId, Texture2D coinTex)
    {
        Random random = new();
        bool isLeft = random.Next(0, 2) == 0;
        Vector2 spawnPos = Coords + new Vector2(tex.Bounds.Width/2, 0);
        Vector2 spawnVel = new((float)random.NextDouble() * 20 + 30, -80);

        if (isLeft) spawnVel.X = - spawnVel.X;
        return new Coin(coinId, coinTex, spawnPos, spawnVel);
    }
}

public struct CasinoMachineState : INetSerializable
{
    public uint machineNum;
    public Vector2 coords;
    public bool spawnedCoin;
    public void Serialize(NetDataWriter writer) {
        writer.Put(machineNum);
        writer.Put(coords);
        writer.Put(spawnedCoin);
    }
    public void Deserialize(NetDataReader reader) {
        machineNum = reader.GetUInt();
        coords = reader.GetVector2();
        spawnedCoin = reader.GetBool();
    }
}
}