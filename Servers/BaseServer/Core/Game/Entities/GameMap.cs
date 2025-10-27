using System;
using System.Collections.Generic;
using System.Linq;
using CommonLib.TableData; // MapData, Planet, Vector2
using CommonLib; // Graph

namespace BaseServer.Core.Game.Entities
{
    public class GameMap
    {
        private MapData _staticMapData; // DB에서 로드된 정적 맵 설정
        private Dictionary<int, Planet> _planetDict; // 런타임 행성 객체들 (ID로 접근)
        private Graph<Planet> _mapGraph; // 행성 간 연결 그래프
        private Dictionary<int, HashSet<int>> _pathCashDict; // 경로 캐시

        public GameMap(MapData staticMapData)
        {
            _staticMapData = staticMapData ?? throw new ArgumentNullException(nameof(staticMapData));
            _planetDict = new Dictionary<int, Planet>();
            _mapGraph = new Graph<Planet>();
            _pathCashDict = new Dictionary<int, HashSet<int>>();

            SetupMap();
        }

        // 맵 초기 설정 (Game 클래스의 SetupMap 로직을 가져옴)
        private void SetupMap()
        {
            _planetDict.Clear();
            _mapGraph = new Graph<Planet>(); // 그래프 초기화
            _pathCashDict.Clear();

            // staticMapData의 Planet 정보를 기반으로 런타임 Planet 객체 생성 및 초기화
            foreach (var planetData in _staticMapData.Planets)
            {
                // Planet 객체는 이미 자원 정보와 런타임 속성을 포함
                _planetDict.Add(planetData.Id, planetData); // PlanetData 자체가 런타임 Planet 객체로 사용
                _mapGraph.AddNode(planetData);
            }

            // 경로 설정
            foreach (var path in _staticMapData.Connections)
            {
                if (_planetDict.TryGetValue(path.planetFromId, out var fromPlanet) && _planetDict.TryGetValue(path.planetToId, out var toPlanet))
                {
                    // 가중치는 두 행성 간의 거리로 설정 (Vector2는 CommonLib에 있다고 가정)
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

        public Planet? GetPlanet(int planetId)
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
