// z.B. in Mockup.Designer/Converters/PixelGridLengthConverter.cs
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mockup.Converter;

public sealed class PixelGridLengthConverter : IValueConverter
{
    // float/double -> GridLength(Pixel)
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var d = value is float f ? (double)f : System.Convert.ToDouble(value ?? 0);
        if (double.IsNaN(d) || d < 0) d = 0;
        return new GridLength(d, GridUnitType.Pixel);
    }

    // GridLength -> float (Pixel). Auto => 0f; Star => nicht vorgesehen, fallback auf Actual Pixel.
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is GridLength gl)
        {
            if (gl.IsAuto) return 0f;
            return (float)gl.Value; // bei Pixel
        }
        return 0f;
    }
}
