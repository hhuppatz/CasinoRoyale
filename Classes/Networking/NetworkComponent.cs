using CasinoRoyale.Classes.GameObjects;
using CasinoRoyale.Utils;
using LiteNetLib.Utils;

namespace CasinoRoyale.Classes.Networking;

/// <summary>
/// Component that automatically networks GameEntity state changes
/// Subscribes to entity's OnChanged event and broadcasts changes via NetworkManager
/// </summary>
public class NetworkComponent
{
    private readonly INetworkObject _object;

    public NetworkComponent(INetworkObject obj)
    {
        _object = obj;

        // Subscribe to the entity's change event
        _object.OnChanged += HandleObjectChanged;
    }

    private void HandleObjectChanged(string propertyName, INetSerializable newValue)
    {
        // If newValue is null, try to get state from the object itself
        INetSerializable stateToSend = newValue;

        if (stateToSend == null && _object is GameEntity entity)
        {
            // Get the entity's current state and wrap it in a packet
            var entityState = entity.GetEntityState();
            stateToSend = new GameEntityStatePacket { state = entityState };
        }

        if (stateToSend != null)
        {
            NetworkManager.Instance.NotifyObjectChanged(
                _object.NetworkObjectId,
                propertyName,
                stateToSend
            );
            Logger.LogNetwork(
                "NETWORK_COMPONENT",
                $"Notified change: objectId={_object.NetworkObjectId}, property={propertyName}"
            );
        }
        else
        {
            Logger.Warning(
                $"NetworkComponent: Could not get state for object {_object.NetworkObjectId}"
            );
        }
    }

    // Optional: call this if you ever need to stop listening
    public void Unsubscribe()
    {
        _object.OnChanged -= HandleObjectChanged;
    }
}
