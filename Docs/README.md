# InterPlanetery Server - 문서 가이드

> **프로젝트**: 2인 대전 RTS 게임 서버  
> **기술 스택**: C# (.NET 8.0), TCP, MySQL  
> **최종 업데이트**: 2025-11-23

---

## 🚀 빠른 시작

### 처음 오셨나요?
1. **[PROJECT_OVERVIEW.md](./PROJECT_OVERVIEW.md)** - 프로젝트 전체 개요부터 읽어보세요
2. **[interplanetary_system_spec.md](./interplanetary_system_spec.md)** - 시스템 기획서 (핵심 문서)
3. **[Guides/CodingStyle.md](./Guides/CodingStyle.md)** - 코딩 스타일 가이드

### 개발자용
- 서버 아키텍처: [Diagrams/01_SystemArchitecture.md](./Diagrams/01_SystemArchitecture.md)
- 프로토콜 명세 (현재, 최신): [클라이언트_구현_가이드.md](./클라이언트_구현_가이드.md)
- 배포 가이드: [Guides/MacDeployment.md](./Guides/MacDeployment.md)

---

## 📚 문서 목록

### 핵심 문서

| 문서 | 설명 | 우선순위 |
|------|------|---------|
| [PROJECT_OVERVIEW.md](./PROJECT_OVERVIEW.md) | 프로젝트 전체 개요 및 구조 | ⭐⭐⭐ |
| [interplanetary_system_spec.md](./interplanetary_system_spec.md) | 시스템 기획서 (아키텍처, 게임 로직, DB 설계) — 일부 수치는 실제 코드와 다를 수 있음 | ⭐⭐⭐ |
| [Diagrams/01_SystemArchitecture.md](./Diagrams/01_SystemArchitecture.md) | 시스템 아키텍처 다이어그램 (7계층) | ⭐⭐ |
| [클라이언트_구현_가이드.md](./클라이언트_구현_가이드.md) | 현재 프로토콜/DB 명세 (최신) | ⭐⭐⭐ |

> `interplanetary_test_client_spec.md`는 저장소에 존재하지 않습니다 (참조 제거).

### 개발 가이드 (`Guides/`)

| 문서 | 설명 |
|------|------|
| [CodingStyle.md](./Guides/CodingStyle.md) | C# 코딩 컨벤션 및 스타일 가이드 |
| [MacDeployment.md](./Guides/MacDeployment.md) | Mac 서버 배포 상세 가이드 |
| [Graph 자료구조 사용 가이드.md](./Guides/Graph%20자료구조%20사용%20가이드.md) | 행성 연결 그래프 자료구조 |

### 배포 가이드

> `MAC_DEPLOYMENT_QUICKSTART.md`는 저장소에 존재하지 않습니다 — [Guides/MacDeployment.md](./Guides/MacDeployment.md)를 사용하세요.

### 데이터베이스

