# CLI 테스트 클라이언트

InterPlanetery 채팅 서버를 테스트하기 위한 CLI 기반 테스트 클라이언트입니다.

## 🚀 실행 방법

### 방법 1: dotnet run 사용
```bash
cd Servers/TestClient
dotnet run
```

### 방법 2: 빌드 후 실행
```bash
cd Servers/TestClient
dotnet build
./bin/Debug/net8.0/TestClient.exe  # Windows
./bin/Debug/net8.0/TestClient      # Linux/Mac
```

## 📝 사용 방법

### 1. 서버 연결
프로그램을 실행하면 서버 정보를 입력하라는 메시지가 나타납니다:
```
Server Host [127.0.0.1]:
Server Port [7777]:
```
엔터를 누르면 기본값(127.0.0.1:7777)으로 연결됩니다.

### 2. 사용 가능한 명령어

| 명령어 | 설명 |
|--------|------|
| `help`, `?` | 명령어 도움말 표시 |
| `join` | 채팅 룸에 입장 |
| `leave` | 현재 룸에서 퇴장 |
| `send <message>` | 채팅 메시지 전송 (단축키: `s`) |
| `quit`, `exit` | 연결 해제 및 종료 |

### 3. 사용 예시

```
> join
[Sent] JOIN_ROOM request
[SUCCESS] Joined room 'ROOM_0001' (Players: 1)

> send Hello, World!
[Sent] Hello, World!
[user123] Hello, World!

> s Hi there!
[Sent] Hi there!
[user123] Hi there!

[INFO] User 'user456' joined (Players: 2)

[user456] Hey everyone!

> leave
[Sent] LEAVE_ROOM request
[SUCCESS] Left the room

> quit
Disconnected. Press any key to exit...
```

## 🔍 상태 메시지

### SUCCESS
- `[SUCCESS] Joined room 'ROOM_ID' (Players: N)` - 룸 입장 성공
- `[SUCCESS] Left the room` - 룸 퇴장 성공

### INFO
- `[INFO] User 'userId' joined (Players: N)` - 다른 유저 입장
- `[INFO] User 'userId' left (Players: N)` - 다른 유저 퇴장
- `[INFO] Room 'ROOM_ID' closed: reason` - 룸 종료
- `[INFO] Disconnected from server` - 서버 연결 해제

### ERROR
- `[ERROR] message` - 에러 메시지
- `[FAILED] Join room failed: reason` - 룸 입장 실패

### 채팅 메시지
- `[senderId] message` - 수신된 채팅 메시지

## 🧪 테스트 시나리오

### 단일 클라이언트 테스트
```bash
# 터미널 1: 서버 실행
cd Servers/TestServer
dotnet run

# 터미널 2: 클라이언트 실행
cd Servers/TestClient
dotnet run

> join
> send Test message
> leave
> quit
```

### 멀티 클라이언트 테스트 (2명)
```bash
# 터미널 1: 서버 실행
cd Servers/TestServer
dotnet run

# 터미널 2: 클라이언트 1
cd Servers/TestClient
dotnet run
> join
> send Hello from Client 1

# 터미널 3: 클라이언트 2
cd Servers/TestClient
dotnet run
> join
> send Hello from Client 2
```

## 📁 프로젝트 구조

```
TestClient/
├── Program.cs           # 메인 프로그램 (명령 루프, 프로토콜 처리)
├── NetworkClient.cs     # TCP 네트워크 클라이언트
├── TestClient.csproj    # 프로젝트 파일
└── README.md            # 이 문서
```

## 🔗 의존성

- `CommonLib` - Protocol, ChatProtocol 클래스 제공
- `.NET 8.0`

## 💡 팁

1. **빠른 메시지 전송**: `send` 대신 `s` 사용
   ```
   > s Quick message
   ```

2. **여러 클라이언트 동시 실행**: 여러 터미널에서 실행 가능

3. **자동 재연결**: 연결이 끊어지면 프로그램을 다시 실행

4. **로그 저장**: 출력을 파일로 리다이렉트
   ```bash
   dotnet run > testlog.txt
   ```

## 🐛 문제 해결

### 연결 실패
- 서버가 실행 중인지 확인
- 포트 번호가 올바른지 확인 (기본값: 7777)
- 방화벽 설정 확인

### "Not in a room" 에러
- `join` 명령으로 먼저 룸에 입장해야 합니다

### "Already in a room" 에러
- 이미 룸에 입장한 상태입니다. `leave` 후 다시 `join` 하세요

---

**개발**: InterPlanetery Server Project
