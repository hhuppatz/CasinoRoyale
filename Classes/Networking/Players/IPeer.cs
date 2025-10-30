using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;

namespace CasinoRoyale.Classes.Networking.Players;

// Common interface for both direct NetPeer connections and relay connections
public interface IPeer
    {
        int Id { get; }
        ConnectionState ConnectionState { get; }
        int Ping { get; }
        
        void Send(NetDataWriter writer, DeliveryMethod deliveryMethod);
        void Send(byte[] data, DeliveryMethod deliveryMethod);
        void Send(byte[] data, int start, int length, DeliveryMethod deliveryMethod);
        void Disconnect(byte[] data = null);
        
        // Gets the underlying NetPeer if this is a direct connection, otherwise null
        NetPeer GetNetPeer();
        
        // Gets a string representation of the peer's address
        string GetAddressString();
    }
    
    // Wrapper for direct NetPeer connections
    public class DirectPeer(NetPeer netPeer) : IPeer
    {
        public int Id => netPeer.Id;
        public ConnectionState ConnectionState => netPeer.ConnectionState;
        public int Ping => netPeer.Ping;
        
        public void Send(NetDataWriter writer, DeliveryMethod deliveryMethod)
        {
            netPeer.Send(writer, deliveryMethod);
        }
        
        public void Send(byte[] data, DeliveryMethod deliveryMethod)
        {
            netPeer.Send(data, deliveryMethod);
        }
        
        public void Send(byte[] data, int start, int length, DeliveryMethod deliveryMethod)
        {
            netPeer.Send(data, start, length, deliveryMethod);
        }
        
        public void Disconnect(byte[] data = null)
        {
            netPeer.Disconnect(data);
        }
        
        public NetPeer GetNetPeer() => netPeer;
        
        public string GetAddressString() => netPeer.Address?.ToString() ?? "Unknown";
        
        public override string ToString() => $"DirectPeer({GetAddressString()})";
    }