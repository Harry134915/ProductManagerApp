using ProductManagerApp.Infrastructure.Logging;
using System.Collections.Concurrent;

namespace ProductManagerApp.Tests.Fakes;

internal sealed record FakeLogEntry(
    string Level,
    string Message,
    Exception? Exception = null);

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
