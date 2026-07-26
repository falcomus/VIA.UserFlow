//// ============================================================================
//// FILE: Mockup.Views/PopupView.xaml.cs
//// MO44 – Struktur-Refactor
//// - Struktur + defensive Null-Checks
//// - Popup-Preview-Position in der Device-Vorschau abhängig von ScreenPopupPosition
//// - Resize-Richtung abhängig von ScreenPopupPosition
//// - Scrollbare Vorschaufläche berücksichtigt Popup-Überstand inkl. Edit/Resize-UI
////
//// ANPASSUNG:
//// - PopupDesigner im Editor immer oben ausgerichtet
//// - Höhen-Resize wächst immer nur nach unten
//// ============================================================================

using Mockup.Helper;
using Mockup.Messages;
using Mockup.ViewModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Mockup.Views;

public partial class PopupView : UserControl
{
    #region === DP: POPUP PREVIEW POSITION ===

    public double PopupPreviewLeft
    {
        get => (double)GetValue(PopupPreviewLeftProperty);
        set => SetValue(PopupPreviewLeftProperty, value);
    }

    public static readonly DependencyProperty PopupPreviewLeftProperty =
        DependencyProperty.Register(
            nameof(PopupPreviewLeft),
            typeof(double),
            typeof(PopupView),
            new PropertyMetadata(0d)
        );

    public double PopupPreviewTop
    {
        get => (double)GetValue(PopupPreviewTopProperty);
        set => SetValue(PopupPreviewTopProperty, value);
    }

    public static readonly DependencyProperty PopupPreviewTopProperty =
        DependencyProperty.Register(
            nameof(PopupPreviewTop),
            typeof(double),
            typeof(PopupView),
            new PropertyMetadata(0d)
        );

    public double PopupPreviewSurfaceWidth
    {
        get => (double)GetValue(PopupPreviewSurfaceWidthProperty);
        set => SetValue(PopupPreviewSurfaceWidthProperty, value);
    }

    public static readonly DependencyProperty PopupPreviewSurfaceWidthProperty =
        DependencyProperty.Register(
            nameof(PopupPreviewSurfaceWidth),
            typeof(double),
            typeof(PopupView),
            new PropertyMetadata(0d)
        );

    public double PopupPreviewSurfaceHeight
    {
        get => (double)GetValue(PopupPreviewSurfaceHeightProperty);
        set => SetValue(PopupPreviewSurfaceHeightProperty, value);
    }

    public static readonly DependencyProperty PopupPreviewSurfaceHeightProperty =
        DependencyProperty.Register(
            nameof(PopupPreviewSurfaceHeight),
            typeof(double),
            typeof(PopupView),
            new PropertyMetadata(0d)
        );

    #endregion

    #region === FIELDS ===

    private MockupViewModel? _vm;
    private ScreenPopup? _subscribedPopup;

    private const double PopupHostPadding = 8d;
    private const double PopupWidthThumbOverhang = 22d;
    private const double PopupHeightThumbOverhang = 22d;

    #endregion

    #region === CTOR / INIT ===

    public PopupView()
    {
        InitializeComponent();

        Loaded += PopupView_Loaded;
        Unloaded += PopupView_Unloaded;
    }

    #endregion

    #region === LOADED / UNLOADED ===

