using ChatClientWPF.Models;
using System.Windows;
using System.Windows.Input;

namespace ChatClientWPF.ViewModels
{
    public class GameViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private long _serverTick;
        private string _gameStatus = "Waiting for game start...";

        public long ServerTick
        {
            get => _serverTick;
            set => SetProperty(ref _serverTick, value);
        }

        public string GameStatus
        {
            get => _gameStatus;
            set => SetProperty(ref _gameStatus, value);
        }

        public ICommand LeaveGameCommand { get; }

        public GameViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            LeaveGameCommand = new RelayCommand(ExecuteLeaveGame);

            _mainViewModel.ChatModel.OnGameState += OnGameState;
            _mainViewModel.ChatModel.OnGameEnded += OnGameEnded;
            
            GameStatus = "Game Started!";
        }

        private void ExecuteLeaveGame(object? obj)
        {
            // For now, just leave room which should trigger game end on server or just client disconnect
            // But usually we might want a specific LeaveGame protocol if it exists.
            // Assuming LeaveRoom is sufficient or we just go back to Lobby.
            _mainViewModel.ChatModel.LeaveRoomAsync();
            _mainViewModel.NavigateToLobby();
        }

        private void OnGameState(long serverTick)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ServerTick = serverTick;
                GameStatus = $"Game Running... Tick: {serverTick}";
            });
        }

        private void OnGameEnded()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                GameStatus = "Game Ended!";
                MessageBox.Show("Game Over!");
                _mainViewModel.NavigateToRoom(); // Go back to room waiting screen
            });
        }

        // Cleanup when view model is switched away? 
        // In a real app we might implement IDisposable or a Cleanup method.
        // For now, we rely on GC and event unsubscription if we had a proper lifecycle management.
        // But since MainViewModel creates new instances, we should be careful about event leaks.
        // Ideally ViewModelBase should have a Dispose/Cleanup.
        // Let's add a simple Unsubscribe method and call it before navigating away in MainViewModel if possible,
        // or just accept it for this test client.
    }
}
