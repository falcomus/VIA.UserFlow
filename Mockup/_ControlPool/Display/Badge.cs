// ======================================================================================
// FILE: Mockup.Controls/Badge.cs
//
// PURPOSE:
// - Lightweight badge / status pill control for the mockup designer.
// - Supports text, variant colors, custom colors and rounded pill appearance.
// - Intended for tags like "New", "Active", "Warning", "Draft", "Success" etc.
//
// PROJECT: Mockup.Controls
// GROUP: Data Visualization
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using System.Windows;
using System.Windows.Media;

namespace Mockup.Controls;

#region === BADGE ===

[ControlType(displayName: "Badge", group: "Indicators")]
public partial class Badge : DesignControl
{
    #region === CONTENT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Text")]
    private string text = "Badge";

    #endregion

    #region === APPEARANCE ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Variant")]
    private ControlVariant variant = ControlVariant.Primary;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("BackgroundColor")]
    private Color backgroundColor = Theme.Primary;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("BorderColor")]
    private Color borderColor = Theme.Primary.Darken(0.18f);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("TextColor")]
    private Color textColor = Colors.White;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("CornerRadius")]
    private float cornerRadius = 999f;

    partial void OnCornerRadiusChanged(float value)
    {
        cornerRadius = Math.Clamp(value, 0f, 999f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("BorderWidth")]
    private float borderWidth = 1f;

    partial void OnBorderWidthChanged(float value)
    {
        borderWidth = Math.Clamp(value, 0f, 8f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Filled")]
    private bool filled = true;

    #endregion

    #region === TYPOGRAPHY ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("FontSize")]
    private float fontSize = 11f;

    partial void OnFontSizeChanged(float value)
    {
        fontSize = Math.Clamp(value, 6f, 40f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("FontWeight")]
    private FontWeight fontWeight = FontWeights.SemiBold;

    #endregion

    #region === LAYOUT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Padding")]
    private Thickness padding = new(10, 2, 10, 2);

    #endregion

    #region === CTOR ===

    public Badge()
    {
        Name = "Badge";
        ResizeStyle = ResizeStyles.ResizeAll;

        ExplicitePreviewHeight = 40f;
        ExplicitePreviewWidth = 40f;

        Width = 20f;
        Height = 20f;

        MinWidth = 20f;
        MinHeight = 20f;

        MaxWidth = 400f;
        MaxHeight = 400f;

        Text = "B";
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === RENDER ===

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        var fill = GetBadgeFillColor();
        var border = GetBadgeBorderColor();
        var text = GetBadgeTextColor();

        var radius = Math.Clamp(CornerRadius, 0f, Math.Min(layout.Width, layout.Height) / 2f);

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: layout,
            cornerRadius: radius,
            fillStyle: FillStyle.Solid,
            fillColor: Filled ? fill : Colors.Transparent,
            borderStyle: BorderStyle.Solid,
            borderColor: border,
            borderWidth: BorderWidth);

        layout.Top += 1;

        TextRenderer.Draw2(
            canvas: canvas,
            text: Text,
            bounds: layout,
            fontSize: FontSize,
            color: text,
            padding: Padding,
            fontWeight: FontWeight,
            textAlignment: TextAlignment.Center);
    }

    #endregion

    #region === HELPERS ===

    private Color GetBadgeFillColor()
    {
        if (Variant == ControlVariant.CUSTOM)
            return BackgroundColor;

        return GetFillColor(Variant, BackgroundColor);
    }

    private Color GetBadgeBorderColor()
    {
        if (Variant == ControlVariant.CUSTOM)
            return BorderColor;

        return GetBorderColor(Variant, BorderColor).Darken(0.15f);
    }

    private Color GetBadgeTextColor()
    {
        if (Filled)
            return GetTextColor(Variant, TextColor);

        if (Variant == ControlVariant.CUSTOM)
            return TextColor;

        return GetBorderColor(Variant, BorderColor);
    }

    #endregion
}

#endregion