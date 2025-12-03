using System;
using System.Threading.Tasks;
using System.Windows;
using ChatClientWPF.Utils;

namespace CommonLib
{
    /// <summary>
    /// 클라이언트에서 서버와의 통신을 관리하는 싱글톤 핸들러.
    /// NetworkManager를 활용하여 네트워크 통신을 처리합니다.
    /// </summary>
    public sealed class ClientServerHandler
    {
        // --- 싱글톤 ---
        private static ClientServerHandler? _instance;
        private static readonly object _lock = new object();

        public static ClientServerHandler Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ClientServerHandler();
                        }
                    }
                }
                return _instance;
            }
        }

        // --- 생성자 (자동 초기화) ---
        private ClientServerHandler()
        {
            OnInitialize();
        }

        // --- NetworkManager 인스턴스 ---
        private NetworkManager networkManager = new NetworkManager();
        private ProtocolHandler protocolHandler = new ProtocolHandler();

        // --- UI 관련 프로토콜 목록 ---
        private readonly int[] uiProtocols = new int[]
        {
            (int)ProtocolType.RESPONSE,
            (int)ProtocolType.BRODCAST_SYSTEM,
            (int)ProtocolType.BRODCAST_CHAT_MESSAGE,
            (int)ProtocolType.USER_JOINED,
            (int)ProtocolType.USER_LEFT,
            (int)ProtocolType.ROOM_INFO_CHANGED,
            (int)ProtocolType.ROOM_CLOSED
        };

        // --- 속성 ---
        public bool IsConnected => networkManager?.Config.IsConnected ?? false;

        // --- 초기화 ---
        public void OnInitialize()
        {
            // 이벤트 등록
            networkManager.ConnectionChanged += OnConnectionChanged;
            networkManager.ErrorOccurred += OnErrorOccurred;

            // 기본 프로토콜 핸들러 등록
            RegisterDefaultHandlers();

            Logger.Log("[ClientServerHandler] Initialized with NetworkManager.");

            SetServerAllReady(true);
        }

        // --- 연결 관리 ---

        /// <summary>
        /// 서버에 연결
        /// </summary>
        public async Task ConnectAsync(string ip, int port)
        {
            try
            {
                await networkManager.ConnectAsync(ip, port);
            }
            catch (Exception e)
            {
                Logger.LogError($"[ClientServerHandler] Connection failed: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 연결 종료
        /// </summary>
        public void Disconnect()
        {
            networkManager?.Disconnect();
        }

        // --- 프로토콜 전송 ---

        /// <summary>
        /// 비동기 프로토콜 전송 및 응답 대기 (통합 구조)
        /// </summary>
        public async Task<NetworkResponse> AsyncSend(Protocol protocol)
        {
            if (networkManager == null)
                throw new InvalidOperationException("NetworkManager not initialized");

            return await networkManager.SendAsync(protocol);
        }

        /// <summary>
        /// 기존 SendAndWaitAsync 메서드 (하위 호환성)
        /// </summary>
        public async Task<Protocol?> SendAndWaitAsync(Protocol request)
        {
            try
            {
                NetworkResponse response = await networkManager.SendAsync(request);

                // NetworkResponse를 Protocol로 변환 (임시 구현)
                var responseProtocol = new Protocol((int)ProtocolType.RESPONSE);
                responseProtocol.AddParam("status", (int)response.resultCode);
                responseProtocol.AddParam("success", response.isSuccess);

                return responseProtocol;
            }
            catch (Exception e)
            {
                Logger.LogError($"[ClientServerHandler] SendAndWaitAsync error: {e.Message}");
                return null;
            }
        }

        // --- 프로토콜 핸들러 관리 ---

        /// <summary>
        /// 프로토콜 핸들러 등록
        /// </summary>
        public void RegisterHandler(int protocolType, ProtocolHandler.ProtocolHandlerDelegate handler)
        {
            // ProtocolHandler용 핸들러를 NetworkManager용으로 래핑
            networkManager.RegisterHandler(protocolType, async (protocol) =>
            {
                if (IsUIProtocol(protocolType))
                {
                    // UI 관련 프로토콜은 WPF Dispatcher를 통해 메인 스레드에서 실행
                    if (Application.Current != null)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            try
                            {
                                await handler(protocol);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError($"[ClientServerHandler] UI 핸들러 실행 중 오류: {ex.Message}");
                            }
                        });
                    }
                    else
                    {
                        // Dispatcher가 없으면 직접 실행
                        await handler(protocol);
                    }
                }
                else
                {
                    // 백그라운드에서 실행
                    await handler(protocol);
                }
            });

            // 기존 ProtocolHandler에도 등록 (하위 호환성)
            protocolHandler.RegisterHandler(protocolType, handler);
        }

        /// <summary>
        /// 프로토콜 핸들러 제거
        /// </summary>
        public void UnregisterHandler(int protocolType)
        {
            networkManager.UnregisterHandler(protocolType);
            protocolHandler.UnregisterHandler(protocolType);
        }

        /// <summary>
        /// 동적 프로토콜 등록 (기존 호환성)
        /// </summary>
        public void RegisterProto(int protocolType, ProtocolHandler.ProtocolHandlerDelegate handler)
        {
            RegisterHandler(protocolType, handler);
        }

        /// <summary>
        /// 동적 프로토콜 제거 (기존 호환성)
        /// </summary>
        public void UnRegisterProto(int protocolType)
        {
            UnregisterHandler(protocolType);
        }

        // --- 상태 관리 ---

        /// <summary>
        /// 재접속 상태 확인
        /// </summary>
        public bool IsReconnecting()
        {
            return networkManager?.Config.IsReconnecting ?? false;
        }

        /// <summary>
        /// 재연결 가능 여부 확인
        /// </summary>
        public bool CanReconnect()
        {
            if (networkManager?.Config == null) return false;
            return !networkManager.Config.IsTryGoToTitle && !networkManager.Config.IsTryApplicationQuit;
        }

        /// <summary>
        /// 서버 준비 상태 확인
        /// </summary>
        public bool IsServerAllReady()
        {
            return networkManager?.Config.IsServerAllReady ?? false;
        }

        /// <summary>
        /// 서버 준비 상태 설정
        /// </summary>
        public void SetServerAllReady(bool ready)
        {
            if (networkManager?.Config != null)
                networkManager.Config.IsServerAllReady = ready;
        }

        /// <summary>
        /// 타이틀 이동 상태 설정
        /// </summary>
        public void SetTryGoToTitle(bool trying)
        {
            if (networkManager?.Config != null)
                networkManager.Config.IsTryGoToTitle = trying;
        }

        /// <summary>
        /// 애플리케이션 종료 상태 설정
        /// </summary>
        public void SetTryApplicationQuit(bool trying)
        {
            if (networkManager?.Config != null)
                networkManager.Config.IsTryApplicationQuit = trying;
        }

        /// <summary>
        /// 마지막 활동 시간 갱신
        /// </summary>
        public void UpdateLastActivity()
        {
            networkManager?.UpdateLastActivity();
        }

        /// <summary>
        /// 재접속 시도
        /// </summary>
        public async Task AsyncReconnect()
        {
            if (networkManager?.Config == null) return;

            networkManager.Config.IsReconnecting = true;
            try
            {
                Logger.Log("[ClientServerHandler] 재접속 시도...");
                networkManager.Disconnect();
                await Task.Delay(1000);
                // TODO: 이전 연결 정보로 재접속
                Logger.Log("[ClientServerHandler] 재접속 완료");
            }
            finally
            {
                networkManager.Config.IsReconnecting = false;
            }
        }

        // --- 내부 메서드 ---

        /// <summary>
        /// 기본 프로토콜 핸들러 등록
        /// </summary>
        private void RegisterDefaultHandlers()
        {
            // 하트비트 ACK 처리
            RegisterHandler((int)ProtocolType.HEARTBEAT_ACK, HandleHeartbeatAck);

            // 일반 응답 처리
            RegisterHandler((int)ProtocolType.RESPONSE, HandleCommonResponse);

            // 브로드캐스트 메시지 처리
            RegisterHandler((int)ProtocolType.BRODCAST_CHAT_MESSAGE, HandleBroadcastChatMessage);
            RegisterHandler((int)ProtocolType.BRODCAST_SYSTEM, HandleBroadcastSystem);

            // 유저 관련 이벤트 처리
            RegisterHandler((int)ProtocolType.USER_JOINED, HandleUserJoined);
            RegisterHandler((int)ProtocolType.USER_LEFT, HandleUserLeft);
        }

        /// <summary>
        /// UI 프로토콜 여부 확인
        /// </summary>
        private bool IsUIProtocol(int protocolType)
        {
            return Array.IndexOf(uiProtocols, protocolType) >= 0;
        }

        // --- 이벤트 핸들러 ---

        private void OnConnectionChanged(bool connected, string message)
        {
            Logger.Log($"[ClientServerHandler] Connection changed: {connected}, {message}");
        }

        private void OnErrorOccurred(string error)
        {
            Logger.LogError($"[ClientServerHandler] Network error: {error}");
        }

        // --- 기본 프로토콜 핸들러들 ---

        private async Task HandleHeartbeatAck(Protocol protocol)
        {
            networkManager?.UpdateLastActivity();
            Logger.Log("[ClientServerHandler] Received HEARTBEAT_ACK. Activity updated.");
            await Task.CompletedTask;
        }

        private async Task HandleCommonResponse(Protocol protocol)
        {
            Logger.Log("[ClientServerHandler] Received RESPONSE on Main Thread.");
            await Task.CompletedTask;
        }

        private async Task HandleBroadcastChatMessage(Protocol protocol)
        {
            Logger.Log("[ClientServerHandler] Received Chat Message Broadcast. Displaying in UI.");
            await Task.CompletedTask;
        }

        private async Task HandleBroadcastSystem(Protocol protocol)
        {
            Logger.Log("[ClientServerHandler] Received System Broadcast. Displaying in UI.");
            await Task.CompletedTask;
        }

        private async Task HandleUserJoined(Protocol protocol)
        {
            Logger.Log("[ClientServerHandler] Received User Joined. Updating lobby list.");
            await Task.CompletedTask;
        }

        private async Task HandleUserLeft(Protocol protocol)
        {
            Logger.Log("[ClientServerHandler] Received User Left. Updating lobby list.");
            await Task.CompletedTask;
        }
    }
}
