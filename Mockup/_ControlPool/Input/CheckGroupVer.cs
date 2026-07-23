// ======================================================================================
// FILE: Mockup.Controls/CheckGroupVer.cs
//
// PURPOSE:
// - Vertical checkbox group control for the mockup designer.
// - Renders a list of checkbox options with multi selection.
// - Visual style aligned with CheckBox.cs.
// - Supports hover / pressed feedback in LiveMode.
// - Clicking an item in LiveMode toggles it.
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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FontWeight = System.Windows.FontWeight;
using RichTextAlignment = Topten.RichTextKit.TextAlignment;

namespace Mockup.Controls;

#region ### CHECK GROUP VER ###

[ControlType(displayName: "Check Group – Vertical", group: "Selection")]
public partial class CheckGroupVer : DesignControl
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
    [property: System.ComponentModel.DisplayName("Checked Indices")]
    private ObservableCollection<int> checkedIndices = [0];

    partial void OnItemsChanged(ObservableCollection<string> value)
    {
        HookItemsCollection(value);
        RecalculateSize();
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
    [property: System.ComponentModel.DisplayName("Box Background")]
    private Color boxBackgroundColor = Colors.White;

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
    [property: System.ComponentModel.DisplayName("Elevation")]
    private int elevation = 0;

    partial void OnElevationChanged(int value) => elevation = Math.Clamp(value, 0, 5);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Corner Radius")]
    private float cornerRadius = 3f;

    partial void OnCornerRadiusChanged(float value) => cornerRadius = Math.Clamp(value, 0f, 8f);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Border Width")]
    private float borderWidth = 1f;

    partial void OnBorderWidthChanged(float value) => borderWidth = Math.Clamp(value, 0f, 8f);

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
        RecalculateSize();
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Weight")]
    private FontWeight fontWeight = FontWeights.Normal;

    partial void OnFontWeightChanged(FontWeight value) => RecalculateSize();

    #endregion

    #region ### LAYOUT ###

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Padding")]
    private Thickness padding = new(8, 8, 8, 8);

    partial void OnPaddingChanged(Thickness value) => RecalculateSize();

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Item Height")]
    private float itemHeight = 24f;

    partial void OnItemHeightChanged(float value)
    {
        itemHeight = Math.Clamp(value, 16f, 80f);
        RecalculateSize();
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Item Spacing")]
    private float itemSpacing = 4f;

    partial void OnItemSpacingChanged(float value)
    {
        itemSpacing = Math.Clamp(value, 0f, 40f);
        RecalculateSize();
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Text Offset")]
    private float textOffset = 8f;

    partial void OnTextOffsetChanged(float value)
    {
        textOffset = Math.Clamp(value, 0f, 24f);
        RecalculateSize();
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Box Size")]
    private float boxSize = 16f;

    partial void OnBoxSizeChanged(float value)
    {
        boxSize = Math.Clamp(value, 12f, 28f);
        RecalculateSize();
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Right To Left")]
    private bool rightToLeft = false;

    partial void OnRightToLeftChanged(bool value) => RecalculateSize();

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
    private readonly List<SKRect> _boxRects = [];

    #endregion

    #region ### CTOR ###

    public CheckGroupVer()
    {
        IsActionControl = true;

        Name = "CheckGroupVer";
        ResizeStyle = ResizeStyles.WidthOnly;

        ExplicitePreviewWidth = 140;
        ExplicitePreviewHeight = 100;

        Width = 140f;
        Height = 90f;

        MinWidth = 100f;
        MinHeight = 40f;

        MaxWidth = 600f;
        MaxHeight = 800f;

        HookItemsCollection(Items);
        RecalculateSize();
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
            ToggleIndex(hitIndex);

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
        RecalculateMinWidth();

        DrawBackground(canvas, layout);

        _itemRects.Clear();
        _boxRects.Clear();

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

            float maxBoxSize = Math.Min(28f, currentItemHeight);
            float resolvedBoxSize = maxBoxSize <= 12f
                ? maxBoxSize
                : Math.Clamp(BoxSize, 12f, maxBoxSize);

            if (resolvedBoxSize <= 1f)
                break;

            float boxTop = y + (currentItemHeight - resolvedBoxSize) / 2f;

            var boxRect = RightToLeft
                ? new SKRect(right - resolvedBoxSize, boxTop, right, boxTop + resolvedBoxSize)
                : new SKRect(left, boxTop, left + resolvedBoxSize, boxTop + resolvedBoxSize);

            _boxRects.Add(boxRect);

            bool isChecked = IsIndexChecked(i);
            bool isHovered = ctx.LiveMode && i == _hoveredIndex;
            bool isPressed = ctx.LiveMode && i == _pressedIndex;

            DrawBox(canvas, boxRect, isChecked, isHovered, isPressed);
            DrawText(canvas, itemRect, boxRect, text);

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

    private void DrawBox(SKCanvas canvas, SKRect boxRect, bool isChecked, bool isHovered, bool isPressed)
    {
        var (fillColor, resolvedBorderColor) = GetBoxVisualColors(isChecked, isHovered, isPressed);

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: boxRect,
            cornerRadius: Math.Clamp(CornerRadius, 0f, 8f),
            fillStyle: FillStyle.Solid,
            fillColor: fillColor,
            borderColor: resolvedBorderColor,
            borderStyle: BorderStyle.Solid,
            shadowOptions: Elevation > 0 ? GetElevation(Elevation) : ShadowOptions.Default,
            borderWidth: 0.9f
        );

        if (isChecked)
            DrawCheckMark(canvas, boxRect, CheckColor);
    }

    private void DrawText(SKCanvas canvas, SKRect itemRect, SKRect boxRect, string text)
    {
        SKRect textRect = RightToLeft
            ? new SKRect(itemRect.Left, itemRect.Top + 1f, boxRect.Left - TextOffset, itemRect.Bottom)
            : new SKRect(boxRect.Right + TextOffset, itemRect.Top + 1f, itemRect.Right, itemRect.Bottom);

        var alignment = RightToLeft ? RichTextAlignment.Right : RichTextAlignment.Left;

        TextRenderer.Draw(
            canvas: canvas,
            text: text,
            bounds: textRect,
            fontSize: FontSize,
            color: TextColor.ToSKColor(),
            fontWeight: FontWeight,
            fontFamily: Theme.FontFamily,
            textAlignment: alignment
        );
    }

    #endregion

    #region ### HELPERS ###

    private bool IsIndexChecked(int index)
    {
        return CheckedIndices?.Contains(index) == true;
    }

    private void ToggleIndex(int index)
    {
        CheckedIndices ??= [];

        if (CheckedIndices.Contains(index))
            CheckedIndices.Remove(index);
        else
            CheckedIndices.Add(index);

        OnPropertyChanged(nameof(CheckedIndices));
    }

    private (Color FillColor, Color BorderColor) GetBoxVisualColors(bool isChecked, bool isHovered, bool isPressed)
    {
        if (isChecked)
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

        Color fillUnchecked = BoxBackgroundColor;
        Color borderUnchecked = BorderColor;

        if (isHovered)
        {
            fillUnchecked = fillUnchecked.Darken(0.02f);
            borderUnchecked = borderUnchecked.Darken(0.04f);
        }

        if (isPressed)
        {
            fillUnchecked = fillUnchecked.Darken(0.04f);
            borderUnchecked = borderUnchecked.Darken(0.08f);
        }

        return (fillUnchecked, borderUnchecked);
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
        path.MoveTo(rect.Left + rect.Width * 0.22f, rect.Top + rect.Height * 0.52f);
        path.LineTo(rect.Left + rect.Width * 0.43f, rect.Top + rect.Height * 0.74f);
        path.LineTo(rect.Left + rect.Width * 0.78f, rect.Top + rect.Height * 0.30f);

        canvas.DrawPath(path, paint);
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

    private void RecalculateSize()
    {
        RecalculateMinWidth();
        RecalculateHeight();
    }

    private void RecalculateHeight()
    {
        int count = Math.Max(1, Items?.Count ?? 0);
        float contentHeight = count * ItemHeight + Math.Max(0, count - 1) * ItemSpacing;
        float desiredHeight = (float)Padding.Top + contentHeight + (float)Padding.Bottom;

        float effectiveMinHeight = Math.Max(0f, desiredHeight);
        MinHeight = effectiveMinHeight;
        Height = Math.Clamp(desiredHeight, effectiveMinHeight, MaxHeight);
    }

    private void RecalculateMinWidth()
    {
        float longestTextWidth = 0f;

        if (Items != null)
        {
            foreach (var item in Items)
            {
                string text = item ?? string.Empty;

                float textWidth = TextRenderer.MeasureTextWidth(
                    text: text,
                    fontSize: FontSize,
                    fontWeight: FontWeight,
                    fontFamily: Theme.FontFamily);

                if (textWidth > longestTextWidth)
                    longestTextWidth = textWidth;
            }
        }

        longestTextWidth += 5;

        float effectiveBoxSize = Math.Clamp(BoxSize, 12f, 28f);

        float desiredMinWidth =
            (float)Padding.Left
            + effectiveBoxSize
            + TextOffset
            + longestTextWidth
            + (float)Padding.Right;

        desiredMinWidth = Math.Max(40f, desiredMinWidth);

        MinWidth = desiredMinWidth;

        if (Width < MinWidth)
            Width = MinWidth;
    }

    private void HookItemsCollection(ObservableCollection<string>? collection)
    {
        if (collection == null)
            return;

        collection.CollectionChanged -= Items_CollectionChanged;
        collection.CollectionChanged += Items_CollectionChanged;
    }

    private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RecalculateSize();
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