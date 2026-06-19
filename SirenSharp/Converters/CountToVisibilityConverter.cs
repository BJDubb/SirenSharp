using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SirenSharp.Converters
{
    // Visible when the integer value is greater than zero, otherwise Collapsed.
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var count = value is int i ? i : 0;
            var hasItems = count > 0;
            if (string.Equals(parameter as string, "invert", StringComparison.OrdinalIgnoreCase))
                hasItems = !hasItems;
            return hasItems ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
