// ======================================================================================
// FILE: Mockup.Controls/SeparatorHor.cs
//
// PURPOSE:
// - Simple horizontal separator for the mockup designer.
// - Supports color, thickness and horizontal inset.
//
// PROJECT: Mockup.Controls
// GROUP: Miscellaneous
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Windows.Media;

namespace Mockup.Controls;

#region === SEPARATOR HORIZONTAL ===

[ControlType(displayName: "Separator – Horizontal", group: "Layout")]
public partial class SeparatorHor : DesignControl
{
    #region === APPEARANCE ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Color")]
    private Color color = Theme.ControlBorder;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Thickness")]
    private float thickness = 1f;

    partial void OnThicknessChanged(float value)
    {
        thickness = Math.Clamp(value, 0.5f, 12f);
    }

    #endregion

    #region === LAYOUT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Inset")]
    private float inset = 0f;

    partial void OnInsetChanged(float value)
    {
        inset = Math.Clamp(value, 0f, 200f);
    }

    #endregion

    #region === CTOR ===

    public SeparatorHor()
    {
        Name = "Hor Separator";

        ResizeStyle = ResizeStyles.WidthOnly;

        ExplicitePreviewWidth = 80f;
        ExplicitePreviewHeight = 30f;

        Width = 80f;
        Height = 20f;

        MinWidth = 20f;
        MinHeight = 20f;

        MaxWidth = 500f;
        MaxHeight = 20f;
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === RENDER ===

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        float safeInset = Math.Clamp(Inset, 0f, layout.Width / 2f - 1f);

        using SKPaint paint = new()
        {
            Color = Color.ToSKColor(),
            IsStroke = true,
            StrokeWidth = Thickness,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round
        };

        canvas.DrawLine(
            layout.Left + safeInset,
            layout.MidY,
            layout.Right - safeInset,
            layout.MidY,
            paint
        );
    }

    #endregion
}

#endregion
