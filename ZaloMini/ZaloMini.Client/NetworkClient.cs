using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ZaloMini.Client
{
    public class NetworkClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        public bool IsConnected = false;

        public event Action<string> OnMessageReceived;
        public event Action OnDisconnected;

        // Protected methods để MockNetworkClient có thể dùng
        protected void RaiseMessageReceived(string message)
        {
            OnMessageReceived?.Invoke(message);
        }

        protected void RaiseDisconnected()
        {
            OnDisconnected?.Invoke();
        }

        public bool Connect(string ip, int port)
        {
            try
            {
                _client = new TcpClient();
                _client.ReceiveTimeout = 5000; // 5 giây timeout
                _client.SendTimeout = 5000;
                _client.Connect(ip, port);
                _stream = _client.GetStream();
                IsConnected = true;

                Thread t = new Thread(ReceiveLoop);
                t.IsBackground = true;
                t.Start();
                return true;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                Console.WriteLine("❌ Lỗi kết nối: " + ex.Message);
                return false;
            }
        }

        public bool Login(string username, string password)
        {
            try
            {
                Send($"LOGIN|{username}|{password}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi gửi LOGIN: " + ex.Message);
                return false;
            }
        }

        public bool Register(string username, string password)
        {
            try
            {
                Send($"REGISTER|{username}|{password}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi gửi REGISTER: " + ex.Message);
                return false;
            }
        }

        public void Send(string message)
        {
            if (!IsConnected || _stream == null) return;
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                _stream.Write(data, 0, data.Length);
                _stream.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi gửi message: " + ex.Message);
            }
        }

        public void SendBytes(byte[] data, int length)
        {
            if (!IsConnected || _stream == null) return;
            try
            {
                _stream.Write(data, 0, length);
                _stream.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi gửi bytes: " + ex.Message);
            }
        }

        private void ReceiveLoop()
        {
            byte[] buffer = new byte[4096];
            while (IsConnected)
            {
                try
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    OnMessageReceived?.Invoke(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Lỗi nhận dữ liệu: " + ex.Message);
                    break;
                }
            }
            IsConnected = false;
            OnDisconnected?.Invoke();
        }

        public void Disconnect()
        {
            IsConnected = false;
            if (_stream != null)
            {
                try { _stream.Close(); }
                catch { }
            }
            if (_client != null)
            {
                try { _client.Close(); }
                catch { }
            }
        }
    }
}