using System;
using System.Globalization;
using System.Windows.Data;

namespace Winhance.WPF.Features.Common.Converters
{
    /// <summary>
    /// Converts enum values to boolean for RadioButton binding.
    /// Usage: IsChecked="{Binding MyEnum, Converter={StaticResource EnumBooleanConverter}, ConverterParameter=EnumValue}".
    /// </summary>
    public class EnumBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return false;
            }

            string parameterString = parameter.ToString()!;

            if (Enum.IsDefined(value.GetType(), value))
            {
                return value.ToString()!.Equals(parameterString, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return Binding.DoNothing;
            }

            if ((bool)value)
            {
                return Enum.Parse(targetType, parameter.ToString()!);
            }

            return Binding.DoNothing;
        }
    }
}
