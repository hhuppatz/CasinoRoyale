using System.Collections.Generic;
using CasinoRoyale.Classes.GameObjects.Items.Interfaces;
using CasinoRoyale.Classes.GameObjects.Items.Strategies;
using CasinoRoyale.Classes.GameObjects.Player;
using CasinoRoyale.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.GameObjects.Items.Coin;

/// <summary>
/// Coin item - automatically collected on contact (no E key required)
/// Can be used with I key for gambling/purchasing
/// Can be dropped with Q key
/// </summary>
public class Coin(
    uint itemId,
    Texture2D tex,
    Vector2 coords,
    Vector2 startVelocity,
    float mass = 4.0f,
    float elasticity = 0.5f
)
    : Item(itemId, ItemType.COIN, tex, coords, startVelocity, mass, elasticity),
        IPickupable,
        IUsable,
        IDroppable
{
    // IPickupable - Coins are auto-collected (don't require E key)
    public bool RequiresManualPickup => false;

    private readonly IItemUseStrategy useStrategy = ItemStrategyFactory.GetStrategy(ItemType.COIN);

    public override void Update(float dt, Rectangle gameArea, IEnumerable<Rectangle> tileRects)
    {
        base.Update(dt, gameArea, tileRects);
        if (Lifetime > 5)
        {
            DestroyEntity();
        }
    }

    public override void Collect()
    {
        // Mark for destruction after collection
        DestroyEntity();
    }

    // IPickupable implementation
    public void OnPickup(PlayableCharacter player)
    {
        var inventory = player.GetInventory();
        if (inventory != null && inventory.TryAddItem(ItemType.COIN))
        {
            Logger.Info($"Player {player.GetUsername()} picked up a coin!");
            Collect();
        }
    }

    // IUsable implementation
    public void Use(PlayableCharacter player)
    {
        useStrategy.Execute(player, ItemType.COIN);
    }

    public string GetUsageDescription()
    {
        return useStrategy.GetDescription();
    }

    // IDroppable implementation
    public void OnDrop(PlayableCharacter player, Vector2 dropPosition, Vector2 dropVelocity)
    {
        // Coin drop behavior is handled by the inventory system
        // This method is here for interface compliance
        Logger.Info($"Player {player.GetUsername()} dropped a coin!");
    }
}
