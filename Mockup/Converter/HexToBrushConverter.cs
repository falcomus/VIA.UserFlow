using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Mockup.Converter;

public sealed class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var hex = value as string;
        if (string.IsNullOrWhiteSpace(hex))
            return Brushes.Transparent;

        try
        {
            var obj = ColorConverter.ConvertFromString(hex.Trim());
            if (obj is Color c)
                return new SolidColorBrush(c);
        }
        catch { }

        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
