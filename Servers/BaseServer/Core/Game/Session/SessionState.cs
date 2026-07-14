using CommonLib;

namespace BaseServer.Core.Game.Session
{
    public enum SessionState
    {
        Connected,
        Authenticated,
        Lobby,
        Room,
        InGame,
        Disconnected
    }

    internal static class SessionProtocolPolicy
    {
        public static bool IsAllowed(SessionState state, int protocolType)
        {
            if (protocolType == ProtocolType.HEARTBEAT)
                return state != SessionState.Disconnected;

            return state switch
            {
                SessionState.Connected => IsConnectedProtocol(protocolType),
                SessionState.Authenticated => IsAuthenticatedProtocol(protocolType),
                SessionState.Lobby => IsLobbyProtocol(protocolType),
                SessionState.Room => IsRoomProtocol(protocolType),
                SessionState.InGame => IsInGameProtocol(protocolType),
                _ => false
            };
        }

        public static bool CanTransition(SessionState current, SessionState next)
        {
            if (current == next)
                return true;

            if (next == SessionState.Disconnected)
                return current != SessionState.Disconnected;

            return current switch
            {
                SessionState.Connected => next == SessionState.Authenticated,
                SessionState.Authenticated => next == SessionState.Lobby,
                SessionState.Lobby => next == SessionState.Room,
                SessionState.Room => next == SessionState.InGame || next == SessionState.Lobby,
                SessionState.InGame => next == SessionState.Room || next == SessionState.Lobby,
                _ => false
            };
        }

        private static bool IsConnectedProtocol(int protocolType)
        {
            return protocolType == ProtocolType.REQUEST_REGISTER
                || protocolType == ProtocolType.REQUEST_REGISTER_AUTO
                || protocolType == ProtocolType.REQUEST_LOGIN;
        }

        private static bool IsAuthenticatedProtocol(int protocolType)
        {
            return protocolType == ProtocolType.REQUEST_LOGOUT
                || protocolType == ProtocolType.REQUEST_JOIN_LOBBY;
        }

        private static bool IsLobbyProtocol(int protocolType)
        {
            return protocolType == ProtocolType.REQUEST_LOGOUT
                || protocolType == ProtocolType.REQUEST_JOIN_LOBBY
                || protocolType == ProtocolType.REFRESH_LOBBY
                || protocolType == ProtocolType.REQUEST_CREATE_ROOM
                || protocolType == ProtocolType.REQUEST_JOIN_ROOM;
        }

        private static bool IsRoomProtocol(int protocolType)
        {
            return protocolType == ProtocolType.REQUEST_LOGOUT
                || protocolType == ProtocolType.REQUEST_LEFT_ROOM
                || protocolType == ProtocolType.REQUEST_READY;
        }

        private static bool IsInGameProtocol(int protocolType)
        {
            return protocolType == ProtocolType.REQUEST_LOGOUT
                || protocolType == ProtocolType.REQUEST_LEFT_ROOM
                || protocolType == ProtocolType.REQUEST_GAME_CL_READY
                || protocolType == ProtocolType.SUBMIT_COMMAND;
        }
    }
}
