# InterPlanetery BaseServer - 데이터 흐름도

## 1. 클라이언트 연결 및 로그인 흐름

```mermaid
graph TD
    A["클라이언트<br/>TCP 연결"] -->|127.0.0.1:9000| B["서버<br/>Program.Main"]
    B -->|AcceptTcpClientAsync| C["ClientSession<br/>생성"]
    C --> D["TCP 스트림<br/>설정"]
    D --> E["프로토콜 수신<br/>대기"]

    E -->|REQUEST_REGISTER| F["회원가입<br/>처리"]
    F --> G["DB_Auth<br/>CreateUser"]
    G --> H["MySQL<br/>authdb"]
    H --> I["회원가입<br/>응답"]

    E -->|REQUEST_LOGIN| J["로그인<br/>처리"]
    J --> K["DB_Auth<br/>GetUser"]
    K --> H
    H --> L["사용자<br/>정보 반환"]
    L --> M["로그인 성공<br/>세션 생성"]
    M --> N["HEARTBEAT<br/>주기적 전송"]

    I --> O["클라이언트로<br/>응답 전송"]
    N --> O

    style A fill:#e3f2fd
    style B fill:#fff3e0
    style C fill:#fce4ec
    style H fill:#e8f5e9
    style O fill:#e3f2fd
```

---

## 2. 게임 방 생성 및 참여 흐름

```mermaid
graph TD
    A["클라이언트"] -->|REQUEST_CREATE_ROOM| B["ClientSession"]
    B --> C["로비 시스템"]
    C --> D["RoomManager<br/>CreateRoom"]
    D --> E["GameRoom 인스턴스<br/>생성"]
    E --> F["방 ID 할당<br/>ROOM_0001"]
    F --> G["RoomManager<br/>딕셔너리에<br/>등록"]
    G --> H["방 생성 응답<br/>클라이언트로<br/>전송"]

    I["클라이언트"] -->|REQUEST_JOIN_ROOM| B
    B --> J["RoomManager<br/>GetAvailableRoom"]
    J --> K["빈 방<br/>검색"]
    K --> L["GameRoom<br/>AddUser"]
    L --> M["GameRoomUser<br/>생성"]
    M --> N["2명 도달 확인"]
    N -->|2명 도달| O["Game 인스턴스<br/>생성"]
    N -->|아직 1명| P["대기 상태<br/>유지"]

    O --> Q["게임 시작<br/>클라이언트로<br/>알림"]

    style A fill:#e3f2fd
    style B fill:#fce4ec
    style D fill:#f3e5f5
    style E fill:#e1f5ff
    style G fill:#f3e5f5
    style Q fill:#fff3e0
```

---

## 3. 게임 루프 및 상태 동기화 흐름

```mermaid
graph TD
    A["Game<br/>Start"] --> B["게임 루프<br/>타이머 시작<br/>20 TPS"]

    B --> C["Tick 대기<br/>50ms"]
    C --> D["명령 큐<br/>확인"]

    D --> E["명령 처리<br/>3틱 버퍼"]
    E --> F["플레이어 명령<br/>Fleet 이동/공격"]

    F --> G["게임 상태<br/>업데이트"]
    G --> H["Fleet 상태<br/>변경"]
    H --> I["행성 점령<br/>확인"]
    I --> J["플레이어 자원<br/>업데이트"]

    J --> K["타이머 체크"]
    K -->|200ms 경과| L["자원 생산<br/>업데이트"]
    K -->|500ms 경과| M["승리 조건<br/>확인"]

    L --> N["모든 플레이어<br/>상태 동기화"]
    M -->|게임 종료| O["게임 종료<br/>처리"]
    M -->|계속 진행| N

    N --> P["Protocol<br/>생성<br/>게임 상태 정보"]
    P --> Q["ClientSession<br/>전송"]
    Q --> R["클라이언트<br/>수신<br/>화면 업데이트"]

    R --> C

    style A fill:#fff3e0
    style B fill:#fff3e0
    style D fill:#ffe0b2
    style F fill:#e1f5ff
    style L fill:#e8f5e9
    style M fill:#e8f5e9
    style O fill:#ffcdd2
    style R fill:#e3f2fd
```

---

## 4. 함대 이동 및 전투 흐름

