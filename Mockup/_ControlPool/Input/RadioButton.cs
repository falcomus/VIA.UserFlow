// ======================================================================================
// FILE: Mockup.Controls/RadioButton.cs
//
// PURPOSE:
// - Modern RadioButton control for the mockup designer.
// - Visual style aligned with CheckBox / TextBox / ComboBox controls.
// - Supports checked / unchecked state, hover in LiveMode and optional right-to-left layout.
// - Supports Group Id for logical grouping in the model.
//
// NOTES:
// - This is a visual mockup control, not a native WPF RadioButton.
// - The control toggles to checked in LiveMode on left mouse click.
// - Group synchronization is prepared via Group Id, but global uncheck logic
//   depends on outer designer/model orchestration.
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

#region === RADIO BUTTON ===

[ControlType("radiobutton", displayName: "Radio Button", group: "Selection")]
public partial class RadioButton : DesignControl
{
    #region === CONTENT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Text")]
    private string text = "RadioButton";

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
    [property: System.ComponentModel.DisplayName("Outer Circle Background")]
    private Color outerCircleBackgroundColor = Colors.White;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Border Color")]
    private Color borderColor = Theme.ControlBorder;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Dot Color")]
    private Color dotColor = Colors.White;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Checked Background")]
    private Color checkedBackgroundColor = SkiaRenderer.SelectionColor;

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
    [property: System.ComponentModel.DisplayName("Circle Size")]
    private float circleSize = 16f;

    partial void OnCircleSizeChanged(float value)
    {
        circleSize = Math.Clamp(value, 12f, 28f);
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
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Group Id")]
    private string groupId = string.Empty;

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
    private SKRect _circleRect;

    #endregion

    #region === CTOR ===

    public RadioButton()
    {
        IsActionControl = true;

        Name = "RadioButton";
        ResizeStyle = ResizeStyles.WidthOnly;

        Width = 130f;
        Height = 25f;

        MinWidth = 40f;
        MinHeight = 20f;

        MaxWidth = 500f;
        MaxHeight = 50f;

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
        {
            if (!IsChecked)
                IsChecked = true;
        }

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
        float resolvedCircleSize = Math.Clamp(CircleSize, 12f, Math.Min(28f, layout.Height));
        float circleTop = layout.Top + (layout.Height - resolvedCircleSize) / 2f;

        _circleRect = RightToLeft
            ? new SKRect(
                layout.Right - resolvedCircleSize,
                circleTop,
                layout.Right,
                circleTop + resolvedCircleSize
            )
            : new SKRect(
                layout.Left,
                circleTop,
                layout.Left + resolvedCircleSize,
                circleTop + resolvedCircleSize
            );

        DrawCircle(canvas, ctx);
        DrawText(canvas, layout);
    }

    #endregion

    #region === DRAW HELPERS ===

    private void DrawCircle(SKCanvas canvas, RenderContext ctx)
    {
        var (fillColor, resolvedBorderColor) = GetCircleVisualColors(ctx);

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: _circleRect,
            cornerRadius: _circleRect.Width / 2f,
            fillStyle: FillStyle.Solid,
            fillColor: fillColor,
            borderColor: resolvedBorderColor,
            borderStyle: BorderStyle.Solid,
            shadowOptions: Elevation > 0 ? GetElevation(Elevation) : ShadowOptions.Default,
            borderWidth: 0.9f
        );

        if (IsChecked)
        {
            DrawDot(canvas, _circleRect, DotColor);
        }
    }

    private void DrawText(SKCanvas canvas, SKRect layout)
    {
        SKRect textRect = RightToLeft
            ? new SKRect(
                layout.Left,
                layout.Top + 1,
                _circleRect.Left - TextOffset,
                layout.Bottom
            )
            : new SKRect(
                _circleRect.Right + TextOffset,
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
            fontFamily: Theme.FontFamily,
            textAlignment: alignment
        );
    }

    private (Color FillColor, Color BorderColor) GetCircleVisualColors(RenderContext ctx)
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
            Color fill = OuterCircleBackgroundColor;
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

    private static void DrawDot(SKCanvas canvas, SKRect circleRect, Color color)
    {
        using var paint = new SKPaint
        {
            Color = color.ToSKColor(),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        float radius = Math.Min(circleRect.Width, circleRect.Height) * 0.22f;
        canvas.DrawCircle(circleRect.MidX, circleRect.MidY, radius, paint);
    }

    private void RecalculateHeight()
    {
        float desiredHeight = Math.Max(30, FontSize * 1.7f);

        desiredHeight = Math.Clamp(desiredHeight, MinHeight, MaxHeight);

        Height = desiredHeight;
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
