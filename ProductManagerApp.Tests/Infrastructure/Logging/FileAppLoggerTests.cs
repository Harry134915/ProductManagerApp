using ProductManagerApp.Infrastructure.Logging;
using ProductManagerApp.Tests.Fakes;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ProductManagerApp.Tests.Infrastructure.Logging;

/// <summary>
/// 验证文件日志格式、并发写入、过期清理、失败降级和组合日志故障隔离。
/// </summary>
public class FileAppLoggerTests
{
    private static readonly DateTimeOffset RetentionNow = new(
        2026,
        7,
        13,
        12,
        0,
        0,
        TimeSpan.FromHours(8));

    [Fact]
    public void LogMethods_WriteDailyUtf8FileWithLevelsAndException()
    {
        var logDirectory = CreateTemporaryDirectory();
        var logger = new FileAppLogger(logDirectory);
        var exception = new InvalidOperationException("database details");

        try
        {
            logger.LogInformation("应用启动。");
            logger.LogWarning("表单校验失败。");
            logger.LogError("数据库访问失败。", exception);

            var logFile = Assert.Single(
                Directory.GetFiles(logDirectory, "ProductManagerApp-*.log"));
            Assert.Matches(
                new Regex(@"ProductManagerApp-\d{4}-\d{2}-\d{2}\.log"),
                Path.GetFileName(logFile));

            var content = File.ReadAllText(logFile);
            Assert.Contains("[INFO] 应用启动。", content);
            Assert.Contains("[WARN] 表单校验失败。", content);
            Assert.Contains("[ERROR] 数据库访问失败。", content);
            Assert.Contains(nameof(InvalidOperationException), content);
            Assert.Contains("database details", content);
        }
        finally
        {
            Directory.Delete(logDirectory, recursive: true);
        }
    }

    [Fact]
    public void LogInformation_WhenCalledConcurrently_DoesNotLoseEntries()
    {
        var logDirectory = CreateTemporaryDirectory();
        var logger = new FileAppLogger(logDirectory);

        try
        {
            Parallel.For(
                0,
                50,
                index => logger.LogInformation($"entry-{index:D2}"));

            var logFile = Assert.Single(
                Directory.GetFiles(logDirectory, "ProductManagerApp-*.log"));
            var lines = File.ReadAllLines(logFile);

            Assert.Equal(50, lines.Length);
            for (var index = 0; index < 50; index++)
            {
                Assert.Contains(
                    lines,
                    line => line.EndsWith($"entry-{index:D2}", StringComparison.Ordinal));
            }
        }
        finally
        {
            Directory.Delete(logDirectory, recursive: true);
        }
    }

