using CommonLib;
using CommonLib.TableData; // MapData, Planet

namespace BaseServer.Core.Game.Entities
{
    public class Fleet
    {
        public const float ATTACK_RANGE = 10;
        public const int ATTACK_DELAY = 2;
        private long _instanceId;
        private int _ownerId;
        private FleetInfoData _data;
        private FleetState _state;
        private Vector2 _position;
        private int _supplyCost;

        // 이동 처리용
        private Vector2 _moveFrom = new Vector2(0,0);
        private Vector2 _moveTo = new Vector2(0, 0);
        private float _distance => Vector2.Distance(_moveFrom, _moveTo);
        private float _progress = 0;
        private long _moveStartTime = 0;

        // 전투 처리용
        private Fleet? _Enemy;
        private float _curHealth;
        private long _lastAttackTick = 0;

        // 외부 출력용
        public long ID => _instanceId;
        public int Owner => _ownerId;
        public FleetState State => _state;
        public float CurHealth => _curHealth;
        public Vector2 Position => _position;

        public string Name => _data.Name;
        public int FleetType => _data.Type;         // 함대 타입 (GameState 전송용)
        public float MaxHealth => _data.MaxHealth;   // 최대 HP (GameState 전송용)
        public float MoveSpeed => _data.MoveSpeed;
        public float AttackPower => _data.AttackPower;
        public int SupplyCost => _supplyCost;

        // 이동 타겟. 이동중이면 목적지. 공격중이면 공격 타겟 포지션. 없으면 현재 위치 전달
        public Vector2 MoveTarget => State == FleetState.Moving ? _moveTo : State == FleetState.Attacking && _Enemy != null && _Enemy.State != FleetState.Removed ? _Enemy.Position : Position;

        public Fleet? Enemy => _Enemy;

        public Fleet(FleetInfoData data, int ownerId, long instanceId, int supplyCost)
        {
            this._data = data;
            this._ownerId = ownerId;
            this._state = FleetState.Idle;
            this._curHealth = data.MaxHealth; // 최대 체력으로 초기화
            this._position = new Vector2(0,0);
            this._instanceId = instanceId;
            this._supplyCost = supplyCost;
        }

        // public Fleet(FleetInfoData data, int ownerId, Vector2 position, long instanceId, int supplyCost)
        // {
        //     this._data = data;
        //     this._ownerId = ownerId;
        //     this._state = FleetState.Idle;
        //     this._curHealth = data.MaxHealth; // 최대 체력으로 초기화
        //     this._position = position;
        //     this._instanceId = instanceId;
        //     this._supplyCost = supplyCost;
        // }

        public bool IsAttackRange(Fleet target)
        {
            return Vector2.Distance(this._position, target._position) < ATTACK_RANGE;
        }

        public void SetAttackTarget(Fleet target)
        {
            _Enemy = target;
        }


        public void TakeDamage(Fleet attacker)
        {
            if(attacker == null)
                return;

            _curHealth -= attacker.AttackPower;

            // 체력 음수 방지
            _curHealth = MathF.Max(0, _curHealth);

            if(_curHealth <= 0)
            {
                _state = FleetState.Removed;
            }
        }

        public void ApplyDamage(Fleet target)
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

        public void UpdateAttack(long currentTick)
        {
            if (_state != FleetState.Attacking)
                return;
            if (_Enemy == null)
                return;
            // Enemy가 Removed 상태면 공격 중지
            if (_Enemy.State == FleetState.Removed)
            {
                _Enemy = null;
                _state = FleetState.Idle;
                return;
            }

            // 첫 시작인 경우
            if (_lastAttackTick == 0)
                _lastAttackTick = currentTick;

            // 마지막 공격 틱 이후 ATTACK_DELAY 이상 지난 경우 공격 처리
            if (currentTick >= _lastAttackTick + ATTACK_DELAY)
            {
                _Enemy.ApplyDamage(this);
                _lastAttackTick = currentTick;
            }
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
            var beforePos = _position;
            _position = Vector2.Lerp(_moveFrom, _moveTo, progress);

            LogWithTimestamp($"[GAME] FleetMoved. ID : {ID}, before :{beforePos} After : {_position}");
        }
        public void PrintInfo()
        {
            LogWithTimestamp($"=== Fleet {this.ID} Info ===");
            LogWithTimestamp($"Name: {this.Name}");
            LogWithTimestamp($"Type: {this._data.Type}");
            LogWithTimestamp($"Health: {this.CurHealth}/{this._data.MaxHealth}");
            LogWithTimestamp($"Attack Power: {this.AttackPower}");
            LogWithTimestamp($"Move Speed: {this.MoveSpeed}");
            LogWithTimestamp($"State: {this.State}");
            LogWithTimestamp($"Owner ID: {this.Owner}");
            LogWithTimestamp($"Position: {this.Position}");
            LogWithTimestamp($"========================");
        }

        private void LogWithTimestamp(string message)
        {
            var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            System.Console.WriteLine($"[{timestamp}] {message}");
        }
    }
}