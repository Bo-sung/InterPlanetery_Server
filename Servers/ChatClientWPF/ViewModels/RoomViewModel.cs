using ChatClientWPF.Models;
using CommonLib;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ChatClientWPF.ViewModels
{
    public class RoomViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private RoomInfo _currentRoom;
        private ObservableCollection<ChatMessage> _chatMessages;
        private string _chatInput = "";
        private bool _isReady;

        public RoomInfo CurrentRoom
        {
            get => _currentRoom;
            set => SetProperty(ref _currentRoom, value);
        }

        public ObservableCollection<ChatMessage> ChatMessages
        {
            get => _chatMessages;
            set => SetProperty(ref _chatMessages, value);
        }

        public string ChatInput
        {
            get => _chatInput;
            set => SetProperty(ref _chatInput, value);
        }

        public bool IsReady
        {
            get => _isReady;
            set => SetProperty(ref _isReady, value);
        }

        public ICommand LeaveRoomCommand { get; }
        public ICommand SendChatCommand { get; }
        public ICommand ReadyCommand { get; }

        public RoomViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _chatMessages = new ObservableCollection<ChatMessage>();
            
            // Initialize with current room info if available (it should be)
            // We might need to store CurrentRoomInfo in MainViewModel or ChatModel if we want to access it synchronously here
            // But usually we get OnRoomJoined event before navigating here.
            // Let's assume we can get it or wait for update.
            
            LeaveRoomCommand = new RelayCommand(ExecuteLeaveRoom);
            SendChatCommand = new RelayCommand(ExecuteSendChat);
            ReadyCommand = new RelayCommand(ExecuteReady);

            _mainViewModel.ChatModel.OnRoomInfoChanged += OnRoomInfoChanged;
            _mainViewModel.ChatModel.OnChatMessageReceived += OnChatMessageReceived;
            _mainViewModel.ChatModel.OnRoomClosed += OnRoomClosed;
            _mainViewModel.ChatModel.OnUserJoinedRoom += OnUserJoinedRoom;
            _mainViewModel.ChatModel.OnUserLeftRoom += OnUserLeftRoom;
        }

        private void ExecuteLeaveRoom(object? obj)
        {
            _mainViewModel.ChatModel.LeaveRoomAsync();
            _mainViewModel.NavigateToLobby();
        }

        private void ExecuteSendChat(object? obj)
        {
            if (string.IsNullOrWhiteSpace(ChatInput)) return;
            _mainViewModel.ChatModel.SendChatMessageAsync(ChatInput, 1, 0); // 1 for InGame/Room chat?
            ChatInput = "";
        }

        private void ExecuteReady(object? obj)
        {
            IsReady = !IsReady;
            // TODO: Send Ready Protocol
        }

        private void OnRoomInfoChanged(RoomInfo roomInfo)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentRoom = roomInfo;
            });
        }

        private void OnChatMessageReceived(ChatMessage message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ChatMessages.Add(message);
            });
        }

        private void OnRoomClosed(string roomId, string reason)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"방이 닫혔습니다: {reason}");
                _mainViewModel.NavigateToLobby();
            });
        }

        private void OnUserJoinedRoom(int userId, string userName, int playerCount)
        {
             Application.Current.Dispatcher.Invoke(() =>
            {
                ChatMessages.Add(new ChatMessage { SenderId = "SYSTEM", Message = $"{userName} 님이 입장하셨습니다." });
            });
        }

        private void OnUserLeftRoom(int userId, int playerCount)
        {
             Application.Current.Dispatcher.Invoke(() =>
            {
                ChatMessages.Add(new ChatMessage { SenderId = "SYSTEM", Message = $"플레이어 {userId} 님이 퇴장하셨습니다." });
            });
        }
    }
}
