using System;

namespace InterPlanetary.Network
{
    /// <summary>
    /// 채팅 서버 프로토콜 타입 정의
    /// </summary>
    public static class ChatProtocolType
    {
        // 클라이언트 -> 서버
        public const int JOIN_ROOM = 1001;          // 룸 입장 요청
        public const int LEAVE_ROOM = 1002;         // 룸 퇴장 요청
        public const int CHAT_MESSAGE = 1003;       // 채팅 메시지 전송
        public const int HEARTBEAT = 1004;          // 하트비트 (연결 유지 확인)

        // 서버 -> 클라이언트
        public const int JOIN_SUCCESS = 2001;       // 입장 성공
        public const int JOIN_FAILED = 2002;        // 입장 실패
        public const int LEAVE_SUCCESS = 2003;      // 퇴장 성공
        public const int USER_JOINED = 2004;        // 다른 유저 입장 알림
        public const int USER_LEFT = 2005;          // 다른 유저 퇴장 알림
        public const int CHAT_BROADCAST = 2006;     // 채팅 메시지 브로드캐스트
        public const int ROOM_CLOSED = 2007;        // 룸 종료 알림
        public const int HEARTBEAT_ACK = 2008;      // 하트비트 응답
        public const int ERROR = 2999;              // 에러 메시지
    }

    /// <summary>
    /// 채팅 메시지 데이터 구조체
    /// </summary>
    [Serializable]
    public struct ChatMessage
    {
        public string SenderId;
        public string Message;
        public long Timestamp;

        public override string ToString()
        {
            return $"[{SenderId}]: {Message}";
        }
    }

    /// <summary>
    /// 룸 정보 구조체
    /// </summary>
    [Serializable]
    public struct RoomInfo
    {
        public string RoomId;
        public int PlayerCount;
        public int MaxPlayers;

        public override string ToString()
        {
            return $"Room {RoomId}: {PlayerCount}/{MaxPlayers}";
        }
    }
}
