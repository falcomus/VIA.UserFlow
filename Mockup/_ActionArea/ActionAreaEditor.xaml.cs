// ======================================================================================
// FILE: Mockup/Actions/ActionAreaEditor.xaml.cs
// ======================================================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Mockup.Actions;

public partial class ActionAreaEditor : Window
{
    public ActionAreaEditor()
    {
        InitializeComponent();

        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Title = "ActionArea Editor";

        DataContextChanged += (_, __) => TryWireCloseFromViewModel();
        Loaded += (_, __) => TryWireCloseFromViewModel();
    }

    private void ContentPresenter_Loaded(object sender, RoutedEventArgs e)
    {
        var cp = sender as ContentPresenter;
        if (cp == null) return;

        var actionControl = cp.ContentTemplate.FindName("actionControl", cp) as ActionItemControl;
        if (actionControl != null)
        {
            if (cp.DataContext != null)
            {
                actionControl.Row = (ActionRow)cp.DataContext;
            }
        }
    }


    private bool _wired;

    private void TryWireCloseFromViewModel()
    {
        if (_wired)
            return;

        if (DataContext is not ActionAreaEditorViewModel vm)
            return;

        _wired = true;

        vm.RequestClose += ok =>
        {
            Dispatcher.Invoke(() =>
            {
                DialogResult = ok;
                Close();
            });
        };
    }

    // Damit Click auf ComboBox auch Item auswählt
    private void ItemComboBox_SelectParent(object sender, RoutedEventArgs e)
    {
        if (sender is not DependencyObject d) return;

        DependencyObject? p = d;
        while (p is not null && p is not ListBoxItem)
            p = VisualTreeHelper.GetParent(p);

        if (p is ListBoxItem lbi)
            lbi.IsSelected = true;
    }

    private void TitleBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var rect = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight), 6, 4);
        Clip = rect;
    }
}


