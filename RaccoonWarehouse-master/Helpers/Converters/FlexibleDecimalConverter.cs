using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RaccoonWarehouse.Helpers.Converters
{
    public sealed class FlexibleDecimalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is decimal decimalValue ? decimalValue.ToString("0.00000", culture) : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                return DependencyProperty.UnsetValue;

            text = text.Replace(',', '.');
            return decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var result)
                ? result
                : DependencyProperty.UnsetValue;
        }
    }
}
