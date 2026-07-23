using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mockup.Converter;

/// <summary>
/// Converts a null value to Visibility.Visible, non-null to Visibility.Collapsed (always inverted).
/// </summary>
public class NullToInversVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isVisible = value == null; // Immer invers: null = sichtbar, nicht-null = ausgeblendet
        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}