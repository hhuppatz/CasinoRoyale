using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using CasinoRoyale.Classes.Networking;
using CasinoRoyale.Classes.Networking.SerializingExtensions;
using CasinoRoyale.Classes.GameSystems;
using CasinoRoyale.Classes.GameObjects.Interfaces;
using CasinoRoyale.Classes.GameObjects.Items;
using CasinoRoyale.Classes.GameObjects.Items.Interfaces;
using CasinoRoyale.Utils;

namespace CasinoRoyale.Classes.GameObjects.Player;

public class PlayableCharacter : GameEntity,
    CasinoRoyale.Classes.GameObjects.Interfaces.IDrawable,
    IJump
{
    // PlayableCharacter
    private readonly uint pid;
    private readonly string username;
    private Texture2D tex;
    public Texture2D Texture { get => tex; set => tex = value;}

    private float standardSpeed;
    public float StandardSpeed { get => standardSpeed; set => standardSpeed = value; }

    // Movement interpolation fields for inbetween network updates
    private Vector2 targetCoords;
    private Vector2 targetVelocity;
    private readonly float interpolationSpeed = 8.0f; // How fast to interpolate to target position
    
    // State buffer for delayed interpolation for responsiveness
    private struct BufferedState
    {
        public Vector2 coords;
        public Vector2 velocity;
        public float timestamp;
    }
    
    private readonly Queue<BufferedState> stateBuffer = new(); // Queue for the state buffer
    private float currentTime = 0f;
    
    // Inventory system
    private readonly Inventory inventory;
    public Inventory GetInventory() => inventory;
    
    // Item pickup range
    private const float PICKUP_RANGE = 50f;

    // IJump
    private bool inJump = false;
    public bool InJump { get => inJump; set => inJump = value; }
    public bool InJumpSquat { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    private float initialJumpVelocity;
    public float InitialJumpVelocity { get => initialJumpVelocity; set => initialJumpVelocity = value; }
    
    // Constructor with inventory initialization
    public PlayableCharacter(uint pid, string username, Texture2D tex, Vector2 coords, Vector2 velocity, float mass, float initialJumpVelocity, float standardSpeed, Rectangle hitbox, bool awake)
        : base(coords, velocity, hitbox, awake, mass)
    {
        this.pid = pid;
        this.username = username;
        this.tex = tex;
        this.standardSpeed = standardSpeed;
        this.initialJumpVelocity = initialJumpVelocity;
        this.inventory = new Inventory(pid);
    }

    // Method to mark new player as changed (call after construction)
    public void MarkAsNewPlayer()
    {
        MarkAsChanged();
    }

    // Include previous keyboard state to check for key releases
    public void TryMovePlayer(KeyboardState ks, KeyboardState previousKs, float dt, GameWorld gameWorld)
    {
        bool m_playerAttemptedJump = false;
        
        // Reset horizontal velocity each frame (for responsive controls)
        Velocity = new Vector2(0, Velocity.Y);
        
        if (ks.IsKeyDown(Keys.A)) // Left, Can be held
        {
            Velocity = new Vector2(-StandardSpeed, Velocity.Y);
        }
        if (ks.IsKeyDown(Keys.D)) // Right, Can be held
        {
            Velocity = new Vector2(StandardSpeed, Velocity.Y);
        }
        if ((ks.GetPressedKeys().Contains(Keys.W) && !previousKs.GetPressedKeys().Contains(Keys.W)) ||
            (ks.GetPressedKeys().Contains(Keys.Space) && !previousKs.GetPressedKeys().Contains(Keys.Space)))
        {
            m_playerAttemptedJump = true;
        }
        if (ks.IsKeyDown(Keys.LeftShift)) // Sprint, Can be held
        {
            Velocity = new Vector2(Velocity.X * 1.5f, Velocity.Y);
        }
        
        // Inventory system key inputs
        // E key - Pick up items
        if (ks.IsKeyDown(Keys.E) && !previousKs.IsKeyDown(Keys.E))
        {
            TryPickupNearbyItems(gameWorld);
        }
        
        // Q key - Drop items
        if (ks.IsKeyDown(Keys.Q) && !previousKs.IsKeyDown(Keys.Q))
        {
            TryDropItem(gameWorld);
        }
        
        // I key - Use items
        if (ks.IsKeyDown(Keys.I) && !previousKs.IsKeyDown(Keys.I))
        {
            TryUseItem();
        }

        // Update velocity according to forces and movement requests
        UpdateJump(m_playerAttemptedJump, gameWorld);

        // Enforce movement rules using grid tiles (new system)
        PhysicsSystem.EnforceMovementRules(gameWorld.GameArea, gameWorld.GetPlatformTileHitboxes(), this, dt);
        
        // Auto-collect coins on contact
        TryAutoCollectItems(gameWorld);
    }

    public void UpdateJump(bool m_playerAttemptedJump, GameWorld gameWorld)
    {
        // Deal with player jumping - only apply jump velocity once when jump starts
        if (m_playerAttemptedJump
            && !InJump
            && PhysicsSystem.IsPlayerGrounded(gameWorld.GameArea, gameWorld.GetPlatformTileHitboxes(), this))
        {
            // Start jump with initial velocity
            InJump = true;
            Velocity = new Vector2(0, -InitialJumpVelocity);
        }
        // Check if player has landed after falling (end jump state)
        else if (InJump
                && Velocity.Y >= 0
                && PhysicsSystem.IsPlayerGrounded(gameWorld.GameArea, gameWorld.GetPlatformTileHitboxes(), this))
        {
            InJump = false;
        }

    }
    
    // Casino machine interaction removed

    public void SetPlayerState(PlayerState playerState)
    {
        Coords = playerState.ges.coords;
        Velocity = playerState.ges.velocity;
        Mass = playerState.ges.mass;
        if (playerState.ges.awake)
            AwakenEntity();
        else
            SleepEntity();
    }

    // Set target position for interpolation (used for network updates)
    public void SetTargetPosition(Vector2 newCoords, Vector2 newVelocity)
    {
        targetCoords = newCoords;
        targetVelocity = newVelocity;
    }
    
    // Add state to buffer for delayed application
    public void AddBufferedState(Vector2 newCoords, Vector2 newVelocity, float timestamp)
    {
        var bufferedState = new BufferedState
        {
            coords = newCoords,
            velocity = newVelocity,
            timestamp = timestamp
        };
        
        stateBuffer.Enqueue(bufferedState);
        
        // Keep buffer size reasonable (max 5 states)
        while (stateBuffer.Count > 5)
        {
            stateBuffer.Dequeue();
        }
        
        // Don't apply immediately - let the interpolation system handle smooth movement
        // SetTargetPosition(newCoords, newVelocity);
    }
    
    // Process buffered states and apply delayed ones
    public void ProcessBufferedStates(float dt)
    {
        currentTime += dt;
        
        // Clean up old states from buffer and apply the most recent valid state
        BufferedState? mostRecentState = null;
        
        while (stateBuffer.Count > 0)
        {
            var oldestState = stateBuffer.Peek();
            
            // Remove states older than 200ms
            if (currentTime - oldestState.timestamp > 0.2f)
            {
                stateBuffer.Dequeue();
            }
            else
            {
                // Keep the most recent state
                mostRecentState = oldestState;
                break;
            }
        }
        
        // Apply the most recent valid state if we have one
        if (mostRecentState.HasValue)
        {
            SetTargetPosition(mostRecentState.Value.coords, mostRecentState.Value.velocity);
        }
    }
    
    // Initialize target coordinates (call this after player creation)
    public void InitializeTargets()
    {
        targetCoords = Coords;
        targetVelocity = Velocity;
        currentTime = 0f;
    }

    // Update position using interpolation towards target
    public void UpdateInterpolation(float dt)
    {
        // Only interpolate if we have a target set
        if (targetCoords != Vector2.Zero || Vector2.Distance(Coords, targetCoords) > 0.1f)
        {
            // Smooth interpolation towards target position
            Coords = Vector2.Lerp(Coords, targetCoords, interpolationSpeed * dt);
            
            // Smooth interpolation towards target velocity
            Velocity = Vector2.Lerp(Velocity, targetVelocity, interpolationSpeed * dt);
        }
    }

    public string GetUsername()
    {
        return username;
    }

    public PlayerState GetPlayerState()
    {
        return new PlayerState {
            objectType = ObjectType.PLAYABLECHARACTER,
            pid = pid,
            username = username,
            ges = GetEntityState(),
            initialJumpVelocity = initialJumpVelocity,
            maxRunSpeed = standardSpeed
        };
    }

    public uint GetID()
    {
        return pid;
    }
    
    // ==================== INVENTORY METHODS ====================
    
    /// <summary>
    /// Try to pick up nearby items that require manual pickup (E key)
    /// Network-compatible: Clients send requests, Host processes directly
    /// Falls back to local processing if networking isn't initialized (single-player)
    /// </summary>
    private void TryPickupNearbyItems(GameWorld gameWorld)
    {
        var nearbyItems = GetNearbyItems(gameWorld);
        
        foreach (var item in nearbyItems)
        {
            if (item is IPickupable pickupable && pickupable.RequiresManualPickup)
            {
                if (!inventory.IsFull())
                {
                    // Check if we're in networked mode with initialized handler
                    if (InventoryNetworkHandler.IsInitialized)
                    {
                        if (NetworkManager.Instance.IsHost)
                        {
                            // Host: Process pickup immediately and will broadcast to clients
                            ProcessItemPickup(item, gameWorld);
                        }
                        else
                        {
                            // Client: Send pickup request to host
                            SendPickupRequest(item.ItemId);
                        }
                    }
                    else
                    {
                        // Single-player or networking not initialized: process locally
                        ProcessItemPickup(item, gameWorld);
                    }
                }
                else
                {
                    Logger.Info($"Player {username}'s inventory is full!");
                }
                break; // Only pick up one item per keypress
            }
        }
    }
    
    /// <summary>
    /// Send pickup request to host (client-side)
    /// </summary>
    private void SendPickupRequest(uint itemId)
    {
        InventoryNetworkHandler.Instance?.SendPickupRequest(pid, itemId);
    }
    
    /// <summary>
    /// Process item pickup (host authoritative)
    /// </summary>
    public bool ProcessItemPickup(Item item, GameWorld gameWorld)
    {
        if (item is IPickupable pickupable)
        {
            if (inventory.TryAddItem(item.ItemType))
            {
                Logger.Info($"Player {username} picked up {item.ItemType}!");
                pickupable.OnPickup(this);
                
                // Host broadcasts pickup to all clients (if networking is active)
                if (InventoryNetworkHandler.IsInitialized && NetworkManager.Instance.IsHost)
                {
                    InventoryNetworkHandler.Instance?.BroadcastPickup(pid, item.ItemId, item.ItemType, true);
                }
                
                return true;
            }
        }
        
        // Broadcast failure if inventory was full (if networking is active)
        if (InventoryNetworkHandler.IsInitialized && NetworkManager.Instance.IsHost)
        {
            InventoryNetworkHandler.Instance?.BroadcastPickup(pid, item.ItemId, item.ItemType, false);
        }
        
        return false;
    }
    
    /// <summary>
    /// Auto-collect items that don't require manual pickup (like coins)
    /// Network-compatible: Clients send requests, Host processes directly
    /// Falls back to local processing if networking isn't initialized (single-player)
    /// </summary>
    private void TryAutoCollectItems(GameWorld gameWorld)
    {
        var nearbyItems = GetNearbyItems(gameWorld);
        
        foreach (var item in nearbyItems)
        {
            if (item is IPickupable pickupable && !pickupable.RequiresManualPickup)
            {
                if (!inventory.IsFull())
                {
                    // Check if we're in networked mode with initialized handler
                    if (InventoryNetworkHandler.IsInitialized)
                    {
                        if (NetworkManager.Instance.IsHost)
                        {
                            // Host: Process pickup immediately and will broadcast to clients
                            ProcessItemPickup(item, gameWorld);
                        }
                        else
                        {
                            // Client: Send pickup request to host
                            SendPickupRequest(item.ItemId);
                        }
                    }
                    else
                    {
                        // Single-player or networking not initialized: process locally
                        ProcessItemPickup(item, gameWorld);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Drop the first non-empty item from inventory
    /// Network-compatible: Clients send requests, Host processes directly
    /// Falls back to local processing if networking isn't initialized (single-player)
    /// </summary>
    private void TryDropItem(GameWorld gameWorld)
    {
        var occupiedSlots = inventory.GetOccupiedSlots();
        if (occupiedSlots.Length == 0)
        {
            Logger.Info($"Player {username} has no items to drop!");
            return;
        }
        
        // Drop from the first occupied slot
        var slot = occupiedSlots[0];
        var itemType = slot.GetItemType();
        
        if (itemType.HasValue)
        {
            // Calculate drop position and velocity
            Vector2 dropPosition = Coords + new Vector2(Hitbox.Width / 2, 0);
            Vector2 dropVelocity = new Vector2(Velocity.X * 0.5f, -100f); // Toss forward and up
            
            // Check if we're in networked mode with initialized handler
            if (InventoryNetworkHandler.IsInitialized)
            {
                if (NetworkManager.Instance.IsHost)
                {
                    // Host: Process drop immediately and will broadcast to clients
                    ProcessItemDrop(itemType.Value, dropPosition, dropVelocity, gameWorld);
                }
                else
                {
                    // Client: Send drop request to host
                    SendDropRequest(itemType.Value, dropPosition, dropVelocity);
                }
            }
            else
            {
                // Single-player or networking not initialized: process locally
                ProcessItemDrop(itemType.Value, dropPosition, dropVelocity, gameWorld);
            }
        }
    }
    
    /// <summary>
    /// Send drop request to host (client-side)
    /// </summary>
    private void SendDropRequest(ItemType itemType, Vector2 dropPosition, Vector2 dropVelocity)
    {
        InventoryNetworkHandler.Instance?.SendDropRequest(pid, itemType, dropPosition, dropVelocity);
    }
    
    /// <summary>
    /// Process item drop (host authoritative)
    /// </summary>
    public bool ProcessItemDrop(ItemType itemType, Vector2 dropPosition, Vector2 dropVelocity, GameWorld gameWorld)
    {
        // Remove one item from inventory
        if (inventory.TryRemoveItem(itemType))
        {
            // Spawn the item in the world
            gameWorld.SpawnItem(itemType, dropPosition, dropVelocity);
            Logger.Info($"Player {username} dropped a {itemType}!");
            
            // Host broadcasts drop to all clients (if networking is active)
            if (InventoryNetworkHandler.IsInitialized && NetworkManager.Instance.IsHost && gameWorld.AllItems.Count > 0)
            {
                var newItem = gameWorld.AllItems[gameWorld.AllItems.Count - 1]; // Last spawned item
                InventoryNetworkHandler.Instance?.BroadcastDrop(pid, itemType, newItem);
            }
            
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Use the first item in inventory (I key)
    /// Network-compatible: Clients send requests, Host processes directly
    /// Falls back to local processing if networking isn't initialized (single-player)
    /// </summary>
    private void TryUseItem()
    {
        var occupiedSlots = inventory.GetOccupiedSlots();
        if (occupiedSlots.Length == 0)
        {
            Logger.Info($"Player {username} has no items to use!");
            return;
        }
        
        // Use from the first occupied slot
        var slot = occupiedSlots[0];
        var itemType = slot.GetItemType();
        
        if (itemType.HasValue)
        {
            // Check if we're in networked mode with initialized handler
            if (InventoryNetworkHandler.IsInitialized)
            {
                if (NetworkManager.Instance.IsHost)
                {
                    // Host: Process use immediately and will broadcast to clients
                    ProcessItemUse(itemType.Value);
                }
                else
                {
                    // Client: Send use request to host
                    SendUseRequest(itemType.Value);
                }
            }
            else
            {
                // Single-player or networking not initialized: process locally
                ProcessItemUse(itemType.Value);
            }
        }
    }
    
    /// <summary>
    /// Send use request to host (client-side)
    /// </summary>
    private void SendUseRequest(ItemType itemType)
    {
        InventoryNetworkHandler.Instance?.SendUseRequest(pid, itemType);
    }
    
    /// <summary>
    /// Process item use (host authoritative)
    /// </summary>
    public void ProcessItemUse(ItemType itemType)
    {
        if (inventory.HasItem(itemType))
        {
            // Get the use strategy for this item type
            var strategy = Items.Strategies.ItemStrategyFactory.GetStrategy(itemType);
            strategy.Execute(this, itemType);
            
            // Host broadcasts use to all clients (if networking is active)
            if (InventoryNetworkHandler.IsInitialized && NetworkManager.Instance.IsHost)
            {
                InventoryNetworkHandler.Instance?.BroadcastUse(pid, itemType);
            }
        }
    }
    
    /// <summary>
    /// Get all items within pickup range of the player
    /// </summary>
    private List<Item> GetNearbyItems(GameWorld gameWorld)
    {
        var nearbyItems = new List<Item>();
        var playerCenter = new Vector2(Coords.X + Hitbox.Width / 2, Coords.Y + Hitbox.Height / 2);
        
        foreach (var item in gameWorld.AllItems)
        {
            var itemCenter = new Vector2(item.Coords.X + item.Hitbox.Width / 2, item.Coords.Y + item.Hitbox.Height / 2);
            float distance = Vector2.Distance(playerCenter, itemCenter);
            
            if (distance <= PICKUP_RANGE)
            {
                nearbyItems.Add(item);
            }
        }
        
        return nearbyItems;
    }

}

public struct PlayerState
{
    public ObjectType objectType;
    public uint pid;
    public string username;
    public GameEntityState ges;
    public float initialJumpVelocity;
    public float maxRunSpeed;
}