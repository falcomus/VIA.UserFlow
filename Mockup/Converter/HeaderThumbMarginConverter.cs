using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mockup.Converter;

public class HeaderThumbMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is float floatVal)
            return new Thickness(0, floatVal, 0, 0);

        return new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
