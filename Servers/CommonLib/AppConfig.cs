using System;
using System.IO;
using System.Text.Json;

namespace CommonLib
{
    /// <summary>
    /// 애플리케이션 설정 관리 클래스
    /// appsettings.json에서 설정을 읽어옵니다.
    /// </summary>
    public class AppConfig : SingletonBase<AppConfig>
    {
        private ConfigData? _config;
        private readonly object _configLock = new object();

        /// <summary>
        /// 데이터베이스 연결 문자열 (자동 생성)
        /// </summary>
        public string DatabaseConnectionString
        {
            get
            {
                EnsureConfigLoaded();

                // ConnectionString이 직접 지정되어 있으면 그것을 사용
                if (!string.IsNullOrEmpty(_config?.Database?.ConnectionString))
                {
                    return _config.Database.ConnectionString;
                }

                // 아니면 개별 항목으로 ConnectionString 생성
                var db = _config?.Database;
                if (db != null)
                {
                    return $"server={db.Server ?? "localhost"};" +
                           $"user={db.UserId ?? "root"};" +
                           $"password={db.Password ?? ""};" +
                           $"database={db.DatabaseName ?? "interplanetery_tabledb_local"};" +
                           $"port={db.Port ?? 3306};";
                }

                return GetDefaultConnectionString();
            }
        }

        /// <summary>
        /// 데이터베이스 서버 주소
        /// </summary>
        public string DatabaseServer
        {
            get
            {
                EnsureConfigLoaded();
                return _config?.Database?.Server ?? "localhost";
            }
        }

        /// <summary>
        /// 데이터베이스 사용자 ID
        /// </summary>
        public string DatabaseUserId
        {
            get
            {
                EnsureConfigLoaded();
                return _config?.Database?.UserId ?? "root";
            }
        }

        /// <summary>
        /// 데이터베이스 비밀번호
        /// </summary>
        public string DatabasePassword
        {
            get
            {
                EnsureConfigLoaded();
                return _config?.Database?.Password ?? "";
            }
        }

        /// <summary>
        /// 데이터베이스 이름
        /// </summary>
        public string DatabaseName
        {
            get
            {
                EnsureConfigLoaded();
                return _config?.Database?.DatabaseName ?? "interplanetery_tabledb_local";
            }
        }

        /// <summary>
        /// 데이터베이스 포트
        /// </summary>
        public int DatabasePort
        {
            get
            {
                EnsureConfigLoaded();
                return _config?.Database?.Port ?? 3306;
            }
        }

        /// <summary>
        /// 서버 포트 번호
        /// </summary>
        public int ServerPort
        {
            get
            {
                EnsureConfigLoaded();
                return _config?.Server?.Port ?? 7777;
            }
        }

        /// <summary>
        /// 서버 호스트 주소
        /// </summary>
        public string ServerHost
        {
            get
            {
                EnsureConfigLoaded();
                return _config?.Server?.Host ?? "localhost";
            }
        }

        /// <summary>
        /// 로그 레벨
        /// </summary>
        public string LogLevel
        {
            get
            {
                EnsureConfigLoaded();
                return _config?.Logging?.LogLevel ?? "Information";
            }
        }

        /// <summary>
        /// 설정 파일 경로
        /// </summary>
        private string ConfigFilePath
        {
            get
            {
                // 실행 파일이 있는 디렉토리에서 appsettings.json 찾기
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(baseDir, "appsettings.json");
            }
        }

        /// <summary>
        /// 초기화 메서드
        /// </summary>
        protected override void OnInitialize()
        {
            LoadConfig();
        }

