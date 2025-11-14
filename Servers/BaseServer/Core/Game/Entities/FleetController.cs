using CommonLib;

namespace BaseServer.Core.Game.Entities
{
    public class FleetController
    {
        private int nextInstanceId = 0;
        private GameMap? _gameMap;
        private Dictionary<int, Fleet> fleets = new Dictionary<int, Fleet>();

        // 행성별 함대 추적
        private Dictionary<int, HashSet<int>> planetFleets = new Dictionary<int, HashSet<int>>();

        // 전투 관련 상수
        private const float ATTACK_TICK_INTERVAL = 1f; // 1초마다 공격
        private const float OCCUPY_DURATION = 5f;      // 점령 5초

        public FleetController()
        {

        }

        public void InitController(GameMap gameMap)
        {
            _gameMap = gameMap;
        }

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

        public void AddFleet(Fleet fleet)
        {
            if (fleet == null)
            {
                Console.WriteLine($"[Game][FleetController] Fleet is null");
                return;
            }

            if (_gameMap == null)
            {
                Console.WriteLine($"[Game][FleetController] _gameMap is null");
                return;
            }

            // 모든 fleet는 모성에서 생산됨
            var planet = _gameMap.GetHomePlanet(fleet.OwnerId);
            if (planet == null)
            {
                Console.WriteLine($"[Game][FleetController] Invalid fleet.OwnerId: {fleet.OwnerId}");
                return;
            }

            // Fleet 초기 설정
            fleet.SetPosition(planet.Position);
            fleet.SetCurrentPlanet(planet.Id);

            // FleetController에 등록
            fleets.Add(fleet.ID, fleet);

            // 행성에 함대 추가
            if (!planetFleets.ContainsKey(planet.Id))
                planetFleets[planet.Id] = new HashSet<int>();
            planetFleets[planet.Id].Add(fleet.ID);

            Console.WriteLine($"[Fleet {fleet.ID}] ({fleet.Data.Name}) added at planet {planet.Id} for player {fleet.OwnerId}");
        }

        public Fleet? GetFleet(int fleetId)
        {
            if (fleets.ContainsKey(fleetId))
                return fleets[fleetId];

            return null;
        }

        public void RemoveFleet(Fleet fleet)
        {
            if (fleet == null)
            {
                Console.WriteLine($"[Game][FleetController] Fleet is null");
                return;
            }

            int fleetId = fleet.ID;

            // 행성에서 제거
            if (fleet.CurrentPlanetId.HasValue && planetFleets.ContainsKey(fleet.CurrentPlanetId.Value))
            {
                planetFleets[fleet.CurrentPlanetId.Value].Remove(fleetId);
            }

            fleets.Remove(fleetId);
            Console.WriteLine($"[Fleet {fleetId}] removed from FleetController");
        }

        public void HandleMovementProcess(float deltaTime)
        {
            foreach (var fleet in fleets.Values)
            {
                switch (fleet.State)
                {
                    case FleetState.Moving:
                        UpdateMovingFleet(fleet, deltaTime);
                        break;

                    case FleetState.Attacking:
                    case FleetState.Occupying:
                    case FleetState.Idle:
                        break;
                }
            }
        }

        public void HandleCombatProcess(float deltaTime)
        {
            // 파괴된 함대 제거 목록
            List<Fleet> fleetsToRemove = new List<Fleet>();

            foreach (var fleet in fleets.Values)
            {
                switch (fleet.State)
                {
                    case FleetState.Attacking:
                        UpdateAttackingFleet(fleet, deltaTime);
                        break;

                    case FleetState.Moving:
                    case FleetState.Occupying:
                    case FleetState.Idle:
                        break;
                }

                // 파괴된 함대 체크
                if (fleet.IsDestroyed())
                {
                    fleetsToRemove.Add(fleet);
                    continue;
                }
            }

        }

        public void HandleConquerProcess(float deltaTime)
        {
            foreach (var fleet in fleets.Values)
            {
                switch (fleet.State)
                {
                    case FleetState.Occupying:
                        UpdateOccupyingFleet(fleet, deltaTime);
                        break;
                    case FleetState.Attacking:
                    case FleetState.Moving:
                    case FleetState.Idle:
                        break;
                }
            }
        }

