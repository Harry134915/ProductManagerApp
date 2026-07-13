namespace ProductManagerApp.BLL.Validators
{
    /// <summary>
    /// 提供表单层和业务层共用的无状态商品校验规则及提示文案。
    /// </summary>
    public static class ProductValidationRules
    {
        public const string CodeRequiredMessage = "请输入商品编码。";
        public const string NameRequiredMessage = "请输入商品名称。";
        public const string PricePositiveMessage = "价格必须大于 0。";
        public const string StockNonNegativeMessage = "库存不能小于 0。";
        public const string DescriptionRequiredMessage =
            "请输入商品描述，且不能只包含空格。";

        public static string? GetCodeError(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return CodeRequiredMessage;
            }

            return ProductCodeRules.IsValid(code)
                ? null
                : ProductCodeRules.InvalidFormatMessage;
        }

        public static string? GetNameError(string? name)
        {
            return string.IsNullOrWhiteSpace(name)
                ? NameRequiredMessage
                : null;
        }

        public static string? GetPriceError(decimal price)
        {
            return price <= 0 ? PricePositiveMessage : null;
        }

        public static string? GetStockError(int stock)
        {
            return stock < 0 ? StockNonNegativeMessage : null;
        }

        public static string? GetDescriptionError(string? description)
        {
            return string.IsNullOrWhiteSpace(description)
                ? DescriptionRequiredMessage
                : null;
        }
    }
}
