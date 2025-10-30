using CasinoRoyale.Classes.GameObjects.Player;

namespace CasinoRoyale.Classes.GameObjects.Items.Interfaces;

/// <summary>
/// Interface for items that can be used (I key)
/// Uses Strategy pattern to define item-specific usage behavior
/// </summary>
public interface IUsable
{
    /// <summary>
    /// Use this item type
    /// </summary>
    /// <param name="player">The player using the item</param>
    void Use(PlayableCharacter player);

    /// <summary>
    /// Get a description of what this item does when used
    /// </summary>
    string GetUsageDescription();
}
