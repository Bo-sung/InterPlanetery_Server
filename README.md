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

# 서버 접속: localhost:11021
# phpMyAdmin: http://localhost:8080
```

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
- **락스텝 동기화**: 결정론적 시뮬레이션 (20 TPS)
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

# appsettings.json 편집하여 DB 연결 정보 수정
# - Server: localhost
# - UserId: root
# - Password: your_password
# - DatabaseName: interplanetery_tabledb
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

```bash
# 1. Windows에서 Mac용 빌드
cd Servers
build-mac.bat arm    # Apple Silicon
# 또는
build-mac.bat intel  # Intel Mac

# 2. Mac 서버로 배포
deploy-to-mac.sh [mac-user] [mac-ip] [target-path] [arm|intel]

# 예시
deploy-to-mac.sh admin 192.168.1.100 /home/admin/server arm
```

자세한 내용은 [Mac 배포 가이드](Docs/MAC_DEPLOYMENT_QUICKSTART.md) 참조

---

## 📊 서버 포트

| 서비스 | 포트 | 설명 |
|--------|------|------|
| BaseServer | 11021 | 게임 서버 (TCP) |
| MySQL | 3306 | 데이터베이스 |
| phpMyAdmin | 8080 | DB 관리 도구 (개발용) |

---

## 📚 문서

### 필수 문서
- **[Docs/PROJECT_OVERVIEW.md](Docs/PROJECT_OVERVIEW.md)** - 프로젝트 전체 개요
- **[Docs/interplanetary_system_spec.md](Docs/interplanetary_system_spec.md)** - 시스템 기획서 (핵심)
- **[Docs/01_SystemArchitecture.md](Docs/01_SystemArchitecture.md)** - 시스템 아키텍처

### 개발 가이드
- [코딩 스타일](Docs/Guides/CodingStyle.md)
- [프로토콜 명세](Docs/Guides/ProtocolSpecification.md)
- [서버 아키텍처](Docs/Guides/ServerArchitecture.md)

### 배포 가이드
- [Mac 배포 빠른 시작](Docs/MAC_DEPLOYMENT_QUICKSTART.md)
- [Mac 배포 상세 가이드](Docs/Guides/MacDeployment.md)

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
```

---

## 🔧 문제 해결

### 서버가 시작되지 않을 때
1. MySQL 서버가 실행 중인지 확인
2. `appsettings.json`의 DB 연결 정보 확인
3. 방화벽에서 포트 11021 허용 확인

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
