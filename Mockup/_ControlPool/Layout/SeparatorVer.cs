// ======================================================================================
// FILE: Mockup.Controls/SeparatorVer.cs
//
// PURPOSE:
// - Simple vertical separator for the mockup designer.
// - Supports color, thickness and vertical inset.
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

#region === SEPARATOR VERTICAL ===

[ControlType(displayName: "Separator – Vertical", group: "Layout")]
public partial class SeparatorVer : DesignControl
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

    public SeparatorVer()
    {
        Name = "Ver Separator";

        ResizeStyle = ResizeStyles.HeightOnly;

        ExplicitePreviewWidth = 30f;
        ExplicitePreviewHeight = 80f;

        Width = 20f;
        Height = 120f;

        MinWidth = 20f;
        MinHeight = 20f;

        MaxWidth = 20f;
        MaxHeight = 500f;
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === RENDER ===

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        float safeInset = Math.Clamp(Inset, 0f, layout.Height / 2f - 1f);

        using SKPaint paint = new()
        {
            Color = Color.ToSKColor(),
            IsStroke = true,
            StrokeWidth = Thickness,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round
        };

        canvas.DrawLine(
            layout.MidX,
            layout.Top + safeInset,
            layout.MidX,
            layout.Bottom - safeInset,
            paint
        );
    }

    #endregion
}

#endregion