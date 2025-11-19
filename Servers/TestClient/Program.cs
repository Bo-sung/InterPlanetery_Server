using System.Net.Sockets;
using System.Text;
using CommonLib;
using ProtoType = CommonLib.ProtocolType;

namespace TestClient
{
    class Program
    {
        private static TcpClient? client;
        private static NetworkStream? stream;
        private static bool isRunning = false;
        private static bool isLoggedIn = false;
        private static string? currentUsername;
        private static string? currentPassword;
        private static CancellationTokenSource? cancellationTokenSource;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Game Server Test Client ===");
            Console.WriteLine();

            while (true)
            {
                if (client == null || !client.Connected)
                {
                    Console.WriteLine("[Disconnected] Please connect to server first.");
                    ShowConnectionMenu();
                }
                else if (!isLoggedIn)
                {
                    Console.WriteLine("[Connected] Please login or register.");
                    ShowAuthMenu();
                }
                else
                {
                    Console.WriteLine($"[Logged in as: {currentUsername}]");
                    ShowMainMenu();
                }

                Console.Write("\nSelect option: ");
                string? input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                    continue;

                try
                {
                    if (client == null || !client.Connected)
                    {
                        await HandleConnectionMenu(input);
                    }
                    else if (!isLoggedIn)
                    {
                        await HandleAuthMenu(input);
                    }
                    else
                    {
                        await HandleMainMenu(input);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[Error] {ex.Message}");
                }

                Console.WriteLine();
            }
        }

        static void ShowConnectionMenu()
        {
            Console.WriteLine("\n--- Connection Menu ---");
            Console.WriteLine("1. Connect to server");
            Console.WriteLine("0. Exit");
        }

        static void ShowAuthMenu()
        {
            Console.WriteLine("\n--- Authentication Menu ---");
            Console.WriteLine("1. Auto Register (Guest)");
            Console.WriteLine("2. Register");
            Console.WriteLine("3. Login");
            Console.WriteLine("9. Disconnect");
            Console.WriteLine("0. Exit");
        }

        static void ShowMainMenu()
        {
            Console.WriteLine("\n--- Main Menu ---");
            Console.WriteLine("1. Join Lobby");
            Console.WriteLine("2. Refresh Room List");
            Console.WriteLine("3. Create Room");
            Console.WriteLine("4. Join Room");
            Console.WriteLine("5. Logout");
            Console.WriteLine("9. Disconnect");
            Console.WriteLine("0. Exit");
        }

        static async Task HandleConnectionMenu(string input)
        {
            switch (input)
            {
                case "1":
                    await ConnectToServer();
                    break;
                case "0":
                    await Disconnect();
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }

        static async Task HandleAuthMenu(string input)
        {
            switch (input)
            {
                case "1":
                    await AutoRegister();
                    break;
                case "2":
                    await Register();
                    break;
                case "3":
                    await Login();
                    break;
                case "9":
                    await Disconnect();
                    break;
                case "0":
                    await Disconnect();
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }

        static async Task HandleMainMenu(string input)
        {
            switch (input)
            {
                case "1":
                    await JoinLobby();
                    break;
                case "2":
                    await RefreshRoomList();
                    break;
                case "3":
                    await CreateRoom();
                    break;
                case "4":
                    await JoinRoom();
                    break;
                case "5":
                    await Logout();
                    break;
                case "9":
                    await Disconnect();
                    break;
                case "0":
                    await Disconnect();
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }

        static async Task ConnectToServer()
        {
            Console.Write("Enter server IP (default: 127.0.0.1): ");
            string? ip = Console.ReadLine();
            if (string.IsNullOrEmpty(ip))
                ip = "127.0.0.1";

            Console.Write("Enter server port (default: 7777): ");
            string? portStr = Console.ReadLine();
            int port = 7777;
            if (!string.IsNullOrEmpty(portStr) && int.TryParse(portStr, out int parsedPort))
                port = parsedPort;

            try
            {
                client = new TcpClient();
                await client.ConnectAsync(ip, port);
                stream = client.GetStream();

                Console.WriteLine($"\n[Success] Connected to {ip}:{port}");

                // Start receive loop
                cancellationTokenSource = new CancellationTokenSource();
                _ = Task.Run(() => ReceiveLoop(cancellationTokenSource.Token));

                // Start heartbeat
                _ = Task.Run(() => HeartbeatLoop(cancellationTokenSource.Token));

                isRunning = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error] Connection failed: {ex.Message}");
                client?.Close();
                client = null;
                stream = null;
            }
        }

        static async Task Disconnect()
        {
            if (client != null)
            {
                isRunning = false;
                isLoggedIn = false;
                currentUsername = null;
                currentPassword = null;

                cancellationTokenSource?.Cancel();
                stream?.Close();
                client?.Close();
                client = null;
                stream = null;

                Console.WriteLine("\n[Info] Disconnected from server.");
            }
        }

        static async Task AutoRegister()
        {
            if (stream == null) return;

            Console.WriteLine("\n[Info] Requesting auto registration...");

            Protocol protocol = new Protocol(ProtoType.REQUEST_REGISTER_AUTO);
            await SendAsync(protocol.Serialize());
        }

        static async Task Register()
        {
            if (stream == null) return;

            Console.Write("Enter username (3-20 characters): ");
            string? username = Console.ReadLine();

            Console.Write("Enter password (4-50 characters): ");
            string? password = Console.ReadLine();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine("\n[Error] Username and password are required.");
                return;
            }

            Protocol protocol = new Protocol(ProtoType.REQUEST_REGISTER)
                .AddParam("username", username)
                .AddParam("password", password);

            await SendAsync(protocol.Serialize());
        }

        static async Task Login()
        {
            if (stream == null) return;

            Console.Write("Enter username: ");
            string? username = Console.ReadLine();

            Console.Write("Enter password: ");
            string? password = Console.ReadLine();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine("\n[Error] Username and password are required.");
                return;
            }

            currentUsername = username;
            currentPassword = password;

            Protocol protocol = new Protocol(ProtoType.REQUEST_LOGIN)
                .AddParam("username", username)
                .AddParam("password", password);

            await SendAsync(protocol.Serialize());
        }

