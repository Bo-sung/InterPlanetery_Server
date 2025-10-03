# Unity 네트워크 테스트 클라이언트

InterPlanetery 서버와 통신하기 위한 Unity 클라이언트 컴포넌트입니다.

## 📁 파일 구조

```
UnityClient/
├── NetworkProtocol.cs        # 프로토콜 직렬화/역직렬화
├── ChatProtocol.cs           # 채팅 프로토콜 타입 정의
├── NetworkClient.cs          # TCP 네트워크 클라이언트
├── ChatNetworkTester.cs      # 채팅 테스트 UI 컴포넌트
└── README.md                 # 이 문서
```

## 🚀 시작하기

### 1. Unity 프로젝트에 파일 추가

모든 `.cs` 파일을 Unity 프로젝트의 `Assets/Scripts/Network/` 폴더에 복사합니다.

### 2. TextMeshPro 설치

이 컴포넌트는 TextMeshPro를 사용합니다.
- `Window > Package Manager`에서 **TextMeshPro** 패키지를 설치하세요.

### 3. UI 씬 설정

#### 기본 UI 구조 생성

1. **Canvas** 생성 (`GameObject > UI > Canvas`)

2. **Server Connection Panel** (연결 설정)
   - InputField (TMP): 서버 호스트 입력 (`m_hostInput`)
   - InputField (TMP): 서버 포트 입력 (`m_portInput`)
   - Button: 연결 버튼 (`m_connectButton`)
   - Button: 연결 해제 버튼 (`m_disconnectButton`)

3. **Room Panel** (룸 관리)
   - Button: 룸 입장 버튼 (`m_joinRoomButton`)
   - Button: 룸 퇴장 버튼 (`m_leaveRoomButton`)

4. **Chat Panel** (채팅)
   - Text (TMP): 채팅 로그 표시 (`m_chatLogText`)
   - InputField (TMP): 채팅 입력 (`m_chatInput`)
   - Button: 전송 버튼 (`m_sendButton`)

5. **Status Panel** (상태 표시)
   - Text (TMP): 연결 상태 (`m_statusText`)

### 4. ChatNetworkTester 컴포넌트 설정

1. Canvas에 빈 GameObject 생성 (`ChatNetworkManager`)
2. `ChatNetworkTester` 컴포넌트 추가
3. Inspector에서 모든 UI 참조 연결:
   - Host Input
   - Port Input
   - Connect Button
   - Disconnect Button
   - Join Room Button
   - Leave Room Button
   - Chat Input
   - Send Button
   - Chat Log Text
   - Status Text

## 🎮 사용 방법

### UI를 통한 사용

1. **서버 연결**
   - Host 입력 필드에 서버 IP 입력 (기본값: `127.0.0.1`)
   - Port 입력 필드에 포트 번호 입력 (기본값: `7777`)
   - "연결" 버튼 클릭

2. **룸 입장**
   - "룸 입장" 버튼 클릭
   - 서버에서 자동으로 룸 배정

3. **채팅 전송**
   - 채팅 입력 필드에 메시지 입력
   - "전송" 버튼 클릭 또는 Enter 키

4. **룸 퇴장**
   - "룸 퇴장" 버튼 클릭

5. **연결 해제**
   - "연결 해제" 버튼 클릭

### 스크립트를 통한 사용

```csharp
using InterPlanetary.Network;

public class GameManager : MonoBehaviour
{
    private ChatNetworkTester m_tester;

    void Start()
    {
        m_tester = FindObjectOfType<ChatNetworkTester>();

        // 프로그래밍 방식으로 연결
        m_tester.Connect("127.0.0.1", 7777);
    }

    void Update()
    {
        // 연결 상태 확인
        if (m_tester.IsConnected && m_tester.IsInRoom)
        {
            Debug.Log("룸에 입장 중");
        }
    }
}
```

## 📡 프로토콜 구조

### 클라이언트 → 서버

| 타입 | 코드 | 설명 | 파라미터 |
|------|------|------|----------|
| JOIN_ROOM | 1001 | 룸 입장 요청 | 없음 |
| LEAVE_ROOM | 1002 | 룸 퇴장 요청 | 없음 |
| CHAT_MESSAGE | 1003 | 채팅 메시지 전송 | `message` (string) |
| HEARTBEAT | 1004 | 하트비트 | 없음 |

### 서버 → 클라이언트

