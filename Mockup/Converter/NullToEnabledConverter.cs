using System.Globalization;
using System.Windows.Data;

namespace Mockup.Converter;

/// <summary>
/// Konvertiert einen null-Wert zu false (disabled), nicht-null zu true (enabled).
/// </summary>
public class NullToEnabledConverter : IValueConverter
{
    public bool Invert { get; set; } = false;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isEnabled = value != null;
        return Invert ? !isEnabled : isEnabled;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}