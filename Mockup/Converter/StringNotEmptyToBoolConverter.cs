using System.Globalization;
using System.Windows.Data;

namespace Mockup.Converter;

[ValueConversion(typeof(string), typeof(bool))]
public class StringNotEmptyToBoolConverter : IValueConverter
{
    public bool Invert { get; set; }
    public bool TreatWhitespaceAsEmpty { get; set; } = true;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isEmpty = value switch
        {
            null => true,
            string s when TreatWhitespaceAsEmpty => string.IsNullOrWhiteSpace(s),
            string s => string.IsNullOrEmpty(s),
            _ => true
        };

        bool result = !isEmpty;

        return Invert ? !result : result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
