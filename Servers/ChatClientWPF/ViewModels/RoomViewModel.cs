using ChatClientWPF.Models;
using CommonLib;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Linq;

namespace ChatClientWPF.ViewModels
{
    public class RoomViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private RoomInfo _currentRoom;
        private ObservableCollection<ChatMessage> _chatMessages;
        private ObservableCollection<WaittingRoomUser> _roomUsers;
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

        public ObservableCollection<WaittingRoomUser> RoomUsers
        {
            get => _roomUsers;
            set => SetProperty(ref _roomUsers, value);
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
        public ICommand RefreshRoomInfoCommand { get; }

        public RoomViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _chatMessages = new ObservableCollection<ChatMessage>();
            _roomUsers = new ObservableCollection<WaittingRoomUser>();

            LeaveRoomCommand = new RelayCommand(ExecuteLeaveRoom);
            SendChatCommand = new RelayCommand(ExecuteSendChat);
            ReadyCommand = new RelayCommand(ExecuteReady);
            RefreshRoomInfoCommand = new RelayCommand(ExecuteRefreshRoomInfo);

            _mainViewModel.ChatModel.OnRoomInfoChanged += OnRoomInfoChanged;
            _mainViewModel.ChatModel.OnChatMessageReceived += OnChatMessageReceived;
            _mainViewModel.ChatModel.OnRoomClosed += OnRoomClosed;
            _mainViewModel.ChatModel.OnUserJoinedRoom += OnUserJoinedRoom;
            _mainViewModel.ChatModel.OnUserLeftRoom += OnUserLeftRoom;
            _mainViewModel.ChatModel.OnRoomInfoRefreshed += OnRoomInfoRefreshed;
            _mainViewModel.ChatModel.OnRoomJoined += OnRoomJoined;

            // 방 정보 자동 새로고침
            _ = _mainViewModel.ChatModel.RefreshRoomInfoAsync();
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
            _mainViewModel.ChatModel.ToggleReadyAsync();
        }

        private void ExecuteRefreshRoomInfo(object? obj)
        {
            _mainViewModel.ChatModel.RefreshRoomInfoAsync();
        }

        private void OnRoomJoined(RoomInfo roomInfo, int chatChannelId)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentRoom = roomInfo;
                // 방 입장 후 정보 새로고침
                _ = _mainViewModel.ChatModel.RefreshRoomInfoAsync();
            });
        }

        private void OnRoomInfoChanged(RoomInfo roomInfo)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentRoom = roomInfo;
            });
        }

        private void OnRoomInfoRefreshed(RoomInfo roomInfo, WaittingRoomUser[] users)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentRoom = roomInfo;
                RoomUsers.Clear();
                foreach (var user in users)
                {
                    RoomUsers.Add(user);
                }

                // 내 Ready 상태 업데이트
                var myUser = users.FirstOrDefault(u => u.userInfo.UserName == _mainViewModel.ChatModel.UserName);
                if (myUser.userInfo.UserName != null)
                {
                    IsReady = myUser.IsReady;
                }
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
                // 방 정보 새로고침
                _ = _mainViewModel.ChatModel.RefreshRoomInfoAsync();
            });
        }

        private void OnUserLeftRoom(int userId, int playerCount)
        {
             Application.Current.Dispatcher.Invoke(() =>
            {
                ChatMessages.Add(new ChatMessage { SenderId = "SYSTEM", Message = $"플레이어 {userId} 님이 퇴장하셨습니다." });
                // 방 정보 새로고침
                _ = _mainViewModel.ChatModel.RefreshRoomInfoAsync();
            });
        }
    }
}
