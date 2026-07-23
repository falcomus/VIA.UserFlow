// ======================================================================================
// FILE: Mockup.Controls/CheckBox.cs
//
// PURPOSE:
// - Modern CheckBox control for the mockup designer.
// - Visual style aligned with Button / TextBox / ComboBox controls.
// - Supports checked / unchecked state, hover in LiveMode and optional right-to-left layout.
// - Uses an explicit box rect for rendering and interaction.
//
// NOTES:
// - This is a visual mockup control, not a native WPF checkbox.
// - The control toggles only in LiveMode on left mouse click.
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
using FontWeight = System.Windows.FontWeight;
using RichTextAlignment = Topten.RichTextKit.TextAlignment;

namespace Mockup.Controls;

#region === CHECK BOX ===

[ControlType("checkbox", displayName: "Check Box", group: "Selection")]
public partial class CheckBox : DesignControl
{
    #region === CONTENT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Text")]
    private string text = "CheckBox";

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
    [property: System.ComponentModel.DisplayName("Text Color")]
    private Color textColor = Theme.Text;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Box Background")]
    private Color boxBackgroundColor = Colors.White;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Border Color")]
    private Color borderColor = Theme.ControlBorder;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Check Color")]
    private Color checkColor = Colors.White;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Checked Background")]
    private Color checkedBackgroundColor = SkiaRenderer.SelectionColor;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Corner Radius")]
    private float cornerRadius = 3f;

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

    #region === TYPOGRAPHY ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Size")]
    private float fontSize = 13.5f;

