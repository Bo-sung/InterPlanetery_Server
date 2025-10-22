
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using System.Reflection;

namespace CommonLib.TableData
{
    /// <summary>
    /// 맵 데이터를 표현하는 순수 데이터 컨테이너 클래스
    /// </summary>
    public class MapData
    {
        public readonly MapInfoData mapInfoData;
        public string MapName => mapInfoData.Name;
        public IReadOnlyList<Planet> Planets { get; private set; }
        public IReadOnlyList<MapRouteInfoData> Connections { get; private set; }

        public MapData(MapInfoData mapInfoData, IEnumerable<Planet> mapPlanetInfoDatas, IEnumerable<MapRouteInfoData> mapRouteInfoDatas)
        {
            this.mapInfoData = mapInfoData;
            Planets = mapPlanetInfoDatas.ToList();
            Connections = mapRouteInfoDatas.ToList();
        }
    }

    public record MapInfoData(
        [DbColumn("id")] int id,
        [DbColumn("name")] string Name,
        [DbColumn("description")] string Description,
        [DbColumn("player1_homeworld_id")] int Player1_HomeID,
        [DbColumn("player2_homeworld_id")] int Player2_HomeID
        )
    {
        public static Dictionary<int, MapInfoData> Convert(MySqlDataReader reader)
        {
            var ctor = typeof(MapInfoData).GetConstructors()[0];
            var Cols = ctor.GetParameters();
            string[] colNames = new string[Cols.Length];
            var result = new Dictionary<int, MapInfoData>();
            for (int i = 0; i < Cols.Length; ++i)
            {
                var attb = Cols[i].GetCustomAttribute<DbColumnAttribute>();
                if (attb != null)
                    colNames[i] = attb.ColumnName;
                else
                    colNames[i] = "";
            }
            while (reader.Read())
            {
                if (!int.TryParse(reader[colNames[0]].ToString(), out int id))
                    continue;
                string Name = reader[colNames[1]]?.ToString();
                if (String.IsNullOrEmpty(Name))
                    continue;
                string Description = reader[colNames[2]]?.ToString();
                if (String.IsNullOrEmpty(Description))
                    continue;
                if (!int.TryParse(reader[colNames[3]].ToString(), out int Player1_HomeID))
                    continue;
                if (!int.TryParse(reader[colNames[4]].ToString(), out int Player2_HomeID))
                    continue;
                MapInfoData data = new MapInfoData(
                    id: id,
                    Name: Name,
                    Description: Description,
                    Player1_HomeID: Player1_HomeID,
                    Player2_HomeID: Player2_HomeID
                    );

                result.Add(data.id, data);
            }

            return result;
        }
    }
    public record MapPlanetInfoData(
        [DbColumn("id")] int id,
        [DbColumn("map_id")] int mapId,
        [DbColumn("planet_id")] int planetId,
        [DbColumn("position_x")] float PositionX,
        [DbColumn("position_y")] float PositionY
        )
    {
        public static Dictionary<int, MapPlanetInfoData> Convert(MySqlDataReader reader)
        {
            var ctor = typeof(MapPlanetInfoData).GetConstructors()[0];
            var Cols = ctor.GetParameters();
            string[] colNames = new string[Cols.Length];
            var result = new Dictionary<int, MapPlanetInfoData>();
            for (int i = 0; i < Cols.Length; ++i)
            {
                var attb = Cols[i].GetCustomAttribute<DbColumnAttribute>();
                if (attb != null)
                    colNames[i] = attb.ColumnName;
                else
                    colNames[i] = "";
            }
            while (reader.Read())
            {
                if (!int.TryParse(reader[colNames[0]].ToString(), out int id))
                    continue;
                if (!int.TryParse(reader[colNames[1]].ToString(), out int mapId))
                    continue;
                if (!int.TryParse(reader[colNames[2]].ToString(), out int planetId))
                    continue;
                if (!int.TryParse(reader[colNames[3]].ToString(), out int PositionX))
                    continue;
                if (!int.TryParse(reader[colNames[4]].ToString(), out int PositionY))
                    continue;
                MapPlanetInfoData data = new MapPlanetInfoData(
                    id: id,
                    mapId: mapId,
                    planetId: planetId,
                    PositionX: PositionX,
                    PositionY: PositionY
                    );

                result.Add(data.id, data);
            }

            return result;
        }
    }
    public record MapRouteInfoData(
        [DbColumn("id")] int id,
        [DbColumn("map_id")] int mapId,
        [DbColumn("planet_from_id")] int planetFromId,
        [DbColumn("planet_to_id")] int planetToId
        )
    {
        public static Dictionary<int, MapRouteInfoData> Convert(MySqlDataReader reader)
        {
            var ctor = typeof(MapRouteInfoData).GetConstructors()[0];
            var Cols = ctor.GetParameters();
            string[] colNames = new string[Cols.Length];
            var result = new Dictionary<int, MapRouteInfoData>();
            for (int i = 0; i < Cols.Length; ++i)
            {
                var attb = Cols[i].GetCustomAttribute<DbColumnAttribute>();
                if (attb != null)
                    colNames[i] = attb.ColumnName;
                else
                    colNames[i] = "";
            }
            while (reader.Read())
            {
                if (!int.TryParse(reader[colNames[0]].ToString(), out int id))
                    continue;
                if (!int.TryParse(reader[colNames[1]].ToString(), out int mapId))
                    continue;
                if (!int.TryParse(reader[colNames[2]].ToString(), out int planetFromId))
                    continue;
                if (!int.TryParse(reader[colNames[3]].ToString(), out int planetToId))
                    continue;
                MapRouteInfoData data = new MapRouteInfoData(
                    id: id,
                    mapId: mapId,
                    planetFromId: planetFromId,
                    planetToId: planetToId
                    );

                result.Add(data.id, data);
            }

            return result;
        }
    }
}