        static async Task Logout()
        {
            if (stream == null) return;

            Protocol protocol = new Protocol(ProtoType.REQUEST_LOGOUT);
            await SendAsync(protocol.Serialize());

            isLoggedIn = false;
            currentUsername = null;
            currentPassword = null;
        }

        static async Task JoinLobby()
        {
            if (stream == null) return;

            Console.Write("Enter page number (0 for all): ");
            string? pageStr = Console.ReadLine();
            int page = 0;
            if (!string.IsNullOrEmpty(pageStr) && int.TryParse(pageStr, out int parsedPage))
                page = parsedPage;

            Protocol protocol = new Protocol(ProtoType.REQUEST_JOIN_LOBBY)
                .AddParam("Page", page);

            await SendAsync(protocol.Serialize());
        }

        static async Task RefreshRoomList()
        {
            if (stream == null) return;

            Protocol protocol = new Protocol(ProtoType.REFRESH_LOBBY);
            await SendAsync(protocol.Serialize());
        }

        static async Task CreateRoom()
        {
            if (stream == null) return;

            Console.Write("Enter room name: ");
            string? roomName = Console.ReadLine();
            if (string.IsNullOrEmpty(roomName))
                roomName = "Test Room";

            Console.Write("Enter map ID (default: 0): ");
            string? mapIdStr = Console.ReadLine();
            int mapId = 0;
            if (!string.IsNullOrEmpty(mapIdStr) && int.TryParse(mapIdStr, out int parsedMapId))
                mapId = parsedMapId;

            Console.Write("Is private? (y/n, default: n): ");
            string? isPrivateStr = Console.ReadLine();
            bool isPrivate = !string.IsNullOrEmpty(isPrivateStr) && isPrivateStr.ToLower() == "y";

            Protocol protocol = new Protocol(ProtoType.REQUEST_CREATE_ROOM)
                .AddParam("roomName", roomName)
                .AddParam("mapId", mapId)
                .AddParam("isPrivate", isPrivate);

            await SendAsync(protocol.Serialize());
        }

        static async Task JoinRoom()
        {
            if (stream == null) return;

            Console.Write("Enter room ID: ");
            string? roomId = Console.ReadLine();
            if (string.IsNullOrEmpty(roomId))
            {
                Console.WriteLine("\n[Error] Room ID is required.");
                return;
            }

            Console.Write("Enter slot (0 or 1): ");
            string? slotStr = Console.ReadLine();
            int slot = 0;
            if (!string.IsNullOrEmpty(slotStr) && int.TryParse(slotStr, out int parsedSlot))
                slot = parsedSlot;

            // userId는 서버에서 세션 정보로부터 가져오지만, 프로토콜 정의상 필요하면 더미 값 전달
            Protocol protocol = new Protocol(ProtoType.REQUEST_JOIN_ROOM)
                .AddParam("userId", 0)
                .AddParam("roomId", roomId)
                .AddParam("slot", slot);

            await SendAsync(protocol.Serialize());
        }

