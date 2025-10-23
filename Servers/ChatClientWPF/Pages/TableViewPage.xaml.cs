using ChatClientWPF.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ChatClientWPF.Pages
{
    /// <summary>
    /// TableViewPage - 데이터베이스 테이블 뷰어 페이지
    /// 좌측: 테이블 리스트
    /// 우측: 선택된 테이블의 컬럼과 로우 데이터
    /// </summary>
    public partial class TableViewPage : Page, ITableViewView
    {
        public event Action? OnRefreshClicked;
        public event Action<string>? OnTableSelected;

        public TableViewPage()
        {
            InitializeComponent();
        }

        public void DisplayTableList(List<string> tableNames)
        {
            // UI 스레드에서 실행되도록 보장
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => DisplayTableList(tableNames));
                return;
            }

            TableListBox.Items.Clear();
            foreach (var tableName in tableNames)
            {
                TableListBox.Items.Add(tableName);
            }
        }

        public void DisplayTableData(string tableName, object data, int recordCount)
        {
            // UI 스레드에서 실행되도록 보장
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => DisplayTableData(tableName, data, recordCount));
                return;
            }

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

        public void ShowError(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ShowError(message));
                return;
            }

            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowInfo(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ShowInfo(message));
                return;
            }

            MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            OnRefreshClicked?.Invoke();
        }

        private void TableListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TableListBox.SelectedItem is string selectedTable)
            {
                OnTableSelected?.Invoke(selectedTable);
            }
        }
    }
}
