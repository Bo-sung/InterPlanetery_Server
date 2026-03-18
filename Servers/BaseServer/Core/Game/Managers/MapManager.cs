using BaseServer.Core.Game.Entities;
using BaseServer.Database;
using BaseServer.Utils;
using CommonLib.TableData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseServer.Core.Game.Managers
{
    /// <summary>
    /// 맵 데이터를 표현하는 순수 데이터 컨테이너 클래스
    /// </summary>
    public class MapData
    {
        public readonly MapInfoData mapInfoData;
        public string MapName => mapInfoData.Name;
        public IReadOnlyList<PlanetInfoData> PlanetInfos { get; private set; }
        public IReadOnlyList<MapPlanetInfoData> PlanetLayouts { get; private set; }
        public IReadOnlyList<MapRouteInfoData> Connections { get; private set; }

        public MapData(MapInfoData mapInfoData, IEnumerable<PlanetInfoData> planetInfos, IEnumerable<MapPlanetInfoData> planetLayouts, IEnumerable<MapRouteInfoData> connections)
        {
            this.mapInfoData = mapInfoData;
            this.PlanetInfos = planetInfos.ToList();
            this.PlanetLayouts = planetLayouts.ToList();
            this.Connections = connections.ToList();
        }
    }

    /// <summary>
    /// 맵 데이터를 캐싱하여 성능 향상
    /// </summary>
    public sealed class MapManager
    {
        private static MapManager m_instance;
        private static readonly object m_lockObj = new object();
        private ConcurrentDictionary<int, MapData> m_dic_mapData = new ConcurrentDictionary<int, MapData>();

        private DBManager m_dbManager = DBManager.Instance;

        private MapManager()
        {

        }

        public static MapManager Instance
        {
            get
            {
                if (m_instance == null)
                {
                    lock (m_lockObj)
                    {
                        if (m_instance == null)
                        {
                            m_instance = new MapManager();
                        }
                    }
                }
                return m_instance;
            }
        }

        /// <summary>
        /// MapService를 초기화합니다.
        /// </summary>
        public void Initialize()
        {

        }

        /// <summary>
        /// 맵 데이터를 로드합니다. (MapManager 패턴 적용)
        /// 캐시에 있으면 캐시에서 반환, 없으면 DB에서 로드 후 캐싱
        /// </summary>
        public MapData? LoadMapData(int mapId)
        {
            // 이미 캐시에 있으면 반환
            if (m_dic_mapData.ContainsKey(mapId))
            {
                return m_dic_mapData[mapId];
            }

            // DB에서 테이블 데이터 로드
            DB_Table? tableCache = m_dbManager.Table;

            if (tableCache == null)
            {
                return null;
            }

            // Get all static data from cache
            List<PlanetInfoData> allPlanetInfos = tableCache.Planet_info.Values.ToList();

            MapData? newMapData = null;
            lock (m_lockObj)
            {
                tableCache.Map_info.TryGetValue(mapId, out var mapInfo);
                if (mapInfo == null)
                    return null;

                // Filter layouts and routes for the specific map
                var mapPlanetLayouts = tableCache.Map_Planet_info.Values.Where(c => c.mapId == mapId).ToList();
                var mapRoutes = tableCache.Map_Route_info.Values.Where(c => c.mapId == mapId).ToList();

                // Create the new MapData object using the refactored constructor
                newMapData = new MapData(mapInfo, allPlanetInfos, mapPlanetLayouts, mapRoutes);

                if (m_dic_mapData.TryAdd(mapId, newMapData))
                {
                    Logger.Log($"[MapManager] Map Seted: {mapId}");
                    return newMapData;
                }
            }

            return newMapData;
        }

    }
}