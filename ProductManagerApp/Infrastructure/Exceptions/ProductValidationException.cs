namespace ProductManagerApp.Infrastructure.Exceptions
{
    /// <summary>
    /// 表示商品输入或业务状态未满足领域规则。
    /// </summary>
    public class ProductValidationException : BusinessException
    {
        public ProductValidationException(string message)
            : base(message)
        {
        }
    }
}
