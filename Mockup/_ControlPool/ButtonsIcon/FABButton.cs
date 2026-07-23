// ======================================================================================
// FILE: Mockup.Controls/FABButton.cs
//
// PURPOSE:
// - Modern floating action button for the mockup designer.
// - Visual style aligned with the updated light-mode control library.
// - Supports hover / pressed feedback in LiveMode.
// - Renders a circular action button with a centered plus glyph.
//
// PROJECT: Mockup.Controls
// GROUP: Buttons [Misc]
//
// NOTES:
// - This is a visual mockup control, not a native WPF button.
// - The control itself handles only visual pressed/hover state.
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.Messages;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Mockup.Controls;

#region === FAB BUTTON ===

[ControlType(displayName: "Floating Action Button", group: "Icon Buttons")]
public partial class FABButton : DesignControl
{
    #region === CONTENT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Text")]
    private string text = "+";

    #endregion

    #region === APPEARANCE ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Variant")]
    private ControlVariant variant = ControlVariant.Accent;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Background Color")]
    private Color backgroundColor = Theme.Accent;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Border Color")]
    private Color borderColor = Theme.Accent;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Text Color")]
    private Color textColor = Colors.White;

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
    [property: System.ComponentModel.DisplayName("Font Size")]
    private double fontSize = 22d;

    partial void OnFontSizeChanged(double value)
    {
        fontSize = Math.Clamp(value, 10d, 40d);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Weight")]
    private FontWeight fontWeight = FontWeights.Medium;

    #endregion

    #region === RUNTIME STATE ===

    [JsonIgnore, Browsable(false)]
    private bool _isHovered;

    [JsonIgnore, Browsable(false)]
    private bool _isPressed;

    #endregion

    #region === CTOR ===

    public FABButton()
    {
        IsActionControl = true;

        Name = "FABButton";
        ResizeStyle = ResizeStyles.KeepRatio;

        ExplicitePreviewHeight = 50f;
        ExplicitePreviewWidth = 50f;

        Width = 30f;
        Height = 30f;

        MinWidth = 25f;
        MinHeight = 25f;

        MaxWidth = 50f;
        MaxHeight = 50f;
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === POINTER HOOKS ===

    public override void OnPointerDown(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;

        if (!VisualRect.Contains(ctx.WorldPoint))
            return;

        _isPressed = true;
        _isHovered = true;
        InvalidateVisuals();
    }

    public override void OnPointerMove(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode)
        {
            ResetInteractionState();
            return;
        }

        bool isInside = VisualRect.Contains(ctx.WorldPoint);

        if (_isHovered != isInside)
        {
            _isHovered = isInside;
            InvalidateVisuals();
        }

        if (isInside)
            Mouse.OverrideCursor = Cursors.Hand;

        if (!isInside && _isPressed)
        {
            _isPressed = false;
            InvalidateVisuals();
        }
    }

    public override void OnPointerUp(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;

        bool isInside = VisualRect.Contains(ctx.WorldPoint);

        _isPressed = false;
        _isHovered = isInside;
        InvalidateVisuals();
    }

    public override void OnPointerLeave()
    {
        ResetInteractionState();
    }

    #endregion

    #region === RENDER ===

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        var (fillColor, resolvedBorderColor, resolvedTextColor) = GetVisualColors(ctx);
        var shadow = GetVisualShadow(ctx);

        float radius = Math.Min(layout.Width, layout.Height) / 2f;
        var center = new SKPoint(layout.MidX, layout.MidY);

        using var fillPaint = new SKPaint
        {
            Color = fillColor.ToSKColor(),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            ImageFilter = Elevation <= 0
                ? null
                : SKImageFilter.CreateDropShadow(
                    shadow.Dx,
                    shadow.Dy,
                    shadow.Sigma,
                    shadow.Sigma,
                    shadow.Color)
        };

        canvas.DrawCircle(center, radius, fillPaint);

        using var borderPaint = new SKPaint
        {
            Color = resolvedBorderColor.ToSKColor(),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 0.8f
        };

        canvas.DrawCircle(center, radius - 0.4f, borderPaint);

        var textRect = layout;
        textRect.Offset(0f, -1f);

        TextRenderer.Draw(
            canvas: canvas,
            text: Text,
            bounds: textRect,
            fontSize: (float)FontSize,
            color: resolvedTextColor.ToSKColor(),
            padding: 0,
            fontWeight: FontWeight
        );
    }

    #endregion

    #region === HELPERS ===

    private (Color FillColor, Color BorderColor, Color TextColor) GetVisualColors(RenderContext ctx)
    {
        Color fillColor = Variant == ControlVariant.CUSTOM
            ? BackgroundColor
            : GetFillColor(Variant, BackgroundColor);

        Color resolvedBorderColor = Variant == ControlVariant.CUSTOM
            ? BorderColor.Darken(0.10f)
            : GetBorderColor(Variant, BorderColor).Darken(0.10f);

        Color resolvedTextColor = TextColor;

        if (ctx.LiveMode && _isHovered)
        {
            fillColor = fillColor.Lighten(0.04f);
            resolvedBorderColor = resolvedBorderColor.Darken(0.02f);
        }

        if (ctx.LiveMode && _isPressed)
        {
            fillColor = fillColor.Darken(0.06f);
            resolvedBorderColor = resolvedBorderColor.Darken(0.05f);
        }

        return (fillColor, resolvedBorderColor, resolvedTextColor);
    }

    private ShadowOptions GetVisualShadow(RenderContext ctx)
    {
        int safeElevation = Math.Clamp(Elevation, 0, 5);

        if (safeElevation <= 0)
            return ShadowOptions.Default;

        if (ctx.LiveMode && _isPressed)
            return GetElevation(Math.Max(0, safeElevation - 1));

        return GetElevation(safeElevation);
    }

    private void ResetInteractionState()
    {
        bool changed = false;

        if (_isHovered)
        {
            _isHovered = false;
            changed = true;
        }

        if (_isPressed)
        {
            _isPressed = false;
            changed = true;
        }

        if (changed)
            InvalidateVisuals();

        Mouse.OverrideCursor = null;
    }

    private void InvalidateVisuals()
    {
        MSG.UI.InvalidateDesigner();
    }

    #endregion
}

#endregion
