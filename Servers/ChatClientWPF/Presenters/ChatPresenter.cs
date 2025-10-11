using System;
using System.Threading.Tasks;
using ChatClientWPF.Models;
using ChatClientWPF.Views;

namespace ChatClientWPF.Presenters
{
	/// <summary>
	/// MVP 패턴의 Presenter
	/// View와 Model 사이의 중재자 역할
	/// </summary>
	public class ChatPresenter
	{
		private readonly IChatView _view;
		private readonly ChatClientModel _model;

		public ChatPresenter(IChatView view, ChatClientModel model)
		{
			_view = view;
			_model = model;

			// View 이벤트 구독
			_view.OnConnectRequested += HandleConnectRequested;
			_view.OnDisconnectRequested += HandleDisconnectRequested;
			_view.OnJoinRoomRequested += HandleJoinRoomRequested;
			_view.OnLeaveRoomRequested += HandleLeaveRoomRequested;
			_view.OnSendMessageRequested += HandleSendMessageRequested;

			// Model 이벤트 구독
			_model.OnConnectionChanged += HandleConnectionChanged;
			_model.OnChatMessageReceived += HandleChatMessageReceived;
			_model.OnJoinRoomSuccess += HandleJoinRoomSuccess;
			_model.OnJoinRoomFailed += HandleJoinRoomFailed;
			_model.OnLeaveRoomSuccess += HandleLeaveRoomSuccess;
			_model.OnUserJoined += HandleUserJoined;
			_model.OnUserLeft += HandleUserLeft;
			_model.OnRoomClosed += HandleRoomClosed;
			_model.OnError += HandleError;
			_model.OnDisconnected += HandleDisconnected;
		}

		// === View -> Model 이벤트 핸들러 ===

		private async void HandleConnectRequested(string host, int port)
		{
			_view.AddSystemMessage($"Connecting to {host}:{port}...");
			bool success = await _model.ConnectAsync(host, port);

			if (success)
			{
				_view.SetUserId(_model.UserId);
				_view.AddSystemMessage("Connected successfully!");
			}
		}

		private void HandleDisconnectRequested()
		{
			_model.Disconnect();
			_view.AddSystemMessage("Disconnected from server.");
			_view.ClearChatMessages();
		}

		private async void HandleJoinRoomRequested()
		{
			if (!_model.IsConnected)
			{
				_view.ShowError("Not connected to server");
				return;
			}

			if (_model.IsInRoom)
			{
				_view.ShowError("Already in a room");
				return;
			}

			_view.AddSystemMessage("Requesting to join room...");
			await _model.JoinRoomAsync();
		}

		private async void HandleLeaveRoomRequested()
		{
			if (!_model.IsInRoom)
			{
				_view.ShowError("Not in a room");
				return;
			}

			_view.AddSystemMessage("Leaving room...");
			await _model.LeaveRoomAsync();
		}

		private async void HandleSendMessageRequested(string message)
		{
			if (!_model.IsInRoom)
			{
				_view.ShowError("You must join a room first");
				return;
			}

			await _model.SendChatMessageAsync(message);
		}

		// === Model -> View 이벤트 핸들러 ===

		private void HandleConnectionChanged(bool isConnected, string message)
		{
			_view.ShowConnectionStatus(isConnected, message);

			if (!isConnected)
			{
				_view.ShowRoomStatus(false, null, 0);
				_view.SetInputEnabled(false);
			}
		}

		private void HandleChatMessageReceived(ChatMessageModel message)
		{
			_view.AddChatMessage(message);
		}

		private void HandleJoinRoomSuccess(string roomId, int playerCount)
		{
			_view.ShowRoomStatus(true, roomId, playerCount);
			_view.SetInputEnabled(true);
			_view.AddSystemMessage($"✓ Joined room '{roomId}' (Players: {playerCount})");
		}

		private void HandleJoinRoomFailed(string reason)
		{
			_view.ShowRoomStatus(false, null, 0);
			_view.SetInputEnabled(false);
			_view.AddSystemMessage($"✗ Failed to join room: {reason}");
			_view.ShowError($"Failed to join room: {reason}");
		}

		private void HandleLeaveRoomSuccess()
		{
			_view.ShowRoomStatus(false, null, 0);
			_view.SetInputEnabled(false);
			_view.AddSystemMessage("✓ Left the room");
			_view.ClearChatMessages();
		}

		private void HandleUserJoined(string userId, int playerCount)
		{
			_view.ShowRoomStatus(true, _model.CurrentRoomId, playerCount);
			_view.AddSystemMessage($"→ User '{userId}' joined (Players: {playerCount})");
		}

		private void HandleUserLeft(string userId, int playerCount)
		{
			_view.ShowRoomStatus(true, _model.CurrentRoomId, playerCount);
			_view.AddSystemMessage($"← User '{userId}' left (Players: {playerCount})");
		}

		private void HandleRoomClosed(string roomId, string reason)
		{
			_view.ShowRoomStatus(false, null, 0);
			_view.SetInputEnabled(false);
			_view.AddSystemMessage($"✗ Room '{roomId}' closed: {reason}");
		}

		private void HandleError(string message)
		{
			_view.AddSystemMessage($"✗ Error: {message}");
		}

		private void HandleDisconnected()
		{
			_view.ShowConnectionStatus(false, "Disconnected");
			_view.ShowRoomStatus(false, null, 0);
			_view.SetInputEnabled(false);
			_view.AddSystemMessage("Connection closed.");
		}

		// === 추가 기능 (필요시 확장) ===

		/// <summary>
		/// 주기적 하트비트 (선택사항)
		/// </summary>
		public async Task StartHeartbeatAsync(int intervalMs = 30000)
		{
			while (_model.IsConnected)
			{
				await Task.Delay(intervalMs);
				if (_model.IsConnected)
				{
					await _model.SendHeartbeatAsync();
				}
			}
		}
	}
}
