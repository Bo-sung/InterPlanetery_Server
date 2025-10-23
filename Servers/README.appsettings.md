# Configuration Setup Guide

## appsettings.json 설정 방법

이 프로젝트는 데이터베이스 연결 정보 등 민감한 설정을 `appsettings.json` 파일로 관리합니다.
보안을 위해 실제 `appsettings.json` 파일은 Git에서 제외되어 있습니다.

### 초기 설정 단계

각 프로젝트(BaseServer, ChatClientWPF)에서 다음 단계를 수행하세요:

1. **appsettings.example.json 복사**
   ```bash
   # BaseServer
   cd BaseServer
   cp appsettings.example.json appsettings.json

   # ChatClientWPF
   cd ChatClientWPF
   cp appsettings.example.json appsettings.json
   ```

2. **appsettings.json 편집**

   복사한 `appsettings.json` 파일을 열어 실제 값으로 수정하세요:

   ```json
   {
     "Database": {
       "Server": "실제_DB_서버_주소",
       "UserId": "실제_DB_사용자명",
       "Password": "실제_DB_비밀번호",
       "DatabaseName": "실제_DB_이름",
       "Port": 3306
     },
     "Server": {
       "Host": "localhost",
       "Port": 11021  // BaseServer: 11021, ChatClientWPF: 7777
     },
     "Logging": {
       "LogLevel": "Debug"
     }
   }
   ```

### 설정 항목 설명

#### Database 섹션
- **Server**: MySQL 데이터베이스 서버 주소 (예: `59.25.218.196` 또는 `localhost`)
- **UserId**: 데이터베이스 접속 사용자 ID
- **Password**: 데이터베이스 접속 비밀번호
- **DatabaseName**: 사용할 데이터베이스 이름 (예: `interplanetery_tabledb`)
- **Port**: MySQL 포트 (기본값: `3306`)

#### Server 섹션
- **Host**: 서버 호스트 주소
- **Port**: 서버 포트 번호
  - BaseServer: `11021`
  - ChatClientWPF: `7777`

#### Logging 섹션
- **LogLevel**: 로그 레벨 (`Debug`, `Info`, `Warning`, `Error`)

### 주의사항

- `appsettings.json` 파일은 절대 Git에 커밋하지 마세요.
- `.gitignore`에 이미 등록되어 있어 자동으로 제외됩니다.
- 팀원과 설정을 공유할 때는 별도의 안전한 채널을 사용하세요.
- `appsettings.example.json` 파일만 Git에 포함되며, 이는 설정 템플릿입니다.

### 문제 해결

**Q: "appsettings.json을 찾을 수 없습니다" 오류가 발생합니다.**
- A: 위의 초기 설정 단계를 따라 `appsettings.example.json`을 복사하여 `appsettings.json`을 생성하세요.

**Q: 설정을 변경했는데 반영되지 않습니다.**
- A: 애플리케이션을 재시작하세요. AppConfig는 싱글톤으로 초기화 시점에 설정을 로드합니다.

**Q: 데이터베이스 연결 오류가 발생합니다.**
- A: `appsettings.json`의 Database 섹션 값들이 올바른지 확인하세요.
  - 서버 주소가 접근 가능한지 확인
  - 사용자 ID와 비밀번호가 정확한지 확인
  - 포트 번호가 올바른지 확인
