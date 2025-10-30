using CommonLib;
using CommonLib.Commands;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BaseServer.Core.Game.Session;

namespace BaseServer.Core.Game.Entities
{
    /// <summary>
    /// 게임 룸 클래스 - 2인 채팅 룸
    /// </summary>
    public class GameRoom
    {
        #region 상수
        public const int MaxPlayers = 2;
        #endregion

        #region 필드
        private readonly object m_lockObj = new object();
        private GameRoomUser[] m_users = new GameRoomUser[MaxPlayers];
        private Game gameInstance = new Game();
        #endregion

        #region 기본 속성
        public string RoomId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public RoomState State { get; private set; }
        public int MapID { get; private set; }
        public int ChatChID => -1; // 채팅 체널 ID. 추후 구현
        #endregion

        #region 계산된 속성
        /// <summary>
        /// 룸 정보 객체
        /// </summary>
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
                    };
                }
            }
        }

        /// <summary>
        /// 현재 플레이어 수
        /// </summary>
        public int PlayerCount
        {
            get
            {
                lock (m_lockObj)
                {
                    int count = 0;
                    foreach (var player in m_users)
                    {
                        if (player.IsValid)
                            count++;
                    }

                    return count;
                }
            }
        }

        /// <summary>
        /// 룸이 가득 찼는지 확인
        /// </summary>
        public bool IsFull
        {
            get
            {
                lock (m_lockObj)
                {
                    int count = 0;
                    foreach (var player in m_users)
                    {
                        if (player.IsValid)
                            count++;
                    }

                    return count >= MaxPlayers;
                }
            }
        }

        /// <summary>
        /// 룸이 비어있는지 확인
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                lock (m_lockObj)
                {
                    foreach (var player in m_users)
                    {
                        if (player.IsValid)
                            return false;
                    }

                    return true;
                }
            }
        }
        #endregion

        #region 생성자
        public GameRoom(string _roomId, int mapID = 0)
        {
            RoomId = _roomId;
            gameInstance = new Game();
            State = RoomState.Open;
            MapID = mapID;

            for (int i = 0; i < MaxPlayers; i++)
            {
                m_users[i] = new GameRoomUser();
                m_users[i].OnChangedReady += HandleOnReady;
            }
        }
        #endregion

        #region 룸 관리 메소드
        public int NextSlot()
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (!m_users[i].IsValid)
                    return i;
            }

            return -1;
        }

        private void HandleOnReady()
        {
            bool isReady = false;
            foreach(var player in m_users)
            {
                isReady = isReady && player.IsValid && player.IsReady;
            }

            if(isReady)
            {
                foreach(var player in m_users)
                {
                    gameInstance.UserJoin(player.Session);
                }
            }
        }

        public void UpdateRoomInfo(string name = "", int mapId = -1)
        {
            if (!name.Equals(""))
                Name = name;

            if (mapId != -1)
                MapID = mapId;

            Protocol proto = new Protocol(ProtocolType.ROOM_INFO_CHANGED);
            proto.AddParam("roomId", RoomId);
            proto.AddStruct("roomInfo", RoomInfo);

            Brodcast(proto);
        }

        /// <summary>
        /// 룸 종료 알림을 모든 플레이어에게 전송
        /// </summary>
        public async Task NotifyRoomClosed()
        {
            List<GameRoomUser> playersCopy;
            lock (m_lockObj)
            {
                playersCopy = new List<GameRoomUser>();
                foreach (var player in m_users)
                {
                    playersCopy.Add(player);
                }
            }

            Protocol protocol = new Protocol(ProtocolType.ROOM_CLOSED)
                .AddParam("roomId", RoomId)
                .AddParam("reason", "Room has been closed");

            byte[] data = protocol.Serialize();

            foreach (var player in playersCopy)
            {
                try
                {
                    await player.HandleBrodcast(data);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[Room {RoomId}] Failed to notify room closure to {player.ID}: {e.Message}");
                }
            }
        }
        #endregion

        #region 플레이어 관리 메소드
        /// <summary>
        /// 플레이어를 룸에 추가
        /// </summary>
        public bool TryAddPlayer(ClientSession _session, int slot)
        {
            lock (m_lockObj)
            {
                if (m_users.Length >= MaxPlayers)
                    return false;
                if (slot < 0 || slot >= m_users.Length)
                    return false;
                if (m_users[slot] != null)
                    return false;

                m_users[slot].Initialize(_session);
                _session.CurrentRoom = this;

                // 다른 플레이어에게 입장 알림
                BroadcastUserJoined(m_users[slot]);

                return true;
            }
        }

        public bool TryRemovePlayer(ClientSession _session)
        {
            for (int i = 0; i < m_users.Length; i++)
            {
                if (m_users[i] != null && m_users[i].IsSameSession(_session))
                {
                    return RemovePlayer(i);
                }
            }

            return false;
        }

        /// <summary>
        /// 플레이어를 룸에서 제거
        /// </summary>
        public bool RemovePlayer(int slot)
        {
            lock (m_lockObj)
            {
                if (slot < 0 || slot >= m_users.Length)
                    return false;
                if (m_users[slot] == null)
                    return false;

                var playerId = m_users[slot].ID;
                var playerName = m_users[slot].Name;
                m_users[slot].Cleanup();
                Console.WriteLine($"[Room {RoomId}] Player {playerId} left. ({PlayerCount}/{MaxPlayers})");

                // 다른 플레이어에게 퇴장 알림
                BroadcastUserLeft(playerId);

                return true;
            }
        }

        /// <summary>
        /// 룸의 모든 플레이어 연결 종료
        /// </summary>
        public void CloseAllConnections()
        {
            lock (m_lockObj)
            {
                foreach (var player in m_users)
                {
                    player.ForceDisconnect();
                }
                Array.Clear(m_users, 0, m_users.Length);
            }
        }
        #endregion

        #region 게임 로직
        public async Task AddCommand(IGameCommand command)
        {
            gameInstance.EnqueueCommand(command);
        }
        #endregion

        #region 브로드캐스트 메소드
        /// <summary>
        /// 채팅 메시지를 룸의 모든 플레이어에게 브로드캐스트
        /// </summary>
        public async Task BroadcastMessage(ClientSession _sender, string _message)
        {
            List<ClientSession> playersCopy;
            lock (m_lockObj)
            {
                playersCopy = new List<ClientSession>();
                foreach (var player in m_users)
                {
                    playersCopy.Add(player.Session);
                }
            }

            ChatMessage chatMsg = new ChatMessage
            {
                SenderId = _sender.SessionId,
                Message = _message,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                MessageType = 0 // 0: LOBBY
            };

            Protocol protocol = new Protocol(ProtocolType.BRODCAST_CHAT_MESSAGE)
                .AddStruct("chatMessage", chatMsg);

            byte[] data = protocol.Serialize();

            foreach (var player in playersCopy)
            {
                try
                {
                    await player.SendAsync(data);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[Room {RoomId}] Failed to send message to {player.SessionId}: {e.Message}");
                }
            }
        }

        private void Brodcast(Protocol proto)
        {
            byte[] data = proto.Serialize();

            foreach (var player in m_users)
            {
                if (player == null)
                    continue;
                _ = player.HandleBrodcast(data);
            }
        }

        /// <summary>
        /// 유저 입장 알림 브로드캐스트
        /// </summary>
        private void BroadcastUserJoined(GameRoomUser _joinedSession)
        {
            Protocol protocol = new Protocol(ProtocolType.USER_JOINED)
                .AddParam("userId", _joinedSession.ID)
                .AddParam("playerCount", PlayerCount);

            byte[] data = protocol.Serialize();

            Brodcast(protocol);
        }

        /// <summary>
        /// 유저 퇴장 알림 브로드캐스트
        /// </summary>
        private void BroadcastUserLeft(int sessionID)
        {
            Protocol protocol = new Protocol(ProtocolType.USER_LEFT)
                .AddParam("userId", sessionID)
                .AddParam("playerCount", PlayerCount);

            Brodcast(protocol);
        }
        #endregion
    }

    public enum GameCommandType
    {
        None = 0,
        ProduceFleet = 1,
        MoveFleet = 2,
    }
}