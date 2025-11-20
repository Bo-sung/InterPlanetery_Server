# 게임 서버 네트워크 프로토콜

## 기본 정보
- 통신 방식: TCP/UDP
- 인코딩: JSON
- 버전: 1.0.0

## 프로토콜 목록

### 클라이언트 -> 서버

| ID | 이름 | 설명 |
|----|------|------|
| 10000 | REQUEST_LOGIN | 로그인 요청 |
| 10001 | REQUEST_LOGOUT | 로그아웃 요청 |
| 10002 | CHAT_MESSAGE | 메시지 전송 |
| 10003 | HEARTBEAT | 하트비트 (연결 유지 확인) |
| 10004 | REQUEST_TABLEDATA | 테이블 데이터 요청 |
| 10010 | REQUEST_JOIN_LOBBY | 로비 접속 요청 |
| 10011 | REFRESH_LOBBY | 로비 새로고침 요청 |
| 10012 | REQUEST_CREATE_ROOM | 방 생성 요청 |
| 10013 | REQUEST_JOIN_ROOM | 방 입장 요청 |
| 10014 | REQUEST_READY | 게임 레디 |
| 10015 | REQUEST_LEFT_ROOM | 방 퇴장 요청 |

### 서버 -> 클라이언트

| ID | 이름 | 설명 |
|----|------|------|
| 20000 | RESPONSE | 전체 공통 응답처리 |
| 20001 | BRODCAST_SYSTEM | 시스템 공통 알림 |
| 20002 | BRODCAST_CHAT_MESSAGE | 메시지 브로드캐스트 |
| 20003 | HEARTBEAT_ACK | 하트비트 응답 |
| 20010 | USER_JOINED | 유저 접속 알림 |
| 20011 | USER_LEFT | 유저 이탈 알림 |
| 20012 | ROOM_INFO_CHANGED | 방 정보 변경 알림 |
| 20013 | ROOM_CLOSED | 방 삭제 알림 |
| 3010 | SUBMIT_COMMAND | 명령 제출 |

## 프로토콜 상세

### REQUEST_LOGIN
- ID: 10000
- param:
  - username: string
  - password: string
- 응답 param:
  - sessionId: string

### REQUEST_LOGOUT
- ID: 10001
- 파라미터:
- 응답 param:

### CHAT_MESSAGE
- ID: 10002
- 파라미터:
  - type: int
  - channelId: string
  - chatMessage: ChatMessage
- 응답 param:
  - serverTime: long

### HEARTBEAT
- ID: 10003
- 파라미터:
  - timestamp: long
- 응답으로 HEARTBEAT_ACK

### REQUEST_TABLEDATA
- ID: 10004
- 파라미터:
  - table_name: string
- 응답 param:
  - tableRowCount: int
  - tableRows: string[]
  - tableTypes: type[]
  - tableData: object[][];

### REQUEST_JOIN_LOBBY
- ID: 10010
- 파라미터:
  - Page: int   //기본값 0 입력시 전체 전송
- 응답 param:
  - roomCount: int
  - page: int
  - roomList: RoomInfo[]

### REFRESH_LOBBY
- ID: 10011
- 파라미터: 없음
- 응답 param:
  - roomList: RoomInfo[]

### REQUEST_CREATE_ROOM
- ID: 10012
- 파라미터:
  - room_name: string
  - mapId: int
  - is_private: boolean
- 응답 param:
  - roomId: string
  - slot: int

### REQUEST_JOIN_ROOM
- ID: 10013
- 파라미터:
  - roomId: string
  - slot: int
- 응답 param:
  - roomInfo: RoomInfo
  - chatChannelID: string

### REQUEST_READY
- ID: 10014
- 파라미터:
  - isReady: boolean
- 응답 param:

### REQUEST_LEFT_ROOM
- ID: 10015
- 파라미터:
- 응답 param: 

### RESPONSE    
- ID: 20000
- 설명 : 응답 공통 프로토콜
- 파라미터:
  - protoId: int
  - status: int
  - message: string
  - data: object

### BRODCAST_SYSTEM
- ID: 20001
- 파라미터:
  - message: string
  - type: int

### BRODCAST_CHAT_MESSAGE
- ID: 20002
- 파라미터:
  - chatMessage: ChatMessage

### HEARTBEAT_ACK
- ID: 20003
- 파라미터:
  - timestamp: long
  - server_time: long

### USER_JOINED
- ID: 20010
- 파라미터:
  - userinfo: UserInfo

### USER_LEFT
- ID: 20011
- 파라미터:
  - userind: string
  - reason: int

### ROOM_INFO_CHANGED
- ID: 20012
- 파라미터:
  - roomId: string
  - roomInfo: RoomInfo

### ROOM_CLOSED
- ID: 20013
- 파라미터:
  - roomId: string
  - reason: int

### SUBMIT_COMMAND
- ID: 3010
- 파라미터:
  - command_type: int
  - command_data: object

## 구조체 정의

### UserData
- userId: int
- username: string

### RoomInfo
- RoomId: string
- playerCount: int
- MaxPlayers: int
- roomName: string
- roomState: RoomState
- mapId: int

## 열거형 정의

### FleetType
- Scout
- Fighter
- Cruiser
- BattleShip

### RoomState
- open
- full
- ingame
- disabled
- closed
- error

## 상태 코드

| 코드 | 설명 |
|------|------|
| 0 | 성공 |
| 1 | 일반 오류 |
| 2 | 인증 실패 |
| 3 | 권한 부족 |
| 4 | 리소스 없음 |
| 5 | 서버 오류 |