using ProductManagerApp.Entity;
using ProductManagerApp.Infrastructure.Exceptions;

namespace ProductManagerApp.BLL.Validators
{
    public class ProductValidator
    {
        public void Validate(Product product)
        {
            if (product == null)
                throw new ProductValidationException("商品不可为空!");

            ThrowIfInvalid(ProductValidationRules.GetNameError(product.Name));
            ThrowIfInvalid(ProductValidationRules.GetCodeError(product.Code));
            ThrowIfInvalid(ProductValidationRules.GetPriceError(product.Price));
            ThrowIfInvalid(ProductValidationRules.GetStockError(product.Stock));
            ThrowIfInvalid(ProductValidationRules.GetDescriptionError(product.Description));
        }

        public void ValidateId(int id)
        {
            if (id <= 0)
                throw new ProductValidationException("商品id不合法！");
        }

        public void ValidatePrice(decimal price)
        {
            ThrowIfInvalid(ProductValidationRules.GetPriceError(price));
        }

        public void ValidateCodeUnchanged(string currentCode, string updateCode)
        {
            if (!string.Equals(currentCode, updateCode, StringComparison.Ordinal))
                throw new ProductValidationException("商品编码不可修改！");
        }

        private static void ThrowIfInvalid(string? message)
        {
            if (message != null)
            {
                throw new ProductValidationException(message);
            }
        }
    }
}
