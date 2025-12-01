using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonLib;
using ProtoType = CommonLib.ProtocolType;

namespace ChatClientWPF.Models
{
    public class ChatClientModel
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private bool _isConnected;
        private string? _currentRoomId;
        private string? _userId;
        private string? _userName;
        private CancellationTokenSource? _cts;

        // Events
        public event Action<string>? OnError;
        public event Action<string>? OnInfo;
        
        // Auth Events
        public event Action<bool, string>? OnLoginResult; // success, message
        public event Action<bool, string>? OnRegisterResult; // success, message

        // Lobby Events
        public event Action<int, int, RoomInfo[]>? OnLobbyJoined; // roomCount, page, roomList
        public event Action<RoomInfo[]>? OnRoomListRefreshed;
        public event Action<string>? OnRoomCreated; // roomId

        // Room Events
        public event Action<RoomInfo, int>? OnRoomJoined; // roomInfo, chatChannelId
        public event Action<int, string, int>? OnUserJoinedRoom; // userId, userName, playerCount
        public event Action<int, int>? OnUserLeftRoom; // userId, playerCount
        public event Action<RoomInfo>? OnRoomInfoChanged;
        public event Action<string, string>? OnRoomClosed; // roomId, reason
        public event Action<RoomInfo, WaittingRoomUser[]>? OnRoomInfoRefreshed; // roomInfo, users

        // Chat Events
        public event Action<ChatMessage>? OnChatMessageReceived;

        // Exposed properties
        public bool IsConnected => _isConnected;
        public bool IsInRoom => !string.IsNullOrEmpty(_currentRoomId);
        public string? CurrentRoomId => _currentRoomId;
        public string? UserId => _userId;
        public string? UserName => _userName;

