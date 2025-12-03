using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ChatClientWPF.Models;
using ChatClientWPF.Utils;
using CommonLib;

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
        private string _statusMessage = "";

        // Logger 구독
        private void SubscribeToLogger()
        {
            Logger.OnLog += (msg) => StatusMessage = msg;
            Logger.OnLogError += (msg) => StatusMessage = "ERROR: " + msg;
        }

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

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }

        public LoginViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
            RegisterCommand = new RelayCommand(ExecuteRegister, CanExecuteLogin);

            // Logger 구독
            SubscribeToLogger();
        }

        private bool CanExecuteLogin(object? obj) => !IsConnecting && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

        private async void ExecuteLogin(object? obj)
        {
            IsConnecting = true;
            StatusMessage = "로그인 중...";

            try
            {
                var handler = ClientServerHandler.Instance;

                // 서버 연결
                if (!handler.IsConnected)
                {
                    StatusMessage = $"서버 연결 중... {ServerIp}:{ServerPort}";
                    await handler.ConnectAsync(ServerIp, ServerPort);

                    if (!handler.IsConnected)
                    {
                        StatusMessage = "서버 연결 실패";
                        MessageBox.Show("서버에 연결할 수 없습니다.", "연결 실패");
                        IsConnecting = false;
                        return;
                    }
                    StatusMessage = "서버 연결 성공";
                }

                // 로그인 요청
                var protocol = new Protocol(ProtocolType.REQUEST_LOGIN)
                    .AddParam("username", Username)
                    .AddParam("password", Password);

                StatusMessage = "로그인 요청 전송 중...";
                var response = await handler.AsyncSend(protocol);

                Logger.Log($"[LoginViewModel] 로그인 응답: isSuccess={response.isSuccess}, resultCode={response.resultCode}");

                if (response.isSuccess)
                {
                    // 로그인 성공 - Unity 클라이언트와 동일한 파라미터 사용
                    string sessionToken = response.GetParam<string>("sessionToken");
                    int userId = response.GetParam<int>("userId");
                    string username = response.GetParam<string>("username") ?? Username;

                    var userInfo = new UserInfo
                    {
                        UserId = userId,
                        UserName = username
                    };

                    StatusMessage = $"로그인 성공! 사용자: {userInfo.UserName}";
                    Logger.Log($"[LoginViewModel] 로그인 성공 - UserID: {userInfo.UserId}, SessionToken: {sessionToken}");

                    // RoomManager 초기화
                    await RoomManager.Instance.Initailize(userInfo, sessionToken);

                    // 로비로 이동
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _mainViewModel.NavigateToLobby();
                    });
                }
                else
                {
                    StatusMessage = "로그인 실패: " + response.resultCode;
                    MessageBox.Show(response.GetParam<string>("message") ?? "로그인에 실패했습니다.", "로그인 실패");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "오류: " + ex.Message;
                Logger.LogError($"[LoginViewModel] 로그인 오류: {ex.Message}");
                MessageBox.Show($"로그인 중 오류가 발생했습니다:\n{ex.Message}", "오류");
            }
            finally
            {
                IsConnecting = false;
            }
        }

        private async void ExecuteRegister(object? obj)
        {
            IsConnecting = true;
            StatusMessage = "회원가입 중...";

            try
            {
                var handler = ClientServerHandler.Instance;

                // 서버 연결
                if (!handler.IsConnected)
                {
                    StatusMessage = $"서버 연결 중... {ServerIp}:{ServerPort}";
                    await handler.ConnectAsync(ServerIp, ServerPort);

                    if (!handler.IsConnected)
                    {
                        StatusMessage = "서버 연결 실패";
                        MessageBox.Show("서버에 연결할 수 없습니다.", "연결 실패");
                        IsConnecting = false;
                        return;
                    }
                    StatusMessage = "서버 연결 성공";
                }

                // 회원가입 요청
                var protocol = new Protocol(ProtocolType.REQUEST_REGISTER)
                    .AddParam("username", Username)
                    .AddParam("password", Password);

                StatusMessage = "회원가입 요청 전송 중...";
                var response = await handler.AsyncSend(protocol);

                if (response.isSuccess)
                {
                    StatusMessage = "회원가입 성공!";
                    MessageBox.Show("회원가입이 완료되었습니다. 로그인해주세요.", "회원가입 성공");
                }
                else
                {
                    StatusMessage = "회원가입 실패: " + response.resultCode;
                    MessageBox.Show(response.GetParam<string>("message") ?? "회원가입에 실패했습니다.", "회원가입 실패");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "오류: " + ex.Message;
                Logger.LogError($"[LoginViewModel] 회원가입 오류: {ex.Message}");
                MessageBox.Show($"회원가입 중 오류가 발생했습니다:\n{ex.Message}", "오류");
            }
            finally
            {
                IsConnecting = false;
            }
        }
    }
}
