using System;
using CasinoRoyale.Utils;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using CasinoRoyale.Classes.GameObjects.Platforms;
using CasinoRoyale.Classes.GameObjects.Items;
using CasinoRoyale.Classes.GameObjects.Player;

namespace CasinoRoyale.Classes.Networking;

// Minimal singleton NetworkManager that tracks role and routes object change notifications
public class NetworkManager
{
    private static readonly Lazy<NetworkManager> _instance = new(() => new NetworkManager());
    public static NetworkManager Instance => _instance.Value;

    private bool _initialized;
    public bool IsHost { get; private set; }
    public bool IsClient => !IsHost;

    private NetworkManager() {}

    public void Initialize(bool isHost)
    {
        if (_initialized) return;
        IsHost = isHost;
        _initialized = true;
        Logger.LogNetwork("NETWORK_MANAGER", $"Initialized. Role={(IsHost ? "Host" : "Client")}");
    }

    // Called by NetworkComponent when a network object's state changes
    public void NotifyObjectChanged(uint objectId, string propertyName, INetSerializable newValue)
    {
        Logger.LogNetwork("NETWORK_MANAGER", $"Change on object {objectId}: {propertyName}");
        if (IsClient)
        {
            ClientObjectChangeRequested?.Invoke(objectId, propertyName, newValue);
                    return;
                }
        HostBroadcastObjectChangeRequested?.Invoke(objectId, propertyName, newValue);
    }

    // =====================================================================
    // Event-based transport hooks (upper layer/transport subscribes to these)
    // =====================================================================
    // Client -> Host: an object's state changed on client, send to host
    public event Action<uint, string, INetSerializable> ClientObjectChangeRequested;
    // Host -> Clients: broadcast an object's state change to all other players
    public event Action<uint, string, INetSerializable> HostBroadcastObjectChangeRequested;

    // Client -> Host: request to spawn a new object (host will allocate id)
    public event Action<string, Vector2, INetSerializable> ClientSpawnRequest;
    // Host -> All: notify that an object was spawned with id
    public event Action<uint, string, Vector2, INetSerializable> HostSpawnBroadcast;

    // Host -> Specific Client: send initial game creation packets to the newly joined client
    public event Action<INetSerializable, uint> HostSendToClient; // (packet, targetClientId)
    // Host -> All Clients: broadcast a packet to everyone
    public event Action<INetSerializable> HostBroadcastPacket;

    // =====================================================================
    // High-level helpers for join flow and world init (host-side)
    // =====================================================================
    public void HostSendJoinAcceptance(
        uint targetClientId,
        Rectangle gameArea,
        PlayableCharacter joiningPlayer,
        PlayerState[] otherPlayers,
        ItemState[] items,
        GridTileState[] gridTiles)
    {
        if (!IsHost) return;
        var joinAccept = new JoinAcceptPacket
        {
            targetClientId = targetClientId,
            gameArea = Rectangle.Empty,
            playerHitbox = joiningPlayer.Hitbox,
            playerState = joiningPlayer.GetPlayerState(),
            playerVelocity = joiningPlayer.Velocity,
            otherPlayerStates = otherPlayers ?? Array.Empty<PlayerState>(),
            itemStates = items ?? Array.Empty<ItemState>(),
            gridTiles = gridTiles ?? Array.Empty<GridTileState>()
        };
        HostSendToClient?.Invoke(joinAccept, targetClientId);

        var gameWorldInit = new GameWorldInitPacket
        {
            targetClientId = targetClientId,
            gameArea = gameArea,
            itemStates = items ?? Array.Empty<ItemState>(),
            gridTiles = gridTiles ?? Array.Empty<GridTileState>()
        };
        HostSendToClient?.Invoke(gameWorldInit, targetClientId);
    }

    // Platform init broadcast removed with grid migration

    public void HostBroadcastPlayerJoined(string username, PlayerState playerState, Rectangle hitbox)
    {
        if (!IsHost) return;
        var pkt = new PlayerJoinedGamePacket
        {
            new_player_username = username,
            new_player_state = playerState,
            new_player_hitbox = hitbox
        };
        HostBroadcastPacket?.Invoke(pkt);
    }

    // =====================================================================
    // Spawn helpers
    // =====================================================================
    public void RequestSpawnFromClient(string prefabKey, Vector2 position, INetSerializable initialState)
    {
        if (!IsClient) return;
        ClientSpawnRequest?.Invoke(prefabKey, position, initialState);
    }

    public void HostApproveAndBroadcastSpawn(uint objectId, string prefabKey, Vector2 position, INetSerializable initialState)
    {
        if (!IsHost) return;
        HostSpawnBroadcast?.Invoke(objectId, prefabKey, position, initialState);
    }
}


