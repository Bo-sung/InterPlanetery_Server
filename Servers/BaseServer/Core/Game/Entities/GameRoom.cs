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
        private readonly object m_lockObj = new object();

        public string RoomId { get; private set; }
        public string Name { get; private set; } = string.Empty;

        public const int MaxPlayers = 2;
        public RoomState State { get; private set; }
        public int MapID { get; private set; }
        private GamePlayer[] m_players = new GamePlayer[MaxPlayers];
        private Game gameInstance = new Game();

        public GameRoom(string _roomId, int mapID = 0)
        {
            RoomId = _roomId;
            gameInstance = new Game();
            State = RoomState.Open;
            MapID = mapID;

        }

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
                    foreach (var player in m_players)
                    {
                        if (player != null)
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
                    foreach (var player in m_players)
                    {
                        if (player != null)
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
                    int count = 0;
                    foreach (var player in m_players)
                    {
                        if (player != null)
                            return false;
                    }

                    return true;
                }
            }
        }

        /// <summary>
        /// 플레이어를 룸에 추가
        /// </summary>
        public bool AddPlayer(ClientSession _session, int slot)
        {
            lock (m_lockObj)
            {
                if (m_players.Length >= MaxPlayers)
                    return false;
                if (slot < 0 || slot >= m_players.Length)
                    return false;
                if (m_players[slot] != null)
                    return false;

                m_players[slot] = new GamePlayer(_session);
                _session.CurrentRoom = this;

                // 다른 플레이어에게 입장 알림
                BroadcastUserJoined(m_players[slot]);
                gameInstance.UserJoin(m_players[slot]);

                return true;
            }
        }

        public bool RemovePlayer(ClientSession _session)
        {
            for(int i = 0; i < m_players.Length; i++)
            {
                if (m_players[i] != null && m_players[i].Session.Equals(_session))
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
                if (slot < 0 || slot >= m_players.Length)
                    return false;
                if (m_players[slot] == null)
                    return false;

                var player = m_players[slot];
                m_players[slot] = null;
                player.Session.CurrentRoom = null;
                Console.WriteLine($"[Room {RoomId}] Player {player.Session.SessionId} left. ({PlayerCount}/{MaxPlayers})");

                // 다른 플레이어에게 퇴장 알림
                BroadcastUserLeft(player.ID);

                return true;
            }
        }

        public async Task AddCommand(IGameCommand command)
        {
            gameInstance.EnqueueCommand(command);
        }

        /// <summary>
        /// 채팅 메시지를 룸의 모든 플레이어에게 브로드캐스트
        /// </summary>
        public async Task BroadcastMessage(ClientSession _sender, string _message)
        {
            List<ClientSession> playersCopy;
            lock (m_lockObj)
            {
                playersCopy = new List<ClientSession>();
                foreach(var  player in m_players)
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

            foreach (var player in m_players)
            {
                if (player == null)
                    continue;
                _ = player.Session.SendAsync(data);
            }
        }

        /// <summary>
        /// 유저 입장 알림 브로드캐스트
        /// </summary>
        private void BroadcastUserJoined(GamePlayer _joinedSession)
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

        /// <summary>
        /// 룸 종료 알림을 모든 플레이어에게 전송
        /// </summary>
        public async Task NotifyRoomClosed()
        {
            List<ClientSession> playersCopy;
            lock (m_lockObj)
            {
                playersCopy = new List<ClientSession>();
                foreach (var player in m_players)
                {
                    playersCopy.Add(player.Session);
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
                    await player.SendAsync(data);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[Room {RoomId}] Failed to notify room closure to {player.SessionId}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 룸의 모든 플레이어 연결 종료
        /// </summary>
        public void CloseAllConnections()
        {
            lock (m_lockObj)
            {
                foreach (var player in m_players)
                {
                    player.Session.Disconnect();
                }
                Array.Clear(m_players, 0, m_players.Length);
            }
        }
    }

    public enum GameCommandType
    {
        None = 0,
        ProduceFleet = 1,
        MoveFleet = 2,
    }
}