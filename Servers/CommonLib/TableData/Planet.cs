using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using MySql.Data.MySqlClient;

namespace CommonLib.TableData
{
    /// <summary>
    /// 행성의 종류를 나타내는 열거형
    /// </summary>
    public enum PlanetType
    {
        Terrestrial, // 지구형 행성
        GasGiant,    // 가스 거인
        IceGiant,    // 얼음 거인
        DwarfPlanet  // 왜소 행성
    }

    /// <summary>
    /// 개별 행성의 데이터를 나타내는 클래스
    /// </summary>
    public class Planet
    {
        PlanetInfoData mPlanetInfoData;
        Vector2 mPlanetPosition;
        public int Id => mPlanetInfoData.id;
        public PlanetType Type => PlanetType.Terrestrial;// 일단 기본값
        public Vector2 Position => mPlanetPosition;
        public string Name => mPlanetInfoData.Name;
        public int Mineral => mPlanetInfoData.Mineral;
        public int Gas => mPlanetInfoData.Gas;
        public int Supply => mPlanetInfoData.Supply;

        public int OwnerId = -1;
        public float ConquestProgress = 0;
        public int GarrisonFleetId = -1;

        public Planet(PlanetInfoData planetInfo, Vector2 Position)
        {
            this.mPlanetInfoData = planetInfo;
            this.mPlanetPosition = Position;
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
}
