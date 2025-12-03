using ChatClientWPF.Utils;
using CommonLib;
using CommonLib.TableData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace ChatClientWPF.Models
{
    /// <summary>
    /// 룸 관리 및 서버 통신 담당 (BaseManager 의존성 제거)
    /// </summary>
    public class RoomManager
    {
        // --- 싱글톤 ---
        private static RoomManager? _instance;
        private static readonly object _lock = new object();

        public static RoomManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new RoomManager();
                        }
                    }
                }
                return _instance;
            }
        }

        // --- 네트워크 및 공통 이벤트 ---
        private ClientServerHandler? networkClient;
        private bool isInitialized = false;
        public bool IsInitialized => isInitialized;
        public event Action<string>? OnError;
        public event Action<string>? OnStatusMessage;
        // --- 룸 관련 이벤트들 ---
        public event Action<RoomInfo>? OnRoomJoinSuccess;
        public event Action<string, int>? OnRoomCreateSuccess;
        public event Action<string>? OnRoomJoinFailure;
        public event Action<List<RoomInfo>>? OnRoomListUpdated;
        public event Action? OnRoomLeft;
        public event Action OnGameStarting; // 게임 시작 알림
        public event Action<RoomInfo, WaittingRoomUser[]>? OnWaittingRoomInfoChanged; // 방 정보 변경 알림 (WaitingRoom UI용)
        public event Action<int, string, int>? OnUserJoinedRoom; // 유저 입장 알림 (userId, userName, playerCount)
        public event Action<int, string, int>? OnUserLeftRoom; // 유저 퇴장 알림 (userId, userName, playerCount)

        // --- 현재 상태 ---
        private RoomInfo? currentRoom = null;
        private List<RoomInfo> cachedRoomList = new List<RoomInfo>();
        private bool isInRoom = false;
        private UserInfo? currentUser = null;
        private string _sessionId = "";
        private (RoomInfo, WaittingRoomUser[]) currentWaittingRoomInfo;

        // --- 속성들 ---
        public RoomInfo? CurrentRoom => currentRoom;
        public bool IsInRoom => isInRoom;
        public List<RoomInfo> CachedRoomList => new List<RoomInfo>(cachedRoomList);

        public (RoomInfo, WaittingRoomUser[]) CachedCurrRoom => currentWaittingRoomInfo;

        // --- 초기화 및 생명주기 ---
        public async Task Initailize(UserInfo user, string sessionId = "")
        {
            currentUser = user;
            _sessionId = sessionId;

            networkClient = ClientServerHandler.Instance;
            if (networkClient == null)
                return;

            if (!isInitialized)
            {
                RegisterNetworkHandlers();
                isInitialized = true;
                EmitStatusMessage("RoomManager 초기화 완료");
            }

            // 로그인 성공했으니 로비 입장 요청 전송
            await RequestJoinLobbyAsync();
        }

        private void RegisterNetworkHandlers()
        {
            // 룸 관련 브로드캐스트 메시지 처리
            RegisterHandler(ProtocolType.BRODCAST_SYSTEM, HandleRoomBroadcast);
            RegisterHandler(ProtocolType.USER_JOINED, HandleUserJoined);
            RegisterHandler(ProtocolType.USER_LEFT, HandleUserLeft);
            RegisterHandler(ProtocolType.ROOM_INFO_CHANGED, HandleRoomInfoChanged);
            RegisterHandler(ProtocolType.ROOM_CLOSED, HandleRoomClosed);
            RegisterHandler(ProtocolType.GAME_SET, HandleGameSet);
        }

        public void Cleanup()
        {
            currentRoom = null;
            isInRoom = false;
            cachedRoomList.Clear();
            EmitStatusMessage("RoomManager 정리 완료");
        }

        public void Reset()
        {
            currentRoom = null;
            isInRoom = false;
            cachedRoomList.Clear();
        }

        // --- 룸 관련 공개 메서드들 ---

        /// <summary>
        /// 로비 접속 및 룸 목록 요청
        /// </summary>
        public async Task<bool> RequestJoinLobbyAsync(int page = 0)
        {
            try
            {
                ValidateNetworkConnection();

                var protocol = new Protocol(ProtocolType.REQUEST_JOIN_LOBBY)
                    .AddParam("Page", page);

                NetworkResponse response = await SafeSendAsync(protocol, "로비 접속");

                if (response.isSuccess)
                {
                    // 응답 파라미터: roomCount, page, roomList
                    int roomCount = response.GetParam<int>("roomCount");
                    int responsePage = response.GetParam<int>("page");
                    var roomListData = response.GetObject<RoomInfo[]>("roomList");
                    UpdateCashedRooms(roomListData);
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                EmitError($"로비 접속 오류: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 로비 새로고침 (룸 목록 갱신)
        /// </summary>
        public async Task<bool> RefreshLobbyAsync()
        {
            try
            {
                ValidateNetworkConnection();

                var protocol = new Protocol(ProtocolType.REFRESH_LOBBY);

                NetworkResponse response = await SafeSendAsync(protocol, "로비 새로고침");

                if (response.isSuccess)
                {
                    // 응답 파라미터: roomList
                    var roomListData = response.GetObject<RoomInfo[]>("roomList");
                    UpdateCashedRooms(roomListData);
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                EmitError($"로비 새로고침 오류: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 룸 생성 요청
        /// </summary>
        public async Task<bool> RequestCreateRoomAsync(string roomName, int mapId, bool isPrivate = false)
        {
            try
            {
                ValidateNetworkConnection();

                var protocol = new Protocol(ProtocolType.REQUEST_CREATE_ROOM)
                    .AddParam("roomName", roomName)
                    .AddParam("mapId", mapId)
                    .AddParam("isPrivate", isPrivate);

                NetworkResponse response = await SafeSendAsync(protocol, "룸 생성");

                if (response.isSuccess)
                {
                    // response.AddParam("roomId", room.RoomId);
                    // response.AddParam("slot", room.NextSlot());
                    // response.AddObject("roomList", roomList);
                    // 응답 파라미터: roomId, slot
                    string roomId = response.GetParam<string>("roomId");
                    int slot = response.GetParam<int>("slot");
                    RoomInfo[] rooms = response.GetObject<RoomInfo[]>("roomList");

                    UpdateCashedRooms(rooms);
                    OnRoomListUpdated?.Invoke(rooms.ToList());
                    EmitStatusMessage($"생성된 룸 ID: {roomId}, 슬롯: {slot}");
                    OnRoomCreateSuccess?.Invoke(roomId, slot);
                    return true;
                }
                else
                {
                    string failureReason = response.GetParam<string>("message");
                    if (string.IsNullOrEmpty(failureReason))
                        failureReason = response.resultCode.ToString();

                    OnRoomJoinFailure?.Invoke(failureReason);
                    return false;
                }
            }
            catch (Exception e)
            {
                EmitError($"룸 생성 오류: {e.Message}");
                OnRoomJoinFailure?.Invoke($"룸 생성 오류: {e.Message}");
                return false;
            }
        }

        private bool UpdateCashedRooms(RoomInfo[] rooms)
        {
            cachedRoomList.Clear();
            foreach (RoomInfo room in rooms)
            {
                cachedRoomList.Add(room);
            }

            EmitStatusMessage($"총 {cachedRoomList.Count}개의 룸 갱신됨");
            OnRoomListUpdated?.Invoke(cachedRoomList);

            return true;
        }

        /// <summary>
        /// 룸 참가 요청
        /// </summary>
        public async Task<bool> RequestJoinRoomAsync(string roomId, int slot = -1)
        {
            try
            {
                ValidateNetworkConnection();

                var protocol = new Protocol(ProtocolType.REQUEST_JOIN_ROOM)
                    .AddParam("roomId", roomId)
                    .AddParam("slot", slot);

                NetworkResponse response = await SafeSendAsync(protocol, "룸 참가");

                if (response.isSuccess)
                {
                    // 응답 파라미터: roomInfo, chatChannelID
                    string chatChannelID = response.GetParam<string>("chatChannelID");
                    RoomInfo roominfo = response.GetStruct<RoomInfo>("roomInfo");
                    WaittingRoomUser[] users = response.GetObject<WaittingRoomUser[]>("users");

                    currentRoom = roominfo;
                    isInRoom = true;

                    EmitStatusMessage($"룸에 참가함: {currentRoom.Value}, 채팅 채널: {chatChannelID}");

                    // 현재 룸 정보 업데이트
                    if (isInRoom && currentRoom.HasValue && currentRoom.Value.RoomId == roominfo.RoomId)
                    {
                        currentRoom = roominfo;
                        EmitStatusMessage($"현재 룸 정보 업데이트: {roominfo}");

                        currentWaittingRoomInfo = (roominfo, users);
                        // WaitingRoom UI를 위한 이벤트 발생
                        OnWaittingRoomInfoChanged?.Invoke(roominfo, users);
                    }

                    // 캐시된 룸 목록 업데이트
                    int index = cachedRoomList.FindIndex(room => room.RoomId == roominfo.RoomId);
                    if (index >= 0)
                    {
                        cachedRoomList[index] = roominfo;
                        OnRoomListUpdated?.Invoke(cachedRoomList);
                    }


                    OnRoomJoinSuccess?.Invoke(currentRoom.Value);
                    return true;
                }
                else
                {
                    string failureReason = response.GetParam<string>("message");
                    if (string.IsNullOrEmpty(failureReason))
                        failureReason = response.resultCode.ToString();

                    OnRoomJoinFailure?.Invoke(failureReason);
                    return false;
                }
            }
            catch (Exception e)
            {
                EmitError($"룸 참가 오류: {e.Message}");
                OnRoomJoinFailure?.Invoke($"룸 참가 오류: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 룸 나가기 요청
        /// </summary>
        public async Task<bool> RequestLeaveRoomAsync()
        {
            try
            {
                if (!isInRoom)
                {
                    EmitStatusMessage("현재 룸에 있지 않음");
                    return true; // 이미 나가진 상태
                }

                ValidateNetworkConnection();

                var protocol = new Protocol(ProtocolType.REQUEST_LEFT_ROOM);

                NetworkResponse response = await SafeSendAsync(protocol, "룸 나가기");

                if (response.isSuccess)
                {
                    // 상태 초기화
                    currentRoom = null;
                    isInRoom = false;

                    OnRoomLeft?.Invoke();
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                EmitError($"룸 나가기 오류: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 게임 준비 상태 변경
        /// </summary>
        public async Task<bool> RequestReadyAsync(bool isReady)
        {
            try
            {
                if (!isInRoom)
                {
                    EmitError("현재 룸에 있지 않음");
                    return false;
                }

                ValidateNetworkConnection();

                var protocol = new Protocol(ProtocolType.REQUEST_READY)
                    .AddParam("isReady", isReady);

                NetworkResponse response = await SafeSendAsync(protocol, $"게임 준비 ({isReady})");

                return response.isSuccess;
            }
            catch (Exception e)
            {
                EmitError($"게임 준비 오류: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 특정 룸 찾기
        /// </summary>
        public RoomInfo? FindRoom(string roomId)
        {
            return cachedRoomList.Find(room => room.RoomId == roomId);
        }

        /// <summary>
        /// 참가 가능한 룸 목록 가져오기
        /// </summary>
        public List<RoomInfo> GetJoinableRooms()
        {
            return cachedRoomList.FindAll(room =>
                room.RoomState == CommonLib.RoomState.Open && room.PlayerCount < room.MaxPlayers);
        }


        public async Task<bool> RequestJoinedRoomInfoRefresh()
        {
            try
            {
                if (!isInRoom)
                {
                    EmitError("현재 룸에 있지 않음");
                    return false;
                }

                ValidateNetworkConnection();

                var protocol = new Protocol(ProtocolType.REQUEST_REFRESH_JOINED_ROOM_INFO);

                NetworkResponse response = await SafeSendAsync(protocol);

                if(response.isSuccess)
                {
                    string roomId = protocol.GetParam<string>("roomId");
                    RoomInfo? roominfo = protocol.GetStruct<RoomInfo>("roomInfo");
                    WaittingRoomUser[] users = protocol.GetObject<WaittingRoomUser[]>("users");

                    if (roomId != null && roominfo != null && users != null)
                    {
                        // 현재 룸 정보 업데이트
                        if (isInRoom && currentRoom.HasValue && currentRoom.Value.RoomId == roominfo.Value.RoomId)
                        {
                            currentRoom = roominfo;
                            EmitStatusMessage($"현재 룸 정보 업데이트: {roominfo}");

                            currentWaittingRoomInfo = (roominfo.Value, users);
                            // WaitingRoom UI를 위한 이벤트 발생
                            OnWaittingRoomInfoChanged?.Invoke(roominfo.Value, users);
                        }

                        // 캐시된 룸 목록 업데이트
                        int index = cachedRoomList.FindIndex(room => room.RoomId == roominfo.Value.RoomId);
                        if (index >= 0)
                        {
                            cachedRoomList[index] = roominfo.Value;
                            OnRoomListUpdated?.Invoke(cachedRoomList);
                        }
                    }
                }
                return response.isSuccess;
            }
            catch (Exception e)
            {
                EmitError($"게임 준비 오류: {e.Message}");
                return false;
            }
        }

        // --- 내부 메서드들 ---

        /// <summary>
        /// Dictionary에서 RoomInfo 구조체로 파싱
        /// </summary>
        private RoomInfo ParseRoomInfo(Dictionary<string, object> data)
        {
            int roomStateValue = GetIntValue(data, "roomState");

            // DEBUG: RoomState 값 확인
            Logger.Log($"[RoomManager] ParseRoomInfo - roomState raw value: {roomStateValue}, as RoomState: {(CommonLib.RoomState)roomStateValue}");

            return new RoomInfo
            {
                RoomId = GetStringValue(data, "roomId"),
                RoomName = GetStringValue(data, "roomName"),
                PlayerCount = GetIntValue(data, "playerCount"),
                MaxPlayers = GetIntValue(data, "maxPlayers"),
                RoomState = (CommonLib.RoomState)roomStateValue,
                MapID = GetIntValue(data, "mapId")
            };
        }

        // --- 네트워크 이벤트 핸들러들 ---

        /// <summary>
        /// 시스템 브로드캐스트 처리
        /// </summary>
        private async Task HandleRoomBroadcast(Protocol protocol)
        {
            string messageType = protocol.GetParam<string>("messageType");
            EmitStatusMessage($"시스템 브로드캐스트: {messageType}");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 룸 정보 변경 처리
        /// </summary>
        private async Task HandleRoomInfoChanged(Protocol protocol)
        {
            string roomId = protocol.GetParam<string>("roomId");
            RoomInfo? roominfo = protocol.GetStruct<RoomInfo>("roomInfo");
            WaittingRoomUser[] users = protocol.GetObject<WaittingRoomUser[]>("users");

            if (roomId != null && roominfo != null && users != null)
            {
                // 현재 룸 정보 업데이트
                if (isInRoom && currentRoom.HasValue && currentRoom.Value.RoomId == roominfo.Value.RoomId)
                {
                    currentRoom = roominfo;
                    EmitStatusMessage($"현재 룸 정보 업데이트: {roominfo}");

                    currentWaittingRoomInfo = (roominfo.Value, users);
                    // WaitingRoom UI를 위한 이벤트 발생
                    OnWaittingRoomInfoChanged?.Invoke(roominfo.Value, users);
                }

                // 캐시된 룸 목록 업데이트
                int index = cachedRoomList.FindIndex(room => room.RoomId == roominfo.Value.RoomId);
                if (index >= 0)
                {
                    cachedRoomList[index] = roominfo.Value;
                    OnRoomListUpdated?.Invoke(cachedRoomList);
                }
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// 룸 폐쇄 처리
        /// </summary>
        private async Task HandleRoomClosed(Protocol protocol)
        {
            string closedRoomId = protocol.GetParam<string>("roomId");

            // 현재 있던 룸이 폐쇄된 경우
            if (isInRoom && currentRoom.HasValue && currentRoom.Value.RoomId == closedRoomId)
            {
                EmitStatusMessage($"현재 룸이 폐쇄됨: {closedRoomId}");
                currentRoom = null;
                isInRoom = false;
                OnRoomLeft?.Invoke();
            }

            // 캐시에서 제거
            cachedRoomList.RemoveAll(room => room.RoomId == closedRoomId);
            OnRoomListUpdated?.Invoke(cachedRoomList);

            await Task.CompletedTask;
        }

        /// <summary>
        /// 유저 입장 처리
        /// </summary>
        private async Task HandleUserJoined(Protocol protocol)
        {
            if (isInRoom && currentRoom.HasValue)
            {
                // 서버는 userName이 아닌 userId를 브로드캐스트합니다.
                int userId = protocol.GetParam<int>("userId");
                string userName = protocol.GetParam<string>("userName") ?? "";
                int newPlayerCount = protocol.GetParam<int>("playerCount");

                EmitStatusMessage($"유저 입장: {userName} (ID: {userId}, 총 {newPlayerCount}명)");

                // 현재 룸 정보 업데이트
                var updatedRoom = currentRoom.Value;
                updatedRoom.PlayerCount = newPlayerCount;
                currentRoom = updatedRoom;

                // 이벤트 발생
                OnUserJoinedRoom?.Invoke(userId, userName, newPlayerCount);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 유저 퇴장 처리
        /// </summary>
        private async Task HandleUserLeft(Protocol protocol)
        {
            if (isInRoom && currentRoom.HasValue)
            {
                int userId = protocol.GetParam<int>("userId");
                string userName = protocol.GetParam<string>("userName") ?? "";
                int newPlayerCount = protocol.GetParam<int>("playerCount");

                EmitStatusMessage($"유저 퇴장: {userName} (ID: {userId}, 총 {newPlayerCount}명)");

                // 현재 룸 정보 업데이트
                var updatedRoom = currentRoom.Value;
                updatedRoom.PlayerCount = newPlayerCount;
                currentRoom = updatedRoom;

                OnUserLeftRoom?.Invoke(userId, userName, newPlayerCount);
            }

            await Task.CompletedTask;
        }

        /// <summary>
          /// 게임 시작 처리 - 씬 전환 이벤트 발생
        /// </summary>
        private async Task HandleGameSet(Protocol protocol)
        {
            EmitStatusMessage("게임이 곧 시작됩니다. GameScene으로 전환합니다...");

            // 게임 시작 이벤트 발생 (UIWaitingRoom에서 씬 전환 처리)
            var mapInfo = protocol.GetObject<MapInfoData>("mapinfo");
            var mapplanetinfo = protocol.GetObject<MapPlanetInfoData[]>("mapplanetinfo");
            var planets = protocol.GetObject<PlanetInfoData[]>("planets");
            var routes = protocol.GetObject<MapRouteInfoData[]>("routes");
            OnGameStarting?.Invoke();

            await Task.CompletedTask;
        }

        // --- UI 호출용 간단 래퍼 메서드들 ---

        public async void JoinLobby(int page = 0)
        {
            await RequestJoinLobbyAsync(page);
        }

        public async void RefreshRoomList()
        {
            await RefreshLobbyAsync();
        }

        public async void CreateRoom(string roomName, int mapId, bool isPrivate = false)
        {
            await RequestCreateRoomAsync(roomName, mapId, isPrivate);
        }

        public async void JoinRoom(string roomId, int slot = -1)
        {
            await RequestJoinRoomAsync(roomId, slot);
        }

        public async void LeaveRoom()
        {
            await RequestLeaveRoomAsync();
        }

        public async void SetReady(bool isReady)
        {
            await RequestReadyAsync(isReady);
        }

        // --- 공통 유틸리티 (BaseManager 대체) ---
        private void EmitStatusMessage(string message)
        {
            Logger.Log($"[RoomManager] {message}");
            OnStatusMessage?.Invoke(message);
        }

        private void EmitError(string error)
        {
            Logger.LogError($"[RoomManager] {error}");
            OnError?.Invoke(error);
        }

        private bool IsNetworkReady()
        {
            return networkClient != null && networkClient.IsConnected;
        }

        private void ValidateNetworkConnection()
        {
            if (!IsNetworkReady())
            {
                throw new InvalidOperationException("서버에 연결되지 않았습니다");
            }
        }

        private void RegisterHandler(int protocolType, ProtocolHandler.ProtocolHandlerDelegate handler)
        {
            if (networkClient != null)
            {
                networkClient.RegisterHandler(protocolType, handler);
                Logger.Log($"[RoomManager] 핸들러 등록됨: {protocolType}");
            }
            else
            {
                Logger.LogError($"[RoomManager] 네트워크 클라이언트가 없어 핸들러 등록 실패: {protocolType}");
            }
        }

        private async Task<NetworkResponse> SafeSendAsync(Protocol protocol, string operationName = "")
        {
            try
            {
                if (networkClient == null)
                {
                    throw new InvalidOperationException("네트워크 클라이언트가 초기화되지 않음");
                }

                if (!string.IsNullOrEmpty(operationName))
                {
                    Logger.Log($"[RoomManager] {operationName} 요청 중...");
                }

                NetworkResponse response = await networkClient.AsyncSend(protocol);

                if (response.isSuccess)
                {
                    if (!string.IsNullOrEmpty(operationName))
                    {
                        Logger.Log($"[RoomManager] {operationName} 성공");
                    }
                }
                else
                {
                    string errorMsg = $"{operationName} 실패: {response.resultCode}";
                    Logger.LogError($"[RoomManager] {errorMsg}");
                    OnError?.Invoke(errorMsg);
                    response.ShowResultCode();
                }

                return response;
            }
            catch (Exception e)
            {
                string errorMsg = $"{operationName} 오류: {e.Message}";
                Logger.LogError($"[RoomManager] {errorMsg}");
                OnError?.Invoke(errorMsg);
                throw;
            }
        }

        // 안전한 값 파싱 유틸리티
        private int GetIntValue(Dictionary<string, object> data, string key, int defaultValue = 0)
        {
            if (data != null && data.TryGetValue(key, out object? value))
            {
                if (value is int intValue) return intValue;
                if (int.TryParse(value?.ToString(), out int parsedValue)) return parsedValue;
            }
            return defaultValue;
        }

        private string GetStringValue(Dictionary<string, object> data, string key, string defaultValue = "")
        {
            if (data != null && data.TryGetValue(key, out object? value))
            {
                return value?.ToString() ?? defaultValue;
            }
            return defaultValue;
        }

        private bool GetBoolValue(Dictionary<string, object> data, string key, bool defaultValue = false)
        {
            if (data != null && data.TryGetValue(key, out object? value))
            {
                if (value is bool boolValue) return boolValue;
                if (bool.TryParse(value?.ToString(), out bool parsedValue)) return parsedValue;
            }
            return defaultValue;
        }
    }
}
