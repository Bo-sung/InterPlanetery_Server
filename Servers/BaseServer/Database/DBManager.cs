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