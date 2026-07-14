> **⚠️ 레거시 테스트 도구.** 원래 2인 채팅 프로토타입(포트 7777) 클라이언트로 만들어졌으며,
> 현재 게임 서버(`BaseServer`, 포트 9000)의 배포 대상 클라이언트가 아닙니다(로컬 전용 개발 도구).
> 정식 클라이언트는 Unity(`Interplanetary_client`)입니다. 자세한 내용은 프로젝트 루트
> [`README.md`](../../README.md)를 참고하세요.

# ChatClientWPF - MVP 패턴 채팅 클라이언트

WPF 기반 채팅 클라이언트로, **MVP(Model-View-Presenter)** 패턴을 충실히 따라 구현되었습니다.

## 🏗️ 아키텍처

### MVP 패턴 구조

```
ChatClientWPF/
├── Models/                  # Model 계층
│   └── ChatClientModel.cs   # 네트워크 로직, 비즈니스 로직
├── Views/                   # View 계층
│   ├── IChatView.cs         # View 인터페이스 (계약)
│   ├── MainWindow.xaml      # UI 레이아웃
│   └── MainWindow.xaml.cs   # View 구현 (IChatView 구현체)
└── Presenters/              # Presenter 계층
    └── ChatPresenter.cs     # View-Model 중재자
```

### MVP 패턴의 역할 분리

#### 📦 Model (ChatClientModel)
- **책임**: 비즈니스 로직과 데이터 관리
- **기능**:
  - TCP 네트워크 연결 관리
  - CommonLib의 Protocol 사용한 직렬화/역직렬화
  - 서버 통신 (연결, 룸 입장/퇴장, 메시지 전송)
  - 이벤트 발생 (연결 상태 변경, 메시지 수신 등)
- **의존성**: CommonLib 사용, View에 대해 무지

#### 🎨 View (IChatView, MainWindow)
- **책임**: UI 표시 및 사용자 입력 수신
- **기능**:
  - XAML로 정의된 UI 레이아웃
  - 사용자 입력 이벤트 발생 (버튼 클릭, 텍스트 입력)
  - Presenter가 요청한 UI 업데이트 수행
- **의존성**: Presenter에 의존, Model은 모름
- **인터페이스**: `IChatView`를 통해 Presenter와 통신

#### 🎯 Presenter (ChatPresenter)
- **책임**: View와 Model 사이의 중재자
- **기능**:
  - View의 사용자 이벤트를 Model 호출로 변환
  - Model의 비즈니스 이벤트를 View 업데이트로 변환
  - UI 로직 처리 (상태 관리, 유효성 검증)
- **의존성**: IChatView 인터페이스와 ChatClientModel 의존

### 이벤트 흐름

```
사용자 입력 → View 이벤트 → Presenter 핸들러 → Model 메서드 호출
                                                        ↓
                                                   서버 통신
                                                        ↓
Model 이벤트 발생 ← 프로토콜 수신 ← 서버 응답 ←───────────┘
    ↓
Presenter 핸들러 → View 메서드 호출 → UI 업데이트
```

## 🚀 실행 방법

### 1. 서버 실행 (먼저 실행 필요)

```bash
cd Servers/TestServer
dotnet run
```

### 2. WPF 클라이언트 실행

```bash
cd Servers/ChatClientWPF
dotnet run
```

또는 Visual Studio에서 `ChatClientWPF` 프로젝트를 시작 프로젝트로 설정하고 실행.

## 📝 사용 방법

### 1. 서버 연결
- **Host**: 서버 주소 입력 (기본값: `127.0.0.1`)
- **Port**: 포트 번호 입력 (기본값: `7777`)
- **Connect** 버튼 클릭

### 2. 룸 입장
- 연결 후 **Join Room** 버튼 클릭
- 룸 정보와 플레이어 수 표시

### 3. 채팅
- 메시지 입력창에 텍스트 입력
- **Send** 버튼 클릭 또는 **Enter** 키
- 시스템 메시지와 채팅 메시지 구분 표시

### 4. 룸 퇴장 및 연결 해제
- **Leave Room**: 현재 룸 퇴장
- **Disconnect**: 서버 연결 해제

## 🎨 UI 기능

### 상태 표시
- **Connection Status**: 연결 상태 (녹색: 연결됨, 빨간색: 연결 안됨)
- **User ID**: 자동 생성된 고유 사용자 ID
- **Room Info**: 현재 룸 ID 및 플레이어 수

