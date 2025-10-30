using System;
using CasinoRoyale.Classes.Networking.Players;
using LiteNetLib;
using LiteNetLib.Utils;

namespace CasinoRoyale.Classes.Networking;

// Base class for all network events
public class NetworkEventArgs : EventArgs
{
    public DateTime Timestamp { get; } = DateTime.Now;
}

// Event arguments for when a player joins the game
public class PlayerJoinedEventArgs(NetworkPlayer player) : NetworkEventArgs
{
    public NetworkPlayer Player { get; } = player;
}

// Event arguments for when a player leaves the game
public class PlayerLeftEventArgs(uint playerId) : NetworkEventArgs
{
    public uint PlayerId { get; } = playerId;
}

// Event arguments for when a packet is received
public class PacketReceivedEventArgs<T>(T packet, NetPeer peer) : NetworkEventArgs
    where T : INetSerializable
{
    public T Packet { get; } = packet;
    public NetPeer Peer { get; } = peer;
}

// Event arguments for when a lobby code is received
public class LobbyCodeReceivedEventArgs(string lobbyCode) : NetworkEventArgs
{
    public string LobbyCode { get; } = lobbyCode;
}

// Event arguments for when a connection is established
public class ConnectionEstablishedEventArgs(bool isHost, string lobbyCode = null) : NetworkEventArgs
{
    public bool IsHost { get; } = isHost;
    public string LobbyCode { get; } = lobbyCode;
}
