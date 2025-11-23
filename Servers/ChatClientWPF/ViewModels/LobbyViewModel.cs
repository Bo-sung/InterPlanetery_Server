using ChatClientWPF.Models;
using CommonLib;
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

            _mainViewModel.ChatModel.OnLobbyJoined += OnLobbyJoined;
            _mainViewModel.ChatModel.OnRoomListRefreshed += OnRoomListRefreshed;
            _mainViewModel.ChatModel.OnRoomCreated += OnRoomCreated;
            _mainViewModel.ChatModel.OnRoomJoined += OnRoomJoined;

            // Initial load
            _mainViewModel.ChatModel.JoinLobbyAsync();
        }

        private void ExecuteRefresh(object? obj)
        {
            _mainViewModel.ChatModel.RefreshRoomListAsync();
        }

        private void ExecuteCreateRoom(object? obj)
        {
            if (string.IsNullOrWhiteSpace(NewRoomName)) return;
            _mainViewModel.ChatModel.CreateRoomAsync(NewRoomName, SelectedMapId, IsPrivate);
        }

        private void ExecuteJoinRoom(object? obj)
        {
            if (obj is string roomId)
            {
                _mainViewModel.ChatModel.JoinRoomAsync(roomId);
            }
        }

        private void ExecuteLogout(object? obj)
        {
            _mainViewModel.ChatModel.Disconnect();
            _mainViewModel.NavigateToLogin();
        }

        private void OnLobbyJoined(int count, int page, RoomInfo[] rooms)
        {
            UpdateRoomList(rooms);
        }

        private void OnRoomListRefreshed(RoomInfo[] rooms)
        {
            UpdateRoomList(rooms);
        }

        private void UpdateRoomList(RoomInfo[] rooms)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RoomList.Clear();
                foreach (var room in rooms)
                {
                    RoomList.Add(room);
                }
            });
        }

        private void OnRoomCreated(string roomId)
        {
            // Usually server auto-joins creator, so we wait for OnRoomJoined
        }

        private void OnRoomJoined(RoomInfo roomInfo, int chatChannelId)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _mainViewModel.NavigateToRoom();
            });
        }
    }
}
