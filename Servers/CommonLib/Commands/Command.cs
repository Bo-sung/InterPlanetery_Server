using System;
using CommonLib.TableData; // FleetType 정의를 위해 필요

namespace CommonLib.Commands
{
    // 기존 BaseServer의 GameCommandType을 CommonLib.Commands로 옮겨옴
    public enum GameCommandType
    {
        ProduceFleet,
        MoveFleet,
        // ... 기타 명령 타입 ...
    }
    public interface IGameCommand
    {
        public int PlayerId { get; }    // 누가 이 명령을 내렸는가?
        public abstract GameCommandType Type { get; } // 이 명령은 어떤 종류인가?
    }

    [Serializable]
    public abstract class Command : IGameCommand // IGameCommand를 구현하도록 변경
    {
        public int PlayerId { get; set; }
        public abstract GameCommandType Type { get; } // IGameCommand의 Type과 일치

        // GameWorld는 게임 상태를 관리하는 객체로, 실제 구현 시 해당 객체를 인자로 받습니다.
        public abstract void Execute(object gameWorld);
    }
}
