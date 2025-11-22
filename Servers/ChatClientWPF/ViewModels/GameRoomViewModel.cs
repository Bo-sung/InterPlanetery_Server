using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ChatClientWPF.Models;
using CommonLib;

namespace ChatClientWPF.ViewModels
{
    /// <summary>
    /// 게임 방 ViewModel
    /// </summary>
    public class GameRoomViewModel : ViewModelBase
    {
        private readonly ChatClientModel _chatModel;

        // 서버 연결 설정 properties
        private string _serverAddress = "127.0.0.1";
        private int _serverPort = 5000;
        private string _playerName = "플레이어_12345";
        private bool _isConnected = false;
        private string _connectionStatus = "연결 안됨";
        private ObservableCollection<AppConfig.GameServerConfig> _gameServers;
        private AppConfig.GameServerConfig _selectedGameServer;

        // 로비 화면 properties
        private ObservableCollection<LobbyRoomItemModel> _roomList;
        private string _newRoomName = "나의 방";
        private int _selectedMapId = 1;
        private bool _isPrivateRoom = false;
        private ObservableCollection<MapModel> _mapList;
        private int _selectedRoomIndex = -1;

        // 대기방 화면 properties
        private GameRoomModel _currentRoom;
        private ObservableCollection<PlayerSlotModel> _playerSlots;
        private GameSettingsModel _gameSettings;
        private ObservableCollection<GameRoomChatMessageModel> _chatMessages;
        private string _chatInput = string.Empty;
        private bool _isPlayerReady = false;
        private bool _isInWaitingRoom = false;

        // Commands
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand CreateRoomCommand { get; }
        public ICommand RefreshRoomListCommand { get; }
        public ICommand JoinRoomCommand { get; }
        public ICommand LeaveRoomCommand { get; }
        public ICommand ReadyCommand { get; }
        public ICommand SendChatCommand { get; }

        public GameRoomViewModel()
        {
            _chatModel = new ChatClientModel();

            // 컬렉션 초기화
            _roomList = new ObservableCollection<LobbyRoomItemModel>();
            _mapList = new ObservableCollection<MapModel>();
            _playerSlots = new ObservableCollection<PlayerSlotModel>();
            _chatMessages = new ObservableCollection<GameRoomChatMessageModel>();
            _currentRoom = new GameRoomModel();
            _gameSettings = new GameSettingsModel();
            _gameServers = new ObservableCollection<AppConfig.GameServerConfig>();

            // Commands 초기화
            ConnectCommand = new RelayCommand(Connect, CanConnect);
            DisconnectCommand = new RelayCommand(Disconnect, CanDisconnect);
            CreateRoomCommand = new RelayCommand(CreateRoom, CanCreateRoom);
            RefreshRoomListCommand = new RelayCommand(RefreshRoomList);
            JoinRoomCommand = new RelayCommand(JoinRoom, CanJoinRoom);
            LeaveRoomCommand = new RelayCommand(LeaveRoom, CanLeaveRoom);
            ReadyCommand = new RelayCommand(ReadyGame, CanReady);
            SendChatCommand = new RelayCommand(SendChat, CanSendChat);

            // ChatClientModel 이벤트 구독
            SubscribeToChatModel();

            // 맵 목록 및 게임 서버 목록 로드
            LoadMapData();
            LoadGameServers();
        }

        #region Properties

        // 서버 연결 설정 Properties
        public string ServerAddress
        {
            get => _serverAddress;
            set => SetProperty(ref _serverAddress, value);
        }

        public int ServerPort
        {
            get => _serverPort;
            set => SetProperty(ref _serverPort, value);
        }

