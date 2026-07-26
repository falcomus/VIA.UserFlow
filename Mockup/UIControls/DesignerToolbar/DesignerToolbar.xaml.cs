using System.Windows;
using System.Windows.Controls;

namespace Mockup.UIControls;

public partial class DesignerToolbar : UserControl
{
    public static readonly DependencyProperty ZoomPercentProperty =
        DependencyProperty.Register(nameof(ZoomPercent), typeof(int), typeof(DesignerToolbar), new FrameworkPropertyMetadata(100, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty MinZoomPercentProperty =
        DependencyProperty.Register(nameof(MinZoomPercent), typeof(int), typeof(DesignerToolbar), new PropertyMetadata(25));

    public static readonly DependencyProperty MaxZoomPercentProperty =
        DependencyProperty.Register(nameof(MaxZoomPercent), typeof(int), typeof(DesignerToolbar), new PropertyMetadata(200));

    public int ZoomPercent
    {
        get => (int)GetValue(ZoomPercentProperty);
        set => SetValue(ZoomPercentProperty, value);
    }

    public int MinZoomPercent
    {
        get => (int)GetValue(MinZoomPercentProperty);
        set => SetValue(MinZoomPercentProperty, value);
    }

    public int MaxZoomPercent
    {
        get => (int)GetValue(MaxZoomPercentProperty);
        set => SetValue(MaxZoomPercentProperty, value);
    }

    public DesignerToolbar()
    {
        InitializeComponent();
    }
}
