using System;
using System.Linq;
using CasinoRoyale.Classes.GameObjects.Items;
using CasinoRoyale.Classes.GameObjects.Items.Interfaces;

namespace CasinoRoyale.Classes.GameObjects.Player;

/// <summary>
/// Player inventory system with 3 fixed slots for different item types
/// Uses Factory and Strategy patterns for item management
/// </summary>
public class Inventory
{
    private readonly uint playerId;
    public uint PlayerId => playerId;
    
    // Fixed array of 3 inventory slots
    private readonly InventorySlot[] slots = new InventorySlot[3];
    public const int MAX_SLOTS = 3;
    
    public Inventory(uint playerId)
    {
        this.playerId = playerId;
        // Initialize all slots
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            slots[i] = new InventorySlot();
        }
    }
    
    /// <summary>
    /// Try to add an item to the inventory
    /// First checks if the item type already exists, then adds to that slot
    /// Otherwise, finds the first available empty slot
    /// </summary>
    /// <returns>True if item was added successfully</returns>
    public bool TryAddItem(ItemType itemType, int quantity = 1)
    {
        // First, check if we already have this item type in any slot
        foreach (var slot in slots)
        {
            if (slot.ContainsItemType(itemType))
            {
                slot.AddItem(itemType, quantity);
                return true;
            }
        }
        
        // If not, find the first empty slot
        foreach (var slot in slots)
        {
            if (slot.IsEmpty())
            {
                slot.AddItem(itemType, quantity);
                return true;
            }
        }
        
        // No empty slots available
        return false;
    }
    
    /// <summary>
    /// Remove one item of the specified type from the inventory
    /// </summary>
    /// <returns>True if item was removed successfully</returns>
    public bool TryRemoveItem(ItemType itemType)
    {
        foreach (var slot in slots)
        {
            if (slot.ContainsItemType(itemType))
            {
                return slot.RemoveItem();
            }
        }
        return false;
    }
    
    /// <summary>
    /// Remove an item from a specific slot index
    /// </summary>
    /// <returns>True if item was removed successfully</returns>
    public bool TryRemoveItemFromSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MAX_SLOTS) return false;
        return slots[slotIndex].RemoveItem();
    }
    
    /// <summary>
    /// Check if the inventory contains the specified item type
    /// </summary>
    public bool HasItem(ItemType itemType)
    {
        return slots.Any(slot => slot.ContainsItemType(itemType));
    }
    
    /// <summary>
    /// Get the count of a specific item type in the inventory
    /// </summary>
    public int GetItemCount(ItemType itemType)
    {
        foreach (var slot in slots)
        {
            if (slot.ContainsItemType(itemType))
            {
                return slot.GetCount();
            }
        }
        return 0;
    }
    
    /// <summary>
    /// Get the slot that contains the specified item type
    /// </summary>
    /// <returns>Slot index or -1 if not found</returns>
    public int GetSlotIndex(ItemType itemType)
    {
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (slots[i].ContainsItemType(itemType))
            {
                return i;
            }
        }
        return -1;
    }
    
    /// <summary>
    /// Get a specific inventory slot by index
    /// </summary>
    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= MAX_SLOTS)
            throw new ArgumentOutOfRangeException(nameof(index), $"Slot index must be between 0 and {MAX_SLOTS - 1}");
        return slots[index];
    }
    
    /// <summary>
    /// Get all non-empty slots
    /// </summary>
    public InventorySlot[] GetOccupiedSlots()
    {
        return slots.Where(slot => !slot.IsEmpty()).ToArray();
    }
    
    /// <summary>
    /// Check if the inventory is full
    /// </summary>
    public bool IsFull()
    {
        return slots.All(slot => !slot.IsEmpty());
    }
    
    /// <summary>
    /// Get the number of empty slots
    /// </summary>
    public int GetEmptySlotCount()
    {
        return slots.Count(slot => slot.IsEmpty());
    }
    
    /// <summary>
    /// Clear all items from the inventory
    /// </summary>
    public void Clear()
    {
        foreach (var slot in slots)
        {
            slot.Clear();
        }
    }
}