        static async Task SendAsync(byte[] data)
        {
            if (stream == null) return;

            try
            {
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error] Send failed: {ex.Message}");
            }
        }

        static async Task ReceiveLoop(CancellationToken cancellationToken)
        {
            byte[] lengthBuffer = new byte[4];

            while (isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (stream == null || !client!.Connected)
                        break;

                    // Read message length
                    int bytesRead = await stream.ReadAsync(lengthBuffer, 0, 4, cancellationToken);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("\n[Info] Server closed connection.");
                        break;
                    }

                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                    if (messageLength <= 0 || messageLength > 1024 * 1024)
                    {
                        Console.WriteLine($"\n[Error] Invalid message length: {messageLength}");
                        break;
                    }

                    // Read full message
                    byte[] messageBuffer = new byte[messageLength + 4];
                    Array.Copy(lengthBuffer, 0, messageBuffer, 0, 4);

                    int totalRead = 0;
                    while (totalRead < messageLength)
                    {
                        bytesRead = await stream.ReadAsync(messageBuffer, 4 + totalRead, messageLength - totalRead, cancellationToken);
                        if (bytesRead == 0)
                            break;
                        totalRead += bytesRead;
                    }

                    // Deserialize and handle
                    Protocol? protocol = Protocol.Deserialize(messageBuffer);
                    if (protocol != null)
                    {
                        HandleReceivedProtocol(protocol);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (isRunning)
                        Console.WriteLine($"\n[Error] Receive error: {ex.Message}");
                    break;
                }
            }

