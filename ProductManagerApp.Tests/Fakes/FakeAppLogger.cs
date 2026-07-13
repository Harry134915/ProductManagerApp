using ProductManagerApp.Infrastructure.Logging;
using System.Collections.Concurrent;

namespace ProductManagerApp.Tests.Fakes;

/// <summary>
/// 表示测试捕获的一条日志，保留级别、消息和可选异常。
/// </summary>
internal sealed record FakeLogEntry(
    string Level,
    string Message,
    Exception? Exception = null);

/// <summary>
/// 使用线程安全队列捕获日志，供异步 ViewModel 测试断言。
/// </summary>
internal sealed class FakeAppLogger : IAppLogger
{
    public ConcurrentQueue<FakeLogEntry> Entries { get; } = new();

    public void LogInformation(string message)
    {
        Entries.Enqueue(new FakeLogEntry("Information", message));
    }

    public void LogWarning(string message)
    {
        Entries.Enqueue(new FakeLogEntry("Warning", message));
    }

    public void LogError(string message, Exception exception)
    {
        Entries.Enqueue(new FakeLogEntry("Error", message, exception));
    }
}
