    using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace ZaloMini.Server
{
    // Duplicate minimal models for server runtime. For production, share assemblies or NuGet package.
    [DataContract]
    public enum PacketType { Login = 0, Message = 1, Status = 2, Logout = 3 }

    [DataContract]
    public class Packet { [DataMember] public PacketType Type { get; set; } [DataMember] public string Payload { get; set; } [DataMember] public string RequestId { get; set; } }

    [DataContract]
    public class LoginRequest { [DataMember] public string Username { get; set; } [DataMember] public string Password { get; set; } }

    [DataContract]
    public class LoginResult { [DataMember] public bool IsSuccess { get; set; } [DataMember] public string ErrorMessage { get; set; } [DataMember] public string UserId { get; set; } }

    [DataContract]
    public class MessageDTO { [DataMember] public string SenderId { get; set; } [DataMember] public string ReceiverId { get; set; } [DataMember] public string Content { get; set; } [DataMember] public DateTime Timestamp { get; set; } }

    class Program
    {
        // maps userId -> TcpClient wrapper (NetworkStream)
        private static ConcurrentDictionary<string, TcpClient> _clients = new ConcurrentDictionary<string, TcpClient>();

        static void Main(string[] args)
        {
            int port = 9000;
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine("ZaloMini.Server listening on port " + port);

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    _ = Task.Run(() => HandleClientAsync(client));
                }
            });

            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            var stream = client.GetStream();
            try
            {
                while (client.Connected)
                {
                    var header = new byte[4];
                    int hread = await ReadExactAsync(stream, header, 0, 4).ConfigureAwait(false);
                    if (hread == 0) break;
                    int len = BitConverter.ToInt32(header, 0);
                    if (len <= 0) continue;

                    var body = new byte[len];
                    int bread = await ReadExactAsync(stream, body, 0, len).ConfigureAwait(false);
                    if (bread == 0) break;

                    var packet = DeserializePacket(body);
                    await ProcessPacketAsync(packet, client).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client handler error: " + ex.Message);
            }
            finally
            {
                client.Close();
            }
        }

        private static async Task ProcessPacketAsync(Packet packet, TcpClient client)
        {
            if (packet.Type == PacketType.Login)
            {
                var ser = new DataContractJsonSerializer(typeof(LoginRequest));
                LoginRequest req;
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(packet.Payload)))
                {
                    req = ser.ReadObject(ms) as LoginRequest;
                }

                // Very simple auth: accept any non-empty username
                var result = new LoginResult();
                if (string.IsNullOrWhiteSpace(req?.Username))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Empty username";
                }
                else
                {
                    result.IsSuccess = true;
                    // use username as userId in this sample
                    result.UserId = req.Username;
                    _clients[req.Username] = client;
                    Console.WriteLine("User logged in: " + req.Username);
                }

                // reply
                var packetReply = new Packet { Type = PacketType.Login, Payload = SerializeToJson(result) };
                await SendPacketToClientAsync(client, packetReply).ConfigureAwait(false);
            }
            else if (packet.Type == PacketType.Message)
            {
                // forward to recipient if online, otherwise broadcast to all except sender
                var ser = new DataContractJsonSerializer(typeof(MessageDTO));
                MessageDTO msg;
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(packet.Payload)))
                {
                    msg = ser.ReadObject(ms) as MessageDTO;
                }

                if (msg == null)
                {
                    Console.WriteLine("Received invalid MessageDTO");
                    return;
                }

                var forward = new Packet { Type = PacketType.Message, Payload = SerializeToJson(msg) };

                // Broadcast to all connected clients except the sender
                foreach (var kvp in _clients)
                {
                    string userId = kvp.Key;
                    TcpClient clientConn = kvp.Value;

                    // skip sender
                    if (string.Equals(userId, msg.SenderId, StringComparison.OrdinalIgnoreCase)) continue;

                    // skip null/disconnected
                    if (clientConn == null || !clientConn.Connected) continue;

                    try
                    {
                        await SendPacketToClientAsync(clientConn, forward).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send to {userId}: {ex.Message}");
                        // continue sending to others
                    }
                }
            }
        }

        private static async Task SendPacketToClientAsync(TcpClient client, Packet packet)
        {
            try
            {
                var stream = client.GetStream();
                var body = Encoding.UTF8.GetBytes(SerializeToJson(packet));
                var header = BitConverter.GetBytes(body.Length);
                await stream.WriteAsync(header, 0, header.Length).ConfigureAwait(false);
                await stream.WriteAsync(body, 0, body.Length).ConfigureAwait(false);
                await stream.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendPacket error: " + ex.Message);
            }
        }

        private static string SerializeToJson<T>(T obj)
        {
            var ser = new DataContractJsonSerializer(typeof(T));
            using (var ms = new MemoryStream())
            {
                ser.WriteObject(ms, obj);
                return Encoding.UTF8.GetString(ms.ToArray());
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

        private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, int offset, int count)
        {
            int total = 0;
            while (total < count)
            {
                int read = await stream.ReadAsync(buffer, offset + total, count - total).ConfigureAwait(false);
                if (read == 0) return 0;
                total += read;
            }
            return total;
        }
    }
}