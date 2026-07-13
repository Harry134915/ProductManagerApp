using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace ProductManagerApp.Infrastructure.Logging
{
    /// <summary>
    /// 以 UTF-8 格式将日志线程安全地追加到按日期划分的本地文件。
    /// </summary>
    public sealed class FileAppLogger : IAppLogger
    {
        public const int DefaultRetentionDays = 30;

        private const string FileNamePrefix = "ProductManagerApp-";
        private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);
        private readonly object _writeLock = new();
        private readonly string _logDirectory;

        public FileAppLogger(
            string logDirectory,
            int retentionDays = DefaultRetentionDays)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retentionDays);
            _logDirectory = Path.GetFullPath(logDirectory);

            // 应用中日志器是单例，因此清理只在启动构造阶段执行一次。
            FileLogRetentionCleaner.Cleanup(
                _logDirectory,
                retentionDays,
                DateTimeOffset.Now);
        }

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

        private void Write(string level, string message, Exception? exception = null)
        {
            var timestamp = DateTimeOffset.Now;
            var entry = LogEntryFormatter.Format(timestamp, level, message, exception);

            try
            {
                lock (_writeLock)
                {
                    Directory.CreateDirectory(_logDirectory);
                    File.AppendAllText(
                        GetLogFilePath(timestamp),
                        entry + Environment.NewLine,
                        Utf8WithoutBom);
                }
            }
            catch (Exception writeException)
            {
                // 日志属于诊断能力，文件系统故障不能反向中断业务操作。
                Debug.WriteLine(
                    LogEntryFormatter.Format(
                        DateTimeOffset.Now,
                        "ERROR",
                        "写入应用日志文件失败。",
                        writeException));
            }
        }

        private string GetLogFilePath(DateTimeOffset timestamp)
        {
            var date = timestamp.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return Path.Combine(_logDirectory, $"{FileNamePrefix}{date}.log");
        }
    }
}
