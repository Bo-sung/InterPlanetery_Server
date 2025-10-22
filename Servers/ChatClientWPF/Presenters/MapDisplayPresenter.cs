using ChatClientWPF.Views;
using CommonLib;
using CommonLib.Services;
using CommonLib.TableData;
using System;
using System.Diagnostics;
using System.Linq;

namespace ChatClientWPF.Presenters
{
    public class MapDisplayPresenter
    {
        private readonly IMapDisplayView _view;
        private MapData? _mapData;

        public MapDisplayPresenter(IMapDisplayView view)
        {
            _view = view;
            _view.OnRenderMapClicked += HandleRenderMapClicked;
            _view.OnPlanetSelected += HandlePlanetSelected;
        }

        private void HandleRenderMapClicked()
        {
            try
            {
                int mapId = _view.MapId;

                // MapService 인스턴스가 초기화되어 있는지 확인
                MapService mapService;
                try
                {
                    mapService = MapService.Instance;
                }
                catch (InvalidOperationException)
                {
                    _view.ShowError("MapService is not initialized. Please ensure MapService.Initialize() is called first.");
                    return;
                }

                // DB에서 맵 데이터 로드
                _mapData = mapService.LoadMapData(mapId);

                if (_mapData == null)
                {
                    _view.ShowError($"Map with ID {mapId} not found in database.");
                    return;
                }

                // 연결 정보를 (FromId, ToId) 튜플 리스트로 변환
                var connections = _mapData.Connections
                    .Select(c => (c.planetFromId, c.planetToId))
                    .ToList();

                _view.DrawMap(_mapData.Planets.ToList(), connections);
            }
            catch (Exception ex)
            {
                _view.ShowError($"Failed to load or render map data:\n{ex.Message}\n{ex.StackTrace}");
            }
        }

        private void HandlePlanetSelected(int planetId)
        {
            var planet = _mapData?.Planets.FirstOrDefault(p => p.Id == planetId);
            if (planet != null)
            {
                Debug.WriteLine($"Planet {planet.Name} (ID: {planet.Id}) was selected.");
            }
        }
    }
}
