using System.Text.RegularExpressions;

namespace ProductManagerApp.BLL.Validators
{
    /// <summary>
    /// 定义商品编码允许的字符集合，避免界面与业务层规则漂移。
    /// </summary>
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