    [Fact]
    public void LogInformation_WhenDirectoryCannotBeCreated_DoesNotThrow()
    {
        var temporaryDirectory = CreateTemporaryDirectory();
        var filePath = Path.Combine(temporaryDirectory, "not-a-directory");
        File.WriteAllText(filePath, "occupied");
        var logger = new FileAppLogger(filePath);

        try
        {
            var exception = Record.Exception(
                () => logger.LogInformation("这条日志无法写入文件。"));

            Assert.Null(exception);
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Fact]
    public void RetentionCleanup_WhenNameAndLastWriteAreExpired_DeletesLog()
    {
        var logDirectory = CreateTemporaryDirectory();
        var expiredLog = CreateLogFile(
            logDirectory,
            RetentionNow.AddDays(-60),
            RetentionNow.AddDays(-60));

        try
        {
            FileLogRetentionCleaner.Cleanup(logDirectory, 30, RetentionNow);

            Assert.False(File.Exists(expiredLog));
        }
        finally
        {
            Directory.Delete(logDirectory, recursive: true);
        }
    }

    [Fact]
    public void RetentionCleanup_WhenOnlyOneAgeSignalIsExpired_PreservesLogs()
    {
        var logDirectory = CreateTemporaryDirectory();
        var oldNameWithRecentWrite = CreateLogFile(
            logDirectory,
            RetentionNow.AddDays(-60),
            RetentionNow);
        var recentNameWithOldWrite = CreateLogFile(
            logDirectory,
            RetentionNow.AddDays(-5),
            RetentionNow.AddDays(-60));

        try
        {
            FileLogRetentionCleaner.Cleanup(logDirectory, 30, RetentionNow);

            Assert.True(File.Exists(oldNameWithRecentWrite));
            Assert.True(File.Exists(recentNameWithOldWrite));
        }
        finally
        {
            Directory.Delete(logDirectory, recursive: true);
        }
    }

    [Fact]
    public void RetentionCleanup_CurrentLog_PreservesFile()
    {
        var logDirectory = CreateTemporaryDirectory();
        var currentLog = CreateLogFile(
            logDirectory,
            RetentionNow,
            RetentionNow.AddDays(-60));

        try
        {
            FileLogRetentionCleaner.Cleanup(logDirectory, 30, RetentionNow);

            Assert.True(File.Exists(currentLog));
        }
        finally
        {
            Directory.Delete(logDirectory, recursive: true);
        }
    }

    [Fact]
    public void RetentionCleanup_UnknownAndNestedFiles_PreservesFiles()
    {
        var logDirectory = CreateTemporaryDirectory();
        var unknownFile = Path.Combine(logDirectory, "notes.log");
        File.WriteAllText(unknownFile, "keep");
        File.SetLastWriteTimeUtc(unknownFile, RetentionNow.AddDays(-60).UtcDateTime);

        var nestedDirectory = Path.Combine(logDirectory, "archive");
        Directory.CreateDirectory(nestedDirectory);
        var nestedLog = CreateLogFile(
            nestedDirectory,
            RetentionNow.AddDays(-60),
            RetentionNow.AddDays(-60));

        try
        {
            FileLogRetentionCleaner.Cleanup(logDirectory, 30, RetentionNow);

            Assert.True(File.Exists(unknownFile));
            Assert.True(File.Exists(nestedLog));
        }
        finally
        {
            Directory.Delete(logDirectory, recursive: true);
        }
    }

    [Fact]
    public void RetentionCleanup_WhenExpiredLogIsLocked_DoesNotThrow()
    {
        var logDirectory = CreateTemporaryDirectory();
        var expiredLog = CreateLogFile(
            logDirectory,
            RetentionNow.AddDays(-60),
            RetentionNow.AddDays(-60));
        FileStream? lockStream = null;

        try
        {
            lockStream = new FileStream(
                expiredLog,
                FileMode.Open,
                FileAccess.Read,
                FileShare.None);

            var exception = Record.Exception(
                () => FileLogRetentionCleaner.Cleanup(logDirectory, 30, RetentionNow));

            Assert.Null(exception);
            Assert.True(File.Exists(expiredLog));
        }
        finally
        {
            lockStream?.Dispose();
            Directory.Delete(logDirectory, recursive: true);
        }
    }

    [Fact]
    public void Constructor_RemovesExpiredLogsAtLoggerStartup()
    {
        var logDirectory = CreateTemporaryDirectory();
        var oldTimestamp = DateTimeOffset.Now.AddDays(-60);
        var expiredLog = CreateLogFile(logDirectory, oldTimestamp, oldTimestamp);

        try
        {
            _ = new FileAppLogger(logDirectory);

            Assert.False(File.Exists(expiredLog));
        }
        finally
        {
            Directory.Delete(logDirectory, recursive: true);
        }
    }

    [Fact]
    public void CompositeLogger_WhenOneLoggerFails_ContinuesWithRemainingLogger()
    {
        var fakeLogger = new FakeAppLogger();
        var logger = new CompositeAppLogger(
            new ThrowingAppLogger(),
            fakeLogger);
        var exception = new InvalidOperationException("failure");

        logger.LogInformation("information");
        logger.LogWarning("warning");
        logger.LogError("error", exception);

        Assert.Collection(
            fakeLogger.Entries,
            entry =>
            {
                Assert.Equal("Information", entry.Level);
                Assert.Equal("information", entry.Message);
            },
            entry =>
            {
                Assert.Equal("Warning", entry.Level);
                Assert.Equal("warning", entry.Message);
            },
            entry =>
            {
                Assert.Equal("Error", entry.Level);
                Assert.Equal("error", entry.Message);
                Assert.Same(exception, entry.Exception);
            });
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"ProductManagerApp-Logs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string CreateLogFile(
        string directory,
        DateTimeOffset fileDate,
        DateTimeOffset lastWrite)
    {
        var fileName = $"ProductManagerApp-" +
            $"{fileDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.log";
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, "log");
        File.SetLastWriteTimeUtc(path, lastWrite.UtcDateTime);
        return path;
    }

    private sealed class ThrowingAppLogger : IAppLogger
    {
        public void LogInformation(string message) => throw new IOException("failure");

        public void LogWarning(string message) => throw new IOException("failure");

        public void LogError(string message, Exception exception) =>
            throw new IOException("failure");
    }
}
