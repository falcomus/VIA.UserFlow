using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Mockup.Converter;

public sealed class ColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color c)
            return new SolidColorBrush(c);

        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush b)
            return b.Color;

        if (value is Color c)
            return c;

        return Colors.Transparent;
    }
}