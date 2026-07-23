using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Controls;

namespace Mockup.UIControls;

[ObservableObject]
public partial class XZoomSlider : UserControl
{
    public XZoomSlider()
    {
        InitializeComponent();
        BorderThickness = new Thickness(1, 0, 1, 0);
    }

    #region ZOOM

    #region Zooming

    /// <summary>
    /// Gets or sets the zoom factor in percent (e.g. 100 = 100%).
    /// </summary>
    public int ZoomPercent
    {
        get => (int)GetValue(ZoomPercentProperty);
        set => SetValue(ZoomPercentProperty, value);
    }

    public static readonly DependencyProperty ZoomPercentProperty =
        DependencyProperty.Register(
            nameof(ZoomPercent),
            typeof(int),
            typeof(XZoomSlider),
            new PropertyMetadata(100, OnZoomPercentChanged));

    private static void OnZoomPercentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is XZoomSlider slider)
        {
            // Convert percent to scale factor
            slider.Zoom = (double)(int)e.NewValue / 100.0;
        }
    }

    /// <summary>
    /// Gets or sets the zoom factor as scale (e.g. 1.0 = 100%).
    /// </summary>
    private double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    private static readonly DependencyProperty ZoomProperty =
        DependencyProperty.Register(
            nameof(Zoom),
            typeof(double),
            typeof(XZoomSlider),
            new PropertyMetadata(1.0));



    /// <summary>
    /// Gets or sets the min zoom percent (e.g. 20 = 20%).
    /// </summary>
    public int MinZoomPercent
    {
        get => (int)GetValue(MinZoomPercentProperty);
        set => SetValue(MinZoomPercentProperty, value);
    }

    public static readonly DependencyProperty MinZoomPercentProperty =
        DependencyProperty.Register(
            nameof(MinZoomPercent),
            typeof(int),
            typeof(XZoomSlider),
            new PropertyMetadata(20));

    /// <summary>
    /// Gets or sets the max zoom percent (e.g. 150 = 150%).
    /// </summary>
    public int MaxZoomPercent
    {
        get => (int)GetValue(MaxZoomPercentProperty);
        set => SetValue(MaxZoomPercentProperty, value);
    }

    public static readonly DependencyProperty MaxZoomPercentProperty =
        DependencyProperty.Register(
            nameof(MaxZoomPercent),
            typeof(int),
            typeof(XZoomSlider),
            new PropertyMetadata(200));

    #endregion Zooming


    [RelayCommand]
    void ResetZoom() => ZoomPercent = MaxZoomPercent > 100 ? 100 : MaxZoomPercent;

    #endregion

    public Visibility ResetButtonVisibility
    {
        get => (Visibility)GetValue(ResetButtonVisibilityProperty);
        set => SetValue(ResetButtonVisibilityProperty, value);
    }

    public static readonly DependencyProperty ResetButtonVisibilityProperty =
        DependencyProperty.Register(
            nameof(ResetButtonVisibility),
            typeof(Visibility),
            typeof(XZoomSlider));

    public double TickFrequency
    {
        get => (double)GetValue(TickFrequencyProperty);
        set => SetValue(TickFrequencyProperty, value);
    }

    public static readonly DependencyProperty TickFrequencyProperty =
        DependencyProperty.Register(
            nameof(TickFrequency),
            typeof(double),
            typeof(XZoomSlider),
            new PropertyMetadata(defaultValue: 5d));

}
