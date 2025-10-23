using CommonLib;
using CommonLib.TableData;
using MySql.Data.MySqlClient;

namespace BaseServer
{
    public sealed class DBManager : SingletonBase<DBManager>
    {
        private readonly object _Updatelock = new object();
        private DB_Table m_db_Table = new DB_Table();
        public DB_Table Table => m_db_Table;

        public void UpdateTable()
        {
            lock (_Updatelock)
            {
                // AppConfig에서 연결 문자열 가져오기
                string connectionString = CommonLib.AppConfig.Instance.DatabaseConnectionString;
                m_db_Table.UpdateTable(connectionString);
            }
        }

        public void TryConnect()
        {
            // AppConfig에서 연결 문자열 가져오기
            string connectionString = CommonLib.AppConfig.Instance.DatabaseConnectionString;
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT * FROM your_table";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // 데이터 처리
                            Console.WriteLine(reader["column_name"].ToString());
                        }
                    }
                }
            }
        }

    }

    public sealed class MapManager : SingletonBase<MapManager>
    {
        private Dictionary<int, MapData> m_dic_mapData = new Dictionary<int, MapData>();
        private readonly object _dataLock = new object();

        public MapData LoadMapData(int mapId)
        {
            if (!m_dic_mapData.ContainsKey(mapId))
            {
                // DB 테이블 로드
                DB_Table tableCashe = DBManager.Instance.Table;
                List<MapInfoData> MapInfo = tableCashe.Map_info.Values.ToList();
                if (MapInfo.Count <= 0)
                {
                    DBManager.Instance.UpdateTable();
                    MapInfo = tableCashe.Map_info.Values.ToList();
                }

                List<PlanetInfoData> PlanetInfo = tableCashe.Planet_info.Values.ToList();
                List<MapPlanetInfoData> MapPlanetInfo = tableCashe.Map_Planet_info.Values.ToList();
                List<MapRouteInfoData> MapRouteInfo = tableCashe.Map_Route_info.Values.ToList();

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

                    if (mapInfo == null)
                        return null;

                    var dummyMapData = new MapData(mapInfo, mapPlanets, mapPaths);
                    m_dic_mapData.Add(mapId, dummyMapData);
                }
            }
            return m_dic_mapData[mapId];
        }
    }
}