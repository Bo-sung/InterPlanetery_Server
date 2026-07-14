// Headless two-client BaseServer E2E harness.
//
// Design notes / evidence tags:
//   FACT       = verified against read server/CommonLib source this task.
//   ASSUMPTION = fallback not fully pinned by the read sources (labeled inline).
//
// Framing/serialization are reused from CommonLib (Protocol.Serialize / Protocol.Deserialize):
//   FACT (CommonLib/Protocol.cs): 18-byte header, size field at offset 0 is the TOTAL frame
//   length INCLUDING the 4-byte size field itself; body is UTF-8 JSON of the parameter map.
//   FACT (BaseServer ClientSession.cs): server bounds a frame to 1 MB and rejects size < 18.
//
// Protocol constants, DTOs and the serializer are NOT copied here; they come from CommonLib.

using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Channels;
using CommonLib;
using CommonLib.Commands;
// Disambiguate CommonLib.ProtocolType from System.Net.Sockets.ProtocolType (alias, not a copy).
using ProtocolType = CommonLib.ProtocolType;

namespace E2ETests;

internal static class Program
{
    private const int MandatoryTotal = 10;

    // Printed by --validate-config and used as documentation of the executed order.
    private static readonly string[] PlannedStages =
    {
        "1. connect two independent TCP clients to <host:port>",
        "2. login both (REQUEST_LOGIN) and correlate RESPONSE by protoId (status == SUCCESS)",
        "3. join lobby both (REQUEST_JOIN_LOBBY)",
        "4. client1 create room (REQUEST_CREATE_ROOM); capture roomId + slot",
        "5. both join distinct slots (REQUEST_JOIN_ROOM; client1=created slot, client2=-1 auto)",
        "6. both set ready (REQUEST_READY)",
        "7. await GAME_SET; then send REQUEST_GAME_CL_READY",
        "   (SOURCE ORDER FACT: Game.StartGame => BroadcastGameSet => wait REQUEST_GAME_CL_READY => BroadcastGameStart;",
        "    so CL_READY is sent AFTER GAME_SET and BEFORE GAME_STARTED to avoid deadlock)",
        "8. await GAME_STARTED both",
        "9. await >=1 GAME_STATE both; validate serverTick + gameState present",
        "10. optional SUBMIT_COMMAND MoveFleet only if a fleet+planet target exists in state, else SKIP",
        "11. best-effort REQUEST_LEFT_ROOM + deterministic disconnect (finally)"
    };

    private static async Task<int> Main(string[] args)
    {
        Config cfg;
        try
        {
            cfg = Config.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"ARG ERROR: {ex.Message}");
            return 2;
        }

        if (cfg.ValidateConfig)
        {
            PrintValidateConfig(cfg);
            return 0; // config structurally valid; no sockets opened, no DB touched
        }

        // Normal (live) mode requires all four credential env vars. No auto-registration, no DB writes.
        (Creds creds, List<string> missing) = Creds.FromEnv();
        if (missing.Count > 0)
        {
            Console.Error.WriteLine(
                $"FATAL: missing required credential env var(s): {string.Join(", ", missing)}. " +
                "Set E2E_USER1, E2E_PASSWORD1, E2E_USER2, E2E_PASSWORD2. " +
                "This harness never auto-registers or writes to the database.");
            return 3;
        }

        Console.WriteLine(
            $"E2ETests live run: host={cfg.Host} port={cfg.Port} timeout={cfg.TimeoutSeconds}s " +
            "(credentials read from env; values are never printed)");

        int passed;
        string command;
        try
        {
            (passed, command) = await RunFlowAsync(cfg, creds);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FATAL: unexpected {Describe(ex)}");
            return 1;
        }

