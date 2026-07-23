// ======================================================================================
// FILE: Mockup.Controls/ListBox.cs
//
// PURPOSE:
// - Modern ListBox control for the mockup designer.
// - Visual style aligned with ComboBox / TextBox controls.
// - Supports optional title above the list area.
// - Supports item selection, hover state in LiveMode and optional mock scrollbar.
// - Uses explicit item rects for rendering and hit testing.
//
// NOTES:
// - This is a visual mockup control, not a real scrolling widget.
// - Items are simple strings.
// - SelectedIndex remains float for compatibility with the existing model.
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

#region === LIST BOX ===

[ControlType(displayName: "List Box", group: "Input Fields")]
public partial class ListBox : DesignControl
{
    #region === CONTENT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Title")]
    private string title = string.Empty;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Items")]
    private List<string> items = ["Item 1", "Item 2", "Item 3"];

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Selected Index")]
    private float selectedIndex = 0f;

    partial void OnSelectedIndexChanged(float value)
    {
        if (Items == null || Items.Count == 0)
        {
            selectedIndex = -1f;
            return;
        }

        float clamped = Math.Clamp(value, 0f, Items.Count - 1);

        if (Math.Abs(selectedIndex - clamped) > 0.001f)
        {
            selectedIndex = clamped;
            OnPropertyChanged(nameof(SelectedIndex));
        }
    }

    #endregion

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
    [property: System.ComponentModel.DisplayName("Text Color")]
    private Color textColor = Theme.Text;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Title Color")]
    private Color titleColor = Theme.Text.Lighten(0.20f);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Selected Item Background")]
    private Color selectedItemBackgroundColor = SkiaRenderer.SelectionColor;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Selected Item Color")]
    private Color selectedItemTextColor = Colors.White;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Hover Item Background")]
    private Color hoverItemBackgroundColor = Theme.ControlBG.Darken(0.03f);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Corner Radius")]
    private float cornerRadius = 4f;

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
    private float fontSize = 13f;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Weight")]
    private FontWeight fontWeight = FontWeights.Normal;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Title Font Size")]
    private double titleFontSize = 12d;

    #endregion

    #region === LAYOUT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Item Height")]
    private float itemHeight = 28f;

