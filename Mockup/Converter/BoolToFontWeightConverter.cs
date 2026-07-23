using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mockup.Converter;

public class BoolToFontWeightConverter : IValueConverter
{
    public static BoolToFontWeightConverter Instance { get; } = new BoolToFontWeightConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value is bool boolValue && boolValue) ? FontWeights.Medium : FontWeights.Normal;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
