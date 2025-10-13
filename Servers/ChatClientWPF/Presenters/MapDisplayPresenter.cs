using ChatClientWPF.Views;
using CommonLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ChatClientWPF.Presenters
{
    public class MapDisplayPresenter
    {
        private readonly IMapDisplayView _view;
        private MapData _mapData;

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
                string planetFilePath = _view.PlanetFilePath;
                string connectionFilePath = _view.ConnectionFilePath;

                if (string.IsNullOrWhiteSpace(planetFilePath) || !File.Exists(planetFilePath))
                {
                    _view.ShowError("Planet data file path is invalid or empty.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(connectionFilePath) || !File.Exists(connectionFilePath))
                {
                    _view.ShowError("Connection data file path is invalid or empty.");
                    return;
                }

                var planets = CsvDataManager.LoadData(planetFilePath, values =>
                    new Planet(
                        int.Parse(values[0]),
                        values[1],
                        Enum.Parse<PlanetType>(values[2]),
                        new Vector2(float.Parse(values[3]), float.Parse(values[4]))
                    )
                );

                var connections = CsvDataManager.LoadData(connectionFilePath, values =>
                    (from: int.Parse(values[0]), to: int.Parse(values[1]))
                );

                // LINQ를 사용하여 즉시 로드
                var planetList = planets.ToList();
                var connectionList = connections.ToList();

                _mapData = new MapData("Custom Map", planetList, connectionList);

                _view.DrawMap(_mapData);
            }
            catch (Exception ex)
            {
                _view.ShowError($"Failed to load or render map data:\n{ex.Message}");
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
