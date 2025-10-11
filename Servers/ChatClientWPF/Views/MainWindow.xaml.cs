using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ChatClientWPF.Models;
using ChatClientWPF.Presenters;

namespace ChatClientWPF.Views
{
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// MVP 패턴의 View 구현
	/// </summary>
	public partial class MainWindow : Window, IChatView
	{
		private ChatPresenter? _presenter;
		private ObservableCollection<string> _chatMessages;

		// View -> Presenter 이벤트
		public event System.Action<string, int>? OnConnectRequested;
		public event System.Action? OnDisconnectRequested;
		public event System.Action? OnJoinRoomRequested;
		public event System.Action? OnLeaveRoomRequested;
		public event System.Action<string>? OnSendMessageRequested;

		public MainWindow()
		{
			InitializeComponent();
			_chatMessages = new ObservableCollection<string>();
			ChatMessagesPanel.ItemsSource = _chatMessages;

			// Presenter 생성 및 연결
			var model = new ChatClientModel();
			_presenter = new ChatPresenter(this, model);

			Loaded += MainWindow_Loaded;
			Closing += MainWindow_Closing;
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			// 초기 UI 상태 설정
			UpdateUIState();
		}

		private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
		{
			// 연결 해제
			OnDisconnectRequested?.Invoke();
		}

		// === UI 이벤트 핸들러 ===

		private void ConnectButton_Click(object sender, RoutedEventArgs e)
		{
			if (!int.TryParse(PortTextBox.Text, out int port))
			{
				MessageBox.Show("Invalid port number", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			string host = HostTextBox.Text.Trim();
			if (string.IsNullOrEmpty(host))
			{
				MessageBox.Show("Host cannot be empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			OnConnectRequested?.Invoke(host, port);
		}

		private void DisconnectButton_Click(object sender, RoutedEventArgs e)
		{
			OnDisconnectRequested?.Invoke();
		}

		private void JoinRoomButton_Click(object sender, RoutedEventArgs e)
		{
			OnJoinRoomRequested?.Invoke();
		}

		private void LeaveRoomButton_Click(object sender, RoutedEventArgs e)
		{
			OnLeaveRoomRequested?.Invoke();
		}

		private void SendButton_Click(object sender, RoutedEventArgs e)
		{
			SendMessage();
		}

		private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				SendMessage();
			}
		}

		private void SendMessage()
		{
			string message = MessageTextBox.Text.Trim();
			if (!string.IsNullOrEmpty(message))
			{
				OnSendMessageRequested?.Invoke(message);
				MessageTextBox.Clear();
			}
		}

		// === IChatView 구현 ===

		public void ShowConnectionStatus(bool isConnected, string message)
		{
			Dispatcher.Invoke(() =>
			{
				ConnectionStatusText.Text = message;
				ConnectionStatusText.Foreground = isConnected
					? System.Windows.Media.Brushes.Green
					: System.Windows.Media.Brushes.Red;

				ConnectButton.IsEnabled = !isConnected;
				DisconnectButton.IsEnabled = isConnected;
				JoinRoomButton.IsEnabled = isConnected;

				HostTextBox.IsEnabled = !isConnected;
				PortTextBox.IsEnabled = !isConnected;

				if (!isConnected)
				{
					ShowRoomStatus(false, null, 0);
					SetInputEnabled(false);
				}
			});
		}

		public void ShowRoomStatus(bool isInRoom, string? roomId, int playerCount)
		{
			Dispatcher.Invoke(() =>
			{
				if (isInRoom && !string.IsNullOrEmpty(roomId))
				{
					RoomIdText.Text = roomId;
					PlayerCountText.Text = playerCount.ToString();
					LeaveRoomButton.IsEnabled = true;
					JoinRoomButton.IsEnabled = false;
				}
				else
				{
					RoomIdText.Text = "Not in room";
					PlayerCountText.Text = "0";
					LeaveRoomButton.IsEnabled = false;
					JoinRoomButton.IsEnabled = true;
				}
			});
		}

		public void AddChatMessage(ChatMessageModel message)
		{
			Dispatcher.Invoke(() =>
			{
				string prefix = message.IsOwnMessage ? "[You]" : $"[{message.SenderId}]";
				string formattedMessage = $"{message.FormattedTime} {prefix} {message.Message}";
				_chatMessages.Add(formattedMessage);
				ScrollToBottom();
			});
		}

		public void AddSystemMessage(string message)
		{
			Dispatcher.Invoke(() =>
			{
				_chatMessages.Add($"[SYSTEM] {message}");
				ScrollToBottom();
			});
		}

		public void ClearChatMessages()
		{
			Dispatcher.Invoke(() =>
			{
				_chatMessages.Clear();
			});
		}

		public void SetInputEnabled(bool enabled)
		{
			Dispatcher.Invoke(() =>
			{
				MessageTextBox.IsEnabled = enabled;
				SendButton.IsEnabled = enabled;
			});
		}

		public void SetUserId(string userId)
		{
			Dispatcher.Invoke(() =>
			{
				UserIdText.Text = userId;
			});
		}

		public void ShowError(string message)
		{
			Dispatcher.Invoke(() =>
			{
				MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			});
		}

		// === Helper Methods ===

		private void UpdateUIState()
		{
			// 초기 상태는 모두 비활성화
			ShowConnectionStatus(false, "Disconnected");
			ShowRoomStatus(false, null, 0);
			SetInputEnabled(false);
		}

		private void ScrollToBottom()
		{
			ChatScrollViewer.ScrollToBottom();
		}
	}
}
