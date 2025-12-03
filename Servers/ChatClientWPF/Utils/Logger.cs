using System;
using System.Diagnostics;

namespace ChatClientWPF.Utils
{
    /// <summary>
    /// WPF용 로깅 유틸리티 (UnityEngine.Debug 대체)
    /// UI에 로그를 표시하기 위한 이벤트 제공
    /// </summary>
    public static class Logger
    {
        // UI에서 구독할 수 있는 로그 이벤트
        public static event Action<string>? OnLog;
        public static event Action<string>? OnLogWarning;
        public static event Action<string>? OnLogError;

        /// <summary>
        /// 일반 로그 출력
        /// </summary>
        public static void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var formattedMessage = $"[{timestamp}] {message}";

            Debug.WriteLine($"[LOG] {formattedMessage}");
            OnLog?.Invoke(formattedMessage);
        }

        /// <summary>
        /// 경고 로그 출력
        /// </summary>
        public static void LogWarning(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var formattedMessage = $"[{timestamp}] ⚠️ {message}";

            Debug.WriteLine($"[WARNING] {formattedMessage}");
            OnLogWarning?.Invoke(formattedMessage);
        }

        /// <summary>
        /// 에러 로그 출력
        /// </summary>
        public static void LogError(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var formattedMessage = $"[{timestamp}] ❌ {message}";

            Debug.WriteLine($"[ERROR] {formattedMessage}");
            OnLogError?.Invoke(formattedMessage);
        }

        /// <summary>
        /// 모든 이벤트 구독 해제
        /// </summary>
        public static void ClearSubscribers()
        {
            OnLog = null;
            OnLogWarning = null;
            OnLogError = null;
        }
    }
}
