using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ChatClientWPF.Models;
using ChatClientWPF.Presenters;
using ChatClientWPF.Views;

namespace ChatClientWPF.Pages
{
	/// <summary>
	/// ChatPage.xaml에 대한 상호 작용 논리
	/// MVP(Model-View-Presenter) 패턴의 View 구현체 (Page 버전)
	///
	/// 역할:
	/// - 채팅 기능을 독립적인 Page로 분리
	/// - 테스트 툴의 하나의 탭/페이지로 사용 가능
	/// - IChatView 인터페이스를 통해 Presenter와 통신
	/// - UI 스레드 안전성 보장 (Dispatcher 사용)
	///
	/// 특징:
	/// - Model에 대해 무지 (Presenter를 통해서만 통신)
	/// - 비즈니스 로직 없음 (순수 UI 로직만 포함)
	/// - Page 라이프사이클에 맞춰 리소스 관리
	/// </summary>
	public partial class ChatPage : Page, IChatView
	{
		// ========================================
		// 필드 및 속성
		// ========================================

		/// <summary>
		/// MVP의 Presenter - View와 Model 사이의 중재자
		/// </summary>
		private ChatPresenter? _presenter;

		/// <summary>
		/// 채팅 메시지 목록 (ObservableCollection으로 자동 UI 업데이트)
		/// </summary>
		private ObservableCollection<string> _chatMessages;

		// ========================================
		// View -> Presenter 이벤트 (사용자 입력)
		// ========================================

		/// <summary>
		/// 서버 연결 요청 이벤트
		/// </summary>
		public event System.Action<string, int>? OnConnectRequested;

		/// <summary>
		/// 서버 연결 해제 요청 이벤트
		/// </summary>
		public event System.Action? OnDisconnectRequested;

		/// <summary>
		/// 룸 입장 요청 이벤트
		/// </summary>
		public event System.Action? OnJoinRoomRequested;

		/// <summary>
		/// 룸 퇴장 요청 이벤트
		/// </summary>
		public event System.Action? OnLeaveRoomRequested;

		/// <summary>
		/// 채팅 메시지 전송 요청 이벤트
		/// </summary>
		public event System.Action<string>? OnSendMessageRequested;

		// ========================================
		// 생성자 및 초기화
		// ========================================

		/// <summary>
		/// ChatPage 생성자
		/// MVP 패턴에 따라 View, Presenter, Model을 생성하고 연결
		/// </summary>
		public ChatPage()
		{
			InitializeComponent();

			// ObservableCollection 초기화 및 ItemsControl에 바인딩
			_chatMessages = new ObservableCollection<string>();
			ChatMessagesPanel.ItemsSource = _chatMessages;

			// MVP 패턴 구성: Model -> Presenter -> View
			var model = new ChatClientModel();
			_presenter = new ChatPresenter(this, model);

			// Page 라이프사이클 이벤트 등록
			Loaded += ChatPage_Loaded;
			Unloaded += ChatPage_Unloaded;
		}

		/// <summary>
		/// Page 로드 완료 시 호출
		/// 초기 UI 상태를 설정 (모든 버튼 비활성화)
		/// </summary>
		private void ChatPage_Loaded(object sender, RoutedEventArgs e)
		{
			UpdateUIState();
		}

		/// <summary>
		/// Page 언로드 시 호출
		/// 리소스 정리를 위해 Disconnect 이벤트 발생
		/// </summary>
		private void ChatPage_Unloaded(object sender, RoutedEventArgs e)
		{
			OnDisconnectRequested?.Invoke();
		}

		// ========================================
		// UI 이벤트 핸들러 (XAML 버튼 클릭 등)
		// ========================================

