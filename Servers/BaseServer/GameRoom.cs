using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommonLib;

namespace BaseServer
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

        public GameRoom(string _roomId)
        {
            RoomId = _roomId;
            m_players = new List<ClientSession>();
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

    public class Game
    {
        public interface IPlayer
        {
            string Id { get; }
            void Send(string message);
        }

        [JsonSerializable(typeof(PlanetData))]
        public class PlanetData
        {
            public int ID;
            public int Name;

            public Dictionary<ResourceType, int> Resources = new Dictionary<ResourceType, int>()
            {
                { ResourceType.Gas, 0 },
                { ResourceType.Mineral, 0 },
                { ResourceType.Supply, 0 }
            };
            // 점령도.
            public int Occupy;
        }

        public class Planet
        {
            public PlanetData data;
            public Vector2 position;

            public Planet(PlanetData data, Vector2 position)
            {
                this.data = data;
                this.position = position;
            }
        }

        public enum ResourceType
        {
            Gas,
            Mineral,
            Supply
        }

        [JsonSerializable(typeof(MapData))]
        public class MapData
        {
            public List<PlanetData> planetDatas = new List<PlanetData>();
            public List<(int from, int to)> paths = new List<(int from, int to)>();
            public List<MapData> planetPositions = new List<MapData>();
        }

        public const int PLAYER_FACTION_1 = 1;
        public const int PLAYER_FACTION_2 = -1;
        public const int PLAYER_FACTION_NONE = 0;


        protected Graph<Planet> mapGraph = new Graph<Planet>();
        protected Dictionary<int, Planet> planetDict = new Dictionary<int, Planet>();
        protected Dictionary<int, HashSet<int>> pathCashDict = new Dictionary<int, HashSet<int>>();
        protected MapData mapData;

        public void InitializeMap(MapData mapData)
        {
            this.mapData = mapData;
            SetupMap();
        }

        private void SetupMap()
        {
            planetDict.Clear();
            var planets = mapData.planetDatas;
            foreach (var planetData in planets)
            {
                var planet = new Planet(planetData, new Vector2(0, 0));
                planetDict.Add(planet.data.ID, planet);
                mapGraph.AddNode(planet);
            }
            var paths = mapData.paths;
            foreach (var path in paths)
            {
                if (planetDict.TryGetValue(path.from, out var from) && planetDict.TryGetValue(path.to, out var to))
                {
                    // 가중치는 두 행성 간의 거리로 설정
                    mapGraph.AddEdge(from, to, Vector2.Distance(from.position, to.position));

                    // 경로 캐시 딕셔너리 업데이트
                    if (!pathCashDict.ContainsKey(path.from))
                        pathCashDict.Add(path.from, new HashSet<int>());
                    pathCashDict[path.from].Add(path.to);
                    if (!pathCashDict.ContainsKey(path.to))
                        pathCashDict.Add(path.to, new HashSet<int>());
                    pathCashDict[path.to].Add(path.from);
                }
            }
        }

        // 시작 행성에서 도착 행성까지 직접 연결이 있는지 체크. mapData에서 탐색
        public bool temp(int fromPlanetId, int toPlanetId)
        {
            if (planetDict.TryGetValue(fromPlanetId, out var from) && planetDict.TryGetValue(toPlanetId, out var to))
            {
                // 경로 캐시에서 직접 연결 여부 확인
                // from을 키로 사용하여 연결된 행성 목록에 to가 있는지 확인
                return pathCashDict.TryGetValue(fromPlanetId, out var connectedPlanets) && connectedPlanets.Contains(toPlanetId);
            }
            return false;
        }
    }
}