        private void UpdateMovingFleet(Fleet fleet, float deltaTime)
        {
            if (!fleet.TargetPlanetId.HasValue)
            {
                Console.WriteLine($"[Fleet {fleet.ID}] No target planet");
                fleet.SetState(FleetState.Idle);
                return;
            }

            var targetPlanet = _gameMap.GetPlanet(fleet.TargetPlanetId.Value);
            if (targetPlanet == null)
            {
                Console.WriteLine($"[Fleet {fleet.ID}] Invalid target planet: {fleet.TargetPlanetId}");
                fleet.SetState(FleetState.Idle);
                return;
            }

            MoveFleet(fleet, targetPlanet.Position, deltaTime);

            // 목표 지점 도달 확인
            float distanceToTarget = Vector2.Distance(fleet.Position, targetPlanet.Position);
            if (distanceToTarget < 0.1f) // 도착 임계값
            {
                // 목적지 도착
                fleet.SetPosition(targetPlanet.Position);
                ArriveAtPlanet(fleet);
            }
        }

        private void MoveFleet(Fleet fleet, Vector2 targetPosition, float deltaTime)
        {
            Vector2 direction = targetPosition - fleet.Position;
            direction.Normalize();
            float speed = fleet.Data.MoveSpeed;
            Vector2 newPosition = fleet.Position + direction * speed * deltaTime;

            fleet.SetPosition(newPosition);
        }

        private void ArriveAtPlanet(Fleet fleet)
        {
            if (!fleet.TargetPlanetId.HasValue)
            {
                fleet.SetState(FleetState.Idle);
                return;
            }

            int planetId = fleet.TargetPlanetId.Value;
            var planet = _gameMap.GetPlanet(planetId);

            if (planet == null)
            {
                fleet.SetState(FleetState.Idle);
                return;
            }

            // 행성 도착 처리
            fleet.SetCurrentPlanet(planetId);
            fleet.SetPosition(planet.Position);

            // 행성에 함대 등록
            if (!planetFleets.ContainsKey(planetId))
                planetFleets[planetId] = new HashSet<int>();
            planetFleets[planetId].Add(fleet.ID);

            Console.WriteLine($"[Fleet {fleet.ID}] arrived at planet {planetId}");

            // 행성 소유권 확인
            if (planet.OwnerId != fleet.OwnerId)
            {
                // 적 행성이거나 중립 행성이면 전투 시작
                fleet.StartAttack();
            }
            else
            {
                // 아군 행성이면 Idle
                fleet.SetState(FleetState.Idle);
            }
        }

        private void UpdateAttackingFleet(Fleet fleet, float deltaTime)
        {
            if (!fleet.CurrentPlanetId.HasValue)
            {
                fleet.SetState(FleetState.Idle);
                return;
            }

            // 전투 진행 (1초마다 공격)
            float attackSpeed = 1f / ATTACK_TICK_INTERVAL;
            fleet.UpdateActionProgress(attackSpeed * deltaTime);

            if (fleet.ActionProgress >= 1f)
            {
                // 공격 실행
                ExecuteAttack(fleet);
                fleet.CompleteAction();
            }
        }

        private void ExecuteAttack(Fleet fleet)
        {
            if (!fleet.CurrentPlanetId.HasValue)
                return;

            var planet = _gameMap.GetPlanet(fleet.CurrentPlanetId.Value);
            if (planet == null)
                return;

            // 같은 행성의 적 함대 찾기
            var enemyFleets = GetFleetsAtPlanet(fleet.CurrentPlanetId.Value)
                .Where(f => f.OwnerId != fleet.OwnerId && f.IsAlive())
                .ToList();

            if (enemyFleets.Count == 0)
            {
                // 적이 없으면 점령 시작
                Console.WriteLine($"[Fleet {fleet.ID}] No enemies left, starting occupation");
                fleet.StartOccupy();
                return;
            }

            // 무작위 적 공격 (또는 첫 번째 적)
            var target = enemyFleets[0];
            target.TakeDamage(fleet.Data.AttackPower);

            Console.WriteLine($"[Fleet {fleet.ID}] (Player {fleet.OwnerId}) attacked [Fleet {target.ID}] (Player {target.OwnerId}) for {fleet.Data.AttackPower} damage");

            // 반격 (타겟이 살아있으면)
            if (target.IsAlive())
            {
                fleet.TakeDamage(target.Data.AttackPower);
                Console.WriteLine($"[Fleet {target.ID}] (Player {target.OwnerId}) counter-attacked [Fleet {fleet.ID}] (Player {fleet.OwnerId}) for {target.Data.AttackPower} damage");
            }
        }

        private void UpdateOccupyingFleet(Fleet fleet, float deltaTime)
        {
            // 점령 진행
            float occupySpeed = 1f / OCCUPY_DURATION;
            fleet.UpdateActionProgress(occupySpeed * deltaTime);

            if (fleet.ActionProgress >= 1f)
            {
                // 점령 완료
                CompleteOccupy(fleet);
            }
        }

