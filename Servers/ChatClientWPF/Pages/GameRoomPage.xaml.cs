using System.Windows;
using System.Windows.Controls;
using ChatClientWPF.ViewModels;

namespace ChatClientWPF.Pages
{
    /// <summary>
    /// GameRoomPage.xaml의 상호 작용 논리
    /// </summary>
    public partial class GameRoomPage : Page
    {
        private GameRoomViewModel? _viewModel;

        public GameRoomPage()
        {
            InitializeComponent();

            // ViewModel 설정
            _viewModel = new GameRoomViewModel();
            DataContext = _viewModel;

            // ViewModel 이벤트 구독
            if (_viewModel != null)
            {
                // 대기방 입장/퇴장 시 화면 전환
                _viewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(GameRoomViewModel.IsInWaitingRoom))
                    {
                        UpdateUIVisibility();
                    }
                };
            }
        }

        /// <summary>
        /// 로비/대기방 UI 가시성 업데이트
        /// </summary>
        private void UpdateUIVisibility()
        {
            if (_viewModel?.IsInWaitingRoom == true)
            {
                LobbyGrid.Visibility = Visibility.Collapsed;
                WaitingRoomGrid.Visibility = Visibility.Visible;
                // 채팅 메시지 스크롤을 최하단으로 이동
                ScrollToBottom();
            }
            else
            {
                LobbyGrid.Visibility = Visibility.Visible;
                WaitingRoomGrid.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 채팅 메시지를 최하단으로 스크롤
        /// </summary>
        private void ScrollToBottom()
        {
            // 채팅 ItemsControl을 찾아 스크롤 위치 조정
            // WPF의 제한으로 인해 최하단 스크롤이 자동으로 이루어지지 않으므로
            // 향후 필요시 ScrollViewer를 직접 제어하는 방식으로 개선 가능
        }

        /// <summary>
        /// 방 카드 클릭 이벤트
        /// </summary>
        private void RoomCard_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Models.LobbyRoomItemModel room)
            {
                // 선택된 방의 인덱스를 찾아 SelectedRoomIndex 설정
                var index = _viewModel?.RoomList.IndexOf(room) ?? -1;
                if (_viewModel != null && index >= 0)
                {
                    _viewModel.SelectedRoomIndex = index;
                }
            }
        }

        /// <summary>
        /// 방 입장 버튼 클릭
        /// </summary>
        private void JoinRoom_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string roomId)
            {
                // 방 ID로 방 찾기
                var room = _viewModel?.RoomList.FirstOrDefault(r => r.RoomId == roomId);
                if (room != null && _viewModel != null)
                {
                    _viewModel.SelectedRoomIndex = _viewModel.RoomList.IndexOf(room);
                    _viewModel.JoinRoomCommand?.Execute(null);
                }
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // 초기화 시 로비 화면 표시
            UpdateUIVisibility();

            // 로비 진입 시 서버에서 방 목록 요청
            if (_viewModel != null)
            {
                _viewModel.RefreshRoomListOnLoaded();
            }
        }
    }
}
