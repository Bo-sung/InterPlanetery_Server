using CommonLib.TableData; // MapData, Planet, PlanetType, Vector2
using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonLib.Services
{
    /// <summary>
    /// MapService - MapManager 패턴을 따라 구현
    /// SingletonBase를 사용하여 싱글톤 패턴 적용
    /// 맵 데이터를 캐싱하여 성능 향상
    /// </summary>
    public sealed class MapService : SingletonBase<MapService>
    {
        // 맵 데이터 캐시 (MapManager와 동일)
        private Dictionary<int, MapData> m_dic_mapData = new Dictionary<int, MapData>();
        private readonly object _dataLock = new object();

        // DB Repository 참조
        private GameDBRepository? _gameDbRepository;

        /// <summary>
        /// MapService를 초기화합니다.
        /// </summary>
        public void Initialize(GameDBRepository gameDbRepository)
        {
            _gameDbRepository = gameDbRepository ?? throw new ArgumentNullException(nameof(gameDbRepository));
        }

        /// <summary>
        /// 초기화 여부 확인
        /// </summary>
        public bool IsInit => _gameDbRepository != null;

        /// <summary>
        /// 맵 데이터를 로드합니다. (MapManager 패턴 적용)
        /// 캐시에 있으면 캐시에서 반환, 없으면 DB에서 로드 후 캐싱
        /// </summary>
        public MapData? LoadMapData(int mapId)
        {
            if (!IsInit)
            {
                throw new InvalidOperationException("MapService가 초기화되지 않았습니다. Initialize()를 먼저 호출하세요.");
            }

            // 이미 캐시에 있으면 반환
            if (m_dic_mapData.ContainsKey(mapId))
            {
                return m_dic_mapData[mapId];
            }

            // DB에서 테이블 데이터 로드
            DB_Table? tableCache = _gameDbRepository?.GetTableCache();

            if (tableCache == null)
            {
                return null;
            }

            List<MapInfoData> MapInfo = tableCache.Map_info.Values.ToList();
            List<PlanetInfoData> PlanetInfo = tableCache.Planet_info.Values.ToList();
            List<MapPlanetInfoData> MapPlanetInfo = tableCache.Map_Planet_info.Values.ToList();
            List<MapRouteInfoData> MapRouteInfo = tableCache.Map_Route_info.Values.ToList();

            List<Planet> mapPlanets;
            List<MapRouteInfoData> mapPaths;
            MapInfoData? mapInfo;

            lock (_dataLock)
            {
                // 해당 맵에 속한 행성 정보 필터링
                var mapPlanetInfos = MapPlanetInfo.Where(c => c.mapId == mapId).ToList();
                mapPaths = MapRouteInfo.Where(c => c.mapId == mapId).ToList();

                // 행성 객체 생성
                mapPlanets = new List<Planet>();
                foreach (var item in mapPlanetInfos)
                {
                    var planetInfo = PlanetInfo.Find(c => c.id == item.planetId);
                    if (planetInfo == null)
                        continue;
                    mapPlanets.Add(new Planet(planetInfo, new Vector2(item.PositionX, item.PositionY)));
                }

                // 맵 기본 정보 가져오기
                mapInfo = MapInfo.Find(c => c.id == mapId);

                if (mapInfo == null)
                    return null;

                // MapData 생성 및 캐싱
                var mapData = new MapData(mapInfo, mapPlanets, mapPaths);
                m_dic_mapData.Add(mapId, mapData);
            }

            return m_dic_mapData[mapId];
        }
    }
}