        public string PlayerName
        {
            get => _playerName;
            set => SetProperty(ref _playerName, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        public ObservableCollection<LobbyRoomItemModel> RoomList
        {
            get => _roomList;
            set => SetProperty(ref _roomList, value);
        }

        public ObservableCollection<MapModel> MapList
        {
            get => _mapList;
            set => SetProperty(ref _mapList, value);
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

        public bool IsPrivateRoom
        {
            get => _isPrivateRoom;
            set => SetProperty(ref _isPrivateRoom, value);
        }


        public int SelectedRoomIndex
        {
            get => _selectedRoomIndex;
            set => SetProperty(ref _selectedRoomIndex, value);
        }

        public bool IsInWaitingRoom
        {
            get => _isInWaitingRoom;
            set => SetProperty(ref _isInWaitingRoom, value);
        }

        public GameRoomModel CurrentRoom
        {
            get => _currentRoom;
            set => SetProperty(ref _currentRoom, value);
        }

        public ObservableCollection<PlayerSlotModel> PlayerSlots
        {
            get => _playerSlots;
            set => SetProperty(ref _playerSlots, value);
        }

        public GameSettingsModel GameSettings
        {
            get => _gameSettings;
            set => SetProperty(ref _gameSettings, value);
        }

        public ObservableCollection<GameRoomChatMessageModel> ChatMessages
        {
            get => _chatMessages;
            set => SetProperty(ref _chatMessages, value);
        }

        public string ChatInput
        {
            get => _chatInput;
            set => SetProperty(ref _chatInput, value);
        }

        public bool IsPlayerReady
        {
            get => _isPlayerReady;
            set
            {
                SetProperty(ref _isPlayerReady, value);
                RaisePropertyChanged(nameof(ReadyButtonText));
            }
        }

        public string ReadyButtonText => IsPlayerReady ? "준비 취소" : "준비 완료";

        public ObservableCollection<AppConfig.GameServerConfig> GameServers
        {
            get => _gameServers;
            set => SetProperty(ref _gameServers, value);
        }

        public AppConfig.GameServerConfig SelectedGameServer
        {
            get => _selectedGameServer;
            set
            {
                if (SetProperty(ref _selectedGameServer, value))
                {
                    // 선택된 서버의 주소와 포트를 자동으로 설정
                    if (value != null)
                    {
                        ServerAddress = value.Host;
                        ServerPort = value.Port;
                    }
                }
            }
        }

        #endregion

        #region Commands Implementation

        private bool CanConnect(object? obj) => !IsConnected && !string.IsNullOrWhiteSpace(ServerAddress) && ServerPort > 0;

        private async void Connect(object? obj)
        {
            try
            {
                ConnectionStatus = "연결 중...";
                bool connected = await _chatModel.ConnectAsync(ServerAddress, ServerPort);
                if (connected)
                {
                    IsConnected = true;
                    ConnectionStatus = $"연결됨 ({ServerAddress}:{ServerPort})";
                    AddSystemMessage("서버에 연결되었습니다.");
                    // 연결 후 방 목록 요청
                    await _chatModel.GetRoomListAsync();
                }
                else
                {
                    ConnectionStatus = "연결 실패";
                    AddSystemMessage("서버 연결에 실패했습니다.");
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"오류: {ex.Message}";
                AddSystemMessage($"연결 오류: {ex.Message}");
            }
        }

        private bool CanDisconnect(object? obj) => IsConnected;

        private void Disconnect(object? obj)
        {
            _chatModel.Disconnect();
            IsConnected = false;
            ConnectionStatus = "연결 안됨";
            RoomList.Clear();
            AddSystemMessage("서버와의 연결이 해제되었습니다.");
        }

        private bool CanCreateRoom(object? obj) => !IsInWaitingRoom && !string.IsNullOrWhiteSpace(NewRoomName) && IsConnected;

        private void CreateRoom(object? obj)
        {
            // TODO: 서버와 통신하여 방 생성
            AddSystemMessage("방을 생성했습니다.");
            EnterWaitingRoom(new GameRoomModel
            {
                RoomId = "ROOM_" + Guid.NewGuid().ToString().Substring(0, 8),
                RoomName = NewRoomName,
                MapId = SelectedMapId,
                MapName = GetMapNameById(SelectedMapId),
                CurrentPlayers = 1,
                MaxPlayers = 2,
                IsPrivate = IsPrivateRoom,
                Status = "waiting"
            });
        }

        private async void RefreshRoomList(object? obj)
        {
            // 서버에서 방 목록 새로고침
            await _chatModel.GetRoomListAsync();
            AddSystemMessage("방 목록을 새로고침했습니다.");
        }

        private bool CanJoinRoom(object? obj) => !IsInWaitingRoom && SelectedRoomIndex >= 0;

        private void JoinRoom(object? obj)
        {
            if (SelectedRoomIndex >= 0 && SelectedRoomIndex < RoomList.Count)
            {
                var room = RoomList[SelectedRoomIndex];
                // TODO: 서버와 통신하여 방 입장
                EnterWaitingRoom(new GameRoomModel
                {
                    RoomId = room.RoomId,
                    RoomName = room.RoomName,
                    MapId = 1, // TODO: 실제 mapId 사용
                    MapName = room.MapName,
                    CurrentPlayers = room.CurrentPlayers,
                    MaxPlayers = room.MaxPlayers,
                    Status = room.Status
                });
            }
        }

        private bool CanLeaveRoom(object? obj) => IsInWaitingRoom;

        private void LeaveRoom(object? obj)
        {
            // TODO: 서버와 통신하여 방 퇴장
            ExitWaitingRoom();
            AddSystemMessage("방을 나갔습니다.");
        }

        private bool CanReady(object? obj) => IsInWaitingRoom;

        private void ReadyGame(object? obj)
        {
            IsPlayerReady = !IsPlayerReady;
            // TODO: 서버와 통신하여 준비 상태 전송
            AddSystemMessage($"{PlayerName}님이 준비 상태를 변경했습니다. (준비: {IsPlayerReady})");
        }

        private bool CanSendChat(object? obj) => !string.IsNullOrWhiteSpace(ChatInput);

        private void SendChat(object? obj)
        {
            if (string.IsNullOrWhiteSpace(ChatInput))
                return;

            // TODO: 서버로 메시지 전송
            var message = new GameRoomChatMessageModel
            {
                SenderId = PlayerName,
                Message = ChatInput,
                Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                IsSystemMessage = false
            };
            ChatMessages.Add(message);
            ChatInput = string.Empty;
        }

        #endregion

        #region Helper Methods

        private void EnterWaitingRoom(GameRoomModel room)
        {
            CurrentRoom = room;
            IsInWaitingRoom = true;
            IsPlayerReady = false;
            ChatMessages.Clear();
            InitializePlayerSlots();
            UpdateGameSettings();
            AddSystemMessage($"{PlayerName}님이 방을 생성했습니다.");
            AddSystemMessage("플레이어 입장을 기다리고 있습니다...");
        }

        private void ExitWaitingRoom()
        {
            IsInWaitingRoom = false;
            IsPlayerReady = false;
            CurrentRoom = new GameRoomModel();
            PlayerSlots.Clear();
            ChatMessages.Clear();
            SelectedRoomIndex = -1;
        }

        private void InitializePlayerSlots()
        {
            PlayerSlots.Clear();
            for (int i = 0; i < CurrentRoom.MaxPlayers; i++)
            {
                var slot = new PlayerSlotModel
                {
                    SlotNumber = i + 1,
                    IsHost = i == 0,
                };

                if (i == 0)
                {
                    // 첫 번째 슬롯은 현재 플레이어
                    slot.PlayerId = 1;
                    slot.PlayerName = PlayerName;
                    slot.IsReady = false;
                }

                PlayerSlots.Add(slot);
            }
        }

        private void UpdateGameSettings()
        {
            GameSettings = new GameSettingsModel
            {
                MapName = CurrentRoom.MapName,
                MaxPlayerCount = CurrentRoom.MaxPlayers,
                StartingResources = "표준",
                RoomType = CurrentRoom.IsPrivate ? "비공개" : "공개"
            };
        }

        private void AddSystemMessage(string message)
        {
            var chatMsg = new GameRoomChatMessageModel
            {
                SenderId = "SYSTEM",
                Message = message,
                Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                IsSystemMessage = true
            };
            ChatMessages.Add(chatMsg);
        }

        private string GetMapNameById(int mapId)
        {
            return mapId switch
            {
                1 => "3레인 전투장",
                2 => "쌍성계",
                3 => "거울 세계",
                4 => "교차로",
                5 => "단순 경로",
                _ => "알 수 없는 맵"
            };
        }

        private void LoadMapData()
        {
            // 맵 목록만 로드 (하드코딩된 데이터)
            MapList.Add(new MapModel { MapId = 1, MapName = "3레인 전투장" });
            MapList.Add(new MapModel { MapId = 2, MapName = "쌍성계" });
            MapList.Add(new MapModel { MapId = 3, MapName = "거울 세계" });
            MapList.Add(new MapModel { MapId = 4, MapName = "교차로" });
            MapList.Add(new MapModel { MapId = 5, MapName = "단순 경로" });

            // 방 목록은 서버에서 받아온다
        }

        private void LoadGameServers()
        {
            // AppConfig에서 게임 서버 목록 로드
            try
            {
                GameServers.Clear();
                foreach (var server in AppConfig.Instance.GameServers)
                {
                    GameServers.Add(server);
                }

                // 첫 번째 서버를 기본값으로 선택
                if (GameServers.Count > 0)
                {
                    SelectedGameServer = GameServers[0];
                }
            }
            catch (Exception ex)
            {
                AddSystemMessage($"게임 서버 목록 로드 실패: {ex.Message}");
            }
        }

        private void SubscribeToChatModel()
        {
            // 방 목록 수신 이벤트 구독
            _chatModel.OnRoomListReceived += (roomList) =>
            {
                // UI 스레드에서 실행 필요
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    RoomList.Clear();
                    foreach (var room in roomList)
                    {
                        RoomList.Add(new LobbyRoomItemModel
                        {
                            RoomId = room.RoomId,
                            RoomName = room.RoomName,
                            CurrentPlayers = room.PlayerCount,
                            MaxPlayers = room.MaxPlayers,
                            MapName = GetMapNameById(room.MapID),
                            Status = room.RoomState.ToString()
                        });
                    }
                });
            };

            // 에러 이벤트 구독
            _chatModel.OnError += (error) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AddSystemMessage($"[에러] {error}");
                });
            };
        }

        /// <summary>
        /// 로비 진입 시 방 목록 요청
        /// </summary>
        public async void RefreshRoomListOnLoaded()
        {
            await _chatModel.GetRoomListAsync();
        }

        #endregion
    }

    /// <summary>
    /// MVVM용 기본 ViewModel 클래스
    /// </summary>
    public abstract class ViewModelBase : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, string? propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }
    }

    /// <summary>
    /// RelayCommand 구현
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);
    }
}
