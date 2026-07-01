using System;

namespace ProductManagerApp.BLL.Exceptions
{
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
