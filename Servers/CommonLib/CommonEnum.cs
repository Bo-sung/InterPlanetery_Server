using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLib
{
    /// <summary>
    /// 방 상태
    /// </summary>
    public enum RoomState
    {
        Open,
        Full,
        Ingame,
        Disabled,
        Closed,
        Error,
    }

    /// <summary>
    /// 서버 응답 코드
    /// </summary>
    public enum ServerMessage
    {
        Success = 1,
        Error = 2
    }
}
