using System.Globalization;
using System.Windows.Data;

namespace Mockup.Converter;

public class ZoomPercentToScaleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Prozentwert (double) in Skalierungsfaktor umrechnen
        if (value is double percent)
            return percent / 100.0;

        return 1.0; // Default-Wert
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Rückkonvertierung (falls nötig)
        if (value is double scale)
            return scale * 100.0;

        return 100.0; // Default-Wert
    }
}