| 타입 | 코드 | 설명 | 파라미터 |
|------|------|------|----------|
| JOIN_SUCCESS | 2001 | 입장 성공 | `roomId`, `playerCount` |
| JOIN_FAILED | 2002 | 입장 실패 | `reason` |
| LEAVE_SUCCESS | 2003 | 퇴장 성공 | 없음 |
| USER_JOINED | 2004 | 다른 유저 입장 | `userId`, `playerCount` |
| USER_LEFT | 2005 | 다른 유저 퇴장 | `userId`, `playerCount` |
| CHAT_BROADCAST | 2006 | 채팅 브로드캐스트 | `chatMessage` (ChatMessage) |
| ROOM_CLOSED | 2007 | 룸 종료 | `roomId`, `reason` |
| HEARTBEAT_ACK | 2008 | 하트비트 응답 | 없음 |
| ERROR | 2999 | 에러 메시지 | `message` |

## 🔧 커스터마이징

### NetworkProtocol 직접 사용

```csharp
using InterPlanetary.Network;

// 프로토콜 생성 및 전송
NetworkProtocol protocol = new NetworkProtocol(ChatProtocolType.CHAT_MESSAGE)
    .AddParam("message", "Hello, World!");

await networkClient.SendAsync(protocol);
```

### 프로토콜 수신 핸들러 추가

```csharp
using InterPlanetary.Network;

public class CustomNetworkHandler : MonoBehaviour
{
    private NetworkClient m_client;

    void Start()
    {
        m_client = new NetworkClient();
        m_client.OnProtocolReceived += OnProtocolReceived;
    }

    private void OnProtocolReceived(NetworkProtocol protocol)
    {
        switch (protocol.Type)
        {
            case ChatProtocolType.CHAT_BROADCAST:
                ChatMessage msg = protocol.GetObject<ChatMessage>("chatMessage");
                Debug.Log($"Received: {msg}");
                break;
        }
    }
}
```

### 커스텀 데이터 타입 전송

```csharp
[Serializable]
public struct PlayerPosition
{
    public float X;
    public float Y;
    public float Z;
}

// 전송
PlayerPosition pos = new PlayerPosition { X = 1.0f, Y = 2.0f, Z = 3.0f };
NetworkProtocol protocol = new NetworkProtocol(1005)
    .AddObject("position", pos);

// 수신
PlayerPosition receivedPos = protocol.GetObject<PlayerPosition>("position");
```

## ⚠️ 주의사항

1. **메인 스레드 처리**
   - 네트워크 이벤트는 자동으로 Unity 메인 스레드에서 실행됩니다
   - `UnityMainThreadDispatcher`가 자동으로 생성됩니다

2. **서버 연결**
   - 서버가 실행 중인지 확인하세요 (`Servers/TestServer`)
   - 방화벽 설정을 확인하세요

3. **에러 처리**
   - 네트워크 에러는 `OnError` 이벤트로 전달됩니다
   - 채팅 로그에 빨간색으로 표시됩니다

4. **TextMeshPro 대신 기본 UI 사용**
   - `TMP_InputField` → `InputField`
   - `TMP_Text` → `Text`
   - `using TMPro;` 제거

## 🐛 디버깅

### 연결 문제

```csharp
// Unity Console에서 로그 확인
Debug.Log($"Connected: {networkClient.IsConnected}");
```

### 프로토콜 내용 확인

```csharp
private void OnProtocolReceived(NetworkProtocol protocol)
{
    Debug.Log(protocol.ToString()); // 프로토콜 전체 내용 출력
}
```

## 📚 관련 문서

- [서버 아키텍처](../Docs/ServerArchitecture.md)
- [프로토콜 명세서](../Docs/ProtocolSpecification.md)
- [프로토콜 사용 가이드](../Docs/ProtocolUsageGuide.md)

## 🎯 테스트 시나리오

1. **기본 연결 테스트**
   - 서버 실행 → 클라이언트 연결 → 연결 성공 확인

2. **룸 입장/퇴장 테스트**
   - 룸 입장 → 성공 메시지 확인 → 룸 퇴장 → 성공 메시지 확인

3. **채팅 테스트**
   - 두 개의 클라이언트 실행 (Unity Editor + Build)
   - 각각 룸 입장
   - 한쪽에서 메시지 전송
   - 다른 쪽에서 수신 확인

4. **연결 해제 테스트**
   - 룸 입장 상태에서 연결 해제
   - 다른 클라이언트에서 퇴장 알림 확인

---

**문의**: 프로젝트 이슈 트래커에 문의하세요
