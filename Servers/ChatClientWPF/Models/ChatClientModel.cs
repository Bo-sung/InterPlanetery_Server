using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CommonLib;

namespace ChatClientWPF.Models
{
	/// <summary>
	/// 채팅 클라이언트 네트워크 모델 (MVP의 Model)
	/// </summary>
	public class ChatClientModel
	{
		private TcpClient? _client;
		private NetworkStream? _stream;
		private bool _isConnected;
		private bool _isRunning;
		private Task? _receiveTask;

		public string UserId { get; private set; } = string.Empty;
		public bool IsConnected => _isConnected;
		public bool IsInRoom { get; private set; }
		public string? CurrentRoomId { get; private set; }

		// 이벤트 정의
		public event Action<bool, string>? OnConnectionChanged;
		public event Action<ChatMessageModel>? OnChatMessageReceived;
		public event Action<string, int>? OnJoinRoomSuccess;
		public event Action<string>? OnJoinRoomFailed;
		public event Action? OnLeaveRoomSuccess;
		public event Action<string, int>? OnUserJoined;
		public event Action<string, int>? OnUserLeft;
		public event Action<string, string>? OnRoomClosed;
		public event Action<string>? OnError;
		public event Action? OnDisconnected;
		public event Action<List<RoomListItem>>? OnRoomListReceived;

		/// <summary>
		/// 서버에 연결
		/// </summary>
		public async Task<bool> ConnectAsync(string host, int port)
		{
			try
			{
				_client = new TcpClient();
				await _client.ConnectAsync(host, port);
				_stream = _client.GetStream();
				_isConnected = true;
				_isRunning = true;

				// 고유 사용자 ID 생성
				UserId = $"user_{Guid.NewGuid().ToString().Substring(0, 8)}";

				// 수신 태스크 시작
				_receiveTask = Task.Run(ReceiveLoop);

				OnConnectionChanged?.Invoke(true, $"Connected to {host}:{port}");
				return true;
			}
			catch (Exception ex)
			{
				_isConnected = false;
				OnConnectionChanged?.Invoke(false, $"Connection failed: {ex.Message}");
				return false;
			}
		}

		/// <summary>
		/// 연결 해제
		/// </summary>
		public void Disconnect()
		{
			_isRunning = false;
			_isConnected = false;
			IsInRoom = false;
			CurrentRoomId = null;

			_stream?.Close();
			_client?.Close();

			OnDisconnected?.Invoke();
			OnConnectionChanged?.Invoke(false, "Disconnected");
		}

		/// <summary>
		/// 룸 목록 조회 요청
		/// </summary>
		public async Task GetRoomListAsync()
		{
			if (!_isConnected)
				return;

			var protocol = new Protocol(CommonLib.ProtocolType.GET_ROOM_LIST);
			await SendProtocolAsync(protocol);
		}

		/// <summary>
		/// 룸 입장 요청
		/// </summary>
		public async Task JoinRoomAsync()
		{
			if (!_isConnected || IsInRoom)
				return;

			var protocol = new Protocol(CommonLib.ProtocolType.JOIN_ROOM)
				.AddParam("userId", UserId);

			await SendProtocolAsync(protocol);
		}

		/// <summary>
		/// 룸 퇴장 요청
		/// </summary>
		public async Task LeaveRoomAsync()
		{
			if (!_isConnected || !IsInRoom)
				return;

			var protocol = new Protocol(CommonLib.ProtocolType.LEAVE_ROOM)
				.AddParam("userId", UserId);

			await SendProtocolAsync(protocol);
		}

		/// <summary>
		/// 채팅 메시지 전송
		/// </summary>
		public async Task SendChatMessageAsync(string message)
		{
			if (!_isConnected || !IsInRoom || string.IsNullOrWhiteSpace(message))
				return;

			// 서버와 호환되도록 "message" 파라미터로 전송
			var protocol = new Protocol(CommonLib.ProtocolType.CHAT_MESSAGE)
				.AddParam("message", message);

			await SendProtocolAsync(protocol);
		}

		/// <summary>
		/// 하트비트 전송
		/// </summary>
		public async Task SendHeartbeatAsync()
		{
			if (!_isConnected)
				return;

			var protocol = new Protocol(CommonLib.ProtocolType.HEARTBEAT);
			await SendProtocolAsync(protocol);
		}

		/// <summary>
		/// 프로토콜 전송
		/// </summary>
		private async Task SendProtocolAsync(Protocol protocol)
		{
			if (_stream == null || !_isConnected)
				return;

			try
			{
				byte[] data = protocol.Serialize();
				await _stream.WriteAsync(data, 0, data.Length);
			}
			catch (Exception ex)
			{
				OnError?.Invoke($"Send error: {ex.Message}");
				Disconnect();
			}
		}

