using System.Net;
using System.Net.Sockets;
using BaseServer.Core.Game;
using BaseServer.Core.Game.Entities;
using BaseServer.Core.Game.Session;
using CommonLib;
using CommonLib.Commands;
using ProtocolType = CommonLib.ProtocolType;

namespace BaseServer.StateTests;

internal static class Program
{
    private static readonly (string Name, Func<Task> Run)[] Tests =
    {
        ("transition matrix", TestTransitionMatrix),
        ("protocol policy", TestProtocolPolicy),
        ("client session lifecycle", TestClientSessionLifecycle),
        ("dynamic handler lifecycle", TestDynamicHandlerLifecycle),
        ("game command contract", TestGameCommandContract),
        ("game player slot lifecycle", TestGamePlayerSlotLifecycle),
        ("game user join rollback", TestGameUserJoinRollback),
        ("game stop is idempotent", TestGameStopIsIdempotent),
        ("player reinit releases prior session", TestPlayerReinitReleasesPriorSession),
        ("waiting room user JSON contract", TestWaitingRoomUserJsonContract)
    };

    public static async Task<int> Main()
    {
        int failed = 0;

        foreach ((string name, Func<Task> run) in Tests)
        {
            try
            {
                await run();
                Console.WriteLine($"PASS: {name}");
            }
            catch (Exception exception)
            {
                failed++;
                Console.Error.WriteLine($"FAIL: {name}\n{exception}");
            }
        }

        Console.WriteLine($"Completed {Tests.Length} tests: {Tests.Length - failed} passed, {failed} failed.");
        return failed == 0 ? 0 : 1;
    }

    private static Task TestTransitionMatrix()
    {
        SessionState[] states = Enum.GetValues<SessionState>();
        foreach (SessionState current in states)
        {
            foreach (SessionState next in states)
            {
                bool expected = IsExpectedTransition(current, next);
                bool actual = SessionProtocolPolicy.CanTransition(current, next);
                AssertEqual(expected, actual, $"transition {current} -> {next}");
            }
        }

        return Task.CompletedTask;
    }

    private static Task TestProtocolPolicy()
    {
        int[] protocols =
        {
            ProtocolType.HEARTBEAT,
            ProtocolType.REQUEST_REGISTER,
            ProtocolType.REQUEST_REGISTER_AUTO,
            ProtocolType.REQUEST_LOGIN,
            ProtocolType.REQUEST_LOGOUT,
            ProtocolType.REQUEST_JOIN_LOBBY,
            ProtocolType.REFRESH_LOBBY,
            ProtocolType.REQUEST_CREATE_ROOM,
            ProtocolType.REQUEST_JOIN_ROOM,
            ProtocolType.REQUEST_LEFT_ROOM,
            ProtocolType.REQUEST_READY,
            ProtocolType.REQUEST_GAME_CL_READY,
            ProtocolType.SUBMIT_COMMAND,
            int.MaxValue
        };

        var allowedByState = new Dictionary<SessionState, HashSet<int>>
        {
            [SessionState.Connected] = Set(
                ProtocolType.HEARTBEAT,
                ProtocolType.REQUEST_REGISTER,
                ProtocolType.REQUEST_REGISTER_AUTO,
                ProtocolType.REQUEST_LOGIN),
            [SessionState.Authenticated] = Set(
                ProtocolType.HEARTBEAT,
                ProtocolType.REQUEST_LOGOUT,
                ProtocolType.REQUEST_JOIN_LOBBY),
            [SessionState.Lobby] = Set(
                ProtocolType.HEARTBEAT,
                ProtocolType.REQUEST_LOGOUT,
                ProtocolType.REQUEST_JOIN_LOBBY,
                ProtocolType.REFRESH_LOBBY,
                ProtocolType.REQUEST_CREATE_ROOM,
                ProtocolType.REQUEST_JOIN_ROOM),
            [SessionState.Room] = Set(
                ProtocolType.HEARTBEAT,
                ProtocolType.REQUEST_LOGOUT,
                ProtocolType.REQUEST_LEFT_ROOM,
                ProtocolType.REQUEST_READY),
            [SessionState.InGame] = Set(
                ProtocolType.HEARTBEAT,
                ProtocolType.REQUEST_LOGOUT,
                ProtocolType.REQUEST_LEFT_ROOM,
                ProtocolType.REQUEST_GAME_CL_READY,
                ProtocolType.SUBMIT_COMMAND),
            [SessionState.Disconnected] = Set()
        };

        foreach ((SessionState state, HashSet<int> allowed) in allowedByState)
        {
            foreach (int protocol in protocols)
            {
                bool expected = allowed.Contains(protocol);
                bool actual = SessionProtocolPolicy.IsAllowed(state, protocol);
                AssertEqual(expected, actual, $"protocol {protocol} in {state}");
            }
        }

        return Task.CompletedTask;
    }

