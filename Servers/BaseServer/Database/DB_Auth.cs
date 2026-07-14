using BaseServer.Utils;
using CommonLib;
using MySql.Data.MySqlClient;

namespace BaseServer.Database
{
    public sealed class DB_Auth
    {

        /// <summary>
        /// 로그인 인증 - userinfo 테이블에서 사용자 확인
        /// </summary>
        /// <param name="username">사용자 이름</param>
        /// <param name="password">클라이언트가 제출한 비밀번호</param>
        /// <returns>인증 성공 시 UserInfo, 실패 시 null</returns>
        public UserInfo? AuthenticateUser(string username, string password)
        {
            try
            {
                // AuthDatabase 연결 문자열 가져오기
                string connectionString = CommonLib.AppConfig.Instance.AuthDatabaseConnectionString;

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT id, name, password FROM userinfo WHERE name = @name LIMIT 1";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", username);

                        int userId;
                        string userName;
                        string storedPassword;
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                                return null;

                            userId = reader.GetInt32("id");
                            userName = reader.GetString("name");
                            storedPassword = reader.GetString("password");
                        }

                        PasswordVerificationResult verification = PasswordHasher.Verify(password, storedPassword);
                        if (verification == PasswordVerificationResult.Failed)
                            return null;

                        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
                            TryUpgradePassword(connection, userId, password, storedPassword);

                        return new UserInfo
                        {
                            UserId = userId,
                            UserName = userName
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                // 로그 출력 (실제로는 로깅 시스템 사용)
                Logger.Log($"[DB_Auth] AuthenticateUser Error: {ex.Message}");
            }

            // 인증 실패
            return null;
        }

        /// <summary>
        /// 회원가입 - userinfo 테이블에 새 사용자 추가
        /// </summary>
        public bool RegisterUser(string username, string password)
        {
            try
            {
                string connectionString = CommonLib.AppConfig.Instance.AuthDatabaseConnectionString;

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // 중복 체크 먼저
                    string checkQuery = "SELECT COUNT(*) FROM userinfo WHERE name = @name";
                    using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@name", username);
                        long count = (long)checkCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            Logger.Log($"[DB_Auth] User '{username}' already exists");
                            return false;
                        }
                    }

                    string passwordHash = PasswordHasher.HashPassword(password);
                    string insertQuery = "INSERT INTO userinfo (name, password) VALUES (@name, @password)";
                    using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, connection))
                    {
                        insertCmd.Parameters.AddWithValue("@name", username);
                        insertCmd.Parameters.AddWithValue("@password", passwordHash);
                        int rowsAffected = insertCmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[DB_Auth] RegisterUser Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 유저 존재 여부 확인
        /// </summary>
        public bool UserExists(string username)
        {
            try
            {
                string connectionString = CommonLib.AppConfig.Instance.AuthDatabaseConnectionString;

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT COUNT(*) FROM userinfo WHERE name = @name";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", username);
                        long count = (long)command.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[DB_Auth] UserExists Error: {ex.Message}");
                return false;
            }
        }

        private static void TryUpgradePassword(
            MySqlConnection connection,
            int userId,
            string password,
            string previousStoredValue)
        {
            try
            {
                string passwordHash = PasswordHasher.HashPassword(password);
                const string query = "UPDATE userinfo SET password = @password WHERE id = @id AND password = @previousPassword";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@password", passwordHash);
                command.Parameters.AddWithValue("@id", userId);
                command.Parameters.AddWithValue("@previousPassword", previousStoredValue);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.Log($"[DB_Auth] Password hash upgrade failed for user id {userId}: {ex.Message}");
            }
        }
    }
}