```mermaid
graph TD
    A["클라이언트<br/>명령"]

    A --> B{"명령<br/>타입"}

    B -->|이동 명령| C["Fleet<br/>Move"]
    B -->|공격 명령| D["Fleet<br/>Attack"]
    B -->|점령 명령| E["Fleet<br/>OccupyPlanet"]

    C --> F["목표 행성<br/>경로 계산"]
    F --> G["Fleet State<br/>→ Moving"]
    G --> H["매 틱마다<br/>위치 업데이트"]
    H --> I["목표 도달<br/>확인"]
    I -->|도달| J["Fleet State<br/>→ Idle"]
    I -->|미도달| H

    D --> K["대상 Fleet<br/>탐색"]
    K --> L["Fleet State<br/>→ InCombat"]
    L --> M["피해 계산<br/>attackPower"]
    M --> N["대상 Fleet<br/>TakeDamage"]
    N --> O{"생존?"}
    O -->|YES| P["Fleet State<br/>→ Idle"]
    O -->|NO| Q["Fleet 제거"]

    E --> R["목표 행성<br/>도착"]
    R --> S["Fleet State<br/>→ Occupying"]
    S --> T["점령 진행"]
    T --> U["점령 완료"]
    U --> V["행성 소유권<br/>변경"]
    V --> W["GamePlayer<br/>자원 획득"]
    W --> X["Fleet State<br/>→ Idle"]

    J --> Y["상태 업데이트<br/>클라이언트로<br/>전송"]
    P --> Y
    X --> Y

    style A fill:#e3f2fd
    style C fill:#e1f5ff
    style D fill:#ffcdd2
    style E fill:#e8f5e9
    style J fill:#fff9c4
    style X fill:#fff9c4
```

---

## 5. 데이터베이스 상호작용 흐름

```mermaid
graph TB
    subgraph Client["클라이언트"]
        A["로그인 요청"]
        B["게임 데이터 요청"]
    end

    subgraph Server["게임 서버"]
        C["ClientSession"]
        D["인증 시스템"]
        E["게임 로직"]
        F["RoomManager"]
        G["MapManager"]
    end

    subgraph DbLayer["데이터베이스 계층"]
        H["DBManager<br/>싱글톤"]
        I["DB_Auth<br/>인증 DB"]
        J["DB_Table<br/>게임 DB"]
    end

    subgraph MySQL["MySQL"]
        K["interplanetery_authdb<br/>- users<br/>- sessions"]
        L["interplanetery_tabledb<br/>- maps<br/>- planets<br/>- routes<br/>- fleets"]
    end

    A -->|REQUEST_LOGIN| C
    C -->|GetUser| D
    D --> H
    H -->|쿼리<br/>실행| I
    I --> K
    K -->|결과<br/>반환| I
    I --> H
    H --> D
    D --> C

    B -->|REQUEST_JOIN_ROOM| C
    C --> F
    F --> H
    H -->|GetMap| J
    J --> L
    L -->|맵 데이터<br/>반환| J
    J --> H
    H --> F

    E -->|GameLoop<br/>업데이트| G
    G --> H
    H -->|맵 캐시<br/>조회| J
    J -->|로컬<br/>캐시| J

    style A fill:#e3f2fd
    style B fill:#e3f2fd
    style H fill:#f3e5f5
    style I fill:#e8f5e9
    style J fill:#e8f5e9
    style K fill:#c8e6c9
    style L fill:#c8e6c9
```

---

## 6. 프로토콜 처리 흐름

```mermaid
graph TD
    A["TCP 수신<br/>바이트 스트림"]

    A --> B["Protocol<br/>객체 역직렬화"]
    B --> C{"Protocol Type<br/>확인"}

    C -->|HEARTBEAT| D["하트비트<br/>타임스탐프<br/>업데이트"]
    C -->|REQUEST_LOGIN| E["로그인<br/>처리"]
    C -->|REQUEST_REGISTER| F["회원가입<br/>처리"]
    C -->|REQUEST_JOIN_LOBBY| G["로비 입장<br/>처리"]
    C -->|REQUEST_CREATE_ROOM| H["방 생성<br/>처리"]
    C -->|REQUEST_JOIN_ROOM| I["방 참여<br/>처리"]
    C -->|GAME_COMMAND<br/>FLEET_MOVE| J["함대 이동<br/>명령 큐에<br/>추가"]
    C -->|GAME_COMMAND<br/>FLEET_ATTACK| K["함대 공격<br/>명령 큐에<br/>추가"]
    C -->|REQUEST_LOGOUT| L["로그아웃<br/>처리"]

    D --> M["응답<br/>Protocol<br/>생성"]
    E --> M
    F --> M
    G --> M
    H --> M
    I --> M
    J --> N["Game 인스턴스<br/>명령 큐에<br/>저장"]
    K --> N
    L --> M

    N --> O["다음 틱에<br/>처리"]

    M --> P["ProtocolHandler<br/>직렬화"]
    P --> Q["TCP 송신<br/>클라이언트로"]

    style A fill:#e3f2fd
    style B fill:#fff3e0
    style C fill:#ffe0b2
    style J fill:#e1f5ff
    style K fill:#ffcdd2
    style Q fill:#e3f2fd
```

