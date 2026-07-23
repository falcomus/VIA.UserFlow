using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mockup.Converter;

public class NegWidthToThicknessConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double width && parameter is string paramString)
        {
            if (double.TryParse(paramString, out double offset))
            {
                return new Thickness(-(width + offset), 0, 0, 0);
            }
        }

        if (value is double widthSimple)
        {
            return new Thickness(-widthSimple, 0, 0, 0);
        }

        return new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}