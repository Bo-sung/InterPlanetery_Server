# InterPlanetery Server

> **2인 대전 RTS 게임 서버**  
> Unity 게임의 핵심 로직을 서버로 분리하여 멀티플레이어 지원

## 🚀 빠른 시작

### 로컬 개발 환경

```bash
# 1. MySQL 서버 실행 (Docker 사용 권장)
docker-compose up -d mysql

# 2. BaseServer 실행
cd Servers
dotnet run --project BaseServer
```

### Docker로 전체 실행

```bash
# BaseServer + MySQL + phpMyAdmin 모두 실행
docker-compose up -d

# 서버 접속: localhost:11021 (기본 호스트 매핑; 컨테이너 내부 포트는 9000)
# phpMyAdmin: http://localhost:8080
```

> ⚠️ **알려진 한계**: `docker-compose up`은 TableDB(`interplanetery_tabledb_local`)만 자동 생성합니다.
> AuthDB(`interplanetery_authdb_local`)/`userinfo`는 자동 생성되지 않으므로 로그인/회원가입은
> 컨테이너만으로는 동작하지 않습니다 — 로컬 개발에는 위 "로컬 개발 환경" 절차를 사용하세요.

---

## 📁 프로젝트 구조

```
InterPlanetery_Server/
├── Servers/                    # 서버 솔루션
│   ├── BaseServer/            # 🎯 게임 서버 (배포 대상)
│   ├── CommonLib/             # 공통 라이브러리
│   ├── ChatClientWPF/         # WPF 테스트 클라이언트
│   └── TestClient/            # 콘솔 테스트 클라이언트
├── Docs/                      # 📚 문서
├── docker-compose.yml         # Docker 배포 설정
├── Dockerfile                 # BaseServer 이미지
└── README.md                  # 이 파일
```

---

## 🎮 시스템 개요

### 핵심 특징
- **2인 대전 RTS**: 행성 점령 및 함대 전투
- **서버 권위형 스냅샷 동기화**: 매 틱(20 TPS)마다 서버가 전체 게임 상태(`GAME_STATE`)를
  브로드캐스트하는 결정론적 시뮬레이션 — 명령만 주고받는 "락스텝"이 아니라 상태 스냅샷 전송 방식
- **권위 있는 서버**: 모든 게임 상태는 서버가 결정

### 기술 스택
- **서버**: C# (.NET 8.0), TCP
- **DB**: MySQL 8.0
- **프로토콜**: CommonLib.Protocol (바이너리 + JSON)
- **배포**: Docker, Docker Compose

---

## 🛠️ 개발 환경 설정

### 필수 요구사항
- .NET 8.0 SDK
- MySQL 8.0 (또는 Docker)
- Visual Studio 2022 / Rider / VS Code

### 데이터베이스 설정

```bash
# Docker로 MySQL 실행
docker-compose up -d mysql

# 또는 로컬 MySQL 사용 시
# Docs/#DB DDL 모음.sql 파일을 실행하여 테이블 생성
```

### 서버 설정

```bash
cd Servers/BaseServer

# appsettings.json 생성 (처음 한 번만)
cp appsettings.example.json appsettings.json

# appsettings.json 편집하여 DB 연결 정보 수정 (코드 기본값 기준, appsettings.example.json 참고)
# - Server: localhost
# - UserId: root
# - Password: your_password
# - DatabaseName: interplanetery_tabledb_local (TableDB) / interplanetery_authdb_local (AuthDB)
# - Server:Port(빌드 기본값)는 9000 — 로컬 실행 시 클라이언트는 9000으로 접속
```

---

## 🚢 배포

### Docker 배포 (권장)

**BaseServer만 배포**됩니다. WPF 클라이언트는 로컬에서만 실행하세요.

```bash
# 전체 스택 실행 (BaseServer + MySQL + phpMyAdmin)
docker-compose up -d

# BaseServer만 재시작
docker-compose restart interplanetary-server

# 로그 확인
docker-compose logs -f interplanetary-server
```

### Mac 서버 배포

`build-mac.bat`은 존재하지 않으며, `deploy-to-mac.sh`가 빌드(`dotnet publish -r osx-x64`,
Intel 전용, self-contained)와 배포를 함께 수행합니다. ARM(Apple Silicon) 빌드는 스크립트가
지원하지 않습니다 — 실제 사용법은 다음과 같습니다.

```bash
cd Servers

# 사용법: ./deploy-to-mac.sh <mac-user> <mac-ip> <target-path> [--no-build]
./deploy-to-mac.sh <mac-user> <mac-ip> <target-path>

# 예시 (플레이스홀더 값 — 실제 계정/호스트로 교체)
./deploy-to-mac.sh <mac-user> <mac-host> /opt/interplanetary-server
```

자세한 내용은 [Mac 배포 가이드](Docs/Guides/MacDeployment.md) 참조 (해당 문서의 예시 설정에는
포트 11021 / DB 이름 `interplanetery_tabledb`가 남아 있어 코드 기본값(9000 / `_local` 접미사)과
다릅니다 — appsettings.json은 실제 코드 기본값 기준으로 작성하세요).

---

## 📊 서버 포트

