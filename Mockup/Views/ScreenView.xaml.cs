using Mockup.Helper;
using Mockup.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Mockup.Views;

public partial class ScreenView : UserControl
{
    #region === CTOR / INIT ===

    public ScreenView()
    {
        InitializeComponent();

        Loaded += ScreenView_Loaded;
    }

    #endregion === CTOR / INIT ===

    #region === LOADED ===

    private void ScreenView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode)
            return;

        if (DataContext is not MockupViewModel vm)
            return;

        if (vm.CurrentScreen == null)
            return;

        PART_ScreenDesigner.PART_Designer.FocusDesignerSurface();

        var screen = vm.CurrentScreen;

        var groupName = string.IsNullOrWhiteSpace(screen.GroupName) ? "General" : screen.GroupName;
    }

    #endregion === LOADED ===

    #region === UI EVENTS ===

    private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;

        if (DataContext is MockupViewModel vm)
        {
            vm.EditScreenCoreCommand.Execute(vm.CurrentScreen);
        }
    }

    #endregion === UI EVENTS ===

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
            ? vm.CurrentProject.ScreenZoomPercent += step
            : vm.CurrentProject.ScreenZoomPercent -= step;

        newZoom = Math.Clamp(newZoom, minZoomPercent, maxZoomPercent);

        vm.CurrentProject.ScreenZoomPercent = newZoom;

        e.Handled = true;
    }

    private void ScreenListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PART_ScreenDesigner.PART_Designer.FocusDesignerSurface();
    }


    private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not MockupViewModel vm)
            return;

        var screen = vm.CurrentScreen;
        if (screen == null)
            return;

        float zoom = 1f;
        if (vm.CurrentProject != null)
        {
            zoom = (float)(vm.CurrentProject.ScreenZoomPercent / 100.0);
            if (!float.IsFinite(zoom) || zoom <= 0f)
                zoom = 1f;
        }

        float dy = (float)(e.VerticalChange / zoom);

        screen.ResizeScreenFromDesigner(dy);

        e.Handled = true;
    }



}
