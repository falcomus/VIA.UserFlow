using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Mockup.Converter;

public class BoolToBrushConverter : IValueConverter
{
    public Brush SelectedBrush { get; set; } = Brushes.DodgerBlue;
    public Brush NormalBrush { get; set; } = Brushes.Transparent;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? SelectedBrush : NormalBrush;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
