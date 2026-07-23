using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mockup.Converter;

public sealed class GreaterThanToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2)
            return Visibility.Collapsed;

        if (values[0] == null || values[1] == null)
            return Visibility.Collapsed;

        if (!double.TryParse(values[0].ToString(), out var left))
            return Visibility.Collapsed;

        if (!double.TryParse(values[1].ToString(), out var right))
            return Visibility.Collapsed;

        return left > right
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}