        /// <summary>
        /// 설정 파일을 로드합니다.
        /// </summary>
        public void LoadConfig()
        {
            lock (_configLock)
            {
                try
                {
                    string configPath = ConfigFilePath;
                    System.Diagnostics.Debug.WriteLine($"=== AppConfig LoadConfig ===");
                    System.Diagnostics.Debug.WriteLine($"Config file path: {configPath}");
                    System.Diagnostics.Debug.WriteLine($"File exists: {File.Exists(configPath)}");

                    if (!File.Exists(configPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"[WARNING] appsettings.json not found at: {configPath}");
                        System.Diagnostics.Debug.WriteLine("[WARNING] Using default configuration values");
                        Console.WriteLine($"[WARNING] appsettings.json not found at: {configPath}");
                        _config = GetDefaultConfig();
                        LogCurrentConfig();
                        return;
                    }

                    string json = File.ReadAllText(configPath);
                    System.Diagnostics.Debug.WriteLine($"JSON content length: {json.Length} characters");
                    System.Diagnostics.Debug.WriteLine($"JSON content:\n{json}");

                    _config = JsonSerializer.Deserialize<ConfigData>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    });

                    System.Diagnostics.Debug.WriteLine($"[INFO] Configuration loaded successfully from: {configPath}");
                    Console.WriteLine($"[INFO] Configuration loaded from: {configPath}");
                    LogCurrentConfig();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to load config: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
                    Console.WriteLine($"[ERROR] Failed to load config: {ex.Message}");
                    _config = GetDefaultConfig();
                    LogCurrentConfig();
                }
            }
        }

        /// <summary>
        /// 현재 설정값을 로그로 출력합니다.
        /// </summary>
        private void LogCurrentConfig()
        {
            if (_config?.Database != null)
            {
                System.Diagnostics.Debug.WriteLine($"Database.Server: {_config.Database.Server}");
                System.Diagnostics.Debug.WriteLine($"Database.UserId: {_config.Database.UserId}");
                System.Diagnostics.Debug.WriteLine($"Database.DatabaseName: {_config.Database.DatabaseName}");
                System.Diagnostics.Debug.WriteLine($"Database.Port: {_config.Database.Port}");
                System.Diagnostics.Debug.WriteLine($"Generated ConnectionString: {DatabaseConnectionString}");
            }
        }

        /// <summary>
        /// 설정을 강제로 다시 로드합니다.
        /// </summary>
        public void Reload()
        {
            System.Diagnostics.Debug.WriteLine("=== AppConfig.Reload() called ===");
            LoadConfig();
        }

        /// <summary>
        /// 현재 설정을 appsettings.json 파일에 저장합니다.
        /// </summary>
        public void SaveConfig(ConfigData config)
        {
            lock (_configLock)
            {
                try
                {
                    string configPath = ConfigFilePath;
                    System.Diagnostics.Debug.WriteLine($"=== AppConfig SaveConfig ===");
                    System.Diagnostics.Debug.WriteLine($"Saving config to: {configPath}");

                    // JSON으로 직렬화 (들여쓰기 포함)
                    string json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    // 파일에 저장
                    File.WriteAllText(configPath, json);

                    // 내부 설정 업데이트
                    _config = config;

                    System.Diagnostics.Debug.WriteLine($"[INFO] Configuration saved successfully to: {configPath}");
                    Console.WriteLine($"[INFO] Configuration saved to: {configPath}");
                    LogCurrentConfig();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to save config: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
                    Console.WriteLine($"[ERROR] Failed to save config: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// 현재 설정 데이터를 반환합니다.
        /// </summary>
        public ConfigData GetCurrentConfig()
        {
            EnsureConfigLoaded();
            return _config ?? GetDefaultConfig();
        }

        /// <summary>
        /// 현재 메모리의 설정을 appsettings.json 파일에 덮어씁니다.
        /// </summary>
        public void OverwriteConfigFile()
        {
            lock (_configLock)
            {
                try
                {
                    EnsureConfigLoaded();

                    string configPath = ConfigFilePath;
                    System.Diagnostics.Debug.WriteLine($"=== AppConfig OverwriteConfigFile ===");
                    System.Diagnostics.Debug.WriteLine($"Overwriting config to: {configPath}");

                    if (_config == null)
                    {
                        throw new InvalidOperationException("No configuration loaded in memory.");
                    }

                    // JSON으로 직렬화 (들여쓰기 포함)
                    string json = JsonSerializer.Serialize(_config, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    // 파일에 덮어쓰기
                    File.WriteAllText(configPath, json);

                    System.Diagnostics.Debug.WriteLine($"[INFO] Configuration file overwritten successfully: {configPath}");
                    Console.WriteLine($"[INFO] Configuration file overwritten: {configPath}");
                    LogCurrentConfig();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to overwrite config file: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
                    Console.WriteLine($"[ERROR] Failed to overwrite config file: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// 설정이 로드되었는지 확인하고, 로드되지 않았으면 로드합니다.
        /// </summary>
        private void EnsureConfigLoaded()
        {
            if (_config == null)
            {
                LoadConfig();
            }
        }

        /// <summary>
        /// 기본 설정값 반환
        /// </summary>
        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                Database = new DatabaseConfig
                {
                    Server = "localhost",
                    UserId = "root",
                    Password = "asdf1358@@",
                    DatabaseName = "interplanetery_tabledb_local",
                    Port = 3306
                },
                Server = new ServerConfig
                {
                    Host = "localhost",
                    Port = 7777
                },
                Logging = new LoggingConfig
                {
                    LogLevel = "Information"
                }
            };
        }

        /// <summary>
        /// 기본 DB 연결 문자열
        /// </summary>
        private string GetDefaultConnectionString()
        {
            return "server=localhost;user=root;password=asdf1358@@;database=interplanetery_tabledb_local;port=3306;";
        }

        #region Config Data Classes

        public class ConfigData
        {
            public DatabaseConfig? Database { get; set; }
            public ServerConfig? Server { get; set; }
            public LoggingConfig? Logging { get; set; }
        }

        public class DatabaseConfig
        {
            /// <summary>
            /// 데이터베이스 서버 주소
            /// </summary>
            public string? Server { get; set; }

            /// <summary>
            /// 데이터베이스 사용자 ID
            /// </summary>
            public string? UserId { get; set; }

            /// <summary>
            /// 데이터베이스 비밀번호
            /// </summary>
            public string? Password { get; set; }

            /// <summary>
            /// 데이터베이스 이름
            /// </summary>
            public string? DatabaseName { get; set; }

            /// <summary>
            /// 데이터베이스 포트 (기본값: 3306)
            /// </summary>
            public int? Port { get; set; }

            /// <summary>
            /// 직접 연결 문자열 지정 (옵션, 이것이 있으면 우선 사용)
            /// </summary>
            public string? ConnectionString { get; set; }
        }

        public class ServerConfig
        {
            public string Host { get; set; } = "localhost";
            public int Port { get; set; } = 7777;
        }

        public class LoggingConfig
        {
            public string LogLevel { get; set; } = "Information";
        }

        #endregion
    }
}
