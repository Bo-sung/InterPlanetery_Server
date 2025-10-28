using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChatClientWPF.Models
{
    /// <summary>
    /// 게임 방 정보
    /// </summary>
    public class GameRoomModel
    {
        public string RoomId { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public int MapId { get; set; }
        public string MapName { get; set; } = string.Empty;
        public int CurrentPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public bool IsPrivate { get; set; }
        public int HostPlayerId { get; set; }
        public string Status { get; set; } = "waiting"; // waiting, playing, finished
    }

    /// <summary>
    /// 플레이어 정보 (대기방용)
    /// </summary>
    public class PlayerSlotModel
    {
        public int SlotNumber { get; set; }
        public int? PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public bool IsOccupied => PlayerId.HasValue && !string.IsNullOrEmpty(PlayerName);
        public bool IsReady { get; set; }
        public bool IsHost { get; set; }

        /// <summary>
        /// 플레이어 초기 문자 (아바타용)
        /// </summary>
        public string AvatarChar => IsOccupied && PlayerName.Length > 0 ? PlayerName[0].ToString().ToUpper() : "?";
    }

    /// <summary>
    /// 게임 설정
    /// </summary>
    public class GameSettingsModel
    {
        public string MapName { get; set; } = string.Empty;
        public int MaxPlayerCount { get; set; }
        public string StartingResources { get; set; } = "표준"; // 표준, 넉넉함, 부족함
        public string RoomType { get; set; } = "공개"; // 공개, 비공개
    }

    /// <summary>
    /// 채팅 메시지 (게임방 채팅용)
    /// </summary>
    public class GameRoomChatMessageModel
    {
        public string SenderId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public long Timestamp { get; set; }
        public bool IsSystemMessage { get; set; }

        public DateTime DateTime => DateTimeOffset.FromUnixTimeMilliseconds(Timestamp).LocalDateTime;
        public string FormattedTime => DateTime.ToString("HH:mm");
    }

    /// <summary>
    /// 로비의 게임 방 목록 항목
    /// </summary>
    public class LobbyRoomItemModel
    {
        public string RoomId { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public int CurrentPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public string MapName { get; set; } = string.Empty;
        public string Status { get; set; } = "waiting"; // waiting, playing, full

        public bool IsFull => CurrentPlayers >= MaxPlayers;
        public bool CanJoin => !IsFull && Status != "playing";
        public string PlayerCountText => $"{CurrentPlayers}/{MaxPlayers}";
    }

    /// <summary>
    /// 맵 정보
    /// </summary>
    public class MapModel
    {
        public int MapId { get; set; }
        public string MapName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 서버로부터 받은 방 목록 항목 (프로토콜용)
    /// </summary>
    public class RoomListItem
    {
        public string RoomId { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public int CurrentPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public int MapId { get; set; }
        public string MapName { get; set; } = string.Empty;
        public string Status { get; set; } = "waiting"; // waiting, playing, finished
        public bool IsPrivate { get; set; }
    }
}
