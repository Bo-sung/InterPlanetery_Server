using BaseServer.Utils;
using CommonLib;
using CommonLib.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseServer.Core.Game.Session;

namespace BaseServer.Core.Game.Entities
{
    /// <summary>
    /// 게임 룸 클래스 - 2인 대전 룸
    /// </summary>
    public class GameRoom : ICommandSender
    {
        #region 상수
        public const int MaxPlayers = 2;
        #endregion

        #region 필드
        private readonly object m_lockObj = new object();
        private GameRoomUser[] m_users = new GameRoomUser[MaxPlayers];
        private Game gameInstance;
        #endregion

        #region 기본 속성
        public string RoomId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public RoomState State { get; private set; }
        public int MapID { get; private set; }
        public int ChatChID => -1; // 채팅 채널 ID (추후 구현)
        #endregion

        #region 계산된 속성
        public RoomInfo RoomInfo
        {
            get
            {
                lock (m_lockObj)
                {
                    return new RoomInfo()
                    {
                        RoomId = RoomId,
                        RoomName = Name,
                        RoomState = State,
                        MaxPlayers = MaxPlayers,
                        PlayerCount = PlayerCount,
                        MapID = MapID,
                    };
                }
            }
        }

        public int PlayerCount
        {
            get
            {
                lock (m_lockObj)
                {
                    return m_users.Count(user => user != null && user.IsValid);
                }
            }
        }

        public bool IsFull
        {
            get
            {
                lock (m_lockObj)
                {
                    return m_users.Count(user => user != null && user.IsValid) >= MaxPlayers;
                }
            }
        }

        public bool IsEmpty
        {
            get
            {
                lock (m_lockObj)
                {
                    return m_users.All(user => user == null || !user.IsValid);
                }
            }
        }
        #endregion

        #region 생성자
        public GameRoom(string roomId, int mapID = 0)
        {
            RoomId = roomId ?? throw new ArgumentNullException(nameof(roomId));
            MapID = mapID;
            State = RoomState.Open;
            gameInstance = new Game();

            for (int i = 0; i < MaxPlayers; i++)
            {
                m_users[i] = new GameRoomUser();
                m_users[i].OnChangedReady += HandleOnReady;
            }

            Logger.Log($"[Room {RoomId}] Created with MapID: {MapID}");
        }

        #endregion

        #region 룸 관리 메소드
        public int NextSlot()
        {
            lock (m_lockObj)
            {
                for (int i = 0; i < MaxPlayers; i++)
                {
                    if (m_users[i] == null || !m_users[i].IsValid)
                        return i;
                }
                return -1;
            }
        }

        private void HandleOnReady()
        {
            lock (m_lockObj)
            {
                // 모든 플레이어가 유효하고 준비 상태인지 확인
                bool allPlayersReady = m_users.All(user =>
                    user != null && user.IsValid && user.IsReady);

                Logger.Log($"[Room {RoomId}] players ready changed.IsAllReady = {allPlayersReady}");
                if (!allPlayersReady)
                    return;

                // 게임 시작
                Logger.Log($"[Room {RoomId}] All players ready. Starting game...");

                foreach (var user in m_users)
                {
                    if (user != null && user.IsValid)
                    {
                        gameInstance.UserJoin(user.Session, this);
                    }
                }

                _ = gameInstance.StartGame(MapID);
            }
        }

        public void UpdateRoomInfo(string name = "", int mapId = -1)
        {
            lock (m_lockObj)
            {
                if (!string.IsNullOrEmpty(name))
                    Name = name;

                if (mapId >= 0)
                    MapID = mapId;

                var value = GetRoomInfo();


                Protocol proto = new Protocol(ProtocolType.ROOM_INFO_CHANGED)
                    .AddParam("roomId", value.roomId)
                    .AddStruct("roomInfo", value.roomInfo)
                    .AddObject<WaittingRoomUser[]>("users", value.waitusers);

                Broadcast(proto);
            }

            Logger.Log($"[Room {RoomId}] Info updated - Name: {Name}, MapID: {MapID}");
        }

