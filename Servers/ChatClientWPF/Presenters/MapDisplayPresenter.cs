using ChatClientWPF.Models;
using ChatClientWPF.Views;
using CommonLib;
using CommonLib.TableData;
using BaseServer.Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ChatClientWPF.Presenters
{
    public class MapDisplayPresenter
    {
        private readonly IMapDisplayView _view;
        private MapData? _mapData;
        private List<DisplayPlanet> _displayPlanets = new List<DisplayPlanet>();
        private readonly DB_Table _dbTable;

        public MapDisplayPresenter(IMapDisplayView view)
        {
            _view = view;
            _view.OnRenderMapClicked += HandleRenderMapClicked;
            _view.OnPlanetSelected += HandlePlanetSelected;

            // DB 테이블 초기화
            _dbTable = new DB_Table();
            LoadMapDataFromDB();
        }

        /// <summary>
        /// DB에서 맵 데이터 로드
        /// </summary>
        private void LoadMapDataFromDB()
        {
            try
            {
                string connectionString = AppConfig.Instance.DatabaseConnectionString;
                _dbTable.UpdateTable(connectionString);
                Debug.WriteLine($"맵 데이터 로드 완료: {_dbTable.Map_info.Count}개 맵");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"맵 데이터 로드 실패: {ex.Message}");
                _view.ShowError($"Failed to load map data from database:\n{ex.Message}");
            }
        }

        private void HandleRenderMapClicked()
        {
            try
            {
                Debug.WriteLine("=== HandleRenderMapClicked 호출됨 ===");

                // MapDisplayPage에서 입력한 MapId 가져오기
                int mapId = (_view as ChatClientWPF.Pages.MapDisplayPage)?.MapId ?? 1;
                Debug.WriteLine($"선택된 맵 ID: {mapId}");

                // DB에서 해당 맵 정보 조회
                if (!_dbTable.Map_info.TryGetValue(mapId, out var mapInfo))
                {
                    _view.ShowError($"Map ID {mapId} not found in database.");
                    return;
                }

                Debug.WriteLine($"맵 이름: {mapInfo.Name}");

                // 해당 맵의 행성 정보 조회
                var planetLayouts = _dbTable.Map_Planet_info.Values
                    .Where(p => p.mapId == mapId)
                    .ToList();

                if (planetLayouts.Count == 0)
                {
                    _view.ShowError($"No planets found for map '{mapInfo.Name}'");
                    return;
                }

                // DisplayPlanet으로 변환
                _displayPlanets.Clear();
                foreach (var layout in planetLayouts)
                {
                    if (_dbTable.Planet_info.TryGetValue(layout.planetId, out var planetInfo))
                    {
                        var displayPlanet = new DisplayPlanet(planetInfo, layout);
                        _displayPlanets.Add(displayPlanet);
                        Debug.WriteLine($"행성 추가: {planetInfo.Name} at ({layout.PositionX}, {layout.PositionY})");
                    }
                }

                // 연결선 정보 조회
                var connections = _dbTable.Map_Route_info.Values
                    .Where(r => r.mapId == mapId)
                    .Select(r => (r.planetFromId, r.planetToId))
                    .ToList();

                Debug.WriteLine($"맵 렌더링: {_displayPlanets.Count}개 행성, {connections.Count}개 연결");

                // View에 맵 그리기
                _view.DrawMap(_displayPlanets, connections);
                Debug.WriteLine("=== 맵 렌더링 완료 ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"맵 렌더링 실패: {ex.Message}\n{ex.StackTrace}");
                _view.ShowError($"Failed to render map:\n{ex.Message}");
            }
        }

        private void HandlePlanetSelected(int planetId)
        {
            var planet = _displayPlanets.FirstOrDefault(p => p.Id == planetId);
            if (planet != null)
            {
                Debug.WriteLine($"Planet {planet.Name} (ID: {planet.Id}) was selected.");
            }
        }
    }
}
