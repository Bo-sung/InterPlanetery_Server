using ChatClientWPF.Views;
using CommonLib;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace ChatClientWPF.Pages
{
    /// <summary>
    /// SettingsPage - 애플리케이션 설정 페이지
    /// appsettings.json 값을 UI에서 수정하고 저장 가능
    /// </summary>
    public partial class SettingsPage : Page, ISettingsView
    {
        public event Action? OnSaveClicked;
        public event Action? OnReloadClicked;
        public event Action? OnTestConnectionClicked;
        public event Action? OnOverwriteFileClicked;
        public event Action? OnSettingsChanged;

        private ObservableCollection<AppConfig.GameServerConfig> _gameServers;

        public string DatabaseServer
        {
            get => DatabaseServerTextBox.Text;
            set
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(() => DatabaseServer = value);
                    return;
                }
                DatabaseServerTextBox.Text = value;
            }
        }

        public string DatabaseUserId
        {
            get => DatabaseUserIdTextBox.Text;
            set
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(() => DatabaseUserId = value);
                    return;
                }
                DatabaseUserIdTextBox.Text = value;
            }
        }

        public string DatabasePassword
        {
            get => DatabasePasswordBox.Password;
            set
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(() => DatabasePassword = value);
                    return;
                }
                DatabasePasswordBox.Password = value;
            }
        }

        public string DatabaseName
        {
            get => DatabaseNameTextBox.Text;
            set
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(() => DatabaseName = value);
                    return;
                }
                DatabaseNameTextBox.Text = value;
            }
        }

        public int DatabasePort
        {
            get
            {
                if (int.TryParse(DatabasePortTextBox.Text, out int port))
                    return port;
                return 3306;
            }
            set
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(() => DatabasePort = value);
                    return;
                }
                DatabasePortTextBox.Text = value.ToString();
            }
        }

        public SettingsPage()
        {
            InitializeComponent();

            // GameServers 컬렉션 초기화
            _gameServers = new ObservableCollection<AppConfig.GameServerConfig>();
            GameServersItemsControl.ItemsSource = _gameServers;

            // 텍스트 변경 시 연결 문자열 미리보기 업데이트
            DatabaseServerTextBox.TextChanged += (s, e) => OnSettingsChanged?.Invoke();
            DatabaseUserIdTextBox.TextChanged += (s, e) => OnSettingsChanged?.Invoke();
            DatabasePasswordBox.PasswordChanged += (s, e) => OnSettingsChanged?.Invoke();
            DatabaseNameTextBox.TextChanged += (s, e) => OnSettingsChanged?.Invoke();
            DatabasePortTextBox.TextChanged += (s, e) => OnSettingsChanged?.Invoke();
        }

        /// <summary>
        /// GameServers 목록을 로드합니다.
        /// </summary>
        public void LoadGameServers()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => LoadGameServers());
                return;
            }

            _gameServers.Clear();
            foreach (var server in AppConfig.Instance.GameServers)
            {
                _gameServers.Add(server);
            }
        }

        /// <summary>
        /// 현재 GameServers 목록을 반환합니다.
        /// </summary>
        public ObservableCollection<AppConfig.GameServerConfig> GetGameServers()
        {
            return _gameServers;
        }

        public void UpdateConnectionStringPreview(string connectionString)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdateConnectionStringPreview(connectionString));
                return;
            }

            // 비밀번호는 ***로 마스킹
            var maskedConnectionString = System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"password=([^;]*);",
                "password=***;",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            ConnectionStringPreview.Text = maskedConnectionString;
        }

        public void ShowSuccess(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ShowSuccess(message));
                return;
            }

            MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowError(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ShowError(message));
                return;
            }

            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowInfo(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ShowInfo(message));
                return;
            }

            MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            OnSaveClicked?.Invoke();
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            OnReloadClicked?.Invoke();
        }

        private void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            OnTestConnectionClicked?.Invoke();
        }

        private void OverwriteFileButton_Click(object sender, RoutedEventArgs e)
        {
            OnOverwriteFileClicked?.Invoke();
        }

        private void AddServerButton_Click(object sender, RoutedEventArgs e)
        {
            // 폼 표시
            AddServerFormBorder.Visibility = Visibility.Visible;
            NewServerNameTextBox.Clear();
            NewServerHostTextBox.Clear();
            NewServerPortTextBox.Clear();
            NewServerNameTextBox.Focus();
        }

        private void CancelAddServerButton_Click(object sender, RoutedEventArgs e)
        {
            // 폼 숨김
            AddServerFormBorder.Visibility = Visibility.Collapsed;
        }

        private void ConfirmAddServerButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NewServerNameTextBox.Text.Trim();
            string host = NewServerHostTextBox.Text.Trim();
            string portText = NewServerPortTextBox.Text.Trim();

            // 유효성 검사
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("서버 이름을 입력하세요.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(host))
            {
                MessageBox.Show("서버 호스트를 입력하세요.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(portText, out int port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("유효한 포트 번호를 입력하세요. (1-65535)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 중복 확인
            foreach (var server in _gameServers)
            {
                if (server.Name == name)
                {
                    MessageBox.Show($"'{name}' 서버는 이미 존재합니다.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // 새 서버 추가
            var newServer = new AppConfig.GameServerConfig
            {
                Name = name,
                Host = host,
                Port = port
            };

            _gameServers.Add(newServer);
            AddServerFormBorder.Visibility = Visibility.Collapsed;
            MessageBox.Show("서버가 추가되었습니다.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string serverName)
            {
                var result = MessageBox.Show(
                    $"'{serverName}' 서버를 삭제하시겠습니까?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    var serverToDelete = _gameServers.FirstOrDefault(s => s.Name == serverName);
                    if (serverToDelete != null)
                    {
                        _gameServers.Remove(serverToDelete);
                        MessageBox.Show("서버가 삭제되었습니다.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }
    }
}
