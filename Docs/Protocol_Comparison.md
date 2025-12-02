# 프로토콜 문서 vs 구현 차이점 분석 보고서

## 1. 기본 정보 차이

| 항목 | 문서 내용 | 구현 내용 (BaseServer) | 비고 |
|---|---|---|---|
| **인코딩** | JSON | **Binary Header (18 bytes) + JSON Body** | 구현은 18바이트 헤더(Size, Type, Timestamp, ParamCount)가 포함된 바이너리 패킷을 사용함. |

## 2. 프로토콜 ID 및 정의 불일치

| 프로토콜 명 | 문서 ID | 구현 ID (ProtocolType.cs) | 상태 |
|---|---|---|---|
| **SUBMIT_COMMAND** | 3010 | **30100** | ID 불일치 |

## 3. 구현되지 않은 프로토콜 (문서에는 존재하나 핸들러 미등록)

다음 프로토콜들은 `ProtocolType.cs`에는 정의되어 있으나, `ClientSession.cs`에 핸들러가 등록되어 있지 않습니다.

*   `CHAT_MESSAGE` (10002)
*   `REQUEST_TABLEDATA` (10004)
*   `REQUEST_READY` (10014)
*   `REQUEST_LEFT_ROOM` (10015)

## 4. 문서에 없는 추가 구현 프로토콜

다음 프로토콜들은 구현되어 있으나 문서에 누락되어 있습니다.

*   `REQUEST_REGISTER` (10005): 회원가입 요청
*   `REQUEST_REGISTER_AUTO` (10006): 자동 회원가입(게스트) 요청

## 5. 파라미터 및 명명 규칙 차이

전반적으로 문서는 **snake_case**를 사용하거나 혼용하는 반면, 코드는 **camelCase**를 주로 사용합니다.

### 5.1 REQUEST_CREATE_ROOM (10012)
*   **요청 파라미터 이름 불일치**:
    *   문서: `room_name`, `is_private`
    *   구현: `roomName`, `isPrivate`
*   **응답 파라미터 차이**:
    *   구현에는 `roomList` (전체 방 목록)가 응답에 포함되지만 문서에는 없음.

### 5.2 REQUEST_JOIN_ROOM (10013)
*   **요청 파라미터 차이**:
    *   구현에서는 `userId` 파라미터를 추가로 요구함 (`int userId = protocol.GetParam<int>("userId");`). 문서에는 없음.

## 6. 기타 발견 사항

*   **ProtocolType.cs**에 정의되어 있으나 문서와 핸들러 모두에 없는 ID들:
    *   `CHAT_CHANNEL_JOIN` (10100)
    *   `CHAT_CHANNEL_REFRESH` (10101)
    *   `CHAT_CHANNEL_LEFT` (10102)
    *   `REQUEST_GAME_CL_READY` (10200)
    *   `GAME_SET` (20200)
    *   `GAME_STARTED` (20201)
    *   `GAME_STATE` (20202)
    *   `GAME_ENDED` (20203)
