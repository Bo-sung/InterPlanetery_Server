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
                Debug.WriteLine("=== HandleRenderMapClicked 호출됨 ===");
                int mapId = _view.MapId;
                Debug.WriteLine($"Map ID: {mapId}");

                // MapService 인스턴스 획득 (SingletonBase 패턴 사용)
                MapService mapService = MapService.Instance;
                Debug.WriteLine($"MapService.IsInit: {mapService.IsInit}");

                if (!mapService.IsInit)
                {
                    _view.ShowError("MapService is not initialized. Please ensure MapService.Initialize() is called first.");
                    return;
                }

                // DB에서 맵 데이터 로드
                _mapData = mapService.LoadMapData(mapId);
                Debug.WriteLine($"LoadMapData 결과: {(_mapData == null ? "null" : "성공")}");

                if (_mapData == null)
                {
                    _view.ShowError($"Map with ID {mapId} not found in database.");
                    return;
                }

                Debug.WriteLine($"Planets 개수: {_mapData.Planets?.Count() ?? 0}");
                Debug.WriteLine($"Connections 개수: {_mapData.Connections?.Count() ?? 0}");

                if (_mapData.Planets != null)
                {
                    foreach (var planet in _mapData.Planets)
                    {
                        Debug.WriteLine($"  Planet: ID={planet.Id}, Name={planet.Name}, Type={planet.Type}, Pos=({planet.Position.X}, {planet.Position.Y})");
                    }
                }

                // 연결 정보를 (FromId, ToId) 튜플 리스트로 변환
                var connections = _mapData.Connections
                    .Select(c => (c.planetFromId, c.planetToId))
                    .ToList();

                Debug.WriteLine($"DrawMap 호출 전: Planets={_mapData.Planets.Count()}, Connections={connections.Count}");
                _view.DrawMap(_mapData.Planets.ToList(), connections);
                Debug.WriteLine("DrawMap 호출 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"예외 발생: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
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
