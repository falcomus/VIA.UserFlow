using System.Globalization;
using System.Windows.Data;

namespace Mockup.Converter;

public class ScaleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double h && parameter is string p && double.TryParse(p, out var scale))
            return h * scale;

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
