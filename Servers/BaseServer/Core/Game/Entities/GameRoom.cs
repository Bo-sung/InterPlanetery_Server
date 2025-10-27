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
        public string RoomId { get; private set; }
        public int MaxPlayers { get; private set; } = 2;
        private List<ClientSession> m_players;
        private readonly object m_lockObj = new object();
        private Game gameInstance = new Game();

        public GameRoom(string _roomId)
        {
            RoomId = _roomId;
            m_players = new List<ClientSession>();
            gameInstance = new Game();
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
                    return m_players.Count;
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
                    return m_players.Count >= MaxPlayers;
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
                    return m_players.Count == 0;
                }
            }
        }

        /// <summary>
        /// 플레이어를 룸에 추가
        /// </summary>
        public bool AddPlayer(ClientSession _session)
        {
            lock (m_lockObj)
            {
                if (m_players.Count >= MaxPlayers)
                    return false;

                if (m_players.Contains(_session))
                    return false;

                m_players.Add(_session);
                _session.CurrentRoom = this;
                Console.WriteLine($"[Room {RoomId}] Player {_session.SessionId} joined. ({m_players.Count}/{MaxPlayers})");

                // 다른 플레이어에게 입장 알림
                BroadcastUserJoined(_session);
                gameInstance.UserJoin(_session);

                return true;
            }
        }

        /// <summary>
        /// 플레이어를 룸에서 제거
        /// </summary>
        public bool RemovePlayer(ClientSession _session)
        {
            lock (m_lockObj)
            {
                if (!m_players.Contains(_session))
                    return false;

                m_players.Remove(_session);
                _session.CurrentRoom = null;
                Console.WriteLine($"[Room {RoomId}] Player {_session.SessionId} left. ({m_players.Count}/{MaxPlayers})");

                // 다른 플레이어에게 퇴장 알림
                BroadcastUserLeft(_session);

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
                playersCopy = new List<ClientSession>(m_players);
            }

            ChatMessage chatMsg = new ChatMessage
            {
                SenderId = _sender.SessionId,
                Message = _message,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            Protocol protocol = new Protocol(ChatProtocolType.CHAT_BROADCAST)
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

        /// <summary>
        /// 유저 입장 알림 브로드캐스트
        /// </summary>
        private void BroadcastUserJoined(ClientSession _joinedSession)
        {
            Protocol protocol = new Protocol(ChatProtocolType.USER_JOINED)
                .AddParam("userId", _joinedSession.SessionId)
                .AddParam("playerCount", m_players.Count);

            byte[] data = protocol.Serialize();

            foreach (var player in m_players)
            {
                if (player != _joinedSession)
                {
                    _ = player.SendAsync(data);
                }
            }
        }

        /// <summary>
        /// 유저 퇴장 알림 브로드캐스트
        /// </summary>
        private void BroadcastUserLeft(ClientSession _leftSession)
        {
            Protocol protocol = new Protocol(ChatProtocolType.USER_LEFT)
                .AddParam("userId", _leftSession.SessionId)
                .AddParam("playerCount", m_players.Count);

            byte[] data = protocol.Serialize();

            foreach (var player in m_players)
            {
                _ = player.SendAsync(data);
            }
        }

        /// <summary>
        /// 룸 종료 알림을 모든 플레이어에게 전송
        /// </summary>
        public async Task NotifyRoomClosed()
        {
            List<ClientSession> playersCopy;
            lock (m_lockObj)
            {
                playersCopy = new List<ClientSession>(m_players);
            }

            Protocol protocol = new Protocol(ChatProtocolType.ROOM_CLOSED)
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
                    player.Disconnect();
                }
                m_players.Clear();
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