        bool allMandatory = passed == MandatoryTotal;
        Console.WriteLine($"SUMMARY: mandatory {passed}/{MandatoryTotal} passed; command {command}; " +
                          $"result {(allMandatory ? "PASS" : "FAIL")}");
        return allMandatory ? 0 : 1;
    }

    private static void PrintValidateConfig(Config cfg)
    {
        Console.WriteLine("E2ETests --validate-config (no sockets opened, no DB touched)");
        Console.WriteLine($"host            = {cfg.Host}");
        Console.WriteLine($"port            = {cfg.Port}");
        Console.WriteLine($"timeout-seconds = {cfg.TimeoutSeconds}");
        Console.WriteLine("credentials (presence only; values are NEVER printed):");
        foreach (string n in Creds.EnvNames)
        {
            bool set = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(n));
            Console.WriteLine($"  {n,-14} = {(set ? "SET" : "MISSING")}");
        }

        Console.WriteLine("planned stages:");
        foreach (string s in PlannedStages)
            Console.WriteLine($"  {s}");

        Console.WriteLine("validate-config: OK");
    }

    private static async Task<(int passed, string command)> RunFlowAsync(Config cfg, Creds creds)
    {
        int passed = 0;
        string commandOutcome = "SKIP: not reached";

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(cfg.TimeoutSeconds));
        CancellationToken ct = cts.Token;

        await using var c1 = new Client("client1");
        await using var c2 = new Client("client2");

        string roomId = "";
        int createdSlot = -1;
        Protocol? capturedState = null;

        async Task<bool> Stage(string name, Func<Task> body)
        {
            try
            {
                await body();
                Console.WriteLine($"PASS {name}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL {name} ({Describe(ex)})");
                return false;
            }
        }

        try
        {
            if (!await Stage("connect", () => Task.WhenAll(
                    c1.ConnectAsync(cfg.Host, cfg.Port, ct),
                    c2.ConnectAsync(cfg.Host, cfg.Port, ct)))) return (passed, commandOutcome);
            passed++;

            if (!await Stage("login", () => Task.WhenAll(
                    LoginAsync(c1, creds.User1, creds.Password1, ct),
                    LoginAsync(c2, creds.User2, creds.Password2, ct)))) return (passed, commandOutcome);
            passed++;

            if (!await Stage("join-lobby", () => Task.WhenAll(
                    JoinLobbyAsync(c1, ct),
                    JoinLobbyAsync(c2, ct)))) return (passed, commandOutcome);
            passed++;

            if (!await Stage("create-room", async () =>
                {
                    (roomId, createdSlot) = await CreateRoomAsync(c1, ct);
                })) return (passed, commandOutcome);
            passed++;

            if (!await Stage("join-room", async () =>
                {
                    // Sequential to guarantee distinct slots: creator claims the returned slot, peer auto-picks the other.
                    await JoinRoomAsync(c1, roomId, createdSlot, ct);
                    await JoinRoomAsync(c2, roomId, -1, ct);
                })) return (passed, commandOutcome);
            passed++;

            if (!await Stage("ready", async () =>
                {
                    // FACT: REQUEST_READY carries no params (TestClient.ToggleReady).
                    // ASSUMPTION: no correlated RESPONSE is asserted for READY (its response shape is not
                    // defined in the read sources); the authoritative gate is GAME_SET below.
                    await c1.SendAsync(new Protocol(ProtocolType.REQUEST_READY), ct);
                    await c2.SendAsync(new Protocol(ProtocolType.REQUEST_READY), ct);
                })) return (passed, commandOutcome);
            passed++;

            if (!await Stage("game-set", () => Task.WhenAll(
                    c1.WaitForBroadcastAsync(ProtocolType.GAME_SET, ct),
                    c2.WaitForBroadcastAsync(ProtocolType.GAME_SET, ct)))) return (passed, commandOutcome);
            passed++;

            if (!await Stage("game-cl-ready", async () =>
                {
                    // FACT: REQUEST_GAME_CL_READY carries no params (GamePlayer.HandleClientReady).
                    await c1.SendAsync(new Protocol(ProtocolType.REQUEST_GAME_CL_READY), ct);
                    await c2.SendAsync(new Protocol(ProtocolType.REQUEST_GAME_CL_READY), ct);
                })) return (passed, commandOutcome);
            passed++;

            if (!await Stage("game-started", () => Task.WhenAll(
                    c1.WaitForBroadcastAsync(ProtocolType.GAME_STARTED, ct),
                    c2.WaitForBroadcastAsync(ProtocolType.GAME_STARTED, ct)))) return (passed, commandOutcome);
            passed++;

            if (!await Stage("game-state", async () =>
                {
                    Task<Protocol> s1t = c1.WaitForBroadcastAsync(ProtocolType.GAME_STATE, ct);
                    Task<Protocol> s2t = c2.WaitForBroadcastAsync(ProtocolType.GAME_STATE, ct);
                    await Task.WhenAll(s1t, s2t);
                    Protocol s1 = await s1t;
                    Protocol s2 = await s2t;
                    // FACT: GAME_STATE carries "serverTick" (long) and "gameState" (object) (GamePlayer.Async_SendGameState).
                    ValidateGameState(s1, "client1");
                    ValidateGameState(s2, "client2");
                    capturedState = s1;
                })) return (passed, commandOutcome);
            passed++;

            // Optional (non-mandatory) command stage.
            commandOutcome = await CommandStageAsync(c1, capturedState, ct);
            Console.WriteLine($"{(commandOutcome.StartsWith("PASS") ? "PASS" : "SKIP")} submit-command ({commandOutcome})");

            return (passed, commandOutcome);
        }
        finally
        {
            // Deterministic best-effort cleanup; never throws.
            await BestEffortLeaveAsync(c1);
            await BestEffortLeaveAsync(c2);
            Console.WriteLine("INFO cleanup: REQUEST_LEFT_ROOM sent best-effort; sockets closed");
        }
    }

    // ---- stage bodies (parameter names are FACTs verified against server source) ----

    private static async Task LoginAsync(Client c, string user, string password, CancellationToken ct)
    {
        // FACT: ClientSession.Handle_RequestLogin reads "username" and "password"; on success replies
        // RESPONSE(protoId=REQUEST_LOGIN, status=SUCCESS, sessionId=...). Password value is never logged.
        var req = new Protocol(ProtocolType.REQUEST_LOGIN)
            .AddParam("username", user)
            .AddParam("password", password);
        Protocol resp = await c.SendRequestAsync(req, ct);
        AssertSuccess(resp, ProtocolType.REQUEST_LOGIN, $"{c.Name} login");
    }

    private static async Task JoinLobbyAsync(Client c, CancellationToken ct)
    {
        // FACT: ClientSession.Handle_RequestJoinLobby reads "Page" (int).
        var req = new Protocol(ProtocolType.REQUEST_JOIN_LOBBY).AddParam("Page", 0);
        Protocol resp = await c.SendRequestAsync(req, ct);
        AssertSuccess(resp, ProtocolType.REQUEST_JOIN_LOBBY, $"{c.Name} join-lobby");
    }

    private static async Task<(string roomId, int slot)> CreateRoomAsync(Client c, CancellationToken ct)
    {
        // FACT: ClientSession.Handle_RequestCreateRoom reads "roomName","mapId","isPrivate";
        //       response carries "roomId" (string) and "slot" (int = room.NextSlot()).
        var req = new Protocol(ProtocolType.REQUEST_CREATE_ROOM)
            .AddParam("roomName", "e2e-harness")
            .AddParam("mapId", 0)
            .AddParam("isPrivate", true);
        Protocol resp = await c.SendRequestAsync(req, ct);
        AssertSuccess(resp, ProtocolType.REQUEST_CREATE_ROOM, $"{c.Name} create-room");
        string roomId = resp.GetParam<string>("roomId") ?? "";
        int slot = resp.GetParam<int>("slot");
        if (string.IsNullOrEmpty(roomId))
            throw new InvalidOperationException("create-room: empty roomId in response");
        return (roomId, slot);
    }

    private static async Task JoinRoomAsync(Client c, string roomId, int slot, CancellationToken ct)
    {
        // FACT: ClientSession.Handle_RequestJoinRoom reads "userId","roomId","slot" (slot -1 => auto).
        var req = new Protocol(ProtocolType.REQUEST_JOIN_ROOM)
            .AddParam("userId", 0)
            .AddParam("roomId", roomId)
            .AddParam("slot", slot);
        Protocol resp = await c.SendRequestAsync(req, ct);
        AssertSuccess(resp, ProtocolType.REQUEST_JOIN_ROOM, $"{c.Name} join-room slot={slot}");
    }

    private static void ValidateGameState(Protocol gs, string who)
    {
        if (!gs.HasParam("serverTick"))
            throw new InvalidOperationException($"{who}: GAME_STATE missing 'serverTick'");
        if (!gs.HasParam("gameState"))
            throw new InvalidOperationException($"{who}: GAME_STATE missing 'gameState'");
        long tick = gs.GetParam<long>("serverTick");
        if (tick < 0)
            throw new InvalidOperationException($"{who}: GAME_STATE negative serverTick {tick}");
    }

    private static async Task<string> CommandStageAsync(Client c, Protocol? capturedState, CancellationToken ct)
    {
        if (capturedState == null)
            return "SKIP: no GAME_STATE captured";

        if (!TryFindMoveTarget(capturedState, out int fleetId, out int planetId))
            return "SKIP: no owned fleet present in received GameState " +
                   "(fleets are produced in-game; a valid produce/move target is not derivable from GAME_STATE) — no false command sent";

        long tick = capturedState.HasParam("serverTick") ? capturedState.GetParam<long>("serverTick") : 0L;
        // FACT: GamePlayer.HandleSubmitCommand reads "commandType"; MoveFleetCommand reads
        //       "tick","target_fleet","target_planet"; GameCommandType.MoveFleet == 2 (CommonLib.Commands).
        // FACT: SUBMIT_COMMAND receives no RESPONSE from the server -> one-way best-effort send.
        var cmd = new Protocol(ProtocolType.SUBMIT_COMMAND)
            .AddParam("commandType", (int)GameCommandType.MoveFleet)
            .AddParam("tick", tick)
            .AddParam("target_fleet", fleetId)
            .AddParam("target_planet", planetId);
        await c.SendAsync(cmd, ct);
        return $"PASS: MoveFleet target_fleet={fleetId} target_planet={planetId} (best-effort, no ack)";
    }

    private static bool TryFindMoveTarget(Protocol gameState, out int fleetId, out int planetId)
    {
        fleetId = 0;
        planetId = 0;
        // Local read-only view for target extraction (NOT the server DTO; only the fields we read).
        StateView? view = gameState.GetObject<StateView>("gameState");
        if (view?.planets == null || view.planets.Length == 0)
            return false;
        planetId = view.planets[0].planetId;
        if (view.players == null)
            return false;
        foreach (PlayerView? pl in view.players)
        {
            if (pl?.fleets != null && pl.fleets.Length > 0)
            {
                fleetId = (int)pl.fleets[0].fleetId;
                return true;
            }
        }

        return false;
    }

    private static async Task BestEffortLeaveAsync(Client c)
    {
        try
        {
            using var lc = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            // FACT: REQUEST_LEFT_ROOM carries no params (TestClient.LeaveRoom).
            await c.SendAsync(new Protocol(ProtocolType.REQUEST_LEFT_ROOM), lc.Token);
        }
        catch
        {
            // best-effort only
        }
    }

    private static void AssertSuccess(Protocol resp, int expectedProtoId, string what)
    {
        int protoId = resp.GetParam<int>("protoId");
        if (protoId != expectedProtoId)
            throw new InvalidOperationException($"{what}: RESPONSE protoId {protoId} != expected {expectedProtoId}");
        byte status = resp.GetParam<byte>("status"); // FACT: Response stores status as byte
        if (status != (byte)StateCode.SUCCESS)
            throw new InvalidOperationException($"{what}: status {(StateCode)status}");
    }

    private static string Describe(Exception ex)
    {
        string msg = ex is OperationCanceledException
            ? "timeout/canceled"
            : $"{ex.GetType().Name}: {ex.Message}";
        // Keep terse and never dump full protocol payloads.
        msg = msg.Replace("\r", " ").Replace("\n", " ");
        if (msg.Length > 160)
            msg = msg.Substring(0, 160) + "...";
        return msg;
    }

    // Minimal local views used only to read move targets from a GAME_STATE snapshot.
    private sealed class StateView
    {
        public PlayerView[]? players { get; set; }
        public PlanetView[]? planets { get; set; }
    }

    private sealed class PlayerView
    {
        public int id { get; set; }
        public FleetView[]? fleets { get; set; }
    }

    private sealed class FleetView
    {
        public long fleetId { get; set; }
        public int ownerId { get; set; }
    }

    private sealed class PlanetView
    {
        public int planetId { get; set; }
    }
}

