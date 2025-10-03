using CommonLib;
using TestClient;

Console.WriteLine("=== InterPlanetery Chat Test Client ===");
Console.WriteLine();

// 네트워크 클라이언트 생성
NetworkClient client = new NetworkClient();
bool isInRoom = false;

// 이벤트 핸들러 등록
client.OnProtocolReceived += OnProtocolReceived;
client.OnDisconnected += OnDisconnected;
client.OnError += OnError;

// 서버 정보 입력
Console.Write("Server Host [127.0.0.1]: ");
string? host = Console.ReadLine();
if (string.IsNullOrWhiteSpace(host))
    host = "127.0.0.1";

Console.Write("Server Port [7777]: ");
string? portStr = Console.ReadLine();
int port = 7777;
if (!string.IsNullOrWhiteSpace(portStr) && int.TryParse(portStr, out int parsedPort))
    port = parsedPort;

// 서버 연결
Console.WriteLine($"\nConnecting to {host}:{port}...");
bool connected = await client.ConnectAsync(host, port);

if (!connected)
{
    Console.WriteLine("Failed to connect to server. Press any key to exit...");
    Console.ReadKey();
    return;
}

Console.WriteLine("Connected to server!");
Console.WriteLine();
PrintCommands();

// 명령 루프
while (client.IsConnected)
{
    Console.Write("> ");
    string? command = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(command))
        continue;

    await HandleCommand(command.Trim());
}

Console.WriteLine("Disconnected. Press any key to exit...");
Console.ReadKey();

// 명령 처리
async Task HandleCommand(string command)
{
    string[] parts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
    string cmd = parts[0].ToLower();

    switch (cmd)
    {
        case "help":
        case "?":
            PrintCommands();
            break;

        case "join":
            if (isInRoom)
            {
                Console.WriteLine("[Error] Already in a room. Use 'leave' first.");
                break;
            }
            await JoinRoom();
            break;

        case "leave":
            if (!isInRoom)
            {
                Console.WriteLine("[Error] Not in a room.");
                break;
            }
            await LeaveRoom();
            break;

        case "send":
        case "s":
            if (!isInRoom)
            {
                Console.WriteLine("[Error] Not in a room. Use 'join' first.");
                break;
            }
            if (parts.Length < 2)
            {
                Console.WriteLine("[Error] Usage: send <message>");
                break;
            }
            await SendMessage(parts[1]);
            break;

        case "quit":
        case "exit":
            client.Disconnect();
            break;

        default:
            Console.WriteLine($"[Error] Unknown command: {cmd}. Type 'help' for commands.");
            break;
    }
}

// 룸 입장
async Task JoinRoom()
{
    Protocol protocol = new Protocol(ChatProtocolType.JOIN_ROOM);
    bool success = await client.SendAsync(protocol);

    if (success)
    {
        Console.WriteLine("[Sent] JOIN_ROOM request");
    }
}

// 룸 퇴장
async Task LeaveRoom()
{
    Protocol protocol = new Protocol(ChatProtocolType.LEAVE_ROOM);
    bool success = await client.SendAsync(protocol);

    if (success)
    {
        Console.WriteLine("[Sent] LEAVE_ROOM request");
    }
}

// 메시지 전송
async Task SendMessage(string message)
{
    Protocol protocol = new Protocol(ChatProtocolType.CHAT_MESSAGE)
        .AddParam("message", message);

    bool success = await client.SendAsync(protocol);

    if (success)
    {
        Console.WriteLine($"[Sent] {message}");
    }
}

// 프로토콜 수신 처리
void OnProtocolReceived(Protocol protocol)
{
    switch (protocol.Type)
    {
        case ChatProtocolType.JOIN_SUCCESS:
            HandleJoinSuccess(protocol);
            break;

        case ChatProtocolType.JOIN_FAILED:
            HandleJoinFailed(protocol);
            break;

        case ChatProtocolType.LEAVE_SUCCESS:
            HandleLeaveSuccess(protocol);
            break;

        case ChatProtocolType.USER_JOINED:
            HandleUserJoined(protocol);
            break;

        case ChatProtocolType.USER_LEFT:
            HandleUserLeft(protocol);
            break;

        case ChatProtocolType.CHAT_BROADCAST:
            HandleChatBroadcast(protocol);
            break;

        case ChatProtocolType.ROOM_CLOSED:
            HandleRoomClosed(protocol);
            break;

        case ChatProtocolType.HEARTBEAT_ACK:
            // 하트비트 응답 (무시)
            break;

        case ChatProtocolType.ERROR:
            HandleError(protocol);
            break;

        default:
            Console.WriteLine($"[Warning] Unknown protocol type: {protocol.Type}");
            break;
    }
}

void HandleJoinSuccess(Protocol protocol)
{
    string roomId = protocol.GetParam<string>("roomId");
    int playerCount = protocol.GetParam<int>("playerCount");

    isInRoom = true;
    Console.WriteLine($"[SUCCESS] Joined room '{roomId}' (Players: {playerCount})");
}

void HandleJoinFailed(Protocol protocol)
{
    string reason = protocol.GetParam<string>("reason", "Unknown error");
    Console.WriteLine($"[FAILED] Join room failed: {reason}");
}

void HandleLeaveSuccess(Protocol protocol)
{
    isInRoom = false;
    Console.WriteLine("[SUCCESS] Left the room");
}

void HandleUserJoined(Protocol protocol)
{
    string userId = protocol.GetParam<string>("userId");
    int playerCount = protocol.GetParam<int>("playerCount");
    Console.WriteLine($"[INFO] User '{userId}' joined (Players: {playerCount})");
}

void HandleUserLeft(Protocol protocol)
{
    string userId = protocol.GetParam<string>("userId");
    int playerCount = protocol.GetParam<int>("playerCount");
    Console.WriteLine($"[INFO] User '{userId}' left (Players: {playerCount})");
}

void HandleChatBroadcast(Protocol protocol)
{
    ChatMessage chatMsg = protocol.GetStruct<ChatMessage>("chatMessage");
    Console.WriteLine($"[{chatMsg.SenderId}] {chatMsg.Message}");
}

void HandleRoomClosed(Protocol protocol)
{
    string roomId = protocol.GetParam<string>("roomId");
    string reason = protocol.GetParam<string>("reason", "Unknown reason");

    isInRoom = false;
    Console.WriteLine($"[INFO] Room '{roomId}' closed: {reason}");
}

void HandleError(Protocol protocol)
{
    string errorMsg = protocol.GetParam<string>("message", "Unknown error");
    Console.WriteLine($"[ERROR] {errorMsg}");
}

void OnDisconnected()
{
    isInRoom = false;
    Console.WriteLine("[INFO] Disconnected from server");
}

void OnError(string errorMsg)
{
    Console.WriteLine($"[ERROR] {errorMsg}");
}

void PrintCommands()
{
    Console.WriteLine("Available Commands:");
    Console.WriteLine("  help, ?         - Show this help");
    Console.WriteLine("  join            - Join a chat room");
    Console.WriteLine("  leave           - Leave the current room");
    Console.WriteLine("  send <message>  - Send a chat message (alias: s)");
    Console.WriteLine("  quit, exit      - Disconnect and exit");
    Console.WriteLine();
}
