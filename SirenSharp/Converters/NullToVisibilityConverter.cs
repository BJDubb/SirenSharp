using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SirenSharp.Converters
{
    // Visible when the value is non-null, Collapsed when null.
    // Pass ConverterParameter=invert to flip (Visible when null).
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var hasValue = value != null;
            if (string.Equals(parameter as string, "invert", StringComparison.OrdinalIgnoreCase))
                hasValue = !hasValue;
            return hasValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
