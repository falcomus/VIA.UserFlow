// ======================================================================================
// FILE: Mockup.Controls/BorderControl.cs
//
// PURPOSE:
// - Simple border / panel surface for the mockup designer.
// - Visual style aligned with the updated light-mode control library.
// - Supports background, border, corner radius and elevation.
//
// PROJECT: Mockup.Controls
// GROUP: Miscellaneous
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using System.Windows.Media;

namespace Mockup.Controls;

#region === BORDER CONTROL ===

[ControlType(displayName: "Border", group: "Layout")]
public partial class BorderControl : DesignControl
{
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
    private int elevation = 0;

    partial void OnElevationChanged(int value)
    {
        elevation = Math.Clamp(value, 0, 5);
    }

    #endregion

    #region === CTOR ===

    public BorderControl()
    {
        Name = "Border";

        ResizeStyle = ResizeStyles.ResizeAll;

        ExplicitePreviewHeight = 100f;
        ExplicitePreviewWidth = 80f;

        Width = 120f;
        Height = 80f;

        MinWidth = 20f;
        MinHeight = 20f;

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

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: layout,
            fillColor: BackgroundColor,
            cornerRadius: Math.Clamp(CornerRadius, 0f, Math.Min(layout.Width, layout.Height) / 2f),
            fillStyle: FillStyle.Solid,
            borderStyle: BorderStyle.Solid,
            borderColor: BorderColor,
            borderWidth: BorderWidth,
            shadowOptions: shadowOptions,
            innerBorder: true
        );
    }

    #endregion
}

#endregion

