// ============================================================================
// FILE: Mockup.Designer/PopupDesignerControl.xaml.cs
// PURPOSE:
// - Host-Control für PopupDesigner
// - unterstützt Edit- und Preview-Betrieb
// - manipuliert im Preview-Modus keine VM-Größen
// - berücksichtigt im Preview den echten Popup-Content-Bereich
// ============================================================================

using GongSolutions.Wpf.DragDrop;
using Mockup.Messages;
using Mockup.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace Mockup.Designer;

public partial class PopupDesignerControl : UserControl, IDropTarget
{
    #region === CTOR ===

    public PopupDesignerControl()
    {
        InitializeComponent();

        Loaded += PopupDesignerControl_Loaded;
        SizeChanged += PopupDesignerControl_SizeChanged;
    }

    #endregion

    #region === DPs ===

    public ScreenPopup ScreenPopup
    {
        get => (ScreenPopup)GetValue(ScreenPopupProperty);
        set => SetValue(ScreenPopupProperty, value);
    }

    public static readonly DependencyProperty ScreenPopupProperty = DependencyProperty.Register(
        nameof(ScreenPopup),
        typeof(ScreenPopup),
        typeof(PopupDesignerControl),
        new PropertyMetadata(null, OnScreenPopupChanged)
    );

    public bool IsPreviewMode
    {
        get => (bool)GetValue(IsPreviewModeProperty);
        set => SetValue(IsPreviewModeProperty, value);
    }

    public static readonly DependencyProperty IsPreviewModeProperty = DependencyProperty.Register(
        nameof(IsPreviewMode),
        typeof(bool),
        typeof(PopupDesignerControl),
        new PropertyMetadata(false, OnIsPreviewModeChanged)
    );

    #endregion

    #region === DP CALLBACKS ===

    private static void OnScreenPopupChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is not PopupDesignerControl ctrl)
            return;

        ctrl.ApplyScreenPopup();
    }

    private static void OnIsPreviewModeChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is not PopupDesignerControl ctrl)
            return;

        ctrl.ApplyPreviewMode();
        ctrl.ApplyScreenPopup();
    }

    #endregion

    #region === LOAD / SIZE ===

    private void PopupDesignerControl_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyPreviewMode();
        ApplyScreenPopup();
        SyncDesignerSize();
    }

    private void PopupDesignerControl_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        SyncDesignerSize();
    }

    #endregion

    #region === APPLY STATE ===

    private void ApplyScreenPopup()
    {
        if (PART_Designer == null)
            return;

        if (DataContext == null)
            return;

        PART_Designer.ScreenPopup = ScreenPopup;

        if (!IsPreviewMode && ScreenPopup != null && DataContext is MockupViewModel vm)
        {
            vm.PopupDesignerWidth = ScreenPopup.Width;
            vm.PopupDesignerHeight = ScreenPopup.Height;
        }

        PART_Designer.DeselectAllControls();
        SyncDesignerSize();
        PART_Designer.InvalidateDesigner();
    }

    private void ApplyPreviewMode()
    {
        if (PART_Designer == null)
            return;

        if (DataContext == null)
            return;

        PART_Designer.LiveMode = IsPreviewMode;
        PART_Designer.IsPreviewHost = IsPreviewMode;
        PART_Designer.AllowBandInteraction = !IsPreviewMode;

        if (IsPreviewMode)
            PART_Designer.DeselectAllControls();

        PART_Designer.InvalidateDesigner();
    }

    //private void SyncDesignerSize()
    //{
    //    if (PART_Designer == null)
    //        return;

    //    PART_Designer.Width = ActualWidth;

    //    if (IsPreviewMode && ScreenPopup != null)
    //    {
    //        // Im Preview ist die verfügbare Fläche bereits der scrollbare Content-Bereich.
    //        // Der innere Designer darf deshalb nur auf der Content-Höhe arbeiten.
    //        PART_Designer.Height = Math.Max(0, ScreenPopup.ContentHeight);
    //        return;
    //    }

    //    // Im Editor arbeiten wir weiterhin auf der äußeren Popup-Gesamthöhe.
    //    PART_Designer.Height = ActualHeight;
    //}

    private void SyncDesignerSize()
    {
        if (PART_Designer == null)
            return;

        PART_Designer.Width = ActualWidth;

        if (IsPreviewMode)
        {
            PART_Designer.Height = Math.Max(0, (float)ActualHeight);
            return;
        }

        PART_Designer.Height = ActualHeight;
    }

    #endregion

    #region === DRAG & DROP ===

    void IDropTarget.DragOver(IDropInfo dropInfo)
    {
        if (IsPreviewMode)
            return;

        PART_Designer?.OnDragOver(dropInfo);
    }

    void IDropTarget.Drop(IDropInfo dropInfo)
    {
        if (IsPreviewMode)
            return;

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
