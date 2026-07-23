// ============================================================================
// FILE: Mockup.Designer/TemplateDesignerControl.cs
// FIX: Keine Verwendung von ScreenTemplate.Height
// MO44-konform – Größe kommt vom Control / Designer, nicht vom Model
// ============================================================================

using GongSolutions.Wpf.DragDrop;
using Mockup.Messages;
using Mockup.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace Mockup.Designer;

public partial class TemplateDesignerControl : UserControl, IDropTarget
{
    public TemplateDesignerControl()
    {
        InitializeComponent();
        SizeChanged += TemplateDesignerControl_SizeChanged;
    }

    #region === ScreenTemplate DependencyProperty ===

    public ScreenTemplate ScreenTemplate
    {
        get => (ScreenTemplate)GetValue(ScreenTemplateProperty);
        set => SetValue(ScreenTemplateProperty, value);
    }

    public static readonly DependencyProperty ScreenTemplateProperty =
        DependencyProperty.Register(
            nameof(ScreenTemplate),
            typeof(ScreenTemplate),
            typeof(TemplateDesignerControl),
            new PropertyMetadata(null, OnTemplateChanged));

    private static void OnTemplateChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is not TemplateDesignerControl ctrl)
            return;

        if (e.NewValue is ScreenTemplate tpl &&
            ctrl.PART_Designer != null)
        {
            ctrl.PART_Designer.ScreenTemplate = tpl;

            if (ctrl.DataContext is MockupViewModel vm)
            {
                vm.TemplateDesignerHeight = tpl.RootBand.Height;
            }

            ctrl.SyncDesignerSize();
        }
    }

    #endregion

    #region === Size Handling ===

    private void TemplateDesignerControl_SizeChanged(object? sender, SizeChangedEventArgs e)
        => SyncDesignerSize();


    private void SyncDesignerSize()
    {
        if (PART_Designer == null)
            return;

        PART_Designer.Width = ActualWidth;
        PART_Designer.Height = ActualHeight;
    }

    #endregion

    #region === Drag & Drop (Pass-Through) ===

    void IDropTarget.DragOver(IDropInfo dropInfo)
    {
        PART_Designer?.OnDragOver(dropInfo);
    }

    void IDropTarget.Drop(IDropInfo dropInfo)
    {
        PART_Designer?.OnDrop(dropInfo);
    }

    #endregion

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