        private void CompleteOccupy(Fleet fleet)
        {
            if (!fleet.CurrentPlanetId.HasValue)
            {
                fleet.SetState(FleetState.Idle);
                return;
            }

            var planet = _gameMap.GetPlanet(fleet.CurrentPlanetId.Value);
            if (planet == null)
            {
                fleet.SetState(FleetState.Idle);
                return;
            }

            Console.WriteLine($"[Fleet {fleet.ID}] (Player {fleet.OwnerId}) completed occupation of planet {planet.Id}");

            // 행성 소유권 변경
            planet.OwnerId = fleet.OwnerId;

            // Idle 상태로 전환
            fleet.CompleteAction();
            fleet.SetState(FleetState.Idle);
        }

        // 함대 이동 명령 - 인접한 행성으로만 이동 가능
        public bool CommandMove(int fleetId, int targetPlanetId)
        {
            var fleet = GetFleet(fleetId);
            if (fleet == null)
            {
                Console.WriteLine($"[FleetController] Fleet not found: {fleetId}");
                return false;
            }

            if (fleet.State != FleetState.Idle)
            {
                Console.WriteLine($"[Fleet {fleetId}] Cannot move in state: {fleet.State}");
                return false;
            }

            if (!fleet.CurrentPlanetId.HasValue)
            {
                Console.WriteLine($"[Fleet {fleetId}] No current planet");
                return false;
            }

            // 인접 경로 검증 - 직접 연결된 행성인지 확인
            if (!_gameMap.IsValidPath(fleet.CurrentPlanetId.Value, targetPlanetId))
            {
                Console.WriteLine($"[Fleet {fleetId}] Not a valid adjacent path from planet {fleet.CurrentPlanetId} to {targetPlanetId}");
                return false;
            }

            // 현재 행성에서 함대 제거
            if (planetFleets.ContainsKey(fleet.CurrentPlanetId.Value))
            {
                planetFleets[fleet.CurrentPlanetId.Value].Remove(fleetId);
            }

            // 단일 경로 이동 시작
            fleet.StartMove(targetPlanetId);

            return true;
        }

        // 특정 행성에서 이동 가능한 인접 행성 목록 가져오기
        public List<int> GetAdjacentPlanets(int planetId)
        {
            var planet = _gameMap.GetPlanet(planetId);
            if (planet == null)
                return new List<int>();

            // GameMap의 연결 정보를 활용하여 인접 행성 찾기
            List<int> adjacentPlanets = new List<int>();

            // 모든 행성을 순회하며 연결 확인
            for (int testPlanetId = 0; testPlanetId < 1000; testPlanetId++)
            {
                if (testPlanetId == planetId)
                    continue;

                if (_gameMap.IsValidPath(planetId, testPlanetId))
                {
                    adjacentPlanets.Add(testPlanetId);
                }
            }

            return adjacentPlanets;
        }

        // 특정 행성의 모든 함대 가져오기
        public List<Fleet> GetFleetsAtPlanet(int planetId)
        {
            if (!planetFleets.ContainsKey(planetId))
                return new List<Fleet>();

            return planetFleets[planetId]
                .Select(fleetId => GetFleet(fleetId))
                .Where(fleet => fleet != null)
                .ToList();
        }

        // 플레이어의 모든 함대 가져오기
        public List<Fleet> GetPlayerFleets(int playerId)
        {
            return fleets.Values
                .Where(f => f.OwnerId == playerId)
                .ToList();
        }

        // 함대 정보 출력 (디버깅용)
        public void PrintFleetInfo(int fleetId)
        {
            var fleet = GetFleet(fleetId);
            if (fleet == null)
            {
                Console.WriteLine($"Fleet {fleetId} not found");
                return;
            }

            Console.WriteLine($"=== Fleet {fleet.ID} Info ===");
            Console.WriteLine($"Name: {fleet.Data.Name}");
            Console.WriteLine($"Type: {fleet.Data.Type}");
            Console.WriteLine($"Health: {fleet.CurrentHealth}/{fleet.Data.MaxHealth}");
            Console.WriteLine($"Attack Power: {fleet.Data.AttackPower}");
            Console.WriteLine($"Move Speed: {fleet.Data.MoveSpeed}");
            Console.WriteLine($"State: {fleet.State}");
            Console.WriteLine($"Owner ID: {fleet.OwnerId}");
            Console.WriteLine($"Position: {fleet.Position}");
            Console.WriteLine($"Current Planet: {fleet.CurrentPlanetId}");
            Console.WriteLine($"Target Planet: {fleet.TargetPlanetId}");
            Console.WriteLine($"========================");
        }
    }
}