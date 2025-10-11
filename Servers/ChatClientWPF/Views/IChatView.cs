using ChatClientWPF.Models;
using System.Collections.Generic;

namespace ChatClientWPF.Views
{
	/// <summary>
	/// MVP 패턴의 View 인터페이스
	/// Presenter가 View를 제어하기 위한 계약
	/// </summary>
	public interface IChatView
	{
		// View -> Presenter 이벤트
		event System.Action<string, int>? OnConnectRequested;
		event System.Action? OnDisconnectRequested;
		event System.Action? OnJoinRoomRequested;
		event System.Action? OnLeaveRoomRequested;
		event System.Action<string>? OnSendMessageRequested;

		// Presenter -> View 메서드
		void ShowConnectionStatus(bool isConnected, string message);
		void ShowRoomStatus(bool isInRoom, string? roomId, int playerCount);
		void AddChatMessage(ChatMessageModel message);
		void AddSystemMessage(string message);
		void ClearChatMessages();
		void SetInputEnabled(bool enabled);
		void SetUserId(string userId);
		void ShowError(string message);
	}
}
