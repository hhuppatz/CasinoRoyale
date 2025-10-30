using CasinoRoyale.Utils;
using LiteNetLib.Utils;

namespace CasinoRoyale.Classes.Networking;

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
        NetworkManager.Instance.NotifyObjectChanged(_object.NetworkObjectId, propertyName, newValue);
    }

    // Optional: call this if you ever need to stop listening
    public void Unsubscribe()
    {
        _object.OnChanged -= HandleObjectChanged;
    }
}
