// ======================================================================================
// FILE: Mockup/Actions/ActionAreaEditor.xaml.cs
// ======================================================================================

using Mockup.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Mockup.Actions;

public partial class ActionAreaEditor : ModalDialogWindow
{
    public ActionAreaEditor()
    {
        InitializeComponent();

        DataContextChanged += (_, __) => TryWireCloseFromViewModel();
        Loaded += (_, __) => TryWireCloseFromViewModel();
    }

    private void ContentPresenter_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ContentPresenter contentPresenter)
            return;

        var actionControl =
            contentPresenter.ContentTemplate.FindName("actionControl", contentPresenter)
                as ActionItemControl;

        if (actionControl is not null && contentPresenter.DataContext is ActionRow row)
            actionControl.Row = row;
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
        if (sender is not DependencyObject dependencyObject)
            return;

        DependencyObject? parent = dependencyObject;

        while (parent is not null && parent is not ListBoxItem)
            parent = VisualTreeHelper.GetParent(parent);

        if (parent is ListBoxItem listBoxItem)
            listBoxItem.IsSelected = true;
    }
}