using CommonLib;

namespace CommonLib.Commands
{
    // "함대 이동" 명령
    [Serializable]
    public class MoveFleetCommand : IGameCommand
    {
        private int m_playerId;
        private long m_tick = 0;
        private int m_targetFleet;
        private int m_targetPlanetId;

        public int PlayerId => m_playerId;
        public long TickNumber => m_tick;
        GameCommandType IGameCommand.Type => GameCommandType.MoveFleet;
        public int TargetFleet => m_targetFleet;
        public int TargetPlanetId => m_targetPlanetId;

        public MoveFleetCommand(Protocol protocol)
        {
            if (protocol == null)
                return;

            this.m_tick = protocol.GetParam<long>("tick");
            this.m_targetFleet = protocol.GetParam<int>("target_fleet");
            this.m_targetPlanetId = protocol.GetParam<int>("target_planet");
        }
    }
}