---

## 7. 게임 종료 및 정리 흐름

```mermaid
graph TD
    A{"게임<br/>종료 조건"}

    A -->|타이머 만료| B["게임<br/>강제 종료"]
    A -->|한 플레이어<br/>전멸| C["승자 판정"]
    A -->|한 플레이어<br/>시간 초과| D["상대 승리"]
    A -->|클라이언트<br/>연결 끊김| E["게임<br/>강제 종료"]

    B --> F["Game<br/>Shutdown"]
    C --> F
    D --> F
    E --> F

    F --> G["게임 상태<br/>저장<br/>DB에 기록"]
    G --> H["GameRoom<br/>정리"]
    H --> I["플레이어 통계<br/>업데이트<br/>점수, 승패"]
    I --> J["RoomManager<br/>방 제거<br/>표시"]

    J --> K["ClientSession<br/>정리"]
    K --> L["TCP 연결<br/>종료"]
    L --> M["타임아웃 후<br/>자동 정리"]
    M --> N["메모리<br/>해제"]

    N --> O["클라이언트<br/>로비로<br/>반환"]

    style A fill:#ffcdd2
    style F fill:#ffcdd2
    style G fill:#e8f5e9
    style I fill:#e8f5e9
    style N fill:#fff3e0
    style O fill:#e3f2fd
```

---

## 데이터 흐름 요약표

| 단계 | 발신자 | 수신자 | 데이터 | 목적 |
|------|--------|--------|--------|------|
| 1 | 클라이언트 | ClientSession | TCP Stream | 연결 설정 |
| 2 | ClientSession | ProtocolHandler | Protocol | 프로토콜 해석 |
| 3 | ProtocolHandler | 핸들러 함수 | Protocol 타입 | 적절한 처리기 선택 |
| 4 | 핸들러 | DBManager | 쿼리 | 데이터 조회 |
| 5 | DBManager | DB_Auth/Table | SQL | 데이터베이스 접근 |
| 6 | DB | MySQL | SQL | 데이터 조회/저장 |
| 7 | Game | ClientSession | 게임 상태 | 상태 동기화 |
| 8 | ClientSession | 클라이언트 | Protocol | 화면 업데이트 |

---

## 타이밍 다이어그램

```mermaid
timeline
    title 게임 루프 타이밍 (20 TPS = 50ms 틱)

    section Tick 0
        0ms: 명령 처리 : 게임 상태 업데이트 : 물리 계산

    section Tick 1
        50ms: 명령 처리 : 게임 상태 업데이트 : 물리 계산

    section Tick 2
        100ms: 명령 처리 : 게임 상태 업데이트 : 물리 계산

    section Tick 3
        150ms: 명령 처리 : 게임 상태 업데이트 : 물리 계산

    section Tick 4-50ms
        200ms: 자원 생산 업데이트 (RESOURCE_UPDATE_INTERVAL=4 틱)

    section Tick 5-10
        250ms - 500ms: 일반 게임 루프

    section Tick 10-20ms
        500ms: 승리 조건 확인 (WIN_CHECK_INTERVAL=10 틱)

    section Tick 20+
        1000ms+: 반복...
```

---

## 네트워크 패킷 흐름

```mermaid
graph LR
    A["클라이언트<br/>바이트 전송"] -->|TCP| B["서버 수신<br/>바이트 스트림"]
    B --> C["Protocol<br/>역직렬화"]
    C --> D{"타입<br/>확인"}

    D -->|알려진 타입| E["핸들러<br/>선택"]
    D -->|미지의 타입| F["로그 기록<br/>무시"]

    E --> G["핸들러<br/>실행"]
    G --> H["응답<br/>Protocol<br/>생성"]
    H --> I["Protocol<br/>직렬화"]
    I --> J["바이트 배열"]
    J -->|TCP| K["클라이언트<br/>수신"]
    K --> L["화면<br/>업데이트"]

    F --> M["오류<br/>응답"]
    M --> I

    style A fill:#e3f2fd
    style B fill:#fff3e0
    style D fill:#ffe0b2
    style G fill:#e1f5ff
    style H fill:#f3e5f5
    style K fill:#e3f2fd
```

