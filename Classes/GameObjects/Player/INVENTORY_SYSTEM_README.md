# Inventory System Documentation

## Overview

This inventory system allows players to pick up, hold, drop, and use items. It implements several Object-Oriented design patterns including **Factory**, **Strategy**, and **Interface Segregation** patterns.

## Architecture

### Core Components

#### 1. **InventorySlot** (`InventorySlot.cs`)
- Represents a single slot in the player's inventory
- Stores an `ItemType?` and a count
- Handles adding/removing items and tracking quantities

#### 2. **Inventory** (`Inventory.cs`)
- Manages 3 fixed slots for different item types
- Automatically adds new item types to the first available slot
- Stacks items of the same type in existing slots
- Provides methods for querying and manipulating inventory contents

#### 3. **Item Behavior Interfaces**
Located in `Classes/GameObjects/Items/Interfaces/`:

- **IPickupable**: Items that can be picked up by players
  - `RequiresManualPickup`: Whether E key is needed (false = auto-collect)
  - `OnPickup()`: Called when player picks up the item

- **IUsable**: Items that can be used with the I key
  - `Use()`: Execute item-specific behavior
  - `GetUsageDescription()`: Get what the item does

- **IDroppable**: Items that can be dropped with Q key
  - `OnDrop()`: Called when player drops the item

### Design Patterns

#### Factory Pattern
- **IItemFactory**: Creates items with consistent initialization
- **CoinFactory** and **SwordFactory**: Concrete implementations
- **ItemStrategyFactory**: Creates appropriate use strategies for item types

#### Strategy Pattern
- **IItemUseStrategy**: Defines interface for item usage behavior
- **CoinUseStrategy**: Behavior for using coins (gambling, purchasing)
- **SwordUseStrategy**: Behavior for using swords (attacking)
- Each item type can have unique usage behavior without modifying the item class

#### Interface Segregation
- Items implement only the interfaces they need
- Clear separation of concerns (pickup vs use vs drop)

## Key Controls

| Key | Action | Description |
|-----|--------|-------------|
| **E** | Pick Up | Pick up items that require manual pickup (swords, etc.) |
| **Q** | Drop | Drop one item from inventory (from first occupied slot) |
| **I** | Use | Use an item (triggers item-specific behavior) |

## Item Behaviors

### Coins
- **Pickup**: Auto-collected on contact (no E key needed)
- **Use (I)**: Logs usage (extendable for gambling/purchasing)
- **Drop (Q)**: Drops a single coin from inventory
- **Lifetime**: 5 seconds before despawning

### Swords
- **Pickup**: Requires E key to pick up
- **Use (I)**: Logs swing action (extendable for combat)
- **Drop (Q)**: Drops a single sword from inventory
- **Lifetime**: 30 seconds before despawning

## Inventory Rules

1. **3 Fixed Slots**: Each slot can hold one item type
2. **Stacking**: Multiple items of the same type stack in one slot
3. **Priority**: When picking up items, existing stacks are prioritized
4. **Full Inventory**: If all 3 slots are occupied with different item types, no new types can be added
5. **First In, First Out**: Drop and Use operations target the first occupied slot

## Usage Examples

### Adding an Item to Inventory
```csharp
// Automatic via pickup
if (inventory.TryAddItem(ItemType.COIN, 1))
{
    Logger.Info("Coin added to inventory!");
}
```

### Checking Inventory Contents
```csharp
// Check if player has a specific item
if (inventory.HasItem(ItemType.SWORD))
{
    int count = inventory.GetItemCount(ItemType.SWORD);
    Logger.Info($"Player has {count} swords");
}

// Check if inventory is full
if (inventory.IsFull())
{
    Logger.Info("Inventory is full!");
}
```

### Using Items
```csharp
// Get use strategy for item type
var strategy = ItemStrategyFactory.GetStrategy(ItemType.SWORD);
strategy.Execute(player, ItemType.SWORD);
```

## Extending the System

### Adding a New Item Type

1. **Add enum value** to `ItemType` in `Item.cs`:
```csharp
public enum ItemType
{
    COIN,
    SWORD,
    POTION  // New item
}
```

2. **Create item class** implementing appropriate interfaces:
```csharp
public class Potion : Item, IPickupable, IUsable, IDroppable
{
    public bool RequiresManualPickup => true;
    // Implement interface methods...
}
```

3. **Create use strategy**:
```csharp
public class PotionUseStrategy : IItemUseStrategy
{
    public void Execute(PlayableCharacter player, ItemType itemType)
    {
        // Heal player, etc.
    }
    
    public string GetDescription() => "Restores health";
}
```

4. **Register strategy** in `ItemStrategyFactory`:
```csharp
strategies.Add(ItemType.POTION, new PotionUseStrategy());
```

5. **Create factory** for the item:
```csharp
public class PotionFactory : IItemFactory<Item>
{
    // Implement factory methods...
}
```

## Code Structure

```
Classes/GameObjects/
├── Player/
│   ├── PlayableCharacter.cs    # Main player class with inventory integration
│   ├── Inventory.cs             # Inventory management (3 slots)
│   └── InventorySlot.cs         # Individual slot logic
└── Items/
    ├── Item.cs                  # Base item class
    ├── Interfaces/
    │   ├── IPickupable.cs       # Pickup behavior
    │   ├── IUsable.cs           # Use behavior
    │   └── IDroppable.cs        # Drop behavior
    ├── Strategies/
    │   ├── IItemUseStrategy.cs  # Strategy interface
    │   ├── CoinUseStrategy.cs   # Coin usage behavior
    │   ├── SwordUseStrategy.cs  # Sword usage behavior
    │   └── ItemStrategyFactory.cs # Factory for strategies
    ├── Coin/
    │   └── Coin.cs              # Coin implementation
    └── Sword/
        └── Sword.cs             # Sword implementation
```

## Future Enhancements

- **Inventory UI**: Visual representation of inventory slots
- **Item Swapping**: Choose which slot to drop from
- **Hotkeys**: Number keys (1, 2, 3) to use specific slots
- **Weight System**: Limit inventory by weight instead of just slots
- **Item Combination**: Craft new items from existing ones
- **Persistence**: Save/load inventory state
- **Networking**: Sync inventory state across multiplayer sessions
