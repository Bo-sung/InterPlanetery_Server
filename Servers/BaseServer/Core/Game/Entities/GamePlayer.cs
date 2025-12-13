using BaseServer.Core.Game;
using BaseServer.Core.Game.Managers;
using BaseServer.Core.Game.Session;
using CommonLib;
using CommonLib.Commands;
using CommonLib.TableData;
using MySqlX.XDevAPI;
using System.Data;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection.Emit;
using System.Windows.Input;
using ProtocolType = CommonLib.ProtocolType;

namespace BaseServer.Core.Game.Entities
{
    /// <summary>
    /// 게임 플레이어 객체. 클라이언트 세션을 래핑하여 사용.
    /// 대충 GamePlayer 라는 메카에 ClientSession이 탑승한 상태라 보면 됨.
    /// </summary>
    public class GamePlayer
    {
        #region 상수
        const int DEFAULT_ID = -1;
        const string DEFAULT_NAME = "IVALID";
        #endregion

        #region 세션 및 기본 필드
        protected ClientSession? clientSession;
        private ICommandSender commandSender;
        protected int m_id = DEFAULT_ID;        // 유저 식별자
        protected string m_name = DEFAULT_NAME; // 유저 닉네임
        private bool m_isDefeated = false;
        #endregion

        #region 자원 관련 필드
        private int m_gas = 0;
        private int m_mineral = 0;
        private int m_supply = 0;
        private int m_maxSup = 0;
        #endregion

        #region 게임 오브젝트 필드
        private List<long> m_fleets = new List<long>();
        private List<int> m_planets = new List<int>();
        private Queue<int> m_productionOrder = new Queue<int>();
        private int m_faction = 0; // 진영 정보
        private int m_homeworld = 0; // 본진 행성 ID
        #endregion

        #region 프로퍼티
        public ClientSession Session => clientSession;
        public bool IsValid
        {
            get
            {
                return clientSession != null
                    && m_id != DEFAULT_ID
                    && m_name != DEFAULT_NAME;
            }
        }

        public int ID { get { return m_id; } set { m_id = value; } }
        public string Name { get { return m_name; } set { m_name = value; } }

        public int Gas { get { return m_gas; } set { m_gas = value; } }
        public int Mineral { get { return m_mineral; } set { m_mineral = value; } }
        public int Supply { get { return m_supply; } set { m_supply = value; } }
        public int MaxSupply { get { return m_maxSup; } set { m_maxSup = value; } }

        public int Faction { get { return m_faction; } set { m_faction = value; } }
        public int Homeworld { get { return m_homeworld; } set { m_homeworld = value; } }
        public bool IsDefeated { get { return m_isDefeated; } set { m_isDefeated = value; } }

        public List<long> Fleets { get { return m_fleets; } }
        public List<int> Planets { get { return m_planets; } }
        public Queue<int> ProductionOrder { get { return m_productionOrder; } }

        public System.Action OnUserReady;
        public System.Action OnClientReady;
        #endregion

        public GamePlayer()
        {
        }

        #region 초기화 및 정리
        public void Initialize(ClientSession clientSession, ICommandSender sender)
        {
            this.clientSession = clientSession;
            this.commandSender = sender;

            // 세션으로부터 플레이어 정보 설정
            this.m_id = clientSession.UserInfo.UserId;
            this.m_name = clientSession.UserInfo.UserName;

            RegistorProtos();
        }

        /// <summary>
        /// 세션 분리 (플레이어 슬롯의 게임 데이터는 유지)
        /// </summary>
        public void Cleanup()
        {
            UnRegistorProtos();

            this.clientSession = null;
            this.m_id = DEFAULT_ID;
            this.m_name = DEFAULT_NAME;
        }
        #endregion

        #region 프로토콜 처리
        private void RegistorProtos()
        {
            if (clientSession == null)
                return;
            clientSession.RegisterProto(ProtocolType.SUBMIT_COMMAND, HandleSubmitCommand);
            clientSession.RegisterProto(ProtocolType.REQUEST_GAME_CL_READY, HandleClientReady);
        }

        protected virtual void UnRegistorProtos()
        {
            if (clientSession == null)
                return;
            clientSession.UnRegisterProto(ProtocolType.SUBMIT_COMMAND);
            clientSession.UnRegisterProto(ProtocolType.REQUEST_GAME_CL_READY);
        }

        /// <summary>
        /// 카멘드 처리
        /// </summary>
        /// <param name="_protocol"></param>
        private async Task HandleSubmitCommand(Protocol _protocol)
        {
            var commandTypeVal = _protocol.GetParam<int>("commandType");
            if (commandTypeVal == 0)
            {
                return;
            }
            GameCommandType commandType = (GameCommandType)commandTypeVal;

            Command command;
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

            if (commandSender != null)
            {
                command.PlayerId = this.m_id;
                await commandSender.SendCommandToGame(command);
            }
        }

        public async Task HandleUserReady(Protocol _protocol)
        {
            OnUserReady?.Invoke();
        }

        public async Task HandleClientReady(Protocol _protocol)
        {
            OnClientReady?.Invoke();
        }

        public async Task Async_SendGameSet(MapData map)
        {
            if (clientSession == null)
                return;

            Protocol result = new Protocol(ProtocolType.GAME_SET);
            result.AddObject("mapinfo", map.mapInfoData);
            result.AddObject("mapplanetinfo", map.PlanetLayouts.ToArray());
            result.AddObject("planets", map.PlanetInfos.ToArray());
            result.AddObject("routes", map.Connections.ToArray());

            await clientSession.SendAsync(result.Serialize());
        }

        public async Task Async_SendGameState(GameState state, long serverTick)
        {
            if (clientSession == null)
                return;

            Protocol result = new Protocol(ProtocolType.GAME_STATE);
            result.AddObject<GameState>("gameState", state);
            result.AddParam("serverTick", serverTick);

            await clientSession.SendAsync(result.Serialize());
        }

        public async Task Async_SendGameStart()
        {
            if (clientSession == null)
                return;

            Protocol result = new Protocol(ProtocolType.GAME_STARTED);

            await clientSession.SendAsync(result.Serialize());
        }

        #endregion

        #region 게임 로직
        public void Update_Resource(GameMap map, long tick)
        {
            // 틱당 자원 처리
            // 점령한 행성들의 자원 총합
            int tickGas = 0;
            int tickMin = 0;
            int maxsup = 0;

            foreach(var pindex in Planets)
            {
                var planet = map.GetPlanet(pindex);
                if (planet == null)
                    continue;
                tickGas += planet.Gas;
                tickMin += planet.Mineral;
                maxsup += planet.Supply;
            }

            Gas += tickGas;
            Mineral += tickMin;
            MaxSupply = maxsup;       // 인구수는 초당 생산이 아니라 케파임. 고르. 최대치만 설정
        }
        #endregion


        private void LogWithTimestamp(string message)
        {
            var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            System.Console.WriteLine($"[{timestamp}] {message}");
        }
    }
}
