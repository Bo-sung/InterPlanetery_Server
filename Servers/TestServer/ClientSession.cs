using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonLib;

namespace TestServer
{
    /// <summary>
    /// 클라이언트 세션 관리 클래스
    /// </summary>
    public class ClientSession
    {
        public string SessionId { get; private set; }
        public TcpClient TcpClient { get; private set; }
        public GameRoom? CurrentRoom { get; set; }

        private NetworkStream m_stream;
        private bool m_isConnected = true;
        private readonly object m_sendLock = new object();

        // 타임아웃 관련
        private DateTime m_lastActivityTime;
        private Timer? m_timeoutCheckTimer;
        private const int TIMEOUT_SECONDS = 30;           // 30초 동안 응답 없으면 타임아웃
        private const int TIMEOUT_CHECK_INTERVAL = 5000;  // 5초마다 체크

        // 프로토콜 핸들러
        private ProtocolHandler m_protocolHandler;

        public ClientSession(TcpClient _client)
        {
            TcpClient = _client;
            m_stream = _client.GetStream();
            SessionId = Guid.NewGuid().ToString("N").Substring(0, 8);
            m_lastActivityTime = DateTime.UtcNow;

            // 프로토콜 핸들러 초기화 및 등록
            m_protocolHandler = new ProtocolHandler();
            RegisterProtocolHandlers();

            Console.WriteLine($"[Session {SessionId}] Created");
        }

        /// <summary>
        /// 프로토콜 핸들러 등록
        /// </summary>
        private void RegisterProtocolHandlers()
        {
            m_protocolHandler.RegisterHandler(ChatProtocolType.CHAT_MESSAGE, HandleChatMessage);
            m_protocolHandler.RegisterHandler(ChatProtocolType.LEAVE_ROOM, HandleLeaveRoom);
            m_protocolHandler.RegisterHandler(ChatProtocolType.HEARTBEAT, HandleHeartbeat);
        }

        /// <summary>
        /// 세션 시작 - 메시지 수신 루프
        /// </summary>
        public async Task StartAsync()
        {
            try
            {
                // 매칭하여 룸에 입장
                GameRoom room = RoomManager.Instance.MatchPlayer(this);

                if (room == null)
                {
                    await SendErrorAsync("Failed to join room");
                    Disconnect();
                    return;
                }

                // 입장 성공 메시지 전송
                await SendJoinSuccessAsync(room);

                // 타임아웃 체크 타이머 시작
                StartTimeoutCheck();

                // 메시지 수신 루프
                await ReceiveLoop();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Session {SessionId}] Error: {e.Message}");
            }
            finally
            {
                Cleanup();
            }
        }

        /// <summary>
        /// 타임아웃 체크 시작
        /// </summary>
        private void StartTimeoutCheck()
        {
            m_timeoutCheckTimer = new Timer(CheckTimeout, null,
                TIMEOUT_CHECK_INTERVAL, TIMEOUT_CHECK_INTERVAL);
        }

        /// <summary>
        /// 타임아웃 체크 콜백
        /// </summary>
        private void CheckTimeout(object? _state)
        {
            if (!m_isConnected)
                return;

            TimeSpan timeSinceLastActivity = DateTime.UtcNow - m_lastActivityTime;

            if (timeSinceLastActivity.TotalSeconds > TIMEOUT_SECONDS)
            {
                Console.WriteLine($"[Session {SessionId}] Timeout detected. Last activity: {timeSinceLastActivity.TotalSeconds:F1}s ago");
                Disconnect();
            }
        }

        /// <summary>
        /// 마지막 활동 시간 갱신
        /// </summary>
        private void UpdateLastActivity()
        {
            m_lastActivityTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 메시지 수신 루프
        /// </summary>
        private async Task ReceiveLoop()
        {
            byte[] lengthBuffer = new byte[4];

            while (m_isConnected)
            {
                try
                {
                    // TCP 연결 상태 체크
                    if (!IsSocketConnected())
                    {
                        Console.WriteLine($"[Session {SessionId}] Socket disconnected");
                        break;
                    }

                    // 1. 메시지 길이 읽기 (4바이트)
                    int bytesRead = await m_stream.ReadAsync(lengthBuffer, 0, 4);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine($"[Session {SessionId}] Client disconnected");
                        break;
                    }

                    UpdateLastActivity(); // 활동 갱신

                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                    // 메시지 길이 검증 (비정상적으로 큰 메시지 방어)
                    if (messageLength <= 0 || messageLength > 1024 * 1024) // 1MB 제한
                    {
                        Console.WriteLine($"[Session {SessionId}] Invalid message length: {messageLength}");
                        break;
                    }

                    // 2. 전체 메시지 읽기
                    byte[] messageBuffer = new byte[messageLength + 4];
                    Array.Copy(lengthBuffer, 0, messageBuffer, 0, 4);

                    int totalRead = 0;
                    while (totalRead < messageLength)
                    {
                        bytesRead = await m_stream.ReadAsync(messageBuffer, 4 + totalRead, messageLength - totalRead);
                        if (bytesRead == 0)
                        {
                            Console.WriteLine($"[Session {SessionId}] Connection lost while reading message");
                            return;
                        }
                        totalRead += bytesRead;
                    }

                    // 3. 프로토콜 역직렬화 및 처리
                    Protocol? protocol = Protocol.Deserialize(messageBuffer);
                    if (protocol != null)
                    {
                        await HandleProtocol(protocol);
                    }
                }
                catch (IOException)
                {
                    Console.WriteLine($"[Session {SessionId}] Connection lost (IOException)");
                    break;
                }
                catch (SocketException)
                {
                    Console.WriteLine($"[Session {SessionId}] Connection lost (SocketException)");
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[Session {SessionId}] Error in receive loop: {e.Message}");
                    break;
                }
            }
        }

