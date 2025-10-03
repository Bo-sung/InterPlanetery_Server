using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InterPlanetary.Network
{
    /// <summary>
    /// 채팅 네트워크 테스트 컴포넌트
    /// 서버와의 연결, 채팅 메시지 송수신을 테스트합니다
    /// </summary>
    public class ChatNetworkTester : MonoBehaviour
    {
        [Header("서버 설정")]
        [SerializeField] private string m_serverHost = "127.0.0.1";
        [SerializeField] private int m_serverPort = 7777;

        [Header("Inspector 테스트")]
        [SerializeField] private bool m_autoConnectOnStart = false;
        [SerializeField] private string m_testMessage = "Test Message";

        [Header("Inspector 버튼 (Play Mode Only)")]
        [SerializeField] private bool m_btnConnect = false;
        [SerializeField] private bool m_btnDisconnect = false;
        [SerializeField] private bool m_btnJoinRoom = false;
        [SerializeField] private bool m_btnLeaveRoom = false;
        [SerializeField] private bool m_btnSendTestMessage = false;

        [Header("UI 참조")]
        [SerializeField] private TMP_InputField m_hostInput;
        [SerializeField] private TMP_InputField m_portInput;
        [SerializeField] private Button m_connectButton;
        [SerializeField] private Button m_disconnectButton;
        [SerializeField] private Button m_joinRoomButton;
        [SerializeField] private Button m_leaveRoomButton;
        [SerializeField] private TMP_InputField m_chatInput;
        [SerializeField] private Button m_sendButton;
        [SerializeField] private TMP_Text m_chatLogText;
        [SerializeField] private TMP_Text m_statusText;

        private NetworkClient m_client;
        private List<string> m_chatLog = new List<string>();
        private bool m_isInRoom = false;

        private const int MAX_CHAT_LINES = 50;

        private static int s_instanceCount = 0;

        void Awake()
        {
            s_instanceCount++;
            if (s_instanceCount > 1)
            {
                Debug.LogWarning($"[ChatNetworkTester] Multiple instances detected! Count: {s_instanceCount}");
            }

            // 클라이언트 초기화
            m_client = new NetworkClient();
            m_client.OnProtocolReceived += OnProtocolReceived;
            m_client.OnDisconnected += OnDisconnected;
            m_client.OnError += OnError;

            // UI 이벤트 등록
            if (m_connectButton != null)
                m_connectButton.onClick.AddListener(OnConnectButtonClicked);

            if (m_disconnectButton != null)
                m_disconnectButton.onClick.AddListener(OnDisconnectButtonClicked);

            if (m_joinRoomButton != null)
                m_joinRoomButton.onClick.AddListener(OnJoinRoomButtonClicked);

            if (m_leaveRoomButton != null)
                m_leaveRoomButton.onClick.AddListener(OnLeaveRoomButtonClicked);

            if (m_sendButton != null)
                m_sendButton.onClick.AddListener(OnSendButtonClicked);

            if (m_chatInput != null)
                m_chatInput.onSubmit.AddListener((_) => OnSendButtonClicked());

            // 초기 UI 상태
            UpdateUI();
        }

        async void Start()
        {
            if (m_autoConnectOnStart)
            {
                await m_client.ConnectAsync(m_serverHost, m_serverPort);
            }
        }

        void Update()
        {
            // Inspector 버튼 처리
            if (m_btnConnect)
            {
                m_btnConnect = false;
                OnConnectButtonClicked();
            }

            if (m_btnDisconnect)
            {
                m_btnDisconnect = false;
                OnDisconnectButtonClicked();
            }

            if (m_btnJoinRoom)
            {
                m_btnJoinRoom = false;
                OnJoinRoomButtonClicked();
            }

            if (m_btnLeaveRoom)
            {
                m_btnLeaveRoom = false;
                OnLeaveRoomButtonClicked();
            }

            if (m_btnSendTestMessage)
            {
                m_btnSendTestMessage = false;
                SendTestMessage();
            }
        }

        void OnDestroy()
        {
            s_instanceCount--;
            m_client?.Disconnect();
        }

        #region UI 이벤트

        [ContextMenu("Connect")]
        private async void OnConnectButtonClicked()
        {
            string host = m_hostInput != null ? m_hostInput.text : m_serverHost;
            int port = m_portInput != null && int.TryParse(m_portInput.text, out int p) ? p : m_serverPort;

            AddLog($"<color=yellow>서버에 연결 중... ({host}:{port})</color>");
            UpdateStatus("연결 중...");

            bool success = await m_client.ConnectAsync(host, port);
            if (success)
            {
                AddLog("<color=green>서버에 연결되었습니다!</color>");
                UpdateStatus("연결됨");
            }
            else
            {
                AddLog("<color=red>서버 연결 실패</color>");
                UpdateStatus("연결 끊김");
            }

            UpdateUI();
        }

        [ContextMenu("Disconnect")]
        private void OnDisconnectButtonClicked()
        {
            m_client.Disconnect();
            m_isInRoom = false;
            AddLog("<color=yellow>서버와의 연결을 종료했습니다</color>");
            UpdateStatus("연결 끊김");
            UpdateUI();
        }

        [ContextMenu("Join Room")]
        private async void OnJoinRoomButtonClicked()
        {
            if (!m_client.IsConnected)
            {
                AddLog("<color=red>서버에 연결되지 않았습니다</color>");
                return;
            }

            Protocol protocol = new Protocol(ChatProtocolType.JOIN_ROOM);
            bool success = await m_client.SendAsync(protocol);

            if (success)
            {
                AddLog("<color=yellow>룸 입장 요청을 전송했습니다...</color>");
            }
        }

        [ContextMenu("Leave Room")]
        private async void OnLeaveRoomButtonClicked()
        {
            if (!m_client.IsConnected || !m_isInRoom)
            {
                AddLog("<color=red>룸에 입장하지 않았습니다</color>");
                return;
            }

            Protocol protocol = new Protocol(ChatProtocolType.LEAVE_ROOM);
            bool success = await m_client.SendAsync(protocol);

            if (success)
            {
                AddLog("<color=yellow>룸 퇴장 요청을 전송했습니다...</color>");
            }
        }

        private async void OnSendButtonClicked()
        {
            if (!m_client.IsConnected || !m_isInRoom)
            {
                AddLog("<color=red>룸에 입장하지 않았습니다</color>");
                return;
            }

            if (m_chatInput == null || string.IsNullOrWhiteSpace(m_chatInput.text))
                return;

            string message = m_chatInput.text.Trim();
            Protocol protocol = new Protocol(ChatProtocolType.CHAT_MESSAGE)
                .AddParam("message", message);

            bool success = await m_client.SendAsync(protocol);

            if (success)
            {
                m_chatInput.text = "";
                m_chatInput.ActivateInputField();
            }
        }

        [ContextMenu("Send Test Message")]
        private async void SendTestMessage()
        {
            if (!m_client.IsConnected || !m_isInRoom)
            {
                Debug.LogWarning("[ChatNetworkTester] Not connected or not in room");
                AddLog("<color=red>룸에 입장하지 않았습니다</color>");
                return;
            }

            if (string.IsNullOrWhiteSpace(m_testMessage))
            {
                Debug.LogWarning("[ChatNetworkTester] Test message is empty");
                return;
            }

            Protocol protocol = new Protocol(ChatProtocolType.CHAT_MESSAGE)
                .AddParam("message", m_testMessage);

            bool success = await m_client.SendAsync(protocol);

            if (success)
            {
                Debug.Log($"[ChatNetworkTester] Sent test message: {m_testMessage}");
                AddLog($"<color=cyan>[테스트] {m_testMessage} 전송됨</color>");
            }
        }

        #endregion

        #region 네트워크 이벤트

        private void OnProtocolReceived(Protocol protocol)
        {
            switch (protocol.Type)
            {
                case ChatProtocolType.JOIN_SUCCESS:
                    HandleJoinSuccess(protocol);
                    break;

                case ChatProtocolType.JOIN_FAILED:
                    HandleJoinFailed(protocol);
                    break;

                case ChatProtocolType.LEAVE_SUCCESS:
                    HandleLeaveSuccess(protocol);
                    break;

                case ChatProtocolType.USER_JOINED:
                    HandleUserJoined(protocol);
                    break;

                case ChatProtocolType.USER_LEFT:
                    HandleUserLeft(protocol);
                    break;

                case ChatProtocolType.CHAT_BROADCAST:
                    HandleChatBroadcast(protocol);
                    break;

                case ChatProtocolType.ROOM_CLOSED:
                    HandleRoomClosed(protocol);
                    break;

                case ChatProtocolType.HEARTBEAT_ACK:
                    // 하트비트 응답 (무시)
                    break;

                case ChatProtocolType.ERROR:
                    HandleError(protocol);
                    break;

                default:
                    Debug.LogWarning($"[ChatNetworkTester] Unknown protocol type: {protocol.Type}");
                    break;
            }
        }

        private void HandleJoinSuccess(Protocol protocol)
        {
            string roomId = protocol.GetParam<string>("roomId");
            int playerCount = protocol.GetParam<int>("playerCount");

            m_isInRoom = true;
            AddLog($"<color=green>룸 입장 성공! (RoomID: {roomId}, 플레이어: {playerCount})</color>");
            UpdateStatus($"룸: {roomId}");
            UpdateUI();
        }

        private void HandleJoinFailed(Protocol protocol)
        {
            string reason = protocol.GetParam<string>("reason", "Unknown error");
            m_isInRoom = false;
            AddLog($"<color=red>룸 입장 실패: {reason}</color>");
            UpdateUI();
        }

        private void HandleLeaveSuccess(Protocol protocol)
        {
            m_isInRoom = false;
            AddLog("<color=yellow>룸에서 퇴장했습니다</color>");
            UpdateStatus("연결됨");
            UpdateUI();
        }

        private void HandleUserJoined(Protocol protocol)
        {
            string userId = protocol.GetParam<string>("userId");
            int playerCount = protocol.GetParam<int>("playerCount");
            AddLog($"<color=cyan>[알림] {userId}님이 입장했습니다 (플레이어: {playerCount})</color>");
        }

        private void HandleUserLeft(Protocol protocol)
        {
            string userId = protocol.GetParam<string>("userId");
            int playerCount = protocol.GetParam<int>("playerCount");
            AddLog($"<color=cyan>[알림] {userId}님이 퇴장했습니다 (플레이어: {playerCount})</color>");
        }

        private void HandleChatBroadcast(Protocol protocol)
        {
            ChatMessage chatMsg = protocol.GetStruct<ChatMessage>("chatMessage");
            AddLog($"<b>[{chatMsg.SenderId}]</b> {chatMsg.Message}");
        }

        private void HandleRoomClosed(Protocol protocol)
        {
            string roomId = protocol.GetParam<string>("roomId");
            string reason = protocol.GetParam<string>("reason", "Unknown reason");

            m_isInRoom = false;
            AddLog($"<color=red>[알림] 룸이 종료되었습니다: {reason}</color>");
            UpdateStatus("연결됨");
            UpdateUI();
        }

        private void HandleError(Protocol protocol)
        {
            string errorMsg = protocol.GetParam<string>("message", "Unknown error");
            AddLog($"<color=red>[에러] {errorMsg}</color>");
        }

        private void OnDisconnected()
        {
            m_isInRoom = false;
            AddLog("<color=red>서버와의 연결이 끊어졌습니다</color>");
            UpdateStatus("연결 끊김");
            UpdateUI();
        }

        private void OnError(string errorMsg)
        {
            AddLog($"<color=red>[에러] {errorMsg}</color>");
        }

        #endregion

        #region UI 업데이트

        private void UpdateUI()
        {
            bool connected = m_client.IsConnected;

            if (m_connectButton != null)
                m_connectButton.interactable = !connected;

            if (m_disconnectButton != null)
                m_disconnectButton.interactable = connected;

            if (m_joinRoomButton != null)
                m_joinRoomButton.interactable = connected && !m_isInRoom;

            if (m_leaveRoomButton != null)
                m_leaveRoomButton.interactable = connected && m_isInRoom;

            if (m_sendButton != null)
                m_sendButton.interactable = connected && m_isInRoom;

            if (m_chatInput != null)
                m_chatInput.interactable = connected && m_isInRoom;

            if (m_hostInput != null)
                m_hostInput.interactable = !connected;

            if (m_portInput != null)
                m_portInput.interactable = !connected;
        }

        private void AddLog(string message)
        {
            m_chatLog.Add(message);

            // 최대 라인 수 제한
            if (m_chatLog.Count > MAX_CHAT_LINES)
            {
                m_chatLog.RemoveAt(0);
            }

            UpdateChatLog();

            // Console에도 로그 출력 (Inspector 테스트용)
            // HTML 태그 제거하여 출력
            string cleanMessage = System.Text.RegularExpressions.Regex.Replace(message, "<.*?>", string.Empty);
            Debug.Log($"[ChatLog] {cleanMessage}");
        }

        private void UpdateChatLog()
        {
            if (m_chatLogText != null)
            {
                m_chatLogText.text = string.Join("\n", m_chatLog);
            }
        }

        private void UpdateStatus(string status)
        {
            if (m_statusText != null)
            {
                m_statusText.text = $"상태: {status}";
            }

            // Console에도 로그 출력 (Inspector 테스트용)
            Debug.Log($"[ChatNetworkTester] Status: {status}");
        }

        #endregion

        #region 공개 API (테스트용)

        /// <summary>
        /// 프로그래밍 방식으로 연결
        /// </summary>
        public async void Connect(string host, int port)
        {
            m_serverHost = host;
            m_serverPort = port;

            if (m_hostInput != null)
                m_hostInput.text = host;

            if (m_portInput != null)
                m_portInput.text = port.ToString();

            await m_client.ConnectAsync(host, port);
        }

        /// <summary>
        /// 프로그래밍 방식으로 연결 해제
        /// </summary>
        public void Disconnect()
        {
            OnDisconnectButtonClicked();
        }

        /// <summary>
        /// 현재 연결 상태
        /// </summary>
        public bool IsConnected => m_client.IsConnected;

        /// <summary>
        /// 현재 룸 입장 상태
        /// </summary>
        public bool IsInRoom => m_isInRoom;

        #endregion
    }
}