### 메시지 표시
- **시스템 메시지**: `[SYSTEM]` 태그로 표시
  - 연결/연결 해제 알림
  - 룸 입장/퇴장 알림
  - 다른 유저 입장/퇴장 알림
  - 에러 메시지
- **채팅 메시지**:
  - 자신의 메시지: `[You]`
  - 다른 사용자 메시지: `[userId]`
  - 타임스탬프 표시 (HH:mm:ss)

### 버튼 상태 관리
- 연결 상태에 따라 버튼 자동 활성화/비활성화
- 룸 입장 상태에 따라 메시지 입력 활성화

## 🔧 기술 스택

- **.NET 8.0**
- **WPF (Windows Presentation Foundation)**
- **CommonLib** 프로토콜 라이브러리 사용
  - `Protocol` 클래스: JSON 기반 직렬화
  - `ChatProtocolType`: 프로토콜 타입 정의
  - `ChatMessage`: 메시지 구조체
- **TCP 소켓 통신**
- **비동기 I/O** (async/await)

## 🧪 테스트 시나리오

### 단일 클라이언트 테스트
```bash
# 터미널 1: 서버 실행
cd TestServer && dotnet run

# 터미널 2: WPF 클라이언트 실행
cd ChatClientWPF && dotnet run
```

1. Connect 버튼 클릭
2. Join Room 버튼 클릭
3. 메시지 전송 테스트
4. Leave Room → Join Room 재입장 테스트
5. Disconnect 테스트

### 멀티 클라이언트 테스트
```bash
# 여러 터미널에서 동시 실행
cd ChatClientWPF && dotnet run
```

1. 여러 클라이언트 동시 연결
2. 동시에 룸 입장
3. 서로 메시지 주고받기
4. 한 클라이언트 퇴장 시 다른 클라이언트에 알림 확인

## 📐 MVP 패턴의 장점

### 1. 관심사의 분리 (Separation of Concerns)
- Model: 비즈니스 로직에만 집중
- View: UI 표시에만 집중
- Presenter: 둘 사이의 조율만 담당

### 2. 테스트 용이성
- View를 Mock으로 대체하여 Presenter 단위 테스트 가능
- Model은 UI와 완전히 독립적이어서 테스트 용이

### 3. 유지보수성
- 각 계층의 책임이 명확하여 수정이 용이
- View 변경 시 Presenter/Model 영향 최소화

### 4. 재사용성
- Model은 다른 View(CLI, 웹 등)에서도 재사용 가능
- View 인터페이스를 통해 다양한 View 구현 가능

## 🔍 코드 설명

### Model 이벤트
```csharp
// Model이 발생시키는 이벤트들
public event Action<bool, string>? OnConnectionChanged;
public event Action<ChatMessageModel>? OnChatMessageReceived;
public event Action<string, int>? OnJoinRoomSuccess;
// ... 기타 이벤트
```

### View 인터페이스
```csharp
// Presenter가 View를 제어하기 위한 계약
public interface IChatView
{
    // View가 발생시키는 이벤트 (사용자 입력)
    event Action<string, int>? OnConnectRequested;
    event Action? OnJoinRoomRequested;
    // ...

    // Presenter가 호출하는 메서드 (UI 업데이트)
    void ShowConnectionStatus(bool isConnected, string message);
    void AddChatMessage(ChatMessageModel message);
    // ...
}
```

### Presenter 중재
```csharp
// View 이벤트를 Model로 전달
private async void HandleConnectRequested(string host, int port)
{
    await _model.ConnectAsync(host, port);
}

// Model 이벤트를 View로 전달
private void HandleConnectionChanged(bool isConnected, string message)
{
    _view.ShowConnectionStatus(isConnected, message);
}
```

## 📦 의존성

- **CommonLib** (../CommonLib/CommonLib.csproj)
  - Protocol 클래스
  - ChatProtocolType
  - ChatMessage 구조체

## 🎯 향후 확장 가능성

1. **추가 View 구현**: CLI, 콘솔, 웹 뷰 등
2. **DI 컨테이너**: 의존성 주입 프레임워크 적용
3. **유닛 테스트**: Presenter/Model 단위 테스트 추가
4. **하트비트**: Presenter의 `StartHeartbeatAsync()` 활용
5. **UI 개선**: Material Design, 애니메이션 등

---

**개발**: InterPlanetery Server Project
**패턴**: MVP (Model-View-Presenter)
**라이선스**: 프로젝트 라이선스 참조
