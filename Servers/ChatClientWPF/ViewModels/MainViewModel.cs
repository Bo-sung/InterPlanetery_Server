using ChatClientWPF.Models;
using System.Windows;

namespace ChatClientWPF.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase _currentViewModel;
        private readonly ChatClientModel _chatModel;

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public ChatClientModel ChatModel => _chatModel;

        public MainViewModel()
        {
            _chatModel = new ChatClientModel();
            _chatModel.OnError += OnError;
            _chatModel.OnGameStarted += OnGameStarted;
            
            // Start with Login View
            
            // Start with Login View
            NavigateToLogin();
        }

        private void OnError(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        public void NavigateToLogin()
        {
            CurrentViewModel = new LoginViewModel(this);
        }

        public void NavigateToLobby()
        {
            CurrentViewModel = new LobbyViewModel(this);
        }

        public void NavigateToRoom()
        {
            CurrentViewModel = new RoomViewModel(this);
        }

        public void NavigateToGame()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentViewModel = new GameViewModel(this);
            });
        }

        private void OnGameStarted()
        {
            NavigateToGame();
        }
    }
}
