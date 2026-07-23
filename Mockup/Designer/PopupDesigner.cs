// ============================================================================
// FILE: Mockup.Designer/PopupDesigner.cs
// PURPOSE:
// - Interaktiver Designer für ScreenPopup
// - Verhalten analog zu TemplateDesigner
// - Bands sind Single-Source-of-Truth für HitTest / Drag / Drop
// - Popup kennt jetzt echte Gesamtgeometrie + Header/Content-Trennung
//
// WICHTIG:
// - Width / Height des Designers = äußere Popup-Gesamtgröße
// - Controls liegen fachlich im Content-Bereich
// - Header-/Content-Geometrie wird hier zentral verfügbar gemacht
// ============================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;
using System.ComponentModel;
using System.Windows;

namespace Mockup.Designer;

[ObservableObject]
public partial class PopupDesigner : BaseDesigner
{
    #region === POPUP GEOMETRY =========================================================

    /// <summary>
    /// Äußere Gesamtbreite des Popups.
    /// </summary>
    public float PopupOuterWidth => ScreenPopup?.Width ?? 0f;

    /// <summary>
    /// Äußere Gesamthöhe des Popups.
    /// </summary>
    public float PopupOuterHeight => ScreenPopup?.Height ?? 0f;

    /// <summary>
    /// Höhe des Popup-Headers.
    /// </summary>
    public float PopupHeaderHeight =>
        ScreenPopup?.HasHeader == true ? MathF.Max(0f, ScreenPopup.HeaderHeight) : 0f;

    /// <summary>
    /// Y-Start des Content-Bereichs innerhalb des Popups.
    /// </summary>
    public float PopupContentTop => ScreenPopup?.ContentTop ?? 0f;

    /// <summary>
    /// Nutzbare Höhe des Content-Bereichs.
    /// </summary>
    public float PopupContentHeight => ScreenPopup?.ContentHeight ?? 0f;

    /// <summary>
    /// Gesamtrechteck des Popups.
    /// </summary>
    public SKRect PopupOuterRect => new(0, 0, PopupOuterWidth, PopupOuterHeight);

    /// <summary>
    /// Content-Rechteck des Popups.
    /// </summary>
    public SKRect PopupContentRect =>
        new(0, PopupContentTop, PopupOuterWidth, PopupContentTop + PopupContentHeight);

    #endregion

    #region === DESIGNER WORLD BOUNDS ==================================================

    /// <summary>
    /// Der Designer arbeitet in äußeren Popup-Koordinaten.
    /// Der Header gehört also zur Weltgröße dazu.
    /// </summary>
    //protected override SKRect GetDesignerWorldBounds()
    //{
    //    //XXX oder so?
    //    //return PopupContentRect;
    //    return PopupOuterRect;
    //}

    protected override SKRect GetDesignerWorldBounds()
    {
        if (IsPreviewHost || LiveMode)
            return new SKRect(0, 0, PopupOuterWidth, PopupContentHeight);

        return PopupOuterRect;
    }

    #endregion

    #region === BAND ACCESS ============================================================

    internal override IEnumerable<Band> GetAllBands() => ScreenPopup?.Bands ?? Enumerable.Empty<Band>();

    protected override IEnumerable<Band>? GetCustomBands() => ScreenPopup?.Bands ?? [];

    protected override Band? GetHeaderBand() => null;

    protected override Band? GetFooterBand() => null;

    #endregion

    #region === FIELD / DP =============================================================

    private ScreenPopup? _popupModel;

    public ScreenPopup? ScreenPopup
    {
        get => (ScreenPopup?)GetValue(ScreenPopupProperty);
        set => SetValue(ScreenPopupProperty, value);
    }

    public static readonly DependencyProperty ScreenPopupProperty = DependencyProperty.Register(
        nameof(ScreenPopup),
        typeof(ScreenPopup),
        typeof(PopupDesigner),
        new PropertyMetadata(null, OnScreenPopupChanged)
    );

    private static void OnScreenPopupChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is not PopupDesigner designer)
            return;

        designer.DeselectAllControls();

        if (designer._popupModel != null)
            designer._popupModel.PropertyChanged -= designer.OnPopupModelChanged;

        designer._popupModel = e.NewValue as ScreenPopup;

        if (designer._popupModel != null)
            designer._popupModel.PropertyChanged += designer.OnPopupModelChanged;

        designer.ScrollOffsetY = 0;
        designer.SyncDesignerSizeFromModel();
        designer.InvalidateDesigner();
    }

    #endregion

    #region === STATIC / CTOR ==========================================================

    static PopupDesigner()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(PopupDesigner),
            new FrameworkPropertyMetadata(typeof(PopupDesigner))
        );
    }

    public PopupDesigner()
    {
        DesignerKind = DesignerKind.Popup;
        AllowBandInteraction = true;
    }

    #endregion

    #region === SIZE / MODEL CHANGE ====================================================

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        InvalidateDesigner();
    }

    private void OnPopupModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ScreenPopup.Width):
            case nameof(ScreenPopup.Height):
            case nameof(ScreenPopup.HasHeader):
            case nameof(ScreenPopup.HeaderHeight):
                SyncDesignerSizeFromModel();
                break;
        }

        InvalidateDesigner();
    }

    //private void SyncDesignerSizeFromModel()
    //{
    //    if (_popupModel == null)
    //        return;

    //    Width = _popupModel.Width;
    //    Height = _popupModel.Height;
    //}
    private void SyncDesignerSizeFromModel()
    {
        if (_popupModel == null)
            return;

        Width = _popupModel.Width;

        if (IsPreviewHost || LiveMode)
            return;

        Height = _popupModel.Height;
    }

    #endregion
}
