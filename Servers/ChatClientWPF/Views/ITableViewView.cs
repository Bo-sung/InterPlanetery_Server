using System;
using System.Collections.Generic;

namespace ChatClientWPF.Views
{
    /// <summary>
    /// 테이블 뷰어 View가 구현해야 할 인터페이스
    /// </summary>
    public interface ITableViewView
    {
        /// <summary>
        /// 테이블 리스트를 UI에 표시합니다.
        /// </summary>
        void DisplayTableList(List<string> tableNames);

        /// <summary>
        /// 선택된 테이블의 데이터를 표시합니다.
        /// </summary>
        void DisplayTableData(string tableName, object data, int recordCount);

        /// <summary>
        /// 사용자에게 오류 메시지를 표시합니다.
        /// </summary>
        void ShowError(string message);

        /// <summary>
        /// 사용자에게 정보 메시지를 표시합니다.
        /// </summary>
        void ShowInfo(string message);

        /// <summary>
        /// 새로고침 버튼 클릭 시 발생하는 이벤트
        /// </summary>
        event Action OnRefreshClicked;

        /// <summary>
        /// 테이블 선택 시 발생하는 이벤트
        /// </summary>
        event Action<string> OnTableSelected;
    }
}
