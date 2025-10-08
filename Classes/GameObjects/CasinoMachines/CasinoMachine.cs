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
    public Vector2 Coords { get => _coords; set { _coords = value; } } // Coords don't change after initialization
    private Rectangle _hitbox = new(coords.ToPoint(), new Point(tex.Bounds.Width, tex.Bounds.Height));
    public Rectangle Hitbox { get => _hitbox; set { _hitbox = value; } } // Hitbox doesn't change after initialization
    private bool _spawnedCoin = false;
    public bool SpawnedCoin { get => _spawnedCoin; set { _spawnedCoin = value; MarkAsChanged(); } } // Only SpawnedCoin triggers updates
    private bool _hasChanged = false;
    public bool HasChanged { get => _hasChanged; private set => _hasChanged = value; }

    public Texture2D GetTex()
    {
        return tex;
    }

    public CasinoMachineState GetState()
    {
        return new CasinoMachineState{
            objectType = ObjectType.CASINOMACHINE,
            machineNum = machineNum,
            coords = Coords,
            spawnedCoin = SpawnedCoin,
            requestId = 0 // Default request ID - will be set by client when making requests
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

    public void MarkAsChanged()
    {
        HasChanged = true;
    }

    public void ClearChangedFlag()
    {
        HasChanged = false;
    }

    public void SetState(CasinoMachineState state)
    {
        _coords = state.coords; // Set directly to avoid triggering change detection
        SpawnedCoin = state.spawnedCoin; // This will trigger change detection if needed
        // Note: requestId is not stored in the CasinoMachine object, only in the state
        ClearChangedFlag(); // Clear changed flag since we're setting the state
    }
}

public struct CasinoMachineState : INetSerializable
{
    public ObjectType objectType;
    public uint machineNum;
    public Vector2 coords;
    public bool spawnedCoin;
    public uint requestId; // Unique request identifier to prevent race conditions
    public void Serialize(NetDataWriter writer) {
        writer.Put((byte)objectType);
        writer.Put(machineNum);
        writer.Put(coords);
        writer.Put(spawnedCoin);
        writer.Put(requestId);
    }
    public void Deserialize(NetDataReader reader) {
        objectType = (ObjectType)reader.GetByte();
        machineNum = reader.GetUInt();
        coords = reader.GetVector2();
        spawnedCoin = reader.GetBool();
        requestId = reader.GetUInt();
    }
}
}