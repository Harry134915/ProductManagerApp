using System.Diagnostics;

namespace ProductManagerApp.Infrastructure.Logging
{
    /// <summary>
    /// 将结构化级别和异常文本写入调试输出窗口。
    /// </summary>
    public sealed class DebugAppLogger : IAppLogger
    {
        public void LogInformation(string message)
        {
            Write("INFO", message);
        }

        public void LogWarning(string message)
        {
            Write("WARN", message);
        }

        public void LogError(string message, Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            Write("ERROR", message, exception);
        }

        private static void Write(
            string level,
            string message,
            Exception? exception = null)
        {
            Debug.WriteLine(
                LogEntryFormatter.Format(
                    DateTimeOffset.Now,
                    level,
                    message,
                    exception));
        }
    }
}