| 파일 | 설명 |
|------|------|
| [#DB DDL 모음.sql](./%23DB%20DDL%20모음.sql) | MySQL 테이블 DDL 스크립트 |

### 아카이브 (`Archives/`)

과거 버전 또는 참고용 문서들입니다. 폐기된 2인 채팅 프로토타입(포트 7777, opcode 1001–2999) 문서
`ServerArchitecture.md`/`ProtocolSpecification.md`/`ProtocolUsageGuide.md`/`ChatClientWPF - MVP 패턴
채팅 클라이언트.md`가 여기로 이동되었으며, 각 파일 상단에 ARCHIVED 배너가 있습니다.
`INTEGRATED_DESIGN_DOCUMENT.md`는 승인 대기 중인 미구현 로드맵(JWT/BCrypt/EF/Redis/gRPC, 별도
opcode 체계)이며 현재 코드에 반영되지 않았습니다.

### 테스트 실행 (`Servers/`)

| 명령 | 설명 | 사전조건 |
|------|------|----------|
| `dotnet run --project Servers/BaseServer.StateTests/BaseServer.StateTests.csproj` | 헤드리스 상태 테스트 5종 | 없음 (서버/DB 불필요) |
| `dotnet run --project Servers/E2ETests/E2ETests.csproj -- --validate-config` | 환경변수/설정 검증만 (소켓 미개방) | 없음 |
| `dotnet run --project Servers/E2ETests/E2ETests.csproj` | 헤드리스 2-클라이언트 E2E (`E2E_USER1/E2E_PASSWORD1/E2E_USER2/E2E_PASSWORD2` env 필요) | 실행 중인 BaseServer + 시드된 TableDB/AuthDB + 계정 2개 |

두 프로젝트 모두 `Servers.sln`에 등록되어 있습니다.

---

## 🎯 학습 경로

### 1단계: 프로젝트 이해
```
PROJECT_OVERVIEW.md
    ↓
interplanetary_system_spec.md (1~3장)
    ↓
01_SystemArchitecture.md
```

### 2단계: 개발 준비
```
Guides/CodingStyle.md
    ↓
Guides/ProtocolSpecification.md
    ↓
Guides/ProtocolUsageGuide.md
```

### 3단계: 구현
```
interplanetary_system_spec.md (4~7장)
    ↓
실제 코드 작성
    ↓
interplanetary_test_client_spec.md (테스트)
```

---

## 🏗️ 프로젝트 구조

```
InterPlanetery_Server/
├── Servers/                    # 서버 솔루션
│   ├── BaseServer/            # TCP 게임 서버 (핵심)
│   ├── CommonLib/             # 공통 라이브러리
│   ├── ChatClientWPF/         # WPF 테스트 클라이언트
│   └── TestClient/            # 콘솔 테스트 클라이언트
├── Docs/                      # 📚 이 폴더
└── README.md                  # 프로젝트 루트 README
```

---

## 🎮 시스템 개요

### 게임 특징
- **2인 대전 RTS**: 행성 점령 및 함대 전투
- **서버 권위형 스냅샷 동기화**: 결정론적 시뮬레이션 (20 TPS), 매 틱 전체 게임 상태(`GAME_STATE`)
  브로드캐스트 — 명령만 전송하는 "락스텝"이 아님
- **권위 있는 서버**: 모든 게임 상태는 서버가 결정

### 기술 스택
- **서버**: C# (.NET 8.0), TCP (Port 9000)
- **DB**: MySQL (localhost:3306)
- **프로토콜**: CommonLib.Protocol (바이너리 + JSON)
- **클라이언트**: WPF (테스트), Unity (예정)

### 개발 단계

| 단계 | 목표 | 상태 |
|------|------|------|
| Phase 1 | CLI 단일 게임 로직 | ✅ 완료 |
| Phase 2 | 네트워크 레이어 추가 | 🚧 진행 중 |
| Phase 3 | Unity 클라이언트 연동 | 📅 예정 |
| Phase 4 | 멀티플레이어 확장 | 📅 예정 |

---

## 📖 자주 찾는 내용

### 게임 시스템
- [게임 엔티티 정의](./interplanetary_system_spec.md#31-핵심-엔티티-정의) (Planet, Fleet, Player)
- [게임 루프 시스템](./interplanetary_system_spec.md#41-game-loop-system)
- [전투 시스템](./interplanetary_system_spec.md#45-combat-system)

### 네트워크
- [프로토콜 명세 (현재, 최신)](./클라이언트_구현_가이드.md)
- [통신 구조](./interplanetary_system_spec.md#61-통신-구조)

### 데이터베이스
- [DB 테이블 구조](./interplanetary_system_spec.md#32-데이터베이스-구조)
- [DDL 스크립트](./%23DB%20DDL%20모음.sql)

---

## 💡 도움말

### 문서를 찾을 수 없나요?
1. 이 README의 목차를 확인하세요
2. `Guides/` 폴더를 확인하세요
3. `Archives/` 폴더에 과거 문서가 있을 수 있습니다

### 문서가 최신 버전인지 확인하려면?
```bash
git log --oneline Docs/
```

### 새 문서를 추가하려면?
1. 적절한 폴더 선택 (루트, Guides, Archives)
2. 문서 작성 (Markdown 형식)
3. 이 README에 링크 추가

---

## 🔗 관련 링크

- [프로젝트 루트 README](../README.md)
- [BaseServer 소스코드](../Servers/BaseServer/)

> `Servers/README.appsettings.md`는 저장소에 존재하지 않습니다 — 서버 설정은 루트
> [README.md](../README.md)의 "서버 설정" 절과 `Servers/BaseServer/appsettings.example.json`을 참고하세요.

---

**프로젝트**: InterPlanetery Server  
**버전**: Phase 2 (네트워크 레이어)  
**최종 업데이트**: 2025-11-23
