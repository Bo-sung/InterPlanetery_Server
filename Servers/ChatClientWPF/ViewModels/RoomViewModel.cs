using ChatClientWPF.Models;
using ChatClientWPF.Utils;
using CommonLib;
using CommonLib.TableData;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

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
        private string _statusMessage = "";

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

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
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

            // RoomManager 이벤트 구독
            var roomManager = RoomManager.Instance;
            roomManager.OnWaittingRoomInfoChanged += OnWaittingRoomInfoChanged;
            roomManager.OnRoomLeft += OnRoomLeft;
            roomManager.OnGameStarting += OnGameStarting;
            roomManager.OnUserJoinedRoom += OnUserJoinedRoom;
            roomManager.OnUserLeftRoom += OnUserLeftRoom;
            roomManager.OnStatusMessage += (msg) => StatusMessage = msg;
            roomManager.OnError += (err) => StatusMessage = "ERROR: " + err;

            // Logger 구독
            Logger.OnLog += (msg) => StatusMessage = msg;
            Logger.OnLogError += (err) => StatusMessage = "ERROR: " + err;

            // 현재 방 정보 가져오기
            var currentRoomInfo = roomManager.CachedCurrRoom;
            if (currentRoomInfo.Item1.RoomId != null)
            {
                CurrentRoom = currentRoomInfo.Item1;
                UpdateRoomUsers(currentRoomInfo.Item2);
            }

            StatusMessage = "대기실에 입장했습니다";
        }

        private async void ExecuteLeaveRoom(object? obj)
        {
            StatusMessage = "방 나가는 중...";
            await RoomManager.Instance.RequestLeaveRoomAsync();
            _mainViewModel.NavigateToLobby();
        }

        private async void ExecuteSendChat(object? obj)
        {
            if (string.IsNullOrWhiteSpace(ChatInput)) return;

            // 채팅은 나중에 구현 (GamePlayManager 필요)
            ChatMessages.Add(new ChatMessage
            {
                SenderId = "ME",
                Message = ChatInput
            });

            StatusMessage = $"채팅 전송: {ChatInput}";
            ChatInput = "";
        }

        private async void ExecuteReady(object? obj)
        {
            bool newReadyState = !IsReady;
            StatusMessage = newReadyState ? "준비 중..." : "준비 취소 중...";

            bool success = await RoomManager.Instance.RequestReadyAsync(newReadyState);
            if (success)
            {
                IsReady = newReadyState;
                StatusMessage = newReadyState ? "준비 완료!" : "준비 취소됨";
            }
        }

        private async void ExecuteRefreshRoomInfo(object? obj)
        {
            StatusMessage = "방 정보 새로고침 중...";
            await RoomManager.Instance.RequestJoinedRoomInfoRefresh();
        }

        private void OnWaittingRoomInfoChanged(RoomInfo roomInfo, WaittingRoomUser[] users)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentRoom = roomInfo;
                UpdateRoomUsers(users);
                StatusMessage = $"방 정보 업데이트: {roomInfo.PlayerCount}/{roomInfo.MaxPlayers}명";
            });
        }

        private void UpdateRoomUsers(WaittingRoomUser[] users)
        {
            RoomUsers.Clear();
            foreach (var user in users)
            {
                RoomUsers.Add(user);
            }

            // 내 Ready 상태 업데이트
            // Note: users 배열에서 첫 번째 유저가 본인이라고 가정 (서버 구현 확인 필요)
            if (users.Length > 0)
            {
                // 모든 사용자 중에서 Ready 상태를 확인
                foreach (var user in users)
                {
                    // IsReady 상태는 서버에서 브로드캐스트로 받을 것이므로 여기서는 일단 스킵
                    // 실제 IsReady는 ROOM_INFO_CHANGED 이벤트에서 업데이트됨
                }
            }
        }

        private void OnRoomLeft()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = "방에서 나갔습니다";
                _mainViewModel.NavigateToLobby();
            });
        }

        private void OnGameStarting()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = "게임이 시작됩니다!";
                MessageBox.Show("게임이 곧 시작됩니다!", "게임 시작");
                _mainViewModel.NavigateToGame();
            });
        }

        private void OnUserJoinedRoom(int userId, string userName, int playerCount)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = $"{userName}님이 입장했습니다 ({playerCount}명)";
                // 방 정보 새로고침하여 유저 목록 업데이트
                ExecuteRefreshRoomInfo(null);
            });
        }

        private void OnUserLeftRoom(int userId, string userName, int playerCount)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = $"{userName}님이 퇴장했습니다 ({playerCount}명)";
                // 방 정보 새로고침하여 유저 목록 업데이트
                ExecuteRefreshRoomInfo(null);
            });
        }
    }
}
