using System.Windows;
using System.Windows.Controls;

namespace Mockup.UIControls;

/// <summary>
/// Small reusable toolbar for Undo/Redo commands and optional snapshot status.
/// The control intentionally keeps the inherited DataContext so it can bind to MockupViewModel.
/// </summary>
public partial class UndoRedoBar : UserControl
{
    public static readonly DependencyProperty ShowStatusProperty =
        DependencyProperty.Register(
            nameof(ShowStatus),
            typeof(bool),
            typeof(UndoRedoBar),
            new PropertyMetadata(true));

    public bool ShowStatus
    {
        get => (bool)GetValue(ShowStatusProperty);
        set => SetValue(ShowStatusProperty, value);
    }

    public UndoRedoBar()
    {
        InitializeComponent();
    }
}
