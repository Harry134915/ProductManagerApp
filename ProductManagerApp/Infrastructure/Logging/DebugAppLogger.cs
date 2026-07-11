using System.Diagnostics;

namespace ProductManagerApp.Infrastructure.Logging
{
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
            Write("ERROR", message);
            Debug.WriteLine(exception);
        }

        private static void Write(string level, string message)
        {
            Debug.WriteLine(
                $"[{DateTimeOffset.Now:O}] [{level}] {message}");
        }
    }
}
