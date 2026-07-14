# InterPlanetery Server - 프로젝트 개요

## 📋 프로젝트 소개

**InterPlanetery**는 Unity 게임의 핵심 로직을 서버로 분리하여 멀티플레이어를 지원하는 2인 대전 RTS(Real-Time Strategy) 게임 서버 시스템입니다.

### 핵심 특징
- **권위 있는 서버(Authoritative Server)**: 모든 게임 상태는 서버가 관리하고 결정
- **결정론적 시뮬레이션**: 동일한 초기 상태 + 동일한 입력 = 동일한 결과
- **락스텝(Lockstep) 동기화**: 모든 클라이언트가 동일한 틱에서 동일한 명령 실행
- **고정 틱 레이트**: 20 TPS (50ms/틱)로 예측 가능한 게임 진행

---

## 🏗️ 시스템 아키텍처

### 전체 구조
```
InterPlanetery_Server/
├── Servers/                    # 서버 솔루션
│   ├── BaseServer/            # TCP 기반 게임 서버 (핵심)
│   ├── CommonLib/             # 공통 라이브러리 (프로토콜, 데이터 모델)
│   ├── ChatClientWPF/         # WPF 테스트 클라이언트
│   └── TestClient/            # 콘솔 테스트 클라이언트
├── Docs/                      # 문서 폴더
│   ├── Guides/                # 개발 가이드
│   └── Archives/              # 아카이브 문서
└── README.md                  # 프로젝트 루트 README
```

### 계층 구조
1. **클라이언트 계층**: Unity/WPF 클라이언트
2. **네트워크 계층**: TCP/IP (Port 9000), 프로토콜 처리
3. **프레젠테이션 계층**: ClientSession (클라이언트 연결 관리)
4. **비즈니스 로직 계층**: 인증, 로비, 게임 루프
5. **게임 엔티티 계층**: GameRoom, Fleet, Planet, Player
6. **관리자 계층**: RoomManager, MapManager (싱글톤)
7. **데이터베이스 계층**: DBManager, MySQL

---

## 🎮 게임 시스템

### 핵심 게임플레이
- **2인 대전 RTS**: 행성 점령 및 함대 전투
- **자원 관리**: 광물(Minerals), 가스(Gas), 보급품(Supply)
- **함대 시스템**: Scout, Fighter, Cruiser, Battleship
- **승리 조건**: 상대 모성(Homeworld) 점령

### 게임 루프 (20 TPS)
```
매 틱(50ms):
1. 명령 처리 (Command Processing)
2. 자원 생산 (200ms마다)
3. 함대 생산 (Production Queue)
4. 함대 이동 (Fleet Movement)
5. 전투 처리 (Combat Resolution)
6. 점령 처리 (Conquest Update)
7. AI 업데이트 (1초마다)
8. 승리 조건 확인 (500ms마다)
9. 이벤트 브로드캐스트
```

---

## 🗄️ 데이터베이스 구조

### 현재 사용 중인 테이블
- **map_info**: 맵 정보 (player1_homeworld_id, player2_homeworld_id)
- **planet_info**: 행성 정보 (name, mineral, gas, supply)
- **map_planet_info**: 맵별 행성 배치 (position_x, position_y)
- **map_route_info**: 행성 간 연결 정보
- **fleet_info**: 함대 기본 스탯 정보
- **production_info**: 생산 정보

### DB 연결 정보
- **MySQL 주소**: localhost:3306
- **게임 데이터 DB**: interplanetery_tabledb_local
- **인증 DB**: interplanetery_authdb_local

---

## 🌐 네트워크 프로토콜

### 통신 방식
- **프로토콜**: TCP 기반
- **직렬화**: CommonLib.Protocol (바이너리 + JSON)
- **포트**: 9000

### 프로토콜 구조
```
헤더: [Length(4)][Type(4)][Timestamp(8)][DataCount(2)]
데이터: Key-Value 형식의 바이너리 직렬화
```

### 주요 프로토콜 타입
- **연결**: CONNECT, CONNECTED, HEARTBEAT
- **인증**: REQUEST_LOGIN, REQUEST_REGISTER
- **로비**: GET_ROOM_LIST, CREATE_ROOM, JOIN_ROOM
- **게임**: GAME_STARTED, SUBMIT_COMMAND, GAME_STATE_UPDATE

---

## 📊 주요 다이어그램

### 시스템 아키텍처
- [01_SystemArchitecture.md](./Servers/BaseServer/Diagrams/01_SystemArchitecture.md)
- 전체 시스템 계층 구조 및 상호작용

