using CommonLib;
using CommonLib.TableData; // MapData, Planet

namespace BaseServer.Core.Game.Entities
{
    public class Fleet_Re
    {
        public const float ATTACK_RANGE = 10;
        // 장버
        private int _instanceId;
        private int _ownerId;
        private FleetInfoData _data;
        private FleetState _state;
        private Vector2 _position;

        // 스텟
        private float _curHealth;

        // 이동 처리용
        private Vector2 _moveFrom = new Vector2(0,0);
        private Vector2 _moveTo = new Vector2(0, 0);
        private float _distance => Vector2.Distance(_moveFrom, _moveTo);
        private float _progress = 0;
        private long _moveStartTime = 0;

        // 외부 출력용
        public int ID => _instanceId;
        public int Owner => _ownerId;
        public FleetState State => _state;
        public float CurHealth => _curHealth;
        public Vector2 Position => _position;

        public string Name => _data.Name;
        public float MoveSpeed => _data.MoveSpeed;
        public float AttackPower => _data.AttackPower;

        public Fleet_Re(FleetInfoData data, int ownerId)
        {
            this._data = data;
            this._ownerId = ownerId;
            this._state = FleetState.Idle;
            this._curHealth = data.MaxHealth; // 최대 체력으로 초기화
            this._position = new Vector2(0,0);
        }

        public Fleet_Re(FleetInfoData data, int ownerId, Vector2 position)
        {
            this._data = data;
            this._ownerId = ownerId;
            this._state = FleetState.Idle;
            this._curHealth = data.MaxHealth; // 최대 체력으로 초기화
            this._position = position;
        }

        public bool IsAttackRange(Fleet_Re target)
        {
            return Vector2.Distance(this._position, target._position) < ATTACK_RANGE;
        }

        public void TakeDamage(Fleet_Re attacker)
        {
            if(attacker == null)
                return;

            _curHealth += attacker.AttackPower;

            // 체력 음수 방지
            _curHealth = MathF.Max(0, _curHealth);

            if(_curHealth <= 0)
            {
                _state = FleetState.Removed;
            }
        }

        public void ApplyDamage(Fleet_Re target)
        {
            if (target == null)
                return;
            target.TakeDamage(this);
        }

        public void Navigate(GamePlanet target, long currentTick)
        {
            _state = FleetState.Moving;
            _moveTo = target.Position;
            _moveFrom = Position;
            _moveStartTime = currentTick;
        }

        public void UpdateMovement(long currentTick)
        {
            // 이동중 아니면 스킵
            if (_state != FleetState.Moving)
                return;
            // 첫 시작인 경우
            if(_moveStartTime == 0)
                _moveStartTime = currentTick;

            // 이동 시작 이후 경과 시간 (밀리초)
            long elapsedMs = currentTick - _moveStartTime;

            // 필요 시간 계산 (거리 / 속도, 단위: 밀리초)
            float distance = Vector2.Distance(_moveFrom, _moveTo);
            long requiredMs = (long)(distance / _data.MoveSpeed * 1000);

            // 목표 도달 여부
            if (elapsedMs >= requiredMs)
            {
                _position = _moveTo;
                _state = FleetState.Idle;
                _moveStartTime = 0;
                return;
            }

            // 선형 보간
            float progress = (float)elapsedMs / requiredMs;
            _position = Vector2.Lerp(_moveFrom, _moveTo, progress);
        }
    }


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