using System;

namespace ChatClientWPF.Views
{
    /// <summary>
    /// 설정 View가 구현해야 할 인터페이스
    /// </summary>
    public interface ISettingsView
    {
        /// <summary>
        /// 데이터베이스 서버 주소
        /// </summary>
        string DatabaseServer { get; set; }

        /// <summary>
        /// 데이터베이스 사용자 ID
        /// </summary>
        string DatabaseUserId { get; set; }

        /// <summary>
        /// 데이터베이스 비밀번호
        /// </summary>
        string DatabasePassword { get; set; }

        /// <summary>
        /// 데이터베이스 이름
        /// </summary>
        string DatabaseName { get; set; }

        /// <summary>
        /// 데이터베이스 포트
        /// </summary>
        int DatabasePort { get; set; }

        /// <summary>
        /// 서버 호스트
        /// </summary>
        string ServerHost { get; set; }

        /// <summary>
        /// 서버 포트
        /// </summary>
        int ServerPort { get; set; }

        /// <summary>
        /// 연결 문자열 미리보기 업데이트
        /// </summary>
        void UpdateConnectionStringPreview(string connectionString);

        /// <summary>
        /// 사용자에게 성공 메시지를 표시합니다.
        /// </summary>
        void ShowSuccess(string message);

        /// <summary>
        /// 사용자에게 오류 메시지를 표시합니다.
        /// </summary>
        void ShowError(string message);

        /// <summary>
        /// 사용자에게 정보 메시지를 표시합니다.
        /// </summary>
        void ShowInfo(string message);

        /// <summary>
        /// 저장 버튼 클릭 시 발생하는 이벤트
        /// </summary>
        event Action OnSaveClicked;

        /// <summary>
        /// 다시 로드 버튼 클릭 시 발생하는 이벤트
        /// </summary>
        event Action OnReloadClicked;

        /// <summary>
        /// 연결 테스트 버튼 클릭 시 발생하는 이벤트
        /// </summary>
        event Action OnTestConnectionClicked;

        /// <summary>
        /// 파일 덮어쓰기 버튼 클릭 시 발생하는 이벤트
        /// </summary>
        event Action OnOverwriteFileClicked;

        /// <summary>
        /// 설정값 변경 시 발생하는 이벤트
        /// </summary>
        event Action OnSettingsChanged;
    }
}