    partial void OnFontSizeChanged(float value)
    {
        fontSize = Math.Clamp(value, 9f, 24f);
        RecalculateHeight();
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Weight")]
    private FontWeight fontWeight = FontWeights.Normal;

    #endregion

    #region === LAYOUT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Text Offset")]
    private float textOffset = 8f;

    partial void OnTextOffsetChanged(float value)
    {
        textOffset = Math.Clamp(value, 0f, 24f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Box Size")]
    private float boxSize = 16f;

    partial void OnBoxSizeChanged(float value)
    {
        boxSize = Math.Clamp(value, 12f, 28f);
        RecalculateHeight();
    }

    #endregion

    #region === BEHAVIOR ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Is Checked")]
    private bool isChecked = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Right To Left")]
    private bool rightToLeft = false;

    #endregion

    #region === RUNTIME STATE ===

    [JsonIgnore, Browsable(false)]
    private bool _isHovered;

    [JsonIgnore, Browsable(false)]
    private bool _isPressed;

    [JsonIgnore, Browsable(false)]
    private SKRect _boxRect;

    #endregion

    #region === CTOR ===

    public CheckBox()
    {
        IsActionControl = true;

        Name = "CheckBox";

        Text = "CheckBox";

        ResizeStyle = ResizeStyles.WidthOnly;

        Width = 130f;
        Height = 25f;

        MinWidth = 40f;
        MinHeight = 25f;

        MaxWidth = 500f;
        MaxHeight = 40f;

        RecalculateHeight();
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === MOUSEEVENT HOOKS ===

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

    public override void OnPointerUp(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;

        bool isInside = VisualRect.Contains(ctx.WorldPoint);
        bool commitClick = _isPressed && isInside;

        _isPressed = false;
        _isHovered = isInside;

        if (commitClick)
            IsChecked = !IsChecked;

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

        if (!isInside && _isPressed)
        {
            _isPressed = false;
            InvalidateVisuals();
        }

        if (isInside)
        {
            Mouse.OverrideCursor = Cursors.Hand;
        }
    }

    public override void OnPointerLeave()
    {
        ResetInteractionState();
    }

    #endregion

    #region === RENDER ===

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        float resolvedBoxSize = Math.Clamp(BoxSize, 12f, Math.Min(28f, layout.Height));
        float boxTop = layout.Top + (layout.Height - resolvedBoxSize) / 2f;

        _boxRect = RightToLeft
            ? new SKRect(
                layout.Right - resolvedBoxSize,
                boxTop,
                layout.Right,
                boxTop + resolvedBoxSize
            )
            : new SKRect(
                layout.Left,
                boxTop,
                layout.Left + resolvedBoxSize,
                boxTop + resolvedBoxSize
            );

        DrawBox(canvas, ctx);
        DrawText(canvas, layout);
    }

    #endregion

    #region === DRAW HELPERS ===

    private void DrawBox(SKCanvas canvas, RenderContext ctx)
    {
        var (fillColor, resolvedBorderColor) = GetBoxVisualColors(ctx);

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: _boxRect,
            cornerRadius: Math.Clamp(CornerRadius, 0f, 8f),
            fillStyle: FillStyle.Solid,
            fillColor: fillColor,
            borderColor: resolvedBorderColor,
            borderStyle: BorderStyle.Solid,
            shadowOptions: Elevation > 0 ? GetElevation(Elevation) : ShadowOptions.Default,
            borderWidth: 0.9f
        );

        if (IsChecked)
        {
            DrawCheckMark(canvas, _boxRect, CheckColor);
        }
    }

    private void DrawText(SKCanvas canvas, SKRect layout)
    {
        SKRect textRect = RightToLeft
            ? new SKRect(
                layout.Left,
                layout.Top + 1,
                _boxRect.Left - TextOffset,
                layout.Bottom
            )
            : new SKRect(
                _boxRect.Right + TextOffset,
                layout.Top + 1,
                layout.Right,
                layout.Bottom
            );

        var alignment = RightToLeft ? RichTextAlignment.Right : RichTextAlignment.Left;

        TextRenderer.Draw(
            canvas: canvas,
            text: Text,
            bounds: textRect,
            fontSize: FontSize,
            color: TextColor.ToSKColor(),
            fontWeight: FontWeight,
            fontFamily: Theme.FontFamily,
            textAlignment: alignment
        );
    }

    private (Color FillColor, Color BorderColor) GetBoxVisualColors(RenderContext ctx)
    {
        if (IsChecked)
        {
            Color fill = Variant == ControlVariant.CUSTOM
                ? CheckedBackgroundColor
                : GetFillColor(Variant, CheckedBackgroundColor);

            Color border = Variant == ControlVariant.CUSTOM
                ? fill.Darken(0.10f)
                : GetBorderColor(Variant, CheckedBackgroundColor).Darken(0.08f);

            if (ctx.LiveMode && _isHovered)
            {
                fill = fill.Lighten(0.04f);
                border = border.Darken(0.02f);
            }

            if (ctx.LiveMode && _isPressed)
            {
                fill = fill.Darken(0.06f);
                border = border.Darken(0.05f);
            }

            return (fill, border);
        }
        else
        {
            Color fill = BoxBackgroundColor;
            Color border = BorderColor;

            if (ctx.LiveMode && _isHovered)
            {
                fill = fill.Darken(0.02f);
                border = border.Darken(0.04f);
            }

            if (ctx.LiveMode && _isPressed)
            {
                fill = fill.Darken(0.04f);
                border = border.Darken(0.08f);
            }

            return (fill, border);
        }
    }

    private static void DrawCheckMark(SKCanvas canvas, SKRect boxRect, Color color)
    {
        using var paint = new SKPaint
        {
            Color = color.ToSKColor(),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

        var rect = boxRect;
        rect.Inflate(-1.5f, -1.5f);

        using var path = new SKPath();
        path.MoveTo(
            rect.Left + rect.Width * 0.22f,
            rect.Top + rect.Height * 0.52f
        );
        path.LineTo(
            rect.Left + rect.Width * 0.43f,
            rect.Top + rect.Height * 0.74f
        );
        path.LineTo(
            rect.Left + rect.Width * 0.78f,
            rect.Top + rect.Height * 0.30f
        );

        canvas.DrawPath(path, paint);
    }

    private void RecalculateHeight()
    {
        float desiredHeight = Math.Max(
            30f,
            Math.Max(FontSize + 8f, BoxSize + 4f)
        );

        desiredHeight = Math.Clamp(desiredHeight, MinHeight, MaxHeight);

        Height = desiredHeight;
        MinHeight = desiredHeight;
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