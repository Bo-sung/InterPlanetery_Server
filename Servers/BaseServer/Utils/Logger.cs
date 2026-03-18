namespace BaseServer.Utils
{
    public static class Logger
    {
        public static void Log(string message)
        {
            var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            System.Console.WriteLine($"[{timestamp}] {message}");
        }
    }
}
