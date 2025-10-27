using CommonLib;
using CommonLib.TableData;
using ChatClientWPF.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace ChatClientWPF.Controls
{
    /// <summary>
    /// PlanetControl - Unity Prefab처럼 재사용 가능한 행성 컨트롤
    /// </summary>
    public partial class PlanetControl : UserControl
    {
        /// <summary>
        /// 행성 데이터
        /// </summary>
        public DisplayPlanet? PlanetData { get; private set; }

        /// <summary>
        /// 행성 클릭 이벤트
        /// </summary>
        public event Action<DisplayPlanet>? OnPlanetClicked;

        /// <summary>
        /// 마우스 진입 이벤트
        /// </summary>
        public event Action<DisplayPlanet>? OnPlanetMouseEnter;

        /// <summary>
        /// 마우스 이탈 이벤트
        /// </summary>
        public event Action<DisplayPlanet>? OnPlanetMouseLeave;

        public PlanetControl()
        {
            InitializeComponent();

            // 이벤트 핸들러 연결
            PlanetEllipse.MouseDown += PlanetEllipse_MouseDown;
            PlanetEllipse.MouseEnter += PlanetEllipse_MouseEnter;
            PlanetEllipse.MouseLeave += PlanetEllipse_MouseLeave;
        }

        /// <summary>
        /// 행성 데이터를 설정하고 UI를 업데이트합니다.
        /// </summary>
        public void SetPlanetData(DisplayPlanet planet, double planetSize)
        {
            PlanetData = planet;

            // 자원 정보 업데이트
            GasText.Text = planet.Gas.ToString();
            MineralText.Text = planet.Mineral.ToString();
            SupplyText.Text = planet.Supply.ToString();

            // 행성 크기 설정
            PlanetEllipse.Width = planetSize;
            PlanetEllipse.Height = planetSize;
            PlanetGrid.Width = planetSize;
            PlanetGrid.Height = planetSize;

            // 행성 색상 설정 (임시로 Terrestrial 타입 사용)
            PlanetEllipse.Fill = GetPlanetBrush(PlanetType.Terrestrial);

            // 행성 이름 설정
            PlanetNameText.Text = planet.Name;

            // RootPanel 크기를 충분히 크게 설정하여 자원 정보와 이름이 잘리지 않도록
            // 최소 너비는 자원 패널 너비와 행성 크기 중 큰 값
            RootPanel.MinWidth = Math.Max(planetSize, 100); // 최소 100px 확보
        }

        /// <summary>
        /// 행성 원의 중심 오프셋을 반환합니다 (Canvas 위치 계산용)
        /// </summary>
        public Point GetPlanetCenterOffset()
        {
            // StackPanel 구조: [행성] - [이름] - [자원]
            // 행성 원의 중심 = (행성크기/2, 행성크기/2)
            if (PlanetData == null) return new Point(0, 0);

            // 임시로 Terrestrial 타입 사용
            double planetSize = GetPlanetSize(PlanetType.Terrestrial);
            return new Point(planetSize / 2, planetSize / 2);
        }

        /// <summary>
        /// 행성을 선택 상태로 표시합니다.
        /// </summary>
        public void Select()
        {
            // 빛나는 효과 추가
            var glowAnimation = new DoubleAnimation
            {
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(300),
                AutoReverse = false
            };

            GlowEffect.Color = Colors.Cyan;
            GlowEffect.BlurRadius = 15;
            GlowEffect.BeginAnimation(DropShadowEffect.OpacityProperty, glowAnimation);
        }

        /// <summary>
        /// 행성 선택을 해제합니다.
        /// </summary>
        public void Deselect()
        {
            var glowAnimation = new DoubleAnimation
            {
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(300),
                AutoReverse = false
            };

            GlowEffect.BeginAnimation(DropShadowEffect.OpacityProperty, glowAnimation);
        }

        /// <summary>
        /// 행성에 하이라이트 효과를 적용합니다.
        /// </summary>
        public void Highlight()
        {
            // 행성 크기를 약간 키우는 애니메이션
            var scaleTransform = new ScaleTransform(1.0, 1.0);
            PlanetEllipse.RenderTransform = scaleTransform;
            PlanetEllipse.RenderTransformOrigin = new Point(0.5, 0.5);

            var scaleAnimation = new DoubleAnimation
            {
                To = 1.2,
                Duration = TimeSpan.FromMilliseconds(200),
                AutoReverse = false
            };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);

            // 자원 패널 밝기 증가
            var opacityAnimation = new DoubleAnimation
            {
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(200),
                AutoReverse = false
            };
            ResourcePanel.BeginAnimation(OpacityProperty, opacityAnimation);
        }

        /// <summary>
        /// 하이라이트 효과를 제거합니다.
        /// </summary>
        public void RemoveHighlight()
        {
            var scaleTransform = PlanetEllipse.RenderTransform as ScaleTransform;
            if (scaleTransform != null)
            {
                var scaleAnimation = new DoubleAnimation
                {
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    AutoReverse = false
                };

                scaleAnimation.Completed += (s, e) =>
                {
                    PlanetEllipse.RenderTransform = null;
                };

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            }

            // 자원 패널 밝기 원복
            var opacityAnimation = new DoubleAnimation
            {
                To = 0.85,
                Duration = TimeSpan.FromMilliseconds(200),
                AutoReverse = false
            };
            ResourcePanel.BeginAnimation(OpacityProperty, opacityAnimation);
        }

        private void PlanetEllipse_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (PlanetData != null)
            {
                OnPlanetClicked?.Invoke(PlanetData);
            }
        }

        private void PlanetEllipse_MouseEnter(object sender, MouseEventArgs e)
        {
            Highlight();
            if (PlanetData != null)
            {
                OnPlanetMouseEnter?.Invoke(PlanetData);
            }
        }

        private void PlanetEllipse_MouseLeave(object sender, MouseEventArgs e)
        {
            RemoveHighlight();
            if (PlanetData != null)
            {
                OnPlanetMouseLeave?.Invoke(PlanetData);
            }
        }

        /// <summary>
        /// 행성 타입에 따른 색상 브러시 반환
        /// </summary>
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

        /// <summary>
        /// 행성 타입에 따른 크기 반환
        /// </summary>
        public static double GetPlanetSize(PlanetType type)
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
    }
}