# InterPlanetery BaseServer - 클래스 다이어그램

## 전체 클래스 구조

```mermaid
classDiagram
    %% 기본 인터페이스 및 베이스 클래스
    class SingletonBase:::utils {
        #T _instance
        #object _lock
        +T Instance
    }

    class ICommandSender:::interface {
        <<interface>>
        +SendCommand(protocol: Protocol)
    }

    %% 게임 엔티티
    class GamePlayer:::entity {
        -int playerId
        -int crystal
        -int mineral
        -int score
        -List~Planet~ occupiedPlanets
        +UpdateResources()
        +AddScore(points: int)
        +OccupyPlanet(planet: Planet)
    }

    class GamePlanet:::entity {
        -int planetId
        -Vector2 position
        -int crystal
        -int mineral
        -int ownerId
        +GetResources()
        +SetOwner(playerId: int)
    }

    class Fleet:::entity {
        -int fleetId
        -int ownerId
        -FleetState state
        -Vector2 position
        -Vector2 targetPosition
        -int health
        -int attackPower
        +Move(target: Vector2)
        +Attack(target: Fleet)
        +OccupyPlanet(planet: Planet)
        +TakeDamage(damage: int)
    }

    class FleetState:::entity {
        <<enumeration>>
        Idle
        Moving
        InCombat
        Occupying
    }

    class ProduceController:::entity {
        -int playerId
        -Queue~FleetOrder~ productionQueue
        -Timer productionTimer
        +ProduceFleet(fleetType: string)
        +CancelProduction(orderId: int)
        +UpdateProduction()
    }

    class GameMap:::entity {
        -int mapId
        -List~Planet~ planets
        -List~Edge~ pathways
        +GetPlanet(planetId: int)
        +GetRoute(from: int, to: int)
        +GetDistance(pos1: Vector2, pos2: Vector2)
    }

    class GameRoomUser:::entity {
        -int sessionId
        -int userId
        -int playerId
        -long lastHeartbeat
        +IsAlive()
        +UpdateHeartbeat()
    }

    class GameRoom:::entity {
        -string roomId
        -RoomState state
        -GameRoomUser[] users
        -Game gameInstance
        +AddUser(user: GameRoomUser)
        +RemoveUser(sessionId: int)
        +StartGame()
        +UpdateGameState()
    }

    class RoomState:::entity {
        <<enumeration>>
        Waiting
        InProgress
        Finished
    }

    class Game:::logic {
        -GameRoom room
        -GameMap map
        -GamePlayer[] players
        -Queue~Protocol~ commandQueue
        -Timer gameLoopTimer
        -int currentTick
        -Timer resourceTimer
        -Timer winCheckTimer
        +Start()
        +Update(deltaTime: float)
        +ProcessCommand(command: Protocol)
        +UpdateResources()
        +CheckWinCondition()
        +Shutdown()
    }

    %% 매니저
    class RoomManager:::manager {
        -Dictionary~string, GameRoom~ rooms
        -int roomCounter
        +GetInstance()
        +CreateRoom()
        +GetRoom(roomId: string)
        +GetAvailableRoom()
        +RemoveRoom(roomId: string)
        +GetRoomList(page: int)
        +Shutdown()
    }

    class MapManager:::manager {
        +GetInstance()
        +LoadMap(mapId: int)
        +GetAllMaps()
    }

    class DBManager:::manager {
        -DB_Table tableDb
        -DB_Auth authDb
        +GetInstance()
        +UpdateTable()
        +GetTableDb()
        +GetAuthDb()
    }

    %% 데이터베이스
    class DB_Table:::database {
        -MySqlConnection connection
        +GetMap(mapId: int)
        +GetPlanets()
        +GetPathways()
    }

    class DB_Auth:::database {
        -MySqlConnection connection
        +Register(username: string, password: string)
        +Login(username: string, password: string)
        +GetUser(userId: int)
        +UpdateUser(user: User)
    }

    %% 세션 및 네트워크
    class ClientSession:::session {
        -TcpClient tcpClient
        -NetworkStream stream
        -int sessionId
        -int userId
        -GameRoom gameRoom
        -Timer heartbeatTimer
        -int lastHeartbeat
        +StartAsync()
        +SendProtocol(protocol: Protocol)
        +HandleProtocol(protocol: Protocol)
        +Disconnect()
    }

    class ProtocolHandler:::network {
        -Dictionary~int, Delegate~ handlers
        +RegisterHandler(protocolType: int, handler: Delegate)
        +HandleProtocol(protocol: Protocol)
    }

    class Protocol:::network {
        -int protocolType
        -byte[] data
        +GetType()
        +GetData()
    }

    %% 상속 및 구현 관계
    RoomManager --|> SingletonBase
    MapManager --|> SingletonBase
    DBManager --|> SingletonBase

    Game --|> ICommandSender
    ClientSession --|> ICommandSender

    GameRoom "*" -- "1" Game
    GameRoom "*" -- "2" GameRoomUser
    GameRoom "*" -- "1" GameMap

    Game -- "*" GamePlayer
    GamePlayer -- "*" GamePlanet
    GamePlayer -- "*" Fleet

    Fleet -- "1" FleetState
    Fleet -- "*" GamePlanet

    GameMap -- "*" GamePlanet
    GameMap -- "*" ProduceController

    ProduceController -- "1" GamePlayer

    ClientSession -- "1" GameRoom
    ClientSession -- "1" ProtocolHandler
    ClientSession -- "*" Protocol

    ProtocolHandler -- "*" Protocol

    RoomManager -- "*" GameRoom
    MapManager -- "*" GameMap

    DBManager -- "1" DB_Table
    DBManager -- "1" DB_Auth

    %% 스타일
    classDef entity fill:#e1f5ff,stroke:#01579b,stroke-width:2px,color:#000
    classDef logic fill:#fff3e0,stroke:#e65100,stroke-width:2px,color:#000
    classDef manager fill:#f3e5f5,stroke:#4a148c,stroke-width:2px,color:#000
    classDef database fill:#e8f5e9,stroke:#1b5e20,stroke-width:2px,color:#000
    classDef session fill:#fce4ec,stroke:#880e4f,stroke-width:2px,color:#000
    classDef network fill:#f1f8e9,stroke:#33691e,stroke-width:2px,color:#000
    classDef interface fill:#eeeeee,stroke:#424242,stroke-width:2px,color:#000
    classDef utils fill:#fff9c4,stroke:#f57f17,stroke-width:2px,color:#000
```

