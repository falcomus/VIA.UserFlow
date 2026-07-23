// ======================================================================================
// FILE: Mockup.Controls/RadioGroupHor.cs
//
// PURPOSE:
// - Horizontal radio group control for the mockup designer.
// - Renders a horizontal list of radio options with one selected item.
// - Visual style aligned with RadioButton.cs.
// - Supports hover / pressed feedback in LiveMode.
// - Clicking an item in LiveMode selects it.
// - Supports optional title.
//
// PROJECT: Mockup.Controls
// GROUP: Input
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.Messages;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FontWeight = System.Windows.FontWeight;
using RichTextAlignment = Topten.RichTextKit.TextAlignment;

namespace Mockup.Controls;

#region ### RADIO GROUP HOR ###

[ControlType(displayName: "Radio Group – Horizontal", group: "Selection")]
public partial class RadioGroupHor : DesignControl
{
    #region ### CONTENT ###

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Items")]
    private ObservableCollection<string> items = ["Item 1", "Item 2", "Item 3"];

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Selected Index")]
    private int selectedIndex = 0;

    partial void OnSelectedIndexChanged(int value)
    {
        if (Items == null || Items.Count == 0)
        {
            selectedIndex = -1;
            return;
        }

        selectedIndex = Math.Clamp(value, 0, Items.Count - 1);
    }

    #endregion

    #region ### APPEARANCE ###

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Background")]
    private bool showBackground = false;

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
    [property: System.ComponentModel.Category("Behaviour")]
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

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Corner Radius")]
    private float cornerRadius = 6f;