		/// <summary>
		/// 수신 루프
		/// </summary>
		private async Task ReceiveLoop()
		{
			byte[] sizeBuffer = new byte[4];

			while (_isRunning && _stream != null)
			{
				try
				{
					// 1. 크기 읽기 (4바이트)
					int bytesRead = await _stream.ReadAsync(sizeBuffer, 0, 4);
					if (bytesRead != 4)
					{
						Disconnect();
						break;
					}

					int packetSize = BitConverter.ToInt32(sizeBuffer, 0);

					// 2. 전체 패킷 읽기
					byte[] packetBuffer = new byte[packetSize + 4];
					Array.Copy(sizeBuffer, 0, packetBuffer, 0, 4);

					int totalRead = 0;
					while (totalRead < packetSize)
					{
						bytesRead = await _stream.ReadAsync(packetBuffer, 4 + totalRead, packetSize - totalRead);
						if (bytesRead == 0)
						{
							Disconnect();
							return;
						}
						totalRead += bytesRead;
					}

					// 3. 역직렬화 및 처리
					Protocol? protocol = Protocol.Deserialize(packetBuffer);
					if (protocol != null)
					{
						HandleProtocol(protocol);
					}
				}
				catch (Exception ex)
				{
					if (_isRunning)
					{
						OnError?.Invoke($"Receive error: {ex.Message}");
						Disconnect();
					}
					break;
				}
			}
		}

		/// <summary>
		/// 프로토콜 처리
		/// </summary>
		private void HandleProtocol(Protocol protocol)
		{
			switch (protocol.Type)
			{
				case CommonLib.ProtocolType.JOIN_SUCCESS:
					{
						// 서버는 roomInfo 구조체로 전송
						RoomInfo roomInfo = protocol.GetStruct<RoomInfo>("roomInfo");
						string roomId = roomInfo.RoomId ?? "UNKNOWN";
						int playerCount = roomInfo.PlayerCount;
						IsInRoom = true;
						CurrentRoomId = roomId;
						OnJoinRoomSuccess?.Invoke(roomId, playerCount);
					}
					break;

				case CommonLib.ProtocolType.JOIN_FAILED:
					{
						string reason = protocol.GetParam<string>("reason", "Unknown");
						OnJoinRoomFailed?.Invoke(reason);
					}
					break;

				case CommonLib.ProtocolType.LEAVE_SUCCESS:
					{
						IsInRoom = false;
						CurrentRoomId = null;
						OnLeaveRoomSuccess?.Invoke();
					}
					break;

				case CommonLib.ProtocolType.USER_JOINED:
					{
						string userId = protocol.GetParam<string>("userId", "");
						int playerCount = protocol.GetParam<int>("playerCount", 0);
						OnUserJoined?.Invoke(userId, playerCount);
					}
					break;

				case CommonLib.ProtocolType.USER_LEFT:
					{
						string userId = protocol.GetParam<string>("userId", "");
						int playerCount = protocol.GetParam<int>("playerCount", 0);
						OnUserLeft?.Invoke(userId, playerCount);
					}
					break;

				case CommonLib.ProtocolType.CHAT_BROADCAST:
					{
						ChatMessage chatMsg = protocol.GetStruct<ChatMessage>("chatMessage");
						var model = new ChatMessageModel
						{
							SenderId = chatMsg.SenderId,
							Message = chatMsg.Message,
							Timestamp = chatMsg.Timestamp,
							IsOwnMessage = chatMsg.SenderId == UserId
						};
						OnChatMessageReceived?.Invoke(model);
					}
					break;

				case CommonLib.ProtocolType.ROOM_CLOSED:
					{
						string roomId = protocol.GetParam<string>("roomId", "");
						string reason = protocol.GetParam<string>("reason", "");
						IsInRoom = false;
						CurrentRoomId = null;
						OnRoomClosed?.Invoke(roomId, reason);
					}
					break;

				case CommonLib.ProtocolType.ROOM_LIST:
					{
						try
						{
							var roomList = protocol.GetParam<List<RoomListItem>>("roomList", new List<RoomListItem>());
							OnRoomListReceived?.Invoke(roomList);
						}
						catch (Exception ex)
						{
							OnError?.Invoke($"Failed to parse room list: {ex.Message}");
						}
					}
					break;

				case CommonLib.ProtocolType.ERROR:
					{
						string message = protocol.GetParam<string>("message", "Unknown error");
						OnError?.Invoke(message);
					}
					break;

				case CommonLib.ProtocolType.HEARTBEAT_ACK:
					// 하트비트 응답은 로그만 (필요시 처리)
					break;
			}
		}
	}

	/// <summary>
	/// 채팅 메시지 모델 (View용)
	/// </summary>
	public class ChatMessageModel
	{
		public string SenderId { get; set; } = string.Empty;
		public string Message { get; set; } = string.Empty;
		public long Timestamp { get; set; }
		public bool IsOwnMessage { get; set; }

		public DateTime DateTime => DateTimeOffset.FromUnixTimeMilliseconds(Timestamp).LocalDateTime;
		public string FormattedTime => DateTime.ToString("HH:mm:ss");
	}
}
