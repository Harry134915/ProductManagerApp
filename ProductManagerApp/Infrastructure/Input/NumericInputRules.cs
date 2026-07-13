namespace ProductManagerApp.Infrastructure.Input
{
    /// <summary>
    /// 判断价格和库存的键盘或粘贴候选文本是否仍可形成合法数值。
    /// </summary>
    public static class NumericInputRules
    {
        public static bool IsValidPriceCandidate(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }

            var decimalPointCount = 0;
            foreach (var character in value)
            {
                if (character == '.')
                {
                    decimalPointCount++;
                    if (decimalPointCount > 1)
                    {
                        return false;
                    }

                    continue;
                }

                if (character is < '0' or > '9')
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsValidStockCandidate(string? value)
        {
            return string.IsNullOrEmpty(value)
                || value.All(character => character is >= '0' and <= '9');
        }

        public static string ApplyInput(
            string currentText,
            int selectionStart,
            int selectionLength,
            string input)
        {
            return currentText
                .Remove(selectionStart, selectionLength)
                .Insert(selectionStart, input);
        }
    }
}
