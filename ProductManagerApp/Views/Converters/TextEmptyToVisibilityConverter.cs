using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ProductManagerApp.Views.Converters
{
    public class TextEmptyToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return Visibility.Collapsed;

            string? text = values[0] as string;
            bool isFocused = values[1] is bool b && b;

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
