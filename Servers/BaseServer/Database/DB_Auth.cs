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
        /// <param name="password">비밀번호 (평문 - 실제로는 해싱 필요)</param>
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

                    // 파라미터화된 쿼리 (SQL Injection 방지)
                    string query = "SELECT id, name FROM userinfo WHERE name = @name AND password = @password";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // 파라미터 바인딩
                        command.Parameters.AddWithValue("@name", username);
                        command.Parameters.AddWithValue("@password", password);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // 인증 성공 - UserInfo 생성
                                return new UserInfo
                                {
                                    UserId = reader.GetInt32("id"),
                                    UserName = reader.GetString("name")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 로그 출력 (실제로는 로깅 시스템 사용)
                Console.WriteLine($"[DB_Auth] AuthenticateUser Error: {ex.Message}");
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
                            Console.WriteLine($"[DB_Auth] User '{username}' already exists");
                            return false;
                        }
                    }

                    // 사용자 추가
                    string insertQuery = "INSERT INTO userinfo (name, password) VALUES (@name, @password)";
                    using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, connection))
                    {
                        insertCmd.Parameters.AddWithValue("@name", username);
                        insertCmd.Parameters.AddWithValue("@password", password);
                        int rowsAffected = insertCmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB_Auth] RegisterUser Error: {ex.Message}");
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
                Console.WriteLine($"[DB_Auth] UserExists Error: {ex.Message}");
                return false;
            }
        }
    }
}
