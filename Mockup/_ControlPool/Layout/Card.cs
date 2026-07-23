// ======================================================================================
// FILE: Mockup.Controls/Card.cs
//
// PURPOSE:
// - Simple card / panel surface for the mockup designer.
// - Visual style aligned with the updated light-mode control library.
// - Supports optional title, background, border, corner radius and elevation.
//
// PROJECT: Mockup.Controls
// GROUP: Miscellaneous
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using System.Windows;
using System.Windows.Media;

namespace Mockup.Controls;

#region === CARD CONTROL ===

[ControlType(displayName: "Card", group: "Layout")]
public partial class Card : DesignControl
{
    #region === CONTENT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Title")]
    private string title = "Title";

    #endregion

    #region === APPEARANCE ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Background Color")]
    private Color backgroundColor = Colors.White;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Border Color")]
    private Color borderColor = Theme.ControlBorder;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Title Color")]
    private Color titleColor = Theme.Text;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Corner Radius")]
    private float cornerRadius = 4f;

    partial void OnCornerRadiusChanged(float value)
    {
        cornerRadius = Math.Clamp(value, 0f, 60f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Border Width")]
    private float borderWidth = 0.8f;

    partial void OnBorderWidthChanged(float value)
    {
        borderWidth = Math.Clamp(value, 0f, 12f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Elevation")]
    private int elevation = 1;

    partial void OnElevationChanged(int value)
    {
        elevation = Math.Clamp(value, 0, 5);
    }

    #endregion

    #region === TYPOGRAPHY ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Title Font Size")]
    private double titleFontSize = 12d;

    partial void OnTitleFontSizeChanged(double value)
    {
        titleFontSize = Math.Clamp(value, 8d, 40d);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Title Font Weight")]
    private FontWeight titleFontWeight = FontWeights.SemiBold;

    #endregion

    #region === LAYOUT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Title")]
    private bool showTitle = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Header Height")]
    private float headerHeight = 30f;

    partial void OnHeaderHeightChanged(float value)
    {
        headerHeight = Math.Clamp(value, 18f, 120f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Title Padding")]
    private Thickness titlePadding = new(12, 0, 12, 0);

    #endregion

    #region === CTOR ===

    public Card()
    {
        Name = "Card";

        ResizeStyle = ResizeStyles.ResizeAll;

        ExplicitePreviewHeight = 100f;
        ExplicitePreviewWidth = 80f;

        Width = 120f;
        Height = 80f;

        MinWidth = 30f;
        MinHeight = 30f;

        MaxWidth = 5000f;
        MaxHeight = 5000f;
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === RENDER ===

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        ShadowOptions shadowOptions = ShadowOptions.Default;

        if (Elevation > 0)
        {
            shadowOptions = GetElevation(Elevation);
            shadowOptions.Dy -= 0.8f;
        }

        float safeCornerRadius = Math.Clamp(
            CornerRadius,
            0f,
            Math.Min(layout.Width, layout.Height) / 2f
        );

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: layout,
            fillColor: BackgroundColor,
            cornerRadius: safeCornerRadius,
            fillStyle: FillStyle.Solid,
            borderStyle: BorderStyle.Solid,
            borderColor: BorderColor.Lighten(0.1f),
            borderWidth: BorderWidth,
            shadowOptions: shadowOptions,
            // A white inner border would pre-compose translucent fills against white.
            // Keep the surface effect for opaque cards, but let alpha blend with the canvas.
            innerBorder: BackgroundColor.A == byte.MaxValue
        );

        if (!ShowTitle || string.IsNullOrWhiteSpace(Title))
            return;

        float safeHeaderHeight = Math.Clamp(HeaderHeight, 18f, Math.Max(18f, layout.Height));
        var titleRect = new SKRect(
            layout.Left + (float)TitlePadding.Left,
            layout.Top,
            layout.Right - (float)TitlePadding.Right,
            Math.Min(layout.Bottom, layout.Top + safeHeaderHeight)
        );

        TextRenderer.Draw2(
            canvas: canvas,
            text: Title,
            bounds: titleRect,
            fontSize: TitleFontSize,
            color: TitleColor,
            padding: new Thickness(0),
            fontWeight: TitleFontWeight,
            textAlignment: System.Windows.TextAlignment.Left
        );
    }

    #endregion
}

#endregion
