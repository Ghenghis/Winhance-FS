using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Winhance.WPF.Features.Common.Converters
{
    /// <summary>
    /// Converts zero/null values to Visibility.Collapsed, non-zero to Visible (reversed with ConverterParameter="Invert").
    /// </summary>
    public class ZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isZero = value switch
            {
                null => true,
                int i => i == 0,
                long l => l == 0,
                double d => d == 0,
                _ => false,
            };

            // ConverterParameter="Invert" shows the element only when the value is zero (empty-state placeholders).
            var invert = string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase);
            return isZero ^ invert ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