    partial void OnCornerRadiusChanged(float value)
    {
        cornerRadius = Math.Clamp(value, 0f, 30f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Border Width")]
    private float borderWidth = 1f;

    partial void OnBorderWidthChanged(float value)
    {
        borderWidth = Math.Clamp(value, 0f, 8f);
    }

    #endregion

    #region ### TYPOGRAPHY ###

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Size")]
    private float fontSize = 13.5f;

    partial void OnFontSizeChanged(float value)
    {
        fontSize = Math.Clamp(value, 9f, 24f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Weight")]
    private FontWeight fontWeight = FontWeights.Normal;

    #endregion

    #region ### LAYOUT ###

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Padding")]
    private Thickness padding = new(8, 2, 8, 2);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Item Height")]
    private float itemHeight = 24f;

    partial void OnItemHeightChanged(float value)
    {
        itemHeight = Math.Clamp(value, 16f, 80f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Item Spacing")]
    private float itemSpacing = 12f;

    partial void OnItemSpacingChanged(float value)
    {
        itemSpacing = Math.Clamp(value, 0f, 60f);
    }

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
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Right To Left")]
    private bool rightToLeft = false;

    #endregion

    #region ### RUNTIME STATE ###

    [JsonIgnore, Browsable(false)]
    private bool _isHovered;

    [JsonIgnore, Browsable(false)]
    private int _hoveredIndex = -1;

    [JsonIgnore, Browsable(false)]
    private int _pressedIndex = -1;

    [JsonIgnore, Browsable(false)]
    private readonly List<SKRect> _itemRects = [];

    [JsonIgnore, Browsable(false)]
    private readonly List<SKRect> _circleRects = [];

    #endregion

    #region ### CTOR ###

    public RadioGroupHor()
    {
        IsActionControl = true;

        Name = "RadioGroupHor";
        ResizeStyle = ResizeStyles.WidthOnly;

        ExplicitePreviewWidth = 260;
        ExplicitePreviewHeight = 32;

        Width = 250f;
        Height = 30f;

        MinWidth = 120f;
        MinHeight = 30f;

        MaxWidth = 900f;
        MaxHeight = 30f;
    }

    public override string ToString() => string.Empty;

    #endregion

    #region ### POINTER HOOKS ###

    public override void OnPointerDown(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;

        if (!TryHitItem(ctx.WorldPoint, out int index))
            return;

        _isHovered = true;
        _hoveredIndex = index;
        _pressedIndex = index;

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
        int hitIndex = -1;

        if (isInside)
            TryHitItem(ctx.WorldPoint, out hitIndex);

        bool changed = false;

        if (_isHovered != isInside)
        {
            _isHovered = isInside;
            changed = true;
        }

        if (_hoveredIndex != hitIndex)
        {
            _hoveredIndex = hitIndex;
            changed = true;
        }

        if (_pressedIndex >= 0 && hitIndex != _pressedIndex)
        {
            _pressedIndex = -1;
            changed = true;
        }

        Mouse.OverrideCursor = isInside ? Cursors.Hand : null;

        if (changed)
            InvalidateVisuals();
    }

    public override void OnPointerUp(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;

        bool isInside = TryHitItem(ctx.WorldPoint, out int hitIndex);
        bool commitClick = _pressedIndex >= 0 && isInside && hitIndex == _pressedIndex;

        _pressedIndex = -1;
        _isHovered = isInside;
        _hoveredIndex = isInside ? hitIndex : -1;

        if (commitClick && hitIndex >= 0 && hitIndex < Items.Count)
            SelectedIndex = hitIndex;

        InvalidateVisuals();
    }

    public override void OnPointerLeave()
    {
        ResetInteractionState();
    }

    #endregion

    #region ### RENDER ###

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        DrawBackground(canvas, layout);

        _itemRects.Clear();
        _circleRects.Clear();

        if (Items == null || Items.Count == 0)
            return;

        DrawItems(canvas, layout, ctx);
    }

    #endregion

    #region ### DRAW ###

    private void DrawBackground(SKCanvas canvas, SKRect layout)
    {
        if (!ShowBackground && BorderWidth <= 0f)
            return;

        float radius = Math.Clamp(CornerRadius, 0f, Math.Min(layout.Width, layout.Height) / 2f);

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: layout,
            cornerRadius: radius,
            fillStyle: FillStyle.Solid,
            fillColor: ShowBackground ? BackgroundColor : Colors.Transparent,
            borderStyle: BorderStyle.Solid,
            borderColor: BorderColor,
            borderWidth: BorderWidth
        );
    }

    private void DrawItems(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        float left = layout.Left + (float)Padding.Left;
        float right = layout.Right - (float)Padding.Right;
        float top = layout.Top + (float)Padding.Top;
        float bottom = layout.Bottom - (float)Padding.Bottom;

        float y = top;

        float rowTop = y;
        float rowBottom = Math.Min(bottom, rowTop + ItemHeight);
        if (rowBottom <= rowTop)
            return;

        float x = RightToLeft ? right : left;
        const float textSlack = 10f;

        for (int i = 0; i < Items.Count; i++)
        {
            string text = Items[i] ?? string.Empty;

            float textWidth = MeasureTextWidth(text, FontSize, FontWeight);
            float resolvedCircleSize = Math.Clamp(CircleSize, 12f, Math.Min(28f, rowBottom - rowTop));
            float itemWidth = resolvedCircleSize + TextOffset + textWidth + textSlack;

            SKRect itemRect;
            SKRect circleRect;

            if (RightToLeft)
            {
                itemRect = new SKRect(x - itemWidth, rowTop, x, rowBottom);
                float circleTop = rowTop + (rowBottom - rowTop - resolvedCircleSize) / 2f;
                circleRect = new SKRect(
                    itemRect.Right - resolvedCircleSize,
                    circleTop,
                    itemRect.Right,
                    circleTop + resolvedCircleSize
                );
                x = itemRect.Left + 0f - ItemSpacing;
            }
            else
            {
                itemRect = new SKRect(x, rowTop, x + itemWidth, rowBottom);
                float circleTop = rowTop + (rowBottom - rowTop - resolvedCircleSize) / 2f;
                circleRect = new SKRect(
                    itemRect.Left,
                    circleTop,
                    itemRect.Left + resolvedCircleSize,
                    circleTop + resolvedCircleSize
                );
                x = itemRect.Right + ItemSpacing;
            }

            if (itemRect.Right < left || itemRect.Left > right)
                continue;

            _itemRects.Add(itemRect);
            _circleRects.Add(circleRect);

            bool isSelected = i == SelectedIndex;
            bool isHovered = ctx.LiveMode && i == _hoveredIndex;
            bool isPressed = ctx.LiveMode && i == _pressedIndex;

            DrawCircle(canvas, circleRect, isSelected, isHovered, isPressed);
            DrawText(canvas, itemRect, circleRect, text);
        }
    }

    private void DrawCircle(SKCanvas canvas, SKRect circleRect, bool isSelected, bool isHovered, bool isPressed)
    {
        var (fillColor, resolvedBorderColor) = GetCircleVisualColors(isSelected, isHovered, isPressed);

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: circleRect,
            cornerRadius: circleRect.Width / 2f,
            fillStyle: FillStyle.Solid,
            fillColor: fillColor,
            borderColor: resolvedBorderColor,
            borderStyle: BorderStyle.Solid,
            shadowOptions: Elevation > 0 ? GetElevation(Elevation) : ShadowOptions.Default,
            borderWidth: 0.9f
        );

        if (isSelected)
            DrawDot(canvas, circleRect, DotColor);
    }

    private void DrawText(SKCanvas canvas, SKRect itemRect, SKRect circleRect, string text)
    {
        SKRect textRect = RightToLeft
            ? new SKRect(itemRect.Left, itemRect.Top + 1f, circleRect.Left - TextOffset + 6f, itemRect.Bottom)
            : new SKRect(circleRect.Right + TextOffset, itemRect.Top + 1f, itemRect.Right + 6f, itemRect.Bottom);

        var alignment = RightToLeft ? RichTextAlignment.Right : RichTextAlignment.Left;

        TextRenderer.Draw(
            canvas: canvas,
            text: text,
            bounds: textRect,
            fontSize: FontSize,
            color: TextColor.ToSKColor(),
            fontFamily: Theme.FontFamily,
            fontWeight: FontWeight,
            textAlignment: alignment
        );
    }

    #endregion

    #region ### HELPERS ###

    private (Color FillColor, Color BorderColor) GetCircleVisualColors(bool isSelected, bool isHovered, bool isPressed)
    {
        if (isSelected)
        {
            Color fill = Variant == ControlVariant.CUSTOM
                ? CheckedBackgroundColor
                : GetFillColor(Variant, CheckedBackgroundColor);

            Color border = Variant == ControlVariant.CUSTOM
                ? fill.Darken(0.10f)
                : GetBorderColor(Variant, CheckedBackgroundColor).Darken(0.08f);

            if (isHovered)
            {
                fill = fill.Lighten(0.04f);
                border = border.Darken(0.02f);
            }

            if (isPressed)
            {
                fill = fill.Darken(0.06f);
                border = border.Darken(0.05f);
            }

            return (fill, border);
        }

        Color unselectedFill = OuterCircleBackgroundColor;
        Color unselectedBorder = BorderColor;

        if (isHovered)
        {
            unselectedFill = unselectedFill.Darken(0.02f);
            unselectedBorder = unselectedBorder.Darken(0.04f);
        }

        if (isPressed)
        {
            unselectedFill = unselectedFill.Darken(0.04f);
            unselectedBorder = unselectedBorder.Darken(0.08f);
        }

        return (unselectedFill, unselectedBorder);
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

    private bool TryHitItem(SKPoint point, out int index)
    {
        for (int i = 0; i < _itemRects.Count; i++)
        {
            if (_itemRects[i].Contains(point))
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    private static float MeasureTextWidth(string text, float fontSize, FontWeight fontWeight)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0f;

        using var paint = new SKPaint
        {
            IsAntialias = true,
            TextSize = fontSize,
            Typeface = fontWeight >= FontWeights.SemiBold
                ? SKTypeface.FromFamilyName(Theme.FontFamily, SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                : SKTypeface.FromFamilyName(Theme.FontFamily, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };

        return paint.MeasureText(text);
    }

    private void ResetInteractionState()
    {
        bool changed = false;

        if (_isHovered)
        {
            _isHovered = false;
            changed = true;
        }

        if (_hoveredIndex != -1)
        {
            _hoveredIndex = -1;
            changed = true;
        }

        if (_pressedIndex != -1)
        {
            _pressedIndex = -1;
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


