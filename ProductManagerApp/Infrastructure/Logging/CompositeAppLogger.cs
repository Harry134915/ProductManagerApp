using System.Diagnostics;

namespace ProductManagerApp.Infrastructure.Logging
{
    public sealed class CompositeAppLogger : IAppLogger
    {
        private readonly IAppLogger[] _loggers;

        public CompositeAppLogger(params IAppLogger[] loggers)
        {
            ArgumentNullException.ThrowIfNull(loggers);

            if (loggers.Length == 0)
            {
                throw new ArgumentException(
                    "至少需要提供一个日志实现。",
                    nameof(loggers));
            }

            if (loggers.Any(logger => logger == null))
            {
                throw new ArgumentException(
                    "日志实现集合中不能包含 null。",
                    nameof(loggers));
            }

            _loggers = loggers.ToArray();
        }

        public void LogInformation(string message)
        {
            Write(logger => logger.LogInformation(message));
        }

        public void LogWarning(string message)
        {
            Write(logger => logger.LogWarning(message));
        }

        public void LogError(string message, Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            Write(logger => logger.LogError(message, exception));
        }

        private void Write(Action<IAppLogger> write)
        {
            foreach (var logger in _loggers)
            {
                try
                {
                    write(logger);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(
                        LogEntryFormatter.Format(
                            DateTimeOffset.Now,
                            "ERROR",
                            $"日志实现 {logger.GetType().Name} 执行失败。",
                            exception));
                }
            }
        }
    }
}
