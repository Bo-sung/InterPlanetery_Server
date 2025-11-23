using ChatClientWPF.Database;
using CommonLib;
using CommonLib.TableData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChatClientWPF.Models
{
	/// <summary>
	/// 맵 데이터를 로드하고 처리하는 서비스
	/// </summary>
	public class MapService
	{
		/// <summary>
		/// 데이터베이스에서 맵 데이터를 로드합니다
		/// </summary>
		/// <param name="mapId">로드할 맵 ID</param>
		/// <returns>행성 목록과 연결 정보</returns>
		public (List<DisplayPlanet> planets, List<(int FromId, int ToId)> connections) LoadMapData(int mapId)
		{
			var planets = new List<DisplayPlanet>();
			var connections = new List<(int FromId, int ToId)>();

			try
			{
				// 1. Map_Planet_info에서 해당 맵의 행성 정보 가져오기
				var mapPlanetInfos = DBManager.Instance.Table.Map_Planet_info.Values
					.Where(mp => mp.mapId == mapId)
					.ToList();

				if (mapPlanetInfos.Count == 0)
				{
					System.Diagnostics.Debug.WriteLine($"Map ID {mapId}에 대한 행성 정보가 없습니다.");
					return (planets, connections);
				}

				// 2. 각 행성의 상세 정보 가져오기
				foreach (var mapPlanetInfo in mapPlanetInfos)
				{
					if (DBManager.Instance.Table.Planet_info.TryGetValue(mapPlanetInfo.planetId, out var planetInfo))
					{
						// DisplayPlanet 생성자는 PlanetInfoData와 MapPlanetInfoData를 받음
						var displayPlanet = new DisplayPlanet(planetInfo, mapPlanetInfo);
						planets.Add(displayPlanet);
					}
				}

				// 3. Map_Route_info에서 연결 정보 가져오기
				var routeInfos = DBManager.Instance.Table.Map_Route_info.Values
					.Where(r => r.mapId == mapId)
					.ToList();

				foreach (var routeInfo in routeInfos)
				{
					connections.Add((routeInfo.planetFromId, routeInfo.planetToId));
				}

				System.Diagnostics.Debug.WriteLine($"Map ID {mapId}: {planets.Count}개 행성, {connections.Count}개 연결 로드됨");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"맵 데이터 로드 실패: {ex.Message}");
				throw;
			}

			return (planets, connections);
		}
	}
}
