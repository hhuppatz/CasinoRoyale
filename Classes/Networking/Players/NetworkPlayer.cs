using CasinoRoyale.Classes.GameObjects.Player;
using LiteNetLib;

namespace CasinoRoyale.Classes.Networking.Players;

// Wrapper for PlayableCharacter class with NetPeer information
public class NetworkPlayer
{
    private NetPeer _peer;
    private PlayableCharacter _player;
    public PlayableCharacter Player
    {
        get => _player;
        set => _player = value;
    }
    public NetPeer Peer
    {
        get => _peer;
        set => _peer = value;
    }

    public NetworkPlayer(NetPeer m_peer, PlayableCharacter m_player)
    {
        Peer = m_peer;
        Player = m_player;
    }
}
