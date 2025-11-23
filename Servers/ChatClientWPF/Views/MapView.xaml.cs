using ChatClientWPF.Controls;
using ChatClientWPF.Database;
using ChatClientWPF.Models;
using CommonLib;
using CommonLib.TableData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ChatClientWPF.Views
{
	public partial class MapView : UserControl
	{
		private readonly Dictionary<int, PlanetControl> _planetControls = new Dictionary<int, PlanetControl>();
		private PlanetControl? _selectedPlanet;

		public MapView()
		{
			InitializeComponent();
		}

		private void RenderMapButton_Click(object sender, RoutedEventArgs e)
		{
			// 로딩 패널 표시
			if (LoadingPanel != null)
			{
				LoadingPanel.Visibility = Visibility.Visible;
			}

			// Map ID 가져오기
			if (!int.TryParse(MapIdTextBox.Text, out int mapId))
			{
				mapId = 1;
			}

			try
			{
				Debug.WriteLine($"=== Map ID {mapId} 로드 시작 ===");

				// DB에서 맵 데이터 로드
				DBManager.Instance.UpdateTable(AppConfig.Instance.DatabaseConnectionString);

				var mapService = new MapService();
				var (planets, connections) = mapService.LoadMapData(mapId);

				Debug.WriteLine($"로드된 행성 수: {planets.Count}");
				Debug.WriteLine($"로드된 연결 수: {connections.Count}");

				// 맵 그리기
				DrawMap(planets, connections);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"맵 로드 실패: {ex.Message}");
				Debug.WriteLine($"StackTrace: {ex.StackTrace}");
				ShowError($"Failed to load map:\n{ex.Message}");
			}
		}

		private void DrawMap(List<DisplayPlanet> planets, List<(int FromId, int ToId)> connections)
		{
			Debug.WriteLine("=== DrawMap 호출됨 ===");
			Debug.WriteLine($"Planets count: {planets?.Count ?? 0}");
			Debug.WriteLine($"Connections count: {connections?.Count ?? 0}");

			// UI 스레드에서 실행되도록 보장
			if (!Dispatcher.CheckAccess())
			{
				Debug.WriteLine("Dispatcher.Invoke 호출");
				Dispatcher.Invoke(() => DrawMap(planets, connections));
				return;
			}

			// 로딩 패널 숨기기
			if (LoadingPanel != null)
			{
				LoadingPanel.Visibility = Visibility.Collapsed;
			}

			MapCanvas.Children.Clear();
			_planetControls.Clear();
			_selectedPlanet = null;

			if (planets == null || planets.Count == 0)
			{
				Debug.WriteLine("행성 데이터가 없습니다!");
				MessageBox.Show("행성 데이터가 없습니다.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			double canvasWidth = MapCanvas.Width;
			double canvasHeight = MapCanvas.Height;

			// 행성들의 실제 좌표 범위 계산
			double minX = planets.Min(p => p.Position.X);
			double maxX = planets.Max(p => p.Position.X);
			double minY = planets.Min(p => p.Position.Y);
			double maxY = planets.Max(p => p.Position.Y);

			double dataWidth = maxX - minX;
			double dataHeight = maxY - minY;

			Debug.WriteLine($"Canvas Size: {canvasWidth}x{canvasHeight}");
			Debug.WriteLine($"Data Range: X[{minX}, {maxX}], Y[{minY}, {maxY}]");
			Debug.WriteLine($"Data Size: {dataWidth}x{dataHeight}");

			// Canvas에 맞게 스케일 자동 계산 (여백 20% 포함)
			double margin = 100; // 픽셀 단위 여백
			double scaleX = (canvasWidth - 2 * margin) / (dataWidth == 0 ? 1 : dataWidth);
			double scaleY = (canvasHeight - 2 * margin) / (dataHeight == 0 ? 1 : dataHeight);
			double scale = Math.Min(scaleX, scaleY); // 작은 쪽 선택하여 모든 행성이 보이도록

			Debug.WriteLine($"Calculated Scale: {scale} (scaleX: {scaleX}, scaleY: {scaleY})");

			// 1. 행성 그리기 (PlanetControl 사용)
			int addedCount = 0;
			foreach (var planet in planets)
			{
				var planetSize = PlanetControl.GetPlanetSize(PlanetType.Terrestrial); // 임시 타입

				// 좌표 변환: 데이터 범위를 Canvas 범위로 매핑
				double canvasX = margin + (planet.Position.X - minX) * scale;
				double canvasY = canvasHeight - margin - (planet.Position.Y - minY) * scale; // Y축 반전

				// PlanetControl 인스턴스 생성
				var planetControl = new PlanetControl();
				planetControl.SetPlanetData(planet, planetSize);

				// 이벤트 핸들러 연결
				planetControl.OnPlanetClicked += HandlePlanetClicked;
				planetControl.OnPlanetMouseEnter += HandlePlanetMouseEnter;
				planetControl.OnPlanetMouseLeave += HandlePlanetMouseLeave;

				// Canvas에 추가
				double minWidth = Math.Max(planetSize, 100);
				Canvas.SetLeft(planetControl, canvasX - minWidth / 2);
				Canvas.SetTop(planetControl, canvasY - planetSize / 2);

				MapCanvas.Children.Add(planetControl);
				_planetControls[planet.Id] = planetControl;
				addedCount++;

				Debug.WriteLine($"  행성 추가: {planet.Name} at ({canvasX}, {canvasY})");
			}

			Debug.WriteLine($"총 {addedCount}개 행성 추가됨");

			// 2. 연결선 그리기
			int lineCount = 0;
			foreach (var conn in connections)
			{
				if (!_planetControls.ContainsKey(conn.FromId) || !_planetControls.ContainsKey(conn.ToId))
					continue;

				var planet1Control = _planetControls[conn.FromId];
				var planet2Control = _planetControls[conn.ToId];

				var planet1Size = PlanetControl.GetPlanetSize(PlanetType.Terrestrial);
				var planet2Size = PlanetControl.GetPlanetSize(PlanetType.Terrestrial);

				double minWidth1 = Math.Max(planet1Size, 100);
				double minWidth2 = Math.Max(planet2Size, 100);

				double x1 = Canvas.GetLeft(planet1Control) + minWidth1 / 2;
				double y1 = Canvas.GetTop(planet1Control) + planet1Size / 2;
				double x2 = Canvas.GetLeft(planet2Control) + minWidth2 / 2;
				double y2 = Canvas.GetTop(planet2Control) + planet2Size / 2;

				var line = new Line
				{
					X1 = x1,
					Y1 = y1,
					X2 = x2,
					Y2 = y2,
					Stroke = Brushes.DarkGray,
					StrokeThickness = 0.5
				};

				MapCanvas.Children.Insert(0, line);
				lineCount++;
			}

			Debug.WriteLine($"총 {lineCount}개 연결선 추가됨");
			Debug.WriteLine($"=== 최종 MapCanvas.Children.Count: {MapCanvas.Children.Count} ===");

			MapCanvas.UpdateLayout();
			MapCanvas.InvalidateVisual();
		}

		private void ShowError(string message)
		{
			// 로딩 패널 숨기기
			if (LoadingPanel != null && Dispatcher.CheckAccess())
			{
				LoadingPanel.Visibility = Visibility.Collapsed;
			}
			else if (LoadingPanel != null)
			{
				Dispatcher.Invoke(() => LoadingPanel.Visibility = Visibility.Collapsed);
			}

			MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		private void HandlePlanetClicked(DisplayPlanet planet)
		{
			if (_selectedPlanet != null)
			{
				_selectedPlanet.Deselect();
			}

			if (_planetControls.ContainsKey(planet.Id))
			{
				_selectedPlanet = _planetControls[planet.Id];
				_selectedPlanet.Select();

				Debug.WriteLine($"Planet {planet.Name} (ID: {planet.Id}) was selected.");
			}
		}

		private void HandlePlanetMouseEnter(DisplayPlanet planet)
		{
			PlanetInfoPanel.Visibility = Visibility.Visible;
			PlanetInfoText.Text = $"ID: {planet.Id}\nName: {planet.Name}\nPosition: ({planet.Position.X}, {planet.Position.Y})";
			Cursor = Cursors.Hand;
		}

		private void HandlePlanetMouseLeave(DisplayPlanet planet)
		{
			PlanetInfoPanel.Visibility = Visibility.Collapsed;
			Cursor = Cursors.Arrow;
		}
	}
}
