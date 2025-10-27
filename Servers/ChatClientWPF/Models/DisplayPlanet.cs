using CommonLib;
using CommonLib.TableData;
using System.Windows.Media;

namespace ChatClientWPF.Models
{
    /// <summary>
    /// UI에 행성을 표시하기 위한 뷰 모델 클래스
    /// </summary>
    public class DisplayPlanet
    {
        public int Id { get; }
        public string Name { get; }
        public Vector2 Position { get; }
        public int Gas { get; }
        public int Mineral { get; }
        public int Supply { get; }

        // UI 바인딩을 위한 추가 속성
        public Brush OwnerColor { get; set; }
        public double ConquestProgress { get; set; }
        public string GarrisonFleetInfo { get; set; }

        public DisplayPlanet(PlanetInfoData info, MapPlanetInfoData layout)
        {
            Id = info.id;
            Name = info.Name;
            Position = new Vector2(layout.PositionX, layout.PositionY);
            Gas = info.Gas;
            Mineral = info.Mineral;
            Supply = info.Supply;

            // 기본값 설정
            OwnerColor = Brushes.Gray; // Neutral
            ConquestProgress = 0;
            GarrisonFleetInfo = string.Empty;
        }
    }
}