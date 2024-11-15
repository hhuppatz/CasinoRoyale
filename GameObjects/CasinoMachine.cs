using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class CasinoMachine
{
    private readonly Texture2D tex;
    private CasinoMachineState machineState;
    public CasinoMachine(uint machineNum, Texture2D tex, Vector2 coords)
    {
        this.tex = tex;
        machineState.coords = coords;
        machineState.machineNum = machineNum;
    }

    public void SetState(CasinoMachineState m_MachineState)
    {
        machineState = m_MachineState;
    }

    public Texture2D GetTex()
    {
        return tex;
    }

    public Vector2 GetCoords()
    {
        return machineState.coords;
    }

    public CasinoMachineState GetState()
    {
        return machineState;
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