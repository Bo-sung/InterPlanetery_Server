using ChatClientWPF.Database;
using CommonLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace ChatClientWPF.Views
{
	/// <summary>
	/// TableView - 데이터베이스 테이블 뷰어
	/// Presenter 로직이 통합된 버전
	/// </summary>
	public partial class TableView : UserControl
	{
		private readonly Dictionary<string, Func<object>> _tableDataProviders;

		public TableView()
		{
			InitializeComponent();

			// 테이블 데이터 제공자 초기화
			_tableDataProviders = new Dictionary<string, Func<object>>
			{
				["fleet_info"] = () => DBManager.Instance.Table.Fleet_info,
				["map_info"] = () => DBManager.Instance.Table.Map_info,
				["map_planet_info"] = () => DBManager.Instance.Table.Map_Planet_info,
				["map_route_info"] = () => DBManager.Instance.Table.Map_Route_info,
				["planet_info"] = () => DBManager.Instance.Table.Planet_info,
				["production_info"] = () => DBManager.Instance.Table.Production_info
			};

			// 초기 로드
			Loaded += (s, e) => LoadTableList();
		}

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

				TableListBox.Items.Clear();
				foreach (var tableName in tableNames)
				{
					TableListBox.Items.Add(tableName);
				}

				Debug.WriteLine("테이블 리스트 로드 완료");
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"테이블 리스트 로드 실패: {ex.Message}");
				MessageBox.Show($"Failed to load table list:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Debug.WriteLine("=== Refresh Tables 클릭됨 ===");
				DBManager.Instance.UpdateTable(AppConfig.Instance.DatabaseConnectionString);
				LoadTableList();
				MessageBox.Show("Tables refreshed successfully.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"새로고침 실패: {ex.Message}");
				MessageBox.Show($"Failed to refresh tables:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void TableListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (TableListBox.SelectedItem is string selectedTable)
			{
				try
				{
					Debug.WriteLine($"=== 테이블 선택됨: {selectedTable} ===");

					if (!_tableDataProviders.ContainsKey(selectedTable))
					{
						MessageBox.Show($"Unknown table: {selectedTable}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
						return;
					}

					var tableData = _tableDataProviders[selectedTable]();
					int recordCount = GetDictionaryCount(tableData);

					Debug.WriteLine($"테이블 데이터 로드: {selectedTable} ({recordCount}개 레코드)");
					DisplayTableData(selectedTable, tableData, recordCount);
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"테이블 데이터 로드 실패: {ex.Message}");
					MessageBox.Show($"Failed to load table data:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void DisplayTableData(string tableName, object data, int recordCount)
		{
			SelectedTableText.Text = tableName;
			RecordCountText.Text = $"Total Records: {recordCount}";

			// DataGrid에 데이터 바인딩
			if (data != null)
			{
				// Dictionary<int, T> 형식을 List<T>로 변환하여 DataGrid에 바인딩
				var dataType = data.GetType();
				if (dataType.IsGenericType && dataType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
				{
					// Dictionary의 Values를 리스트로 변환
					var valuesProperty = dataType.GetProperty("Values");
					if (valuesProperty != null)
					{
						var values = valuesProperty.GetValue(data);
						var listType = typeof(List<>).MakeGenericType(dataType.GetGenericArguments()[1]);
						var list = Activator.CreateInstance(listType);
						var addMethod = listType.GetMethod("Add");

						foreach (var item in (System.Collections.IEnumerable)values)
						{
							addMethod?.Invoke(list, new[] { item });
						}

						TableDataGrid.ItemsSource = (System.Collections.IEnumerable)list;
					}
				}
				else
				{
					TableDataGrid.ItemsSource = (System.Collections.IEnumerable)data;
				}
			}
			else
			{
				TableDataGrid.ItemsSource = null;
			}
		}

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
