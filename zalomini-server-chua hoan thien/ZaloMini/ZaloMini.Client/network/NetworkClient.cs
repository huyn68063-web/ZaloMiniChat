using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using ZaloMini.Client.Models;

namespace ZaloMini.Client
{
    public class NetworkClient : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        public bool IsConnected { get; private set; }

        // Packet-based event (JSON Packet objects)
        public event Action<Packet> OnPacketReceived;

        // Keep a disconnect event for UI
        public event Action OnDisconnected;

        public bool Connect(string ip, int port)
        {
            try
            {
                _client = new TcpClient();
                _client.ReceiveTimeout = 5000;
                _client.SendTimeout = 5000;
                _client.Connect(ip, port);
                _stream = _client.GetStream();
                IsConnected = true;

                _cts = new CancellationTokenSource();
                Task.Run(() => ReceiveLoopAsync(_cts.Token));
                return true;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                Console.WriteLine("❌ Lỗi kết nối: " + ex.Message);
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _cts?.Cancel();
                _stream?.Close();
                _client?.Close();
            }
            catch { }
            finally
            {
                IsConnected = false;
            }

            await Task.CompletedTask;
        }

        public void Disconnect()
        {
            _ = DisconnectAsync();
        }

        public void Dispose()
        {
            _ = DisconnectAsync();
        }

        // Send a Packet (JSON) with 4-byte length prefix (async, awaited)
        public async Task SendPacketAsync(Packet packet)
        {
            if (!IsConnected || _stream == null) throw new InvalidOperationException("Not connected");

            byte[] body;
            var ser = new DataContractJsonSerializer(typeof(Packet));
            using (var ms = new MemoryStream())
            {
                ser.WriteObject(ms, packet);
                body = ms.ToArray();
            }

            byte[] header = BitConverter.GetBytes(body.Length); // little-endian

            await _sendLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await _stream.WriteAsync(header, 0, header.Length).ConfigureAwait(false);
                await _stream.WriteAsync(body, 0, body.Length).ConfigureAwait(false);
                await _stream.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendPacket error: " + ex.Message);
                throw;
            }
            finally
            {
                _sendLock.Release();
            }
        }

        // Legacy helper: wrap a raw status/string into a Packet (keeps compatibility)
        public async Task SendRawStringAsStatusAsync(string message)
        {
            var p = new Packet { Type = PacketType.Status, Payload = message };
            await SendPacketAsync(p).ConfigureAwait(false);
        }

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            try
            {
                var header = new byte[4];
                while (!ct.IsCancellationRequested && IsConnected)
                {
                    int read = await ReadExactAsync(_stream, header, 0, 4, ct).ConfigureAwait(false);
                    if (read == 0) break;

                    int len = BitConverter.ToInt32(header, 0);
                    if (len <= 0) continue;

                    var body = new byte[len];
                    read = await ReadExactAsync(_stream, body, 0, len, ct).ConfigureAwait(false);
                    if (read == 0) break;

                    Packet packet = null;
                    try
                    {
                        var ser = new DataContractJsonSerializer(typeof(Packet));
                        using (var ms = new MemoryStream(body))
                        {
                            packet = ser.ReadObject(ms) as Packet;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to deserialize Packet: " + ex.Message);
                        continue;
                    }

                    try
                    {
                        OnPacketReceived?.Invoke(packet);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("OnPacketReceived handler error: " + ex.Message);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine("ReceiveLoop error: " + ex.Message);
            }
            finally
            {
                IsConnected = false;
                try { OnDisconnected?.Invoke(); } catch { }
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
    }
}