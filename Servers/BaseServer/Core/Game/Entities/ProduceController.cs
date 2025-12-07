using BaseServer.Database;
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
        private DB_Table _db;

        public System.Action<Fleet> OnProductionFinish;

        public ProduceController(DBManager db)
        {
            this._db = db.Table;
        }

        private void LogWithTimestamp(string message)
        {
            var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            System.Console.WriteLine($"[{timestamp}] {message}");
        }

        public bool IsOnProduction(long id)
        {
            return productions.Contains(id);
        }

        // 생산 요청 처리
        public void RequestProcess(int fleetType, int playerId, long nextFleetId, long currentTick)
        {
            var data = GetProductionInfoDataFromDB(fleetType);
            if (data == null)
            {
                LogWithTimestamp($"ProductionInfoData not found: {fleetType}");
                return;
            }

            RequestProcess(data, playerId, nextFleetId, currentTick);
        }

        // 생산 요청 처리
        public void RequestProcess(ProductionInfoData productionData, int playerId, long nextFleetId, long currentTick)
        {
            // 플레이어의 생산 큐가 없으면 생성
            if (!productionQueue.ContainsKey(playerId))
            {
                productionQueue[playerId] = new List<ProductionInfo>();
            }

            FleetInfoData fleetData = GetFleetDataFromDB(productionData.Targetid);
            if (fleetData == null)
            {
                LogWithTimestamp($"FleetInfoData not found: {productionData.Targetid}");
                return;
            }

            ProductionInfo production = new ProductionInfo
            {
                PlayerId = playerId,
                Fleet = new Fleet(fleetData, playerId, nextFleetId),
                StartTick = currentTick,
                ProductionTime = productionData.ProductionTime,
                RemainingTicks = productionData.ProductionTime
            };

            productionQueue[playerId].Add(production);
            productions.Add(nextFleetId);
            LogWithTimestamp($"Player {playerId} started producing fleet {production.Fleet.ID} (will take {productionData.ProductionTime} ticks)");
        }

        // 매 틱마다 호출되어 생산 상태 업데이트
        public void ProcessUpdate()
        {
            foreach (var kvp in productionQueue)
            {
                int playerId = kvp.Key;
                List<ProductionInfo> productions = kvp.Value;

                // 완료된 생산 목록
                List<ProductionInfo> completedProductions = new List<ProductionInfo>();

                // 각 생산 항목의 남은 틱 감소
                foreach (var production in productions)
                {
                    production.RemainingTicks--;

                    // 생산 완료 체크
                    if (production.RemainingTicks <= 0)
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
            LogWithTimestamp($"Fleet {production.Fleet.ID} production completed for player {production.PlayerId}");

            // FleetController로 완성된 Fleet 전달
            OnProductionFinish?.Invoke(production.Fleet);
            productions.Remove(production.Fleet.ID);
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
            return productionQueue.TryGetValue(playerId, out var productions)
                ? new List<ProductionInfo>(productions)
                : new List<ProductionInfo>();
        }
    }
}