using System;
using System.Globalization;
using System.Windows.Data;

namespace RaccoonWarehouse.Helpers.Converters
{
    public sealed class AbsoluteQuantityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal quantity)
                return Math.Abs(quantity).ToString("0.00000", CultureInfo.InvariantCulture);

            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (decimal.TryParse(value?.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity))
                return Math.Abs(quantity);

            return Binding.DoNothing;
        }
    }
}
