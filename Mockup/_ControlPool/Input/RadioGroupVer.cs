// ======================================================================================
// FILE: Mockup.Controls/RadioGroupVer.cs
//
// PURPOSE:
// - Vertical radio group control for the mockup designer.
// - Renders a list of radio options with one selected item.
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

#region ### RADIO GROUP VER ###

[ControlType(displayName: "Radio Group – Vertical", group: "Selection")]
public partial class RadioGroupVer : DesignControl
{
    #region ### CONTENT ###

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Title")]
    private string title = string.Empty;

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

    partial void OnTitleChanged(string value)
    {
        RecalculateHeight();
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

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Dividers")]
    private bool showDividers = false;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Divider Color")]
    private Color dividerColor = Theme.ControlBorder;

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
        RecalculateHeight();
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Weight")]
    private FontWeight fontWeight = FontWeights.Normal;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Title Font Size")]
    private float titleFontSize = 12f;

    partial void OnTitleFontSizeChanged(float value)
    {
        titleFontSize = Math.Clamp(value, 8f, 30f);
        RecalculateHeight();
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Title Font Weight")]
    private FontWeight titleFontWeight = FontWeights.Medium;

    #endregion

    #region ### LAYOUT ###

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Padding")]
    private Thickness padding = new(8, 8, 8, 8);

    partial void OnPaddingChanged(Thickness value)
    {
        RecalculateHeight();
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Item Height")]
    private float itemHeight = 24f;

    partial void OnItemHeightChanged(float value)
    {
        itemHeight = Math.Clamp(value, 16f, 80f);
        RecalculateHeight();
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Item Spacing")]
    private float itemSpacing = 4f;

    partial void OnItemSpacingChanged(float value)
    {
        itemSpacing = Math.Clamp(value, 0f, 40f);
        RecalculateHeight();
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
        RecalculateHeight();
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

    public RadioGroupVer()
    {
        IsActionControl = true;

        Name = "RadioGroupVer";
        ResizeStyle = ResizeStyles.ResizeAll;

        ExplicitePreviewWidth = 120;
        ExplicitePreviewHeight = 100;

        Width = 100f;
        Height = 90f;

        MinWidth = 90f;
        MinHeight = 50f;

        MaxWidth = 600f;
        MaxHeight = 800f;

        RecalculateHeight();
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

        if (!string.IsNullOrWhiteSpace(Title))
        {
            float titleHeight = GetTitleHeight();
            var titleRect = new SKRect(left, y, right, y + titleHeight);

            TextRenderer.Draw(
                canvas: canvas,
                text: Title,
                bounds: titleRect,
                fontSize: TitleFontSize,
                color: TextColor.ToSKColor(),
                fontFamily: Theme.FontFamily,
                fontWeight: TitleFontWeight,
                textAlignment: RightToLeft ? RichTextAlignment.Right : RichTextAlignment.Left
            );

            y += titleHeight + ItemSpacing;
        }

        for (int i = 0; i < Items.Count; i++)
        {
            if (y >= bottom)
                break;

            string text = Items[i] ?? string.Empty;
            float currentItemHeight = Math.Min(ItemHeight, bottom - y);
            if (currentItemHeight <= 1f)
                break;

            var itemRect = new SKRect(left, y, right, y + currentItemHeight);
            _itemRects.Add(itemRect);

            float resolvedCircleSize = Math.Clamp(CircleSize, 12f, Math.Min(28f, currentItemHeight));
            float circleTop = y + (currentItemHeight - resolvedCircleSize) / 2f;

            var circleRect = RightToLeft
                ? new SKRect(right - resolvedCircleSize, circleTop, right, circleTop + resolvedCircleSize)
                : new SKRect(left, circleTop, left + resolvedCircleSize, circleTop + resolvedCircleSize);

            _circleRects.Add(circleRect);

            bool isSelected = i == SelectedIndex;
            bool isHovered = ctx.LiveMode && i == _hoveredIndex;
            bool isPressed = ctx.LiveMode && i == _pressedIndex;

            DrawCircle(canvas, circleRect, isSelected, isHovered, isPressed);
            DrawText(canvas, itemRect, circleRect, text);

            if (ShowDividers && i < Items.Count - 1)
            {
                float dividerY = y + currentItemHeight + ItemSpacing / 2f;

                if (dividerY < bottom)
                {
                    using var dividerPaint = new SKPaint
                    {
                        IsAntialias = true,
                        Style = SKPaintStyle.Stroke,
                        StrokeWidth = 1f,
                        Color = DividerColor.ToSKColor()
                    };

                    canvas.DrawLine(left, dividerY, right, dividerY, dividerPaint);
                }
            }

            y += currentItemHeight + ItemSpacing;
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
            ? new SKRect(itemRect.Left, itemRect.Top + 1f, circleRect.Left - TextOffset, itemRect.Bottom)
            : new SKRect(circleRect.Right + TextOffset, itemRect.Top + 1f, itemRect.Right, itemRect.Bottom);

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

    private float GetTitleHeight()
    {
        return TitleFontSize + 6f;
    }

    private void RecalculateHeight()
    {
        int count = Math.Max(1, Items?.Count ?? 0);
        float titleHeight = string.IsNullOrWhiteSpace(Title) ? 0f : GetTitleHeight() + ItemSpacing;
        float contentHeight = count * ItemHeight + Math.Max(0, count - 1) * ItemSpacing;
        float desiredHeight = (float)Padding.Top + titleHeight + contentHeight + (float)Padding.Bottom;

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
