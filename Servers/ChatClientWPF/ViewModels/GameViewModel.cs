using ChatClientWPF.Utils;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace ChatClientWPF.ViewModels
{
    public class GameViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private string _gameLog = "";
        private string _gameStatus = "게임 대기 중...";

        public string GameLog
        {
            get => _gameLog;
            set => SetProperty(ref _gameLog, value);
        }

        public string GameStatus
        {
            get => _gameStatus;
            set => SetProperty(ref _gameStatus, value);
        }

        public ICommand LeaveGameCommand { get; }

        private StringBuilder logBuilder = new StringBuilder();

        public GameViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            LeaveGameCommand = new RelayCommand(ExecuteLeaveGame);

            // Logger 구독하여 게임 로그 표시
            Logger.OnLog += OnLogMessage;
            Logger.OnLogError += OnLogError;
            Logger.OnLogWarning += OnLogWarning;

            GameStatus = "게임 시작됨!";
            AddLog("[GameViewModel] 게임 화면 초기화 완료");
            AddLog("[INFO] 이 화면은 더미 클라이언트용 로그 표시 화면입니다.");
            AddLog("[INFO] 유니티 클라이언트와 함께 멀티플레이 테스트에 사용됩니다.");
        }

        private void ExecuteLeaveGame(object? obj)
        {
            AddLog("[GameViewModel] 게임 나가기 요청");
            _mainViewModel.NavigateToLobby();
        }

        private void OnLogMessage(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                AddLog($"[LOG] {message}");
            });
        }

        private void OnLogError(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                AddLog($"[ERROR] {message}");
            });
        }

        private void OnLogWarning(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                AddLog($"[WARNING] {message}");
            });
        }

        private void AddLog(string message)
        {
            logBuilder.AppendLine(message);
            GameLog = logBuilder.ToString();

            // 로그가 너무 길어지면 처음 부분 제거 (최대 1000줄 유지)
            var lines = GameLog.Split('\n');
            if (lines.Length > 1000)
            {
                logBuilder.Clear();
                for (int i = lines.Length - 1000; i < lines.Length; i++)
                {
                    logBuilder.AppendLine(lines[i]);
                }
                GameLog = logBuilder.ToString();
            }
        }
    }
}
