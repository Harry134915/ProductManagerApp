namespace ProductManagerApp.BLL.Exceptions
{
    public class ProductValidationException : BusinessException
    {
        public ProductValidationException(string message)
            : base(message)
        {
        }
    }
    //给 ViewModel / 日志 / 上层逻辑用的
}
