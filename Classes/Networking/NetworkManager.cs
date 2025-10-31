using System;
using CasinoRoyale.Classes.GameObjects.Items;
using CasinoRoyale.Classes.GameObjects.Player;
using CasinoRoyale.Utils;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;

namespace CasinoRoyale.Classes.Networking;

public class NetworkManager
{
    private static readonly Lazy<NetworkManager> _instance = new(() => new NetworkManager());
    public static NetworkManager Instance => _instance.Value;

    private bool _initialized;
    public bool IsHost { get; private set; }
    public bool IsClient => !IsHost;

    private NetworkManager() { }

    public void Initialize(bool isHost)
    {
        if (_initialized)
            return;
        IsHost = isHost;
        _initialized = true;
        Logger.LogNetwork("NETWORK_MANAGER", $"Initialized. Role={(IsHost ? "Host" : "Client")}");
    }

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

    public event Action<uint, string, INetSerializable> ClientObjectChangeRequested;
    public event Action<uint, string, INetSerializable> HostBroadcastObjectChangeRequested;

    public event Action<string, Vector2, INetSerializable> ClientSpawnRequest;
    public event Action<uint, string, Vector2, INetSerializable> HostSpawnBroadcast;

    public event Action<INetSerializable, uint> HostSendToClient;
    public event Action<INetSerializable> HostBroadcastPacket;
    public event Action<INetSerializable> ClientSendToHost;

    public void HostSendJoinAcceptance(
        uint targetClientId,
        Rectangle gameArea,
        PlayableCharacter joiningPlayer,
        PlayerState[] otherPlayers,
        ItemState[] items,
        GridTileState[] gridTiles
    )
    {
        if (!IsHost)
            return;
        var joinAccept = new JoinAcceptPacket
        {
            targetClientId = targetClientId,
            gameArea = Rectangle.Empty,
            playerHitbox = joiningPlayer.Hitbox,
            playerState = joiningPlayer.GetPlayerState(),
            playerVelocity = joiningPlayer.Velocity,
            otherPlayerStates = otherPlayers ?? Array.Empty<PlayerState>(),
            itemStates = items ?? Array.Empty<ItemState>(),
            gridTiles = gridTiles ?? Array.Empty<GridTileState>(),
        };
        HostSendToClient?.Invoke(joinAccept, targetClientId);

        var gameWorldInit = new GameWorldInitPacket
        {
            targetClientId = targetClientId,
            gameArea = gameArea,
            itemStates = items ?? Array.Empty<ItemState>(),
            gridTiles = gridTiles ?? Array.Empty<GridTileState>(),
        };
        HostSendToClient?.Invoke(gameWorldInit, targetClientId);
    }

    public void HostBroadcastPlayerJoined(
        string username,
        PlayerState playerState,
        Rectangle hitbox
    )
    {
        if (!IsHost)
            return;
        var pkt = new PlayerJoinedGamePacket
        {
            new_player_username = username,
            new_player_state = playerState,
            new_player_hitbox = hitbox,
        };
        HostBroadcastPacket?.Invoke(pkt);
    }

    public void RequestSpawnFromClient(
        string prefabKey,
        Vector2 position,
        INetSerializable initialState
    )
    {
        if (!IsClient)
            return;
        ClientSpawnRequest?.Invoke(prefabKey, position, initialState);
    }

    public void HostApproveAndBroadcastSpawn(
        uint objectId,
        string prefabKey,
        Vector2 position,
        INetSerializable initialState
    )
    {
        if (!IsHost)
            return;
        HostSpawnBroadcast?.Invoke(objectId, prefabKey, position, initialState);
    }

    public void SendPacketToHost(INetSerializable packet)
    {
        if (IsHost)
            return; // Host doesn't send to itself
        ClientSendToHost?.Invoke(packet);
    }

    public void BroadcastPacketToAll(INetSerializable packet)
    {
        if (!IsHost)
            return;
        HostBroadcastPacket?.Invoke(packet);
    }
}
