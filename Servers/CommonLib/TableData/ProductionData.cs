using System.Collections.Generic;
using System.Reflection;
using MySql.Data.MySqlClient;

namespace CommonLib.TableData
{
    public class ProductionData
    {
        // ID, TargetID, ProductionTime, GasCost, MineralCost, SupplyCost
        public int Id { get; set; }
        public int TargetId { get; set; }
        public float ProductionTime { get; set; }
        public int GasCost { get; set; }
        public int MineralCost { get; set; }
        public int SupplyCost { get; set; }
        public ProductionData(int id, int targetId, float productionTime, int gasCost, int mineralCost, int supplyCost)
        {
            Id = id;
            TargetId = targetId;
            ProductionTime = productionTime;
            GasCost = gasCost;
            MineralCost = mineralCost;
            SupplyCost = supplyCost;
        }

        public ProductionData(ProductionInfoData record)
        {
            Id = record.id;
            TargetId = record.Targetid;
            ProductionTime = record.ProductionTime;
            GasCost = record.GasCost;
            MineralCost = record.MineralCost;
            SupplyCost = record.SupplyCost;
        }
    }

    public record ProductionInfoData(
         [DbColumn("id")] int id,
         [DbColumn("target_id")] int Targetid,
         [DbColumn("production_time")] float ProductionTime,
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
                if (!float.TryParse(reader[colNames[2]].ToString(), out float productionTime))
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
}
