using CasinoRoyale.Classes.GameObjects.Items;

namespace CasinoRoyale.Classes.GameObjects.Player;

/// <summary>
/// Represents a single slot in the player's inventory
/// Stores a reference to an item type and the count of that item
/// </summary>
public class InventorySlot
{
    public ItemType? ItemType { get; private set; }
    public int Count { get; private set; }
    
    public InventorySlot()
    {
        ItemType = null;
        Count = 0;
    }
    
    /// <summary>
    /// Check if this slot is empty
    /// </summary>
    public bool IsEmpty() => ItemType == null || Count <= 0;
    
    /// <summary>
    /// Check if this slot contains the specified item type
    /// </summary>
    public bool ContainsItemType(ItemType itemType) => ItemType == itemType && Count > 0;
    
    /// <summary>
    /// Add an item of the specified type to this slot
    /// Returns true if successful, false if slot already contains a different item type
    /// </summary>
    public bool AddItem(ItemType itemType, int quantity = 1)
    {
        if (IsEmpty())
        {
            ItemType = itemType;
            Count = quantity;
            return true;
        }
        else if (ItemType == itemType)
        {
            Count += quantity;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Remove one item from this slot
    /// Returns true if successful, false if slot is empty
    /// </summary>
    public bool RemoveItem()
    {
        if (IsEmpty()) return false;
        
        Count--;
        if (Count <= 0)
        {
            Clear();
        }
        return true;
    }
    
    /// <summary>
    /// Remove all items from this slot
    /// </summary>
    public void Clear()
    {
        ItemType = null;
        Count = 0;
    }
    
    /// <summary>
    /// Get the current count of items in this slot
    /// </summary>
    public int GetCount() => Count;
    
    /// <summary>
    /// Get the item type stored in this slot (null if empty)
    /// </summary>
    public ItemType? GetItemType() => ItemType;
}
