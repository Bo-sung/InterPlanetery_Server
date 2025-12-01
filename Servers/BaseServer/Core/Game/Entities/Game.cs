using BaseServer.Core.Game.Managers;
using BaseServer.Core.Game.Session;
using BaseServer.Database;
using CommonLib.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using static BaseServer.Core.Game.Entities.GameState;

namespace BaseServer.Core.Game.Entities
{

    /// <summary>
    /// 게임 상태 클래스. 매 틱마다 클라 전송.
    /// </summary>
    public class GameState
    {
        public class Player
        {
            public int id = -1;
            public FleetInfo[] fleets;
        }

        public class FleetInfo
        {
            public CommonLib.Vector2 position;
            public int state = 0;   // 0 = Idle, 1 = battle. 2 = move
            public float HP = 0;
            public CommonLib.Vector2 target;    // idle일때는 무시
        }

        public class Planet
        {
            public int owner = -1;
            public float conquestProgress = 0;
        }

        public int state = 0; // 0 = 준비. 1 = 진행중, 2 = 종료

        public Player[] players;
        public Planet[] planets;

        public GameState(GameMap gameMap, GamePlayer[] playerarr, Dictionary<long, Fleet> fleets, int curr_state)
        {
            List<Planet> planetList = new List<Planet>();
            foreach (var item in gameMap.Planets)
            {
                Planet p = new Planet();
                p.owner = item.OwnerId;
                p.conquestProgress = item.ConquestProgress;
                planetList.Add(p);
            }

            planets = planetList.ToArray();

            List<Player> playerList = new List<Player>();
            foreach (var item in playerarr)
            {
                Player p = new Player();
                p.id = item.ID;

                List<FleetInfo> fleetList = new List<FleetInfo>();

                // fleets 에서 owner Id와 현재 Id와 매치되는것들만 추출
                foreach (var fitem in fleets.Values.ToList().FindAll(x => x.Owner == p.id))
                {
                    FleetInfo fleet = new FleetInfo();
                    fleet.state = (int)fitem.State;
                    fleet.HP = fitem.CurHealth;
                    fleet.position = fitem.Position;
                    fleet.target = fitem.MoveTarget;
                    fleetList.Add(fleet);
                }
                p.fleets = fleetList.ToArray();
                playerList.Add(p);
            }
            players = playerList.ToArray();
            state = curr_state;
        }
    }

    /// <summary>
    /// 게임 인스턴스 클래스
    /// - 고정 틱 레이트(20 TPS)로 동작하는 게임 루프 관리
    /// - 최대 2명의 플레이어 지원
    /// - 명령 큐 기반의 결정론적 게임 시뮬레이션
    /// </summary>
    public class Game : IDisposable
    {
        #region 상수
        // 플레이어 관련 상수
        public const int MAX_PLAYERS = 2;
        public const int PLAYER_FACTION_1 = 1;      // 플레이어 1 진영
        public const int PLAYER_FACTION_2 = -1;     // 플레이어 2 진영
        public const int PLAYER_FACTION_NONE = 0;   // 중립 진영

        // 게임 상태 상수
        private const int GAMESTATE_WAITING = 0;    // 대기 중 (플레이어 입장 대기)
        private const int GAMESTATE_RUNNING = 1;    // 게임 진행 중
        private const int GAMESTATE_ENDED = 2;      // 게임 종료

        // 틱 관련 상수
        public const float FIXED_TICK_RATE = 0.05f; // 50ms (20 TPS = Ticks Per Second)
        private const int COMMAND_BUFFER_TICKS = 3; // 명령을 미래 3틱 후에 실행 (네트워크 지연 보상)
        private const int COMMAND_HISTORY_LIMIT = 100; // 처리된 틱 히스토리 최대 보관 개수

        // 업데이트 주기 상수
        private const int RESOURCE_UPDATE_INTERVAL = 4;  // 4틱마다 자원 생산 (200ms)
        private const int WIN_CHECK_INTERVAL = 10;       // 10틱마다 승리 조건 체크 (500ms)

        // 전투 관련 상수
        private const float ATTACK_TICK_INTERVAL = 1f; // 1초마다 공격
        private const float OCCUPY_DURATION = 5f;      // 점령 5초
        #endregion

