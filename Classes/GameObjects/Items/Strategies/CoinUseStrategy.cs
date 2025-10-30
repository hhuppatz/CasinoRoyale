using CasinoRoyale.Classes.GameObjects.Player;
using CasinoRoyale.Utils;

namespace CasinoRoyale.Classes.GameObjects.Items.Strategies;

/// <summary>
/// Strategy for using coins - could be for purchasing, gambling, etc.
/// </summary>
public class CoinUseStrategy : IItemUseStrategy
{
    public void Execute(PlayableCharacter player, ItemType itemType)
    {
        // For now, just log that a coin was used
        // In the future, this could open a shop, gambling interface, etc.
        var inventory = player.GetInventory();
        if (inventory != null)
        {
            int coinCount = inventory.GetItemCount(ItemType.COIN);
            Logger.Info($"Player {player.GetUsername()} used a coin! (Has {coinCount} coins)");
        }
    }
    
    public string GetDescription()
    {
        return "Use coin for gambling or purchasing";
    }
}