        public (string roomId, RoomInfo roomInfo, WaittingRoomUser[] waitusers) GetRoomInfo()
        {
            WaittingRoomUser[] tempUsers = new WaittingRoomUser[m_users.Length];

            for (int index = 0; index < tempUsers.Length; index++)
            {
                var temp = new WaittingRoomUser();
                var user = m_users[index];
                if (user != null && user.IsValid)
                {
                    temp.userInfo = new UserInfo()
                    {
                        UserId = user.ID,
                        UserName = user.Name
                    };
                }
                else
                {
                    temp.userInfo = new UserInfo()
                    {
                        UserId = -1,
                        UserName = string.Empty
                    };
                }
                tempUsers[index] = temp;
            }

            return (roomid: RoomId, roomInfo: RoomInfo, waitusers: tempUsers);
        }

        public async Task NotifyRoomClosed()
        {
            List<GameRoomUser> validUsers = new List<GameRoomUser>();

            lock (m_lockObj)
            {
                foreach (var user in m_users)
                {
                    if (user != null && user.IsValid)
                    {
                        validUsers.Add(user);
                    }
                }
            }

            if (validUsers.Count == 0)
                return;

            Protocol protocol = new Protocol(ProtocolType.ROOM_CLOSED)
                .AddParam("roomId", RoomId)
                .AddParam("reason", "Room has been closed");

            byte[] data = protocol.Serialize();

            var tasks = validUsers.Select(user =>
                SafeSendAsync(user, data, "room closure notification"));

            await Task.WhenAll(tasks);

            Logger.Log($"[Room {RoomId}] Closed notification sent to {validUsers.Count} players");
        }

        public void Dispose()
        {
            lock (m_lockObj)
            {
                // 이벤트 구독 해제
                for (int i = 0; i < m_users.Length; i++)
                {
                    if (m_users[i] != null)
                    {
                        m_users[i].OnChangedReady -= HandleOnReady;
                        m_users[i].Cleanup();
                        m_users[i] = null;
                    }
                }

                gameInstance?.Dispose();
            }

            Logger.Log($"[Room {RoomId}] Disposed");
        }
        #endregion

        #region 플레이어 관리 메소드
        /// <summary>
        /// 빈 슬롯을 찾아서 반환. 빈 슬롯이 없으면 -1 반환
        /// </summary>
        public int FindEmptySlot()
        {
            lock (m_lockObj)
            {
                for (int i = 0; i < m_users.Length; i++)
                {
                    if (m_users[i] == null || !m_users[i].IsValid)
                    {
                        return i;
                    }
                }
                return -1; // 빈 슬롯 없음
            }
        }

        public bool TryAddPlayer(ClientSession session, int slot)
        {
            if (session == null)
            {
                Logger.Log($"[Room {RoomId}] Cannot add null session");
                return false;
            }

            lock (m_lockObj)
            {
                if (slot < 0 || slot >= MaxPlayers)
                {
                    Logger.Log($"[Room {RoomId}] Invalid slot: {slot}");
                    return false;
                }

                if (m_users[slot] == null || !m_users[slot].IsValid)
                {
                    if (!session.TryTransitionTo(SessionState.Room))
                        return false;

                    if (m_users[slot] == null)
                        m_users[slot] = new GameRoomUser();

                    m_users[slot].Initialize(session);
                    session.CurrentRoom = this;

                    Logger.Log($"[Room {RoomId}] Player {session.SessionId} joined at slot {slot}. ({PlayerCount}/{MaxPlayers})");

                    // 다른 플레이어에게 입장 알림
                    BroadcastUserJoined(m_users[slot]);

                    return true;
                }

                Logger.Log($"[Room {RoomId}] Slot {slot} is already occupied");
                return false;
            }
        }

        public bool TryRemovePlayer(ClientSession session)
        {
            if (session == null)
                return false;

            lock (m_lockObj)
            {
                for (int i = 0; i < m_users.Length; i++)
                {
                    if (m_users[i] != null && m_users[i].IsSameSession(session))
                    {
                        return RemovePlayer(i);
                    }
                }
            }

            return false;
        }

