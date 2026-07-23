using Mockup.Rendering;
using Mockup.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mockup.Views;

/// <summary>
/// Interaction logic for ProjectView.xaml
/// </summary>
public partial class ProjectView : UserControl
{
    public ProjectView()
    {
        InitializeComponent();
        Loaded += ProjectView_Loaded;
    }

    private void ProjectView_Loaded(object sender, RoutedEventArgs e)
    {
        ScreenThumbnail.RefreshVisibleThumbnails(this);
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
}