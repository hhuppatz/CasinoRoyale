using CasinoRoyale.Classes.GameObjects.Player;

namespace CasinoRoyale.Classes.GameObjects.Items.Strategies;

/// <summary>
/// Strategy pattern interface for item usage behavior
/// Different items can have different use behaviors
/// </summary>
public interface IItemUseStrategy
{
    /// <summary>
    /// Execute the use behavior for this item
    /// </summary>
    void Execute(PlayableCharacter player, ItemType itemType);
    
    /// <summary>
    /// Get a description of what this strategy does
    /// </summary>
    string GetDescription();
}
