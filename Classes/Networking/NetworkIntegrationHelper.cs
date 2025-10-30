using System.Collections.Generic;
using LiteNetLib;
using CasinoRoyale.Classes.GameObjects.Player;
using CasinoRoyale.Classes.GameSystems;
using CasinoRoyale.Utils;

namespace CasinoRoyale.Classes.Networking;

/// <summary>
/// Helper class to simplify integration of network-aware systems
/// Provides a single point of initialization for all network handlers
/// </summary>
public static class NetworkIntegrationHelper
{
    /// <summary>
    /// Initialize all network handlers for the game
    /// Call this once after establishing network connection
    /// </summary>
    /// <param name="relayPeer">The peer connected to the relay server</param>
    /// <param name="packetProcessor">The packet processor for serialization</param>
    /// <param name="players">Dictionary of all players in the game</param>
    /// <param name="gameWorld">The game world instance</param>
    /// <param name="isHost">Whether this instance is the host</param>
    public static void InitializeNetworking(
        NetPeer relayPeer,
        AsyncPacketProcessor packetProcessor,
        Dictionary<uint, PlayableCharacter> players,
        GameWorld gameWorld,
        bool isHost)
    {
        Logger.LogNetwork("NETWORK_HELPER", $"Initializing networking as {(isHost ? "Host" : "Client")}");
        
        // Initialize inventory networking
        InventoryNetworkHandler.Initialize(
            relayPeer,
            packetProcessor,
            players,
            gameWorld,
            isHost
        );
        
        // Future network handlers can be initialized here
        // e.g., CombatNetworkHandler.Initialize(...);
        // e.g., ChatNetworkHandler.Initialize(...);
        
        Logger.LogNetwork("NETWORK_HELPER", "All network handlers initialized");
    }
    
    /// <summary>
    /// Check if networking is fully initialized
    /// </summary>
    public static bool IsNetworkingReady()
    {
        return InventoryNetworkHandler.IsInitialized;
        // && CombatNetworkHandler.IsInitialized
        // && ChatNetworkHandler.IsInitialized
    }
    
    /// <summary>
    /// Get initialization status for debugging
    /// </summary>
    public static string GetNetworkStatus()
    {
        return $"Inventory: {(InventoryNetworkHandler.IsInitialized ? "Ready" : "Not Ready")}";
        // Add other handlers as they're implemented
    }
}