        #region 필드
        // 동기화 객체
        private readonly SemaphoreSlim m_commandSemaphore = new SemaphoreSlim(1, 1); // 명령 큐 접근 동기화

        private long m_CASHED_NEXTFLEET_ID = 0;
        // 게임 상태
        private int m_gameState = GAMESTATE_WAITING;    // 현재 게임 상태
        private bool m_inCombat = false;                // 전투 중인지 여부
        private long m_startTime = 0;                   // 게임 시작 시간 (Unix 밀리초)
        private long m_tickCount = 0;                   // 현재 틱 카운터 (게임 시작부터 누적)
        private bool m_disposed = false;                // Dispose 호출 여부

        // 명령 관리
        private Dictionary<long, List<Command>> m_commandQueue = new Dictionary<long, List<Command>>();
        // Key: 틱 번호, Value: 해당 틱에 실행될 명령 리스트

        private HashSet<long> m_processedTicks = new HashSet<long>();
        // 이미 처리된 틱 번호들 (중복 처리 방지용)

        // 게임 시스템 컴포넌트
        private GameMap? m_gameMap;                         // 게임 맵 (행성, 경로 정보)
        private readonly MapManager m_mapManager;           // 맵 데이터 로더
        private readonly DBManager m_dbManager;             // 데이터베이스 매니저
        private readonly ProduceController m_produceController;  // 함대 생산 컨트롤러
        //private readonly FleetController m_fleetController;      // 함대 이동/전투 컨트롤러
        private readonly Dictionary<long, Fleet> m_dic_fleets = new Dictionary<long, Fleet>();
        private readonly GamePlayer[] m_players = new GamePlayer[MAX_PLAYERS]; // 플레이어 배열

        // 게임 루프 제어
        private CancellationTokenSource? m_gameLoopCts;     // 게임 루프 취소 토큰
        #endregion

        #region 프로퍼티
        /// <summary>게임이 실행 중인지 여부</summary>
        public bool IsRunning => m_gameState == GAMESTATE_RUNNING;

        /// <summary>게임이 종료되었는지 여부</summary>
        public bool IsEnded => m_gameState == GAMESTATE_ENDED;

        /// <summary>현재 틱 번호</summary>
        public long CurrentTick => m_tickCount;
        #endregion

        #region 생성자
        /// <summary>
        /// Game 인스턴스 생성자
        /// - 싱글톤 매니저들 초기화
        /// - 컨트롤러 생성
        /// - 플레이어 슬롯 사전 생성
        /// </summary>
        public Game()
        {
            // 싱글톤 매니저 인스턴스 가져오기
            m_dbManager = DBManager.Instance;
            m_mapManager = MapManager.Instance;

            // 게임 컨트롤러 생성
            m_produceController = new ProduceController(m_dbManager);

            // 플레이어 슬롯 미리 생성 (최대 2명)
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                m_players[i] = new GamePlayer();
            }

            Console.WriteLine("[Game] Game instance created");
        }
        #endregion

        #region 커맨드 관리
        /// <summary>
        /// 명령을 게임 큐에 추가
        /// - 명령은 현재 틱 + COMMAND_BUFFER_TICKS 후에 실행됨
        /// - 네트워크 지연을 보상하기 위한 버퍼링 메커니즘
        /// </summary>
        /// <param name="command">실행할 명령</param>
        public async Task EnqueueCommand(Command command)
        {
            // null 체크
            if (command == null)
            {
                Console.WriteLine("[Game] Cannot enqueue null command");
                return;
            }

            // 게임이 실행 중인지 확인
            if (m_gameState != GAMESTATE_RUNNING)
            {
                Console.WriteLine($"[Game] Cannot enqueue command - game not running (state: {m_gameState})");
                return;
            }

            await EnqueueCommandInternal(command);
        }

