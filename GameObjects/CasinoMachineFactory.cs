using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

public class CasinoMachineFactory
{
    private readonly Texture2D machineTex;
    private List<CasinoMachine> machines;
    public CasinoMachineFactory(Texture2D machineTex)
    {
        this.machineTex = machineTex;
        machines = new List<CasinoMachine>();
    }

    public void SpawnCasinoMachine()
    {
        machines.Add(new CasinoMachine(machineTex, new Vector2(200,0)));
    }

    public List<CasinoMachine> GetCasinoMachines()
    {
        return machines;
    }
}