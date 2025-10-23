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
        private readonly GameDBRepository _repository;
        private DB_Table? _tableCache;

        // 테이블 이름과 실제 Dictionary 매핑
        private readonly Dictionary<string, Func<object>> _tableDataProviders;

        public TableViewPresenter(ITableViewView view)
        {
            _view = view;
            _repository = new GameDBRepository();

            // 이벤트 핸들러 연결
            _view.OnRefreshClicked += HandleRefreshClicked;
            _view.OnTableSelected += HandleTableSelected;

            // 테이블 데이터 제공자 초기화
            _tableDataProviders = new Dictionary<string, Func<object>>
            {
                ["fleet_info"] = () => _tableCache?.Fleet_info,
                ["map_info"] = () => _tableCache?.Map_info,
                ["map_planet_info"] = () => _tableCache?.Map_Planet_info,
                ["map_route_info"] = () => _tableCache?.Map_Route_info,
                ["planet_info"] = () => _tableCache?.Planet_info,
                ["production_info"] = () => _tableCache?.Production_info
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

                if (_tableCache == null)
                {
                    _view.ShowError("Table cache is not loaded. Please refresh tables first.");
                    return;
                }

                // 테이블 이름에 해당하는 데이터 가져오기
                if (_tableDataProviders.ContainsKey(tableName))
                {
                    var data = _tableDataProviders[tableName]();
                    if (data == null)
                    {
                        _view.ShowError($"No data found for table: {tableName}");
                        return;
                    }

                    // Dictionary의 Count 가져오기
                    int count = GetDictionaryCount(data);
                    Debug.WriteLine($"{tableName} 레코드 개수: {count}");

                    _view.DisplayTableData(tableName, data, count);
                }
                else
                {
                    _view.ShowError($"Unknown table: {tableName}");
                }
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
            try
            {
                Debug.WriteLine("=== 테이블 리스트 로드 시작 ===");

                // DB에서 테이블 캐시 로드
                _repository.UpdateTableCache();
                _tableCache = _repository.GetTableCache();

                if (_tableCache == null)
                {
                    _view.ShowError("Failed to load table cache from database.");
                    return;
                }

                Debug.WriteLine($"Fleet_info: {_tableCache.Fleet_info.Count} records");
                Debug.WriteLine($"Map_info: {_tableCache.Map_info.Count} records");
                Debug.WriteLine($"Map_Planet_info: {_tableCache.Map_Planet_info.Count} records");
                Debug.WriteLine($"Map_Route_info: {_tableCache.Map_Route_info.Count} records");
                Debug.WriteLine($"Planet_info: {_tableCache.Planet_info.Count} records");
                Debug.WriteLine($"Production_info: {_tableCache.Production_info.Count} records");

                // 테이블 이름 리스트 생성
                var tableNames = new List<string>
                {
                    "fleet_info",
                    "map_info",
                    "map_planet_info",
                    "map_route_info",
                    "planet_info",
                    "production_info"
                };

                _view.DisplayTableList(tableNames);
                Debug.WriteLine("=== 테이블 리스트 로드 완료 ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"테이블 리스트 로드 실패: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                _view.ShowError($"Failed to load table list:\n{ex.Message}");
            }
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