---

## 주요 클래스 상세 설명

### 게임 엔티티 (Entity)

#### **GamePlayer**
- **역할**: 게임 플레이어 정보 관리
- **필드**: 크리스탈, 광물, 점수, 점령한 행성 목록
- **메서드**: 자원 업데이트, 점수 추가, 행성 점령

#### **Fleet**
- **역할**: 우주 함대 관리
- **상태**: `FleetState` (Idle, Moving, InCombat, Occupying)
- **기능**: 이동, 전투, 행성 점령, 피해 처리

#### **GamePlanet**
- **역할**: 게임 월드의 행성
- **정보**: 위치, 자원, 소유자

#### **GameMap**
- **역할**: 게임 맵 정보
- **구성**: 행성 목록, 경로 정보, 거리 계산

#### **ProduceController**
- **역할**: 함대 생산 관리
- **기능**: 생산 큐, 타이머 관리

#### **GameRoom**
- **역할**: 2인 대전 게임 방
- **상태**: `RoomState` (Waiting, InProgress, Finished)
- **기능**: 사용자 추가/제거, 게임 시작, 상태 동기화

#### **GameRoomUser**
- **역할**: 게임 방 내 플레이어 정보
- **필드**: 세션 ID, 사용자 ID, 플레이어 ID, 마지막 하트비트

---

### 게임 로직 (Logic)

#### **Game**
- **역할**: 게임 루프 및 상태 관리
- **특징**:
  - 고정 틱 레이트 (20 TPS = 50ms)
  - 명령 큐 기반 결정론적 시뮬레이션
  - 네트워크 지연 보상 (3틱 버퍼)
- **메서드**:
  - `Start()`: 게임 시작
  - `Update()`: 게임 상태 업데이트
  - `ProcessCommand()`: 명령 처리
  - `UpdateResources()`: 자원 생산 (200ms마다)
  - `CheckWinCondition()`: 승리 조건 확인 (500ms마다)

---

### 관리자 (Manager)

#### **RoomManager** (싱글톤)
- **역할**: 모든 게임 룸 관리
- **기능**:
  - 룸 생성 (ROOM_0001, ROOM_0002, ...)
  - 룸 조회 및 획득
  - 빈 자리가 있는 룸 검색
  - 빈 룸 자동 정리 (60초마다)
  - 페이지네이션된 룸 리스트

#### **MapManager** (싱글톤)
- **역할**: 게임 맵 데이터 관리
- **기능**: 맵 ID로 맵 로드

#### **DBManager** (싱글톤)
- **역할**: 모든 데이터베이스 접근 중앙 관리
- **구성**: DB_Table, DB_Auth

---

### 데이터베이스 (Database)

#### **DB_Table**
- **역할**: 게임 데이터 관리
- **기능**: 맵, 행성, 경로 정보 조회

#### **DB_Auth**
- **역할**: 사용자 인증 관리
- **기능**: 회원가입, 로그인, 사용자 정보 관리

---

### 세션 및 네트워크 (Session/Network)

#### **ClientSession**
- **역할**: 개별 클라이언트 연결 관리
- **기능**:
  - TCP 스트림 읽기/쓰기
  - 프로토콜 수신 및 처리
  - 타임아웃 감지 (30초)
  - 로그인/로그아웃
  - 룸 생성/참여
  - 게임 명령 송수신

#### **ProtocolHandler**
- **역할**: 프로토콜 타입별 핸들러 관리
- **패턴**: 전략 패턴 (Dictionary 기반 이벤트 처리)

#### **Protocol**
- **역할**: 네트워크 프로토콜 데이터 구조

---

### 구현된 인터페이스

#### **ICommandSender**
- **역할**: 게임 명령 송신 인터페이스
- **구현**: `Game`, `ClientSession`

#### **SingletonBase<T>**
- **역할**: 스레드 안전한 싱글톤 패턴
- **구현**: `RoomManager`, `MapManager`, `DBManager`

---

## 상속 관계 요약

```
SingletonBase<T>
├─ RoomManager
├─ MapManager
└─ DBManager

ICommandSender
├─ Game
└─ ClientSession
```

---

## 집합 관계 요약

| 포함자 | 포함 개수 | 포함물 |
|--------|---------|--------|
| GameRoom | 2 | GameRoomUser |
| GameRoom | 1 | Game |
| Game | 2 | GamePlayer |
| GamePlayer | N | Fleet |
| GamePlayer | N | GamePlanet |
| GameMap | N | GamePlanet |
| RoomManager | N | GameRoom |
| DBManager | 1 | DB_Table |
| DBManager | 1 | DB_Auth |