/// <summary>Parsed command-line configuration.</summary>
internal sealed class Config
{
    public string Host { get; private set; } = "127.0.0.1";
    public int Port { get; private set; } = 9000;
    public int TimeoutSeconds { get; private set; } = 60;
    public bool ValidateConfig { get; private set; }

    public static Config Parse(string[] args)
    {
        var c = new Config();
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            string key = a;
            string? inlineVal = null;
            int eq = a.IndexOf('=');
            if (a.StartsWith("--", StringComparison.Ordinal) && eq > 0)
            {
                key = a.Substring(0, eq);
                inlineVal = a.Substring(eq + 1);
            }

            switch (key)
            {
                case "--validate-config":
                    c.ValidateConfig = true;
                    break;
                case "--host":
                    c.Host = inlineVal ?? Next(args, ref i, key);
                    break;
                case "--port":
                    c.Port = ParseInt(inlineVal ?? Next(args, ref i, key), key);
                    break;
                case "--timeout-seconds":
                    c.TimeoutSeconds = ParseInt(inlineVal ?? Next(args, ref i, key), key);
                    break;
                default:
                    throw new ArgumentException($"unknown argument '{a}'");
            }
        }

        if (string.IsNullOrWhiteSpace(c.Host))
            throw new ArgumentException("--host must be non-empty");
        if (c.Port < 1 || c.Port > 65535)
            throw new ArgumentException($"--port must be within 1..65535 (got {c.Port})");
        if (c.TimeoutSeconds < 1 || c.TimeoutSeconds > 3600)
            throw new ArgumentException($"--timeout-seconds must be a positive value within 1..3600 (got {c.TimeoutSeconds})");
        return c;
    }

    private static string Next(string[] args, ref int i, string key)
    {
        if (i + 1 >= args.Length)
            throw new ArgumentException($"{key} requires a value");
        return args[++i];
    }

    private static int ParseInt(string s, string key)
    {
        if (!int.TryParse(s, out int v))
            throw new ArgumentException($"{key} requires an integer (got '{s}')");
        return v;
    }
}

