using System.Collections.Generic;
using CasinoRoyale.Classes.GameObjects.Items;

namespace CasinoRoyale.Classes.GameObjects.Player;

public class Inventory(uint playerId)
{
    private readonly uint playerId = playerId;
    public uint PlayerId { get => playerId; }
    private readonly List<Item> items = [];
    public List<Item> Items { get => items; }

    public void AddItem(Item item) {
        items.Add(item);
    }
}