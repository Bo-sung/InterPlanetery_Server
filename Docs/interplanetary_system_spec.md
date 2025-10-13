# Interplanetary 서버 시스템 기획서

## 📑 목차
1. [문서 개요](#1-문서-개요)
2. [시스템 아키텍처](#2-시스템-아키텍처)
3. [데이터 구조 설계](#3-데이터-구조-설계)
4. [핵심 시스템 명세](#4-핵심-시스템-명세)
5. [CLI 테스트 환경](#5-cli-테스트-환경)
6. [네트워크 프로토콜](#6-네트워크-프로토콜)
7. [구현 로드맵](#7-구현-로드맵)

---

## 1. 문서 개요

### 1.1 프로젝트 목표
- Unity 게임의 핵심 로직을 서버로 분리하여 멀티플레이어 지원
- CLI 환경에서 게임 로직 테스트 및 검증
- 확장 가능하고 유지보수 용이한 구조 설계

### 1.2 개발 단계

| 단계 | 목표 | 산출물 |
|------|------|--------|
| **Phase 1** | CLI 단일 게임 로직 구현 | 로컬 게임 서버 |
| **Phase 2** | 네트워크 레이어 추가 | 로컬 멀티 서버 |
| **Phase 3** | Unity 클라이언트 연동 | 통합 프로토타입 |
| **Phase 4** | 멀티플레이어 확장 | 정식 서비스 |

### 1.3 기술 스택
- **언어**: C# (.NET 6.0+)
- **서버 프레임워크**: ASP.NET Core (REST API) 또는 Unity Mirror (게임 서버)
- **통신 프로토콜**: WebSocket / TCP
- **데이터 포맷**: JSON
- **테스트**: CLI 콘솔 애플리케이션

---

## 2. 시스템 아키텍처

### 2.1 전체 구조도

```
┌─────────────────────────────────────────────────────────┐
│                    GAME SERVER                          │
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │          Core Game Engine                        │  │
│  │  - GameState Manager (게임 상태 총괄)            │  │
│  │  - Game Loop (Tick 기반 업데이트)                │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────┐  │
│  │   Map/       │  │   Fleet      │  │  Resource   │  │
│  │   Planet     │  │   Manager    │  │  Manager    │  │
│  │   Manager    │  │              │  │             │  │
│  └──────────────┘  └──────────────┘  └─────────────┘  │
│                                                         │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────┐  │
│  │   Combat     │  │  Conquest    │  │     AI      │  │
│  │   System     │  │  System      │  │  Controller │  │
│  └──────────────┘  └──────────────┘  └─────────────┘  │
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │          Command Processor                       │  │
│  │  - 플레이어 명령 큐 관리                          │  │
│  │  - 명령 유효성 검증                               │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │          Event System                            │  │
│  │  - 게임 이벤트 발생 및 브로드캐스트               │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                            ↕
┌─────────────────────────────────────────────────────────┐
│                  Network Layer                          │
│  - WebSocket / TCP 서버                                 │
│  - 클라이언트 세션 관리                                  │
│  - 메시지 직렬화/역직렬화                                │
└─────────────────────────────────────────────────────────┘
                            ↕
┌─────────────────────────────────────────────────────────┐
│               Unity Client (Multiple)                   │
│  - Rendering & Animation                                │
│  - Input Handling                                       │
│  - UI/UX                                                │
└─────────────────────────────────────────────────────────┘
```

### 2.2 설계 원칙

#### 2.2.1 권위 있는 서버 (Authoritative Server)
- 모든 게임 상태는 서버가 관리하고 결정
- 클라이언트는 입력만 전송하고 결과만 수신
- 치트 방지 및 공정한 게임 진행 보장

#### 2.2.2 결정론적 시뮬레이션 (Deterministic Simulation)
- 동일한 초기 상태 + 동일한 입력 = 동일한 결과
- 리플레이 기능 구현 가능
- 디버깅 용이

#### 2.2.3 명령 패턴 (Command Pattern)
- 모든 플레이어 행동은 Command 객체로 캡슐화
- 명령 큐를 통한 순차 처리
- 명령 취소, 재실행 가능

#### 2.2.4 이벤트 기반 (Event-Driven)
- 상태 변화는 Event로 브로드캐스트
- 느슨한 결합(Loose Coupling)
- 클라이언트 동기화 용이

#### 2.2.5 틱 기반 업데이트 (Tick-Based Update)
- 고정된 시간 간격으로 게임 상태 업데이트 (예: 50ms = 20 TPS)
- 네트워크 지연에 강건한 구조
- 예측 가능한 동작

---

## 3. 데이터 구조 설계

### 3.1 핵심 엔티티 정의

#### 3.1.1 Planet (행성)

| 속성 | 타입 | 설명 |
|------|------|------|
| Id | string | 고유 식별자 (UUID) |
| Name | string | 행성 이름 |
| Position | Vector2 | 맵 좌표 (x, y) |
| OwnerId | string? | 소유 플레이어 ID (null = 중립) |
| ConquestProgress | float | 점령도 (0~100) |
| Resources | ResourceBonus | 제공 자원 |
| GarrisonFleetId | string? | 주둔 함대 ID |
| AdjacentPlanetIds | List<string> | 인접 행성 ID 목록 |
| IsHomeworld | bool | 모성 여부 |

**ResourceBonus 구조**
- MineralPerSecond: int (광물 생산량/초)
- GasPerSecond: int (가스 생산량/초)
- SupplyCapacity: int (보급품 최대치 증가량)

#### 3.1.2 Fleet (함대)

| 속성 | 타입 | 설명 |
|------|------|------|
| Id | string | 고유 식별자 |
| Type | FleetType | 함대 종류 (Scout/Fighter/Cruiser/Battleship) |
| OwnerId | string | 소유 플레이어 ID |
| CurrentHealth | int | 현재 체력 |
| MaxHealth | int | 최대 체력 |
| AttackPower | int | 공격력 |
| MoveSpeed | float | 이동 속도 |
| Location | FleetLocation | 위치 정보 |
| State | FleetState | 상태 (Idle/Garrison/Moving/InCombat/Constructing) |
| ControlGroupNumber | int? | 부대 번호 (1~5) |

**FleetLocation 구조**
- Type: LocationType (OnPlanet / InTransit)
- PlanetId: string (행성에 있을 때)
- Route: RouteInfo (이동 중일 때)
  - FromPlanetId: string
  - ToPlanetId: string
  - Progress: float (0~1)
  - StartTime: float

#### 3.1.3 Player (플레이어)

| 속성 | 타입 | 설명 |
|------|------|------|
| Id | string | 고유 식별자 |
| Name | string | 플레이어 이름 |
| Type | PlayerType | Human / AI |
| Resources | ResourcePool | 보유 자원 |
| OwnedPlanetIds | List<string> | 소유 행성 목록 |
| HomeworldId | string | 모성 ID |
| FleetIds | List<string> | 소유 함대 목록 |
| ProductionQueue | Queue<ProductionOrder> | 생산 대기열 |
| IsDefeated | bool | 패배 여부 |

**ResourcePool 구조**
- Minerals: float (현재 광물)
- Gas: float (현재 가스)
- CurrentSupply: int (현재 보급품 사용량)
- MaxSupply: int (최대 보급품)
- MineralRate: float (광물 생산량/초)
- GasRate: float (가스 생산량/초)

#### 3.1.4 GameState (게임 상태)

| 속성 | 타입 | 설명 |
|------|------|------|
| GameId | string | 게임 세션 ID |
| Phase | GamePhase | 게임 단계 (Lobby/Loading/Playing/Ended) |
| GameTime | float | 경과 시간 (초) |
| TickCount | long | 틱 카운터 |
| MapId | string | 맵 ID |
| Players | Dictionary<string, Player> | 플레이어 목록 |
| Planets | Dictionary<string, Planet> | 행성 목록 |
| Fleets | Dictionary<string, Fleet> | 함대 목록 |
| WinnerId | string? | 승자 ID |

### 3.2 게임 설정 (Config)

#### 3.2.1 FleetConfig (함대 종류별 설정)

| 함대 타입 | 체력 | 공격력 | 이동속도 | 광물 | 가스 | 보급 | 생산시간 |
|----------|------|--------|----------|------|------|------|----------|
| Scout | 50 | 10 | 2.0 | 50 | 0 | 1 | 5초 |
| Fighter | 100 | 20 | 1.5 | 100 | 25 | 2 | 10초 |
| Cruiser | 200 | 40 | 1.0 | 200 | 75 | 3 | 20초 |
| Battleship | 400 | 80 | 0.7 | 400 | 150 | 5 | 40초 |

#### 3.2.2 GameConfig (전역 설정)

| 설정 항목 | 값 | 설명 |
|----------|-----|------|
| TickRate | 20 TPS | 초당 틱 횟수 (50ms) |
| ResourceTickRate | 1.0초 | 자원 생산 주기 |
| ConquestRatePerSecond | 10% | 점령 속도 (%/초) |
| CombatTickRate | 1.0초 | 전투 판정 주기 |
| AIProductionInterval | 2.0초 | AI 생산 판단 주기 |
| AICommandInterval | 1.0초 | AI 명령 실행 주기 |

**시작 자원**
- Minerals: 50
- Gas: 0
- MaxSupply: 10
- MineralRate: 5/초 (모성 제공)
- GasRate: 0/초

---

## 4. 핵심 시스템 명세

### 4.1 Game Loop System

#### 4.1.1 개요
- 고정된 시간 간격(50ms)으로 게임 상태 업데이트
- 모든 시스템을 순차적으로 실행

#### 4.1.2 업데이트 순서
```
1. 명령 처리 (Command Processing)
   ↓
2. 자원 생산 (Resource Production)
   ↓
3. 함대 생산 (Fleet Production)
   ↓
4. 함대 이동 (Fleet Movement)
   ↓
5. 전투 처리 (Combat Resolution)
   ↓
6. 점령 처리 (Conquest Update)
   ↓
7. AI 업데이트 (AI Decision Making)
   ↓
8. 승리 조건 확인 (Victory Check)
   ↓
9. 이벤트 브로드캐스트 (Event Broadcasting)
```

#### 4.1.3 틱 관리
- **고정 틱 레이트**: 20 TPS (50ms/틱)
- **deltaTime**: 항상 0.05초로 고정
- **틱 카운터**: 게임 시작부터 누적

### 4.2 Resource Management System

#### 4.2.1 자원 생산
- **주기**: 1초마다
- **계산식**: 
  - `현재 자원 += 생산률 × deltaTime`
  - 생산률 = 소유한 모든 행성의 자원 보너스 합계

#### 4.2.2 자원 소비
- **함대 생산 시**: 즉시 차감
- **자원 부족 시**: 명령 거부
- **보급품**: 함대 생산 시 증가, 파괴 시 감소

#### 4.2.3 자원 재계산 트리거
- 행성 점령/상실
- 게임 시작 시

### 4.3 Fleet Production System

#### 4.3.1 생산 요청 처리

**검증 단계**
1. 자원 충분 여부 확인
2. 보급품 여유 확인
3. 모성에 함대 주둔 여부 확인
4. 이미 생산 중인지 확인

**생산 시작**
1. 자원 즉시 차감
2. ProductionQueue에 추가
3. 완료 시간 계산 (현재시간 + 생산시간)

#### 4.3.2 생산 완료 처리

**완료 조건**
- `현재시간 >= 완료시간`

**완료 시**
1. Queue에서 제거
2. 모성에 함대 생성
3. 함대 ID를 플레이어에게 추가
4. 모성 GarrisonFleetId 설정
5. 이벤트 발생

#### 4.3.3 생산 취소
- CLI 버전: 지원 안 함
- 멀티 버전: 자원 일부 환불 (50%)

### 4.4 Fleet Movement System

#### 4.4.1 이동 명령 검증

**실패 조건**
- 이미 이동 중인 함대
- 인접하지 않은 행성
- 타인 소유 함대

**성공 시**
1. 상태를 Moving으로 변경
2. Route 정보 설정
3. 현재 행성에서 함대 제거

#### 4.4.2 이동 진행

**진행도 계산**
```
거리 = Distance(출발행성, 도착행성)
이동시간 = 거리 / 함대속도
진행도 = 경과시간 / 이동시간
```

**매 틱마다**
- 진행도 업데이트
- 충돌 감지 (경로상 다른 함대)
- 도착 확인 (진행도 >= 1.0)

#### 4.4.3 도착 처리

**경우의 수**
1. **행성에 적 함대 있음** → 전투 시작
2. **행성에 아군 함대 있음** → 출발지로 복귀
3. **행성이 비어있음** → 주둔 시작

#### 4.4.4 이동 중 충돌

**감지 조건**
- 같은 두 행성을 연결하는 경로
- 반대 방향 이동
- 진행도가 비슷함 (±10%)

**충돌 처리**
- **아군 함대**: 둘 다 출발지로 복귀
- **적군 함대**: 중간 지점에서 전투 시작

### 4.5 Combat System

#### 4.5.1 전투 시작 조건
- 행성 도착 시 적 함대 존재
- 이동 중 적 함대와 충돌

#### 4.5.2 전투 진행

**전투 주기**: 1초마다

**데미지 계산**
```
함대A.현재체력 -= 함대B.공격력
함대B.현재체력 -= 함대A.공격력
```

**전투 종료 조건**
1. **한쪽만 파괴**: 생존자 승리
2. **동시 파괴**: 상호 파괴
3. **도주**: 미지원 (CLI 버전)

#### 4.5.3 전투 결과 처리

**함대 파괴**
1. Fleets에서 제거
2. Player.FleetIds에서 제거
3. 보급품 반환
4. 행성 GarrisonFleetId 제거
5. 이벤트 발생

**승리 함대**
- 행성에서 전투: 해당 행성 주둔
- 이동 중 전투: 원래 목적지로 계속 이동

### 4.6 Conquest System

#### 4.6.1 점령 진행

**조건**
- 행성에 함대 주둔 중
- 모성이 아니거나 이미 점령된 모성

**점령도 변화**
```
변화량 = ConquestRatePerSecond × deltaTime (10%/초)
```

**경우의 수**
1. **중립 행성**: 점령도 증가 → 100% 도달 시 점령
2. **적 행성**: 점령도 감소 → 0% 도달 시 중립화 → 다시 증가 시작
3. **아군 행성**: 변화 없음

#### 4.6.2 점령 완료

**처리 절차**
1. 소유권 변경
2. 플레이어 OwnedPlanetIds 업데이트
3. 자원 생산률 재계산
4. 이벤트 발생

#### 4.6.3 모성 점령

**특수 처리**
- 원래 소유자 패배 처리
- IsDefeated = true
- 플레이어 패배 이벤트 발생

### 4.7 AI System

#### 4.7.1 AI 행동 주기
- **생산 판단**: 2초마다
- **함대 명령**: 1초마다

#### 4.7.2 함대 생산 로직

**판단 순서**
1. 이미 생산 중? → 중단
2. 모성에 함대 있음? → 중단
3. 생산 가능한 함대 목록 조회 (강력한 순)
4. 자원 충족하는 가장 강력한 함대 생산

**우선순위**
1. Battleship
2. Cruiser
3. Fighter
4. Scout

#### 4.7.3 함대 명령 로직

**각 함대마다**
1. 명령 가능 상태 확인 (Garrison 상태)
2. 인접 행성 분석
3. 목표 행성 선택
4. 이동 명령 실행

**목표 선택 우선순위**
1. 적 소유 행성 (공격)
2. 중립 행성 (확장)
3. 함대 없는 아군 행성 (재배치)
4. 정해진 순찰 경로

#### 4.7.4 AI 난이도 조절
- **Easy**: 생산 간격 3초, 명령 간격 2초
- **Normal**: 기본 설정
- **Hard**: 생산 간격 1초, 명령 간격 0.5초, 자원 보너스 +20%

### 4.8 Victory System

#### 4.8.1 승리 조건
- 상대 플레이어의 모성 점령

#### 4.8.2 패배 조건
- 자신의 모성이 점령됨
- IsDefeated = true

#### 4.8.3 게임 종료 처리
1. GamePhase를 Ended로 변경
2. WinnerId 설정
3. 게임 종료 이벤트 발생
4. 통계 기록 (게임 시간, 생산한 함대 수 등)

---

## 5. CLI 테스트 환경

### 5.1 CLI 구조

#### 5.1.1 실행 모드
```
┌─────────────────────────────────────┐
│         Main Menu                   │
├─────────────────────────────────────┤
│  1. New Game (vs AI)                │
│  2. Load Game                       │
│  3. Settings                        │
│  4. Exit                            │
└─────────────────────────────────────┘
```

#### 5.1.2 게임 화면 레이아웃
```
================== INTERPLANETARY CLI ==================
Game Time: 00:05:32          Tick: 6640

┌─────────────── RESOURCES ───────────────┐
│ Minerals: 450 (+15/s)                   │
│ Gas: 120 (+5/s)                         │
│ Supply: 12/25                           │
└─────────────────────────────────────────┘

┌─────────────── MAP ─────────────────────┐
│  [P1: Alpha★]──[P2: Beta]               │
│       │            │                     │
│  [P3: Gamma]──[P4: Delta★]              │
│                                          │
│  ★ = Homeworld                          │
│  [ ] = Neutral  [P] = Player            │
│  [A] = AI                                │
└─────────────────────────────────────────┘

┌─────────────── FLEETS ──────────────────┐
│  1. Scout     @ Alpha    [HP: 50/50]    │
│  2. Fighter   → Beta     [Moving 45%]   │
│  3. Cruiser   @ Gamma    [HP: 180/200]  │
└─────────────────────────────────────────┘

┌─────────────── PRODUCTION ──────────────┐
│  Battleship building... [25s remaining]  │
└─────────────────────────────────────────┘

┌─────────────── COMMANDS ────────────────┐
│  [P] Produce  [M] Move  [S] Status      │
│  [G] Control Group  [N] Next Turn       │
│  [Q] Quit                                │
└─────────────────────────────────────────┘

>
```

### 5.2 명령어 시스템

#### 5.2.1 생산 명령 (P)
```
> P
Select fleet type:
  1. Scout (50M, 0G, 1S) - 5s
  2. Fighter (100M, 25G, 2S) - 10s
  3. Cruiser (200M, 75G, 3S) - 20s
  4. Battleship (400M, 150G, 5S) - 40s
  0. Cancel
>
```

#### 5.2.2 이동 명령 (M)
```
> M
Select fleet (ID or control group):
> 1

Current location: Alpha
Adjacent planets:
  1. Beta (Neutral, 50% conquered)
  2. Gamma (Player)
Select destination:
> 1

Fleet #1 moving Alpha → Beta
```

#### 5.2.3 상태 조회 (S)
```
> S
Select:
  1. Planet details
  2. Fleet details
  3. Player stats
  4. Game summary
>
```

#### 5.2.4 부대 지정 (G)
```
> G
Select fleet:
> 1

Assign to control group (1-5):
> 1

Fleet #1 assigned to group 1
```

#### 5.2.5 시간 진행

**실시간 모드** (Phase 1)
- 자동으로 틱 진행
- 0.05초마다 업데이트
- 명령은 언제든 입력 가능

**턴 기반 모드** (Phase 1 대체안)
- N 키로 수동 틱 진행
- 각 턴마다 1초 시뮬레이션
- 디버깅 용이

### 5.3 디버그 기능

#### 5.3.1 로그 레벨
- **ERROR**: 치명적 오류
- **WARN**: 경고 (명령 실패 등)
- **INFO**: 주요 이벤트 (함대 생산, 행성 점령)
- **DEBUG**: 상세 정보 (틱마다 상태)

#### 5.3.2 저장/불러오기

**저장 형식**: JSON
```json
{
  "gameId": "game_12345",
  "gameTime": 332.5,
  "tickCount": 6650,
  "players": [...],
  "planets": [...],
  "fleets": [...]
}
```

#### 5.3.3 치트 명령 (개발용)
- `/god` - 무한 자원
- `/spawn <type>` - 즉시 함대 생성
- `/tp <fleet> <planet>` - 순간이동
- `/win` - 즉시 승리

### 5.4 테스트 시나리오

#### 5.4.1 기본 기능 테스트
1. **자원 생산**: 1분간 대기 후 자원 증가 확인
2. **함대 생산**: 각 타입별 생산 및 생성 확인
3. **함대 이동**: 인접/비인접 행성 이동 테스트
4. **전투**: 적 함대와 조우 시 전투 결과
5. **점령**: 중립/적 행성 점령 과정

#### 5.4.2 엣지 케이스 테스트
1. **자원 부족**: 생산 시도 → 거부
2. **동시 도착**: 두 함대가 같은 행성 도착
3. **중간 충돌**: 반대 방향 이동 함대
4. **모성 방어**: AI가 모성 공격 시
5. **생산 중 모성 점령**: 생산 큐 처리

#### 5.4.3 성능 테스트
- 100개 행성, 200개 함대 동시 시뮬레이션
- 1시간 실시간 게임 안정성
- 저장/불러오기 대용량 데이터

---

## 6. 네트워크 프로토콜

### 6.1 통신 구조

#### 6.1.1 프로토콜 선택

**Phase 2: WebSocket**
- 양방향 실시간 통신
- JSON 메시지 형식
- 웹 브라우저 호환

**Phase 4: TCP (선택)**
- 더 낮은 지연시간
- 바이너리 프로토콜
- 모바일 최적화

#### 6.1.2 메시지 구조
```json
{
  "messageType": "Command|Event|Sync",
  "timestamp": 1234567890,
  "payload": { ... }
}
```

### 6.2 메시지 타입 정의

#### 6.2.1 클라이언트 → 서버 (Commands)

**연결/인증**
```json
{
  "messageType": "Command",
  "commandType": "Connect",
  "payload": {
    "playerId": "player_123",
    "playerName": "Player1",
    "authToken": "jwt_token_here"
  }
}
```

**게임 참가**
```json
{
  "messageType": "Command",
  "commandType": "JoinGame",
  "payload": {
    "gameId": "game_456",
    "teamId": 1
  }
}
```

**함대 생산**
```json
{
  "messageType": "Command",
  "commandType": "ProduceFleet",
  "payload": {
    "fleetType": "Fighter"
  }
}
```

**함대 이동**
```json
{
  "messageType": "Command",
  "commandType": "MoveFleet",
  "payload": {
    "fleetId": "fleet_789",
    "targetPlanetId": "planet_012"
  }
}
```

**부대 지정**
```json
{
  "messageType": "Command",
  "commandType": "AssignControlGroup",
  "payload": {
    "fleetId": "fleet_789",
    "groupNumber": 1
  }
}
```

#### 6.2.2 서버 → 클라이언트 (Events)

**연결 성공**
```json
{
  "messageType": "Event",
  "eventType": "Connected",
  "payload": {
    "playerId": "player_123",
    "serverTime": 1234567890
  }
}
```

**게임 시작**
```json
{
  "messageType": "Event",
  "eventType": "GameStarted",
  "payload": {
    "gameId": "game_456",
    "mapId": "map_3lanes",
    "players": [
      {"id": "player_123", "name": "Player1"},
      {"id": "ai_001", "name": "AI"}
    ]
  }
}
```

**자원 업데이트**
```json
{
  "messageType": "Event",
  "eventType": "ResourcesUpdated",
  "payload": {
    "playerId": "player_123",
    "minerals": 450,
    "gas": 120,
    "currentSupply": 12,
    "maxSupply": 25
  }
}
```

**함대 생성**
```json
{
  "messageType": "Event",
  "eventType": "FleetSpawned",
  "payload": {
    "fleet": {
      "id": "fleet_789",
      "type": "Fighter",
      "ownerId": "player_123",
      "locationPlanetId": "planet_001"
    }
  }
}
```

**함대 이동 시작**
```json
{
  "messageType": "Event",
  "eventType": "FleetMoving",
  "payload": {
    "fleetId": "fleet_789",
    "fromPlanetId": "planet_001",
    "toPlanetId": "planet_005",
    "estimatedArrival": 345.8
  }
}
```

**전투 시작**
```json
{
  "messageType": "Event",
  "eventType": "CombatStarted",
  "payload": {
    "combatId": "combat_111",
    "attackerFleetId": "fleet_789",
    "defenderFleetId": "fleet_555",
    "locationPlanetId": "planet_005"
  }
}
```

**전투 틱**
```json
{
  "messageType": "Event",
  "eventType": "CombatTick",
  "payload": {
    "combatId": "combat_111",
    "attackerHealth": 80,
    "defenderHealth": 60
  }
}
```

**전투 종료**
```json
{
  "messageType": "Event",
  "eventType": "CombatEnded",
  "payload": {
    "combatId": "combat_111",
    "winnerId": "fleet_789",
    "loserId": "fleet_555"
  }
}
```

**행성 점령**
```json
{
  "messageType": "Event",
  "eventType": "PlanetCaptured",
  "payload": {
    "planetId": "planet_005",
    "newOwnerId": "player_123",
    "previousOwnerId": null
  }
}
```

**게임 종료**
```json
{
  "messageType": "Event",
  "eventType": "GameEnded",
  "payload": {
    "winnerId": "player_123",
    "reason": "HomeworldCaptured",
    "gameDuration": 1234.5
  }
}
```

#### 6.2.3 상태 동기화 (Sync)

**전체 상태 동기화**
```json
{
  "messageType": "Sync",
  "syncType": "FullState",
  "payload": {
    "gameState": {
      "gameTime": 332.5,
      "tickCount": 6650,
      "players": [...],
      "planets": [...],
      "fleets": [...]
    }
  }
}
```

**증분 업데이트**
```json
{
  "messageType": "Sync",
  "syncType": "DeltaUpdate",
  "payload": {
    "tickCount": 6651,
    "changes": [
      {
        "type": "FleetMoved",
        "fleetId": "fleet_789",
        "progress": 0.52
      }
    ]
  }
}
```

### 6.3 네트워크 최적화

#### 6.3.1 대역폭 최적화
- **이벤트 배칭**: 여러 이벤트를 하나의 메시지로 묶음
- **증분 업데이트**: 변경된 부분만 전송
- **압축**: JSON → MessagePack 또는 Protobuf

#### 6.3.2 지연 보상
- **클라이언트 예측**: 입력 즉시 로컬 시뮬레이션
- **서버 조정**: 차이 발생 시 부드럽게 보정
- **보간**: 이동 중인 오브젝트 위치 보간

#### 6.3.3 동기화 전략
- **Tick-based Sync**: N 틱마다 전체 상태 동기화 (예: 100틱)
- **Event-driven**: 중요한 이벤트만 즉시 전송
- **Snapshot Interpolation**: 두 스냅샷 사이 보간

---

## 7. 구현 로드맵

### 7.1 Phase 1: CLI 단일 게임 로직 (4주)

#### Week 1: 기반 구조
- [ ] 프로젝트 구조 설정
- [ ] 데이터 모델 구현 (Planet, Fleet, Player, GameState)
- [ ] GameConfig 구현
- [ ] 기본 GameLoop 구현

**산출물**
- 빈 게임 상태 생성
- 틱 기반 업데이트 작동

#### Week 2: 핵심 시스템 구현
- [ ] Resource Manager 구현
- [ ] Fleet Production Manager 구현
- [ ] Fleet Movement Manager 구현
- [ ] Combat System 구현

**산출물**
- 자원 생산/소비 작동
- 함대 생산 작동
- 함대 이동 및 전투 작동

#### Week 3: 게임 로직 완성
- [ ] Conquest System 구현
- [ ] AI Controller 구현
- [ ] Victory System 구현
- [ ] Event System 구현

**산출물**
- AI와 대전 가능
- 승패 판정 작동

#### Week 4: CLI 인터페이스 & 테스트
- [ ] CLI 화면 구현
- [ ] 명령어 파서 구현
- [ ] 저장/불러오기 구현
- [ ] 통합 테스트

**산출물**
- 완전히 작동하는 CLI 게임
- 테스트 결과 문서

### 7.2 Phase 2: 네트워크 레이어 (3주)

#### Week 5: 네트워크 기반
- [ ] WebSocket 서버 구현
- [ ] 세션 관리자 구현
- [ ] Command/Event 직렬화
- [ ] 메시지 큐 구현

**산출물**
- 클라이언트 연결 및 메시지 송수신

#### Week 6: 멀티플레이어 로직
- [ ] 게임 로비 시스템
- [ ] 명령 검증 및 실행
- [ ] 상태 동기화 구현
- [ ] 재연결 처리

**산출물**
- 로컬 네트워크 멀티플레이 가능

#### Week 7: 최적화 & 테스트
- [ ] 네트워크 최적화
- [ ] 지연 보상 구현
- [ ] 부하 테스트
- [ ] 버그 수정

**산출물**
- 안정적인 멀티플레이어 서버

### 7.3 Phase 3: Unity 클라이언트 연동 (4주)

#### Week 8-9: 기본 클라이언트
- [ ] Unity 프로젝트 설정
- [ ] 네트워크 클라이언트 구현
- [ ] 맵 렌더링
- [ ] 행성 및 함대 표시

**산출물**
- 게임 상태 시각화

#### Week 10-11: UI & 상호작용
- [ ] 플레이어 입력 처리
- [ ] 함대 선택/이동 UI
- [ ] 생산 UI
- [ ] HUD (자원, 미니맵 등)

**산출물**
- 완전한 게임 플레이 가능

### 7.4 Phase 4: 멀티플레이어 확장 (진행 중)

#### 추가 기능
- [ ] 매치메이킹 시스템
- [ ] 랭킹/리더보드
- [ ] 리플레이 시스템
- [ ] 관전 모드
- [ ] 팀전 (2v2, 3v3)
- [ ] 커스텀 맵 에디터

#### 운영 인프라
- [ ] 전용 서버 호스팅
- [ ] 모니터링 시스템
- [ ] 로그 수집/분석
- [ ] 자동 스케일링

---

## 8. 기술적 고려사항

### 8.1 성능 목표
- **틱 레이트**: 20 TPS 안정적 유지
- **동시 접속**: 최소 100 게임 (200 플레이어)
- **응답 시간**: 명령 → 결과 100ms 이내
- **메모리**: 게임당 50MB 이하

### 8.2 확장성
- **수평 확장**: 게임 세션별 분산 가능
- **상태 관리**: Redis 등 외부 저장소 활용 가능
- **로드 밸런싱**: 게임 서버 간 부하 분산

### 8.3 보안
- **인증**: JWT 기반 토큰 인증
- **권한**: 자신 소유 함대만 조작 가능
- **검증**: 모든 명령 서버에서 재검증
- **치트 방지**: 클라이언트 예측과 서버 상태 비교

### 8.4 디버깅 & 모니터링
- **상세 로깅**: 모든 명령과 이벤트 기록
- **리플레이**: 게임 재생 가능
- **메트릭**: 게임 시간, 명령 수, 오류율 등
- **프로파일링**: 성능 병목 지점 분석

---

## 9. 참고 자료

### 9.1 관련 문서
- 원본 게임 기획서 (Interplanetary 기말 과제)
- Unity C# 스타일 가이드
- ASP.NET Core 웹소켓 문서
- Mirror Networking 문서

### 9.2 유사 프로젝트 분석
- **StarCraft II**: 멀티플레이어 RTS 네트워킹
- **Age of Empires**: 결정론적 시뮬레이션
- **Neptune's Pride**: 웹 기반 실시간 전략
- **Galcon**: 단순화된 행성 정복 게임

### 9.3 기술 스택 문서
- .NET 6.0 Documentation
- WebSocket Protocol (RFC 6455)
- JSON Serialization Best Practices
- Game Server Architecture Patterns

---

## 10. 용어 정의

| 용어 | 정의 |
|------|------|
| **Tick** | 게임 상태 업데이트 한 주기 (50ms) |
| **TPS** | Ticks Per Second (초당 틱 수) |
| **Command** | 플레이어가 서버에 보내는 명령 |
| **Event** | 서버가 클라이언트에게 보내는 상태 변화 |
| **Garrison** | 행성에 주둔 중인 상태 |
| **Conquest** | 행성 점령 과정 |
| **Homeworld** | 모성 (시작 행성) |
| **Control Group** | 부대 번호 지정 (1~5) |
| **Deterministic** | 결정론적 (같은 입력 = 같은 결과) |
| **Authoritative** | 권위 있는 (서버가 진실의 원천) |

---

## 부록 A: 맵 설계 예시

### A.1 3-Lane Map (기본 맵)
```
        [중립1]
          |
[P모성]─[중립2]─[중립3]─[AI모성]
          |
        [중립4]
```

**행성 정보**
- P모성: Mineral +5/s, Supply +10
- AI모성: Mineral +5/s, Supply +10
- 중립1: Gas +3/s, Supply +5
- 중립2: Mineral +3/s, Supply +3
- 중립3: Mineral +3/s, Supply +3
- 중립4: Gas +3/s, Supply +5

### A.2 Complex Map (고급 맵)
```
    [중립1]─[중립2]
       |  ╳  |
[P모성]─[중립3]─[AI모성]
       |      |
    [중립4]─[중립5]
```

**특징**
- 교차 경로로 전략적 깊이 증가
- 자원 분포 비대칭

---

## 부록 B: 샘플 CLI 세션

```
$ dotnet run

================== INTERPLANETARY CLI ==================
Welcome to Interplanetary!

Main Menu:
  1. New Game (vs AI)
  2. Load Game
  3. Exit

> 1

Select difficulty:
  1. Easy
  2. Normal
  3. Hard

> 2

Loading map: 3-Lane...
Game started!

================== INTERPLANETARY CLI ==================
Game Time: 00:00:00          Tick: 0

Your homeworld: Alpha
Enemy homeworld: Delta

> P
Select fleet type:
  1. Scout (50M, 0G, 1S) - 5s
> 1

Producing Scout... (5s)

> [waiting 5 seconds]

Scout spawned at Alpha!

> M
Select fleet:
> 1

Adjacent planets:
  1. Beta (Neutral)
> 1

Scout moving Alpha → Beta...

> [waiting 3 seconds]

Scout arrived at Beta!
Conquering Beta... (10s to capture)

> [waiting 10 seconds]

Beta captured!
Minerals: 50 → 53/s (+3 from Beta)

> Q
Saving game...
Game saved to: save_20250113_143022.json
Goodbye!
```

---

## 문서 버전 이력

| 버전 | 날짜 | 변경 내용 |
|------|------|-----------|
| 1.0 | 2025-01-13 | 초안 작성 |

---

**문서 작성자**: System Architect  
**최종 수정일**: 2025-01-13  
**문서 상태**: Draft