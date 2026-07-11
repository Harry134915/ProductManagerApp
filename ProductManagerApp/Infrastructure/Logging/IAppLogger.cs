namespace ProductManagerApp.Infrastructure.Logging
{
    public interface IAppLogger
    {
        void LogInformation(string message);

        void LogWarning(string message);

        void LogError(string message, Exception exception);
    }
}
