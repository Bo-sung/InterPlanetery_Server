using CommonLib.TableData; // MapData, Planet, PlanetType, Vector2
using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonLib.Services
{
    public sealed class MapService
    {
        // 공개 데이터 컬렉션은 기존과 동일하게 유지 (필요시 외부에서도 조회 가능)
        public List<PlanetInfoData> PlanetInfo = new List<PlanetInfoData>();
        public List<MapInfoData> MapInfo = new List<MapInfoData>();
        public List<MapPlanetInfoData> MapPlanetInfo = new List<MapPlanetInfoData>();
        public List<MapRouteInfoData> MapRouteInfo = new List<MapRouteInfoData>();

        private readonly GameDBRepository _gameDbRepository;

        // 싱글톤 인스턴스 관리
        private static MapService? _instance;
        private static readonly object _syncRoot = new object();

        // 데이터 동기화 락 (인스턴스 레벨)
        private readonly object _dataLock = new object();

        // private 생성자: 외부에서 직접 인스턴스 생성 불가
        private MapService(GameDBRepository gameDbRepository)
        {
            _gameDbRepository = gameDbRepository ?? throw new ArgumentNullException(nameof(gameDbRepository));
        }

        /// <summary>
        /// MapService를 한 번만 초기화합니다. 멀티스레드 환경에서 안전합니다.
        /// 이미 초기화되어 있으면 무시됩니다.
        /// </summary>
        public static void Initialize(GameDBRepository gameDbRepository)
        {
            if (gameDbRepository == null)
                throw new ArgumentNullException(nameof(gameDbRepository));

            if (_instance != null)
                return;

            lock (_syncRoot)
            {
                if (_instance == null)
                {
                    _instance = new MapService(gameDbRepository);
                }
            }
        }

        /// <summary>
        /// 초기화된 싱글톤 인스턴스를 반환합니다. Initialize를 먼저 호출해야 합니다.
        /// </summary>
        public static MapService Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("MapService가 초기화되지 않았습니다. MapService.Initialize(gameDbRepository)를 먼저 호출하세요.");
                return _instance;
            }
        }

        public MapData LoadMapData(int mapId)
        {
            // TODO: Implement actual loading logic from DB using GameDBRepository
            // This will involve multiple queries and assembling the MapData object.

            // Placeholder for now: 
            // 1. Get basic map info from 'maps' table
            // 2. Get planet data from 'map_planets' and 'planet_info' tables
            // 3. Get connections from 'planet_routes' table
            // 4. Assemble into CommonLib.TableData.MapData

            // For demonstration, returning a dummy MapData

            // 임시 데이터 삽입 영역 시작 (스레드 안전하게 처리)
            lock (_dataLock)
            {
                if (PlanetInfo.Count == 0)
                {
                    PlanetInfo.Add(new PlanetInfoData(1, "Earth", 100, 200, 50));
                    PlanetInfo.Add(new PlanetInfoData(2, "Mars", 150, 180, 30));
                    PlanetInfo.Add(new PlanetInfoData(3, "Jupiter", 500, 50, 10));
                    PlanetInfo.Add(new PlanetInfoData(4, "Earth", 100, 200, 50));
                    PlanetInfo.Add(new PlanetInfoData(5, "Mars", 150, 180, 30));
                    PlanetInfo.Add(new PlanetInfoData(6, "Jupiter", 500, 50, 10));
                    PlanetInfo.Add(new PlanetInfoData(7, "TestPlanet ", 1, 1, 1));
                    PlanetInfo.Add(new PlanetInfoData(8, "Earth", 100, 200, 50));
                    PlanetInfo.Add(new PlanetInfoData(9, "Mars", 150, 180, 30));
                    PlanetInfo.Add(new PlanetInfoData(10, "Jupiter", 500, 50, 10));
                    PlanetInfo.Add(new PlanetInfoData(11, "Venus", 80, 150, 40));
                    PlanetInfo.Add(new PlanetInfoData(12, "Saturn", 600, 30, 5));
                }

                if (MapInfo.Count == 0)
                {
                    MapInfo.Add(new MapInfoData(1, "Tutorial Map", "Description", 0, 1));
                    MapInfo.Add(new MapInfoData(2, "Battle Arena", "Description", 2, 1));
                }

                if (MapPlanetInfo.Count == 0)
                {
                    MapPlanetInfo.Add(new MapPlanetInfoData(1, 1, 1, 100, 150));
                    MapPlanetInfo.Add(new MapPlanetInfoData(2, 1, 2, 300, 200));
                    MapPlanetInfo.Add(new MapPlanetInfoData(3, 1, 3, 500, 100));
                    MapPlanetInfo.Add(new MapPlanetInfoData(4, 1, 4, 200, 400));
                    MapPlanetInfo.Add(new MapPlanetInfoData(5, 2, 2, 250, 250));
                    MapPlanetInfo.Add(new MapPlanetInfoData(6, 2, 3, 450, 350));
                    MapPlanetInfo.Add(new MapPlanetInfoData(7, 2, 5, 600, 150));
                }

                if (MapRouteInfo.Count == 0)
                {
                    MapRouteInfo.Add(new MapRouteInfoData(1, 1, 1, 2));
                    MapRouteInfo.Add(new MapRouteInfoData(3, 1, 1, 4));
                    MapRouteInfo.Add(new MapRouteInfoData(2, 1, 2, 3));
                    MapRouteInfo.Add(new MapRouteInfoData(4, 1, 3, 4));
                    MapRouteInfo.Add(new MapRouteInfoData(5, 2, 2, 3));
                    MapRouteInfo.Add(new MapRouteInfoData(6, 2, 3, 5));
                }
            }
            // 임시 데이터 삽입 영역 끝

            // 데이터 조회 및 MapData 조립 (데이터 접근은 _dataLock으로 보호 후 복사본 사용)
            List<Planet> mapPlanets;
            List<MapRouteInfoData> mapPaths;
            MapInfoData? mapInfo;

            lock (_dataLock)
            {
                var mapPlanetInfos = MapPlanetInfo.Where(c => c.mapId == mapId).ToList();
                mapPaths = MapRouteInfo.Where(c => c.mapId == mapId).ToList();

                mapPlanets = new List<Planet>();
                foreach (var item in mapPlanetInfos)
                {
                    var planetInfo = PlanetInfo.Find(c => c.id == item.planetId);
                    if (planetInfo == null)
                        continue;
                    mapPlanets.Add(new Planet(planetInfo, new Vector2(item.PositionX, item.PositionY)));
                }

                mapInfo = MapInfo.Find(c => c.id == mapId);
            }

            if (mapInfo == null)
                return null;

            var dummyMapData = new MapData(mapInfo, mapPlanets, mapPaths);
            return dummyMapData;
        }
    }
}
