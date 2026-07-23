using System.Globalization;
using System.Windows.Data;

namespace Mockup.Converter;

/// <summary>
/// Konvertiert einen null-Wert zu false (disabled), nicht-null zu true (enabled).
/// </summary>
public class NullToDisabledConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value != null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}