    private static async Task TestClientSessionLifecycle()
    {
        (ClientSession session, TcpClient peer) = await CreateSessionPair();
        using (peer)
        {
            AssertEqual(SessionState.Connected, session.State, "initial state");
            AssertFalse(session.TryTransitionTo(SessionState.Lobby), "Connected must not jump to Lobby");
            AssertEqual(SessionState.Connected, session.State, "state after rejected transition");

            AssertTrue(session.TryTransitionTo(SessionState.Authenticated), "Connected -> Authenticated");
            AssertTrue(session.TryTransitionTo(SessionState.Lobby), "Authenticated -> Lobby");
            AssertTrue(session.TryTransitionTo(SessionState.Room), "Lobby -> Room");
            AssertTrue(session.TryTransitionTo(SessionState.InGame), "Room -> InGame");
            AssertTrue(session.TryTransitionTo(SessionState.Room), "InGame -> Room");
            AssertTrue(session.TryTransitionTo(SessionState.Lobby), "Room -> Lobby");

            session.Disconnect();
            AssertEqual(SessionState.Disconnected, session.State, "state after disconnect");
            AssertFalse(session.TryTransitionTo(SessionState.Authenticated), "Disconnected must be terminal");
        }
    }

    private static async Task TestDynamicHandlerLifecycle()
    {
        (ClientSession session, TcpClient peer) = await CreateSessionPair();
        using (peer)
        {
            int baseline = session.RegisteredProtocolHandlerCount;

            var roomUser = new GameRoomUser();
            roomUser.Initialize(session);
            AssertEqual(baseline + 2, session.RegisteredProtocolHandlerCount, "room handlers registered");

            roomUser.Initialize(session);
            AssertEqual(baseline + 2, session.RegisteredProtocolHandlerCount, "room handler reinitialize is stable");

            roomUser.Cleanup();
            AssertEqual(baseline, session.RegisteredProtocolHandlerCount, "room handlers removed");

            var gamePlayer = new GamePlayer();
            gamePlayer.Initialize(session, new CommandSenderStub());
            AssertEqual(baseline + 2, session.RegisteredProtocolHandlerCount, "game handlers registered");

            gamePlayer.Cleanup();
            AssertEqual(baseline, session.RegisteredProtocolHandlerCount, "game handlers removed");
            session.Disconnect();
        }
    }

    private static Task TestGameCommandContract()
    {
        AssertEqual(0, (int)GameCommandType.None, "None command value");
        AssertEqual(1, (int)GameCommandType.ProduceFleet, "ProduceFleet command value");
        AssertEqual(2, (int)GameCommandType.MoveFleet, "MoveFleet command value");
        return Task.CompletedTask;
    }

    private static async Task TestGamePlayerSlotLifecycle()
    {
        var stub = new CommandSenderStub();
        var peers = new List<TcpClient>();
        try
        {
            (ClientSession s1, TcpClient p1) = await CreateSessionInRoom(); peers.Add(p1);
            (ClientSession s2, TcpClient p2) = await CreateSessionInRoom(); peers.Add(p2);
            (ClientSession s3, TcpClient p3) = await CreateSessionInRoom(); peers.Add(p3);

            var game = new Game();
            AssertEqual(0, game.ActivePlayerCount, "no players before join");

            AssertTrue(game.UserJoin(s1, stub), "join s1");
            AssertEqual(SessionState.InGame, s1.State, "s1 InGame after join");
            AssertEqual(1, game.ActivePlayerCount, "one player after first join");

            AssertTrue(game.UserJoin(s2, stub), "join s2");
            AssertEqual(2, game.ActivePlayerCount, "two players after second join");

            // Room is full (2 slots): further joins must be rejected without touching the session.
            AssertFalse(game.UserJoin(s3, stub), "join s3 rejected when full");
            AssertEqual(2, game.ActivePlayerCount, "still two players when full");
            AssertEqual(SessionState.Room, s3.State, "s3 stays Room when rejected");

            AssertTrue(game.UserLeave(s1), "leave s1");
            AssertEqual(SessionState.Room, s1.State, "s1 back to Room after leave");
            AssertEqual(1, game.ActivePlayerCount, "one player after leave");

            // Duplicate leave of an already-removed session must be an idempotent no-op.
            AssertFalse(game.UserLeave(s1), "duplicate leave is a no-op");
            AssertEqual(1, game.ActivePlayerCount, "count unchanged after duplicate leave");

            AssertTrue(game.UserLeave(s2), "leave s2");
            AssertEqual(0, game.ActivePlayerCount, "no players after all leave");

            s1.Disconnect();
            s2.Disconnect();
            s3.Disconnect();
        }
        finally
        {
            foreach (TcpClient peer in peers)
                peer.Dispose();
        }
    }

    private static async Task TestGameUserJoinRollback()
    {
        var stub = new CommandSenderStub();
        (ClientSession session, TcpClient peer) = await CreateSessionPair();
        using (peer)
        {
            // Authenticated cannot transition directly to InGame; UserJoin must roll back the slot.
            AssertTrue(session.TryTransitionTo(SessionState.Authenticated), "-> Authenticated");

            var game = new Game();
            AssertFalse(game.UserJoin(session, stub), "join rejected when session cannot enter InGame");
            AssertEqual(0, game.ActivePlayerCount, "no player retained after rollback");
            AssertEqual(SessionState.Authenticated, session.State, "session state unchanged after rollback");

            session.Disconnect();
        }
    }

