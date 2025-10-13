
using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonLib
{
    /// <summary>
    /// 맵 데이터를 표현하는 순수 데이터 컨테이너 클래스
    /// </summary>
    public class MapData
    {
        public string MapName { get; set; }
        public IReadOnlyList<Planet> Planets { get; private set; }
        public IReadOnlyList<(int FromId, int ToId)> Connections { get; private set; }

        public MapData(string mapName, IEnumerable<Planet> planets, IEnumerable<(int from, int to)> connections)
        {
            MapName = mapName;
            Planets = planets.ToList();
            Connections = connections.Select(c => (c.from, c.to)).ToList();
        }
    }
}
