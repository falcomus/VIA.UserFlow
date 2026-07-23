using System.Globalization;
using System.Windows.Data;

namespace Mockup.Converter;

public class ZoomPercentToSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double percent)
        {
            // Basisgröße (können Sie anpassen)
            double baseSize = 100.0;
            return baseSize * (percent / 100.0);
        }
        return 100.0; // Default-Größe
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}