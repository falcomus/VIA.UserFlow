using System.Windows;
using System.Windows.Media;

namespace Mockup.Helper;

public static class ExpanderHelper
{
    public static readonly DependencyProperty ContentBackgroundProperty =
        DependencyProperty.RegisterAttached(
            "ContentBackground",
            typeof(Brush),
            typeof(ExpanderHelper),
            new FrameworkPropertyMetadata(null));

    public static Brush GetContentBackground(DependencyObject obj)
        => (Brush)obj.GetValue(ContentBackgroundProperty);

    public static void SetContentBackground(DependencyObject obj, Brush value)
        => obj.SetValue(ContentBackgroundProperty, value);
}