using CommonLib;
using CommonLib.TableData;
using MySql.Data.MySqlClient;

namespace BaseServer.Database
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
}