		/// <summary>
		/// Connect 버튼 클릭 시 호출
		/// 입력 검증 후 OnConnectRequested 이벤트 발생
		/// </summary>
		private void ConnectButton_Click(object sender, RoutedEventArgs e)
		{
			// 포트 번호 유효성 검증
			if (!int.TryParse(PortTextBox.Text, out int port))
			{
				MessageBox.Show("Invalid port number", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// 호스트 주소 유효성 검증
			string host = HostTextBox.Text.Trim();
			if (string.IsNullOrEmpty(host))
			{
				MessageBox.Show("Host cannot be empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// Presenter에게 연결 요청 이벤트 발생
			OnConnectRequested?.Invoke(host, port);
		}

		/// <summary>
		/// Disconnect 버튼 클릭 시 호출
		/// OnDisconnectRequested 이벤트 발생
		/// </summary>
		private void DisconnectButton_Click(object sender, RoutedEventArgs e)
		{
			OnDisconnectRequested?.Invoke();
		}

		/// <summary>
		/// Join Room 버튼 클릭 시 호출
		/// OnJoinRoomRequested 이벤트 발생
		/// </summary>
		private void JoinRoomButton_Click(object sender, RoutedEventArgs e)
		{
			OnJoinRoomRequested?.Invoke();
		}

		/// <summary>
		/// Leave Room 버튼 클릭 시 호출
		/// OnLeaveRoomRequested 이벤트 발생
		/// </summary>
		private void LeaveRoomButton_Click(object sender, RoutedEventArgs e)
		{
			OnLeaveRoomRequested?.Invoke();
		}

		/// <summary>
		/// Send 버튼 클릭 시 호출
		/// SendMessage() 메서드 호출
		/// </summary>
		private void SendButton_Click(object sender, RoutedEventArgs e)
		{
			SendMessage();
		}

		/// <summary>
		/// 메시지 입력창에서 키 입력 시 호출
		/// Enter 키 입력 시 메시지 전송
		/// </summary>
		private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				SendMessage();
			}
		}

		/// <summary>
		/// 채팅 메시지 전송 처리
		/// 입력창의 텍스트를 가져와 OnSendMessageRequested 이벤트 발생 후 입력창 초기화
		/// </summary>
		private void SendMessage()
		{
			string message = MessageTextBox.Text.Trim();
			if (!string.IsNullOrEmpty(message))
			{
				OnSendMessageRequested?.Invoke(message);
				MessageTextBox.Clear();
			}
		}

		// ========================================
		// IChatView 인터페이스 구현 (Presenter -> View 호출)
		// ========================================

		/// <summary>
		/// 서버 연결 상태를 UI에 표시 (IChatView 인터페이스 구현)
		/// 스레드 안전: 백그라운드 스레드에서 호출 가능 (Dispatcher 처리)
		/// </summary>
		/// <param name="isConnected">연결 여부</param>
		/// <param name="message">표시할 상태 메시지</param>
		public void ShowConnectionStatus(bool isConnected, string message)
		{
			// UI 스레드가 아닌 경우 Dispatcher를 통해 UI 스레드로 전환
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.Invoke(() => ShowConnectionStatus(isConnected, message));
				return;
			}

			// UI 스레드에서 실제 UI 업데이트 실행
			UpdateConnectionStatusUI(isConnected, message);
		}

		/// <summary>
		/// 연결 상태 UI 업데이트 실제 처리 (UI 스레드에서만 호출됨)
		/// - 상태 텍스트 및 색상 변경
		/// - 버튼 활성화/비활성화 처리
		/// - 입력 필드 잠금/해제
		/// </summary>
		private void UpdateConnectionStatusUI(bool isConnected, string message)
		{
			// 연결 상태 텍스트 및 색상 설정
			ConnectionStatusText.Text = message;
			ConnectionStatusText.Foreground = isConnected
				? System.Windows.Media.Brushes.Green   // 연결됨: 녹색
				: System.Windows.Media.Brushes.Red;    // 연결 안됨: 빨간색

			// 버튼 활성화 상태 설정
			ConnectButton.IsEnabled = !isConnected;     // 연결 안됨일 때만 Connect 버튼 활성화
			DisconnectButton.IsEnabled = isConnected;   // 연결됨일 때만 Disconnect 버튼 활성화
			JoinRoomButton.IsEnabled = isConnected;     // 연결됨일 때만 Join Room 버튼 활성화

			// 호스트/포트 입력 필드는 연결 안됨일 때만 수정 가능
			HostTextBox.IsEnabled = !isConnected;
			PortTextBox.IsEnabled = !isConnected;

			// 연결 해제 시 룸 상태 및 입력 초기화
			if (!isConnected)
			{
				ShowRoomStatus(false, null, 0);
				SetInputEnabled(false);
			}
		}

		/// <summary>
		/// 룸 상태를 UI에 표시 (IChatView 인터페이스 구현)
		/// 스레드 안전: 백그라운드 스레드에서 호출 가능
		/// </summary>
		/// <param name="isInRoom">룸 입장 여부</param>
		/// <param name="roomId">룸 ID (null 가능)</param>
		/// <param name="playerCount">현재 플레이어 수</param>
		public void ShowRoomStatus(bool isInRoom, string? roomId, int playerCount)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.Invoke(() => ShowRoomStatus(isInRoom, roomId, playerCount));
				return;
			}

			UpdateRoomStatusUI(isInRoom, roomId, playerCount);
		}

		/// <summary>
		/// 룸 상태 UI 업데이트 실제 처리 (UI 스레드에서만 호출됨)
		/// - 룸 ID 및 플레이어 수 표시
		/// - Join/Leave 버튼 활성화 상태 토글
		/// </summary>
		private void UpdateRoomStatusUI(bool isInRoom, string? roomId, int playerCount)
		{
			if (isInRoom && !string.IsNullOrEmpty(roomId))
			{
				// 룸 입장 상태: 룸 정보 표시 및 Leave 버튼 활성화
				RoomIdText.Text = roomId;
				PlayerCountText.Text = playerCount.ToString();
				LeaveRoomButton.IsEnabled = true;
				JoinRoomButton.IsEnabled = false;
			}
			else
			{
				// 룸 미입장 상태: 기본 텍스트 표시 및 Join 버튼 활성화
				RoomIdText.Text = "Not in room";
				PlayerCountText.Text = "0";
				LeaveRoomButton.IsEnabled = false;
				JoinRoomButton.IsEnabled = true;
			}
		}

