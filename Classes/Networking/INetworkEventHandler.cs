using System;
using CasinoRoyale.Classes.GameObjects.Player;
using CasinoRoyale.Classes.GameObjects.Platforms;
using CasinoRoyale.Classes.GameObjects.Items;

namespace CasinoRoyale.Classes.Networking;

/// <summary>
/// Shared interface for network event handlers to eliminate duplication
/// </summary>
public interface INetworkEventHandler
{
    // Connection events
    event EventHandler<LobbyCodeReceivedEventArgs> LobbyCodeReceived;
    event EventHandler<ConnectionEstablishedEventArgs> ConnectionEstablished;
    event EventHandler<string> ErrorOccurred;

    // Packet events
    event EventHandler<PacketReceivedEventArgs<PlayerSendUpdatePacket>> PlayerUpdateReceived;
    event EventHandler<PacketReceivedEventArgs<JoinPacket>> JoinRequestReceived;
    event EventHandler<PacketReceivedEventArgs<JoinAcceptPacket>> JoinAcceptReceived;
    event EventHandler<PacketReceivedEventArgs<PlayerReceiveUpdatePacket>> PlayerStatesUpdateReceived;
    // Platform update event removed with grid migration
    event EventHandler<PacketReceivedEventArgs<ItemUpdatePacket>> ItemSpawnedReceived;
    event EventHandler<PacketReceivedEventArgs<ItemRemovedPacket>> ItemRemovedReceived;
    event EventHandler<PacketReceivedEventArgs<PlayerJoinedGamePacket>> PlayerJoinedGameReceived;
    event EventHandler<PacketReceivedEventArgs<PlayerLeftGamePacket>> PlayerLeftGameReceived;
}
