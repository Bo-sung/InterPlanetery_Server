using BaseServer.Database;
using BaseServer.Utils;
using CommonLib.TableData; // MapData, Planet

namespace BaseServer.Core.Game.Entities
{
    // 생산 정보 클래스
    public class ProductionInfo
    {
        public int PlayerId { get; set; }
        public Fleet Fleet { get; set; }
        public long StartTick { get; set; }        // 생산 시작 틱
        public int ProductionTime { get; set; }    // 총 생산 시간 (틱)
        public int RemainingTicks { get; set; }    // 남은 생산 시간 (틱)

        public float Progress => 1f - ((float)RemainingTicks / ProductionTime);
    }

    public class ProduceController
    {
        // 생산 중인 함대 정보 (PlayerId -> 생산 중인 Fleet 리스트)
        private Dictionary<int, List<ProductionInfo>> productionQueue = new Dictionary<int, List<ProductionInfo>>();
        private List<long> productions = new List<long>();
        private readonly object _lock = new object();
        private DB_Table _db;

        public System.Action<Fleet> OnProductionFinish;

        public ProduceController(DBManager db)
        {
            this._db = db.Table;
        }

        public bool IsOnProduction(long id)
        {
            lock (_lock)
            {
                return productions.Contains(id);
            }
        }

        // 생산 요청 처리
        public void RequestProcess(int fleetType, int playerId, long nextFleetId, long currentTick)
        {
            var data = GetProductionInfoDataFromDB(fleetType);
            if (data == null)
            {
                Logger.Log($"ProductionInfoData not found: {fleetType}");
                return;
            }

            RequestProcess(data, playerId, nextFleetId, currentTick);
        }

        // 생산 요청 처리
        public bool RequestProcess(ProductionInfoData productionData, int playerId, long nextFleetId, long currentTick)
        {
            // DB 조회는 lock 바깥에서 수행 (느린 I/O를 lock 안에 넣지 않음)
            FleetInfoData fleetData = GetFleetDataFromDB(productionData.Targetid);
            if (fleetData == null)
            {
                Logger.Log($"FleetInfoData not found: {productionData.Targetid}");
                return false;
            }

            ProductionInfo production = new ProductionInfo
            {
                PlayerId = playerId,
                Fleet = new Fleet(fleetData, playerId, nextFleetId, productionData.SupplyCost),
                StartTick = currentTick,
                ProductionTime = productionData.ProductionTime,
                RemainingTicks = productionData.ProductionTime
            };

            lock (_lock)
            {
                if (!productionQueue.ContainsKey(playerId))
                    productionQueue[playerId] = new List<ProductionInfo>();

                productionQueue[playerId].Add(production);
                productions.Add(nextFleetId);
            }

            Logger.Log($"Player {playerId} started producing fleet {production.Fleet.ID} (will take {productionData.ProductionTime} ticks)");
            return true;
        }

        // 매 틱마다 호출되어 생산 상태 업데이트
        public void ProcessUpdate()
        {
            List<ProductionInfo> allCompleted = new List<ProductionInfo>();

            lock (_lock)
            {
                foreach (var kvp in productionQueue)
                {
                    List<ProductionInfo> queue = kvp.Value;
                    List<ProductionInfo> completedInQueue = new List<ProductionInfo>();

                    foreach (var production in queue)
                    {
                        production.RemainingTicks--;

                        if (production.RemainingTicks <= 0)
                            completedInQueue.Add(production);
                    }

                    foreach (var completed in completedInQueue)
                        queue.Remove(completed);

                    allCompleted.AddRange(completedInQueue);
                }
            }

            // 완료된 생산 처리는 lock 바깥에서 (콜백 호출 포함)
            foreach (var completed in allCompleted)
                HandleProduceComplete(completed);
        }

        // 생산 완료 처리
        private void HandleProduceComplete(ProductionInfo production)
        {
            Logger.Log($"Fleet {production.Fleet.ID} production completed for player {production.PlayerId}");

            lock (_lock)
            {
                productions.Remove(production.Fleet.ID);
            }

            // FleetController로 완성된 Fleet 전달
            OnProductionFinish?.Invoke(production.Fleet);
            // DB에 생산 완료 기록 (필요시)
            SaveProductionToDB(production);
        }

        // DB에서 함대 데이터 가져오기
        private ProductionInfoData GetProductionInfoDataFromDB(int fleetType)
        {
            if (_db.Production_info.ContainsKey(fleetType))
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
            lock (_lock)
            {
                return productionQueue.TryGetValue(playerId, out var queue)
                    ? new List<ProductionInfo>(queue)
                    : new List<ProductionInfo>();
            }
        }
    }
}