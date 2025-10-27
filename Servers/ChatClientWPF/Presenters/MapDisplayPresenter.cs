using ChatClientWPF.Models;
using ChatClientWPF.Views;
using CommonLib;
using CommonLib.TableData;
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

        public MapDisplayPresenter(IMapDisplayView view)
        {
            _view = view;
            _view.OnRenderMapClicked += HandleRenderMapClicked;
            _view.OnPlanetSelected += HandlePlanetSelected;
        }

        private void HandleRenderMapClicked()
        {
            // 이 부분은 ChatClientModel을 통해 서버와 통신하도록 재설계될 예정입니다.
            // 현재는 BaseServer.Services.MapService에 대한 직접적인 참조를 제거합니다.
            Debug.WriteLine("HandleRenderMapClicked: MapService 직접 호출 제거됨. ChatClientModel을 통한 통신 대기중.");
            _view.ShowError("맵 로딩 기능이 아직 구현되지 않았습니다. (클라이언트-서버 통신 필요)");
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