    private void PopupView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode)
            return;

        PART_PopupDesignerControl.PART_Designer.FocusDesignerSurface();

        AttachToViewModel(DataContext as MockupViewModel);
        ApplyPopupPreviewPlacement();
    }

    private void PopupView_Unloaded(object sender, RoutedEventArgs e)
    {
        DetachFromViewModel();
    }

    #endregion

    #region === VIEWMODEL / POPUP SUBSCRIPTIONS ===

    private void AttachToViewModel(MockupViewModel? vm)
    {
        if (ReferenceEquals(_vm, vm))
            return;

        DetachFromViewModel();

        _vm = vm;

        if (_vm == null)
            return;

        _vm.PropertyChanged += OnViewModelPropertyChanged;
        AttachToCurrentPopup(_vm.CurrentPopup);
    }

    private void DetachFromViewModel()
    {
        if (_vm != null)
            _vm.PropertyChanged -= OnViewModelPropertyChanged;

        DetachFromCurrentPopup();

        _vm = null;
    }

    private void AttachToCurrentPopup(ScreenPopup? popup)
    {
        if (ReferenceEquals(_subscribedPopup, popup))
            return;

        DetachFromCurrentPopup();

        _subscribedPopup = popup;

        if (_subscribedPopup != null)
            _subscribedPopup.PropertyChanged += OnCurrentPopupPropertyChanged;
    }

    private void DetachFromCurrentPopup()
    {
        if (_subscribedPopup != null)
            _subscribedPopup.PropertyChanged -= OnCurrentPopupPropertyChanged;

        _subscribedPopup = null;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_vm == null)
            return;

        if (e.PropertyName == nameof(MockupViewModel.CurrentPopup))
        {
            AttachToCurrentPopup(_vm.CurrentPopup);
            ApplyPopupPreviewPlacement();
            return;
        }

        if (
            e.PropertyName == nameof(MockupViewModel.CurrentProject)
            || e.PropertyName == nameof(MockupViewModel.PopupDesignerWidth)
            || e.PropertyName == nameof(MockupViewModel.PopupDesignerHeight)
        )
        {
            ApplyPopupPreviewPlacement();
            return;
        }
    }

    private void OnCurrentPopupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_vm == null || _vm.CurrentProject == null || _vm.CurrentPopup == null)
            return;

        if (e.PropertyName == nameof(ScreenPopup.Position))
        {
            var popup = _vm.CurrentPopup;
            var project = _vm.CurrentProject;

            switch (popup.Position)
            {
                case ScreenPopupPosition.Left:
                case ScreenPopupPosition.Right:
                    {
                        double minHeight = GetPopupMinimumOuterHeight(popup);
                        double targetHeight = Math.Max(project.DeviceHeight, minHeight);

                        _vm.PopupDesignerWidth = popup.Width;
                        _vm.PopupDesignerHeight = (float)targetHeight;
                        popup.Height = (float)targetHeight;
                        break;
                    }

                case ScreenPopupPosition.Top:
                case ScreenPopupPosition.Bottom:
                    {
                        double minWidth = GetPopupMinimumOuterWidth(popup);
                        double targetWidth = Math.Max(project.DeviceWidth, minWidth);

                        _vm.PopupDesignerWidth = (float)targetWidth;
                        _vm.PopupDesignerHeight = popup.Height;
                        popup.Width = (float)targetWidth;
                        break;
                    }

                case ScreenPopupPosition.Center:
                case ScreenPopupPosition.MousePos:
                default:
                    _vm.PopupDesignerWidth = popup.Width;
                    _vm.PopupDesignerHeight = popup.Height;
                    break;
            }

            ApplyPopupPreviewPlacement();

            PART_PopupDesignerControl?.InvalidateMeasure();
            PART_PopupDesignerControl?.InvalidateArrange();
            PART_PopupDesignerControl?.UpdateLayout();
            PART_PopupDesignerControl?.InvalidateVisual();

            InvalidateMeasure();
            InvalidateArrange();
            UpdateLayout();
            InvalidateVisual();

            MSG.UI.InvalidateDesigner();
            return;
        }
    }

    #endregion

    #region === POPUP PREVIEW PLACEMENT ===

    private void ApplyPopupPreviewPlacement()
    {
        if (_vm == null || _vm.CurrentProject == null || _vm.CurrentPopup == null)
        {
            PopupPreviewLeft = 0d;
            PopupPreviewTop = 0d;
            PopupPreviewSurfaceWidth = 0d;
            PopupPreviewSurfaceHeight = 0d;

            UpdateLayout();

            PART_PopupDesignerControl?.InvalidateMeasure();
            PART_PopupDesignerControl?.InvalidateArrange();
            PART_PopupDesignerControl?.UpdateLayout();
            PART_PopupDesignerControl?.InvalidateVisual();

            InvalidateVisual();
            return;
        }

        var popup = _vm.CurrentPopup;

        double deviceWidth = Math.Max(0d, _vm.CurrentProject.DeviceWidth);
        double deviceHeight = Math.Max(0d, _vm.CurrentProject.DeviceHeight);

        double popupWidth = Math.Max(0d, _vm.PopupDesignerWidth);
        double popupHeight = Math.Max(0d, _vm.PopupDesignerHeight);

        double left;
        double top;

        switch (popup.Position)
        {
            case ScreenPopupPosition.Left:
                left = 0d;
                top = Math.Max(0d, (deviceHeight - popupHeight) / 2d);
                break;

            case ScreenPopupPosition.Right:
                left = Math.Max(0d, deviceWidth - popupWidth);
                top = Math.Max(0d, (deviceHeight - popupHeight) / 2d);
                break;

            case ScreenPopupPosition.Bottom:
                left = Math.Max(0d, (deviceWidth - popupWidth) / 2d);
                top = Math.Max(0d, deviceHeight - popupHeight);
                break;

            case ScreenPopupPosition.Top:
                left = Math.Max(0d, (deviceWidth - popupWidth) / 2d);
                top = 0d;
                break;

            case ScreenPopupPosition.MousePos:
            case ScreenPopupPosition.Center:
            default:
                left = Math.Max(0d, (deviceWidth - popupWidth) / 2d);
                top = Math.Max(0d, (deviceHeight - popupHeight) / 2d);
                break;
        }

        PopupPreviewLeft = left;
        PopupPreviewTop = top;

        double surfaceWidth = Math.Max(deviceWidth, left + popupWidth + PopupHostPadding);
        surfaceWidth = Math.Max(
            surfaceWidth,
            left + popupWidth + PopupWidthThumbOverhang + PopupHostPadding
        );

        double surfaceHeight = Math.Max(deviceHeight, top + popupHeight + PopupHostPadding);

        if (popup.Position == ScreenPopupPosition.Bottom)
        {
            surfaceHeight = Math.Max(surfaceHeight, deviceHeight + PopupHostPadding);
        }
        else
        {
            surfaceHeight = Math.Max(
                surfaceHeight,
                top + popupHeight + PopupHeightThumbOverhang + PopupHostPadding
            );
        }

        PopupPreviewSurfaceWidth = surfaceWidth;
        PopupPreviewSurfaceHeight = surfaceHeight;

        InvalidateVisual();
    }

    #endregion

    #region === RESIZE ===

    private void ResizeWidth(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not MockupViewModel vm)
            return;

        if (vm.CurrentProject == null)
            return;

        if (vm.CurrentPopup == null)
            return;

        double minWidth = GetPopupMinimumOuterWidth(vm.CurrentPopup);
        double maxWidth = Math.Max(minWidth, vm.CurrentProject.DeviceWidth);
        const double popupEdgeSnapTolerance = 20d;

        double newWidth =
            vm.CurrentPopup.Position == ScreenPopupPosition.Right
                ? vm.PopupDesignerWidth - e.HorizontalChange
                : vm.PopupDesignerWidth + e.HorizontalChange;

        if (Math.Abs(vm.CurrentProject.DeviceWidth - newWidth) <= popupEdgeSnapTolerance)
            newWidth = vm.CurrentProject.DeviceWidth;

        newWidth = Math.Round(newWidth / 10.0) * 10.0;
        newWidth = Math.Clamp(newWidth, minWidth, maxWidth);

        vm.PopupDesignerWidth = (float)newWidth;
        vm.CurrentPopup.Width = (float)vm.PopupDesignerWidth;

        ApplyPopupPreviewPlacement();
    }

    private void ResizeHeight(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not MockupViewModel vm)
            return;

        if (vm.CurrentProject == null)
            return;

        if (vm.CurrentPopup == null)
            return;

        double minHeight = GetPopupMinimumOuterHeight(vm.CurrentPopup);
        double maxHeight = Math.Max(minHeight, 2 * vm.CurrentProject.DeviceHeight);
        const double popupEdgeSnapTolerance = 20d;

        double newHeight =
            vm.CurrentPopup.Position == ScreenPopupPosition.Bottom
                ? vm.PopupDesignerHeight - e.VerticalChange
                : vm.PopupDesignerHeight + e.VerticalChange;

        if (Math.Abs(vm.CurrentProject.DeviceHeight - newHeight) <= popupEdgeSnapTolerance)
            newHeight = vm.CurrentProject.DeviceHeight;

        newHeight = Math.Round(newHeight / 10.0) * 10.0;
        newHeight = Math.Clamp(newHeight, minHeight, maxHeight);

        vm.PopupDesignerHeight = (float)newHeight;
        vm.CurrentPopup.Height = (float)vm.PopupDesignerHeight;

        ApplyPopupPreviewPlacement();
    }

    private static double GetPopupMinimumOuterWidth(ScreenPopup popup)
    {
        const double DEFAULT_MIN_WIDTH = 30d;

        if (popup.Bands == null || popup.Bands.Count == 0)
            return DEFAULT_MIN_WIDTH;

        double contentMinWidth = GetBandContentMinimumWidthFromControls(popup.Bands[0]);

        return Math.Max(DEFAULT_MIN_WIDTH, contentMinWidth);
    }

    private static double GetPopupMinimumOuterHeight(ScreenPopup popup)
    {
        const double DEFAULT_MIN_HEIGHT = 30d;

        double popupHeaderHeight = popup.HasHeader ? Math.Round(Math.Max(0d, popup.HeaderHeight)) : 0d;

        if (popup.Bands == null || popup.Bands.Count == 0)
            return Math.Max(DEFAULT_MIN_HEIGHT, popupHeaderHeight);

        double contentMinHeight = GetBandContentMinimumHeightFromControls(popup.Bands[0]);

        return Math.Max(Math.Max(DEFAULT_MIN_HEIGHT, popupHeaderHeight), popupHeaderHeight + contentMinHeight);
    }

    private static double GetBandContentMinimumWidthFromControls(Band band)
    {
        const double PADDING_RIGHT = 10d;

        double GetRequiredWidth(BandPage? page)
        {
            if (page == null || page.Controls == null || page.Controls.Count == 0)
                return 0d;

            double maxRight = 0d;

            foreach (var control in page.Controls)
            {
                double right = Math.Round(control.X + control.Width);
                if (right > maxRight)
                    maxRight = right;
            }

            return Math.Round(maxRight + PADDING_RIGHT);
        }

        if (band.Pages == null || band.Pages.Count == 0)
            return 0d;

        double requiredAcrossPages = 0d;

        foreach (var page in band.Pages)
            requiredAcrossPages = Math.Max(requiredAcrossPages, GetRequiredWidth(page));

        return requiredAcrossPages;
    }

    private static double GetBandContentMinimumHeightFromControls(Band band)
    {
        const double PADDING_BOTTOM = 10d;

        double GetRequiredHeight(BandPage? page)
        {
            if (page == null || page.Controls == null || page.Controls.Count == 0)
                return 0d;

            double maxBottom = 0d;

            foreach (var control in page.Controls)
            {
                double bottom = Math.Round(control.Y + control.Height);
                if (bottom > maxBottom)
                    maxBottom = bottom;
            }

            return Math.Round(maxBottom + PADDING_BOTTOM);
        }

        if (band.UniformPageHeight)
        {
            if (band.Pages == null || band.Pages.Count == 0)
                return 0d;

            double requiredAcrossPages = 0d;

            foreach (var page in band.Pages)
                requiredAcrossPages = Math.Max(requiredAcrossPages, GetRequiredHeight(page));

            return requiredAcrossPages;
        }

        return GetRequiredHeight(band.ActivePage);
    }

    #endregion

    #region === UI EVENTS ===

    private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;

        if (DataContext is MockupViewModel vm)
            vm.EditPopupCommand.Execute(vm.CurrentPopup);
    }

    private void Root_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is not MockupViewModel vm || vm.CurrentProject is null)
            return;

        if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            return;

        int step = 10;
        int maxZoomPercent = PART_DesignerToolbar.MaxZoomPercent;
        int minZoomPercent = PART_DesignerToolbar.MinZoomPercent;

        double newZoom = e.Delta > 0
            ? vm.CurrentProject.PopupZoomPercent += step
            : vm.CurrentProject.PopupZoomPercent -= step;

        newZoom = Math.Clamp(newZoom, minZoomPercent, maxZoomPercent);

        vm.CurrentProject.PopupZoomPercent = newZoom;

        e.Handled = true;
    }

    #endregion

    private void PopupListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PART_PopupDesignerControl.PART_Designer.FocusDesignerSurface();
    }
}
