
using CommonLib.TableData;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CommonLib
{
    /// <summary>
    /// WPF 클라이언트용 DB Repository
    /// BaseServer의 DB_Table과 유사한 기능 제공
    /// </summary>
    public class GameDBRepository
    {
        private DB_Table? _tableCache;
        private readonly object _cacheLock = new object();

        public GameDBRepository()
        {
            // 초기화 시 테이블 캐시 생성
            _tableCache = new DB_Table();
        }

        /// <summary>
        /// 테이블 캐시 반환 (MapService에서 사용)
        /// </summary>
        public DB_Table? GetTableCache()
        {
            lock (_cacheLock)
            {
                // 캐시가 비어있으면 DB에서 로드
                if (_tableCache == null || _tableCache.Map_info.Count == 0)
                {
                    UpdateTableCache();
                }
                return _tableCache;
            }
        }

        /// <summary>
        /// DB에서 테이블 데이터를 새로 로드
        /// </summary>
        public void UpdateTableCache()
        {
            lock (_cacheLock)
            {
                if (_tableCache == null)
                {
                    _tableCache = new DB_Table();
                }
                // AppConfig에서 연결 문자열 가져오기
                string connectionString = AppConfig.Instance.DatabaseConnectionString;
                _tableCache.UpdateTable(connectionString);
            }
        }

        public string GetFleetTypesQuery()
        {
            // QueryManager에서 SQL 쿼리 문자열을 가져와 그대로 반환합니다.
            return QueryManager.GetQuery("FleetMapper.getFleetTypes");
        }

        // 예시: ID로 특정 함대 타입을 가져오는 쿼리를 반환하는 함수

        public string GetFleetTypeByIdQuery(int id)
        {
            string sql = QueryManager.GetQuery("FleetMapper.getFleetTypeById");
            // 간단한 파라미터 치환 로직이 필요할 수 있습니다.
            return sql.Replace("@id", id.ToString());
        }

        public string GetFleetInfo(int id)
        {
            string sql = QueryManager.GetQuery("TableMapper.getFleetInfo");
            // 간단한 파라미터 치환 로직이 필요할 수 있습니다.
            return sql.Replace("@id", id.ToString());
        }
    }

    /// <summary>
    /// 테이블 데이터 캐시 (BaseServer의 DB_Table과 동일한 구조)
    /// </summary>
    public sealed class DB_Table
    {
        private Dictionary<int, FleetInfoData> m_dic_fleet_info = new Dictionary<int, FleetInfoData>();
        private Dictionary<int, MapInfoData> m_dic_map_info = new Dictionary<int, MapInfoData>();
        private Dictionary<int, MapPlanetInfoData> m_dic_map_planet_info = new Dictionary<int, MapPlanetInfoData>();
        private Dictionary<int, MapRouteInfoData> m_dic_map_route_info = new Dictionary<int, MapRouteInfoData>();
        private Dictionary<int, PlanetInfoData> m_dic_planet_info = new Dictionary<int, PlanetInfoData>();
        private Dictionary<int, ProductionInfoData> m_dic_production_info = new Dictionary<int, ProductionInfoData>();

        public Dictionary<int, FleetInfoData> Fleet_info => m_dic_fleet_info;
        public Dictionary<int, MapInfoData> Map_info => m_dic_map_info;
        public Dictionary<int, MapPlanetInfoData> Map_Planet_info => m_dic_map_planet_info;
        public Dictionary<int, MapRouteInfoData> Map_Route_info => m_dic_map_route_info;
        public Dictionary<int, PlanetInfoData> Planet_info => m_dic_planet_info;
        public Dictionary<int, ProductionInfoData> Production_info => m_dic_production_info;

        public void UpdateTable(string _connectionString)
        {
            LoadAllTables(_connectionString);
        }

        private Dictionary<int, T> LoadDataTable<T>(string connectionString, string tableName)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = $"SELECT * FROM {tableName}";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        MethodInfo convertMethod = typeof(T).GetMethod("Convert",
                            BindingFlags.Public | BindingFlags.Static,
                            null,
                            new[] { typeof(MySqlDataReader) },
                            null);

                        if (convertMethod == null)
                        {
                            LogError($"Type {typeof(T).Name} does not have a static Convert method. Table {tableName} will not be loaded.");
                            return new Dictionary<int, T>();
                        }

                        try
                        {
                            return (Dictionary<int, T>)convertMethod.Invoke(null, new object[] { reader });
                        }
                        catch (Exception ex)
                        {
                            LogError($"Error converting data for table {tableName}: {ex.Message}");
                            return new Dictionary<int, T>();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error loading table {tableName}: {ex.Message}");
                return new Dictionary<int, T>();
            }
        }

        private void LogError(string message)
        {
            Console.Error.WriteLine($"[ERROR] {DateTime.Now}: {message}");
            System.Diagnostics.Debug.WriteLine($"[ERROR] {DateTime.Now}: {message}");
        }

        private void LoadAllTables(string connectionString)
        {
            try
            {
                m_dic_fleet_info = LoadDataTable<FleetInfoData>(connectionString, "fleet_info");
                LogInfo($"Loaded {m_dic_fleet_info.Count} fleet info records");
            }
            catch (Exception ex)
            {
                LogError($"Failed to load fleet_info table: {ex.Message}");
            }

            try
            {
                m_dic_map_info = LoadDataTable<MapInfoData>(connectionString, "map_info");
                LogInfo($"Loaded {m_dic_map_info.Count} map info records");
            }
            catch (Exception ex)
            {
                LogError($"Failed to load map_info table: {ex.Message}");
            }

            try
            {
                m_dic_map_planet_info = LoadDataTable<MapPlanetInfoData>(connectionString, "map_planet_info");
                LogInfo($"Loaded {m_dic_map_planet_info.Count} map planet info records");
            }
            catch (Exception ex)
            {
                LogError($"Failed to load map_planet_info table: {ex.Message}");
            }

            try
            {
                m_dic_map_route_info = LoadDataTable<MapRouteInfoData>(connectionString, "map_route_info");
                LogInfo($"Loaded {m_dic_map_route_info.Count} map route info records");
            }
            catch (Exception ex)
            {
                LogError($"Failed to load map_route_info table: {ex.Message}");
            }

            try
            {
                m_dic_planet_info = LoadDataTable<PlanetInfoData>(connectionString, "planet_info");
                LogInfo($"Loaded {m_dic_planet_info.Count} planet info records");
            }
            catch (Exception ex)
            {
                LogError($"Failed to load planet_info table: {ex.Message}");
            }

            try
            {
                m_dic_production_info = LoadDataTable<ProductionInfoData>(connectionString, "production_info");
                LogInfo($"Loaded {m_dic_production_info.Count} production info records");
            }
            catch (Exception ex)
            {
                LogError($"Failed to load production_info table: {ex.Message}");
            }
        }

        private void LogInfo(string message)
        {
            Console.WriteLine($"[INFO] {DateTime.Now}: {message}");
            System.Diagnostics.Debug.WriteLine($"[INFO] {DateTime.Now}: {message}");
        }
    }
}
