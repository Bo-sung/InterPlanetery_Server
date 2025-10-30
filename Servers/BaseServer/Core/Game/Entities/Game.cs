using BaseServer.Core.Game.Session;
using BaseServer.Database;
using CommonLib.Commands; // IGameCommand
using BaseServer.Services; // MapService
using CommonLib.TableData; // MapData, Planet
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaseServer.Core.Game.Entities
{
    public class Game
    {
        #region 상수
        public const int MAX_PLAYERS = 2;

        public const int PLAYER_FACTION_1 = 1;
        public const int PLAYER_FACTION_2 = -1;
        public const int PLAYER_FACTION_NONE = 0;

        const int GAMESTATE_WAITING = 0;
        const int GAMESTATE_RUNNING = 1;
        const int GAMESTATE_ENDED = 2;

        public const float FIXED_TICK_RATE = 0.05f; // 50ms
        #endregion

        #region 필드
        private object m_lock_command = new object();
        private int gameState = 0;              // 0: 대기, 1: 진행 중, 2: 종료
        private bool mInCombat = false;
        private long m_StartTime = 0;           // 게임 시작 시간 (Unix 밀리초)
        private Dictionary<long, List<IGameCommand>> m_dic_Commands = new Dictionary<long, List<IGameCommand>>();
        private GameMap _gameMap;               // GameMap 객체로 맵 관련 데이터와 로직 위임
        private MapService _mapService;
        private DBManager _dbManager;
        protected GamePlayer[] players = new GamePlayer[MAX_PLAYERS];
        #endregion

        #region 생성자
        public Game()
        {
            _dbManager = DBManager.Instance;
            _mapService = MapService.Instance;
        }
        #endregion

        #region 커맨드 관리
        public void EnqueueCommand(IGameCommand command)
        {
            if (command == null)
                return;
            lock (m_lock_command)
            {
                long tick = command.TickNumber;
                if (!m_dic_Commands.ContainsKey(tick))
                    m_dic_Commands.Add(tick, new List<IGameCommand>());

                m_dic_Commands[tick].Add(command);
            }
        }
        #endregion

        #region 플레이어 관리
        public void UserJoin(ClientSession player)
        {
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                if (players[i] != null)
                    continue;

                players[i].Initilaize(player);
            }
        }

        public void UserLeave(ClientSession player)
        {
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                // 플레이어가 null 이 아니고. 입력받은 유저와 같을때 null로 변경
                if (players[i] != null ? players[i].Equals(player) : false)
                {
                    players[i].Cleanup();
                    break;
                }
            }
        }
        #endregion

        #region 게임 초기화 및 루프
        public async Task StartGame(int mapIndex = 0)
        {
            // 게임 시작 로직 구현

            // 맵 데이터 로드
            MapData staticMapData = _mapService.LoadMapData(mapIndex);
            if (staticMapData == null)
            {
                Console.WriteLine("[Game] Invalid map index.");
                return;
            }

            // GameMap 객체 생성 및 초기화
            _gameMap = new GameMap(staticMapData);

            m_StartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // 게임 루프 시작
            await GameLoop();
        }

        /*
         * 고정 틱 레이트: 20 TPS (50ms/틱)
         * deltaTime : 항상 0.05초로 고정
         * 틱 카운터 : 게임 시작부터 누적
         */
        async Task GameLoop()
        {
            // 틱 레이트 설정 및 초기화
            float deltaTime = FIXED_TICK_RATE;
            int tickCount = 0;
            Console.WriteLine("[Game] Game started.");

            // 게임 루프
            while (gameState != GAMESTATE_ENDED)
            {
                // 만약 gameState가 대기 상태라면, 대기
                if (gameState == GAMESTATE_WAITING)
                {
                    await Task.Delay(100); // 100ms 대기
                    continue;
                }

                var tickStartTime = DateTime.UtcNow;
                // 게임 상태 업데이트
                UpdateGameState(deltaTime, tickCount);
                tickCount++;
                // 다음 틱까지 대기
                var tickEndTime = DateTime.UtcNow;
                var elapsed = (tickEndTime - tickStartTime).TotalSeconds;
                var delay = deltaTime - elapsed;
                if (delay > 0)
                    await Task.Delay(TimeSpan.FromSeconds(delay));
            }
        }

        // 경과 시간(밀리초) 계산
        public long GetElapsedTime()
        {
            long currentTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return currentTimeMs - m_StartTime;
        }

        // 현재 틱을 계산하는 메서드
        public long GetCurrentTick()
        {
            long elapsedTimeMs = GetElapsedTime();
            return elapsedTimeMs / (long)(FIXED_TICK_RATE * 1000);
        }

        public void UpdateGameState(float deltaTime, int tickCount)
        {
            // 현재 틱 기반 처리 추가
            long currentTick = GetCurrentTick();

            CommandProcess();
            if (currentTick % 4 == 0)  // 200ms마다 실행
            {
                ResourceProduction();
            }
            ProductionProcess();
            MovementProcess();

            if (mInCombat)
            {
                CombatProcess();
            }
            ConquerProcess();

            if (currentTick % 10 == 0) // 500ms마다 실행
            {
                CheckWinCondition();
                if (gameState == GAMESTATE_ENDED)
                {
                    Console.WriteLine("[Game] Game ended.");
                    return;
                }
            }

            BrodcastEvent();
        }
        #endregion

        #region 게임 시스템 프로세스
        /// <summary>
        /// 커맨드 처리
        /// </summary>
        void CommandProcess()
        {

        }

        /// <summary>
        /// 자원 생산 처리
        /// </summary>
        void ResourceProduction()
        {

        }

        /// <summary>
        /// 생산 처리
        /// </summary>
        void ProductionProcess()
        {

        }

        /// <summary>
        /// 이동 처리
        /// </summary>
        void MovementProcess()
        {

        }

        /// <summary>
        /// 전투 처리
        /// </summary>
        void CombatProcess()
        {

        }

        /// <summary>
        /// 점령 처리
        /// </summary>
        void ConquerProcess()
        {

        }

        /// <summary>
        /// 승리 조건 확인
        /// </summary>
        void CheckWinCondition()
        {

        }

        /// <summary>
        /// 상태 브로드캐스트
        /// </summary>
        void BrodcastEvent()
        {

        }
        #endregion
    }
}