using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mockup.Converter;

[ValueConversion(typeof(string), typeof(Visibility))]
public class StringToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }  // Property-based inversion option
    public bool TreatWhitespaceAsEmpty { get; set; } = true;  // New option

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Handle parameter (more robust parsing)
        bool localInvert = Invert;

        if (parameter != null)
        {
            if (parameter is string stringParam)
            {
                localInvert |= stringParam.Equals("Invert", StringComparison.OrdinalIgnoreCase);
            }
            else if (parameter is bool boolParam)
            {
                localInvert |= boolParam;
            }
        }

        // Check string value (with whitespace handling option)
        bool isStringEmpty = value switch
        {
            null => true,
            string s when TreatWhitespaceAsEmpty => string.IsNullOrWhiteSpace(s),
            string s => string.IsNullOrEmpty(s),
            _ => true
        };

        bool shouldBeVisible = localInvert ? isStringEmpty : !isStringEmpty;

        return shouldBeVisible ? Visibility.Visible : Visibility.Collapsed;
    }


    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("Two-way conversion not supported");
    }

}
