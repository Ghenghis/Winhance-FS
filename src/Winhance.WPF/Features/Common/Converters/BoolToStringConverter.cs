using System;
using System.Globalization;
using System.Windows.Data;

namespace Winhance.WPF.Features.Common.Converters
{
    /// <summary>
    /// Converts a boolean to one of two strings based on ConverterParameter.
    /// Parameter format: "TrueValue|FalseValue" (e.g., "Resume|Pause").
    /// </summary>
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not bool boolValue)
            {
                return string.Empty;
            }

            if (parameter is not string paramString || !paramString.Contains('|'))
            {
                return boolValue.ToString();
            }

            var parts = paramString.Split('|');
            if (parts.Length != 2)
            {
                return boolValue.ToString();
            }

            return boolValue ? parts[0] : parts[1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