| 서비스 | 포트 | 설명 |
|--------|------|------|
| BaseServer (로컬 실행, 코드/appsettings 기본값) | **9000** | 게임 서버 (TCP) — `cd Servers && dotnet run --project BaseServer` |
| BaseServer (Docker 호스트 기본 매핑) | 11021 | `${SERVER_HOST_PORT:-11021}` → 컨테이너 내부 9000 |
| MySQL | 3306 | 데이터베이스 |
| phpMyAdmin | 8080 | DB 관리 도구 (개발용) |

> 포트 7777은 현재 서버 코드에 존재하지 않습니다(과거 채팅 프로토타입의 유물) — `Servers/TestClient`의
> 접속 메뉴 기본값이 7777로 남아 있으니, 접속 시 위 포트(로컬 9000 / Docker 11021)를 직접 입력하세요.

---

## 📚 문서

### 필수 문서
- **[Docs/PROJECT_OVERVIEW.md](Docs/PROJECT_OVERVIEW.md)** - 프로젝트 전체 개요
- **[Docs/interplanetary_system_spec.md](Docs/interplanetary_system_spec.md)** - 시스템 기획서 (핵심,
  일부 수치는 코드와 다를 수 있음 — 정확한 값은 `.analysis/document_audit_server.md` 참조)
- **[Docs/Diagrams/01_SystemArchitecture.md](Docs/Diagrams/01_SystemArchitecture.md)** - 시스템 아키텍처

### 개발 가이드
- [코딩 스타일](Docs/Guides/CodingStyle.md)
- [클라이언트 구현 가이드 (현재 프로토콜, 최신)](Docs/클라이언트_구현_가이드.md)
- ~~[프로토콜 명세](Docs/Guides/ProtocolSpecification.md)~~ / ~~[서버 아키텍처](Docs/Guides/ServerArchitecture.md)~~
  — **ARCHIVED**: 폐기된 2인 채팅 프로토타입(포트 7777, opcode 1001–2999) 문서, 현재 서버와 무관

### 배포 가이드
- [Mac 배포 상세 가이드](Docs/Guides/MacDeployment.md) (`MAC_DEPLOYMENT_QUICKSTART.md`는 저장소에 없음)

전체 문서 목록은 [Docs/README.md](Docs/README.md) 참조

---

## 🎯 개발 단계

| 단계 | 목표 | 상태 |
|------|------|------|
| Phase 1 | CLI 단일 게임 로직 | ✅ 완료 |
| Phase 2 | 네트워크 레이어 추가 | 🚧 진행 중 |
| Phase 3 | Unity 클라이언트 연동 | 📅 예정 |
| Phase 4 | 멀티플레이어 확장 | 📅 예정 |

---

## 🧪 테스트

### WPF 테스트 클라이언트 실행

```bash
cd Servers
dotnet run --project ChatClientWPF
```

### 콘솔 테스트 클라이언트

```bash
cd Servers
dotnet run --project TestClient
# 접속 메뉴 기본 포트는 7777 — 로컬 서버는 9000, Docker는 11021로 직접 입력하세요.
```

### 헤드리스 상태 테스트 (서버/DB 불필요)

```bash
dotnet run --project "Servers/BaseServer.StateTests/BaseServer.StateTests.csproj" -c Debug
# 기대 결과: "Completed 5 tests: 5 passed, 0 failed." (exit code 0)
```

### 헤드리스 2-클라이언트 E2E 테스트 (서버 + 시드된 DB 필요)

```bash
# 사전조건: BaseServer가 127.0.0.1:9000에서 실행 중이고 TableDB/AuthDB가 시드되어 있어야 함.
# --validate-config 로 서버/소켓 없이 필수 환경변수 존재만 검증 가능 (실행/DB 불필요).
set E2E_USER1=<user1> & set E2E_PASSWORD1=<pw1> & set E2E_USER2=<user2> & set E2E_PASSWORD2=<pw2>
dotnet run --project "Servers/E2ETests/E2ETests.csproj" -- --validate-config
dotnet run --project "Servers/E2ETests/E2ETests.csproj"
# 기대 결과: "SUMMARY: mandatory N/N passed" (exit code 0)
```

> `E2ETests`/`BaseServer.StateTests`는 `Servers.sln`에 등록되어 있으나 이전에는 문서화되어 있지
> 않았습니다 (`.analysis/document_audit_server.md` §2.7 MISSING-DOC).

---

## 🔧 문제 해결

### 서버가 시작되지 않을 때
1. MySQL 서버가 실행 중인지 확인
2. `appsettings.json`의 DB 연결 정보 확인 (서버 내부 포트는 **9000**, Docker 호스트 기본 매핑은 11021)
3. 방화벽에서 사용 중인 포트(로컬 9000 / Docker 11021) 허용 확인

### Docker 컨테이너 문제
```bash
# 컨테이너 상태 확인
docker-compose ps

# 로그 확인
docker-compose logs interplanetary-server

# 컨테이너 재시작
docker-compose restart

# 완전히 재빌드
docker-compose down
docker-compose up -d --build
```

---

## 📝 라이선스

이 프로젝트는 개인 프로젝트입니다.

---

## 👥 기여

현재 개인 프로젝트로 진행 중입니다.

---

**프로젝트**: InterPlanetery Server  
**버전**: Phase 2 (네트워크 레이어)  
**최종 업데이트**: 2025-11-23
