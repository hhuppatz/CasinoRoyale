using LiteNetLib;

// Wrapper for Player class with NetPeer information
public class NetworkPlayer
{
    private NetPeer _peer;
    private Player _player;
    public Player Player { get => _player; set => _player = value; }
    public NetPeer Peer { get => _peer; set => _peer = value; }

    public NetworkPlayer(NetPeer m_peer, Player m_player)
    {
        Peer = m_peer;
        Player = m_player;
    }
}