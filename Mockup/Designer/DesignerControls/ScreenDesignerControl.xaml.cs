using GongSolutions.Wpf.DragDrop;
using Mockup.Messages;
using System.Windows;
using System.Windows.Controls;

namespace Mockup.Designer;

public partial class ScreenDesignerControl : UserControl, IDropTarget
{
    public ScreenDesignerControl()
    {
        InitializeComponent();
    }

    public Screen? Screen
    {
        get => (Screen?)GetValue(ScreenProperty);
        set => SetValue(ScreenProperty, value);
    }
    public object Bands { get; private set; }

    public static readonly DependencyProperty ScreenProperty =
        DependencyProperty.Register(
            nameof(Screen),
            typeof(Screen),
            typeof(ScreenDesignerControl),
            new PropertyMetadata(null, OnScreenChanged));

    private static void OnScreenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (ScreenDesignerControl)d;

        ctrl.PART_Designer.Screen = (Screen)e.NewValue;

        ctrl.OnScreenChanged(
            e.OldValue as Screen,
            e.NewValue as Screen);
    }

    private void OnScreenChanged(Screen? oldScreen, Screen? newScreen)
    {
    }


    void IDropTarget.DragOver(IDropInfo dropInfo)
    {
        PART_Designer?.OnDragOver(dropInfo);
    }

    void IDropTarget.Drop(IDropInfo dropInfo)
    {
        PART_Designer?.OnDrop(dropInfo);
    }

    private void UserControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = false;
        MSG.UI.InvalidateDesigner();
    }

    private void UserControl_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = false;
        MSG.UI.InvalidateDesigner();
    }
}
