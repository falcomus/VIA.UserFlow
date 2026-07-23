using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace Mockup.Converter;

public class PathToFileNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string path)
        {
            try
            {
                return Path.GetFileNameWithoutExtension(path);
            }
            catch
            {
                return value;
            }
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}