using System;
using System.Collections.Generic;
using System.Linq;
using CasinoRoyale.Classes.GameObjects.CasinoMachines;
using CasinoRoyale.Classes.GameObjects.Platforms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Classes.GameObjects.Items;

public class ItemFactory(Texture2D coinTex)
{
    private readonly Texture2D coinTex = coinTex;
    public List<Item> Items { get; private set; } = [];
    private uint nextItemId = 0;

    public Texture2D GetCoinTexture() => coinTex;

    public void UpdateItems(float dt, Rectangle gameArea, List<Platform> platforms)
    {
        // Remove coins that have fallen off the world or been stationary for too long
        Items.RemoveAll(item => 
            item.Coords.Y > gameArea.Bottom || 
            (Math.Abs(item.Velocity.X) < 0.01f && Math.Abs(item.Velocity.Y) < 0.01f && item.Coords.Y > gameArea.Height * 0.9f) ||
            item.Destroyed
        );

        // Update remaining coins
        foreach (var item in Items)
        {
            item.Update(dt, gameArea, platforms);
        }
    }

    public void RemoveItemById(uint id)
    {
        // Find and remove coin by actual coinId, not array index
        var item = Items.FirstOrDefault(c => c.ItemId == id);
        if (item != null)
        {
            Items.Remove(item);
        }
    }
    
    public Item SpawnCoinFromCasinoMachine(uint machineNum, List<CasinoMachine> casinoMachines)
    {
        var machine = casinoMachines.FirstOrDefault(m => m.GetState().machineNum == machineNum);
        if (machine != null)
        {
            var coin = machine.SpawnCoin(nextItemId++, coinTex);
            coin.MarkAsChanged(); // Mark new coin as changed
            Items.Add(coin);
            // Reset the spawnedCoin flag after spawning
            machine.SpawnedCoin = false;
            machine.MarkAsChanged(); // Mark machine as changed since spawnedCoin flag changed
            return coin;
        }
        return null;
    }
    
    public void RecreateItemsFromStates(ItemState[] itemStates)
    {
        Console.WriteLine($"RecreateItemsFromStates called with {itemStates?.Length ?? 0} item states");
        Items.Clear();
        uint maxItemId = 0;
        foreach (var itemState in itemStates ?? [])
        {
            var coin = new Item(itemState.itemId, ItemType.COIN, coinTex, itemState.gameEntityState.coords, itemState.gameEntityState.velocity, itemState.gameEntityState.mass);
            coin.MarkAsChanged(); // Mark recreated coins as changed
            Items.Add(coin);
            
            // Track the highest coin ID to avoid conflicts
            if (itemState.itemId >= maxItemId)
            {
                maxItemId = itemState.itemId + 1;
            }
        }
        
        // Update nextCoinId to avoid conflicts with existing coins
        if (maxItemId > nextItemId)
        {
            nextItemId = maxItemId;
        }
        
        Console.WriteLine($"Created {Items.Count} coins, nextCoinId set to {nextItemId}");
    }
    
    public ItemState[] GetItemStates()
    {
        var itemStates = new ItemState[Items.Count];
        for (int i = 0; i < Items.Count; i++)
        {
            itemStates[i] = Items[i].GetState();
        }
        return itemStates;
    }

    // Gets only changed item states for networking
    public ItemState[] GetChangedItemStates()
    {
        var changedItems = Items.Where(c => c.HasChanged).ToList();
        var itemStates = new ItemState[changedItems.Count];
        for (int i = 0; i < changedItems.Count; i++)
        {
            itemStates[i] = changedItems[i].GetState();
        }
        return itemStates;
    }
}