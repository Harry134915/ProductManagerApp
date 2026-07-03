using System;

namespace ProductManagerApp.Infrastructure.Exceptions
{
    public class DataAccessException : BusinessException
    {
        public DataAccessException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
