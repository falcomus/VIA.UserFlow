using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mockup.Converter;

/// <summary>
/// Converts a boolean selection state into a border thickness.
/// </summary>
public class BoolToThicknessConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isSelected = value is bool b && b;
        return isSelected ? new Thickness(2) : new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
