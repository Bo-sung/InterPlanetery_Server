using CommonLib;
using CommonLib.Commands;
using CommonLib.TableData;
using System.Data;
using System.Net.Sockets;
using System.Windows.Input;
using BaseServer.Core.Game.Session;
using ProtocolType = CommonLib.ProtocolType;

namespace BaseServer.Core.Game.Entities
{
    /// <summary>
    /// 게임 플레이어 객체. 클라이언트 세션을 래핑하여 사용.
    /// 대충 GamePlayer 라는 메카에 ClientSession이 탑승한 상태라 보면 됨.
    /// </summary>
    public class GamePlayer
    {
        private ClientSession clientSession;
        public ClientSession Session => clientSession;
        private int m_id = 0;
        private string m_name = "";

        private int m_gas = 0;
        private int m_mineral = 0;
        private int m_supply = 0;

        private List<int> m_fleets = new List<int>();
        private List<int> m_planets = new List<int>();
        private Queue<int> m_productionOrder = new Queue<int>();

        private int m_faction = 0; // 진영 정보
        private int m_homeworld = 0; // 본진 행성 ID
        private bool m_isDefeated = false;

        public int ID { get { return m_id; } set { m_id = value; } }
        public string Name { get { return m_name; } set { m_name = value; } }

        public int Gas { get { return m_gas; } set { m_gas = value; } }
        public int Mineral { get { return m_mineral; } set { m_mineral = value; } }
        public int Supply { get { return m_supply; } set { m_supply = value; } }

        public int Faction { get { return m_faction; } set { m_faction = value; } }
        public int Homeworld { get { return m_homeworld; } set { m_homeworld = value; } }
        public bool IsDefeated { get { return m_isDefeated; } set { m_isDefeated = value; } }

        public List<int> Fleets { get { return m_fleets; } }
        public List<int> Planets { get { return m_planets; } }
        public Queue<int> ProductionOrder { get { return m_productionOrder; } }

        public GamePlayer(ClientSession clientSession)
        {
            this.clientSession = clientSession;
            RegistorProtos();
        }

        private void RegistorProtos()
        {
            clientSession.RegisterProto(ProtocolType.SUBMIT_COMMAND, HandleSubmitCommand);
        }

        /// <summary>
        /// 카멘드 처리
        /// </summary>
        /// <param name="_protocol"></param>
        /// <returns></returns>
        private async Task HandleSubmitCommand(Protocol _protocol)
        {
            var commandTypeVal = _protocol.GetParam<int>("commandType");
            if (commandTypeVal == 0)
            {
                return;
            }
            GameCommandType commandType = (GameCommandType)commandTypeVal;

            var commandDataVal = _protocol.GetParam<string>("commandData");
            if (commandDataVal == null)
            {
                return;
            }

            IGameCommand command;
            switch (commandType)
            {
                case GameCommandType.ProduceFleet:
                    command = new ProduceFleetCommand(_protocol);
                    break;
                case GameCommandType.MoveFleet:
                    command = new MoveFleetCommand(_protocol);
                    break;
                default:
                    return;
            }

            if (clientSession.CurrentRoom != null)
            {
                await clientSession.CurrentRoom.AddCommand(command);
            }
        }

        public void HandleResurceExcute(long tick)
        {
            // 틱당 자원 처리
            // 점령한 행성들의 자원 총합
            int tickGas = 0;
            int tickMin = 0;
            int tickSup = 0;


        }
    }
}