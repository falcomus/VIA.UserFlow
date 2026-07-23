using System.Globalization;
using System.Windows.Data;

namespace Mockup.Converter;

public sealed class EnumBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        return value.ToString() == parameter.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter != null)
            return Enum.Parse(targetType, parameter.ToString()!);

        return Binding.DoNothing;
    }
}