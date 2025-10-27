# UI 프로토콜 구현 상태

이 문서는 UI 구현에 필요한 프로토콜이 `interplanetary_system_spec.md`에 정의되어 있는지 확인한 결과입니다.

---

## ✅ 스펙에 정의된 프로토콜 (구현 가능)

### 로비 화면

#### 클라이언트 → 서버
- ✅ **3001 CONNECT** (서버 연결)
  - playerName, version

- ✅ **3103 GET_ROOM_LIST** (룸 목록 조회)
  - 파라미터 없음

- ✅ **3100 CREATE_ROOM** (룸 생성)
  - mapId, roomName, isPrivate

- ✅ **3101 JOIN_ROOM** (룸 참가)
  - roomId

#### 서버 → 클라이언트
- ✅ **4001 CONNECTED** (연결 성공)
  - sessionId, playerName, serverTime

- ✅ **4205 ROOM_LIST** (룸 목록)
  - roomCount, rooms (JSON String)

- ✅ **4200 ROOM_CREATED** (룸 생성 완료)
  - roomId, roomName, mapId

- ✅ **4201 ROOM_JOINED** (룸 참가 완료)
  - roomId, playerSlot, roomInfo (JSON String)

---

### 대기방 화면

#### 클라이언트 → 서버
- ✅ **3104 READY** (준비 완료/취소)
  - isReady

- ✅ **3102 LEAVE_ROOM** (룸 퇴장)
  - 파라미터 없음

#### 서버 → 클라이언트
- ✅ **4202 ROOM_LEFT** (룸 퇴장 완료)
  - 파라미터 없음

- ✅ **4203 PLAYER_JOINED_ROOM** (다른 플레이어 입장)
  - playerName, playerSlot

- ✅ **4204 PLAYER_LEFT_ROOM** (다른 플레이어 퇴장)
  - playerSlot, reason

- ✅ **4206 PLAYER_READY_STATE** (플레이어 준비 상태)
  - playerSlot, isReady

- ✅ **4002 GAME_STARTED** (게임 시작)
  - gameId, mapId, players (JSON String)

---

### 공통
- ✅ **4999 ERROR** (에러 메시지)
  - errorCode, message

- ✅ **3005 HEARTBEAT** (하트비트)
  - timestamp

- ✅ **4013 HEARTBEAT_ACK** (하트비트 응답)
  - serverTime

---

## ❌ 스펙에 없는 프로토콜 (추가 필요)

### 1. 맵 목록 조회

현재 방 생성 UI에서 맵을 선택하려면 맵 목록이 필요하지만, 이를 조회하는 프로토콜이 없습니다.

**해결 방안:**

#### 옵션 1: 클라이언트에 하드코딩
```csharp
// 클라이언트 코드에 직접 작성
var maps = new List<Map>
{
    new Map { MapId = 1, MapName = "3레인 전투장" },
    new Map { MapId = 2, MapName = "쌍성계" },
    new Map { MapId = 3, MapName = "거울 세계" },
    new Map { MapId = 4, MapName = "교차로" },
    new Map { MapId = 5, MapName = "단순 경로" }
};
```
- 장점: 간단, 빠른 구현
- 단점: 서버 DB와 동기화 필요, 맵 추가 시 클라이언트 업데이트 필요

#### 옵션 2: 프로토콜 추가 (권장)
```
3105 GET_MAP_LIST (클라이언트 → 서버)
- 파라미터 없음

4210 MAP_LIST (서버 → 클라이언트)
- mapCount: int
- maps: String (JSON)

JSON 구조:
[
  { "mapId": 1, "mapName": "3레인 전투장" },
  { "mapId": 2, "mapName": "쌍성계" }
]
```
- 장점: 서버 DB와 자동 동기화, 확장성
- 단점: 프로토콜 추가 작업 필요

---

### 2. ChatMessage 구조체 수정

BaseServer에 이미 채팅 프로토콜(1003 CHAT_MESSAGE, 2006 CHAT_BROADCAST)이 구현되어 있습니다.

**현재 ChatMessage 구조체:**
```csharp
public struct ChatMessage
{
    public string SenderId;
    public string Message;
    public long Timestamp;
}
```

**수정 필요:**
`MessageType` 필드를 추가하여 대기방/인게임 채팅을 구분해야 합니다.

```csharp
public struct ChatMessage
{
    public string SenderId;      // "SYSTEM"이면 시스템 메시지
    public string Message;
    public long Timestamp;
    public int MessageType;      // 0: LOBBY(대기방), 1: INGAME(인게임)
}
```

