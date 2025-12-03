using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ChatClientWPF.Utils;
using static ProtocolHandler;

namespace CommonLib
{
    /// <summary>
    /// 네트워크 연결 상태 및 설정을 관리하는 클래스
    /// </summary>
    public class NetworkConfig
    {
        public bool IsTryGoToTitle { get; set; } = false;
        public bool IsTryApplicationQuit { get; set; } = false;
        public bool IsReconnecting { get; set; } = false;
        public bool IsServerAllReady { get; set; } = false;
        public bool IsConnected { get; set; } = false;
        public DateTime LastActivityTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 네트워크 통신 및 프로토콜 처리를 담당하는 핵심 클래스
    /// </summary>
    public class NetworkManager
    {
        // --- 네트워크 연결 관련 ---
        private TcpClient tcpClient;
        private NetworkStream stream;
        private CancellationTokenSource cts;

        // 전송 동기화를 위한 세마포어 (Thread-Safety)
        private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);

        // WPF 메인 스레드 동기화 컨텍스트 (스레드 안전성)
        private SynchronizationContext? wpfSyncContext;

        // --- 상태 관리 ---
        public NetworkConfig Config { get; private set; } = new NetworkConfig();

        // --- 프로토콜 핸들러 관리 ---
        private Dictionary<int, ProtocolHandlerDelegate> protocolHandlers = new Dictionary<int, ProtocolHandlerDelegate>();
        private readonly object handlersLock = new object();

        // --- Request-Response 패턴 ---
        private Dictionary<int, ResponseAwaiter> pendingResponses = new Dictionary<int, ResponseAwaiter>();
        private readonly object pendingResponsesLock = new object();

        // --- 타이머 관리 ---
        private Timer heartbeatTimer;
        private Timer timeoutCheckTimer;

        // --- 상수 ---
        private const int HEARTBEAT_INTERVAL_MS = 5000;  // 5초 (10초에서 변경)
        private const int TIMEOUT_SECONDS = 60;          // 60초
        private const int TIMEOUT_CHECK_INTERVAL = 5000; // 5초
        private const int RESPONSE_TIMEOUT_MS = 30000;   // 30초

        // --- 이벤트 ---
        public event Action<bool, string> ConnectionChanged;
        public event Action<string> ErrorOccurred;

