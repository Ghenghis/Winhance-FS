using System;
using System.Globalization;
using System.Windows.Data;

namespace Winhance.WPF.Features.FileManager.Converters
{
    /// <summary>
    /// Converts progress percentage to width.
    /// </summary>
    public class ProgressToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is double progress && values[1] is double totalWidth)
            {
                return (progress / 100.0) * totalWidth;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            var results = new object[targetTypes.Length];
            Array.Fill(results, Binding.DoNothing);
            return results;
        }
    }

}
