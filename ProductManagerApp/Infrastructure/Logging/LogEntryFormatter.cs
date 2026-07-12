using System.Globalization;

namespace ProductManagerApp.Infrastructure.Logging
{
    internal static class LogEntryFormatter
    {
        public static string Format(
            DateTimeOffset timestamp,
            string level,
            string message,
            Exception? exception = null)
        {
            var entry = string.Create(
                CultureInfo.InvariantCulture,
                $"[{timestamp:O}] [{level}] {message}");

            return exception == null
                ? entry
                : $"{entry}{Environment.NewLine}{exception}";
        }
    }
}
