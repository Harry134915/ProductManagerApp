namespace ProductManagerApp.BLL.Exceptions
{
    public class DataAccessException : BusinessException
    {
        public DataAccessException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
