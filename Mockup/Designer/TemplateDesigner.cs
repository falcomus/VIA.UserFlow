// ======================================================================================
// FILE: Mockup.Designer/TemplateDesigner.cs
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;
using System.ComponentModel;
using System.Windows;

namespace Mockup.Designer;

[ObservableObject]
public partial class TemplateDesigner : BaseDesigner
{
    #region === DESIGNER WORLD BOUNDS ===

    protected override SKRect GetDesignerWorldBounds()
    {
        return new SKRect(0, 0, ScreenTemplate?.Width ?? 0, ScreenTemplate?.Height ?? 0);
    }

    #endregion === DESIGNER  WORLD BOUNDS ===


    #region ==== BAND ACCESS ===

    internal override IEnumerable<Band> GetAllBands() => ScreenTemplate?.Bands ?? Enumerable.Empty<Band>();

    protected override IEnumerable<Band>? GetCustomBands() => ScreenTemplate?.Bands;

    protected override Band? GetHeaderBand() => null;

    protected override Band? GetFooterBand() => null;

    #endregion ==== BAND ACCESS ===


    public ScreenTemplate? ScreenTemplate
    {
        get => (ScreenTemplate?)GetValue(ScreenTemplateProperty);
        set => SetValue(ScreenTemplateProperty, value);
    }

    public static readonly DependencyProperty ScreenTemplateProperty = DependencyProperty.Register(
        nameof(ScreenTemplate),
        typeof(ScreenTemplate),
        typeof(TemplateDesigner),
        new PropertyMetadata(null, OnScreenTemplateChanged)
    );

    private static void OnScreenTemplateChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is not TemplateDesigner designer)
            return;

        designer.DeselectAllControls();

        if (designer._templateModel != null)
            designer._templateModel.PropertyChanged -= designer.OnTemplateModelChanged;

        designer._templateModel = e.NewValue as ScreenTemplate;

        if (designer._templateModel != null)
            designer._templateModel.PropertyChanged += designer.OnTemplateModelChanged;

        designer.ScrollOffsetY = 0;

        if (designer._templateModel != null)
            designer.Width = designer._templateModel.Width;

        designer.InvalidateDesigner();
    }

    private ScreenTemplate? _templateModel;

    static TemplateDesigner()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(TemplateDesigner),
            new FrameworkPropertyMetadata(typeof(TemplateDesigner))
        );
    }

    public TemplateDesigner()
    {
        DesignerKind = DesignerKind.Template;

        AllowBandInteraction = true;
    }

    private void OnTemplateModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_templateModel == null)
            return;

        if (e.PropertyName == nameof(ScreenTemplate.Width))
        {
            Width = _templateModel.Width;
            InvalidateDesigner();
        }
    }

    internal void SyncBandToDesignerSize()
    {
        var band = GetAllBands()?.FirstOrDefault();

        if (band?.ActivePage == null)
            return;

        var h = (float)Height;
        var w = (float)Width;

        band.Width = w;
        band.Height = h;
        band.ActivePage.Height = h;

        UpdateActivePageWorldBounds(band);
        InvalidateDesigner();
    }
}
