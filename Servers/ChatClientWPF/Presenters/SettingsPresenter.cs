using ChatClientWPF.Views;
using CommonLib;
using MySql.Data.MySqlClient;
using System;
using System.Diagnostics;

namespace ChatClientWPF.Presenters
{
    /// <summary>
    /// SettingsView의 Presenter - 설정 관리 로직 담당
    /// </summary>
    public class SettingsPresenter
    {
        private readonly ISettingsView _view;

        public SettingsPresenter(ISettingsView view)
        {
            _view = view;

            // 이벤트 핸들러 연결
            _view.OnSaveClicked += HandleSaveClicked;
            _view.OnReloadClicked += HandleReloadClicked;
            _view.OnTestConnectionClicked += HandleTestConnectionClicked;
            _view.OnOverwriteFileClicked += HandleOverwriteFileClicked;
            _view.OnSettingsChanged += HandleSettingsChanged;

            // 초기 설정값 로드
            LoadCurrentSettings();
        }

        /// <summary>
        /// 현재 AppConfig의 설정값을 View에 로드
        /// </summary>
        private void LoadCurrentSettings()
        {
            try
            {
                Debug.WriteLine("=== LoadCurrentSettings 호출됨 ===");

                var config = AppConfig.Instance;

                _view.DatabaseServer = config.DatabaseServer;
                _view.DatabaseUserId = config.DatabaseUserId;
                _view.DatabasePassword = config.DatabasePassword;
                _view.DatabaseName = config.DatabaseName;
                _view.DatabasePort = config.DatabasePort;
                _view.ServerHost = config.ServerHost;
                _view.ServerPort = config.ServerPort;

                // 연결 문자열 미리보기 업데이트
                UpdateConnectionStringPreview();

                Debug.WriteLine("=== 설정값 로드 완료 ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"설정 로드 실패: {ex.Message}");
                _view.ShowError($"Failed to load settings:\n{ex.Message}");
            }
        }

        /// <summary>
        /// 저장 버튼 클릭 핸들러
        /// </summary>
        private void HandleSaveClicked()
        {
            try
            {
                Debug.WriteLine("=== HandleSaveClicked 호출됨 ===");

                // View에서 설정값 가져오기
                var config = new AppConfig.ConfigData
                {
                    Database = new AppConfig.DatabaseConfig
                    {
                        Server = _view.DatabaseServer,
                        UserId = _view.DatabaseUserId,
                        Password = _view.DatabasePassword,
                        DatabaseName = _view.DatabaseName,
                        Port = _view.DatabasePort
                    },
                    Server = new AppConfig.ServerConfig
                    {
                        Host = _view.ServerHost,
                        Port = _view.ServerPort
                    },
                    Logging = new AppConfig.LoggingConfig
                    {
                        LogLevel = "Debug"
                    }
                };

                // AppConfig에 저장
                AppConfig.Instance.SaveConfig(config);

                _view.ShowSuccess("Settings saved successfully to appsettings.json!\n\nNote: Some components may need to be reinitialized to use the new settings.");
                Debug.WriteLine("=== 설정 저장 완료 ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"설정 저장 실패: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                _view.ShowError($"Failed to save settings:\n{ex.Message}");
            }
        }

        /// <summary>
        /// 다시 로드 버튼 클릭 핸들러
        /// </summary>
        private void HandleReloadClicked()
        {
            try
            {
                Debug.WriteLine("=== HandleReloadClicked 호출됨 ===");

                // appsettings.json에서 다시 로드
                AppConfig.Instance.Reload();

                // View에 반영
                LoadCurrentSettings();

                _view.ShowInfo("Settings reloaded from appsettings.json successfully!");
                Debug.WriteLine("=== 설정 다시 로드 완료 ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"설정 다시 로드 실패: {ex.Message}");
                _view.ShowError($"Failed to reload settings:\n{ex.Message}");
            }
        }

        /// <summary>
        /// 연결 테스트 버튼 클릭 핸들러
        /// </summary>
        private void HandleTestConnectionClicked()
        {
            try
            {
                Debug.WriteLine("=== HandleTestConnectionClicked 호출됨 ===");

                // 현재 View의 설정값으로 연결 문자열 생성
                string connectionString = $"server={_view.DatabaseServer};" +
                                        $"user={_view.DatabaseUserId};" +
                                        $"password={_view.DatabasePassword};" +
                                        $"database={_view.DatabaseName};" +
                                        $"port={_view.DatabasePort};";

                Debug.WriteLine($"Testing connection: {connectionString.Replace(_view.DatabasePassword, "***")}");

                // MySQL 연결 테스트
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    Debug.WriteLine("연결 테스트 성공!");

                    // 서버 버전 확인
                    string serverVersion = connection.ServerVersion;
                    _view.ShowSuccess($"Connection test successful!\n\nServer Version: {serverVersion}");
                }
            }
            catch (MySqlException mysqlEx)
            {
                Debug.WriteLine($"MySQL 연결 실패: {mysqlEx.Message}");
                _view.ShowError($"Database connection failed:\n\n{mysqlEx.Message}\n\nPlease check your database settings.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"연결 테스트 실패: {ex.Message}");
                _view.ShowError($"Connection test failed:\n{ex.Message}");
            }
        }

        /// <summary>
        /// 파일 덮어쓰기 버튼 클릭 핸들러
        /// </summary>
        private void HandleOverwriteFileClicked()
        {
            try
            {
                Debug.WriteLine("=== HandleOverwriteFileClicked 호출됨 ===");

                // 사용자에게 확인 요청
                var result = System.Windows.MessageBox.Show(
                    "This will overwrite the appsettings.json file with the current in-memory configuration.\n\n" +
                    "WARNING: This operation cannot be undone!\n\n" +
                    "Do you want to continue?",
                    "Confirm Overwrite",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning
                );

                if (result != System.Windows.MessageBoxResult.Yes)
                {
                    Debug.WriteLine("덮어쓰기 취소됨");
                    return;
                }

                // 현재 메모리의 설정을 파일에 덮어쓰기
                AppConfig.Instance.OverwriteConfigFile();

                _view.ShowSuccess("Configuration file overwritten successfully!\n\nThe appsettings.json file now contains the current in-memory settings.");
                Debug.WriteLine("=== 설정 파일 덮어쓰기 완료 ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"설정 파일 덮어쓰기 실패: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                _view.ShowError($"Failed to overwrite configuration file:\n{ex.Message}");
            }
        }

        /// <summary>
        /// 설정값 변경 핸들러 - 연결 문자열 미리보기 업데이트
        /// </summary>
        private void HandleSettingsChanged()
        {
            UpdateConnectionStringPreview();
        }

        /// <summary>
        /// 연결 문자열 미리보기 업데이트
        /// </summary>
        private void UpdateConnectionStringPreview()
        {
            try
            {
                string connectionString = $"server={_view.DatabaseServer};" +
                                        $"user={_view.DatabaseUserId};" +
                                        $"password={_view.DatabasePassword};" +
                                        $"database={_view.DatabaseName};" +
                                        $"port={_view.DatabasePort};";

                _view.UpdateConnectionStringPreview(connectionString);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"연결 문자열 미리보기 업데이트 실패: {ex.Message}");
            }
        }
    }
}
