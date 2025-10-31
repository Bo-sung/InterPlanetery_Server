using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ChatClientWPF.Models
{
    public class NetworkClient
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private bool _isRunning;

        public bool IsConnected => _client?.Connected ?? false;

        public event Action? OnConnected;
        public event Action? OnDisconnected;
        public event Action<byte[]>? OnDataReceived;
        public event Action<string>? OnError;

        public async Task<bool> ConnectAsync(string host, int port)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(host, port);
                _stream = _client.GetStream();
                _isRunning = true;

                OnConnected?.Invoke();
                Task.Run(ReceiveLoop);
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Connection failed: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            _isRunning = false;
            _stream?.Close();
            _client?.Close();
        }

        public async Task SendAsync(byte[] data)
        {
            if (_stream == null || !IsConnected)
            {
                OnError?.Invoke("Not connected to server.");
                return;
            }

            try
            {
                await _stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Send error: {ex.Message}");
                Disconnect();
            }
        }

        private async Task ReceiveLoop()
        {
            byte[] sizeBuffer = new byte[4];

            while (_isRunning && _stream != null)
            {
                try
                {
                    int bytesRead = await _stream.ReadAsync(sizeBuffer, 0, 4);
                    if (bytesRead != 4)
                    {
                        Disconnect();
                        break;
                    }

                    int packetSize = BitConverter.ToInt32(sizeBuffer, 0);

                    byte[] packetBuffer = new byte[packetSize];
                    
                    int totalRead = 0;
                    while (totalRead < packetSize)
                    {
                        bytesRead = await _stream.ReadAsync(packetBuffer, totalRead, packetSize - totalRead);
                        if (bytesRead == 0)
                        {
                            Disconnect();
                            return;
                        }
                        totalRead += bytesRead;
                    }

                    // Prepend the size buffer to the packet buffer to reconstruct the full message
                    byte[] fullMessage = new byte[packetSize + 4];
                    Buffer.BlockCopy(sizeBuffer, 0, fullMessage, 0, 4);
                    Buffer.BlockCopy(packetBuffer, 0, fullMessage, 4, packetSize);

                    OnDataReceived?.Invoke(fullMessage);
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        OnError?.Invoke($"Receive error: {ex.Message}");
                        Disconnect();
                    }
                    break;
                }
            }
            
            OnDisconnected?.Invoke();
        }
    }
}
