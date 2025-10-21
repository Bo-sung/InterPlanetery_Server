using MySql.Data.MySqlClient;

namespace BaseServer
{
    public sealed class DBManager : SingletonBase<DBManager>
    {
        private const string _connectionString = "server=localhost;user=root;password=asdf1358@@;database=interplanetery_tabledb_local;";
        private DB_Table m_db_Table = new DB_Table();
        public DB_Table Table => m_db_Table;

        public void UpdateTable()
        {
            m_db_Table.UpdateTable(_connectionString);
        }

        public void TryConnect()
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
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