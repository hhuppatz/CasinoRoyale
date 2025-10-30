namespace CasinoRoyale.Classes.GameObjects.Interfaces;

public interface IJump
{
    public bool InJumpSquat { get; set; }
    public bool InJump { set; get; }
    public float InitialJumpVelocity { get; set; }
}
