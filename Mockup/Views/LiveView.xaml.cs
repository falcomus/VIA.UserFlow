// ============================================================================
// FILE: Mockup.Views/LiveView.xaml.cs
// PURPOSE: LivePreview-Host für Screen + PreviewPopup-Overlay
// NOTES:
// - Popup-Overlay wird per Fade + Slide animiert
// - Slide-Offsets sind zentral über Konstanten steuerbar
// - Schließen läuft animiert, ohne dass das Overlay sofort collabiert
// ============================================================================

using Mockup.Helper;
using Mockup.ViewModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Mockup.Views;

/// <summary>
/// LivePreview für Screens inkl. animiertem Popup-Overlay.
/// </summary>
public partial class LiveView : UserControl
{
    #region === FIELDS / CONSTANTS ===

    private MockupViewModel? _vm;
    private INotifyPropertyChanged? _observedPreviewScreen;
    private INotifyPropertyChanged? _observedProject;
    private bool _isPopupClosingAnimated;
    private bool _isSyncingPreviewScrollBar;
    private int _popupAnimationVersion;

    // Popup animation tuning
    private const double PopupSlideOffsetEdge = 80d;
    private const double PopupSlideOffsetCenter = 30d;

    private const int PopupFadeInMs = 200;
    private const int PopupSlideInMs = 250;
    private const int PopupFadeOutMs = 150;
    private const int PopupSlideOutMs = 250;

    #endregion

    #region === CTOR ===

    public LiveView()
    {
        InitializeComponent();

        if (DesignModeHelper.IsInDesignMode)
            return;

        if (DesignModeHelper.IsInDesignMode)
            return;

        Loaded += LiveView_Loaded;
        Unloaded += LiveView_Unloaded;
        DataContextChanged += LiveView_DataContextChanged;
    }

    #endregion

    #region === LIFECYCLE ===

