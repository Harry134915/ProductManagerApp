using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ProductManagerApp.Views
{
    public class TextEmptyToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return Visibility.Collapsed;

            string? text = values[0] as string;
            bool isFocused = values[1] is bool b && b;
            //bool isFocused = (bool)values[1];

            // 如果输入框已有文字 → 隐藏提示
            if (!string.IsNullOrEmpty(text))
                return Visibility.Collapsed;

            // 中文输入法输入中（有焦点，但文字未上屏）→ 显示提示
            if (isFocused)
                return Visibility.Visible;

            // 其他情况（完全空白、未输入） → 显示提示
            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
/*
 * 单值转换器只能判断 Text 是否为空，不知道 TextBox 是否聚焦。
而正确的 Placeholder 行为需要 Text + Focus 两个条件共同决定。
换成 MultiValueConverter 后，逻辑才正确，提示才如预期消失。

Placeholder 不只是取决于是否为空，还取决于是否获得焦点，
而原 Converter 不知道焦点，所以提示消失不正常。改成 MultiBinding 后才真正合理。

解决问题:
数组越界或 null 未处理：values[0] 和 values[1] 使用前应检查 values 是否为 null，以及长度是否足够。
拆箱风险：直接 (bool)values[1] 在值为 null 或非 bool 时会抛异常。
*/

