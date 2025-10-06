using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.GameObjects.CasinoMachines
{
    public class CasinoMachineFactory(Texture2D machineTex)
    {
        private readonly Texture2D machineTex = machineTex;

        public Texture2D GetTexture() => machineTex;

        public List<CasinoMachine> SpawnCasinoMachines()
        {
            uint id = 0;
            List<CasinoMachine> machines = [new CasinoMachine(id, machineTex, new Vector2(200,0))];
            id++;
            return machines;
        }
    }
}