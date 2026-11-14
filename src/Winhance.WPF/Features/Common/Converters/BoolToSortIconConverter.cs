using System;
using System.Globalization;
using System.Windows.Data;
using iconPacks = MahApps.Metro.IconPacks;

namespace Winhance.WPF.Features.Common.Converters
{
    /// <summary>
    /// Converts boolean value to sort icon (ascending/descending).
    /// </summary>
    public class BoolToSortIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAscending)
            {
                return isAscending ? iconPacks.PackIconMaterialKind.SortAscending : iconPacks.PackIconMaterialKind.SortDescending;
            }
            return iconPacks.PackIconMaterialKind.SortAscending;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
