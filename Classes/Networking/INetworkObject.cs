using System;
using LiteNetLib.Utils;

namespace CasinoRoyale.Classes.Networking;

public interface INetworkObject
{
    public uint NetworkObjectId { get; }
    public event Action<string, INetSerializable> OnChanged;
}