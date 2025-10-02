# 프로토콜 명세서 (Protocol Specification)

## 목차
1. [개요](#개요)
2. [프로토콜 구조](#프로토콜-구조)
3. [프로토콜 타입](#프로토콜-타입)
4. [데이터 구조](#데이터-구조)
5. [핸들러 시스템](#핸들러-시스템)

---

## 개요

본 문서는 2인 채팅 서버의 네트워크 프로토콜 명세를 정의합니다.

### 기본 특성
- **직렬화 방식**: 바이너리 + JSON (복합 타입)
- **전송 방식**: TCP/IP
- **인코딩**: UTF-8
- **바이트 순서**: Little Endian

### 프로토콜 설계 철학
- 크로스 플랫폼 지원 (C#, Unity, 기타 언어)
- JSON 기반으로 구조체/클래스 직렬화
- 타입 안전성 보장
- 확장 가능한 구조

---

## 프로토콜 구조

### 전체 패킷 구조

```
[전체 크기(4)] [프로토콜 타입(4)] [타임스탬프(8)] [데이터 개수(2)] [데이터...]
```

### 헤더 구조 (18 바이트)

| 필드 | 크기 | 타입 | 설명 |
|------|------|------|------|
| Length | 4 bytes | int32 | 헤더 제외한 전체 메시지 크기 |
| Type | 4 bytes | int32 | 프로토콜 타입 (1001~2999) |
| Timestamp | 8 bytes | int64 | UTC 밀리초 타임스탬프 |
| DataCount | 2 bytes | uint16 | 데이터 파라미터 개수 |

### 데이터 파라미터 구조

각 데이터 파라미터는 다음 구조를 가집니다:

```
[키 길이(1)] [키(N)] [타입(1)] [값(N)]
```

| 필드 | 크기 | 타입 | 설명 |
|------|------|------|------|
| KeyLength | 1 byte | byte | 키 문자열 길이 |
| Key | N bytes | UTF-8 string | 파라미터 키 이름 |
| DataType | 1 byte | byte | 데이터 타입 (0x10~0x19) |
| Value | N bytes | varies | 타입별 가변 크기 값 |

### 데이터 타입 정의

| 타입 코드 | 이름 | 크기 | C# 타입 | 설명 |
|-----------|------|------|---------|------|
| 0x10 | TYPE_BYTE | 1 | byte | 8비트 부호 없는 정수 |
| 0x11 | TYPE_SHORT | 2 | short | 16비트 부호 있는 정수 |
| 0x12 | TYPE_INT | 4 | int | 32비트 부호 있는 정수 |
| 0x13 | TYPE_LONG | 8 | long | 64비트 부호 있는 정수 |
| 0x14 | TYPE_FLOAT | 4 | float | 32비트 부동소수점 |
| 0x15 | TYPE_DOUBLE | 8 | double | 64비트 부동소수점 |
| 0x16 | TYPE_BOOL | 1 | bool | 불린 (0=false, 1=true) |
| 0x17 | TYPE_STRING | 2+N | string | [길이(2)][UTF-8 문자열] |
| 0x18 | TYPE_BYTES | 4+N | byte[] | [길이(4)][바이트 배열] |
| 0x19 | TYPE_OBJECT | 4+N | object | [길이(4)][JSON 문자열] |

---

## 프로토콜 타입

### 클라이언트 → 서버 (1000번대)

| 코드 | 상수명 | 설명 |
|------|--------|------|
| 1001 | JOIN_ROOM | 룸 입장 요청 (현재 자동 매칭) |
| 1002 | LEAVE_ROOM | 룸 퇴장 요청 |
| 1003 | CHAT_MESSAGE | 채팅 메시지 전송 |
| 1004 | HEARTBEAT | 하트비트 (연결 유지 확인) |

### 서버 → 클라이언트 (2000번대)

| 코드 | 상수명 | 설명 |
|------|--------|------|
| 2001 | JOIN_SUCCESS | 입장 성공 |
| 2002 | JOIN_FAILED | 입장 실패 |
| 2003 | LEAVE_SUCCESS | 퇴장 성공 |
| 2004 | USER_JOINED | 다른 유저 입장 알림 |
| 2005 | USER_LEFT | 다른 유저 퇴장 알림 |
| 2006 | CHAT_BROADCAST | 채팅 메시지 브로드캐스트 |
| 2007 | ROOM_CLOSED | 룸 종료 알림 |
| 2008 | HEARTBEAT_ACK | 하트비트 응답 |
| 2999 | ERROR | 에러 메시지 |

---

## 데이터 구조

### ChatMessage 구조체

채팅 메시지 전송 시 사용되는 데이터 구조

```csharp
public struct ChatMessage
{
    public string SenderId;     // 발신자 세션 ID
    public string Message;      // 메시지 내용
    public long Timestamp;      // 타임스탬프 (밀리초)
}
```

**JSON 예시:**
```json
{
    "SenderId": "abc12345",
    "Message": "Hello, World!",
    "Timestamp": 1234567890123
}
```

### RoomInfo 구조체

룸 정보를 전달하는 데이터 구조

```csharp
public struct RoomInfo
{
    public string RoomId;       // 룸 ID (예: "ROOM_0001")
    public int PlayerCount;     // 현재 플레이어 수
    public int MaxPlayers;      // 최대 플레이어 수 (2명)
}
```

**JSON 예시:**
```json
{
    "RoomId": "ROOM_0001",
    "PlayerCount": 2,
    "MaxPlayers": 2
}
```

---

## 핸들러 시스템

### ProtocolHandler 구조

서버는 딕셔너리 기반 프로토콜 핸들러 시스템을 사용합니다.

```csharp
public class ProtocolHandler
{
    public delegate Task ProtocolHandlerDelegate(Protocol _protocol);
    private Dictionary<int, ProtocolHandlerDelegate> m_handlers;

    // 핸들러 등록
    public void RegisterHandler(int _protocolType, ProtocolHandlerDelegate _handler);

    // 프로토콜 처리
    public async Task HandleProtocol(Protocol _protocol);
}
```

### 핸들러 등록 예시

```csharp
private void RegisterProtocolHandlers()
{
    m_protocolHandler.RegisterHandler(ChatProtocolType.CHAT_MESSAGE, HandleChatMessage);
    m_protocolHandler.RegisterHandler(ChatProtocolType.LEAVE_ROOM, HandleLeaveRoom);
    m_protocolHandler.RegisterHandler(ChatProtocolType.HEARTBEAT, HandleHeartbeat);
}
```

### 장점

- 새 프로토콜 추가 시 등록만 하면 됨 (switch-case 불필요)
- 동적 핸들러 등록/해제 가능
- 확장성 및 유지보수성 향상

---

## 참고 문서

- [서버 아키텍처 문서](ServerArchitecture.md)
- [코딩 스타일 가이드](CodingStyle.md)

---

[⬅️ 서버 README로 돌아가기](../README.md)