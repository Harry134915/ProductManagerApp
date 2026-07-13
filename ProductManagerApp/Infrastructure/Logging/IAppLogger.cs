namespace ProductManagerApp.Infrastructure.Logging
{
    /// <summary>
    /// 为 ViewModel 和应用生命周期提供与具体输出目标无关的日志入口。
    /// </summary>
    public interface IAppLogger
    {
        void LogInformation(string message);

        void LogWarning(string message);

        void LogError(string message, Exception exception);
    }
}