        public async Task<bool> ConnectAsync(string ip, int port)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ip, port);
                _stream = _client.GetStream();
                _isConnected = true;
                _cts = new CancellationTokenSource();

                // Start receive loop
                _ = ReceiveLoop(_cts.Token);
                // Start heartbeat loop
                _ = HeartbeatLoop(_cts.Token);

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
            _isConnected = false;
            _cts?.Cancel();
            _stream?.Close();
            _client?.Close();
            _client = null;
            _stream = null;
            _currentRoomId = null;
            _userId = null;
            _userName = null;
        }

        // ---------------------------------------------------------------------
        // Send Methods
        // ---------------------------------------------------------------------

        private async Task SendAsync(Protocol protocol)
        {
            if (!_isConnected || _stream == null) return;
            try
            {
                byte[] data = protocol.Serialize();
                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Send failed: {ex.Message}");
                Disconnect();
            }
        }

        public async Task LoginAsync(string username, string password)
        {
            _userName = username;
            var protocol = new Protocol(ProtoType.REQUEST_LOGIN)
                .AddParam("username", username)
                .AddParam("password", password);
            await SendAsync(protocol);
        }

        public async Task RegisterAsync(string username, string password)
        {
            var protocol = new Protocol(ProtoType.REQUEST_REGISTER)
                .AddParam("username", username)
                .AddParam("password", password);
            await SendAsync(protocol);
        }

        public async Task JoinLobbyAsync(int page = 0)
        {
            var protocol = new Protocol(ProtoType.REQUEST_JOIN_LOBBY)
                .AddParam("Page", page);
            await SendAsync(protocol);
        }

        public async Task RefreshRoomListAsync()
        {
            var protocol = new Protocol(ProtoType.REFRESH_LOBBY);
            await SendAsync(protocol);
        }

        public async Task CreateRoomAsync(string roomName, int mapId, bool isPrivate)
        {
            var protocol = new Protocol(ProtoType.REQUEST_CREATE_ROOM)
                .AddParam("roomName", roomName)
                .AddParam("mapId", mapId)
                .AddParam("isPrivate", isPrivate);
            await SendAsync(protocol);
        }

        public async Task JoinRoomAsync(string roomId, int slot = 0)
        {
            // userId is handled by session on server, but protocol might require it
            var protocol = new Protocol(ProtoType.REQUEST_JOIN_ROOM)
                .AddParam("userId", 0) 
                .AddParam("roomId", roomId)
                .AddParam("slot", slot);
            await SendAsync(protocol);
        }

        public async Task LeaveRoomAsync()
        {
            var protocol = new Protocol(ProtoType.REQUEST_LEFT_ROOM);
            await SendAsync(protocol);
            _currentRoomId = null;
        }

        public async Task ToggleReadyAsync()
        {
            var protocol = new Protocol(ProtoType.REQUEST_READY);
            await SendAsync(protocol);
        }

        public async Task RefreshRoomInfoAsync()
        {
            var protocol = new Protocol(ProtoType.REQUEST_REFRESH_JOINED_ROOM_INFO);
            await SendAsync(protocol);
        }

        public async Task SendChatMessageAsync(string message, int type, int channelId)
        {
            var chatMsg = new ChatMessage
            {
                SenderId = _userName ?? "Unknown",
                Message = message,
                MessageType = type,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var protocol = new Protocol(ProtoType.CHAT_MESSAGE)
                .AddParam("type", type)
                .AddParam("channelId", channelId)
                .AddStruct("chatMessage", chatMsg);
            
            await SendAsync(protocol);
        }

        // ---------------------------------------------------------------------
        // Receive Loop
        // ---------------------------------------------------------------------

        private async Task ReceiveLoop(CancellationToken token)
        {
            byte[] lengthBuffer = new byte[4];

            while (_isConnected && _stream != null && !token.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await _stream.ReadAsync(lengthBuffer, 0, 4, token);
                    if (bytesRead == 0) break;

                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                    if (messageLength <= 0 || messageLength > 1024 * 1024 * 10) // 10MB limit
                    {
                        OnError?.Invoke($"Invalid message length: {messageLength}");
                        break;
                    }

                    byte[] messageBuffer = new byte[messageLength + 4];
                    Array.Copy(lengthBuffer, 0, messageBuffer, 0, 4);

                    int totalRead = 0;
                    while (totalRead < messageLength)
                    {
                        bytesRead = await _stream.ReadAsync(messageBuffer, 4 + totalRead, messageLength - totalRead, token);
                        if (bytesRead == 0) break;
                        totalRead += bytesRead;
                    }

                    var protocol = Protocol.Deserialize(messageBuffer);
                    if (protocol != null)
                    {
                        HandleProtocol(protocol);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    OnError?.Invoke($"Receive error: {ex.Message}");
                    break;
                }
            }
            Disconnect();
            OnError?.Invoke("Disconnected from server.");
        }

        private async Task HeartbeatLoop(CancellationToken token)
        {
            while (_isConnected && !token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(10000, token);
                    var protocol = new Protocol(ProtoType.HEARTBEAT);
                    await SendAsync(protocol);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception) { break; }
            }
        }

        // ---------------------------------------------------------------------
        // Protocol Handling
        // ---------------------------------------------------------------------

        private void HandleProtocol(Protocol protocol)
        {
            // Dispatch to UI thread if necessary, but Model usually just fires events on whatever thread
            // ViewModels should handle marshalling to UI thread.

            switch (protocol.Type)
            {
                case ProtoType.RESPONSE:
                    HandleResponse(protocol);
                    break;
                case ProtoType.USER_JOINED:
                    {
                        int uId = protocol.GetParam<int>("userId");
                        string uName = protocol.GetParam<string>("userName");
                        int pCount = protocol.GetParam<int>("playerCount");
                        OnUserJoinedRoom?.Invoke(uId, uName, pCount);
                    }
                    break;
                case ProtoType.USER_LEFT:
                    {
                        int uId = protocol.GetParam<int>("userId");
                        int pCount = protocol.GetParam<int>("playerCount");
                        OnUserLeftRoom?.Invoke(uId, pCount);
                    }
                    break;
                case ProtoType.ROOM_INFO_CHANGED:
                    {
                        var rInfo = protocol.GetStruct<RoomInfo>("roomInfo");
                        OnRoomInfoChanged?.Invoke(rInfo);
                    }
                    break;
                case ProtoType.ROOM_CLOSED:
                    {
                        string rId = protocol.GetParam<string>("roomId");
                        string reason = protocol.GetParam<string>("reason");
                        OnRoomClosed?.Invoke(rId, reason);
                        _currentRoomId = null;
                    }
                    break;
                case ProtoType.BRODCAST_CHAT_MESSAGE:
                    {
                        var chatMsg = protocol.GetStruct<ChatMessage>("chatMessage");
                        OnChatMessageReceived?.Invoke(chatMsg);
                    }
                    break;
            }
        }

        private void HandleResponse(Protocol protocol)
        {
            int protoId = protocol.GetParam<int>("protoId");
            byte statusByte = protocol.GetParam<byte>("status");
            StateCode status = (StateCode)statusByte;
            string message = protocol.GetParam<string>("message");

            if (status != StateCode.SUCCESS)
            {
                // Handle errors
                if (protoId == ProtoType.REQUEST_LOGIN) OnLoginResult?.Invoke(false, message);
                else if (protoId == ProtoType.REQUEST_REGISTER) OnRegisterResult?.Invoke(false, message);
                else OnError?.Invoke($"Request {protoId} failed: {message}");
                return;
            }

            switch (protoId)
            {
                case ProtoType.REQUEST_LOGIN:
                    _userId = protocol.GetParam<string>("sessionId"); // SessionId as UserId for now? Or check logic
                    OnLoginResult?.Invoke(true, "Login successful");
                    break;
                case ProtoType.REQUEST_REGISTER:
                    OnRegisterResult?.Invoke(true, "Registration successful");
                    break;
                case ProtoType.REQUEST_JOIN_LOBBY:
                    {
                        int roomCount = protocol.GetParam<int>("roomCount");
                        int page = protocol.GetParam<int>("page");
                        var roomList = protocol.GetParam<RoomInfo[]>("roomList");
                        OnLobbyJoined?.Invoke(roomCount, page, roomList ?? new RoomInfo[0]);
                    }
                    break;
                case ProtoType.REFRESH_LOBBY:
                    {
                        var roomList = protocol.GetParam<RoomInfo[]>("roomList");
                        OnRoomListRefreshed?.Invoke(roomList ?? new RoomInfo[0]);
                    }
                    break;
                case ProtoType.REQUEST_CREATE_ROOM:
                    {
                        string roomId = protocol.GetParam<string>("roomId");
                        _currentRoomId = roomId;
                        OnRoomCreated?.Invoke(roomId);
                    }
                    break;
                case ProtoType.REQUEST_JOIN_ROOM:
                    {
                        var roomInfo = protocol.GetStruct<RoomInfo>("roominfo");
                        int chatChannelId = protocol.GetParam<int>("chatChannelId");
                        _currentRoomId = roomInfo.RoomId;
                        OnRoomJoined?.Invoke(roomInfo, chatChannelId);
                    }
                    break;
                case ProtoType.REQUEST_LEFT_ROOM:
                    _currentRoomId = null;
                    break;
                case ProtoType.REQUEST_REFRESH_JOINED_ROOM_INFO:
                    {
                        var roomInfo = protocol.GetStruct<RoomInfo>("roomInfo");
                        var users = protocol.GetParam<WaittingRoomUser[]>("users");
                        OnRoomInfoRefreshed?.Invoke(roomInfo, users ?? new WaittingRoomUser[0]);
                    }
                    break;
            }
        }
    }
}