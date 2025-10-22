using BaseServer;
using CommonLib.Commands; // IGameCommand
using CommonLib.Services; // MapService
using CommonLib.TableData; // MapData, Planet
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaseServer
{
    public class Game
    {
        public const int MAX_PLAYERS = 2;

        public const int PLAYER_FACTION_1 = 1;
        public const int PLAYER_FACTION_2 = -1;
        public const int PLAYER_FACTION_NONE = 0;

        const int GAMESTATE_WAITING = 0;
        const int GAMESTATE_RUNNING = 1;
        const int GAMESTATE_ENDED = 2;

        private object m_lock_command = new object();

        private int gameState = 0; // 0: 대기, 1: 진행 중, 2: 종료
                                   // 전투 중 여부
        private bool mInCombat = false;

        private Dictionary<long, List<IGameCommand>> m_dic_Commands = new Dictionary<long, List<IGameCommand>>();
        private GameMap _gameMap; // GameMap 객체로 맵 관련 데이터와 로직 위임
        private MapManager _mapManager;
        private DBManager _dbManager;

        protected ClientSession[] players = new ClientSession[MAX_PLAYERS];

        public Game() 
        {
            _dbManager = DBManager.Instance;
            _mapManager = MapManager.Instance;
        }

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

        public void UserJoin(ClientSession player)
        {
            // 플레이어가 게임에 참여할 때의 로직 구현
        }

        public void UserLeave(ClientSession player)
        {
            // 플레이어가 게임에서 나갈 때의 로직 구현
        }

        public async Task StartGame(int mapIndex = 0)
        {
            // 게임 시작 로직 구현

            // 맵 데이터 로드 (MapService 사용)
            MapData staticMapData = _mapManager.LoadMapData(mapIndex);
            if (staticMapData == null)
            {
                Console.WriteLine("[Game] Invalid map index.");
                return;
            }

            // GameMap 객체 생성 및 초기화
            _gameMap = new GameMap(staticMapData);

            // 게임 루프 시작
            await GameLoop();
        }

        /*
         * 고정 틱 레이트: 20 TPS (50ms/틱)
         * deltaTime : 항상 0.05초로 고정
         * 틱 카운터 : 게임 시작부터 누적
         */
        public const float FIXED_TICK_RATE = 0.05f; // 50ms
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

        public void UpdateGameState(float deltaTime, int tickCount)
        {
            CommandProcess();
            if (tickCount % 4 == 0)  // 200ms마다 실행
            {
                ResourceProduction();
            }
            ProductionProcess();
            CombatProcess();

            if (mInCombat)
            {
                CombatProcess();
            }
            ConquerProcess();

            if (tickCount % 10 == 0) // 1초마다 실행
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
    }
}