using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mockup.Converter;

public class AndBooleanToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2)
            return Visibility.Collapsed;

        // Prüfe ob beide Werte bool sind und true
        bool allTrue = true;
        foreach (var value in values)
        {
            if (value is bool boolValue)
            {
                if (!boolValue)
                {
                    allTrue = false;
                    break;
                }
            }
            else
            {
                // Falls ein Wert kein bool ist, geben wir Collapsed zurück
                return Visibility.Collapsed;
            }
        }

        return allTrue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ConvertBack is not supported for AndBooleanToVisibilityConverter");
    }
}