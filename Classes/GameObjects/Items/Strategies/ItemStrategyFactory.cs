using System.Collections.Generic;

namespace CasinoRoyale.Classes.GameObjects.Items.Strategies;

/// <summary>
/// Factory for creating item use strategies based on item type
/// Implements Factory pattern for strategy creation
/// </summary>
public static class ItemStrategyFactory
{
    private static readonly Dictionary<ItemType, IItemUseStrategy> strategies = new()
    {
        { ItemType.COIN, new CoinUseStrategy() },
        { ItemType.SWORD, new SwordUseStrategy() }
    };
    
    /// <summary>
    /// Get the use strategy for a specific item type
    /// </summary>
    public static IItemUseStrategy GetStrategy(ItemType itemType)
    {
        if (strategies.TryGetValue(itemType, out var strategy))
        {
            return strategy;
        }
        
        // Default strategy if none found
        return new DefaultUseStrategy();
    }
    
    /// <summary>
    /// Register a custom strategy for an item type
    /// </summary>
    public static void RegisterStrategy(ItemType itemType, IItemUseStrategy strategy)
    {
        strategies[itemType] = strategy;
    }
}

/// <summary>
/// Default use strategy when no specific strategy is defined
/// </summary>
public class DefaultUseStrategy : IItemUseStrategy
{
    public void Execute(CasinoRoyale.Classes.GameObjects.Player.PlayableCharacter player, ItemType itemType)
    {
        Utils.Logger.Info($"Item {itemType} has no specific use behavior defined");
    }
    
    public string GetDescription()
    {
        return "No specific use";
    }
}
