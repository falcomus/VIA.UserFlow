using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mockup.Converter;

public sealed class IsNotFirstToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => (value is int i && i > 0) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object v, Type t, object p, CultureInfo c)
        => throw new NotSupportedException();
}

