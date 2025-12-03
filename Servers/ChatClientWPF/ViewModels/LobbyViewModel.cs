using ChatClientWPF.Models;
using ChatClientWPF.Utils;
using CommonLib;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ChatClientWPF.ViewModels
{
    public class LobbyViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private ObservableCollection<RoomInfo> _roomList;
        private string _newRoomName = "New Room";
        private int _selectedMapId = 1;
        private bool _isPrivate;
        private string _statusMessage = "";

        public ObservableCollection<RoomInfo> RoomList
        {
            get => _roomList;
            set => SetProperty(ref _roomList, value);
        }

        public string NewRoomName
        {
            get => _newRoomName;
            set => SetProperty(ref _newRoomName, value);
        }

        public int SelectedMapId
        {
            get => _selectedMapId;
            set => SetProperty(ref _selectedMapId, value);
        }

        public bool IsPrivate
        {
            get => _isPrivate;
            set => SetProperty(ref _isPrivate, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand CreateRoomCommand { get; }
        public ICommand JoinRoomCommand { get; }
        public ICommand LogoutCommand { get; }

        public LobbyViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _roomList = new ObservableCollection<RoomInfo>();

            RefreshCommand = new RelayCommand(ExecuteRefresh);
            CreateRoomCommand = new RelayCommand(ExecuteCreateRoom);
            JoinRoomCommand = new RelayCommand(ExecuteJoinRoom);
            LogoutCommand = new RelayCommand(ExecuteLogout);

            // RoomManager 이벤트 구독
            var roomManager = RoomManager.Instance;
            roomManager.OnRoomListUpdated += OnRoomListUpdated;
            roomManager.OnRoomCreateSuccess += OnRoomCreateSuccess;
            roomManager.OnRoomJoinSuccess += OnRoomJoinSuccess;
            roomManager.OnRoomJoinFailure += OnRoomJoinFailure;
            roomManager.OnStatusMessage += (msg) => StatusMessage = msg;
            roomManager.OnError += (err) => StatusMessage = "ERROR: " + err;

            // Logger 구독
            Logger.OnLog += (msg) => StatusMessage = msg;
            Logger.OnLogError += (err) => StatusMessage = "ERROR: " + err;

            StatusMessage = "로비에 접속했습니다";
        }

        private async void ExecuteRefresh(object? obj)
        {
            StatusMessage = "방 목록 새로고침 중...";
            await RoomManager.Instance.RefreshLobbyAsync();
        }

        private async void ExecuteCreateRoom(object? obj)
        {
            if (string.IsNullOrWhiteSpace(NewRoomName))
            {
                MessageBox.Show("방 이름을 입력해주세요.", "입력 오류");
                return;
            }

            StatusMessage = $"방 생성 중: {NewRoomName}...";
            await RoomManager.Instance.RequestCreateRoomAsync(NewRoomName, SelectedMapId, IsPrivate);
        }

        private async void ExecuteJoinRoom(object? obj)
        {
            if (obj is RoomInfo roomInfo)
            {
                StatusMessage = $"방 참가 중: {roomInfo.RoomName}...";
                await RoomManager.Instance.RequestJoinRoomAsync(roomInfo.RoomId);
            }
            else if (obj is string roomId)
            {
                StatusMessage = $"방 참가 중: {roomId}...";
                await RoomManager.Instance.RequestJoinRoomAsync(roomId);
            }
        }

        private void ExecuteLogout(object? obj)
        {
            ClientServerHandler.Instance.Disconnect();
            _mainViewModel.NavigateToLogin();
        }

        private void OnRoomListUpdated(List<RoomInfo> rooms)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RoomList.Clear();
                foreach (var room in rooms)
                {
                    RoomList.Add(room);
                }
                StatusMessage = $"방 목록 갱신됨 ({rooms.Count}개)";
            });
        }

        private void OnRoomCreateSuccess(string roomId, int slot)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = $"방 생성 성공! RoomID: {roomId}, Slot: {slot}";
                Logger.Log($"[LobbyViewModel] 방 생성 성공 - RoomID: {roomId}");
                // 방 생성 후 자동으로 방에 입장되므로 OnRoomJoinSuccess에서 처리
            });
        }

        private void OnRoomJoinSuccess(RoomInfo roomInfo)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = $"방 참가 성공: {roomInfo.RoomName}";
                Logger.Log($"[LobbyViewModel] 방 참가 성공 - RoomID: {roomInfo.RoomId}");
                _mainViewModel.NavigateToRoom();
            });
        }

        private void OnRoomJoinFailure(string reason)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = $"방 참가 실패: {reason}";
                MessageBox.Show($"방에 참가할 수 없습니다:\n{reason}", "참가 실패");
            });
        }
    }
}
