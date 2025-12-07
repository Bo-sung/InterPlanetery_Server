using System.Data.Common;
using System.Reflection;
using MySql.Data.MySqlClient;

namespace CommonLib.TableData
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class DbColumnAttribute : Attribute
    {
        public string ColumnName { get; }

        public DbColumnAttribute(string columnName)
        {
            ColumnName = columnName;
        }
    }

    public record FleetInfoData(
        [DbColumn("id")] int id,
        [DbColumn("name")] string Name,
        [DbColumn("type")] int Type,
        [DbColumn("max_health")] int MaxHealth,
        [DbColumn("attack_power")] int AttackPower,
        [DbColumn("move_speed")] float MoveSpeed
        )
    {
        public static Dictionary<int, FleetInfoData> Convert(MySqlDataReader reader)
        {
            var ctor = typeof(FleetInfoData).GetConstructors()[0];
            var Cols = ctor.GetParameters();
            string[] colNames = new string[Cols.Length];
            var result = new Dictionary<int, FleetInfoData>();
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
                if (!int.TryParse(reader[colNames[2]].ToString(), out int Type))
                    continue;
                if (!int.TryParse(reader[colNames[3]].ToString(), out int MaxHealth))
                    continue;
                if (!int.TryParse(reader[colNames[3]].ToString(), out int AttackPower))
                    continue;
                if (!int.TryParse(reader[colNames[4]].ToString(), out int MoveSpeed))
                    continue;
                FleetInfoData data = new FleetInfoData(
                    id: id,
                    Name: Name,
                    Type: Type,
                    MaxHealth: MaxHealth,
                    AttackPower: AttackPower,
                    MoveSpeed: MoveSpeed
                    );

                result.Add(data.id, data);
            }

            return result;
        }
    }
    public record ProductionInfoData(
         [DbColumn("id")] int id,
         [DbColumn("target_id")] int Targetid,
         [DbColumn("production_time")] int ProductionTime,
         [DbColumn("gas_cost")] int GasCost,
         [DbColumn("mineral_cost")] int MineralCost,
         [DbColumn("supply_cost")] int SupplyCost
        )
    {
        public static Dictionary<int, ProductionInfoData> Convert(MySql.Data.MySqlClient.MySqlDataReader reader)
        {
            var ctor = typeof(ProductionInfoData).GetConstructors()[0];
            var Cols = ctor.GetParameters();
            string[] colNames = new string[Cols.Length];
            var result = new Dictionary<int, ProductionInfoData>();

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
                if (!int.TryParse(reader[colNames[1]].ToString(), out int targetId))
                    continue;
                if (!int.TryParse(reader[colNames[2]].ToString(), out int productionTime))
                    continue;
                if (!int.TryParse(reader[colNames[3]].ToString(), out int gasCost))
                    continue;
                if (!int.TryParse(reader[colNames[4]].ToString(), out int mineralCost))
                    continue;
                if (!int.TryParse(reader[colNames[5]].ToString(), out int supplyCost))
                    continue;

                ProductionInfoData data = new ProductionInfoData(
                    id: id,
                    Targetid: targetId,
                    ProductionTime: productionTime,
                    GasCost: gasCost,
                    MineralCost: mineralCost,
                    SupplyCost: supplyCost
                );

                result.Add(data.id, data);
            }

            return result;
        }
    }
    public record PlanetInfoData(
       [DbColumn("id")] int id,
       [DbColumn("name")] string Name,
       [DbColumn("gas")] int Gas,
       [DbColumn("mineral")] int Mineral,
       [DbColumn("supply")] int Supply
        )
    {
        public static Dictionary<int, PlanetInfoData> Convert(MySqlDataReader reader)
        {
            var ctor = typeof(PlanetInfoData).GetConstructors()[0];
            var Cols = ctor.GetParameters();
            string[] colNames = new string[Cols.Length];
            var result = new Dictionary<int, PlanetInfoData>();
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
                if (!int.TryParse(reader[colNames[2]].ToString(), out int Gas))
                    continue;
                if (!int.TryParse(reader[colNames[3]].ToString(), out int Mineral))
                    continue;
                if (!int.TryParse(reader[colNames[4]].ToString(), out int Supply))
                    continue;
                PlanetInfoData data = new PlanetInfoData(
                    id: id,
                    Name: Name,
                    Gas: Gas,
                    Mineral: Mineral,
                    Supply: Supply
                    );

                result.Add(data.id, data);
            }

            return result;
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
