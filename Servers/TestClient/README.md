# Test Client for Game Server

간단한 CLI 기반 테스트 클라이언트입니다.

## 실행 방법

```bash
cd TestClient
dotnet run
```

## 사용 가능한 기능

### 1. 연결 메뉴 (미접속 상태)
- **1. Connect to server** - 서버 접속 (기본값: 127.0.0.1:7777)
- **0. Exit** - 프로그램 종료

### 2. 인증 메뉴 (접속 완료, 미로그인 상태)
- **1. Auto Register (Guest)** - 자동 회원가입 (guest 계정 생성)
  - username과 password가 자동 생성됨
  - 자동으로 로그인 시도
- **2. Register** - 일반 회원가입
  - username (3-20자)
  - password (4-50자)
- **3. Login** - 로그인
- **9. Disconnect** - 서버 연결 해제
- **0. Exit** - 프로그램 종료

### 3. 메인 메뉴 (로그인 완료 상태)
- **1. Join Lobby** - 로비 접속
  - 페이지 번호 입력 (0이면 전체 조회)
  - 방 리스트 확인 가능
- **2. Refresh Room List** - 방 리스트 새로고침
- **3. Create Room** - 방 생성
  - 방 이름
  - 맵 ID (기본값: 0)
  - 비공개 여부 (y/n)
- **4. Join Room** - 방 입장
  - 방 ID
  - 슬롯 번호 (0 or 1)
- **5. Logout** - 로그아웃
- **9. Disconnect** - 서버 연결 해제
- **0. Exit** - 프로그램 종료

## 자동 기능

- **하트비트** - 10초마다 자동으로 HEARTBEAT 전송
- **메시지 수신** - 백그라운드에서 자동으로 서버 메시지 수신 및 출력

## 테스트 시나리오 예시

### 빠른 테스트 (자동 회원가입)
1. `1` - 서버 접속 (엔터 = 기본값 사용)
2. `1` - 자동 회원가입 (자동 로그인됨)
3. `1` - 로비 접속, `0` 입력 (전체 방 조회)
4. `3` - 방 생성
   - 방 이름: `Test Room`
   - 맵 ID: `0`
   - 비공개: `n`
5. `4` - 방 입장
   - 방 ID: 생성된 방 ID 입력
   - 슬롯: `0`

### 일반 회원가입 테스트
1. `1` - 서버 접속
2. `2` - 일반 회원가입
   - username: `testuser`
   - password: `test1234`
3. `3` - 로그인
   - username: `testuser`
   - password: `test1234`
4. `1` - 로비 접속

### 2명 동시 테스트
- 클라이언트 2개를 실행하여 각각 다른 계정으로 로그인
- 한 명이 방 생성
- 다른 한 명이 같은 방에 입장
- `USER_JOINED`, `USER_LEFT` 등 브로드캐스트 메시지 확인

## 출력 메시지 설명

- `[Disconnected]` - 서버 미접속 상태
- `[Connected]` - 서버 접속됨, 미로그인
- `[Logged in as: username]` - 로그인 완료
- `[Received]` - 서버로부터 메시지 수신
- `[Response]` - 서버 응답 (성공/실패)
- `[Success]` - 작업 성공
- `[Error]` - 에러 발생
- `[Info]` - 정보성 메시지
- `[User Joined]` - 다른 유저 입장
- `[User Left]` - 다른 유저 퇴장
- `[Room Info Changed]` - 방 정보 변경
- `[Room Closed]` - 방 폐쇄

## 주의사항

- 서버가 실행 중이어야 접속 가능
- 기본 포트는 7777
- 자동 회원가입으로 생성된 계정 정보는 반드시 저장할 것
