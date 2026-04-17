csharp ZaloMini.Client\Network\INetworkClient.cs
using System.Threading.Tasks;
using ZaloMini.Client.Models;
using System;

namespace ZaloMini.Client.Network
{
    public interface INetworkClient : IDisposable
    {
        bool IsConnected { get; }
        Task ConnectAsync(string host, int port);
        Task DisconnectAsync();
        Task SendPacketAsync(Packet packet);
        event Action<Packet> OnPacketReceived;
    }
}