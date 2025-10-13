using System;
using System.Collections.Generic;

namespace CommonLib
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
        public int Id { get; set; }
        public PlanetType Type { get; set; }
        public Vector2 Position { get; set; }
        public string Name { get; set; }

        public Planet(int id, string name, PlanetType type, Vector2 position)
        {
            Id = id;
            Name = name;
            Type = type;
            Position = position;
        }
    }
}
