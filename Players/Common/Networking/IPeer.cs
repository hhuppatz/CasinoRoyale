using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;

namespace CasinoRoyale.Players.Common.Networking
{
    /// <summary>
    /// Common interface for both direct NetPeer connections and relay connections
    /// </summary>
    public interface IPeer
    {
        int Id { get; }
        ConnectionState ConnectionState { get; }
        int Ping { get; }
        
        void Send(NetDataWriter writer, DeliveryMethod deliveryMethod);
        void Send(byte[] data, DeliveryMethod deliveryMethod);
        void Send(byte[] data, int start, int length, DeliveryMethod deliveryMethod);
        void Disconnect(byte[] data = null);
        
        /// <summary>
        /// Gets the underlying NetPeer if this is a direct connection, otherwise null
        /// </summary>
        NetPeer GetNetPeer();
        
        /// <summary>
        /// Gets a string representation of the peer's address
        /// </summary>
        string GetAddressString();
    }
    
    /// <summary>
    /// Wrapper for direct NetPeer connections
    /// </summary>
    public class DirectPeer : IPeer
    {
        private readonly NetPeer _netPeer;
        
        public DirectPeer(NetPeer netPeer)
        {
            _netPeer = netPeer;
        }
        
        public int Id => _netPeer.Id;
        public ConnectionState ConnectionState => _netPeer.ConnectionState;
        public int Ping => _netPeer.Ping;
        
        public void Send(NetDataWriter writer, DeliveryMethod deliveryMethod)
        {
            _netPeer.Send(writer, deliveryMethod);
        }
        
        public void Send(byte[] data, DeliveryMethod deliveryMethod)
        {
            _netPeer.Send(data, deliveryMethod);
        }
        
        public void Send(byte[] data, int start, int length, DeliveryMethod deliveryMethod)
        {
            _netPeer.Send(data, start, length, deliveryMethod);
        }
        
        public void Disconnect(byte[] data = null)
        {
            _netPeer.Disconnect(data);
        }
        
        public NetPeer GetNetPeer() => _netPeer;
        
        public string GetAddressString() => _netPeer.Address?.ToString() ?? "Unknown";
        
        public override string ToString() => $"DirectPeer({GetAddressString()})";
    }
}
