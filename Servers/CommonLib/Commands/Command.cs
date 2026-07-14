using System;
using CommonLib.TableData; // FleetType 정의를 위해 필요

namespace CommonLib.Commands
{
    // 기존 BaseServer의 GameCommandType을 CommonLib.Commands로 옮겨옴
    public enum GameCommandType
    {
        None = 0,
        ProduceFleet = 1,
        MoveFleet = 2,
        // ... 기타 명령 타입 ...
    }

    [Serializable]
    public abstract class Command
    {
        public int PlayerId { get; set; }
        public abstract GameCommandType Type { get; }
    }
}
