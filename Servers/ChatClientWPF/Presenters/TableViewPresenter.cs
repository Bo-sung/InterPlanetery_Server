using ChatClientWPF.Views;
using CommonLib;
using BaseServer.Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ChatClientWPF.Presenters
{
    /// <summary>
    /// TableView의 Presenter - View와 Model 사이의 중재자
    /// DB에서 직접 테이블 데이터를 로드합니다.
    /// </summary>
    public class TableViewPresenter
    {
        private readonly ITableViewView _view;
        private readonly DB_Table _dbTable;
        private readonly Dictionary<string, Func<object>> _tableDataProviders;

        public TableViewPresenter(ITableViewView view)
        {
            _view = view;

            // 이벤트 핸들러 연결
            _view.OnRefreshClicked += HandleRefreshClicked;
            _view.OnTableSelected += HandleTableSelected;

            // DB 테이블 초기화
            _dbTable = new DB_Table();
            LoadTablesFromDB();

            // 테이블 데이터 제공자 초기화
            _tableDataProviders = new Dictionary<string, Func<object>>
            {
                ["fleet_info"] = () => _dbTable.Fleet_info,
                ["map_info"] = () => _dbTable.Map_info,
                ["map_planet_info"] = () => _dbTable.Map_Planet_info,
                ["map_route_info"] = () => _dbTable.Map_Route_info,
                ["planet_info"] = () => _dbTable.Planet_info,
                ["production_info"] = () => _dbTable.Production_info
            };

            // 초기 로드
            LoadTableList();
        }

        /// <summary>
        /// DB에서 테이블 데이터 로드
        /// </summary>
        private void LoadTablesFromDB()
        {
            try
            {
                string connectionString = AppConfig.Instance.DatabaseConnectionString;
                _dbTable.UpdateTable(connectionString);
                Debug.WriteLine($"테이블 데이터 로드 완료");
                Debug.WriteLine($"  - Fleet Info: {_dbTable.Fleet_info.Count}");
                Debug.WriteLine($"  - Map Info: {_dbTable.Map_info.Count}");
                Debug.WriteLine($"  - Map Planet Info: {_dbTable.Map_Planet_info.Count}");
                Debug.WriteLine($"  - Map Route Info: {_dbTable.Map_Route_info.Count}");
                Debug.WriteLine($"  - Planet Info: {_dbTable.Planet_info.Count}");
                Debug.WriteLine($"  - Production Info: {_dbTable.Production_info.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"테이블 데이터 로드 실패: {ex.Message}");
                _view.ShowError($"Failed to load tables from database:\n{ex.Message}");
            }
        }

        /// <summary>
        /// 새로고침 버튼 클릭 핸들러
        /// </summary>
        private void HandleRefreshClicked()
        {
            try
            {
                Debug.WriteLine("=== Refresh Tables 클릭됨 ===");
                LoadTablesFromDB();
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

                if (!_tableDataProviders.ContainsKey(tableName))
                {
                    _view.ShowError($"Unknown table: {tableName}");
                    return;
                }

                var tableData = _tableDataProviders[tableName]();
                int recordCount = GetDictionaryCount(tableData);

                Debug.WriteLine($"테이블 데이터 로드: {tableName} ({recordCount}개 레코드)");
                _view.DisplayTableData(tableName, tableData, recordCount);
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
                Debug.WriteLine("테이블 리스트 로드 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"테이블 리스트 로드 실패: {ex.Message}");
                _view.ShowError($"Failed to load table list:\n{ex.Message}");
            }
        }

        /// <summary>
        /// Dictionary의 Count 속성 가져오기 (리플렉션 사용)
        /// </summary>
        private int GetDictionaryCount(object? data)
        {
            if (data == null)
                return 0;

            var countProperty = data.GetType().GetProperty("Count");
            if (countProperty != null)
            {
                try
                {
                    return (int?)countProperty.GetValue(data) ?? 0;
                }
                catch
                {
                    return 0;
                }
            }
            return 0;
        }
    }
}