/// <summary>Credentials sourced only from environment variables; values are never logged.</summary>
internal sealed class Creds
{
    public static readonly string[] EnvNames = { "E2E_USER1", "E2E_PASSWORD1", "E2E_USER2", "E2E_PASSWORD2" };

    public string User1 { get; private set; } = "";
    public string Password1 { get; private set; } = "";
    public string User2 { get; private set; } = "";
    public string Password2 { get; private set; } = "";

    public static (Creds creds, List<string> missing) FromEnv()
    {
        var c = new Creds();
        var missing = new List<string>();
        c.User1 = Require("E2E_USER1", missing);
        c.Password1 = Require("E2E_PASSWORD1", missing);
        c.User2 = Require("E2E_USER2", missing);
        c.Password2 = Require("E2E_PASSWORD2", missing);
        return (c, missing);
    }

    private static string Require(string name, List<string> missing)
    {
        string? v = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(v))
        {
            missing.Add(name);
            return "";
        }

        return v;
    }
}

/// <summary>
/// Lightweight TCP client over CommonLib's Protocol framing:
/// concurrent receive loop, protoId RESPONSE correlation, unsolicited-broadcast queue with waiters,
/// exact 18-byte-header/bounded-max frame validation, cancellation, and deterministic disposal.
/// </summary>
internal sealed class Client : IAsyncDisposable
{
    private const int HeaderSize = 18;         // FACT: CommonLib Protocol header size
    private const int MaxFrame = 1024 * 1024;  // FACT: BaseServer ClientSession bounds frames to 1 MB

