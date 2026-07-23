using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Mockup.ColorSystem;

public sealed class ColorToAutoTextBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Color c)
            return Brushes.Black;

        // relative luminance (einfach)
        var luminance = 0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B;
        return luminance > 140 ? Brushes.Black : Brushes.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}