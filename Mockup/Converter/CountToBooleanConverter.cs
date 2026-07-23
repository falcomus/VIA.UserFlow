using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace Mockup.Converter;

public class CountToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 1;
        }

        if (value is ICollection collection)
        {
            return collection.Count > 1;
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}