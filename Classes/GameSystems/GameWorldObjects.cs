using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using CasinoRoyale.Utils;
using CasinoRoyale.Classes.GameObjects.CasinoMachines;
using CasinoRoyale.Classes.GameObjects.Platforms;
using CasinoRoyale.Classes.GameObjects;
using CasinoRoyale.Classes.Networking;
using CasinoRoyale.Classes.GameObjects.Items;

namespace CasinoRoyale.Classes.GameSystems;
    // Manages game world objects (platforms and casino machines) for both Host and Client
public class GameWorldObjects(Properties properties, ContentManager content, SpriteBatch spriteBatch, MainCamera camera, Vector2 ratio)
{
    private readonly Properties gameProperties = properties;
    private readonly Artist artist = new(content, spriteBatch, camera, ratio);
        
    private readonly CasinoMachineFactory casinoMachineFactory = new(content.Load<Texture2D>(properties.get("casinoMachine.image.1", "CasinoMachine1")));
    private readonly PlatformFactory platformFactory = new(content.Load<Texture2D>(properties.get("casinoFloor.image.1", "CasinoFloor1")));
    private readonly ItemFactory itemFactory = new(content.Load<Texture2D>(properties.get("coin.image", "Coin")));
    
    private readonly Dictionary<uint, uint> processedRequests = []; // machineNum -> lastProcessedRequestId

    public IEnumerable<object> CasinoMachines { get; internal set; }

    public void Update(float dt, Rectangle gameArea)
    {
        itemFactory.UpdateItems(dt, gameArea, platformFactory.Platforms);
    }

    public void DrawGameObjects()
    {
        artist.DrawPlatforms(platformFactory.Platforms);
        artist.DrawCasinoMachines(casinoMachineFactory.CasinoMachines);
        artist.DrawItems(itemFactory.Items);
    }

    // Generates the complete game world with platforms and casino machines
    public void GenerateGameWorld(Rectangle gameArea, Vector2 playerOrigin)
    {   
        // Generate platforms
        platformFactory.GeneratePlatforms(gameArea, playerOrigin);
        
        // Generate casino machines
        casinoMachineFactory.GenerateCasinoMachines(gameProperties, platformFactory.Platforms);
    }

    public void GenerateGameWorldFromState(JoinAcceptPacket joinAccept)
    {
        // Recreate coins from coin states
        if (joinAccept.itemStates != null)
        {
            itemFactory.RecreateItemsFromStates(joinAccept.itemStates);
        }
        
        // Recreate casino machines from casino machine states (override generated ones)
        if (joinAccept.casinoMachineStates != null)
        {
            casinoMachineFactory.RecreateCasinoMachinesFromStates(joinAccept.casinoMachineStates);
        }
    }

    // Clear change flags for all objects after sending updates
    public void ClearAllChangedFlags()
    {
        foreach (var platform in platformFactory.Platforms)
        {
            platform.ClearChangedFlag();
        }
        foreach (var machine in casinoMachineFactory.CasinoMachines)
        {
            machine.ClearChangedFlag();
        }
    }

    public List<Platform> GetPlatforms()
    {
        return platformFactory.Platforms;
    }

    public List<CasinoMachine> GetCasinoMachines()
    {
        return casinoMachineFactory.CasinoMachines;
    }

    public List<Item> GetItems()
    {
        return itemFactory.Items;
    }

    public void UpdateCasinoMachineById(uint id, CasinoMachineState state)
    {
        casinoMachineFactory.UpdateCasinoMachineById(id, state);
    }

    public void UpdatePlatformById(uint id, PlatformState state)
    {
        platformFactory.UpdatePlatformById(id, state);
    }

    // Coin management methods
    public Item SpawnCoinFromCasinoMachine(uint machineNum)
    {
        return itemFactory.SpawnCoinFromCasinoMachine(machineNum, casinoMachineFactory.CasinoMachines);
    }

    public void RemoveCoinById(uint id)
    {
        itemFactory.RemoveItemById(id);
    }

    public void AddItem(Item item)
    {
        itemFactory.Items.Add(item);
    }

    public Texture2D GetCoinTexture()
    {
        return itemFactory.GetCoinTexture();
    }

    public ItemState[] GetItemStates()
    {
        return itemFactory.GetItemStates();
    }

    public ItemState[] GetChangedItemStates()
    {
        return itemFactory.GetChangedItemStates();
    }

    // Casino machine state methods
        public CasinoMachineState[] GetCasinoMachineStates()
        {
        return casinoMachineFactory.GetCasinoMachineStates();
    }

        public CasinoMachineState[] GetChangedCasinoMachineStates()
        {
        return casinoMachineFactory.GetChangedCasinoMachineStates();
    }

    // Platform state methods
    public PlatformState[] GetPlatformStates()
    {
        return platformFactory.GetPlatformStates();
    }

        public List<(uint id, PlatformState state)> GetChangedPlatformUpdates()
        {
        return platformFactory.GetChangedPlatformUpdates();
        }

        public List<(uint id, CasinoMachineState state)> GetChangedCasinoMachineUpdates()
        {
        return casinoMachineFactory.GetChangedCasinoMachineUpdates();
    }

    // Collision detection
    public bool CheckCasinoMachineCollision(Rectangle hitbox)
    {
        return casinoMachineFactory.CheckCasinoMachineCollision(hitbox);
    }

    // Process casino machine states from clients and handle coin spawning
    public List<(uint machineNum, uint requestId, Item coin, bool wasSuccessful)> ProcessCasinoMachineStates(CasinoMachineState[] casinoMachineStates)
    {
        var results = new List<(uint, uint, Item coin, bool wasSuccessful)>();
        
        foreach (var state in casinoMachineStates)
        {
            if (state.spawnedCoin)
            {
                // Check if this request has already been processed
                if (processedRequests.TryGetValue(state.machineNum, out uint lastRequestId))
                {
                    if (state.requestId <= lastRequestId)
                    {
                        // Already processed this request, skip it
                        results.Add((state.machineNum, state.requestId, null, false));
                        continue;
                    }
                }
                
                // Process the coin spawn request
                var coin = SpawnCoinFromCasinoMachine(state.machineNum);
                bool wasSuccessful = coin != null;
                
                if (wasSuccessful)
                {
                    // Track this request ID
                    processedRequests[state.machineNum] = state.requestId;
                }
                
                results.Add((state.machineNum, state.requestId, coin, wasSuccessful));
            }
        }
        
        return results;
    }
}
