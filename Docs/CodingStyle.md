# 코딩 스타일 가이드

## 📋 목차
1. [코드 블록 스타일](#코드-블록-스타일)
2. [네이밍 규칙](#네이밍-규칙)
3. [코드 정리](#코드-정리)
4. [예시](#예시)

---

## 코드 블록 스타일

### BSD 스타일 사용
```csharp
// ✅ 올바른 예시 (BSD 스타일)
void DoSomething()
{
    if (condition)
    {
        // 코드
    }
}

// ❌ 잘못된 예시 (1줄 코드 블록 - 지양)
if (condition) return;

// ✅ 올바른 예시 (1줄도 중괄호 사용)
if (condition)
{
    return;
}

// ✅ 올바른 예시 (바로 밑에 붙히기.)
if (condition)
    return;
```

### 코드 정리
- Visual Studio 기준: **Ctrl + K, Ctrl + D** (전체 문서 정리)
- 커밋 전 **반드시** 코드 정리 실행

---

## 네이밍 규칙

### 1. 클래스명
- **PascalCase** (대문자로 시작)
- 예시: `ClientSession`, `GameRoom`, `RoomManager`

```csharp
public class ClientSession { }
public class GameRoom { }
```

### 2. 함수명
- **PascalCase** (대문자로 시작)
- 동사로 시작 권장

```csharp
void GetData() { }
void ProcessMessage() { }
async Task SendAsync() { }
```

### 3. 변수명
- **camelCase** (소문자로 시작)
- 의미 있는 이름 사용
- 프로퍼티는 함수명과 동일

```csharp
int count = 0;
string sessionId = "";
bool isConnected = false;
```

- 너무 길거나 보기 힘들면 **snake_case** 허용

```csharp
// camelCase가 너무 길 때
string canvas_panel = ""; // 허용
```

### 4. 파라미터
- **camelCase** + 앞에 밑줄(`_`) 붙이기
- 목적: 파라미터와 멤버변수, 지역변수 구분

```csharp
void AddPlayer(ClientSession _session, string _userId)
{
    // _session, _userId는 파라미터임을 명확히 알 수 있음
    m_players.Add(_session);
}
```

### 5. 멤버 변수
- **camelCase** + 앞에 `m_` 붙이기
- 목적: 멤버 변수임을 명확히 표시

```csharp
public class ClientSession
{
    private string m_sessionId;
    private TcpClient m_tcpClient;
    private NetworkStream m_stream;
    private bool m_isConnected;
    private DateTime m_lastActivityTime;
}
```

### 6. 상수 (const/static)
- **UPPER_SNAKE_CASE** (모두 대문자 + 밑줄)

```csharp
private const int TIMEOUT_SECONDS = 30;
private const int MAX_PLAYERS = 2;
private static readonly int MAX_LENGTH = 100;
public const string DEFAULT_SERVER_NAME = "GameServer";
```

### 7. 프로퍼티
- **PascalCase** (대문자로 시작)

```csharp
public string SessionId { get; private set; }
public GameRoom CurrentRoom { get; set; }
public int PlayerCount { get; private set; }
```

---

## 예시

### 완전한 클래스 예시

```csharp
namespace TestServer
{
    /// <summary>
    /// 클라이언트 세션 관리 클래스
    /// </summary>
    public class ClientSession
    {
        // ===== 상수 =====
        private const int TIMEOUT_SECONDS = 30;
        private const int TIMEOUT_CHECK_INTERVAL = 5000;
        private const int MAX_MESSAGE_SIZE = 1024 * 1024;

        // ===== 프로퍼티 =====
        public string SessionId { get; private set; }
        public TcpClient TcpClient { get; private set; }
        public GameRoom CurrentRoom { get; set; }

        // ===== 멤버 변수 (private) =====
        private NetworkStream m_stream;
        private bool m_isConnected;
        private DateTime m_lastActivityTime;
        private Timer m_timeoutCheckTimer;
        private readonly object m_sendLock = new object();

        // ===== 생성자 =====
        public ClientSession(TcpClient _client)
        {
            TcpClient = _client;
            m_stream = _client.GetStream();
            SessionId = Guid.NewGuid().ToString("N").Substring(0, 8);
            m_lastActivityTime = DateTime.UtcNow;
            m_isConnected = true;
        }

        // ===== Public 메서드 =====
        public async Task StartAsync()
        {
            try
            {
                GameRoom room = RoomManager.Instance.MatchPlayer(this);

                if (room == null)
                {
                    await SendErrorAsync("Failed to join room");
                    Disconnect();
                    return;
                }

                await SendJoinSuccessAsync(room);
                StartTimeoutCheck();
                await ReceiveLoop();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Session {SessionId}] Error: {e.Message}");
            }
            finally
            {
                Cleanup();
            }
        }

        public void Disconnect()
        {
            if (!m_isConnected)
            {
                return;
            }

            m_isConnected = false;
            Console.WriteLine($"[Session {SessionId}] Disconnecting...");

            try
            {
                m_timeoutCheckTimer?.Dispose();
                m_stream?.Close();
                TcpClient?.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Session {SessionId}] Error during disconnect: {e.Message}");
            }
        }

        // ===== Private 메서드 =====
        private void StartTimeoutCheck()
        {
            m_timeoutCheckTimer = new Timer(CheckTimeout, null,
                TIMEOUT_CHECK_INTERVAL, TIMEOUT_CHECK_INTERVAL);
        }

        private void CheckTimeout(object _state)
        {
            if (!m_isConnected)
            {
                return;
            }

            TimeSpan timeSinceLastActivity = DateTime.UtcNow - m_lastActivityTime;

            if (timeSinceLastActivity.TotalSeconds > TIMEOUT_SECONDS)
            {
                Console.WriteLine($"[Session {SessionId}] Timeout detected");
                Disconnect();
            }
        }

        private void UpdateLastActivity()
        {
            m_lastActivityTime = DateTime.UtcNow;
        }
    }
}
```

---

## 요약 체크리스트

### 네이밍
- [ ] 클래스명: `PascalCase`
- [ ] 함수명: `PascalCase` (대문자 시작)
- [ ] 변수명: `camelCase` (소문자 시작)
- [ ] 파라미터: `_camelCase` (밑줄 prefix)
- [ ] 멤버변수: `m_camelCase` (m_ prefix)
- [ ] 상수: `UPPER_SNAKE_CASE`
- [ ] 프로퍼티: `PascalCase`

### 스타일
- [ ] BSD 스타일 코드 블록 사용
- [ ] 1줄 코드도 중괄호 사용
- [ ] 커밋 전 코드 정리 (Ctrl + K, D)

### 코드 구조
```csharp
public class ClassName
{
    // 1. 상수
    private const int CONSTANT_NAME = 100;

    // 2. 프로퍼티
    public string PropertyName { get; set; }

    // 3. 멤버 변수
    private int m_memberVariable;

    // 4. 생성자
    public ClassName(int _param) { }

    // 5. Public 메서드
    public void PublicMethod() { }

    // 6. Private 메서드
    private void PrivateMethod() { }
}
```

---

[⬅️ 서버 README로 돌아가기](../README.md)