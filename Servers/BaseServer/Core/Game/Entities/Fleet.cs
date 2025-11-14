using CommonLib;
using CommonLib.TableData; // MapData, Planet

namespace BaseServer.Core.Game.Entities
{
    public class Fleet
    {
        private int _instanceId;
        public int ID => _instanceId;

        private FleetInfoData _data;
        public FleetInfoData Data => _data;

        public Vector2 Position { get; private set; }
        public FleetState State { get; private set; }

        // 현재 체력
        public int CurrentHealth { get; private set; }

        // 이동 관련
        public int? CurrentPlanetId { get; private set; }  // 현재 위치한 행성 (Idle 상태)
        public int? TargetPlanetId { get; private set; }   // 목표 행성 (인접한 행성만 가능)

        // 전투/점령 관련
        public float ActionProgress { get; private set; }  // 작업 진행도 (0~1)
        public int OwnerId { get; private set; }           // 함대 소유자 ID

        public Fleet(FleetInfoData data, int ownerId)
        {
            this._data = data;
            this.OwnerId = ownerId;
            this.State = FleetState.Idle;
            this.CurrentHealth = data.MaxHealth; // 최대 체력으로 초기화
        }

        public void SetID(int id)
        {
            this._instanceId = id;
        }

        public void SetPosition(Vector2 position)
        {
            this.Position = position;
        }

        public void SetState(FleetState newState)
        {
            Console.WriteLine($"[Fleet {ID}] State changed: {State} -> {newState}");
            this.State = newState;
        }

        public void SetCurrentPlanet(int? planetId)
        {
            this.CurrentPlanetId = planetId;
        }

        public void StartMove(int targetPlanetId)
        {
            this.TargetPlanetId = targetPlanetId;
            this.CurrentPlanetId = null;
            this.State = FleetState.Moving;
            this.ActionProgress = 0f;

            Console.WriteLine($"[Fleet {ID}] Started moving to planet {targetPlanetId}");
        }

        public void StartAttack()
        {
            this.State = FleetState.Attacking;
            this.ActionProgress = 0f;
            Console.WriteLine($"[Fleet {ID}] Started attacking");
        }

        public void StartOccupy()
        {
            this.State = FleetState.Occupying;
            this.ActionProgress = 0f;
            Console.WriteLine($"[Fleet {ID}] Started occupying");
        }

        public void UpdateActionProgress(float deltaProgress)
        {
            this.ActionProgress = Math.Min(1f, this.ActionProgress + deltaProgress);
        }

        public void CompleteAction()
        {
            this.ActionProgress = 0f;
        }

        public void TakeDamage(int damage)
        {
            CurrentHealth = Math.Max(0, CurrentHealth - damage);
            Console.WriteLine($"[Fleet {ID}] Took {damage} damage. Health: {CurrentHealth}/{_data.MaxHealth}");

            if (CurrentHealth <= 0)
            {
                Console.WriteLine($"[Fleet {ID}] Destroyed!");
            }
        }

        public void Heal(int amount)
        {
            CurrentHealth = Math.Min(_data.MaxHealth, CurrentHealth + amount);
            Console.WriteLine($"[Fleet {ID}] Healed {amount}. Health: {CurrentHealth}/{_data.MaxHealth}");
        }

        public bool IsAlive()
        {
            return CurrentHealth > 0;
        }

        public bool IsDestroyed()
        {
            return CurrentHealth <= 0;
        }
    }
}