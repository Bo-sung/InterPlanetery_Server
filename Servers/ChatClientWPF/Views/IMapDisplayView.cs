using CommonLib;
using CommonLib.TableData;
using System;
using System.Collections.Generic;

namespace ChatClientWPF.Views
{
    /// <summary>
    /// 맵 시각화 View가 구현해야 할 인터페이스
    /// </summary>
    public interface IMapDisplayView
    {
        int MapId { get; }

        /// <summary>
        /// 맵 데이터를 기반으로 캔버스에 모든 요소를 그립니다.
        /// </summary>
        void DrawMap(List<Planet> planets, List<(int FromId, int ToId)> connections);

        /// <summary>
        /// 사용자에게 오류 메시지를 표시합니다.
        /// </summary>
        void ShowError(string message);

        /// <summary>
        /// '맵 생성' 버튼 클릭 시 발생하는 이벤트
        /// </summary>
        event Action OnRenderMapClicked;

        /// <summary>
        /// 특정 행성을 클릭했을 때 발생하는 이벤트
        /// </summary>
        event Action<int> OnPlanetSelected;
    }
}
