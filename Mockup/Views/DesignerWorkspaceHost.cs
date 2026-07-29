using System.Windows;
using System.Windows.Controls;

namespace Mockup.Views;

/// <summary>
/// Hosts a designer and its toolbox, reserving dock space only while both fit comfortably.
/// The persisted pin preference is kept when the toolbox temporarily overlays narrow workspaces.
/// </summary>
public sealed class DesignerWorkspaceHost : ContentControl
{
    // Navigator, splitter, minimum designer surface, and the 60-DIP toolbox rail.
    private const double DefaultMinimumUnpinnedWorkspaceWidth = 1130d;
    private const double DefaultToolboxWidth = 570d;

    private static readonly DependencyPropertyKey EffectiveToolboxReservedWidthPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(EffectiveToolboxReservedWidth),
            typeof(double),
            typeof(DesignerWorkspaceHost),
            new FrameworkPropertyMetadata(0d));

    public static readonly DependencyProperty EffectiveToolboxReservedWidthProperty =
        EffectiveToolboxReservedWidthPropertyKey.DependencyProperty;

    public static readonly DependencyProperty IsToolboxPinnedProperty =
        DependencyProperty.Register(
            nameof(IsToolboxPinned),
            typeof(bool),
            typeof(DesignerWorkspaceHost),
            new FrameworkPropertyMetadata(false, OnAdaptiveLayoutPropertyChanged));

    public static readonly DependencyProperty ToolboxWidthProperty =
        DependencyProperty.Register(
            nameof(ToolboxWidth),
            typeof(double),
            typeof(DesignerWorkspaceHost),
            new FrameworkPropertyMetadata(DefaultToolboxWidth, OnAdaptiveLayoutPropertyChanged));

    public static readonly DependencyProperty MinimumUnpinnedWorkspaceWidthProperty =
        DependencyProperty.Register(
            nameof(MinimumUnpinnedWorkspaceWidth),
            typeof(double),
            typeof(DesignerWorkspaceHost),
            new FrameworkPropertyMetadata(DefaultMinimumUnpinnedWorkspaceWidth, OnAdaptiveLayoutPropertyChanged));

    public DesignerWorkspaceHost()
    {
        SizeChanged += OnHostSizeChanged;
    }

    public double EffectiveToolboxReservedWidth =>
        (double)GetValue(EffectiveToolboxReservedWidthProperty);

    public bool IsToolboxPinned
    {
        get => (bool)GetValue(IsToolboxPinnedProperty);
        set => SetValue(IsToolboxPinnedProperty, value);
    }

    public double ToolboxWidth
    {
        get => (double)GetValue(ToolboxWidthProperty);
        set => SetValue(ToolboxWidthProperty, value);
    }

    public double MinimumUnpinnedWorkspaceWidth
    {
        get => (double)GetValue(MinimumUnpinnedWorkspaceWidthProperty);
        set => SetValue(MinimumUnpinnedWorkspaceWidthProperty, value);
    }

    private static void OnAdaptiveLayoutPropertyChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is DesignerWorkspaceHost host)
            host.UpdateAdaptiveToolboxReservation();
    }

    private void OnHostSizeChanged(object sender, SizeChangedEventArgs eventArgs) =>
        UpdateAdaptiveToolboxReservation();

    private void UpdateAdaptiveToolboxReservation()
    {
        double toolboxWidth = NormalizeDimension(ToolboxWidth, DefaultToolboxWidth);
        double minimumUnpinnedWorkspaceWidth = NormalizeDimension(
            MinimumUnpinnedWorkspaceWidth,
            DefaultMinimumUnpinnedWorkspaceWidth);

        bool hasRoomForPinnedToolbox =
            IsToolboxPinned
            && ActualWidth >= minimumUnpinnedWorkspaceWidth + toolboxWidth;

        SetValue(
            EffectiveToolboxReservedWidthPropertyKey,
            hasRoomForPinnedToolbox ? toolboxWidth : 0d);
    }

    private static double NormalizeDimension(double value, double fallback) =>
        double.IsFinite(value) && value >= 0d ? value : fallback;
}
