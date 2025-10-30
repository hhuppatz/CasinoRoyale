using Microsoft.Xna.Framework;

namespace CasinoRoyale.Classes.GameObjects.Interfaces;

public interface IObject
{
    public Vector2 Coords { get; set; }
    public Vector2 Velocity { get; set; }
    public bool Destroyed { get; set; }
    public bool HasChanged { get; set; }
}
