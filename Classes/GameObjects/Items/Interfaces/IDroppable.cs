using Microsoft.Xna.Framework;
using CasinoRoyale.Classes.GameObjects.Player;

namespace CasinoRoyale.Classes.GameObjects.Items.Interfaces;

/// <summary>
/// Interface for items that can be dropped by players (Q key)
/// </summary>
public interface IDroppable
{
    /// <summary>
    /// Called when a player drops this item
    /// </summary>
    /// <param name="player">The player dropping the item</param>
    /// <param name="dropPosition">The position to drop the item at</param>
    /// <param name="dropVelocity">The velocity to drop the item with</param>
    void OnDrop(PlayableCharacter player, Vector2 dropPosition, Vector2 dropVelocity);
}
