using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mockup.Converter;

public class FooterThumbMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is float floatVal)
            return new Thickness(0, 0, 0, floatVal);

        return new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}