using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class CasinoMachineFactory
{
    private readonly Texture2D machineTex;
    public CasinoMachineFactory(Texture2D machineTex)
    {
        this.machineTex = machineTex;
    }

    public List<CasinoMachine> SpawnCasinoMachines()
    {
        uint id = 0;
        List<CasinoMachine> machines = new List<CasinoMachine>();
        machines.Add(new CasinoMachine(id, machineTex, new Vector2(200,0)));
        id++;
        return machines;
    }

}