    private void LiveView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode)
            return;



        PART_LiveViewControl.PreviewScrollChanged -= LiveViewControl_PreviewScrollChanged;
        PART_LiveViewControl.PreviewScrollChanged += LiveViewControl_PreviewScrollChanged;
        PART_LiveViewControl.SizeChanged -= LiveViewControl_SizeChanged;
        PART_LiveViewControl.SizeChanged += LiveViewControl_SizeChanged;

        MockupService.Mockup.HomeScreen = MockupService.Mockup.CurrentScreen;
        PART_LiveViewControl.Screen = MockupService.Mockup.HomeScreen;


        AttachToViewModel(DataContext as MockupViewModel);
        SyncPopupVisualState(immediate: true);
        PART_LiveViewControl.RefreshPreview();
        InvalidateVisual();
        UpdatePreviewScrollBar();
    }

    private void LiveView_Unloaded(object? sender, RoutedEventArgs e)
    {
        PART_LiveViewControl.PreviewScrollChanged -= LiveViewControl_PreviewScrollChanged;
        PART_LiveViewControl.SizeChanged -= LiveViewControl_SizeChanged;

        DetachFromViewModel();
    }

    private void LiveView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        AttachToViewModel(e.NewValue as MockupViewModel);
        SyncPopupVisualState(immediate: true);
    }

    #endregion

    #region === VIEWMODEL WIRING ===

    private void AttachToViewModel(MockupViewModel? vm)
    {
        if (ReferenceEquals(_vm, vm))
            return;

        DetachFromViewModel();

        _vm = vm;

        if (_vm != null)
        {
            _vm.PropertyChanged += ViewModel_PropertyChanged;
            AttachPreviewScreen(_vm.PreviewScreen as INotifyPropertyChanged);
            AttachProject(_vm.CurrentProject as INotifyPropertyChanged);
        }

        UpdatePreviewScrollBar();
    }

    private void DetachFromViewModel()
    {
        if (_vm != null)
            _vm.PropertyChanged -= ViewModel_PropertyChanged;

        AttachPreviewScreen(null);
        AttachProject(null);

        _vm = null;
    }

    private void AttachPreviewScreen(INotifyPropertyChanged? previewScreen)
    {
        if (ReferenceEquals(_observedPreviewScreen, previewScreen))
            return;

        if (_observedPreviewScreen != null)
            _observedPreviewScreen.PropertyChanged -= PreviewScreen_PropertyChanged;

        _observedPreviewScreen = previewScreen;

        if (_observedPreviewScreen != null)
            _observedPreviewScreen.PropertyChanged += PreviewScreen_PropertyChanged;
    }

    private void AttachProject(INotifyPropertyChanged? project)
    {
        if (ReferenceEquals(_observedProject, project))
            return;

        if (_observedProject != null)
            _observedProject.PropertyChanged -= Project_PropertyChanged;

        _observedProject = project;

        if (_observedProject != null)
            _observedProject.PropertyChanged += Project_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MockupViewModel.IsPreviewPopupOpen))
        {
            Dispatcher.Invoke(() => SyncPopupVisualState(immediate: false));
            return;
        }

        if (e.PropertyName == nameof(MockupViewModel.PreviewPopupPosition))
        {
            if (_vm?.IsPreviewPopupOpen == true)
                Dispatcher.Invoke(() => PrepareOpenState(_vm.PreviewPopupPosition));
        }

        if (e.PropertyName == nameof(MockupViewModel.PreviewScreen))
        {
            AttachPreviewScreen(_vm?.PreviewScreen as INotifyPropertyChanged);
            Dispatcher.Invoke(UpdatePreviewScrollBar);
            return;
        }

        if (e.PropertyName == nameof(MockupViewModel.CurrentProject))
        {
            AttachProject(_vm?.CurrentProject as INotifyPropertyChanged);
            Dispatcher.Invoke(UpdatePreviewScrollBar);
        }
    }

    private void PreviewScreen_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Screen.ScreenHeight)
            || e.PropertyName == nameof(Screen.UserHeight)
            || e.PropertyName == nameof(Screen.ShowHeader)
            || e.PropertyName == nameof(Screen.ShowFooter))
        {
            Dispatcher.Invoke(UpdatePreviewScrollBar);
        }
    }

    private void Project_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Project.DeviceHeight)
            || e.PropertyName == nameof(Project.DeviceWidth))
        {
            Dispatcher.Invoke(UpdatePreviewScrollBar);
        }
    }

    #endregion

    #region === PREVIEW SCROLLBAR ===

    private void LiveViewControl_PreviewScrollChanged(object? sender, EventArgs e)
    {
        UpdatePreviewScrollBar();
    }

    private void LiveViewControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdatePreviewScrollBar();
    }

    private void PreviewScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isSyncingPreviewScrollBar)
            return;

        PART_LiveViewControl.SetPreviewScrollValue(e.NewValue, notify: false);
        UpdatePreviewScrollBar();
    }

    private void UpdatePreviewScrollBar()
    {
        if (PART_PreviewScrollBar == null || PART_LiveViewControl == null)
            return;

        double maxScroll = PART_LiveViewControl.GetPreviewScrollMaximum();
        bool canScroll = maxScroll > 0.5d;

        _isSyncingPreviewScrollBar = true;

        PART_PreviewScrollBar.Maximum = maxScroll;
        PART_PreviewScrollBar.ViewportSize = Math.Max(1d, PART_LiveViewControl.ActualHeight);
        PART_PreviewScrollBar.LargeChange = Math.Max(30d, PART_LiveViewControl.ActualHeight * 0.75d);
        PART_PreviewScrollBar.SmallChange = 15d;

        if (!canScroll)
            PART_LiveViewControl.SetPreviewScrollValue(0d, notify: false);

        PART_PreviewScrollBar.Value = PART_LiveViewControl.GetPreviewScrollValue();
        PART_PreviewScrollBar.Visibility = canScroll ? Visibility.Visible : Visibility.Collapsed;

        _isSyncingPreviewScrollBar = false;
    }

    #endregion

    #region === UI EVENTS ===

    private void PreviewPopupOverlay_MouseLeftButtonDown(
        object sender,
        System.Windows.Input.MouseButtonEventArgs e
    )
    {
        if (DataContext is not MockupViewModel vm)
            return;

        vm.ClosePreviewPopup();
        e.Handled = true;
    }

    private void Root_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is not MockupViewModel vm || vm.CurrentProject is null)
            return;

        if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            return;

        int step = 10;
        int maxZoomPercent = PART_ZoomSlider.MaxZoomPercent;
        int minZoomPercent = PART_ZoomSlider.MinZoomPercent;

        double newZoom = e.Delta > 0
            ? vm.CurrentProject.PreviewZoomPercent += step
            : vm.CurrentProject.PreviewZoomPercent -= step;

        newZoom = Math.Clamp(newZoom, minZoomPercent, maxZoomPercent);

        vm.CurrentProject.PreviewZoomPercent = newZoom;

        e.Handled = true;
    }

    #endregion

    #region === POPUP VISUAL STATE ===

    private void SyncPopupVisualState(bool immediate)
    {
        if (_vm == null)
        {
            CollapsePopupOverlay();
            return;
        }

        if (_vm.IsPreviewPopupOpen)
        {
            ShowPopupOverlay(immediate);
            return;
        }

        HidePopupOverlay(immediate);
    }

    private void ShowPopupOverlay(bool immediate)
    {
        StopPopupAnimations();

        PART_PopupOverlayRoot.Visibility = Visibility.Visible;
        PART_PopupOverlayRoot.IsHitTestVisible = true;

        if (_vm == null)
            return;

        if (immediate)
        {
            PART_PopupDimmer.Opacity = 1d;
            PART_PopupCard.Opacity = 1d;
            PART_PopupTranslate.X = 0d;
            PART_PopupTranslate.Y = 0d;
            _isPopupClosingAnimated = false;
            return;
        }

        PrepareOpenState(_vm.PreviewPopupPosition);

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        PART_PopupDimmer.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(0d, 1d, TimeSpan.FromMilliseconds(PopupFadeInMs))
        );

        PART_PopupCard.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(0d, 1d, TimeSpan.FromMilliseconds(PopupFadeInMs))
        );

        PART_PopupTranslate.BeginAnimation(
            System.Windows.Media.TranslateTransform.XProperty,
            new DoubleAnimation(
                PART_PopupTranslate.X,
                0d,
                TimeSpan.FromMilliseconds(PopupSlideInMs)
            )
            {
                EasingFunction = ease,
            }
        );

        PART_PopupTranslate.BeginAnimation(
            System.Windows.Media.TranslateTransform.YProperty,
            new DoubleAnimation(
                PART_PopupTranslate.Y,
                0d,
                TimeSpan.FromMilliseconds(PopupSlideInMs)
            )
            {
                EasingFunction = ease,
            }
        );

        _isPopupClosingAnimated = false;
    }

    private void HidePopupOverlay(bool immediate)
    {
        if (immediate)
        {
            CollapsePopupOverlay();
            return;
        }

        if (PART_PopupOverlayRoot.Visibility != Visibility.Visible || _isPopupClosingAnimated)
        {
            CollapsePopupOverlay();
            return;
        }

        _isPopupClosingAnimated = true;
        PART_PopupOverlayRoot.IsHitTestVisible = false;

        int version = ++_popupAnimationVersion;
        var closeOffset = GetPopupSlideOffset(_vm?.PreviewPopupPosition);
        var ease = new CubicEase { EasingMode = EasingMode.EaseIn };

        PART_PopupDimmer.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(
                PART_PopupDimmer.Opacity,
                0d,
                TimeSpan.FromMilliseconds(PopupFadeOutMs)
            )
        );

        PART_PopupCard.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(
                PART_PopupCard.Opacity,
                0d,
                TimeSpan.FromMilliseconds(PopupFadeOutMs)
            )
        );

        PART_PopupTranslate.BeginAnimation(
            System.Windows.Media.TranslateTransform.XProperty,
            new DoubleAnimation(
                PART_PopupTranslate.X,
                closeOffset.X,
                TimeSpan.FromMilliseconds(PopupSlideOutMs)
            )
            {
                EasingFunction = ease,
            }
        );

        var yAnim = new DoubleAnimation(
            PART_PopupTranslate.Y,
            closeOffset.Y,
            TimeSpan.FromMilliseconds(PopupSlideOutMs)
        )
        {
            EasingFunction = ease,
        };

        yAnim.Completed += (_, _) =>
        {
            if (version != _popupAnimationVersion)
                return;

            if (_vm?.IsPreviewPopupOpen == true)
                return;

            CollapsePopupOverlay();
        };

        PART_PopupTranslate.BeginAnimation(
            System.Windows.Media.TranslateTransform.YProperty,
            yAnim
        );
    }

    private void PrepareOpenState(ScreenPopupPosition position)
    {
        var offset = GetPopupSlideOffset(position);

        PART_PopupDimmer.Opacity = 0d;
        PART_PopupCard.Opacity = 0d;
        PART_PopupTranslate.X = offset.X;
        PART_PopupTranslate.Y = offset.Y;
    }

    private Point GetPopupSlideOffset(ScreenPopupPosition? position)
    {
        return position switch
        {
            ScreenPopupPosition.Left => new Point(-PopupSlideOffsetEdge, 0d),
            ScreenPopupPosition.Right => new Point(PopupSlideOffsetEdge, 0d),
            ScreenPopupPosition.Top => new Point(0d, -PopupSlideOffsetEdge),
            ScreenPopupPosition.Bottom => new Point(0d, PopupSlideOffsetEdge),
            ScreenPopupPosition.Center => new Point(0d, PopupSlideOffsetCenter),
            ScreenPopupPosition.MousePos => new Point(0d, PopupSlideOffsetCenter),
            _ => new Point(0d, PopupSlideOffsetCenter),
        };
    }

    private void CollapsePopupOverlay()
    {
        StopPopupAnimations();

        PART_PopupDimmer.Opacity = 0d;
        PART_PopupCard.Opacity = 0d;
        PART_PopupTranslate.X = 0d;
        PART_PopupTranslate.Y = 0d;

        PART_PopupOverlayRoot.IsHitTestVisible = false;
        PART_PopupOverlayRoot.Visibility = Visibility.Collapsed;

        _isPopupClosingAnimated = false;
    }

    private void StopPopupAnimations()
    {
        PART_PopupDimmer.BeginAnimation(OpacityProperty, null);
        PART_PopupCard.BeginAnimation(OpacityProperty, null);
        PART_PopupTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, null);
        PART_PopupTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, null);
    }

    #endregion

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        MockupService.Mockup.HomeScreen = MockupService.Mockup.CurrentProject?.Screens.FirstOrDefault(x => x.IsHomeScreen);
        PART_LiveViewControl.Screen = MockupService.Mockup.HomeScreen;
        MockupService.Mockup.NavigationTrail.Clear();
        UpdatePreviewScrollBar();
    }
}
