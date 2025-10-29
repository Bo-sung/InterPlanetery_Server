using System;
using System.Collections.Generic;
using System.Linq;
using CommonLib.TableData; // MapData, Planet, Vector2
using CommonLib; // Graph

namespace BaseServer.Core.Game.Entities
{
    /// <summary>
    /// 맵 데이터를 표현하는 순수 데이터 컨테이너 클래스
    /// </summary>
    public class MapData
    {
        public readonly MapInfoData mapInfoData;
        public string MapName => mapInfoData.Name;
        public IReadOnlyList<PlanetInfoData> PlanetInfos { get; private set; }
        public IReadOnlyList<MapPlanetInfoData> PlanetLayouts { get; private set; }
        public IReadOnlyList<MapRouteInfoData> Connections { get; private set; }

        public MapData(MapInfoData mapInfoData, IEnumerable<PlanetInfoData> planetInfos, IEnumerable<MapPlanetInfoData> planetLayouts, IEnumerable<MapRouteInfoData> connections)
        {
            this.mapInfoData = mapInfoData;
            this.PlanetInfos = planetInfos.ToList();
            this.PlanetLayouts = planetLayouts.ToList();
            this.Connections = connections.ToList();
        }
    }
    public class GameMap
    {
        private MapData _staticMapData; // DB에서 로드된 정적 맵 설정
        private Dictionary<int, GamePlanet> _planetDict; // 런타임 행성 객체들 (ID로 접근)
        private Graph<GamePlanet> _mapGraph; // 행성 간 연결 그래프
        private Dictionary<int, HashSet<int>> _pathCashDict; // 경로 캐시

        public GameMap(MapData staticMapData)
        {
            _staticMapData = staticMapData ?? throw new ArgumentNullException(nameof(staticMapData));
            _planetDict = new Dictionary<int, GamePlanet>();
            _mapGraph = new Graph<GamePlanet>();
            _pathCashDict = new Dictionary<int, HashSet<int>>();

            SetupMap();
        }

        private void SetupMap()
        {
            _planetDict.Clear();
            _mapGraph = new Graph<GamePlanet>(); // 그래프 초기화
            _pathCashDict.Clear();

            // PlanetInfoData를 빠르게 찾기 위해 Dictionary로 변환
            var planetInfoLookup = _staticMapData.PlanetInfos.ToDictionary(info => info.id);

            // 맵에 배치된 행성들(PlanetLayouts)을 기반으로 GamePlanet 객체 생성
            foreach (var planetLayout in _staticMapData.PlanetLayouts)
            {
                if (planetInfoLookup.TryGetValue(planetLayout.planetId, out var planetInfo))
                {
                    var gamePlanet = new GamePlanet(planetInfo, planetLayout);
                    _planetDict.Add(gamePlanet.Id, gamePlanet);
                    _mapGraph.AddNode(gamePlanet);
                }
            }

            // 경로 설정
            foreach (var path in _staticMapData.Connections)
            {
                if (_planetDict.TryGetValue(path.planetFromId, out var fromPlanet) && _planetDict.TryGetValue(path.planetToId, out var toPlanet))
                {
                    // 가중치는 두 행성 간의 거리로 설정
                    _mapGraph.AddEdge(fromPlanet, toPlanet, Vector2.Distance(fromPlanet.Position, toPlanet.Position));

                    // 경로 캐시 딕셔너리 업데이트
                    if (!_pathCashDict.ContainsKey(path.planetFromId))
                        _pathCashDict.Add(path.planetFromId, new HashSet<int>());
                    _pathCashDict[path.planetFromId].Add(path.planetToId);

                    if (!_pathCashDict.ContainsKey(path.planetToId))
                        _pathCashDict.Add(path.planetToId, new HashSet<int>());
                    _pathCashDict[path.planetToId].Add(path.planetFromId);
                }
            }
        }

        public GamePlanet? GetPlanet(int planetId)
        {
            _planetDict.TryGetValue(planetId, out var planet);
            return planet;
        }

        public bool IsValidPath(int from, int to)
        {
            if (!_pathCashDict.ContainsKey(from))
                return false;

            return _pathCashDict[from].Contains(to);
        }
    }
}