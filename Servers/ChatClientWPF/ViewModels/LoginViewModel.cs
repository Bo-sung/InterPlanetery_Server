using System.Windows;
using System.Windows.Input;

namespace ChatClientWPF.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private string _username = "Player_" + new Random().Next(1000, 9999);
        private string _password = "password";
        private string _serverIp = "127.0.0.1";
        private int _serverPort = 9000;
        private bool _isConnecting;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ServerIp
        {
            get => _serverIp;
            set => SetProperty(ref _serverIp, value);
        }

        public int ServerPort
        {
            get => _serverPort;
            set => SetProperty(ref _serverPort, value);
        }

        public bool IsConnecting
        {
            get => _isConnecting;
            set => SetProperty(ref _isConnecting, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }

        public LoginViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
            RegisterCommand = new RelayCommand(ExecuteRegister, CanExecuteLogin);

            _mainViewModel.ChatModel.OnLoginResult += OnLoginResult;
            _mainViewModel.ChatModel.OnRegisterResult += OnRegisterResult;
        }

        private bool CanExecuteLogin(object? obj) => !IsConnecting && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

        private async void ExecuteLogin(object? obj)
        {
            IsConnecting = true;
            if (!_mainViewModel.ChatModel.IsConnected)
            {
                bool connected = await _mainViewModel.ChatModel.ConnectAsync(ServerIp, ServerPort);
                if (!connected)
                {
                    IsConnecting = false;
                    return;
                }
            }

            await _mainViewModel.ChatModel.LoginAsync(Username, Password);
        }

        private async void ExecuteRegister(object? obj)
        {
            IsConnecting = true;
            if (!_mainViewModel.ChatModel.IsConnected)
            {
                bool connected = await _mainViewModel.ChatModel.ConnectAsync(ServerIp, ServerPort);
                if (!connected)
                {
                    IsConnecting = false;
                    return;
                }
            }

            await _mainViewModel.ChatModel.RegisterAsync(Username, Password);
        }

        private void OnLoginResult(bool success, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsConnecting = false;
                if (success)
                {
                    _mainViewModel.NavigateToLobby();
                }
                else
                {
                    MessageBox.Show(message, "로그인 실패");
                }
            });
        }

        private void OnRegisterResult(bool success, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsConnecting = false;
                MessageBox.Show(message, success ? "회원가입 성공" : "회원가입 실패");
            });
        }
    }
}
