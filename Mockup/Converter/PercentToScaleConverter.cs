using System.Globalization;
using System.Windows.Data;

namespace Mockup.Converter;

/// <summary>
/// Converts percentage value (e.g. 100) into scale (e.g. 1.0).
/// </summary>
public class PercentToScaleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int percent ? percent / 100.0 : 1.0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double scale ? (int)(scale * 100) : 100;
}
