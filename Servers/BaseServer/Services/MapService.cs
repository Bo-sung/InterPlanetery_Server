using System;
using System.Collections.Generic;
using System.Linq;
using CommonLib;
using CommonLib.TableData;
using BaseServer.Database;

namespace BaseServer.Services
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
        private BaseServer.Database.GameDBRepository? _gameDbRepository;

        /// <summary>
        /// MapService를 초기화합니다.
        /// </summary>
        public void Initialize(BaseServer.Database.GameDBRepository gameDbRepository)
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

            // Get all static data from cache
            List<PlanetInfoData> allPlanetInfos = tableCache.Planet_info.Values.ToList();

            MapData? newMapData = null;
            lock (_dataLock)
            {
                // Find the specific map info
                tableCache.Map_info.TryGetValue(mapId, out var mapInfo);
                if (mapInfo == null)
                    return null;

                // Filter layouts and routes for the specific map
                var mapPlanetLayouts = tableCache.Map_Planet_info.Values.Where(c => c.mapId == mapId).ToList();
                var mapRoutes = tableCache.Map_Route_info.Values.Where(c => c.mapId == mapId).ToList();

                // Create the new MapData object using the refactored constructor
                newMapData = new MapData(mapInfo, allPlanetInfos, mapPlanetLayouts, mapRoutes);
                m_dic_mapData.Add(mapId, newMapData);
            }

            return newMapData;
        }
    }
}
