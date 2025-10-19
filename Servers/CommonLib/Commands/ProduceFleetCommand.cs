using CommonLib;

namespace CommonLib.Commands
{
    public class ProduceFleetCommand : IGameCommand
    {
        private int m_playerId;
        private long m_tick = 0;
        private int m_targetId;

        public int PlayerId => m_playerId;
        public long TickNumber => m_tick;
        GameCommandType IGameCommand.Type => GameCommandType.ProduceFleet;
        public int TargetId => m_targetId;

        public ProduceFleetCommand(Protocol protocol)
        {
            if(protocol == null)
                return;

            this.m_tick = protocol.GetParam<long>("tick");
            this.m_targetId = protocol.GetParam<int>("target");
        }
    }
}