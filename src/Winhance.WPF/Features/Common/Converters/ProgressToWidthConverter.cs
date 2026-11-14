using System;
using System.Globalization;
using System.Windows.Data;

namespace Winhance.WPF.Features.Common.Converters
{
    /// <summary>
    /// Converts progress value and maximum to actual width for progress bar.
    /// Provides smooth animation by calculating percentage-based width.
    /// </summary>
    public class ProgressToWidthConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts progress value, maximum, and total width to actual progress width.
        /// </summary>
        /// <param name="values">Values: [0] = Value, [1] = Maximum, [2] = ActualWidth.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The calculated width for the progress indicator.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3)
            {
                return 0.0;
            }

            if (values[0] is not double value ||
                values[1] is not double maximum ||
                values[2] is not double actualWidth)
            {
                return 0.0;
            }

            if (maximum <= 0)
            {
                return 0.0;
            }

            // Calculate percentage and return proportional width
            var percentage = value / maximum;
            var width = actualWidth * percentage;

            // Account for margins (2px on each side)
            return Math.Max(0, width - 2);
        }

        /// <summary>
        /// Converts back is not supported.
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            var results = new object[targetTypes.Length];
            Array.Fill(results, Binding.DoNothing);
            return results;
        }
    }
}