    private static Task TestGameStopIsIdempotent()
    {
        var game = new Game();
        AssertFalse(game.IsEnded, "new game not ended");
        AssertFalse(game.IsRunning, "new game not running");

        game.StopGame();
        AssertTrue(game.IsEnded, "ended after StopGame");

        game.StopGame(); // second stop must be a safe no-op
        AssertTrue(game.IsEnded, "still ended after second StopGame");

        game.Dispose(); // dispose is idempotent and terminal
        AssertTrue(game.IsEnded, "still ended after Dispose");

        game.Dispose(); // second dispose must not throw
        return Task.CompletedTask;
    }

    private static async Task TestPlayerReinitReleasesPriorSession()
    {
        var stub = new CommandSenderStub();
        (ClientSession s1, TcpClient p1) = await CreateSessionPair();
        (ClientSession s2, TcpClient p2) = await CreateSessionPair();
        using (p1)
        using (p2)
        {
            int base1 = s1.RegisteredProtocolHandlerCount;
            int base2 = s2.RegisteredProtocolHandlerCount;

            var player = new GamePlayer();
            player.Initialize(s1, stub);
            AssertEqual(base1 + 2, s1.RegisteredProtocolHandlerCount, "s1 gets game handlers");

            // Re-initializing onto a different session must release the prior session's handlers.
            player.Initialize(s2, stub);
            AssertEqual(base1, s1.RegisteredProtocolHandlerCount, "s1 handlers released on reinit");
            AssertEqual(base2 + 2, s2.RegisteredProtocolHandlerCount, "s2 gets game handlers");

            player.Cleanup();
            AssertEqual(base2, s2.RegisteredProtocolHandlerCount, "s2 handlers released on cleanup");

            s1.Disconnect();
            s2.Disconnect();
        }
    }

    private static Task TestWaitingRoomUserJsonContract()
    {
        var value = new WaittingRoomUser
        {
            userInfo = new UserInfo { UserId = 7, UserName = "contract-user" },
            IsReady = true
        };

        string json = Newtonsoft.Json.JsonConvert.SerializeObject(value);
        AssertTrue(json.Contains("\"userInfo\"", StringComparison.Ordinal), "canonical userInfo wire name");
        AssertFalse(json.Contains("\"UserInfo\"", StringComparison.Ordinal), "legacy casing not serialized");

        WaittingRoomUser legacy = Newtonsoft.Json.JsonConvert.DeserializeObject<WaittingRoomUser>(
            "{\"UserInfo\":{\"UserId\":8,\"UserName\":\"legacy-user\"},\"IsReady\":false}");
        AssertEqual(8, legacy.userInfo.UserId, "legacy casing remains readable");
        return Task.CompletedTask;
    }

    private static bool IsExpectedTransition(SessionState current, SessionState next)
    {
        if (current == next)
            return true;
        if (next == SessionState.Disconnected)
            return current != SessionState.Disconnected;

        return (current, next) switch
        {
            (SessionState.Connected, SessionState.Authenticated) => true,
            (SessionState.Authenticated, SessionState.Lobby) => true,
            (SessionState.Lobby, SessionState.Room) => true,
            (SessionState.Room, SessionState.InGame) => true,
            (SessionState.Room, SessionState.Lobby) => true,
            (SessionState.InGame, SessionState.Room) => true,
            (SessionState.InGame, SessionState.Lobby) => true,
            _ => false
        };
    }

    private static async Task<(ClientSession Session, TcpClient Peer)> CreateSessionPair()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            var endpoint = (IPEndPoint)listener.LocalEndpoint;
            Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();
            var peer = new TcpClient();
            await peer.ConnectAsync(endpoint.Address, endpoint.Port);
            TcpClient serverConnection = await acceptTask;
            return (new ClientSession(serverConnection), peer);
        }
        finally
        {
            listener.Stop();
        }
    }

    private static async Task<(ClientSession Session, TcpClient Peer)> CreateSessionInRoom()
    {
        (ClientSession session, TcpClient peer) = await CreateSessionPair();
        AssertTrue(session.TryTransitionTo(SessionState.Authenticated), "-> Authenticated");
        AssertTrue(session.TryTransitionTo(SessionState.Lobby), "-> Lobby");
        AssertTrue(session.TryTransitionTo(SessionState.Room), "-> Room");
        return (session, peer);
    }

    private static HashSet<int> Set(params int[] values) => new(values);

    private static void AssertTrue(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException($"Expected true: {message}");
    }

    private static void AssertFalse(bool value, string message)
    {
        if (value)
            throw new InvalidOperationException($"Expected false: {message}");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
        where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{message}: expected {expected}, actual {actual}");
    }

    private sealed class CommandSenderStub : ICommandSender
    {
        public Task SendCommandToGame(Command command) => Task.CompletedTask;
    }
}