        /// <summary>
        /// 명령 큐에 명령 추가 (내부 구현)
        /// - Thread-safe하게 명령 큐에 추가
        /// - 이미 처리된 틱이면 다음 틱으로 자동 이동
        /// </summary>
        private async Task EnqueueCommandInternal(Command command)
        {
            // 세마포어로 명령 큐 접근 동기화
            await m_commandSemaphore.WaitAsync();
            try
            {
                // 현재 틱 계산
                long currentTick = GetCurrentTick();

                // 목표 틱 = 현재 틱 + 버퍼 (네트워크 지연 보상)
                long targetTick = currentTick + COMMAND_BUFFER_TICKS;

                // 이미 처리된 틱이면 다음 틱으로 이동
                // (지연된 명령이 과거 틱에 등록되는 것을 방지)
                while (m_processedTicks.Contains(targetTick))
                {
                    targetTick++;
                }

                // 해당 틱의 명령 리스트가 없으면 생성
                if (!m_commandQueue.ContainsKey(targetTick))
                {
                    m_commandQueue[targetTick] = new List<Command>();
                }

                // 명령 추가
                m_commandQueue[targetTick].Add(command);

                Console.WriteLine($"[Game] Command queued for tick {targetTick} " +
                    $"(Type: {command.Type}, Player: {command.PlayerId}, Current: {currentTick})");
            }
            finally
            {
                // 세마포어 해제
                m_commandSemaphore.Release();
            }
        }
        #endregion

        #region 플레이어 관리
        /// <summary>
        /// 플레이어를 게임에 추가
        /// - 빈 슬롯을 찾아 플레이어 세션 연결
        /// - 최대 2명까지 입장 가능
        /// </summary>
        /// <param name="session">클라이언트 세션</param>
        /// <param name="commandSender">명령 전송 인터페이스</param>
        /// <returns>입장 성공 여부</returns>
        public bool UserJoin(ClientSession session, ICommandSender commandSender)
        {
            // null 체크
            if (session == null || commandSender == null)
            {
                Console.WriteLine("[Game] Cannot join - null session or command sender");
                return false;
            }

            // 빈 슬롯 찾기
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                // 슬롯이 비어있으면 (IsValid == false)
                if (m_players[i] != null && !m_players[i].IsValid)
                {
                    // 플레이어 초기화 및 세션 연결
                    m_players[i].Initilaize(session, commandSender);
                    Console.WriteLine($"[Game] Player {session.SessionId} joined at slot {i}");
                    return true;
                }
            }