        /// <summary>
        /// TCP 소켓 연결 상태 체크
        /// </summary>
        private bool IsSocketConnected()
        {
            try
            {
                if (TcpClient == null || TcpClient.Client == null)
                    return false;

                Socket socket = TcpClient.Client;

                // Poll을 사용하여 연결 상태 확인
                bool part1 = socket.Poll(1000, SelectMode.SelectRead);
                bool part2 = (socket.Available == 0);

                if (part1 && part2)
                    return false; // 연결 끊김
                else
                    return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 프로토콜 처리 - 핸들러에 위임
        /// </summary>
        private async Task HandleProtocol(Protocol _protocol)
        {
            await m_protocolHandler.HandleProtocol(_protocol);
        }

        /// <summary>
        /// 하트비트 처리
        /// </summary>
        private async Task HandleHeartbeat(Protocol _protocol)
        {
            UpdateLastActivity();

            // 하트비트 응답 전송
            Protocol ackProtocol = new Protocol(ChatProtocolType.HEARTBEAT_ACK)
                .AddParam("serverTime", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            await SendAsync(ackProtocol.Serialize());
        }

        /// <summary>
        /// 채팅 메시지 처리
        /// </summary>
        private async Task HandleChatMessage(Protocol _protocol)
        {
            string message = _protocol.GetParam<string>("message");

            if (string.IsNullOrEmpty(message))
                return;

            UpdateLastActivity(); // 활동 갱신

            // "-1" 명령어 체크
            if (message.Trim() == "-1")
            {
                await HandleLeaveRoom(_protocol);
                return;
            }

            Console.WriteLine($"[Session {SessionId}] Chat: {message}");

            if (CurrentRoom != null)
            {
                await CurrentRoom.BroadcastMessage(this, message);
            }
        }

        /// <summary>
        /// 룸 나가기 처리
        /// </summary>
        private async Task HandleLeaveRoom(Protocol _protocol)
        {
            if (CurrentRoom != null)
            {
                Console.WriteLine($"[Session {SessionId}] Leaving room {CurrentRoom.RoomId}");

                CurrentRoom.RemovePlayer(this);

                // 퇴장 성공 메시지 전송
                Protocol leaveSuccessProtocol = new Protocol(ChatProtocolType.LEAVE_SUCCESS)
                    .AddParam("message", "You have left the room");

                await SendAsync(leaveSuccessProtocol.Serialize());

                // 연결 종료
                Disconnect();
            }
        }

        /// <summary>
        /// 입장 성공 메시지 전송
        /// </summary>
        private async Task SendJoinSuccessAsync(GameRoom _room)
        {
            RoomInfo roomInfo = new RoomInfo
            {
                RoomId = _room.RoomId,
                PlayerCount = _room.PlayerCount,
                MaxPlayers = _room.MaxPlayers
            };

            Protocol protocol = new Protocol(ChatProtocolType.JOIN_SUCCESS)
                .AddParam("sessionId", SessionId)
                .AddStruct("roomInfo", roomInfo)
                .AddParam("message", $"Welcome to {_room.RoomId}! Type '-1' to leave.");

            await SendAsync(protocol.Serialize());
        }

        /// <summary>
        /// 에러 메시지 전송
        /// </summary>
        private async Task SendErrorAsync(string _errorMessage)
        {
            Protocol protocol = new Protocol(ChatProtocolType.ERROR)
                .AddParam("message", _errorMessage);

            await SendAsync(protocol.Serialize());
        }

        /// <summary>
        /// 데이터 전송 (스레드 세이프)
        /// </summary>
        public async Task SendAsync(byte[] _data)
        {
            if (!m_isConnected || m_stream == null)
                return;

            try
            {
                lock (m_sendLock)
                {
                    m_stream.Write(_data, 0, _data.Length);
                    m_stream.Flush();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Session {SessionId}] Send error: {e.Message}");
                Disconnect();
            }
        }

        /// <summary>
        /// 연결 종료
        /// </summary>
        public void Disconnect()
        {
            if (!m_isConnected)
                return;

            m_isConnected = false;
            Console.WriteLine($"[Session {SessionId}] Disconnecting...");

            try
            {
                m_timeoutCheckTimer?.Dispose();
                m_stream?.Close();
                TcpClient?.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Session {SessionId}] Error during disconnect: {e.Message}");
            }
        }

        /// <summary>
        /// 정리 작업
        /// </summary>
        private void Cleanup()
        {
            // 룸에서 제거
            if (CurrentRoom != null)
            {
                CurrentRoom.RemovePlayer(this);
                CurrentRoom = null;
            }

            Disconnect();
            Console.WriteLine($"[Session {SessionId}] Cleaned up");
        }
    }
}