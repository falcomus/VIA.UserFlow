using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mockup.Converter;

public class FirstItemMarginConverter : IValueConverter
{
    public Thickness DefaultMargin { get; set; }
    public Thickness FirstItemMargin { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int index && index == 0)
            return FirstItemMargin;

        return DefaultMargin;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}