        /// <summary>
        /// 서버에 연결
        /// </summary>
        public async Task ConnectAsync(string ip, int port)
        {
            if (Config.IsConnected)
                return;

            try
            {
                Logger.Log($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 서버 연결 시도: {ip}:{port}");

                // WPF 메인 스레드 SynchronizationContext 캡처
                if (wpfSyncContext == null)
                {
                    wpfSyncContext = SynchronizationContext.Current;
                    if (wpfSyncContext != null)
                    {
                        Logger.Log($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] WPF 메인 스레드 컨텍스트 캡처 완료");
                    }
                    else
                    {
                        Logger.LogWarning($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] ⚠️ SynchronizationContext를 캡처하지 못했습니다. 메인 스레드에서 호출되었는지 확인하세요.");
                    }
                }

                // 기존 연결 정리
                Cleanup();

                tcpClient = new TcpClient();
                cts = new CancellationTokenSource();

                // Nagle 알고리즘 비활성화 - 즉시 전송!
                tcpClient.NoDelay = true;

                //  송신 버퍼 크기 최소화 (선택사항)
                tcpClient.SendBufferSize = 1024;
                await tcpClient.ConnectAsync(ip, port);

                stream = tcpClient.GetStream();
                Config.IsConnected = true;
                Config.IsServerAllReady = true; // ✅ 연결 즉시 서버 준비 완료로 설정
                UpdateLastActivity();

                Logger.Log($"[{DateTime.Now:HH:mm:ss.fff}] <color=green>[NetworkManager] 서버 연결 성공: {ip}:{port}</color>");

                StartTimers();

                // 수신 루프 시작 (Fire-and-forget)
                _ = Task.Run(() => ReceiveLoop(cts.Token));

                ConnectionChanged?.Invoke(true, "Connected successfully");
            }
            catch (Exception e)
            {
                Logger.LogError($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 연결 오류: {e.Message}");
                Cleanup();
                ConnectionChanged?.Invoke(false, e.Message);
                throw;
            }
        }

        /// <summary>
        /// 연결 종료
        /// </summary>
        public void Disconnect()
        {
            if (!Config.IsConnected)
                return;

            Logger.Log($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 연결 종료 요청됨");

            // 먼저 연결 상태를 false로 변경하여 추가적인 전송/수신을 막음
            Config.IsConnected = false;

            // 취소 토큰으로 수신 루프 등 중단 요청
            try
            {
                cts?.Cancel();
            }
            catch (ObjectDisposedException) { }

            Cleanup();
            ConnectionChanged?.Invoke(false, "Disconnected");
        }

        /// <summary>
        /// 프로토콜 핸들러 등록
        /// </summary>
        public void RegisterHandler(int protocolType, ProtocolHandlerDelegate handler)
        {
            lock (handlersLock)
            {
                protocolHandlers[protocolType] = handler;
            }
        }

        /// <summary>
        /// 프로토콜 핸들러 제거
        /// </summary>
        public void UnregisterHandler(int protocolType)
        {
            lock (handlersLock)
            {
                protocolHandlers.Remove(protocolType);
            }
        }

        /// <summary>
        /// 비동기 프로토콜 전송 및 응답 대기 (통합 구조)
        /// </summary>
        public async Task<NetworkResponse> SendAsync(Protocol protocol)
        {
            // 상태 검증
            ValidateState(protocol);

            // 재접속 및 연결 상태 확인
            await EnsureConnection(protocol);

            // 서버 준비 상태 대기
            await WaitForServerReady(protocol);

            return await SendRequest(protocol);
        }

        /// <summary>
        /// 단방향 프로토콜 전송 (응답 불필요) - 통합 구조
        /// </summary>
        public async Task SendOneWayAsync(Protocol protocol)
        {
            ValidateState(protocol);

            if (!Config.IsConnected || stream == null)
                throw new InvalidOperationException("Not connected to server");

            await SendRawData(protocol.Serialize());
        }

        /// <summary>
        /// 마지막 활동 시간 갱신
        /// </summary>
        public void UpdateLastActivity()
        {
            Config.LastActivityTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 상태 검증
        /// </summary>
        private void ValidateState(Protocol protocol)
        {
            if (Config.IsTryGoToTitle || Config.IsTryApplicationQuit)
                throw new NetworkException("Application is shutting down or going to title");

            if (protocol == null)
                throw new ArgumentNullException(nameof(protocol));
        }

        /// <summary>
        /// 연결 상태 확인 및 재접속
        /// </summary>
        private async Task EnsureConnection(Protocol protocol)
        {
            // 재접속 중
            if (Config.IsReconnecting)
            {
                if (!IsReconnectingSend(protocol))
                {
                    throw new NetworkException("Reconnecting in progress");
                }
                await WaitWhileReconnecting();
            }

            // 연결 끊어짐 - 재접속
            if (!Config.IsConnected)
            {
                if (CanReconnect())
                {
                    await Reconnect();
                }
                else
                {
                    throw new NetworkException("Connection lost and cannot reconnect");
                }
            }
        }

        /// <summary>
        /// 서버 준비 상태 대기
        /// </summary>
        private async Task WaitForServerReady(Protocol protocol)
        {
            if (!IsEnterProtocol(protocol))
            {
                if (!Config.IsServerAllReady)
                {
                    Logger.LogWarning($"[NetworkManager] ⚠️ 서버 준비 대기 시작!");
                    await WaitUntilServerReady();
                    Logger.Log($"[NetworkManager] 서버 준비 완료");
                }
            }
        }

        /// <summary>
        /// 실제 요청 전송 및 응답 대기 (TaskCompletionSource 사용)
        /// </summary>
        private async Task<NetworkResponse> SendRequest(Protocol protocol)
        {
            ResponseAwaiter awaiter = new ResponseAwaiter(new Dictionary<string, object>());
            int protocolId = protocol.Type;

            lock (pendingResponsesLock)
            {
                pendingResponses[protocolId] = awaiter;
            }

            if (IsShowLog(protocol))
            {
                string paramStr = FormatProtocolParams(protocol, 2);
                if (!string.IsNullOrEmpty(paramStr))
                {
                    Logger.Log($"[NetworkManager] <color=yellow>▲ 클라 => 서버: {protocol.Type.ToString()}\n  {paramStr}</color>");
                }
                else
                {
                    Logger.Log($"[NetworkManager] <color=yellow>▲ 클라 => 서버: {protocol.Type.ToString()}</color>");
                }
            }

            // 요청 전송
            try
            {
                await SendRawData(protocol.Serialize());
            }
            catch (Exception ex)
            {
                lock (pendingResponsesLock)
                {
                    pendingResponses.Remove(protocolId);
                }
                throw;
            }

            // ✅ TaskCompletionSource 직접 await - Unity 프레임 독립적!
            try
            {
                using (var timeoutCts = new CancellationTokenSource(RESPONSE_TIMEOUT_MS))
                {
                    var completedTask = await Task.WhenAny(
                        awaiter.Task,
                        Task.Delay(RESPONSE_TIMEOUT_MS, timeoutCts.Token)
                    );

                    if (completedTask == awaiter.Task)
                    {
                        timeoutCts.Cancel();
                        return await awaiter.Task;
                    }
                    else
                    {
                        Logger.LogError($"[NetworkManager] ❌ 타임아웃: {protocol.Type.ToString()}");
                        lock (pendingResponsesLock)
                        {
                            pendingResponses.Remove(protocolId);
                        }
                        throw new TimeoutException($"Protocol {protocol.Type.ToString()} response timeout after {RESPONSE_TIMEOUT_MS}ms");
                    }
                }
            }
            catch (Exception)
            {
                lock (pendingResponsesLock)
                {
                    pendingResponses.Remove(protocolId);
                }
                throw;
            }
        }

        /// <summary>
        /// 원시 데이터 전송 (Thread-Safe)
        /// </summary>
        private async Task SendRawData(byte[] data)
        {
            if (!Config.IsConnected || stream == null)
                return;

            await _sendSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!Config.IsConnected || stream == null)
                    return;

                if (data.Length >= 4)
                {
                    int length = BitConverter.ToInt32(data, 0);
                    string hex = BitConverter.ToString(data, 0, 4);
                    Logger.Log($"[NetworkManager] 전송 데이터: TotalBytes={data.Length}, HeaderLength={length}, HeaderHex={hex}");
                }
                await stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                await stream.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.LogError($"[NetworkManager] 전송 오류: {e.Message}");
                Disconnect();
                throw;
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        /// <summary>
        /// 수신 루프
        /// </summary>
        private async Task ReceiveLoop(CancellationToken cancellationToken)
        {
            Logger.Log($"[{DateTime.Now:HH:mm:ss.fff}] [ReceiveLoop] 시작!");
            byte[] lengthBuffer = new byte[4];

            try
            {
                while (Config.IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    if (stream == null || !stream.CanRead)
                        break;

                    // 1. 메시지 길이 읽기 (4바이트)
                    int bytesRead = 0;
                    try
                    {
                        bytesRead = await stream.ReadAsync(lengthBuffer, 0, 4, cancellationToken);
                    }
                    catch (Exception) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        Logger.Log($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 서버 연결이 종료되었습니다. (Read 0 bytes)");
                        break;
                    }

                    if (bytesRead < 4)
                    {
                        int remaining = 4 - bytesRead;
                        while (remaining > 0)
                        {
                            int read = await stream.ReadAsync(lengthBuffer, 4 - remaining, remaining, cancellationToken);
                            if (read == 0) throw new EndOfStreamException("Connection closed while reading length");
                            remaining -= read;
                        }
                    }

                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                    // 유효성 검사
                    if (messageLength <= 0 || messageLength > 1024 * 1024)
                    {
                        Logger.LogError($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 잘못된 메시지 길이: {messageLength}");
                        break;
                    }

                    // 2. 전체 메시지 읽기
                    byte[] messageBuffer = new byte[messageLength];
                    Array.Copy(lengthBuffer, 0, messageBuffer, 0, 4);

                    int totalRead = 4;
                    while (totalRead < messageLength)
                    {
                        int toRead = messageLength - totalRead;
                        int read = await stream.ReadAsync(messageBuffer, totalRead, toRead, cancellationToken);
                        if (read == 0)
                            throw new EndOfStreamException("Connection closed while reading body");
                        totalRead += read;
                    }

                    // 3. 역직렬화 및 처리
                    try
                    {   
                        Protocol protocol = Protocol.Deserialize(messageBuffer);
                        if (protocol != null)
                        {
                            await HandleIncomingProtocol(protocol);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"[NetworkManager] 프로토콜 처리 오류: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Log($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 수신 루프 취소됨");
            }
            catch (Exception e)
            {
                if (Config.IsConnected)
                {
                    Logger.LogError($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 수신 루프 치명적 오류: {e.Message}");
                    ErrorOccurred?.Invoke($"Receive error: {e.Message}");
                    Disconnect();
                }
            }
            finally
            {
                Logger.Log($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 수신 루프 종료");
                Cleanup();
            }
        }

        /// <summary>
        /// 들어오는 프로토콜 처리
        /// </summary>
        private async Task HandleIncomingProtocol(Protocol protocol)
        {
            // 응답 매칭 확인
            if (protocol.Type == (int)ProtocolType.RESPONSE)
            {
                // protoId 확인
                if (!protocol.HasParam("protoId"))
                {
                    Logger.LogError($"[NetworkManager] ⚠️ Response에 protoId 없음!");
                    return;
                }

                int protocolId = protocol.GetParam<int>("protoId");

                if (IsShowLog(new Protocol(protocolId)))
                {
                    string paramStr = FormatProtocolParams(protocol, 2);
                    if (!string.IsNullOrEmpty(paramStr))
                    {
                        Logger.Log($"[NetworkManager] <color=cyan>▼ 클라 <= 서버: {protocol.Type.ToString()} (응답)\n  {paramStr}</color>");
                    }
                    else
                    {
                        Logger.Log($"[NetworkManager] <color=cyan>▼ 클라 <= 서버: {protocol.Type.ToString()} (응답)</color>");
                    }
                }

                lock (pendingResponsesLock)
                {
                    if (pendingResponses.TryGetValue(protocolId, out var awaiter))
                    {
                        pendingResponses.Remove(protocolId);

                        var response = new NetworkResponse(protocol, awaiter.sendParam);
                        awaiter.Complete(response, null);
                        return;
                    }
                    else
                    {
                        Logger.LogError($"[NetworkManager] ⚠️ 응답 매칭 실패: {protocol.Type.ToString()}");
                    }
                }
            }

            // 등록된 핸들러 실행
            ProtocolHandlerDelegate handler;
            lock (handlersLock)
            {
                protocolHandlers.TryGetValue(protocol.Type, out handler);
            }

            if (handler != null)
            {
                if (IsShowLog(protocol))
                {
                    string paramStr = FormatProtocolParams(protocol, 2);
                    if (!string.IsNullOrEmpty(paramStr))
                    {
                        Logger.Log($"[NetworkManager] <color=cyan>▼ 클라 <= 서버: {protocol.Type.ToString()}\n  {paramStr}</color>");
                    }
                    else
                    {
                        Logger.Log($"[NetworkManager] <color=cyan>▼ 클라 <= 서버: {protocol.Type.ToString()}</color>");
                    }
                }

                // WPF 메인 스레드로 마샬링
                if (wpfSyncContext != null)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    wpfSyncContext.Post(async _ =>
                    {
                        try
                        {
                            await handler(protocol);
                            tcs.SetResult(true);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError($"[NetworkManager] 프로토콜 핸들러 오류: {e.Message}");
                            Logger.LogError($"[NetworkManager] Stack Trace: {e.StackTrace}");
                            tcs.SetException(e);
                        }
                    }, null);
                    await tcs.Task;
                }
                else
                {
                    // SynchronizationContext가 없으면 직접 실행 (폴백)
                    Logger.LogWarning($"[NetworkManager] ⚠️ SynchronizationContext 없음, 백그라운드 스레드에서 직접 실행");
                    try
                    {
                        await handler(protocol);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 프로토콜 핸들러 오류: {e.Message}");
                        Logger.LogError($"[{DateTime.Now:HH:mm:ss.fff}] Stack Trace: {e.StackTrace}");
                    }
                }
            }
            else
            {
                Logger.LogWarning($"[{DateTime.Now:HH:mm:ss.fff}] <color=yellow>⚠️ 등록된 핸들러 없음: Protocol {protocol.Type}</color>");
            }
        }

        /// <summary>
        /// 타이머 시작
        /// </summary>
        private void StartTimers()
        {
            StopTimers();
            heartbeatTimer = new Timer(SendHeartbeat, null, HEARTBEAT_INTERVAL_MS, HEARTBEAT_INTERVAL_MS);
            timeoutCheckTimer = new Timer(CheckTimeout, null, TIMEOUT_CHECK_INTERVAL, TIMEOUT_CHECK_INTERVAL);
        }

        private void StopTimers()
        {
            heartbeatTimer?.Dispose();
            heartbeatTimer = null;
            timeoutCheckTimer?.Dispose();
            timeoutCheckTimer = null;
        }

        /// <summary>
        /// 하트비트 전송
        /// </summary>
        private void SendHeartbeat(object state)
        {
            if (!Config.IsConnected)
                return;

            try
            {
                var protocol = new Protocol((int)ProtocolType.HEARTBEAT);
                Task.Run(async () =>
                {
                    try
                    {
                        await SendRawData(protocol.Serialize());
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 하트비트 전송 실패: {ex.Message}");
                    }
                });
            }
            catch (Exception e)
            {
                Logger.LogError($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 하트비트 생성 오류: {e.Message}");
            }
        }

        /// <summary>
        /// 타임아웃 체크
        /// </summary>
        private void CheckTimeout(object state)
        {
            if (!Config.IsConnected)
                return;

            if ((DateTime.UtcNow - Config.LastActivityTime).TotalSeconds > TIMEOUT_SECONDS)
            {
                Logger.LogWarning($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 타임아웃 감지. 연결 종료.");
                Disconnect();
            }
        }

        /// <summary>
        /// 재접속
        /// </summary>
        private async Task Reconnect()
        {
            Config.IsReconnecting = true;
            try
            {
                Logger.Log($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 재접속 시도...");
                Cleanup();
                await Task.Delay(1000);
                Logger.Log($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 재접속 완료 (로직 미구현)");
            }
            finally
            {
                Config.IsReconnecting = false;
            }
        }

        /// <summary>
        /// 프로토콜 파라미터를 문자열로 포맷 (상세 출력)
        /// </summary>
        private string FormatProtocolParams(Protocol protocol, int indent = 0)
        {
            if (protocol == null)
                return "";

            var parameters = protocol.GetParams();
            if (parameters == null || parameters.Count == 0)
                return "";

            var sb = new System.Text.StringBuilder();
            string indentStr = new string(' ', indent);
            bool isFirst = true;

            foreach (var param in parameters)
            {
                if (!isFirst)
                    sb.Append("\n" + indentStr);
                isFirst = false;

                string value = FormatValue(param.Value, indent + 2);
                sb.Append($"{param.Key} = {value}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 값을 재귀적으로 포맷
        /// </summary>
        private string FormatValue(object value, int indent = 0)
        {
            if (value == null)
                return "null";

            string indentStr = new string(' ', indent);

            // 문자열
            if (value is string str)
                return $"\"{str}\"";

            // 기본 타입 (숫자, bool 등)
            if (value.GetType().IsPrimitive || value is decimal)
                return value.ToString();

            // Newtonsoft.Json.Linq.JArray 처리
            if (value is Newtonsoft.Json.Linq.JArray jArray)
            {
                if (jArray.Count == 0)
                    return "[]";

                var sb = new System.Text.StringBuilder();
                sb.Append($"[\n{indentStr}  ");
                for (int i = 0; i < jArray.Count; i++)
                {
                    if (i > 0)
                        sb.Append($",\n{indentStr}  ");
                    sb.Append(FormatValue(jArray[i], indent + 2));
                }
                sb.Append($"\n{indentStr}]");
                return sb.ToString();
            }

            // Newtonsoft.Json.Linq.JObject 처리
            if (value is Newtonsoft.Json.Linq.JObject jObject)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append($"{{\n{indentStr}  ");
                bool isFirst = true;
                foreach (var prop in jObject.Properties())
                {
                    if (!isFirst)
                        sb.Append($",\n{indentStr}  ");
                    isFirst = false;
                    sb.Append($"{prop.Name} = {FormatValue(prop.Value, indent + 2)}");
                }
                sb.Append($"\n{indentStr}}}");
                return sb.ToString();
            }

            // 배열
            if (value is System.Array array)
            {
                if (array.Length == 0)
                    return "[]";

                var sb = new System.Text.StringBuilder();
                sb.Append($"[\n{indentStr}  ");
                for (int i = 0; i < array.Length; i++)
                {
                    if (i > 0)
                        sb.Append($",\n{indentStr}  ");
                    sb.Append(FormatValue(array.GetValue(i), indent + 2));
                }
                sb.Append($"\n{indentStr}]");
                return sb.ToString();
            }

            // 일반 객체 (JSON 직렬화)
            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented);
                // 들여쓰기 조정
                if (indent > 0)
                {
                    var lines = json.Split('\n');
                    for (int i = 1; i < lines.Length; i++)
                    {
                        lines[i] = indentStr + lines[i];
                    }
                    return string.Join("\n", lines);
                }
                return json;
            }
            catch
            {
                return value.ToString();
            }
        }

        /// <summary>
        /// 정리 작업 (Idempotent)
        /// </summary>
        private void Cleanup()
        {
            // 대기 중인 모든 응답 취소
            lock (pendingResponsesLock)
            {
                if (pendingResponses.Count > 0)
                {
                    foreach (var awaiter in pendingResponses.Values)
                    {
                        awaiter.Complete(new NetworkException("Connection closed"));
                    }
                    pendingResponses.Clear();
                }
            }

            StopTimers();

            try { stream?.Close(); } catch { }
            stream = null;

            try { tcpClient?.Close(); } catch { }
            tcpClient = null;

            try { cts?.Dispose(); } catch { }
            cts = null;

            Config.IsConnected = false;

            Logger.Log($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 리소스 정리 완료");
        }

        // --- 유틸리티 메서드들 ---

        private bool CanReconnect()
        {
            return !Config.IsTryGoToTitle && !Config.IsTryApplicationQuit;
        }

        private async Task WaitWhileReconnecting()
        {
            while (Config.IsReconnecting)
            {
                await Task.Delay(100);
            }
        }

        private async Task WaitUntilServerReady()
        {
            int waitCount = 0;
            while (!Config.IsServerAllReady)
            {
                await Task.Delay(100);
                waitCount++;

                if (waitCount % 10 == 0) // 1초마다
                {
                    Logger.LogWarning($"[{DateTime.Now:HH:mm:ss.fff}] 서버 준비 대기 중... ({waitCount * 100}ms)");
                }

                if (waitCount > 300) // 30초 타임아웃
                {
                    Logger.LogError($"[{DateTime.Now:HH:mm:ss.fff}] 서버 준비 대기 타임아웃!");
                    throw new TimeoutException("Server ready timeout");
                }
            }
        }

        private bool IsShowLog(Protocol protocol)
        {
            return protocol.Type != (int)ProtocolType.HEARTBEAT && protocol.Type != (int)ProtocolType.HEARTBEAT_ACK;
        }

        private bool IsReconnectingSend(Protocol protocol)
        {
            return protocol.Type == (int)ProtocolType.HEARTBEAT;
        }

        private bool IsEnterProtocol(Protocol protocol)
        {
            // ✅ 로그인(10000)도 EnterProtocol로 인정
            return protocol.Type == 10000 || protocol.Type == 10001 || protocol.Type == 10002;
        }
    }

    /// <summary>
    /// 응답 대기자 (TaskCompletionSource 사용)
    /// </summary>
    public class ResponseAwaiter
    {
        private TaskCompletionSource<NetworkResponse> tcs = new TaskCompletionSource<NetworkResponse>();
        public readonly Dictionary<string, object> sendParam;

        public ResponseAwaiter(Dictionary<string, object> sendParam)
        {
            this.sendParam = sendParam ?? new Dictionary<string, object>();
        }

        public Task<NetworkResponse> Task => tcs.Task;

        public void Complete(NetworkResponse result, Exception exception)
        {
            if (exception != null)
            {
                tcs.TrySetException(exception);
            }
            else if (result != null)
            {
                tcs.TrySetResult(result);
            }
            else
            {
                tcs.TrySetException(new InvalidOperationException("Response is null"));
            }
        }

        public void Complete(Exception exception)
        {
            tcs.TrySetException(exception ?? new Exception("Unknown error"));
        }
    }

    /// <summary>
    /// 네트워크 예외
    /// </summary>
    public class NetworkException : Exception
    {
        public NetworkException() : base("Network error occurred") { }
        public NetworkException(string message) : base(message) { }
        public NetworkException(string message, Exception innerException) : base(message, innerException) { }
    }
}