    public string Name { get; }

    private TcpClient? _tcp;
    private NetworkStream? _stream;
    private Task? _recvLoop;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly ConcurrentDictionary<int, TaskCompletionSource<Protocol>> _pending = new();
    private readonly Channel<Protocol> _broadcasts =
        Channel.CreateUnbounded<Protocol>(new UnboundedChannelOptions { SingleReader = false, SingleWriter = true });
    private readonly List<Protocol> _stash = new();
    private readonly object _stashLock = new();

    public Client(string name)
    {
        Name = name;
    }

    public async Task ConnectAsync(string host, int port, CancellationToken ct)
    {
        _tcp = new TcpClient { NoDelay = true };
        await _tcp.ConnectAsync(host, port, ct);
        _stream = _tcp.GetStream();
        _recvLoop = Task.Run(() => ReceiveLoopAsync(ct));
    }

    public async Task SendAsync(Protocol p, CancellationToken ct)
    {
        if (_stream == null)
            throw new InvalidOperationException($"{Name}: not connected");
        byte[] data = p.Serialize(); // FACT: CommonLib framing; size field includes the header
        await _sendLock.WaitAsync(ct);
        try
        {
            await _stream.WriteAsync(data, ct);
            await _stream.FlushAsync(ct);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task<Protocol> SendRequestAsync(Protocol req, CancellationToken ct)
    {
        // FACT: server Response sets protoId == the request's Type, so we correlate by request opcode.
        var tcs = new TaskCompletionSource<Protocol>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pending.TryAdd(req.Type, tcs))
            throw new InvalidOperationException($"{Name}: a request for protoId {req.Type} is already pending");
        try
        {
            await SendAsync(req, ct);
            using CancellationTokenRegistration reg = ct.Register(
                static s => ((TaskCompletionSource<Protocol>)s!).TrySetCanceled(), tcs);
            return await tcs.Task;
        }
        finally
        {
            _pending.TryRemove(req.Type, out _);
        }
    }

    public async Task<Protocol> WaitForBroadcastAsync(int opcode, CancellationToken ct)
    {
        lock (_stashLock)
        {
            for (int i = 0; i < _stash.Count; i++)
            {
                if (_stash[i].Type == opcode)
                {
                    Protocol m = _stash[i];
                    _stash.RemoveAt(i);
                    return m;
                }
            }
        }

        while (true)
        {
            Protocol p;
            try
            {
                p = await _broadcasts.Reader.ReadAsync(ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new IOException($"{Name}: stream ended before opcode {opcode}", ex);
            }

            if (p.Type == opcode)
                return p;
            lock (_stashLock)
            {
                _stash.Add(p);
            }
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var header = new byte[4];
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (!await ReadExactAsync(header, 0, 4, ct))
                    break; // EOF
                int total = BitConverter.ToInt32(header, 0);
                if (total < HeaderSize || total > MaxFrame) // exact frame-size validation
                    throw new InvalidDataException($"{Name}: invalid frame size {total} (allowed {HeaderSize}..{MaxFrame})");
                var buf = new byte[total];
                Array.Copy(header, 0, buf, 0, 4);
                if (!await ReadExactAsync(buf, 4, total - 4, ct))
                    break; // truncated
                Protocol p = Protocol.Deserialize(buf);
                Dispatch(p);
            }

            Fault(new IOException($"{Name}: connection closed"));
        }
        catch (OperationCanceledException)
        {
            Fault(new OperationCanceledException());
        }
        catch (Exception ex)
        {
            Fault(ex);
        }
    }

    private void Dispatch(Protocol p)
    {
        if (p.Type == ProtocolType.RESPONSE)
        {
            int protoId = p.GetParam<int>("protoId");
            if (_pending.TryGetValue(protoId, out TaskCompletionSource<Protocol>? tcs))
                tcs.TrySetResult(p);
            return; // uncorrelated RESPONSE is ignored
        }

        _broadcasts.Writer.TryWrite(p); // unsolicited broadcast (GAME_SET/STARTED/STATE, USER_JOINED, ...)
    }

    private void Fault(Exception ex)
    {
        foreach (KeyValuePair<int, TaskCompletionSource<Protocol>> kv in _pending)
            kv.Value.TrySetException(ex);
        _broadcasts.Writer.TryComplete(ex);
    }

    private async Task<bool> ReadExactAsync(byte[] buf, int offset, int count, CancellationToken ct)
    {
        int read = 0;
        while (read < count)
        {
            int n = await _stream!.ReadAsync(buf.AsMemory(offset + read, count - read), ct);
            if (n == 0)
                return false;
            read += n;
        }

        return true;
    }

    public async ValueTask DisposeAsync()
    {
        try { _stream?.Close(); } catch { /* ignore */ }
        try { _tcp?.Close(); } catch { /* ignore */ }
        if (_recvLoop != null)
        {
            try { await _recvLoop; } catch { /* ignore */ }
        }

        _sendLock.Dispose();
    }
}