            isRunning = false;
        }

        static async Task HeartbeatLoop(CancellationToken cancellationToken)
        {
            while (isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(10000, cancellationToken); // Send heartbeat every 10 seconds

                    if (stream != null && client != null && client.Connected)
                    {
                        Protocol heartbeat = new Protocol(ProtoType.HEARTBEAT);
                        await SendAsync(heartbeat.Serialize());
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (isRunning)
                        Console.WriteLine($"\n[Error] Heartbeat error: {ex.Message}");
                }
            }
        }

        static void HandleReceivedProtocol(Protocol protocol)
        {
            Console.WriteLine($"\n[Received] Protocol Type: {protocol.Type}");

            switch (protocol.Type)
            {
                case ProtoType.RESPONSE:
                    HandleResponse(protocol);
                    break;

                case ProtoType.HEARTBEAT_ACK:
                    // Heartbeat acknowledged - no need to print
                    break;

                case ProtoType.USER_JOINED:
                    HandleUserJoined(protocol);
                    break;

                case ProtoType.USER_LEFT:
                    HandleUserLeft(protocol);
                    break;

                case ProtoType.ROOM_INFO_CHANGED:
                    HandleRoomInfoChanged(protocol);
                    break;

                case ProtoType.ROOM_CLOSED:
                    HandleRoomClosed(protocol);
                    break;

                default:
                    Console.WriteLine($"[Info] Unhandled protocol type: {protocol.Type}");
                    break;
            }
        }

        static void HandleResponse(Protocol protocol)
        {
            int protoId = protocol.GetParam<int>("protoId");
            byte statusByte = protocol.GetParam<byte>("status");
            StateCode status = (StateCode)statusByte;
            string message = protocol.GetParam<string>("message");

            Console.WriteLine($"[Response] ProtoId: {protoId}, Status: {status}, Message: {message}");

            if (status == StateCode.SUCCESS)
            {
                switch (protoId)
                {
                    case ProtoType.REQUEST_REGISTER_AUTO:
                        string username = protocol.GetParam<string>("username");
                        string password = protocol.GetParam<string>("password");
                        Console.WriteLine($"[Auto Register Success]");
                        Console.WriteLine($"  Username: {username}");
                        Console.WriteLine($"  Password: {password}");
                        Console.WriteLine("  Please save these credentials!");

                        // Auto login
                        currentUsername = username;
                        currentPassword = password;
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(1000);
                            await Login();
                        });
                        break;

                    case ProtoType.REQUEST_REGISTER:
                        Console.WriteLine($"[Register Success] You can now login.");
                        break;

                    case ProtoType.REQUEST_LOGIN:
                        string sessionId = protocol.GetParam<string>("sessionId");
                        Console.WriteLine($"[Login Success] Session ID: {sessionId}");
                        isLoggedIn = true;
                        break;

                    case ProtoType.REQUEST_LOGOUT:
                        Console.WriteLine($"[Logout Success]");
                        isLoggedIn = false;
                        currentUsername = null;
                        currentPassword = null;
                        break;

                    case ProtoType.REQUEST_JOIN_LOBBY:
                        int roomCount = protocol.GetParam<int>("roomCount");
                        int page = protocol.GetParam<int>("page");
                        var roomList = protocol.GetParam<RoomInfo[]>("roomList");
                        Console.WriteLine($"[Join Lobby Success]");
                        Console.WriteLine($"  Total Rooms: {roomCount}, Page: {page}");
                        if (roomList != null && roomList.Length > 0)
                        {
                            Console.WriteLine("  Rooms:");
                            foreach (var room in roomList)
                            {
                                Console.WriteLine($"    - {room.RoomId}: {room.RoomName} ({room.PlayerCount}/{room.MaxPlayers}) - State: {room.RoomState}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("  No rooms available.");
                        }
                        break;

                    case ProtoType.REFRESH_LOBBY:
                        var refreshRoomList = protocol.GetParam<RoomInfo[]>("roomList");
                        Console.WriteLine($"[Refresh Lobby Success]");
                        if (refreshRoomList != null && refreshRoomList.Length > 0)
                        {
                            Console.WriteLine("  Rooms:");
                            foreach (var room in refreshRoomList)
                            {
                                Console.WriteLine($"    - {room.RoomId}: {room.RoomName} ({room.PlayerCount}/{room.MaxPlayers}) - State: {room.RoomState}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("  No rooms available.");
                        }
                        break;

                    case ProtoType.REQUEST_CREATE_ROOM:
                        string createdRoomId = protocol.GetParam<string>("roomId");
                        int nextSlot = protocol.GetParam<int>("slot");
                        Console.WriteLine($"[Create Room Success]");
                        Console.WriteLine($"  Room ID: {createdRoomId}");
                        Console.WriteLine($"  Available Slot: {nextSlot}");
                        break;

                    case ProtoType.REQUEST_JOIN_ROOM:
                        var roomInfo = protocol.GetStruct<RoomInfo>("roominfo");
                        int chatChannelId = protocol.GetParam<int>("chatChannelId");
                        Console.WriteLine($"[Join Room Success]");
                        Console.WriteLine($"  Room: {roomInfo.RoomId} - {roomInfo.RoomName}");
                        Console.WriteLine($"  Players: {roomInfo.PlayerCount}/{roomInfo.MaxPlayers}");
                        Console.WriteLine($"  Chat Channel: {chatChannelId}");
                        break;
                }
            }
            else
            {
                Console.WriteLine($"[Error] Request failed: {message}");
            }
        }

        static void HandleUserJoined(Protocol protocol)
        {
            int userId = protocol.GetParam<int>("userId");
            string userName = protocol.GetParam<string>("userName");
            int playerCount = protocol.GetParam<int>("playerCount");

            Console.WriteLine($"[User Joined] {userName} (ID: {userId}) - Total Players: {playerCount}");
        }

        static void HandleUserLeft(Protocol protocol)
        {
            int userId = protocol.GetParam<int>("userId");
            int playerCount = protocol.GetParam<int>("playerCount");

            Console.WriteLine($"[User Left] User ID: {userId} - Total Players: {playerCount}");
        }

        static void HandleRoomInfoChanged(Protocol protocol)
        {
            string roomId = protocol.GetParam<string>("roomId");
            var roomInfo = protocol.GetStruct<RoomInfo>("roomInfo");

            Console.WriteLine($"[Room Info Changed] Room: {roomId}");
            Console.WriteLine($"  Name: {roomInfo.RoomName}");
            Console.WriteLine($"  Players: {roomInfo.PlayerCount}/{roomInfo.MaxPlayers}");
            Console.WriteLine($"  State: {roomInfo.RoomState}");
        }

        static void HandleRoomClosed(Protocol protocol)
        {
            string roomId = protocol.GetParam<string>("roomId");
            string reason = protocol.GetParam<string>("reason");

            Console.WriteLine($"[Room Closed] Room: {roomId}, Reason: {reason}");
        }
    }
}
