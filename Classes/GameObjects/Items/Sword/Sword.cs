using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CasinoRoyale.Classes.GameObjects.Items.Interfaces;
using CasinoRoyale.Classes.GameObjects.Items.Strategies;
using CasinoRoyale.Classes.GameObjects.Player;
using CasinoRoyale.Utils;

namespace CasinoRoyale.Classes.GameObjects.Items.Sword;

/// <summary>
/// Sword item - requires E key to pick up
/// Can be used with I key to attack
/// Can be dropped with Q key
/// </summary>
public class Sword(uint itemId, Texture2D tex, Vector2 coords, Vector2 startVelocity, float mass = 4.0f, float elasticity = 0.5f)
: Item(itemId, ItemType.SWORD, tex, coords, startVelocity, mass, elasticity),
  IPickupable, IUsable, IDroppable
{
    // IPickupable - Swords require manual pickup with E key
    public bool RequiresManualPickup => true;
    
    private readonly IItemUseStrategy useStrategy = ItemStrategyFactory.GetStrategy(ItemType.SWORD);
    
    public override void Update(float dt, Rectangle gameArea, IEnumerable<Rectangle> tileRects)
    {
        base.Update(dt, gameArea, tileRects);
        if (Lifetime > 30)
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
        if (inventory != null && inventory.TryAddItem(ItemType.SWORD))
        {
            Logger.Info($"Player {player.GetUsername()} picked up a sword!");
            Collect();
        }
    }
    
    // IUsable implementation
    public void Use(PlayableCharacter player)
    {
        useStrategy.Execute(player, ItemType.SWORD);
    }
    
    public string GetUsageDescription()
    {
        return useStrategy.GetDescription();
    }
    
    // IDroppable implementation
    public void OnDrop(PlayableCharacter player, Vector2 dropPosition, Vector2 dropVelocity)
    {
        // Sword drop behavior is handled by the inventory system
        // This method is here for interface compliance
        Logger.Info($"Player {player.GetUsername()} dropped a sword!");
    }
}