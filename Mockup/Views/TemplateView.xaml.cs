// ======================================================================================
// FILE: Mockup.Views/TemplateView.cs
// ======================================================================================

using Mockup.Helper;
using Mockup.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Mockup.Views;

public partial class TemplateView : UserControl
{
    #region === CTOR / INIT ===

    public TemplateView()
    {
        InitializeComponent();

        Loaded += TemplateView_Loaded;
    }

    #endregion === CTOR / INIT ===

    #region === LOADED ===

    private void TemplateView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode) return;

        PART_TemplateDesigner.PART_Designer.FocusDesignerSurface();
    }

    #endregion === LOADED ===

    #region === RESIZE ===

    // ------------------------------------------------------------
    // HEIGHT: wird über das einzige Custom-Band gesteuert
    // ------------------------------------------------------------
    private void TemplateResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not MockupViewModel vm)
            return;

        double minHeight = GetTemplateMinimumHeight(vm);
        double maxHeight = Math.Max(minHeight, vm.CurrentProject.DeviceHeight);

        double newHeight = vm.TemplateDesignerHeight + e.VerticalChange;

        newHeight = (int)(Math.Round(newHeight / 10.0) * 10);

        newHeight = Math.Clamp(newHeight, minHeight, maxHeight);

        vm.TemplateDesignerHeight = newHeight;
    }

    private static double GetTemplateMinimumHeight(MockupViewModel vm)
    {
        const double DEFAULT_MIN_HEIGHT = 40d;

        var template = vm.CurrentTemplate;
        if (template == null || template.Bands == null || template.Bands.Count == 0)
            return DEFAULT_MIN_HEIGHT;

        return Math.Max(DEFAULT_MIN_HEIGHT, GetBandMinimumHeightFromContent(template.Bands[0], true));
    }

    private static double GetBandMinimumHeightFromContent(Band band, bool includeBandHeader)
    {
        const double PADDING_BOTTOM = 10d;

        double headerHeight = includeBandHeader ? Math.Round(band.HeaderHeight) : 0d;
        double minHeight = headerHeight;

        if (band.MinHeight > 0)
            minHeight = Math.Max(minHeight, Math.Round(band.MinHeight));

        double GetRequiredHeight(BandPage? page)
        {
            if (page == null || page.Controls == null || page.Controls.Count == 0)
                return minHeight;

            double maxBottom = 0d;

            foreach (var control in page.Controls)
            {
                double bottom = Math.Round(control.Y + control.Height);
                if (bottom > maxBottom)
                    maxBottom = bottom;
            }

            return Math.Max(minHeight, headerHeight + Math.Round(maxBottom + PADDING_BOTTOM));
        }

        if (band.UniformPageHeight)
        {
            if (band.Pages == null || band.Pages.Count == 0)
                return minHeight;

            double requiredAcrossPages = minHeight;

            foreach (var page in band.Pages)
                requiredAcrossPages = Math.Max(requiredAcrossPages, GetRequiredHeight(page));

            return requiredAcrossPages;
        }

        return GetRequiredHeight(band.ActivePage);
    }

    #endregion === RESIZE ===

    #region === UI EVENTS ===

    private void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        e.Handled = true;

        if (DataContext is MockupViewModel vm)
        {
            vm.EditTemplateCommand.Execute(vm.CurrentTemplate);
        }
    }

    // Momentan automatisches Zuklappen nicht erwünscht!
    //private void OnTemplateGroupExpanded(object sender, System.Windows.RoutedEventArgs e)
    //{
    //    if (DataContext is MockupViewModel vm)
    //    {
    //        vm.CollapseAllTemplateGroupsCommand.Execute(sender);
    //        e.Handled = true;
    //    }
    //}

    #endregion === UI EVENTS ===

    private void PART_TemplateTreeView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is MockupViewModel vm)
        {
            ScreenTemplate? tmpl = ((Button)sender).DataContext as ScreenTemplate;

            if (tmpl != null)
            {
                vm.EditTemplateCommand.Execute(tmpl);
            }
        }
    }

    private void Thumb_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        e.Handled = true;
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
            ? vm.CurrentProject.TemplateZoomPercent += step
            : vm.CurrentProject.TemplateZoomPercent -= step;

        newZoom = Math.Clamp(newZoom, minZoomPercent, maxZoomPercent);

        vm.CurrentProject.TemplateZoomPercent = newZoom;

        e.Handled = true;
    }

    private void TemplateListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PART_TemplateDesigner.PART_Designer.FocusDesignerSurface();
    }

    private void TemplateNavigatorSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (DataContext is MockupViewModel vm)
        {
            vm.TemplateNavigatorWidth = Math.Max(430, TemplateNavigatorColumn.ActualWidth);
            TemplateNavigatorColumn.Width = new GridLength(vm.TemplateNavigatorWidth);
        }
    }

    private void TemplateNavigatorSplitter_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is MockupViewModel vm)
            vm.TemplateNavigatorWidth = Math.Max(430, TemplateNavigatorColumn.ActualWidth);
    }
}
