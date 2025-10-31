using CommonLib;
using ChatClientWPF.Database;
using System;

namespace ChatClientWPF.Database
{
    public sealed class DBManager : SingletonBase<DBManager>
    {
        private readonly object _updateLock = new object();
        private DB_Table m_db_Table = new DB_Table();
        public DB_Table Table => m_db_Table;

        private DBManager() { }

        protected override void OnInitialize()
        {
            // Initially load the table with the default connection string from AppConfig
            try
            {
                UpdateTable(AppConfig.Instance.DatabaseConnectionString);
            }
            catch (Exception ex)
            {
                // Log error, but don't crash the client app on startup
                System.Diagnostics.Debug.WriteLine($"[ERROR] Initial DB load failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the cached database tables using the provided connection string.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        public void UpdateTable(string connectionString)
        {
            lock (_updateLock)
            {
                // Create a new instance of DB_Table to effectively clear all old data
                m_db_Table = new DB_Table();
                m_db_Table.UpdateTable(connectionString);
            }
        }

        /// <summary>
        /// Tests the database connection using the provided connection string.
        /// </summary>
        /// <param name="connectionString">The database connection string to test.</param>
        /// <returns>A tuple containing the success status and a message (server version on success, error message on failure).</returns>
        public (bool success, string message) TestConnection(string connectionString)
        {
            try
            {
                using (var connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString))
                {
                    connection.Open();
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        return (true, $"Connection successful! Server Version: {connection.ServerVersion}");
                    }
                    else
                    {
                        return (false, "Connection failed. Connection state is not Open.");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] DB Connection Test Failed: {ex.Message}");
                return (false, "Database connection failed:\n\n{ex.Message}\n\nPlease check your database settings.");
            }
        }
    }
}
