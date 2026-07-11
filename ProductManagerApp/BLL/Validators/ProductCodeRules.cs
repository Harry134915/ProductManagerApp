using System.Text.RegularExpressions;

namespace ProductManagerApp.BLL.Validators
{
    public static partial class ProductCodeRules
    {
        public const string InvalidFormatMessage =
            "商品编码只能包含英文字母、数字、连字符（-）和下划线（_）。";

        public static bool IsValid(string? code)
        {
            return !string.IsNullOrWhiteSpace(code) && ProductCodeRegex().IsMatch(code);
        }

        [GeneratedRegex("^[A-Za-z0-9_-]+$")]
        private static partial Regex ProductCodeRegex();
    }
}
