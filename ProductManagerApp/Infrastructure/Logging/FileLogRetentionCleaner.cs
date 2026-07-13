using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace ProductManagerApp.Infrastructure.Logging
{
    /// <summary>
    /// 仅清理日志目录第一层中可确认已过期的应用日志文件。
    /// </summary>
    internal static class FileLogRetentionCleaner
    {
        private const string FileNamePrefix = "ProductManagerApp-";
        private const string FileExtension = ".log";

        public static void Cleanup(
            string logDirectory,
            int retentionDays,
            DateTimeOffset now)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retentionDays);

            try
            {
                if (!Directory.Exists(logDirectory))
                {
                    return;
                }

                var expirationThreshold = now.AddDays(-retentionDays);
                var oldestRetainedDate = DateOnly.FromDateTime(
                    expirationThreshold.LocalDateTime);
                var currentDate = DateOnly.FromDateTime(now.LocalDateTime);

                foreach (var filePath in Directory.EnumerateFiles(
                    logDirectory,
                    $"{FileNamePrefix}*{FileExtension}",
                    SearchOption.TopDirectoryOnly))
                {
                    TryDeleteExpiredFile(
                        filePath,
                        currentDate,
                        oldestRetainedDate,
                        expirationThreshold.UtcDateTime);
                }
            }
            catch (Exception exception)
            {
                ReportCleanupFailure(logDirectory, exception);
            }
        }

        private static void TryDeleteExpiredFile(
            string filePath,
            DateOnly currentDate,
            DateOnly oldestRetainedDate,
            DateTime expirationThresholdUtc)
        {
            try
            {
                if (!TryGetLogDate(filePath, out var logDate)
                    || logDate == currentDate
                    || logDate >= oldestRetainedDate)
                {
                    return;
                }

                // 文件名和最后写入时间必须同时过期，避免误删被恢复或手动更新的日志。
                if (File.GetLastWriteTimeUtc(filePath) >= expirationThresholdUtc)
                {
                    return;
                }

                File.Delete(filePath);
            }
            catch (Exception exception)
            {
                ReportCleanupFailure(filePath, exception);
            }
        }

        private static bool TryGetLogDate(string filePath, out DateOnly logDate)
        {
            logDate = default;
            if (!string.Equals(
                Path.GetExtension(filePath),
                FileExtension,
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            if (!fileName.StartsWith(FileNamePrefix, StringComparison.Ordinal))
            {
                return false;
            }

            return DateOnly.TryParseExact(
                fileName[FileNamePrefix.Length..],
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out logDate);
        }

        private static void ReportCleanupFailure(string path, Exception exception)
        {
            // 清理属于维护能力，失败时保留日志文件并继续应用启动。
            Debug.WriteLine(
                LogEntryFormatter.Format(
                    DateTimeOffset.Now,
                    "WARN",
                    $"清理过期日志失败：{path}",
                    exception));
        }
    }
}
