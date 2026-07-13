using System;

namespace ProductManagerApp.Infrastructure.Exceptions
{
    /// <summary>
    /// 在不暴露 SQL 和底层驱动细节的前提下表示数据访问失败。
    /// </summary>
    public class DataAccessException : BusinessException
    {
        public DataAccessException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
