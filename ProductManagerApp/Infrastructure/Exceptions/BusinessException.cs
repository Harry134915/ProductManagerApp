using System;

namespace ProductManagerApp.Infrastructure.Exceptions
{
    /// <summary>
    /// 表示可由应用边界识别并转换为用户提示的业务相关异常。
    /// </summary>
    public abstract class BusinessException : Exception
    {
        protected BusinessException(string message)
            : base(message)
        {
        }

        protected BusinessException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
