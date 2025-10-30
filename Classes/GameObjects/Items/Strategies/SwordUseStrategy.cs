using CasinoRoyale.Classes.GameObjects.Player;
using CasinoRoyale.Utils;

namespace CasinoRoyale.Classes.GameObjects.Items.Strategies;

/// <summary>
/// Strategy for using swords - could be for attacking, equipping, etc.
/// </summary>
public class SwordUseStrategy : IItemUseStrategy
{
    public void Execute(PlayableCharacter player, ItemType itemType)
    {
        // For now, just log that a sword was used
        // In the future, this could trigger an attack animation, equip the weapon, etc.
        var inventory = player.GetInventory();
        if (inventory != null)
        {
            int swordCount = inventory.GetItemCount(ItemType.SWORD);
            Logger.Info($"Player {player.GetUsername()} swung their sword! (Has {swordCount} swords)");
        }
    }
    
    public string GetDescription()
    {
        return "Swing sword to attack";
    }
}
