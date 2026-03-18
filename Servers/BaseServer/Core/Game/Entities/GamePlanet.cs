using BaseServer.Utils;
using CommonLib;
using CommonLib.TableData;

namespace BaseServer.Core.Game.Entities
{
    /// <summary>
    /// 게임 로직 내에서 사용되는 행성 객체.
    /// CommonLib의 순수 데이터 Record들을 기반으로, 실시간 상태 값을 가집니다.
    /// </summary>
    public class GamePlanet
    {
        private readonly PlanetInfoData _planetInfo;
        private readonly MapPlanetInfoData _planetLayout;

        // --- 실시간 상태 값 (서버 로직에서만 사용) ---
        public int OwnerId { get; set; }
        public float ConquestProgress { get; set; }

        // -----------------------------------------

        // 기본 데이터에 대한 접근자 프로퍼티
        public int Id => _planetInfo.id;
        public Vector2 Position { get; private set; }
        public string Name => _planetInfo.Name;
        public int Mineral => _planetInfo.Mineral;
        public int Gas => _planetInfo.Gas;
        public int Supply => _planetInfo.Supply;

        /// <summary>
        /// 공용 데이터 Record들을 기반으로 게임 로직용 Planet을 생성합니다.
        /// </summary>
        public GamePlanet(PlanetInfoData planetInfo, MapPlanetInfoData planetLayout)
        {
            _planetInfo = planetInfo;
            _planetLayout = planetLayout;

            Position = new Vector2(_planetLayout.PositionX, _planetLayout.PositionY);

            OwnerId = -1; // 기본값: 중립
            ConquestProgress = 0;
        }

        public void Conquest(Fleet fleet, long tick)
        {
            if(fleet == null)
                return;

            if(fleet.Owner == OwnerId)
            {
                ConquestProgress++;
            }
            else
            {
                ConquestProgress--;
                // 음수 방지
                if (ConquestProgress < 0)
                    ConquestProgress = 0;
            }
        }

    }
}