# Mac 서버 배포 가이드

## 📋 목차
1. [배포 방법 선택](#배포-방법-선택)
2. [방법 1: 자체 포함 배포 (Self-Contained)](#방법-1-자체-포함-배포-self-contained)
3. [방법 2: 프레임워크 의존 배포](#방법-2-프레임워크-의존-배포)
4. [방법 3: Docker 배포](#방법-3-docker-배포)
5. [Mac 서버 설정](#mac-서버-설정)
6. [문제 해결](#문제-해결)

---

## 배포 방법 선택

| 방법 | 장점 | 단점 | 추천 대상 |
|------|------|------|-----------|
| **자체 포함 배포** | .NET 설치 불필요, 독립 실행 | 파일 크기 큼 (~70MB) | ⭐ 대부분의 경우 |
| **프레임워크 의존** | 파일 크기 작음 (~5MB) | .NET 8.0 설치 필요 | .NET이 이미 설치된 환경 |
| **Docker** | 환경 독립적, 관리 용이 | Docker 설치 필요 | 프로덕션 환경 |

---

## 방법 1: 자체 포함 배포 (Self-Contained)

### ⭐ 추천: Mac에 .NET이 없어도 실행 가능

### 1단계: Windows에서 Mac용 빌드

```bash
# BaseServer 디렉토리로 이동
cd H:\Git\InterPlanetery_Server\Servers\BaseServer

# Mac (Intel) 용 빌드
dotnet publish -c Release -r osx-x64 --self-contained true -o ./publish/osx-x64

# Mac (Apple Silicon M1/M2/M3) 용 빌드
dotnet publish -c Release -r osx-arm64 --self-contained true -o ./publish/osx-arm64
```

**빌드 결과 위치:**
- Intel Mac: `H:\Git\InterPlanetery_Server\Servers\BaseServer\publish\osx-x64\`
- Apple Silicon Mac: `H:\Git\InterPlanetery_Server\Servers\BaseServer\publish\osx-arm64\`

### 2단계: Mac으로 파일 전송

**방법 A: SCP 사용 (추천)**
```bash
# Windows PowerShell에서 실행
scp -r ./publish/osx-arm64/* username@mac-server-ip:/path/to/server/

# 예시
scp -r ./publish/osx-arm64/* admin@192.168.1.100:/home/admin/interplanetary-server/
```

**방법 B: 압축 후 전송**
```bash
# 1. 압축 (Windows)
Compress-Archive -Path ./publish/osx-arm64/* -DestinationPath interplanetary-server.zip

# 2. Mac으로 전송 (SCP, FTP, 또는 클라우드 스토리지)
scp interplanetary-server.zip username@mac-server-ip:/path/to/server/

# 3. Mac에서 압축 해제
unzip interplanetary-server.zip
```

### 3단계: Mac에서 실행 권한 부여

```bash
# Mac 터미널에서 실행
cd /path/to/server/
chmod +x BaseServer
```

### 4단계: appsettings.json 설정

```bash
# appsettings.example.json 복사
cp appsettings.example.json appsettings.json

# 설정 파일 편집
nano appsettings.json
```

**appsettings.json 예시:**
```json
{
  "Database": {
    "Server": "your-mysql-server-ip",
    "UserId": "your-username",
    "Password": "your-password",
    "DatabaseName": "interplanetery_tabledb",
    "Port": 3306
  },
  "Server": {
    "Host": "0.0.0.0",
    "Port": 11021
  },
  "Logging": {
    "LogLevel": "Info"
  }
}
```

### 5단계: 서버 실행

```bash
# 직접 실행
./BaseServer

# 백그라운드 실행 (nohup)
nohup ./BaseServer > server.log 2>&1 &

# 백그라운드 실행 (screen)
screen -S interplanetary
./BaseServer
# Ctrl+A, D로 detach
```

---

## 방법 2: 프레임워크 의존 배포

### Mac에 .NET 8.0이 설치되어 있는 경우

### 1단계: Mac에 .NET 8.0 설치

```bash
# Homebrew 사용
brew install dotnet@8

# 또는 공식 설치 프로그램
# https://dotnet.microsoft.com/download/dotnet/8.0
```

### 2단계: 프레임워크 의존 빌드

```bash
# Windows에서 실행
cd H:\Git\InterPlanetery_Server\Servers\BaseServer

# Mac용 빌드 (프레임워크 의존)
dotnet publish -c Release -r osx-arm64 --self-contained false -o ./publish/osx-arm64-fdd
```

### 3단계: Mac으로 전송 및 실행

```bash
# Mac에서 실행
dotnet BaseServer.dll
```

---

## 방법 3: Docker 배포

### ⭐ 프로덕션 환경 추천

### 1단계: Dockerfile 생성

프로젝트 루트에 `Dockerfile` 생성:

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["Servers/CommonLib/CommonLib.csproj", "CommonLib/"]
COPY ["Servers/BaseServer/BaseServer.csproj", "BaseServer/"]
RUN dotnet restore "BaseServer/BaseServer.csproj"

# Copy everything else and build
COPY Servers/CommonLib/ CommonLib/
COPY Servers/BaseServer/ BaseServer/
WORKDIR /src/BaseServer
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Copy appsettings
COPY Servers/BaseServer/appsettings.example.json ./appsettings.json

EXPOSE 11021
ENTRYPOINT ["dotnet", "BaseServer.dll"]
```

### 2단계: Docker 이미지 빌드

```bash
# Windows에서 실행
cd H:\Git\InterPlanetery_Server

# 이미지 빌드
docker build -t interplanetary-server:latest .

# 이미지 저장 (Mac으로 전송용)
docker save -o interplanetary-server.tar interplanetary-server:latest
```

### 3단계: Mac으로 전송 및 로드

```bash
# Windows에서 Mac으로 전송
scp interplanetary-server.tar username@mac-server-ip:/path/to/

# Mac에서 이미지 로드
docker load -i interplanetary-server.tar
```

### 4단계: Docker Compose 설정 (선택)

`docker-compose.yml` 생성:

```yaml
version: '3.8'

services:
  interplanetary-server:
    image: interplanetary-server:latest
    container_name: interplanetary-server
    ports:
      - "11021:11021"
    environment:
      - Database__Server=mysql-server
      - Database__UserId=root
      - Database__Password=yourpassword
      - Database__DatabaseName=interplanetery_tabledb
      - Database__Port=3306
      - Server__Host=0.0.0.0
      - Server__Port=11021
    restart: unless-stopped
    networks:
      - interplanetary-network

  mysql:
    image: mysql:8.0
    container_name: mysql-server
    environment:
      MYSQL_ROOT_PASSWORD: yourpassword
      MYSQL_DATABASE: interplanetery_tabledb
    ports:
      - "3306:3306"
    volumes:
      - mysql-data:/var/lib/mysql
    restart: unless-stopped
    networks:
      - interplanetary-network

networks:
  interplanetary-network:
    driver: bridge

volumes:
  mysql-data:
```

### 5단계: Docker 실행

```bash
# Mac에서 실행
docker-compose up -d

# 로그 확인
docker-compose logs -f interplanetary-server

# 중지
docker-compose down
```

---

## Mac 서버 설정

### 1. 방화벽 설정

```bash
# 포트 11021 열기 (macOS 방화벽)
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --add /path/to/BaseServer
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --unblockapp /path/to/BaseServer
```

### 2. 자동 시작 설정 (launchd)

`~/Library/LaunchAgents/com.interplanetary.server.plist` 생성:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.interplanetary.server</string>
    <key>ProgramArguments</key>
    <array>
        <string>/path/to/server/BaseServer</string>
    </array>
    <key>WorkingDirectory</key>
    <string>/path/to/server</string>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
    <key>StandardOutPath</key>
    <string>/path/to/server/logs/stdout.log</string>
    <key>StandardErrorPath</key>
    <string>/path/to/server/logs/stderr.log</string>
</dict>
</plist>
```

**launchd 등록:**
```bash
# 등록
launchctl load ~/Library/LaunchAgents/com.interplanetary.server.plist

# 시작
launchctl start com.interplanetary.server

# 중지
launchctl stop com.interplanetary.server

# 등록 해제
launchctl unload ~/Library/LaunchAgents/com.interplanetary.server.plist
```

### 3. systemd 대안 (Homebrew services)

```bash
# Homebrew services 사용
brew services start interplanetary-server
brew services stop interplanetary-server
brew services restart interplanetary-server
```

---

## 문제 해결

### Q1: "Permission denied" 오류

```bash
# 실행 권한 부여
chmod +x BaseServer
```

### Q2: "libssl" 또는 "libcrypto" 오류

```bash
# OpenSSL 설치
brew install openssl@3

# 심볼릭 링크 생성
sudo ln -s /opt/homebrew/opt/openssl@3/lib/libssl.3.dylib /usr/local/lib/
sudo ln -s /opt/homebrew/opt/openssl@3/lib/libcrypto.3.dylib /usr/local/lib/
```

### Q3: MySQL 연결 실패

```bash
# MySQL 서버 확인
mysql -h your-server-ip -u username -p

# 방화벽 확인
telnet your-server-ip 3306

# MySQL 원격 접속 허용 확인
# MySQL에서 실행:
# GRANT ALL PRIVILEGES ON *.* TO 'username'@'%' IDENTIFIED BY 'password';
# FLUSH PRIVILEGES;
```

### Q4: 포트가 이미 사용 중

```bash
# 포트 사용 확인
lsof -i :11021

# 프로세스 종료
kill -9 <PID>
```

### Q5: "Segmentation fault" 오류

```bash
# 올바른 아키텍처로 빌드했는지 확인
# Intel Mac: osx-x64
# Apple Silicon: osx-arm64

# Mac 아키텍처 확인
uname -m
# x86_64 = Intel
# arm64 = Apple Silicon
```

### Q6: appsettings.json을 찾을 수 없음

```bash
# 현재 디렉토리 확인
pwd

# appsettings.json이 실행 파일과 같은 디렉토리에 있는지 확인
ls -la

# 없으면 복사
cp appsettings.example.json appsettings.json
```

---

## 배포 체크리스트

### 빌드 전
- [ ] 올바른 Mac 아키텍처 확인 (Intel vs Apple Silicon)
- [ ] appsettings.example.json 준비
- [ ] MySQL 데이터베이스 준비

### 빌드
- [ ] `dotnet publish` 명령 실행
- [ ] 빌드 에러 없음 확인
- [ ] publish 폴더 생성 확인

### 전송
- [ ] Mac 서버로 파일 전송
- [ ] 실행 권한 부여 (`chmod +x`)
- [ ] appsettings.json 설정

### 실행
- [ ] MySQL 연결 테스트
- [ ] 포트 11021 열림 확인
- [ ] 서버 정상 시작 확인
- [ ] 클라이언트 연결 테스트

### 프로덕션
- [ ] 자동 시작 설정 (launchd)
- [ ] 로그 모니터링 설정
- [ ] 백업 스크립트 설정
- [ ] 방화벽 규칙 설정

---

## 유용한 명령어

### 서버 상태 확인
```bash
# 프로세스 확인
ps aux | grep BaseServer

# 포트 확인
lsof -i :11021

# 네트워크 연결 확인
netstat -an | grep 11021
```

### 로그 확인
```bash
# 실시간 로그 (nohup 사용 시)
tail -f server.log

# 실시간 로그 (Docker 사용 시)
docker logs -f interplanetary-server
```

### 성능 모니터링
```bash
# CPU/메모리 사용량
top -pid $(pgrep BaseServer)

# 네트워크 트래픽
nettop -p BaseServer
```

---

## 추가 리소스

- [.NET 8.0 다운로드](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker 설치 (Mac)](https://docs.docker.com/desktop/install/mac-install/)
- [MySQL 설치 (Mac)](https://dev.mysql.com/doc/refman/8.0/en/macos-installation.html)

---

[⬅️ 서버 README로 돌아가기](../README.md)