		/// <summary>
		/// 채팅 메시지를 UI에 추가 (IChatView 인터페이스 구현)
		/// 스레드 안전: 네트워크 수신 스레드에서 호출됨
		/// </summary>
		/// <param name="message">채팅 메시지 모델</param>
		public void AddChatMessage(ChatMessageModel message)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.Invoke(() => AddChatMessage(message));
				return;
			}

			AddChatMessageToUI(message);
		}

		/// <summary>
		/// 채팅 메시지를 ObservableCollection에 추가 (UI 스레드에서만 호출됨)
		/// - 자신의 메시지는 "[You]", 타인의 메시지는 "[SenderId]" 표시
		/// - 타임스탬프 포맷: HH:mm:ss
		/// - 자동 스크롤 처리
		/// </summary>
		private void AddChatMessageToUI(ChatMessageModel message)
		{
			string prefix = message.IsOwnMessage ? "[You]" : $"[{message.SenderId}]";
			string formattedMessage = $"{message.FormattedTime} {prefix} {message.Message}";
			_chatMessages.Add(formattedMessage);
			ScrollToBottom();
		}

		/// <summary>
		/// 시스템 메시지를 UI에 추가 (IChatView 인터페이스 구현)
		/// 스레드 안전: 백그라운드 스레드에서 호출 가능
		/// </summary>
		/// <param name="message">시스템 메시지 내용</param>
		public void AddSystemMessage(string message)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.Invoke(() => AddSystemMessage(message));
				return;
			}

			AddSystemMessageToUI(message);
		}

		/// <summary>
		/// 시스템 메시지를 ObservableCollection에 추가 (UI 스레드에서만 호출됨)
		/// - "[SYSTEM]" 태그로 표시
		/// - 연결/연결 해제, 입장/퇴장 알림 등에 사용
		/// </summary>
		private void AddSystemMessageToUI(string message)
		{
			_chatMessages.Add($"[SYSTEM] {message}");
			ScrollToBottom();
		}

		/// <summary>
		/// 모든 채팅 메시지를 삭제 (IChatView 인터페이스 구현)
		/// 스레드 안전: 백그라운드 스레드에서 호출 가능
		/// 주로 룸 퇴장 시 호출됨
		/// </summary>
		public void ClearChatMessages()
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.Invoke(() => ClearChatMessages());
				return;
			}

			_chatMessages.Clear();
		}

		/// <summary>
		/// 메시지 입력 UI 활성화/비활성화 (IChatView 인터페이스 구현)
		/// 스레드 안전: 백그라운드 스레드에서 호출 가능
		/// </summary>
		/// <param name="enabled">활성화 여부 (true: 입력 가능, false: 입력 불가)</param>
		public void SetInputEnabled(bool enabled)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.Invoke(() => SetInputEnabled(enabled));
				return;
			}

			UpdateInputEnabledUI(enabled);
		}

		/// <summary>
		/// 메시지 입력 UI 업데이트 실제 처리 (UI 스레드에서만 호출됨)
		/// - 룸 입장 시: 활성화 (입력 가능)
		/// - 룸 미입장 시: 비활성화 (입력 불가)
		/// </summary>
		private void UpdateInputEnabledUI(bool enabled)
		{
			MessageTextBox.IsEnabled = enabled;
			SendButton.IsEnabled = enabled;
		}

		/// <summary>
		/// 사용자 ID를 UI에 표시 (IChatView 인터페이스 구현)
		/// 스레드 안전: 백그라운드 스레드에서 호출 가능
		/// 서버 연결 성공 시 Model이 생성한 고유 ID 표시
		/// </summary>
		/// <param name="userId">사용자 ID (예: "user_a1b2c3d4")</param>
		public void SetUserId(string userId)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.Invoke(() => SetUserId(userId));
				return;
			}

			UserIdText.Text = userId;
		}

		/// <summary>
		/// 에러 메시지를 MessageBox로 표시 (IChatView 인터페이스 구현)
		/// 스레드 안전: 백그라운드 스레드에서 호출 가능
		/// </summary>
		/// <param name="message">에러 메시지 내용</param>
		public void ShowError(string message)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.Invoke(() => ShowError(message));
				return;
			}

			MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		// ========================================
		// 헬퍼 메서드
		// ========================================

		/// <summary>
		/// 초기 UI 상태 설정
		/// Page 로드 시 모든 버튼을 비활성화 상태로 초기화
		/// - 연결 상태: Disconnected
		/// - 룸 상태: Not in room
		/// - 메시지 입력: 비활성화
		/// </summary>
		private void UpdateUIState()
		{
			ShowConnectionStatus(false, "Disconnected");
			ShowRoomStatus(false, null, 0);
			SetInputEnabled(false);
		}

		/// <summary>
		/// 채팅 메시지 ScrollViewer를 맨 아래로 스크롤
		/// 새 메시지 추가 시 자동으로 최신 메시지를 표시하기 위해 사용
		/// </summary>
		private void ScrollToBottom()
		{
			ChatScrollViewer.ScrollToBottom();
		}
	}
}
