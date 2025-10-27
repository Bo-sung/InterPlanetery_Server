using ChatClientWPF.Views;
using CommonLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ChatClientWPF.Presenters
{
    /// <summary>
    /// TableView의 Presenter - View와 Model 사이의 중재자
    /// </summary>
    public class TableViewPresenter
    {
        private readonly ITableViewView _view;

        // 테이블 이름과 실제 Dictionary 매핑
        private readonly Dictionary<string, Func<object>> _tableDataProviders;

        public TableViewPresenter(ITableViewView view)
        {
            _view = view;

            // 이벤트 핸들러 연결
            _view.OnRefreshClicked += HandleRefreshClicked;
            _view.OnTableSelected += HandleTableSelected;

            // 테이블 데이터 제공자 초기화 (임시)
            _tableDataProviders = new Dictionary<string, Func<object>> 
            {
                ["fleet_info"] = () => null,
                ["map_info"] = () => null,
                ["map_planet_info"] = () => null,
                ["map_route_info"] = () => null,
                ["planet_info"] = () => null,
                ["production_info"] = () => null
            };

            // 초기 로드
            LoadTableList();
        }

        /// <summary>
        /// 새로고침 버튼 클릭 핸들러
        /// </summary>
        private void HandleRefreshClicked()
        {
            try
            {
                Debug.WriteLine("=== Refresh Tables 클릭됨 ===");
                LoadTableList();
                _view.ShowInfo("Tables refreshed successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"새로고침 실패: {ex.Message}");
                _view.ShowError($"Failed to refresh tables:\n{ex.Message}");
            }
        }

        /// <summary>
        /// 테이블 선택 핸들러
        /// </summary>
        private void HandleTableSelected(string tableName)
        {
            try
            {
                Debug.WriteLine($"=== 테이블 선택됨: {tableName} ===");

                // 이 부분은 ChatClientModel을 통해 서버와 통신하도록 재설계될 예정입니다.
                _view.ShowError("테이블 데이터 로딩 기능이 아직 구현되지 않았습니다. (클라이언트-서버 통신 필요)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"테이블 데이터 로드 실패: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                _view.ShowError($"Failed to load table data:\n{ex.Message}");
            }
        }

        /// <summary>
        /// 테이블 리스트를 로드하고 View에 표시
        /// </summary>
        private void LoadTableList()
        {
            // 이 부분은 ChatClientModel을 통해 서버와 통신하도록 재설계될 예정입니다.
            _view.ShowError("테이블 리스트 로딩 기능이 아직 구현되지 않았습니다. (클라이언트-서버 통신 필요)");
        }

        /// <summary>
        /// Dictionary의 Count 속성 가져오기 (리플렉션 사용)
        /// </summary>
        private int GetDictionaryCount(object data)
        {
            var countProperty = data.GetType().GetProperty("Count");
            if (countProperty != null)
            {
                return (int)countProperty.GetValue(data);
            }
            return 0;
        }
    }
}
