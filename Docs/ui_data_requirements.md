# UI 데이터 요구사항 명세

이 문서는 게임 로비 & 대기방 UI를 구현하기 위해 서버로부터 주고받아야 할 데이터의 종류와 구조를 정의합니다.

**참고:** 모든 프로토콜의 상세 명세(번호, 파라미터 등)는 메인 기획서인 `interplanetary_system_spec.md` 문서를 기준으로 합니다.

---

## 1. 로비 화면

### 1.1 사용자 정보 패널

**표시 요소:**
- 플레이어 이름
- 접속 상태

**필요 데이터 (서버 → 클라이언트):**
- `4001 CONNECTED` 프로토콜을 통해 `playerName`, `sessionId` 등을 받습니다.

---

### 1.2 게임 방 목록

**표시 요소:**
- 방 이름
- 플레이어 수 (현재/최대)
- 맵 이름
- 방 상태 (대기중/만원/게임중)

**필요 데이터 (서버 → 클라이언트):**
- `4205 ROOM_LIST` 프로토콜을 통해 아래와 같은 구조의 방 목록(JSON)을 받습니다.
```json
{
  "rooms": [
    {
      "roomId": "room_001",
      "roomName": "은하계 정복전",
      "currentPlayers": 1,
      "maxPlayers": 2,
      "mapId": 1,
      "mapName": "3레인 전투장",
      "status": "waiting",
      "isPrivate": false
    }
  ]
}
```

**관련 동작 (클라이언트 → 서버):**
- 로비 진입 또는 '새로고침' 버튼 클릭 시 `3103 GET_ROOM_LIST` 프로토콜을 서버로 전송합니다.
- 방 '입장' 버튼 클릭 시 `3101 JOIN_ROOM` 프로토콜을 전송합니다.

---

### 1.3 방 만들기 폼

**표시 요소:**
- 맵 목록 (드롭다운)

**필요 데이터 (서버 → 클라이언트):**
- `4210 MAP_LIST` 프로토콜을 통해 아래와 같은 구조의 맵 목록(JSON)을 받습니다.
```json
{
  "maps": [
    {
      "mapId": 1,
      "mapName": "3레인 전투장"
    },
    {
      "mapId": 2,
      "mapName": "쌍성계"
    }
  ]
}
```

**관련 동작 (클라이언트 → 서버):**
- '방 만들기' UI 진입 시 `3105 GET_MAP_LIST` 프로토콜을 서버로 전송합니다.
- '방 만들기' 버튼 클릭 시 `3100 CREATE_ROOM` 프로토콜을 전송합니다.

---

## 2. 대기방 화면

### 2.1 방 정보 및 게임 설정

**표시 요소:**
- 방 이름, 맵 이름
- 최대 플레이어, 시작 자원, 방 유형 등

**필요 데이터 (서버 → 클라이언트):**
- `4201 ROOM_JOINED` 프로토콜의 `roomInfo` 파라미터(JSON)를 통해 방의 상세 정보를 받습니다.
```json
{
  "roomId": "ROOM_abc123",
  "roomName": "은하계 정복전",
  "mapId": 1,
  "mapName": "3레인 전투장",
  "maxPlayers": 2,
  "isPrivate": false,
  "hostPlayerId": 1
}
```

---

### 2.2 플레이어 슬롯

**표시 요소:**
- 플레이어 이름, 역할(방장), 준비 상태 등

**필요 데이터 (서버 → 클라이언트):**
- `4201 ROOM_JOINED` 시 전체 플레이어 목록을 받습니다.
- `4203 PLAYER_JOINED_ROOM` / `4204 PLAYER_LEFT_ROOM` 프로토콜을 통해 다른 플레이어의 입장/퇴장 정보를 실시간으로 받습니다.
- `4206 PLAYER_READY_STATE` 프로토콜을 통해 플레이어의 준비 상태 변경을 실시간으로 받습니다.

**관련 동작 (클라이언트 → 서버):**
- '준비' 버튼 클릭 시 `3104 READY` 프로토콜을 전송합니다.

---

### 2.3 채팅

**표시 요소:**
- 채팅 메시지, 시스템 메시지, 보낸 사람, 시간

**필요 데이터 (서버 → 클라이언트):**
- `2006 CHAT_BROADCAST` 프로토콜을 통해 아래 `ChatMessage` 구조체를 받습니다.

**ChatMessage 구조체:**
```csharp
public struct ChatMessage
{
    public string SenderId;      // "SYSTEM"이면 시스템 메시지
    public string Message;
    public long Timestamp;
    public int MessageType;      // 0: LOBBY(대기방), 1: INGAME(인게임)
}
```

**관련 동작 (클라이언트 → 서버):**
- 메시지 입력 후 '전송' 시 `1003 CHAT_MESSAGE` 프로토콜을 전송합니다.