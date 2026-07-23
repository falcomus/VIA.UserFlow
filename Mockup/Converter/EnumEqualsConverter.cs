// ======================================================================================
// FILE: Mockup.UIControls/Converters/EnumEqualsConverter.cs
// ======================================================================================

using System.Globalization;
using System.Windows.Data;

namespace Mockup.Converter;

public sealed class EnumEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        // parameter kommt als string (z.B. "Left")
        var paramText = parameter.ToString();

        // Enum-Wert -> string vergleichen (case-insensitive)
        return string.Equals(value.ToString(), paramText, StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // nur bei Checked true setzen
        if (value is not bool b || !b)
            return Binding.DoNothing;

        if (parameter == null || targetType == null)
            return Binding.DoNothing;

        var paramText = parameter.ToString();

        // targetType ist z.B. ScreenPopupPosition
        if (targetType.IsEnum && !string.IsNullOrWhiteSpace(paramText))
        {
            try
            {
                return Enum.Parse(targetType, paramText!, ignoreCase: true);
            }
            catch
            {
                return Binding.DoNothing;
            }
        }

        return Binding.DoNothing;
    }
}