            // 모든 슬롯이 찼을 경우
            Console.WriteLine("[Game] Cannot join - game is full");
            return false;
        }

        /// <summary>
        /// 플레이어를 게임에서 제거
        /// - 세션을 찾아 해당 슬롯 정리
        /// </summary>
        /// <param name="session">제거할 플레이어 세션</param>
        /// <returns>제거 성공 여부</returns>
        public bool UserLeave(ClientSession session)
        {
            if (session == null)
                return false;

            // 해당 세션을 가진 플레이어 찾기
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                if (m_players[i] != null && m_players[i].Session == session)
                {
                    // 플레이어 슬롯 정리
                    m_players[i].Cleanup();
                    Console.WriteLine($"[Game] Player {session.SessionId} left from slot {i}");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 현재 유효한 플레이어 수 반환
        /// </summary>
        private int GetValidPlayerCount()
        {
            return m_players.Count(p => p != null && p.IsValid);
        }
        #endregion

        #region 게임 초기화 및 루프
        /// <summary>
        /// 게임 시작
        /// - 맵 데이터 로드
        /// - 컨트롤러 초기화
        /// - 게임 루프 시작
        /// </summary>
        /// <param name="mapIndex">로드할 맵 인덱스</param>
        /// <returns>시작 성공 여부</returns>
        public async Task<bool> StartGame(int mapIndex = 0)
        {
            // 게임 상태 확인 (WAITING 상태에서만 시작 가능)
            if (m_gameState != GAMESTATE_WAITING)
            {
                Console.WriteLine($"[Game] Cannot start - invalid state: {m_gameState}");
                return false;
            }

            // 맵 데이터 로드
            MapData? staticMapData = m_mapManager.LoadMapData(mapIndex);
            if (staticMapData == null)
            {
                Console.WriteLine($"[Game] Failed to load map data (index: {mapIndex})");
                return false;
            }

            // GameMap 객체 생성 (행성, 경로 정보 초기화)
            m_gameMap = new GameMap(staticMapData);
            m_produceController.OnProductionFinish += HandleOnProductionFinish;

            // 게임 상태 초기화
            m_gameState = GAMESTATE_RUNNING;                              // 게임 실행 상태로 변경
            m_startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // 시작 시간 기록
            m_tickCount = 0;                                              // 틱 카운터 초기화
            m_processedTicks.Clear();                                     // 처리된 틱 히스토리 초기화
            m_gameLoopCts = new CancellationTokenSource();                // 게임 루프 취소 토큰 생성

            Console.WriteLine($"[Game] Game started with map {mapIndex}");

            // 게임 루프를 별도 Task로 시작 (비동기 실행)
            _ = Task.Run(() => GameLoop(m_gameLoopCts.Token));

            return true;
        }

        private void HandleOnProductionFinish(Fleet output)
        {
            m_dic_fleets.Add(output.ID, output);
        }

        /// <summary>
        /// 게임 메인 루프
        /// - 고정 틱 레이트(20 TPS)로 동작
        /// - 각 틱마다 게임 상태 업데이트
        /// - 정확한 타이밍 제어를 위한 대기 시간 계산
        /// </summary>
        /// <param name="cancellationToken">취소 토큰</param>
        private async Task GameLoop(CancellationToken cancellationToken)
        {
            const float deltaTime = FIXED_TICK_RATE; // 고정 델타타임 (50ms)

            Console.WriteLine("[Game] Game loop started");

            try
            {
                // 게임이 실행 중이고 취소되지 않았으면 계속 루프
                while (m_gameState == GAMESTATE_RUNNING && !cancellationToken.IsCancellationRequested)
                {
                    var tickStartTime = DateTime.UtcNow; // 틱 시작 시간 기록

                    // === 게임 상태 업데이트 (핵심 로직) ===
                    UpdateGameState(deltaTime, m_tickCount);
                    m_tickCount++; // 틱 카운터 증가

                    // === 다음 틱까지 정확한 대기 시간 계산 ===
                    var tickEndTime = DateTime.UtcNow; // 틱 종료 시간
                    var elapsed = (tickEndTime - tickStartTime).TotalSeconds; // 실제 소요 시간
                    var delay = deltaTime - elapsed; // 다음 틱까지 남은 시간

                    if (delay > 0)
                    {
                        // 남은 시간이 있으면 대기
                        await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
                    }
                    else if (delay < -0.01) // 10ms 이상 지연되면 경고
                    {
                        // 틱 처리가 목표 시간보다 오래 걸린 경우
                        Console.WriteLine($"[Game] Tick {m_tickCount} lagging: {-delay * 1000:F2}ms behind");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 게임 루프가 취소된 경우 (정상 종료)
                Console.WriteLine("[Game] Game loop cancelled");
            }
            catch (Exception ex)
            {
                // 예상치 못한 예외 발생 (비정상 종료)
                Console.WriteLine($"[Game] Fatal error in game loop: {ex.Message}\n{ex.StackTrace}");
                m_gameState = GAMESTATE_ENDED;
            }
            finally
            {
                // 게임 루프 종료 시 리소스 정리
                Console.WriteLine("[Game] Game loop ended");
                Cleanup();
            }
        }

        /// <summary>
        /// 게임 시작 이후 경과 시간 계산
        /// </summary>
        /// <returns>경과 시간 (밀리초)</returns>
        public long GetElapsedTime()
        {
            if (m_startTime == 0)
                return 0;

            long currentTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return currentTimeMs - m_startTime;
        }

        /// <summary>
        /// 현재 틱 번호 계산
        /// - 경과 시간을 틱 레이트로 나누어 계산
        /// </summary>
        /// <returns>현재 틱 번호</returns>
        public long GetCurrentTick()
        {
            long elapsedTimeMs = GetElapsedTime();
            return elapsedTimeMs / (long)(FIXED_TICK_RATE * 1000);
        }

        /// <summary>
        /// 게임 상태 업데이트 (매 틱마다 호출)
        /// - 명령 처리
        /// - 각 시스템 업데이트 (생산, 이동, 전투, 점령)
        /// - 승리 조건 체크
        /// - 클라이언트에게 상태 브로드캐스트
        /// </summary>
        /// <param name="deltaTime">고정 델타타임 (50ms)</param>
        /// <param name="tickCount">현재 틱 번호</param>
        private void UpdateGameState(float deltaTime, long tickCount)
        {
            long currentTick = GetCurrentTick();

            // 현재 틱을 처리된 것으로 표시 및 오래된 히스토리 정리
            MarkTickProcessed(currentTick);

            // === 각 시스템 순차 업데이트 ===

            // 1. 명령 처리 (플레이어 입력 처리)
            CommandProcess(currentTick);

            // 2. 자원 생산 (4틱마다 = 200ms)
            if (currentTick % RESOURCE_UPDATE_INTERVAL == 0)
            {
                ResourceProduction(currentTick);
            }

            // 3. 함대 생산 처리
            ProductionProcess(currentTick);

            // 4. 함대 이동 처리
            MovementProcess(currentTick);

            // 5. 전투 처리 (전투 중일 때만)
            if (m_inCombat)
            {
                CombatProcess(currentTick);
            }

            // 6. 행성 점령 처리
            ConquerProcess(currentTick);

            // 7. 승리 조건 체크 (10틱마다 = 500ms)
            if (currentTick % WIN_CHECK_INTERVAL == 0)
            {
                CheckWinCondition(currentTick);
            }

            // 8. 게임 상태를 클라이언트에게 브로드캐스트
            BroadcastEvent(currentTick);
        }

        /// <summary>
        /// 현재 틱을 처리된 것으로 표시
        /// - 중복 처리 방지
        /// - 오래된 틱 히스토리 정리 (메모리 관리)
        /// </summary>
        private void MarkTickProcessed(long currentTick)
        {
            lock (m_commandSemaphore)
            {
                // 현재 틱을 처리된 것으로 추가
                m_processedTicks.Add(currentTick);

                // 메모리 관리: 히스토리가 너무 많이 쌓이면 오래된 것 삭제
                if (m_processedTicks.Count > COMMAND_HISTORY_LIMIT)
                {
                    long threshold = currentTick - (COMMAND_HISTORY_LIMIT / 2);
                    m_processedTicks.RemoveWhere(tick => tick < threshold);
                }
            }
        }

        /// <summary>
        /// 게임 중지 요청
        /// - 게임 루프를 취소하고 종료 상태로 변경
        /// </summary>
        public void StopGame()
        {
            if (m_gameState == GAMESTATE_ENDED)
                return;

            Console.WriteLine("[Game] Stopping game...");
            m_gameState = GAMESTATE_ENDED;
            m_gameLoopCts?.Cancel(); // 게임 루프 취소
        }

        /// <summary>
        /// 게임 리소스 정리
        /// - 플레이어 세션 정리
        /// - 명령 큐 정리
        /// - 맵 정리
        /// </summary>
        private void Cleanup()
        {
            Console.WriteLine("[Game] Cleaning up resources...");

            // 게임 상태 종료
            m_gameState = GAMESTATE_ENDED;

            // 모든 플레이어 정리
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                m_players[i]?.Cleanup();
            }

            // 명령 큐 정리 (thread-safe)
            lock (m_commandSemaphore)
            {
                m_commandQueue.Clear();
                m_processedTicks.Clear();
            }

            // 맵 정리
            m_gameMap = null;

            Console.WriteLine("[Game] Cleanup completed");
        }
        #endregion

        #region 게임 시스템 프로세스
        /// <summary>
        /// 명령 처리 시스템
        /// - 현재 틱에 등록된 명령들을 가져와 실행
        /// - Thread-safe하게 명령 큐에서 꺼내기
        /// </summary>
        /// <param name="currentTick">현재 틱 번호</param>
        private void CommandProcess(long currentTick)
        {
            List<Command>? tickCommands = null;

            // 명령 큐에서 현재 틱의 명령들 가져오기 (thread-safe)
            lock (m_commandSemaphore)
            {
                if (m_commandQueue.TryGetValue(currentTick, out var commands))
                {
                    tickCommands = new List<Command>(commands); // 복사본 생성
                    m_commandQueue.Remove(currentTick); // 큐에서 제거
                }
            }

            // 명령이 없으면 조기 반환
            if (tickCommands == null || tickCommands.Count == 0)
                return;

            // 모든 명령 순차 실행
            foreach (Command command in tickCommands)
            {
                ExecuteCommand(command);
            }
        }

        /// <summary>
        /// 명령 실행 (타입별 분기)
        /// - 명령 타입에 따라 적절한 핸들러 호출
        /// - 예외 발생 시 로그 출력 후 계속 진행
        /// </summary>
        /// <param name="command">실행할 명령</param>
        private void ExecuteCommand(Command command)
        {
            try
            {
                // 명령 타입별 분기
                switch (command.Type)
                {
                    case CommonLib.Commands.GameCommandType.MoveFleet:
                        // 함대 이동 명령 처리
                        HandleMoveFleetCommand(command);
                        break;

                    case CommonLib.Commands.GameCommandType.ProduceFleet:
                        // 함대 생산 명령 처리
                        HandleProduceFleetCommand(command);
                        break;

                    default:
                        // 알 수 없는 명령 타입
                        Console.WriteLine($"[Game] Unknown command type: {command.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                // 명령 실행 중 예외 발생 시 로그 출력
                // 한 명령의 실패가 전체 게임을 멈추지 않도록 함
                Console.WriteLine($"[Game] Error executing command (Type: {command.Type}, Player: {command.PlayerId}): {ex.Message}");
            }
        }

        /// <summary>
        /// 함대 이동 명령 처리
        /// - 명령 타입 검증
        /// - FleetController에게 이동 요청
        /// </summary>
        private void HandleMoveFleetCommand(Command command)
        {
            // 타입 캐스팅 및 검증
            if (command is not MoveFleetCommand moveCommand)
            {
                Console.WriteLine($"[Game] Invalid MoveFleetCommand");
                return;
            }
            var fleetId = moveCommand.TargetFleet;
            var playerId = moveCommand.PlayerId;
            var planetId = moveCommand.TargetPlanetId;
            // 이동 명령 실행
            Console.WriteLine($"[Game] Move fleet command: Player {playerId}, " +
                $"Fleet {fleetId} -> Planet {planetId}");

            if (!m_dic_fleets.ContainsKey(fleetId))
            {
                Console.WriteLine($"[Game] Invalid TargetFleet!! : TargetFleet = {fleetId}");
                return;
            }

            var planet = m_gameMap?.GetPlanet(planetId);
            if (planet == null)
            {
                Console.WriteLine($"[Game] Invalid TargetPlanetId!! : TargetPlanetId = {planetId}");
                return;
            }

            var fleet = m_dic_fleets[fleetId];
            fleet.Navigate(planet, GetCurrentTick());
        }

        /// <summary>
        /// 함대 생산 명령 처리
        /// - 명령 타입 검증
        /// - DB에서 생산 정보 조회
        /// - ProduceController에게 생산 요청
        /// </summary>
        private void HandleProduceFleetCommand(Command command)
        {
            // 타입 캐스팅 및 검증
            if (command is not ProduceFleetCommand produceCommand)
            {
                Console.WriteLine($"[Game] Invalid ProduceFleetCommand");
                return;
            }

            int playerId = command.PlayerId;
            int targetId = produceCommand.TargetId;

            // DB에서 생산 정보 조회
            if (!m_dbManager.Table.Production_info.TryGetValue(targetId, out var produceFleetData))
            {
                Console.WriteLine($"[Game] Invalid production target ID: {targetId}");
                return;
            }
            // 유효한 키 찾을때까지 반복
            for (long id = 0; id < long.MaxValue; ++id)
            {
                // 이미 있으면 패스
                if (m_dic_fleets.ContainsKey(id))
                    continue;
                // 생산 리스트에 있으면 패스
                if (m_produceController.IsOnProduction(id))
                    continue;

                // 생산 컨트롤러에게 생산 요청
                Console.WriteLine($"[Game] Produce fleet command: Player {playerId}, Target {targetId}");
                m_produceController.RequestProcess(produceFleetData, playerId, id);
                break;
            }
        }

        /// <summary>
        /// 자원 생산 처리
        /// - 각 플레이어의 행성에서 자원 생산
        /// - 4틱마다 호출됨 (200ms 주기)
        /// </summary>
        private void ResourceProduction(long currentTick)
        {
            if (m_gameMap == null)
                return;

            foreach (var user in m_players)
            {
                user.Update_Resource(m_gameMap, currentTick);
            }
        }

        /// <summary>
        /// 생산 처리
        /// - 진행 중인 함대 생산 업데이트
        /// - ProduceController에게 위임
        /// </summary>
        private void ProductionProcess(long currentTick)
        {
            m_produceController.ProcessUpdate(currentTick);
        }

        /// <summary>
        /// 이동 처리
        /// - 이동 중인 함대들의 위치 업데이트
        /// - FleetController에게 위임
        /// </summary>
        private void MovementProcess(long currentTick)
        {
            foreach (var fleet in m_dic_fleets)
            {
                fleet.Value.UpdateMovement(currentTick);
                // 전투 범위 체크
                // 함대 리스트.
                var enemys = m_dic_fleets.Values.ToList();
                // 함대 리스트에서 소유자 같은 모든 함선 제거
                enemys.RemoveAll(x => x.Owner == fleet.Value.Owner);
                // 가장 가까운 적을 enemy로 세팅
                foreach (var enemy in enemys)
                {
                    // 만약 적의 적이 있는데. 그게 현재 함대면 패스
                    if (enemy.Enemy != null && enemy.Enemy.Equals(fleet.Value))
                    {
                        continue;
                    }
                    // 공격 범위 체크
                    if (fleet.Value.IsAttackRange(enemy))
                    {
                        if (fleet.Value.Enemy == null)
                        {
                            fleet.Value.SetAttackTarget(enemy);
                            enemy.SetAttackTarget(fleet.Value);
                            continue;
                        }
                        else
                        {
                            float dist1 = CommonLib.Vector2.Distance(fleet.Value.Position, enemy.Position);
                            float dist2 = CommonLib.Vector2.Distance(fleet.Value.Position, fleet.Value.Enemy.Position);
                            // 지금 적이 더 가까우면 교체
                            if (dist1 < dist2)
                            {
                                fleet.Value.SetAttackTarget(enemy);
                                enemy.SetAttackTarget(fleet.Value);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 전투 처리
        /// - 같은 행성에 있는 적 함대 간 전투 진행
        /// - FleetController에게 위임
        /// </summary>
        private void CombatProcess(long currentTick)
        {
            foreach (var fleet in m_dic_fleets)
            {
                fleet.Value.UpdateAttack(currentTick);
            }
        }

        /// <summary>
        /// 점령 처리
        /// - 함대가 적 행성을 점령하는 과정 처리
        /// </summary>
        private void ConquerProcess(long currentTick)
        {
            if (m_gameMap == null)
                return;
            foreach (var planet in m_gameMap.Planets)
            {
            }
        }

        /// <summary>
        /// 승리 조건 확인
        /// - 게임 종료 조건 체크
        /// - 10틱마다 호출됨 (500ms 주기)
        /// </summary>
        private void CheckWinCondition(long currentTick)
        {
            // TODO: 승리 조건 확인 로직 구현
            // 예시:
            // - 한 플레이어가 모든 행성 점령
            // - 한 플레이어의 모든 함대가 파괴됨
            // - 시간 제한 도달 (더 많은 행성 소유한 플레이어 승리)
        }

        /// <summary>
        /// 게임 상태 브로드캐스트
        /// - 현재 게임 상태를 모든 클라이언트에게 전송
        /// - 함대 위치, 행성 소유권, 자원 등
        /// </summary>
        private void BroadcastEvent(long currentTick)
        {
            if (m_gameMap == null)
            {
                Console.WriteLine("[Game] m_gameMap == null");
                return;
            }
            GameState state = new GameState(m_gameMap, m_players, m_dic_fleets, m_gameState);
            foreach (var player in m_players)
            {
                player.Async_SendGameState(state,currentTick);
            }
        }
        #endregion

        #region IDisposable 구현
        /// <summary>
        /// 리소스 해제 (public 인터페이스)
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Finalizer 호출 방지
        }

        /// <summary>
        /// 리소스 해제 (실제 구현)
        /// - Managed 리소스만 해제 (Unmanaged 리소스는 없음)
        /// - 중복 호출 방지
        /// </summary>
        /// <param name="disposing">Dispose 메서드에서 호출되었는지 여부</param>
        protected virtual void Dispose(bool disposing)
        {
            // 이미 Dispose 되었으면 조기 반환
            if (m_disposed)
                return;

            if (disposing)
            {
                // Managed 리소스 해제
                StopGame(); // 게임 중지

                m_gameLoopCts?.Cancel();    // 게임 루프 취소
                m_gameLoopCts?.Dispose();   // CancellationTokenSource 해제
                m_commandSemaphore?.Dispose(); // SemaphoreSlim 해제

                Console.WriteLine("[Game] Disposed");
            }

            m_disposed = true;
        }
        #endregion
    }
}