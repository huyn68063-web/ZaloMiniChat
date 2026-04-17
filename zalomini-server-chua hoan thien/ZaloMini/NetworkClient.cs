csharp ZaloMini.Client\Network\NetworkClient.cs
using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZaloMini.Client.Models;

namespace ZaloMini.Client.Network
{
    public class NetworkClient : INetworkClient
    {
        private TcpClient _tcp;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private readonly object _sendLock = new object();

        public bool IsConnected => _tcp?.Connected ?? false;

        public event Action<Packet> OnPacketReceived;

        public async Task ConnectAsync(string host, int port)
        {
            if (IsConnected) return;

            _tcp = new TcpClient();
            await _tcp.ConnectAsync(host, port).ConfigureAwait(false);
            _stream = _tcp.GetStream();
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _cts?.Cancel();
                _stream?.Close();
                _tcp?.Close();
            }
            catch { }
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            _ = DisconnectAsync();
        }

        public async Task SendPacketAsync(Packet packet)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected");

            byte[] body = SerializePacket(packet);
            byte[] header = BitConverter.GetBytes(body.Length); // little-endian

            lock (_sendLock)
            {
                // rely on stream WriteAsync for thread-safety across awaits
                _stream.Write(header, 0, header.Length);
                _stream.Write(body, 0, body.Length);
                _stream.Flush();
            }

            await Task.CompletedTask;
        }

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            try
            {
                var headerBuf = new byte[4];
                while (!ct.IsCancellationRequested && IsConnected)
                {
                    int read = await ReadExactAsync(_stream, headerBuf, 0, 4, ct).ConfigureAwait(false);
                    if (read == 0) break;

                    int len = BitConverter.ToInt32(headerBuf, 0);
                    if (len <= 0) continue;

                    var bodyBuf = new byte[len];
                    read = await ReadExactAsync(_stream, bodyBuf, 0, len, ct).ConfigureAwait(false);
                    if (read == 0) break;

                    var packet = DeserializePacket(bodyBuf);
                    OnPacketReceived?.Invoke(packet);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception) { /* log as needed */ }
            finally
            {
                await DisconnectAsync().ConfigureAwait(false);
            }
        }

        private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
        {
            int total = 0;
            while (total < count)
            {
                int read = await stream.ReadAsync(buffer, offset + total, count - total, ct).ConfigureAwait(false);
                if (read == 0) return 0;
                total += read;
            }
            return total;
        }

        private static byte[] SerializePacket(Packet packet)
        {
            var ser = new DataContractJsonSerializer(typeof(Packet));
            using (var ms = new MemoryStream())
            {
                ser.WriteObject(ms, packet);
                return ms.ToArray();
            }
        }

        private static Packet DeserializePacket(byte[] data)
        {
            var ser = new DataContractJsonSerializer(typeof(Packet));
            using (var ms = new MemoryStream(data))
            {
                return ser.ReadObject(ms) as Packet;
            }
        }
    }
}