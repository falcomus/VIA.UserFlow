using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Mockup.Resources;
using VIA.WPF.Localization;

namespace Mockup.Views;

/// <summary>
/// Interaction logic for ToolboxView.xaml
/// </summary>
public partial class ToolboxView : UserControl
{
    public static readonly DependencyProperty IsFlyoutOpenProperty = DependencyProperty.Register(
        nameof(IsFlyoutOpen),
        typeof(bool),
        typeof(ToolboxView),
        new PropertyMetadata(false));

    public static readonly DependencyProperty IsFlyoutPinnedProperty = DependencyProperty.Register(
        nameof(IsFlyoutPinned),
        typeof(bool),
        typeof(ToolboxView),
        new PropertyMetadata(false, OnIsFlyoutPinnedChanged));

    public static readonly DependencyProperty DockWidthProperty = DependencyProperty.Register(
        nameof(DockWidth),
        typeof(double),
        typeof(ToolboxView),
        new PropertyMetadata(570d, OnDockWidthChanged));

    private readonly DispatcherTimer flyoutCloseTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(260)
    };

    public bool IsFlyoutOpen
    {
        get => (bool)GetValue(IsFlyoutOpenProperty);
        private set => SetValue(IsFlyoutOpenProperty, value);
    }

    public bool IsFlyoutPinned
    {
        get => (bool)GetValue(IsFlyoutPinnedProperty);
        set => SetValue(IsFlyoutPinnedProperty, value);
    }

    public double DockWidth
    {
        get => (double)GetValue(DockWidthProperty);
        set => SetValue(DockWidthProperty, value);
    }

    public ToolboxView()
    {
        InitializeComponent();

        flyoutCloseTimer.Tick += FlyoutCloseTimer_Tick;
        Loaded += ToolboxView_Loaded;
        Unloaded += ToolboxView_Unloaded;
    }

    private void ToolboxView_Loaded(object sender, RoutedEventArgs e)
    {
        XLocalizationService.Current.LanguageChanged -= LocalizationService_LanguageChanged;
        XLocalizationService.Current.LanguageChanged += LocalizationService_LanguageChanged;

        if (IsFlyoutPinned)
            SetFlyoutOpen(true);
        else
            UpdateFlyoutPresentation();
    }

    private void ToolboxView_Unloaded(object sender, RoutedEventArgs e)
    {
        flyoutCloseTimer.Stop();
        XLocalizationService.Current.LanguageChanged -= LocalizationService_LanguageChanged;
    }

    private void LocalizationService_LanguageChanged(object? sender, XLanguageChangedEventArgs e)
    {
        UpdateFlyoutPresentation();
    }

    private void ToolboxRoot_MouseLeave(object sender, MouseEventArgs e) => ScheduleFlyoutClose();

    private void Flyout_MouseEnter(object sender, MouseEventArgs e) => flyoutCloseTimer.Stop();

    private void Flyout_MouseLeave(object sender, MouseEventArgs e) => ScheduleFlyoutClose();

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsFlyoutPinned)
        {
            SetFlyoutOpen(true);
        }
    }

    private static void OnDockWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToolboxView { IsLoaded: true } toolbox)
            toolbox.UpdateFlyoutPresentation();
    }

    private static void OnIsFlyoutPinnedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ToolboxView { IsLoaded: true } toolbox)
            return;

        if ((bool)e.NewValue)
            toolbox.SetFlyoutOpen(true);

        toolbox.UpdateFlyoutPresentation();
    }

    private void PinnedSplitter_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
    {
        if (IsFlyoutPinned)
            DockWidth = Math.Clamp(DockWidth - e.HorizontalChange, 420, 900);
    }

    private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, PART_Tabs))
        {
            return;
        }

        UpdateFlyoutPresentation();
    }

    private void RailButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string indexText } &&
            int.TryParse(indexText, out int index))
        {
            PART_Tabs.SelectedIndex = index;
        }

        OpenFlyout();
    }

    public void ToggleFlyout()
    {
        SetFlyoutOpen(!IsFlyoutOpen);
    }

    public void OpenFlyout()
    {
        SetFlyoutOpen(true);
    }

    private void ScheduleFlyoutClose()
    {
        if (IsFlyoutPinned)
        {
            return;
        }

        flyoutCloseTimer.Stop();
        flyoutCloseTimer.Start();
    }

    private void FlyoutCloseTimer_Tick(object? sender, EventArgs e)
    {
        flyoutCloseTimer.Stop();

        if (IsMouseOver)
        {
            return;
        }

        if (Mouse.LeftButton == MouseButtonState.Pressed)
        {
            flyoutCloseTimer.Start();
            return;
        }

        SetFlyoutOpen(false);
    }

    private void SetFlyoutOpen(bool isOpen)
    {
        flyoutCloseTimer.Stop();
        IsFlyoutOpen = isOpen;

        if (isOpen)
        {
            UpdateFlyoutPresentation();
        }
    }

    private void UpdateFlyoutPresentation()
    {
        PART_FlyoutSurface.Width = Math.Max(420, DockWidth);

        PART_HeaderTitle.Text = PART_Tabs.SelectedIndex switch
        {
            0 => Loc("Toolbox.HeaderControls"),
            1 => Loc("Toolbox.HeaderTemplates"),
            2 => Loc("Toolbox.HeaderPopups"),
            _ => Loc("Toolbox.HeaderProperties"),
        };
    }

    private static string Loc(string key)
    {
        return XLocalizationService.Current.GetString(
            UserFlowResources.ResourceManager,
            key);
    }
}
