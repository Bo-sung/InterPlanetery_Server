using ChatClientWPF.Views;
using CommonLib;
using CommonLib.TableData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ChatClientWPF.Pages
{
    public partial class MapDisplayPage : Page, IMapDisplayView
    {
        private readonly Dictionary<int, FrameworkElement> _planetUIElements = new Dictionary<int, FrameworkElement>();

        public int MapId
        {
            get
            {
                if (int.TryParse(MapIdTextBox.Text, out int mapId))
                {
                    return mapId;
                }
                return 1; // 파싱 실패 시 기본값
            }
        }

        public event Action? OnRenderMapClicked;
        public event Action<int>? OnPlanetSelected;

        public MapDisplayPage()
        {
            InitializeComponent();
        }

        public void DrawMap(List<Planet> planets, List<(int FromId, int ToId)> connections)
        {
            MapCanvas.Children.Clear();
            _planetUIElements.Clear();

            const double scale = 50.0; // 고정 스케일
            double canvasWidth = MapCanvas.Width;
            double canvasHeight = MapCanvas.Height;
            double centerX = canvasWidth / 2;
            double centerY = canvasHeight / 2;

            // 1. 행성 그리기
            foreach (var planet in planets)
            {
                var planetSize = GetPlanetSize(planet.Type);
                var planetBrush = GetPlanetBrush(planet.Type);

                // 좌표 변환
                double canvasX = centerX + (planet.Position.X * scale);
                double canvasY = centerY - (planet.Position.Y * scale); // Y축은 반대

                var ellipse = new Ellipse
                {
                    Width = planetSize,
                    Height = planetSize,
                    Fill = planetBrush,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1,
                    Tag = planet
                };

                var textBlock = new TextBlock
                {
                    Text = planet.Name,
                    Foreground = Brushes.White,
                    FontSize = 10,
                    IsHitTestVisible = false
                };

                Canvas.SetLeft(ellipse, canvasX - planetSize / 2);
                Canvas.SetTop(ellipse, canvasY - planetSize / 2);
                Canvas.SetLeft(textBlock, canvasX + planetSize / 2 + 5);
                Canvas.SetTop(textBlock, canvasY - 8);

                ellipse.MouseDown += Planet_MouseDown;
                ellipse.MouseEnter += Planet_MouseEnter;
                ellipse.MouseLeave += Planet_MouseLeave;

                MapCanvas.Children.Add(ellipse);
                MapCanvas.Children.Add(textBlock);
                _planetUIElements[planet.Id] = ellipse;
            }

            // 2. 연결선 그리기
            foreach (var conn in connections)
            {
                if (!_planetUIElements.ContainsKey(conn.FromId) || !_planetUIElements.ContainsKey(conn.ToId))
                    continue;

                var planet1UI = _planetUIElements[conn.FromId];
                var planet2UI = _planetUIElements[conn.ToId];

                var line = new Line
                {
                    X1 = Canvas.GetLeft(planet1UI) + planet1UI.Width / 2,
                    Y1 = Canvas.GetTop(planet1UI) + planet1UI.Height / 2,
                    X2 = Canvas.GetLeft(planet2UI) + planet2UI.Width / 2,
                    Y2 = Canvas.GetTop(planet2UI) + planet2UI.Height / 2,
                    Stroke = Brushes.DarkGray,
                    StrokeThickness = 0.5
                };

                MapCanvas.Children.Insert(0, line);
            }
        }

        public void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void RenderMapButton_Click(object sender, RoutedEventArgs e)
        {
            OnRenderMapClicked?.Invoke();
        }

        private void Planet_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is Planet planet)
            {
                OnPlanetSelected?.Invoke(planet.Id);
                element.Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Cyan, BlurRadius = 15 };
            }
        }

        private void Planet_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is Planet planet)
            {
                PlanetInfoPanel.Visibility = Visibility.Visible;
                PlanetInfoText.Text = $"ID: {planet.Id}\nName: {planet.Name}\nType: {planet.Type}\nPosition: ({planet.Position.X}, {planet.Position.Y})";
                Cursor = Cursors.Hand;
            }
        }

        private void Planet_MouseLeave(object sender, MouseEventArgs e)
        {
            PlanetInfoPanel.Visibility = Visibility.Collapsed;
            Cursor = Cursors.Arrow;
        }

        private double GetPlanetSize(PlanetType type)
        {
            switch (type)
            {
                case PlanetType.GasGiant: return 30;
                case PlanetType.IceGiant: return 24;
                case PlanetType.Terrestrial: return 16;
                case PlanetType.DwarfPlanet: return 10;
                default: return 12;
            }
        }

        private Brush GetPlanetBrush(PlanetType type)
        {
            switch (type)
            {
                case PlanetType.GasGiant: return Brushes.OrangeRed;
                case PlanetType.IceGiant: return Brushes.LightBlue;
                case PlanetType.Terrestrial: return Brushes.SeaGreen;
                case PlanetType.DwarfPlanet: return Brushes.SlateGray;
                default: return Brushes.White;
            }
        }
    }
}
