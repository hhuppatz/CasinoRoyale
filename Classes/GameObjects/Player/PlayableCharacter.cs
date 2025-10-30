using System;
using System.Collections.Generic;
using System.Linq;
using CasinoRoyale.Classes.GameObjects.Interfaces;
using CasinoRoyale.Classes.GameObjects.Items;
using CasinoRoyale.Classes.GameObjects.Items.Interfaces;
using CasinoRoyale.Classes.GameSystems;
using CasinoRoyale.Classes.Networking;
using CasinoRoyale.Classes.Networking.SerializingExtensions;
using CasinoRoyale.Utils;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CasinoRoyale.Classes.GameObjects.Player;

public class PlayableCharacter
    : GameEntity,
        CasinoRoyale.Classes.GameObjects.Interfaces.IDrawable,
        IJump
{
    private readonly uint pid;
    private readonly string username;
    private Texture2D tex;
    public Texture2D Texture
    {
        get => tex;
        set => tex = value;
    }

    private float standardSpeed;
    public float StandardSpeed
    {
        get => standardSpeed;
        set => standardSpeed = value;
    }

    private Vector2 targetCoords;
    private Vector2 targetVelocity;
    private readonly float interpolationSpeed = 8.0f;

    private struct BufferedState
    {
        public Vector2 coords;
        public Vector2 velocity;
        public float timestamp;
    }

    private readonly Queue<BufferedState> stateBuffer = new();
    private float currentTime = 0f;

    private readonly Inventory inventory;

    public Inventory GetInventory() => inventory;

    private const float PICKUP_RANGE = 50f;

    private bool inJump = false;
    public bool InJump
    {
        get => inJump;
        set => inJump = value;
    }
    public bool InJumpSquat
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }
    private float initialJumpVelocity;
    public float InitialJumpVelocity
    {
        get => initialJumpVelocity;
        set => initialJumpVelocity = value;
    }

    public PlayableCharacter(
        uint pid,
        string username,
        Texture2D tex,
        Vector2 coords,
        Vector2 velocity,
        float mass,
        float initialJumpVelocity,
        float standardSpeed,
        Rectangle hitbox,
        bool awake
    )
        : base(coords, velocity, hitbox, awake, mass)
    {
        this.pid = pid;
        this.username = username;
        this.tex = tex;
        this.standardSpeed = standardSpeed;
        this.initialJumpVelocity = initialJumpVelocity;
        this.inventory = new Inventory(pid);
    }

    public void MarkAsNewPlayer()
    {
        MarkAsChanged();
    }

    public void TryMovePlayer(
        KeyboardState ks,
        KeyboardState previousKs,
        float dt,
        GameWorld gameWorld
    )
    {
        bool m_playerAttemptedJump = false;

        Velocity = new Vector2(0, Velocity.Y);

        if (ks.IsKeyDown(Keys.A))
        {
            Velocity = new Vector2(-StandardSpeed, Velocity.Y);
        }
        if (ks.IsKeyDown(Keys.D))
        {
            Velocity = new Vector2(StandardSpeed, Velocity.Y);
        }
        if (
            (ks.GetPressedKeys().Contains(Keys.W) && !previousKs.GetPressedKeys().Contains(Keys.W))
            || (
                ks.GetPressedKeys().Contains(Keys.Space)
                && !previousKs.GetPressedKeys().Contains(Keys.Space)
            )
        )
        {
            m_playerAttemptedJump = true;
        }
        if (ks.IsKeyDown(Keys.LeftShift))
        {
            Velocity = new Vector2(Velocity.X * 1.5f, Velocity.Y);
        }

        if (ks.IsKeyDown(Keys.E) && !previousKs.IsKeyDown(Keys.E))
        {
            TryPickupNearbyItems(gameWorld);
        }

        if (ks.IsKeyDown(Keys.Q) && !previousKs.IsKeyDown(Keys.Q))
        {
            TryDropItem(gameWorld);
        }

        if (ks.IsKeyDown(Keys.I) && !previousKs.IsKeyDown(Keys.I))
        {
            TryUseItem();
        }

        UpdateJump(m_playerAttemptedJump, gameWorld);

        PhysicsSystem.EnforceMovementRules(
            gameWorld.GameArea,
            gameWorld.GetPlatformTileHitboxes(),
            this,
            dt
        );

        TryAutoCollectItems(gameWorld);
    }

    public void UpdateJump(bool m_playerAttemptedJump, GameWorld gameWorld)
    {
        if (
            m_playerAttemptedJump
            && !InJump
            && PhysicsSystem.IsPlayerGrounded(
                gameWorld.GameArea,
                gameWorld.GetPlatformTileHitboxes(),
                this
            )
        )
        {
            InJump = true;
            Velocity = new Vector2(0, -InitialJumpVelocity);
        }
        else if (
            InJump
            && Velocity.Y >= 0
            && PhysicsSystem.IsPlayerGrounded(
                gameWorld.GameArea,
                gameWorld.GetPlatformTileHitboxes(),
                this
            )
        )
        {
            InJump = false;
        }
    }

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

    public void SetTargetPosition(Vector2 newCoords, Vector2 newVelocity)
    {
        targetCoords = newCoords;
        targetVelocity = newVelocity;
    }

    public void AddBufferedState(Vector2 newCoords, Vector2 newVelocity, float timestamp)
    {
        var bufferedState = new BufferedState
        {
            coords = newCoords,
            velocity = newVelocity,
            timestamp = timestamp,
        };

        stateBuffer.Enqueue(bufferedState);

        while (stateBuffer.Count > 5)
        {
            stateBuffer.Dequeue();
        }
    }

    public void ProcessBufferedStates(float dt)
    {
        currentTime += dt;

        BufferedState? mostRecentState = null;

        while (stateBuffer.Count > 0)
        {
            var oldestState = stateBuffer.Peek();

            if (currentTime - oldestState.timestamp > 0.2f)
            {
                stateBuffer.Dequeue();
            }
            else
            {
                mostRecentState = oldestState;
                break;
            }
        }

        if (mostRecentState.HasValue)
        {
            SetTargetPosition(mostRecentState.Value.coords, mostRecentState.Value.velocity);
        }
    }

    public void InitializeTargets()
    {
        targetCoords = Coords;
        targetVelocity = Velocity;
        currentTime = 0f;
    }

    public void UpdateInterpolation(float dt)
    {
        if (targetCoords != Vector2.Zero || Vector2.Distance(Coords, targetCoords) > 0.1f)
        {
            Coords = Vector2.Lerp(Coords, targetCoords, interpolationSpeed * dt);

            Velocity = Vector2.Lerp(Velocity, targetVelocity, interpolationSpeed * dt);
        }
    }

    public string GetUsername()
    {
        return username;
    }

    public PlayerState GetPlayerState()
    {
        return new PlayerState
        {
            objectType = ObjectType.PLAYABLECHARACTER,
            pid = pid,
            username = username,
            ges = GetEntityState(),
            initialJumpVelocity = initialJumpVelocity,
            maxRunSpeed = standardSpeed,
        };
    }

    public uint GetID()
    {
        return pid;
    }

    private void TryPickupNearbyItems(GameWorld gameWorld)
    {
        var nearbyItems = GetNearbyItems(gameWorld);

        foreach (var item in nearbyItems)
        {
            if (item is IPickupable pickupable && pickupable.RequiresManualPickup)
            {
                if (!inventory.IsFull())
                {
                    if (inventory.TryAddItem(item.ItemType))
                    {
                        Logger.Info($"Player {username} picked up {item.ItemType}!");
                        pickupable.OnPickup(this);

                        gameWorld.RemoveItemById(item.ItemId);
                    }
                }
                else
                {
                    Logger.Info($"Player {username}'s inventory is full!");
                }
                break;
            }
        }
    }

    private void TryAutoCollectItems(GameWorld gameWorld)
    {
        var nearbyItems = GetNearbyItems(gameWorld);

        foreach (var item in nearbyItems)
        {
            if (item is IPickupable pickupable && !pickupable.RequiresManualPickup)
            {
                if (!inventory.IsFull())
                {
                    if (inventory.TryAddItem(item.ItemType))
                    {
                        Logger.Info($"Player {username} auto-collected {item.ItemType}!");
                        pickupable.OnPickup(this);

                        gameWorld.RemoveItemById(item.ItemId);
                    }
                }
            }
        }
    }

    private void TryDropItem(GameWorld gameWorld)
    {
        var occupiedSlots = inventory.GetOccupiedSlots();
        if (occupiedSlots.Length == 0)
        {
            Logger.Info($"Player {username} has no items to drop!");
            return;
        }

        var slot = occupiedSlots[0];
        var itemType = slot.GetItemType();

        if (itemType.HasValue)
        {
            if (inventory.TryRemoveItem(itemType.Value))
            {
                Vector2 dropPosition = Coords + new Vector2(Hitbox.Width / 2, 0);
                Vector2 dropVelocity = new Vector2(Velocity.X * 0.5f, -100f);

                gameWorld.SpawnItem(itemType.Value, dropPosition, dropVelocity);
                Logger.Info($"Player {username} dropped a {itemType.Value}!");
            }
        }
    }

    private void TryUseItem()
    {
        var occupiedSlots = inventory.GetOccupiedSlots();
        if (occupiedSlots.Length == 0)
        {
            Logger.Info($"Player {username} has no items to use!");
            return;
        }

        var slot = occupiedSlots[0];
        var itemType = slot.GetItemType();

        if (itemType.HasValue && inventory.HasItem(itemType.Value))
        {
            var strategy = Items.Strategies.ItemStrategyFactory.GetStrategy(itemType.Value);
            strategy.Execute(this, itemType.Value);
            Logger.Info($"Player {username} used {itemType.Value}!");
        }
    }

    private List<Item> GetNearbyItems(GameWorld gameWorld)
    {
        var nearbyItems = new List<Item>();
        var playerCenter = new Vector2(Coords.X + Hitbox.Width / 2, Coords.Y + Hitbox.Height / 2);

        foreach (var item in gameWorld.AllItems)
        {
            var itemCenter = new Vector2(
                item.Coords.X + item.Hitbox.Width / 2,
                item.Coords.Y + item.Hitbox.Height / 2
            );
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
