using ChatClientWPF.Views;
using System;
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

        public string ServerHost
        {
            get => ServerHostTextBox.Text;
            set
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(() => ServerHost = value);
                    return;
                }
                ServerHostTextBox.Text = value;
            }
        }

        public int ServerPort
        {
            get
            {
                if (int.TryParse(ServerPortTextBox.Text, out int port))
                    return port;
                return 7777;
            }
            set
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(() => ServerPort = value);
                    return;
                }
                ServerPortTextBox.Text = value.ToString();
            }
        }

        public SettingsPage()
        {
            InitializeComponent();

            // 텍스트 변경 시 연결 문자열 미리보기 업데이트
            DatabaseServerTextBox.TextChanged += (s, e) => OnSettingsChanged?.Invoke();
            DatabaseUserIdTextBox.TextChanged += (s, e) => OnSettingsChanged?.Invoke();
            DatabasePasswordBox.PasswordChanged += (s, e) => OnSettingsChanged?.Invoke();
            DatabaseNameTextBox.TextChanged += (s, e) => OnSettingsChanged?.Invoke();
            DatabasePortTextBox.TextChanged += (s, e) => OnSettingsChanged?.Invoke();
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
    }
}
