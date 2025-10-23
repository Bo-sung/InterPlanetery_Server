using System.Windows;
using ChatClientWPF.Pages;
using ChatClientWPF.Presenters;
using CommonLib;
using CommonLib.Services;

namespace ChatClientWPF.Views
{
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// 테스트 툴의 메인 윈도우 - TabControl 기반 멀티 페이지 구조
	///
	/// 역할:
	/// - 여러 테스트 페이지를 탭으로 관리
	/// - 각 페이지는 독립적인 기능 제공 (Chat Test, Performance Test, Stress Test 등)
	///
	/// 특징:
	/// - Frame을 사용한 Page 네비게이션
	/// - 각 탭은 독립적인 Page 인스턴스
	/// - 향후 테스트 페이지 추가 용이
	/// </summary>
	public partial class MainWindow : Window
	{
		/// <summary>
		/// MainWindow 생성자
		/// TabControl 기반 테스트 툴 초기화
		/// </summary>
		public MainWindow()
		{
			InitializeComponent();

			// AppConfig 초기화 및 확인
			System.Diagnostics.Debug.WriteLine("=== MainWindow 초기화 ===");
			var config = CommonLib.AppConfig.Instance;
			System.Diagnostics.Debug.WriteLine($"AppConfig loaded. DB Server: {config.DatabaseServer}");

			// MapService 초기화 (SingletonBase 패턴 사용)
			var gameDbRepository = new GameDBRepository();
			MapService.Instance.Initialize(gameDbRepository);

            // 맵 시각화 페이지 초기화
            var mapPage = new MapDisplayPage();
            var mapPresenter = new MapDisplayPresenter(mapPage);
            MapFrame.Content = mapPage;

			// 테이블 뷰어 페이지 초기화
			var tablePage = new TableViewPage();
			var tablePresenter = new TableViewPresenter(tablePage);
			TableFrame.Content = tablePage;
		}
	}
}
