using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.Rendering;
using Mockup.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Mockup.Views;

/// <summary>
/// Interaction logic for ProjectView.xaml
/// </summary>
[ObservableObject]
public partial class ProjectView : UserControl
{
    private readonly DispatcherTimer _hoverCloseTimer;
    private FrameworkElement? _hoveredCardElement;

    public ProjectView()
    {
        InitializeComponent();

        Loaded += ProjectView_Loaded;

        _hoverCloseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _hoverCloseTimer.Tick += HoverCloseTimer_Tick;
    }

    [ObservableProperty]
    private Screen? hoveredScreen;

    [ObservableProperty]
    private bool isHoverActionPanelMouseOver;

    [ObservableProperty]
    private double hoverOverlayX;

    [ObservableProperty]
    private double hoverOverlayY;

    private void ProjectView_Loaded(object sender, RoutedEventArgs e)
    {
        ScreenThumbnail.RefreshVisibleThumbnails(this);
        RepositionHoverOverlay();
    }

    private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RepositionHoverOverlay();
    }

    private void Root_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is not MockupViewModel vm || vm.CurrentProject is null)
            return;

        if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            return;

        int step = 2;
        int maxZoomPercent = PART_ZoomSlider.MaxZoomPercent;
        int minZoomPercent = PART_ZoomSlider.MinZoomPercent;

        double newZoom = e.Delta > 0
            ? vm.CurrentProject.ProjectZoomPercent += step
            : vm.CurrentProject.ProjectZoomPercent -= step;

        newZoom = Math.Clamp(newZoom, minZoomPercent, maxZoomPercent);

        vm.CurrentProject.ProjectZoomPercent = newZoom;

        RepositionHoverOverlay();

        e.Handled = true;
    }

    private void OpenSelectedProject_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MockupViewModel vm)
            return;

        vm.OpenSelectedProjectFile();
    }


    private void DeleteSelectedProject_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MockupViewModel vm)
            return;

        vm.DeleteSelectedProjectFile();
    }

    private void ProjectMoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement button || button.ContextMenu == null)
            return;

        button.ContextMenu.DataContext = button.DataContext;
        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.IsOpen = true;
    }

    private void ScreenMoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement button || button.ContextMenu == null)
            return;

        button.ContextMenu.DataContext = button.Tag ?? button.DataContext;
        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.IsOpen = true;
        e.Handled = true;
    }

    private void ScreenMoreButton_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement button || button.ContextMenu == null)
            return;

        button.ContextMenu.DataContext = button.Tag ?? button.DataContext;
        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.IsOpen = true;
    }


    private void ScreenCard_MouseEnter(object sender, MouseEventArgs e)
    {
        _hoverCloseTimer.Stop();

        if (sender is FrameworkElement fe && fe.DataContext is Screen screen)
        {
            _hoveredCardElement = fe;
            HoveredScreen = screen;
            UpdateHoverOverlayPosition(fe);
        }
    }

    private void ScreenCard_MouseLeave(object sender, MouseEventArgs e)
    {
        StartHoverCloseTimer();
    }

    private void HoverActionPanel_MouseEnter(object sender, MouseEventArgs e)
    {
        _hoverCloseTimer.Stop();
        IsHoverActionPanelMouseOver = true;
    }

    private void HoverActionPanel_MouseLeave(object sender, MouseEventArgs e)
    {
        IsHoverActionPanelMouseOver = false;
        StartHoverCloseTimer();
    }

    private void StartHoverCloseTimer()
    {
        _hoverCloseTimer.Stop();
        _hoverCloseTimer.Start();
    }

    private void HoverCloseTimer_Tick(object? sender, EventArgs e)
    {
        _hoverCloseTimer.Stop();

        if (IsHoverActionPanelMouseOver)
            return;

        HoveredScreen = null;
        _hoveredCardElement = null;
    }

    private void RepositionHoverOverlay()
    {
        if (_hoveredCardElement == null || HoveredScreen == null)
            return;

        UpdateHoverOverlayPosition(_hoveredCardElement);
    }

    private void UpdateHoverOverlayPosition(FrameworkElement cardElement)
    {
        if (PART_ScreensHost == null || PART_ScreensOverlay == null || PART_OverlayBorder == null)
            return;

        if (PART_ScreensHost.ActualHeight == 0 || PART_ScreensHost.ActualWidth == 0)
            return;

        if (!cardElement.IsLoaded || cardElement.ActualWidth <= 0 || cardElement.ActualHeight <= 0)
            return;

        try
        {
            GeneralTransform transform = cardElement.TransformToAncestor(PART_ScreensHost);
            Point topLeft = transform.Transform(new Point(0, 0));

            Rect cardRect = new(topLeft.X, topLeft.Y, cardElement.ActualWidth, cardElement.ActualHeight);

            double overlayWidth = PART_OverlayBorder.ActualWidth;
            double x = cardRect.Left + cardRect.Width / 2 - overlayWidth / 2;
            double y = cardRect.Top - 45;

            y = Math.Max(y, 0);

            HoverOverlayX = Math.Round(x);
            HoverOverlayY = Math.Round(y);
        }
        catch
        {
        }
    }

}
