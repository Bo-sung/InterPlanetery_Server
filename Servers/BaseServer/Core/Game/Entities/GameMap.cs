using BaseServer.Core.Game.Managers;
using BaseServer.Utils;
using CommonLib;
using CommonLib.TableData; // MapData, Planet, Vector2
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace BaseServer.Core.Game.Entities
{
    public class GameMap
    {
        private MapData _staticMapData; // DB에서 로드된 정적 맵 설정
        private Dictionary<int, GamePlanet> _planetDict; // 런타임 행성 객체들 (ID로 접근)
        private Graph<GamePlanet> _mapGraph; // 행성 간 연결 그래프
        private Dictionary<int, HashSet<int>> _pathCacheDict; // 경로 캐시 (Cache)
        private int[] _players = new int[Game.MAX_PLAYERS];
        private Dictionary<int, int> _homePlanet = new Dictionary<int, int>();
        public GamePlanet[] Planets => _planetDict.Values.ToArray();


        public GameMap(MapData staticMapData)
        {
            _staticMapData = staticMapData ?? throw new ArgumentNullException(nameof(staticMapData));
            _planetDict = new Dictionary<int, GamePlanet>();
            _mapGraph = new Graph<GamePlanet>();
            _pathCacheDict = new Dictionary<int, HashSet<int>>();

            SetupMap();
        }

        private void SetupMap()
        {
            _planetDict.Clear();
            _mapGraph = new Graph<GamePlanet>(); // 그래프 초기화
            _pathCacheDict.Clear();

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
                    _mapGraph.AddEdge(fromPlanet, toPlanet, CommonLib.Vector2.Distance(fromPlanet.Position, toPlanet.Position));

                    // 경로 캐시 딕셔너리 업데이트
                    if (!_pathCacheDict.ContainsKey(path.planetFromId))
                        _pathCacheDict.Add(path.planetFromId, new HashSet<int>());
                    _pathCacheDict[path.planetFromId].Add(path.planetToId);

                    if (!_pathCacheDict.ContainsKey(path.planetToId))
                        _pathCacheDict.Add(path.planetToId, new HashSet<int>());
                    _pathCacheDict[path.planetToId].Add(path.planetFromId);
                }
            }
        }

        public void HandleOnStart(int[] playerIDs)
        {
            _players = playerIDs;

            _homePlanet.Clear();

            // Player 1 홈 행성 설정 (유효한 경우에만)
            if (_players.Length > 0 && _players[0] != -1)
            {
                _homePlanet.Add(_players[0], _staticMapData.mapInfoData.Player1_HomeID);
            }

            // Player 2 홈 행성 설정 (유효한 경우에만)
            if (_players.Length > 1 && _players[1] != -1)
            {
                _homePlanet.Add(_players[1], _staticMapData.mapInfoData.Player2_HomeID);
            }
        }

        public GamePlanet? GetPlanet(int planetId)
        {
            _planetDict.TryGetValue(planetId, out var planet);
            return planet;
        }

        public GamePlanet? GetHomePlanet(int playerId)
        {
            if (_homePlanet.TryGetValue(playerId, out int homeId))
            {
                _planetDict.TryGetValue(homeId, out var planet);

                return planet;
            }
            return null;
        }

        public bool IsValidPath(int from, int to)
        {
            if (!_pathCacheDict.ContainsKey(from))
                return false;

            return _pathCacheDict[from].Contains(to);
        }

        public int? GetHomePlanetId(int playerId)
        {
            if (_homePlanet.TryGetValue(playerId, out int val))
                return val;
            return null;
        }

    }
}