        public bool RemovePlayer(int slot)
        {
            lock (m_lockObj)
            {
                if (slot < 0 || slot >= m_users.Length)
                    return false;

                if (m_users[slot] == null || !m_users[slot].IsValid)
                    return false;

                int playerId = m_users[slot].ID;
                string playerName = m_users[slot].Name;
                ClientSession session = m_users[slot].Session;

                gameInstance.UserLeave(session);

                m_users[slot].Cleanup();
                session.CurrentRoom = null;
                session.TryTransitionTo(SessionState.Lobby);

                Logger.Log($"[Room {RoomId}] Player {playerId} ({playerName}) left. ({PlayerCount}/{MaxPlayers})");

                // 다른 플레이어에게 퇴장 알림
                BroadcastUserLeft(playerId, playerName);

                return true;
            }
        }

        public void CloseAllConnections()
        {
            lock (m_lockObj)
            {
                int disconnectedCount = 0;

                foreach (var user in m_users)
                {
                    if (user != null && user.IsValid)
                    {
                        user.ForceDisconnect();
                        disconnectedCount++;
                    }
                }

                Array.Clear(m_users, 0, m_users.Length);

                Logger.Log($"[Room {RoomId}] Closed all connections ({disconnectedCount} players)");
            }
        }
        #endregion

        #region 게임 로직
        public async Task SendCommandToGame(Command command)
        {
            if (command == null)
            {
                Logger.Log($"[Room {RoomId}] Cannot send null command");
                return;
            }

            await gameInstance.EnqueueCommand(command);
        }

        public long GetGameCurrentTick()
        {
            return gameInstance.GetCurrentTick();
        }
        #endregion

        #region 브로드캐스트 메소드
        public async Task BroadcastMessage(ClientSession sender, string message)
        {
            if (sender == null || string.IsNullOrEmpty(message))
                return;

            List<ClientSession> validSessions = new List<ClientSession>();

            lock (m_lockObj)
            {
                foreach (var user in m_users)
                {
                    if (user != null && user.IsValid && user.Session != null)
                    {
                        validSessions.Add(user.Session);
                    }
                }
            }

            if (validSessions.Count == 0)
                return;

            ChatMessage chatMsg = new ChatMessage
            {
                SenderId = sender.SessionId,
                Message = message,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                MessageType = 0 // 0: ROOM_CHAT
            };

            Protocol protocol = new Protocol(ProtocolType.BRODCAST_CHAT_MESSAGE)
                .AddStruct("chatMessage", chatMsg);

            byte[] data = protocol.Serialize();

            var tasks = validSessions.Select(session =>
                SafeSendAsync(session, data, "chat message"));

            await Task.WhenAll(tasks);
        }

        private void Broadcast(Protocol proto)
        {
            if (proto == null)
                return;

            byte[] data = proto.Serialize();

            lock (m_lockObj)
            {
                foreach (var user in m_users)
                {
                    if (user != null && user.IsValid)
                    {
                        _ = user.HandleBrodcast(data);
                    }
                }
            }
        }

        private void BroadcastUserJoined(GameRoomUser joinedUser)
        {
            if (joinedUser == null)
                return;

            Protocol protocol = new Protocol(ProtocolType.USER_JOINED)
                .AddParam("userId", joinedUser.ID)
                .AddParam("userName", joinedUser.Name)
                .AddParam("playerCount", PlayerCount);

            Broadcast(protocol);
        }

        private void BroadcastUserLeft(int userId, string userName)
        {
            Protocol protocol = new Protocol(ProtocolType.USER_LEFT)
                .AddParam("userId", userId)
                .AddParam("userName", userName)
                .AddParam("playerCount", PlayerCount);

            Broadcast(protocol);
        }

        /// <summary>
        /// 안전한 비동기 전송 (예외 처리 포함)
        /// </summary>
        private async Task SafeSendAsync(dynamic target, byte[] data, string messageType)
        {
            try
            {
                if (target is ClientSession session)
                {
                    await session.SendAsync(data);
                }
                else if (target is GameRoomUser user)
                {
                    await user.HandleBrodcast(data);
                }
            }
            catch (Exception e)
            {
                Logger.Log($"[Room {RoomId}] Failed to send {messageType}: {e.Message}");
            }
        }
        #endregion
    }
}