    partial void OnItemHeightChanged(float value)
    {
        itemHeight = Math.Clamp(value, 20f, 80f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Item Spacing")]
    private float itemSpacing = 0f;

    partial void OnItemSpacingChanged(float value)
    {
        itemSpacing = Math.Clamp(value, 0f, 20f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Padding")]
    private Thickness padding = new(2, 2, 2, 2);

    #endregion

    #region === BEHAVIOR ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Title")]
    private bool showTitle = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Scrollbar")]
    private bool showScrollbar = false;

    #endregion

    #region === RUNTIME STATE ===

    [JsonIgnore, Browsable(false)]
    private readonly List<ItemHitTarget> _itemHitTargets = new();

    [JsonIgnore, Browsable(false)]
    private int _hoverItemIndex = -1;

    [JsonIgnore, Browsable(false)]
    private bool _isPressed;

    private const float ScrollbarWidth = 10f;
    private const float ScrollbarMargin = 2f;
    private const float TitleGap = 2f;

    #endregion

    #region === CTOR ===

    public ListBox()
    {
        IsActionControl = true;

        Name = "ListBox";
        ResizeStyle = ResizeStyles.ResizeAll;

        Width = 140f;
        Height = 100f;

        MinWidth = 70f;
        MinHeight = 50f;

        MaxWidth = 500f;
        MaxHeight = 500f;
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === PROPERTY REACTIONS ===

    partial void OnTitleChanged(string value)
    {
        InvalidateVisuals();
    }

    partial void OnShowTitleChanged(bool value)
    {
        InvalidateVisuals();
    }

    partial void OnTitleFontSizeChanged(double value)
    {
        InvalidateVisuals();
    }

    #endregion

    #region === HIT TEST ===

    public override bool HitTest(SKPoint point)
    {
        return VisualRect.Contains(point);
    }

    #endregion

    #region === POINTER HOOKS ===

    public override void OnPointerDown(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;

        _isPressed = true;
    }

    public override void OnPointerMove(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode)
        {
            ResetInteractionState();
            return;
        }

        int hoverIndex = HitTestItemIndex(ctx.WorldPoint);

        if (_hoverItemIndex != hoverIndex)
        {
            _hoverItemIndex = hoverIndex;
            InvalidateVisuals();
        }
    }

    public override void OnPointerUp(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;

        int hitIndex = HitTestItemIndex(ctx.WorldPoint);
        _isPressed = false;

        if (hitIndex >= 0 && hitIndex < Items.Count)
        {
            SelectedIndex = hitIndex;
            InvalidateVisuals();
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
        _itemHitTargets.Clear();

        bool hasTitle = HasVisibleTitle();
        float titleHeight = hasTitle ? GetMeasuredTitleHeight() : 0f;
        float titleGap = hasTitle ? TitleGap : 0f;

        var titleRect = hasTitle
            ? new SKRect(layout.Left, layout.Top, layout.Right, layout.Top + titleHeight)
            : SKRect.Empty;

        var listRect = new SKRect(
            layout.Left,
            layout.Top + titleHeight + titleGap,
            layout.Right,
            layout.Bottom
        );

        DrawBackground(canvas, listRect, ctx);

        if (hasTitle)
        {
            TextRenderer.Draw2(
                canvas: canvas,
                text: Title,
                bounds: titleRect,
                fontSize: (float)TitleFontSize,
                color: TitleColor,
                padding: new Thickness(0),
                fontWeight: FontWeights.Normal,
                textAlignment: System.Windows.TextAlignment.Left
            );
        }

        if (Items == null || Items.Count == 0)
            return;

        float leftInset = (float)Padding.Left;
        float topInset = (float)Padding.Top;
        float rightInset = (float)Padding.Right;
        float bottomInset = (float)Padding.Bottom;

        float scrollbarReserve = ShowScrollbar ? ScrollbarWidth + ScrollbarMargin + 3f : 0f;

        float contentLeft = listRect.Left + leftInset;
        float contentTop = listRect.Top + topInset;
        float contentRight = listRect.Right - rightInset - scrollbarReserve;
        float contentBottom = listRect.Bottom - bottomInset;

        float y = contentTop;

        for (int i = 0; i < Items.Count; i++)
        {
            float nextBottom = y + ItemHeight;
            if (nextBottom > contentBottom)
                break;

            var itemRect = new SKRect(
                contentLeft,
                y,
                contentRight,
                nextBottom
            );

            _itemHitTargets.Add(new ItemHitTarget(itemRect, i));

            bool isSelected = i == GetSelectedIndexInt();
            bool isHover = ctx.LiveMode && i == _hoverItemIndex;

            if (isSelected)
            {
                SkiaRenderer.DrawRect(
                    canvas: canvas,
                    rect: itemRect,
                    cornerRadius: 3f,
                    fillStyle: FillStyle.Solid,
                    fillColor: SelectedItemBackgroundColor,
                    borderColor: Colors.Transparent,
                    borderStyle: BorderStyle.None,
                    shadowOptions: ShadowOptions.Default,
                    borderWidth: 0f
                );
            }
            else if (isHover)
            {
                SkiaRenderer.DrawRect(
                    canvas: canvas,
                    rect: itemRect,
                    cornerRadius: 3f,
                    fillStyle: FillStyle.Solid,
                    fillColor: HoverItemBackgroundColor,
                    borderColor: Colors.Transparent,
                    borderStyle: BorderStyle.None,
                    shadowOptions: ShadowOptions.Default,
                    borderWidth: 0f
                );
            }

            TextRenderer.Draw2(
                canvas: canvas,
                text: Items[i],
                bounds: itemRect,
                fontSize: FontSize,
                color: isSelected ? SelectedItemTextColor : TextColor,
                textAlignment: System.Windows.TextAlignment.Left,
                padding: new Thickness(8, 0, 8, 0),
                fontWeight: isSelected ? FontWeights.Medium : FontWeight
            );

            y += ItemHeight + ItemSpacing;
        }

        if (ShowScrollbar)
        {
            DrawScrollBar(canvas, listRect);
        }
    }

    #endregion

    #region === HELPERS ===

    private void DrawBackground(SKCanvas canvas, SKRect rect, RenderContext ctx)
    {
        Color fillColor = BackgroundColor;
        Color resolvedBorderColor = BorderColor;

        if (ctx.LiveMode && _hoverItemIndex >= 0)
        {
            resolvedBorderColor = resolvedBorderColor.Darken(0.03f);
        }

        ShadowOptions shadowOptions = ShadowOptions.Default;

        if (Elevation > 0)
        {
            shadowOptions = GetElevation(Elevation);
        }

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: rect,
            fillColor: fillColor,
            cornerRadius: Math.Clamp(CornerRadius, 0f, 12f),
            fillStyle: FillStyle.Solid,
            borderStyle: BorderStyle.Solid,
            borderColor: resolvedBorderColor,
            borderWidth: 0.9f,
            shadowOptions: shadowOptions
        );
    }

    private void DrawScrollBar(SKCanvas canvas, SKRect rect)
    {
        float scrollbarLeft = rect.Right - ScrollbarWidth - ScrollbarMargin;
        float scrollbarTop = rect.Top + ScrollbarMargin;
        float scrollbarHeight = rect.Height - (2 * ScrollbarMargin);

        using (var trackPaint = new SKPaint
        {
            Color = Theme.ControlBG.ToSKColor().WithAlpha(220),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        })
        {
            canvas.DrawRoundRect(
                new SKRect(
                    scrollbarLeft,
                    scrollbarTop,
                    scrollbarLeft + ScrollbarWidth,
                    scrollbarTop + scrollbarHeight
                ),
                4f,
                4f,
                trackPaint
            );
        }

        float thumbHeight = Math.Max(12f, scrollbarHeight * 0.25f);
        float thumbPosition = scrollbarTop + 5f;

        using (var thumbPaint = new SKPaint
        {
            Color = Theme.ControlBorder.ToSKColor().WithAlpha(190),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        })
        {
            canvas.DrawRoundRect(
                new SKRect(
                    scrollbarLeft + 1f,
                    thumbPosition,
                    scrollbarLeft + ScrollbarWidth - 1f,
                    thumbPosition + thumbHeight
                ),
                3f,
                3f,
                thumbPaint
            );
        }
    }

    private bool HasVisibleTitle()
    {
        return ShowTitle && !string.IsNullOrWhiteSpace(Title);
    }

    private float GetMeasuredTitleHeight()
    {
        var style = new Topten.RichTextKit.Style
        {
            FontFamily = Theme.FontFamily,
            FontSize = (float)TitleFontSize,
            FontWeight = FontWeights.Normal.ToFontWeightValue(),
            TextColor = TitleColor.ToSKColor()
        };

        var tb = new Topten.RichTextKit.TextBlock
        {
            MaxWidth = Math.Max(1f, Width),
            Alignment = Topten.RichTextKit.TextAlignment.Left,
            EllipsisEnabled = true
        };

        tb.AddText(string.IsNullOrWhiteSpace(Title) ? " " : Title, style);
        tb.Layout();

        return Math.Max(12f, tb.MeasuredHeight + 2f);
    }

    private int GetSelectedIndexInt()
    {
        if (Items == null || Items.Count == 0)
            return -1;

        return Math.Clamp((int)MathF.Round(SelectedIndex), 0, Items.Count - 1);
    }

    private int HitTestItemIndex(SKPoint point)
    {
        foreach (var item in _itemHitTargets)
        {
            if (item.Rect.Contains(point))
                return item.Index;
        }

        return -1;
    }

    private void ResetInteractionState()
    {
        bool changed = false;

        if (_hoverItemIndex >= 0)
        {
            _hoverItemIndex = -1;
            changed = true;
        }

        if (_isPressed)
        {
            _isPressed = false;
            changed = true;
        }

        if (changed)
            InvalidateVisuals();
    }

    private void InvalidateVisuals()
    {
        MSG.UI.InvalidateDesigner();
    }

    #endregion

    #region === PRIVATE TYPES ===

    private readonly record struct ItemHitTarget(SKRect Rect, int Index);

    #endregion
}

#endregion