**사용 프로토콜:**
- ✅ **1003 CHAT_MESSAGE** (클라이언트 → 서버) - 이미 구현됨
- ✅ **2006 CHAT_BROADCAST** (서버 → 클라이언트) - 이미 구현됨

**시스템 메시지:**
- `SenderId == "SYSTEM"`으로 판별

---

### 3. 방 목록 실시간 업데이트 (선택 사항)

현재는 `3103 GET_ROOM_LIST`로 수동 조회만 가능합니다.
방 상태가 변경될 때 자동으로 알림 받는 기능이 없습니다.

**해결 방안:**

#### 옵션 1: 주기적 폴링
클라이언트가 주기적으로 `3103 GET_ROOM_LIST` 호출
- 장점: 현재 프로토콜만으로 구현 가능
- 단점: 서버 부하, 실시간성 낮음

#### 옵션 2: 서버 푸시 프로토콜 추가
```
4207 ROOM_UPDATED (서버 → 클라이언트, 브로드캐스트)
- roomId: String
- currentPlayers: int
- status: String (waiting/full/playing)

4208 ROOM_REMOVED (서버 → 클라이언트, 브로드캐스트)
- roomId: String
```
- 장점: 실시간 업데이트, 효율적
- 단점: 프로토콜 추가 작업 필요

---

## 구현 전략

### Phase 1: 최소 기능 구현 (스펙 내 프로토콜만 사용)

#### 로비 화면
1. ✅ 서버 연결 (3001 → 4001)
2. ✅ 방 목록 조회 - **수동 새로고침** (3103 → 4205)
3. ✅ 방 생성 - **맵은 하드코딩** (3100 → 4200)
4. ✅ 방 참가 (3101 → 4201)

#### 대기방 화면
1. ✅ 플레이어 입장/퇴장 (4203, 4204)
2. ✅ 준비 상태 (3104 → 4206)
3. ✅ 게임 시작 (4002)
4. ✅ **채팅 기능** (1003 → 2006) - BaseServer의 기존 프로토콜 사용

---

### Phase 2: 추가 기능 구현 (코드 수정)

1. 맵 목록 조회 프로토콜 추가 (3105, 4210)
2. **ChatMessage 구조체에 MessageType 필드 추가** (CommonLib/ChatProtocol.cs)
3. 방 목록 실시간 업데이트 프로토콜 추가 (4207, 4208)

---

## 필요한 JSON 데이터 구조

### 4205 ROOM_LIST - rooms 파라미터
```json
[
  {
    "roomId": "ROOM_abc123",
    "roomName": "은하계 정복전",
    "currentPlayers": 1,
    "maxPlayers": 2,
    "mapId": 1,
    "mapName": "3레인 전투장",
    "status": "waiting",
    "isPrivate": false
  }
]
```

### 4201 ROOM_JOINED - roomInfo 파라미터
```json
{
  "roomId": "ROOM_abc123",
  "roomName": "은하계 정복전",
  "mapId": 1,
  "mapName": "3레인 전투장",
  "maxPlayers": 2,
  "isPrivate": false,
  "hostPlayerId": 1,
  "players": [
    {
      "playerSlot": 1,
      "playerName": "플레이어_12345",
      "isHost": true,
      "isReady": false
    }
  ]
}
```

### 4002 GAME_STARTED - players 파라미터
```json
[
  {
    "playerId": 1,
    "playerName": "플레이어_12345",
    "homeworldId": 1
  },
  {
    "playerId": 2,
    "playerName": "플레이어_67890",
    "homeworldId": 8
  }
]
```

---

## 결론

### ✅ 즉시 구현 가능한 UI 기능
- 서버 연결
- 방 목록 조회 (수동 새로고침)
- 방 생성 (맵 하드코딩)
- 방 참가/퇴장
- 플레이어 관리
- 준비 상태 관리
- 게임 시작

### ❌ 프로토콜 추가 필요한 기능
- 맵 목록 동적 조회
- 채팅 시스템
- 방 목록 실시간 업데이트

### 권장 사항
1. **Phase 1**: 기존 프로토콜로 기본 UI 구현
2. **Phase 2**: 맵 목록 조회 프로토콜 추가 (3105, 4210)
3. **Phase 3**: 채팅 프로토콜 추가 (3200, 4300, 4301)
4. **Phase 4**: 실시간 업데이트 프로토콜 추가 (4207, 4208)
