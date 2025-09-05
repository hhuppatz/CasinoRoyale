// Packet classes
using Microsoft.Xna.Framework;

public class JoinPacket
{
    public string username { get; set; }
}

// Sent to new player on accceptance
public class JoinAcceptPacket {
    public Rectangle gameArea { get; set; }
    public Rectangle playerHitbox { get; set; }
    public PlayerState playerState { get; set; }
    public Vector2 playerVelocity { get; set; }
    public PlayerState[] otherPlayerStates { get; set; }
    public PlatformState[] platformStates { get; set; }
    public CasinoMachineState[] casinoMachineStates { get; set; }
}

public class PlayerSendUpdatePacket {
    public Vector2 coords { get; set; }
    public Vector2 velocity { get; set; }
    public float dt { get; set; }
}

public class PlayerReceiveUpdatePacket {
    public PlayerState[] playerStates { get; set; }
    public CasinoMachineState[] casinoMachineStates { get; set; }
}

public class PlayerJoinedGamePacket {
    public string new_player_username{ get; set; }
    public PlayerState new_player_state { get; set; }
    public Rectangle new_player_hitbox { get; set; }
}

public class PlayerLeftGamePacket {
    public uint pid { get; set; }
}