### 클래스 다이어그램
- [02_ClassDiagram.md](./Servers/BaseServer/Diagrams/02_ClassDiagram.md)
- 주요 클래스 관계 및 구조

### 데이터 흐름도
- [03_DataFlow.md](./Servers/BaseServer/Diagrams/03_DataFlow.md)
- 클라이언트 연결, 게임 루프, 전투 흐름 등

---

## 🛠️ 기술 스택

### 서버
- **언어**: C# (.NET 8.0)
- **서버**: TCP 소켓 기반
- **데이터베이스**: MySQL
- **동기화**: 락스텝(Lockstep)

### 클라이언트
- **테스트 클라이언트**: WPF (MVP 패턴)
- **최종 클라이언트**: Unity (Phase 3 예정)

### 공통 라이브러리
- **CommonLib**: 프로토콜, 데이터 모델, 그래프 자료구조

---

## 📚 문서 구조

### 개발 가이드
- [코딩 스타일 가이드](./Guides/CodingStyle.md)
- [프로토콜 명세서](./Guides/ProtocolSpecification.md)
- [프로토콜 사용 가이드](./Guides/ProtocolUsageGuide.md)
- [서버 아키텍처 문서](./Guides/ServerArchitecture.md)
- [Graph 자료구조 사용 가이드](./Guides/Graph%20자료구조%20사용%20가이드.md)

### 배포 가이드
- [🍎 Mac 서버 배포 빠른 시작](./MAC_DEPLOYMENT_QUICKSTART.md)
- [Mac 서버 배포 상세 가이드](./Guides/MacDeployment.md)

### 시스템 명세
- [Interplanetary 서버 시스템 기획서](./interplanetary_system_spec.md)
- [WPF 테스트 클라이언트 명세](./interplanetary_test_client_spec.md)
- [UI 데이터 요구사항](./ui_data_requirements.md)

### 데이터베이스
- [DB DDL 모음](#DB%20DDL%20모음.sql)

---

## 🚀 빠른 시작

### 서버 실행
```bash
cd Servers
dotnet run --project BaseServer
```

### 클라이언트 실행
```bash
cd Servers
dotnet run --project ChatClientWPF
```

### Mac 배포
자세한 내용은 [Mac 배포 빠른 시작](./MAC_DEPLOYMENT_QUICKSTART.md) 참조

---

## 🔧 주요 설정

### 게임 설정
- **틱 레이트**: 20 TPS (50ms)
- **자원 생산 주기**: 1.0초
- **점령 속도**: 10%/초
- **전투 판정 주기**: 1.0초

### 네트워크 설정
- **TCP 서버 주소**: 127.0.0.1:9000
- **클라이언트 타임아웃**: 30초
- **명령 버퍼**: 3틱

---

## 👥 주요 컴포넌트

### BaseServer
- **역할**: 게임 서버 핵심 로직
- **주요 클래스**: 
  - `Game`: 게임 루프 및 상태 관리
  - `GameRoom`: 2인 대전 방 관리
  - `RoomManager`: 룸 생성/관리 (싱글톤)
  - `ClientSession`: 클라이언트 연결 관리

### CommonLib
- **역할**: 서버-클라이언트 공통 라이브러리
- **주요 기능**:
  - Protocol: 네트워크 프로토콜
  - 데이터 모델: Planet, Fleet, Player
  - Graph: 행성 경로 탐색

### ChatClientWPF
- **역할**: WPF 기반 테스트 클라이언트
- **패턴**: MVP (Model-View-Presenter)
- **기능**: 로비, 게임 UI, 실시간 동기화

---

## 📝 참고 사항

### 모성(Homeworld)의 특수 기능
- **함대 생산**: 모든 함대는 오직 모성에서만 생산 가능
- **승패 조건**: 상대 모성을 점령하면 승리
- **초기 자원**: 게임 시작 시 안정적인 자원 제공

### 자원 시스템
- **광물(Minerals)**: 모성에서 기본 수급, 확장으로 증가
- **가스(Gas)**: 중립 행성 점령 필요, 고급 함대 생산에 필수
- **보급품(Supply)**: 행성 점령으로 최대치 증가

---

## 🔗 관련 링크

- [프로젝트 루트 README](../README.md)
- [서버 설정 가이드](../Servers/README.appsettings.md)
- [BaseServer 다이어그램](../Servers/BaseServer/Diagrams/)

---

**마지막 업데이트**: 2025-11-23
