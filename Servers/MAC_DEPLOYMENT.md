# Mac 서버 배포 가이드

이 문서는 Windows에서 빌드한 서버를 Mac으로 배포하는 방법을 설명합니다.

## 사전 요구사항

- Windows에서 .NET 8.0 SDK 설치
- Mac 서버에 SSH 접근 권한
- rsync 설치 (Windows에서 Git Bash 또는 WSL 사용 시 기본 포함)

## 1단계: Mac용 빌드

### Windows에서 실행

```batch
# 둘 다 빌드 (Intel + Apple Silicon)
build-mac.bat both

# Intel Mac만 빌드
build-mac.bat intel

# Apple Silicon만 빌드
build-mac.bat arm
```

### 빌드 결과

- `BaseServer/publish/osx-x64/` - Intel Mac용
- `BaseServer/publish/osx-arm64/` - Apple Silicon용
- 각 폴더에 `appsettings.example.json` 파일이 자동으로 복사됨
- **중요**: `appsettings.json`은 빌드 결과에 포함되지 않음 (로컬 설정 보호)

## 2단계: Mac 서버로 배포

### Git Bash 또는 WSL에서 실행

```bash
# 배포 스크립트 실행 권한 부여 (처음 한 번만)
chmod +x deploy-to-mac.sh

# Apple Silicon Mac에 배포
./deploy-to-mac.sh <mac-user> <mac-ip> <target-path> arm

# Intel Mac에 배포
./deploy-to-mac.sh <mac-user> <mac-ip> <target-path> intel
```

### 예시

```bash
# Apple Silicon Mac 배포 예시
./deploy-to-mac.sh admin 192.168.1.100 /home/admin/gameserver arm

# Intel Mac 배포 예시
./deploy-to-mac.sh admin 192.168.1.100 /home/admin/gameserver intel
```

## 3단계: Mac에서 설정

### SSH로 Mac 서버 접속

```bash
ssh <mac-user>@<mac-ip>
cd <target-path>
```

### appsettings.json 생성 및 편집

```bash
# appsettings.example.json을 appsettings.json으로 복사
cp appsettings.example.json appsettings.json

# 설정 파일 편집
nano appsettings.json
```

### appsettings.json 설정 예시

```json
{
  "ServerConfig": {
    "IP": "0.0.0.0",
    "Port": 7777,
    "MaxConnections": 100
  },
  "Database": {
    "Host": "localhost",
    "Port": 3306,
    "Database": "gameserver",
    "UserId": "your_user",
    "Password": "your_password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

## 4단계: 서버 실행

```bash
# 실행 권한 부여 (처음 한 번만)
chmod +x BaseServer

# 서버 실행
./BaseServer
```

## 배포 스크립트 동작 방식

### build-mac.bat
1. Mac용 .NET 런타임을 포함한 독립 실행형 빌드 생성
2. `appsettings.example.json`을 publish 폴더에 복사
3. `appsettings.json`은 **제외** (로컬 설정 보호)

### deploy-to-mac.sh
1. Mac 서버에 대상 디렉토리 생성
2. **rsync를 사용하여 파일 전송 (`appsettings.json` 제외)**
3. BaseServer 실행 파일에 실행 권한 부여
4. `appsettings.json`이 없으면 `appsettings.example.json`에서 자동 생성

## 장점

✅ **보안**: 로컬 개발 환경의 `appsettings.json` (DB 비밀번호 등)이 서버에 덮어씌워지지 않음
✅ **안전성**: 서버의 기존 설정이 배포로 인해 삭제되지 않음
✅ **편리성**: 최초 배포 시 `appsettings.example.json`에서 자동으로 설정 파일 생성
✅ **버전 관리**: 설정 예시는 Git에 포함되지만, 실제 설정은 제외

## 주의사항

⚠️ **최초 배포 후 반드시 `appsettings.json`을 편집하여 올바른 DB 정보 입력**
⚠️ **재배포 시 서버의 `appsettings.json`은 유지됨**
⚠️ **설정을 초기화하려면 Mac에서 수동으로 `appsettings.json` 삭제 후 재배포**

## 트러블슈팅

### rsync를 찾을 수 없음 (Windows)
- Git Bash 또는 WSL을 사용하세요
- 또는 SCP 방식으로 수동 전송:
  ```bash
  scp -r BaseServer/publish/osx-arm64/* user@ip:/path/
  ssh user@ip "rm /path/appsettings.json"  # appsettings.json 삭제
  ```

### Permission denied (publickey)
- SSH 키 설정 필요:
  ```bash
  ssh-keygen -t rsa
  ssh-copy-id user@mac-ip
  ```

### Database connection failed
- Mac에서 MySQL/MariaDB가 실행 중인지 확인
- `appsettings.json`의 DB 정보가 올바른지 확인
- 방화벽 설정 확인

## 참고

- 빌드된 파일은 self-contained 이므로 Mac에 .NET 런타임 설치 불필요
- Apple Silicon과 Intel은 서로 호환되지 않으므로 올바른 아키텍처로 빌드 필요
