using CasinoRoyale.Classes.GameObjects.Player;

namespace CasinoRoyale.Classes.GameObjects.Items.Interfaces;

/// <summary>
/// Interface for items that can be picked up by players
/// Items implementing this should define whether they need manual pickup (E key)
/// </summary>
public interface IPickupable
{
    /// <summary>
    /// Whether this item requires manual pickup (E key) or is auto-collected
    /// </summary>
    bool RequiresManualPickup { get; }
    
    /// <summary>
    /// Called when a player picks up this item
    /// </summary>
    /// <param name="player">The player picking up the item</param>
    void OnPickup(PlayableCharacter player);
}
