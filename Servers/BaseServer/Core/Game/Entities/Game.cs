using BaseServer.Core.Game.Managers;
using BaseServer.Core.Game.Session;
using BaseServer.Database;
using CommonLib.Commands; // IGameCommand
using CommonLib.TableData; // MapData, Planet
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BaseServer.Core.Game.Entities
{
    public class FleetController
    {
        private int nextInstanceId = 0; 

        public int GetNextFleetId()
        {
            if (nextInstanceId == int.MaxValue)
            {
                nextInstanceId = 0;
                while (fleets.Keys.Contains(nextInstanceId))
                {
                    nextInstanceId++;
                }
                return nextInstanceId;
            }
            return nextInstanceId++; 
        }
        private Dictionary<int, Fleet> fleets = new Dictionary<int, Fleet>();

        public void AddFleet(Fleet fleet)
        {
            if (fleet == null)
            {
                Console.WriteLine($"[Game][FleetController] Fleet is null:");
                return;
            }

            fleets.Add(fleet.ID, fleet);
            Console.WriteLine($"Fleet {fleet.ID} added to FleetController");
        }

        public Fleet GetFleet(int fleetId)
        {
            return fleets.TryGetValue(fleetId, out Fleet fleet) ? fleet : null;
        }

        public void RemoveFleet(Fleet fleet)
        {
            if (fleet == null)
            {
                Console.WriteLine($"[Game][FleetController] Fleet is null:");
                return;
            }
            int fleetid = fleet.ID;
            fleets.Remove(fleetid);
            Console.WriteLine($"Fleet {fleetid} removed from FleetController");
        }
    }

    public class Fleet
    {
        private int _insctanceId;
        public int ID => _insctanceId;
        private FleetInfoData data;
        public FleetInfoData Data => data;

        public Fleet(FleetInfoData data)
        {
            this.data = data;
        }

        public void SetID(int id)
        {
            this._insctanceId = id;
        }
    }

    public class ProduceController
    {
        // 생산 중인 함대 정보 (PlayerId -> 생산 중인 Fleet 리스트)
        private Dictionary<int, List<ProductionInfo>> productionQueue = new Dictionary<int, List<ProductionInfo>>();

        private DB_Table _db;
        private FleetController fleetController;

        public ProduceController(DBManager db, FleetController fleetController)
        {
            _db = db.Table;
            this.fleetController = fleetController;
        }

        // 생산 요청 처리
        public void RequestProcess(ProductionInfoData command, int playerId)
        {
            // 플레이어의 생산 큐가 없으면 생성
            if (!productionQueue.ContainsKey(playerId))
            {
                productionQueue[playerId] = new List<ProductionInfo>();
            }

            FleetInfoData fleetData = GetFleetDataFromDB(command.Targetid);
            if (fleetData == null)
            {
                Console.WriteLine($"ProductionInfoData not found: {command.Targetid}");
                return;
            }

            ProductionInfo production = new ProductionInfo
            {
                PlayerId = playerId,
                Fleet = new Fleet(fleetData),
                StartTime = DateTime.Now,
                ProductionTime = command.ProductionTime,
                RemainingTime = command.ProductionTime
            };

            productionQueue[playerId].Add(production);
            Console.WriteLine($"Player {playerId} started producing fleet {production.Fleet.ID}");
        }

        // 매 틱마다 호출되어 생산 상태 업데이트
        public void HandleUpdateLoop(float deltaTime)
        {
            foreach (var kvp in productionQueue)
            {
                int playerId = kvp.Key;
                List<ProductionInfo> productions = kvp.Value;

                // 완료된 생산 목록
                List<ProductionInfo> completedProductions = new List<ProductionInfo>();

                // 각 생산 항목의 남은 시간 감소
                foreach (var production in productions)
                {
                    production.RemainingTime -= deltaTime;

                    // 생산 완료 체크
                    if (production.RemainingTime <= 0)
                    {
                        completedProductions.Add(production);
                    }
                }

                // 완료된 생산 처리
                foreach (var completed in completedProductions)
                {
                    HandleProduceComplete(completed);
                    productions.Remove(completed);
                }
            }
        }

        // 생산 완료 처리
        private void HandleProduceComplete(ProductionInfo production)
        {
            Console.WriteLine($"Fleet {production.Fleet.ID} production completed for player {production.PlayerId}");

            // FleetController로 완성된 Fleet 전달
            fleetController.AddFleet(production.Fleet);

            // DB에 생산 완료 기록 (필요시)
            SaveProductionToDB(production);
        }

        // DB에서 함대 데이터 가져오기
        private ProductionInfoData GetProductionInfoDataFromDB(int fleetType)
        {
            if(_db.Production_info.ContainsKey(fleetType))
                return _db.Production_info[fleetType];

            return null;
        }

        // DB에서 함대 데이터 가져오기
        private FleetInfoData GetFleetDataFromDB(int fleetType)
        {
            if (_db.Fleet_info.ContainsKey(fleetType))
                return _db.Fleet_info[fleetType];

            return null;
        }

        // DB에 생산 완료 기록
        private void SaveProductionToDB(ProductionInfo production)
        {
            // DB에 생산 완료 정보 저장
        }

        // 플레이어의 현재 생산 목록 조회
        public List<ProductionInfo> GetPlayerProductions(int playerId)
        {
            return productionQueue.TryGetValue(playerId, out var productions)
                ? new List<ProductionInfo>(productions)
                : new List<ProductionInfo>();
        }
    }

    // 생산 정보 클래스
    public class ProductionInfo
    {
        public int PlayerId { get; set; }
        public Fleet Fleet { get; set; }
        public DateTime StartTime { get; set; }
        public float ProductionTime { get; set; }  // 총 생산 시간 (초)
        public float RemainingTime { get; set; }   // 남은 생산 시간 (초)

        public float Progress => 1f - (RemainingTime / ProductionTime);
    }
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

        // 커맨드 버퍼링을 위한 상수
        private const int COMMAND_BUFFER_TICKS = 3; // 미래에 명령을 등록할 틱 수
        private const int COMMAND_HISTORY_LIMIT = 100; // 처리된 틱 이력 제한
        #endregion

        #region 필드
        private object m_lock_command = new object();
        private int gameState = 0;              // 0: 대기, 1: 진행 중, 2: 종료
        private bool mInCombat = false;
        private long m_StartTime = 0;           // 게임 시작 시간 (Unix 밀리초)
        private Dictionary<long, List<Command>> m_dic_Commands = new Dictionary<long, List<Command>>();
        private GameMap _gameMap;               // GameMap 객체로 맵 관련 데이터와 로직 위임
        private MapManager _mapService;
        private DBManager _dbManager;
        private ProduceController produceController;
        private FleetController fleetController;
        protected GamePlayer[] players = new GamePlayer[MAX_PLAYERS];
        private long _tickCount;

        // 틱 처리 상태 추적
        private HashSet<long> _processedTicks = new HashSet<long>();

        // 세마포어 추가
        private readonly SemaphoreSlim m_semaphore_command = new SemaphoreSlim(1, 1);
        #endregion

        #region 생성자
        public Game()
        {
            _dbManager = DBManager.Instance;
            _mapService = MapManager.Instance;
            fleetController = new FleetController();
            produceController = new ProduceController(_dbManager, fleetController);
        }
        #endregion

        #region 커맨드 관리
        public async Task EnqueueCommand(Command command)
        {
            if (command == null)
                return;
            await AsyncEnqueueCommand(command);
        }

        private async Task AsyncEnqueueCommand(Command command)
        {
            if (command == null)
                return;

            // 비동기적으로 세마포어 획득 시도
            await m_semaphore_command.WaitAsync();
            try
            {
                long currentTick = GetCurrentTick();

                // 미래 틱 계산 (현재 + 버퍼)
                long targetTick = currentTick + COMMAND_BUFFER_TICKS;

                // 이미 처리된 틱인지 확인
                if (_processedTicks.Contains(targetTick))
                {
                    // 이미 처리된 틱이면 추가 버퍼링
                    targetTick++;
                    Console.WriteLine($"[Game] Target tick already processed, registering for next tick: {targetTick}");
                }

                if (!m_dic_Commands.ContainsKey(targetTick))
                    m_dic_Commands.Add(targetTick, new List<Command>());

                m_dic_Commands[targetTick].Add(command);

                // 로그 추가
                Console.WriteLine($"[Game] Command registered for tick {targetTick} (current: {currentTick})");
            }
            finally
            {
                m_semaphore_command.Release();
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

                // 새 GamePlayer 객체 생성 후 초기화
                players[i] = new GamePlayer();
                players[i].Initilaize(player);

                Console.WriteLine($"[Game] Player joined at slot {i}");
                return; // 플레이어 추가 완료 후 메서드 종료
            }

            // 모든 슬롯이 차 있을 경우
            Console.WriteLine("[Game] Cannot join: game is full");
        }

        public void UserLeave(ClientSession player)
        {
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                // 플레이어가 null 이 아니고. 입력받은 유저와 같을때 null로 변경
                if (players[i] != null && players[i].Equals(player))
                {
                    players[i].Cleanup();
                    players[i] = null;
                    Console.WriteLine($"[Game] Player left from slot {i}");
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

            // 게임 상태 설정
            gameState = GAMESTATE_RUNNING;
            m_StartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // 처리된 틱 초기화
            _processedTicks.Clear();

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
            _tickCount = 0;
            Console.WriteLine("[Game] Game started.");

            try
            {
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
                    UpdateGameState(deltaTime, _tickCount);
                    _tickCount++;

                    // 다음 틱까지 대기
                    var tickEndTime = DateTime.UtcNow;
                    var elapsed = (tickEndTime - tickStartTime).TotalSeconds;
                    var delay = deltaTime - elapsed;
                    if (delay > 0)
                        await Task.Delay(TimeSpan.FromSeconds(delay));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Game] Error in game loop: {ex.Message}");
            }
            finally
            {
                // 게임 종료 시 리소스 정리
                Cleanup();
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

        public void UpdateGameState(float deltaTime, long tickCount)
        {
            // 현재 틱 기반 처리 추가
            long currentTick = GetCurrentTick();

            // 현재 틱을 처리된 것으로 표시
            lock (m_lock_command)
            {
                _processedTicks.Add(currentTick);

                // 메모리 관리를 위해 오래된 처리 이력 정리
                if (_processedTicks.Count > COMMAND_HISTORY_LIMIT)
                {
                    _processedTicks.RemoveWhere(tick => tick < currentTick - (COMMAND_HISTORY_LIMIT / 2));
                }
            }

            CommandProcess(currentTick);
            if (currentTick % 4 == 0)  // 200ms마다 실행
            {
                ResourceProduction(currentTick);
            }
            ProductionProcess(currentTick);
            MovementProcess(currentTick);

            if (mInCombat)
            {
                CombatProcess(currentTick);
            }
            ConquerProcess(currentTick);

            if (currentTick % 10 == 0) // 500ms마다 실행
            {
                CheckWinCondition(currentTick);
                if (gameState == GAMESTATE_ENDED)
                {
                    Console.WriteLine("[Game] Game ended.");
                    return;
                }
            }

            BroadcastEvent(currentTick);
        }

        // 게임 정리 메서드 추가
        public void Cleanup()
        {
            // 게임 상태를 종료로 설정
            gameState = GAMESTATE_ENDED;

            // 플레이어 정리
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                if (players[i] != null)
                {
                    players[i].Cleanup();
                    players[i] = null;
                }
            }

            // 명령 큐 정리
            lock (m_lock_command)
            {
                m_dic_Commands.Clear();
                _processedTicks.Clear();
            }

            // SemaphoreSlim 해제
            m_semaphore_command.Dispose();

            // 맵 정리
            if (_gameMap != null)
            {
                _gameMap = null;
            }

            Console.WriteLine("[Game] Resources cleaned up");
        }
        #endregion

        #region 게임 시스템 프로세스
        /// <summary>
        /// 커맨드 처리
        /// </summary>
        void CommandProcess(long currentTick)
        {
            List<Command> tickCommands = new List<Command>();

            // 스레드 안전하게 명령 가져오기
            lock (m_lock_command)
            {
                if (m_dic_Commands.ContainsKey(currentTick))
                {
                    tickCommands.AddRange(m_dic_Commands[currentTick]);
                    m_dic_Commands.Remove(currentTick);
                }
            }

            // 명령 실행
            foreach (Command command in tickCommands)
            {
                try
                {
                    switch(command.Type)
                    {
                        case CommonLib.Commands.GameCommandType.MoveFleet:
                            {
                                if(command is not MoveFleetCommand)
                                    throw new Exception("command Type Error. command is not MoveFleetCommand!!");

                                var mvfcommand = (MoveFleetCommand)command;
                            }break;
                        case CommonLib.Commands.GameCommandType.ProduceFleet:
                            {
                                if (command is not ProduceFleetCommand)
                                    throw new Exception("command Type Error. command is not ProduceFleetCommand!!");

                                var pdfcommand = (ProduceFleetCommand)command;
                                int playerId = command.PlayerId;
                                if(_dbManager.Table.Production_info.ContainsKey(pdfcommand.TargetId))
                                    throw new Exception("command Type Error. command is not ProduceFleetCommand!!");

                                var produceFleetData = _dbManager.Table.Production_info[pdfcommand.TargetId];
                                produceController.RequestProcess(produceFleetData, playerId);
                            }
                            break;
                    }

                    // 디버깅용 로그
                    Console.WriteLine($"[Game] Command executed: {command.GetType().Name}");
                }
                catch (Exception ex)
                {
                    // 명령 실행 중 예외 처리
                    Console.WriteLine($"[Game] Error executing command: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 자원 생산 처리
        /// </summary>
        void ResourceProduction(long currentTick)
        {
            // 자원 생산 로직 구현
            // 예: 플레이어 자원 업데이트, 건물 생산량 계산 등
        }

        /// <summary>
        /// 생산 처리
        /// </summary>
        void ProductionProcess(long currentTick)
        {
            // 생산 로직 구현
            // 예: 건물, 유닛 생산 진행 상황 업데이트

            produceController.HandleUpdateLoop(currentTick);
        }

        /// <summary>
        /// 이동 처리
        /// </summary>
        void MovementProcess(long currentTick)
        {
            // 이동 로직 구현
            // 예: 유닛 위치 업데이트, 경로 계산 등   
        }

        /// <summary>
        /// 전투 처리
        /// </summary>
        void CombatProcess(long currentTick)
        {
            // 전투 로직 구현
            // 예: 유닛 간 전투 처리, 데미지 계산 등
        }

        /// <summary>
        /// 점령 처리
        /// </summary>
        void ConquerProcess(long currentTick)
        {
            // 점령 로직 구현
            // 예: 행성 점령 상태 업데이트
        }

        /// <summary>
        /// 승리 조건 확인
        /// </summary>
        void CheckWinCondition(long currentTick)
        {
            // 승리 조건 확인 로직 구현
            // 예: 점령 상태, 자원량, 유닛 수 등을 기준으로 승리 여부 판단
        }

        /// <summary>
        /// 상태 브로드캐스트
        /// </summary>
        void BroadcastEvent(long currentTick)
        {
            // 상태 브로드캐스트 로직 구현
            // 예: 게임 상태를 모든 클라이언트에 전송

            // 필요한 경우 비동기로 처리 가능
            // _ = Task.Run(() => SendGameStateToClients());
        }
        #endregion
    }
}