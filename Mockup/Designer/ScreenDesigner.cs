// ======================================================================================
// FILE: Mockup.Designer/ScreenDesigner.cs
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;
using System.Windows;

namespace Mockup.Designer;

[ObservableObject]
public partial class ScreenDesigner : BaseDesigner
{
    #region === DESIGNER WORLD BOUNDS ===

    protected override SKRect GetDesignerWorldBounds()
    {
        //XXXreturn new SKRect(0, 0, Screen?.Width ?? 0, Screen?.DesignHeight ?? 0);
        return new SKRect(0, 0, Screen?.Width ?? 0, Screen?.ScreenHeight ?? 0);
    }

    #endregion === DESIGNER  WORLD BOUNDS ===

    #region ==== SHOWHEADER / SHOWFOOTER / BAND ACCESS ===

    [ObservableProperty]
    private bool _showHeader;

    [ObservableProperty]
    public bool _showFooter;

    internal override IEnumerable<Band> GetAllBands() => Screen?.Bands ?? Enumerable.Empty<Band>();

    protected override IEnumerable<Band>? GetCustomBands() => Screen?.CustomBands ?? [];

    protected override Band? GetHeaderBand() =>
        Screen != null && Screen.ShowHeader ? Screen?.HeaderBand : null;

    protected override Band? GetFooterBand() =>
        Screen != null && Screen.ShowFooter ? Screen?.FooterBand : null;

    #endregion ==== SHOWHEADER / SHOWFOOTER / BAND ACCESS ===


    static ScreenDesigner()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ScreenDesigner),
            new FrameworkPropertyMetadata(typeof(ScreenDesigner))
        );
    }

    public ScreenDesigner()
    {
        DesignerKind = DesignerKind.Screen;
    }

    protected override void OnScreenChanged(Screen? oldValue, Screen? newValue)
    {
        if (newValue == null)
            return;

        ShowHeader = newValue.ShowHeader;
        ShowFooter = newValue.ShowFooter;

        ScrollOffsetY = 0;
    }
}
