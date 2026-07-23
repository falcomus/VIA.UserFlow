// ======================================================================================
// FILE: Mockup.Controls/ComboBox.cs
//
// PURPOSE:
// - Modern ComboBox control for the mockup designer.
// - Visual style aligned with Button / TextBox controls.
// - Compact header with optional title, selected value / placeholder and chevron button.
// - Popup list with explicit interactive rects for reliable hit testing.
//
// NOTES:
// - This is a visual mockup control, not a real input widget.
// - Items are simple strings.
// - IsDropDownOpen is part of the mockup state and controls popup rendering.
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

namespace Mockup.Controls;

#region === COMBO BOX ===

[ControlType(displayName: "Combo Box", group: "Input Fields")]
public partial class ComboBox : DesignControl
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
    [property: System.ComponentModel.DisplayName("Placeholder")]
    private string placeholder = "Select...";

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Selected Index")]
    private int selectedIndex = -1;

    partial void OnSelectedIndexChanged(int value)
    {
        if (Items == null || Items.Count == 0)
        {
            selectedIndex = -1;
            SelectedItem = string.Empty;
            return;
        }

        int clamped = Math.Clamp(value, -1, Items.Count - 1);

        if (selectedIndex != clamped)
        {
            selectedIndex = clamped;
            OnPropertyChanged(nameof(SelectedIndex));
        }

        SelectedItem = clamped >= 0 ? Items[clamped] : string.Empty;
    }

    [ObservableProperty]
    [property: Browsable(false)]
    private string selectedItem = string.Empty;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Items")]
    private ObservableCollection<string> items = new() { "Item 1", "Item 2", "Item 3" };

    partial void OnItemsChanged(ObservableCollection<string> value)
    {
        NormalizeSelection();
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
    [property: System.ComponentModel.DisplayName("Placeholder Color")]
    private Color placeholderColor = Theme.Text.Lighten(0.45f);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Title Color")]
    private Color titleColor = Theme.Text.Lighten(0.20f);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Popup Background")]
    private Color popupBackgroundColor = Colors.White;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Popup Border")]
    private Color popupBorderColor = Theme.ControlBorder;

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

    #endregion

    #region === TYPOGRAPHY ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Size")]
    private double fontSize = 13d;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Title Font Size")]
    private double titleFontSize = 12d;

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
    [property: System.ComponentModel.DisplayName("Size")]
    private ButtonSizePreset sizePreset = ButtonSizePreset.Normal;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Padding")]
    private Thickness padding = new(10, 0, 10, 0);

    #endregion

    #region === BEHAVIOR ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Title")]
    private bool showTitle = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Is DropDown Open")]
    private bool isDropDownOpen = false;

    #endregion

    #region === RUNTIME STATE ===

    [JsonIgnore, Browsable(false)]
    private bool _isHovered;

    [JsonIgnore, Browsable(false)]
    private bool _isPressed;

    [JsonIgnore, Browsable(false)]
    private bool _hoverToggle;

    [JsonIgnore, Browsable(false)]
    private int _hoverPopupIndex = -1;

    [JsonIgnore, Browsable(false)]
    private bool _applyingSizePreset;

    [JsonIgnore, Browsable(false)]
    private SKRect _headerRect;

    [JsonIgnore, Browsable(false)]
    private SKRect _toggleButtonRect;

    [JsonIgnore, Browsable(false)]
    private SKRect _popupRect;

    [JsonIgnore, Browsable(false)]
    private readonly List<PopupItemHitTarget> _popupItems = new();

    private const float TitleGap = 2f;
    private const float PopupGap = 4f;
    private const float PopupOuterPadding = 4f;

    #endregion

    #region === CTOR ===

    public ComboBox()
    {
        IsActionControl = true;

        Name = "ComboBox";
        ResizeStyle = ResizeStyles.WidthOnly;

        Width = 140f;
        Height = 30f;

        MinWidth = 70f;
        MinHeight = 26f;

        MaxWidth = 600f;
        MaxHeight = 220f;

        ApplySizePreset(SizePreset);
        RecalculateOverallHeight();
        NormalizeSelection();
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === PROPERTY REACTIONS ===

    partial void OnSizePresetChanged(ButtonSizePreset value)
    {
        ApplySizePreset(value);
        RecalculateOverallHeight();
    }

    partial void OnTitleChanged(string value)
    {
        RecalculateOverallHeight();
    }

    partial void OnShowTitleChanged(bool value)
    {
        RecalculateOverallHeight();
    }

    partial void OnTitleFontSizeChanged(double value)
    {
        RecalculateOverallHeight();
    }

    #endregion

    #region === HIT TEST ===

    public bool HitTestToggle(SKPoint point) => _toggleButtonRect.Contains(point);

    public override bool HitTest(SKPoint point)
    {
        if (VisualRect.Contains(point))
            return true;

        if (IsDropDownOpen && _popupRect.Contains(point))
            return true;

        return false;
    }

    #endregion

    #region === POINTER HOOKS ===

    public override void OnPointerDown(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;

        if (IsDropDownOpen)
        {
            if (_popupRect.Contains(ctx.WorldPoint))
            {
                if (TryHitTestPopupItem(ctx.WorldPoint, out int popupIndex))
                {
                    SelectPopupIndex(popupIndex);
                    return;
                }
            }

            if (_headerRect.Contains(ctx.WorldPoint))
            {
                SetPressedState(true);
                SetHoverState(true);
                if (_toggleButtonRect.Contains(ctx.WorldPoint))
                    _hoverToggle = true;

                return;
            }

            IsDropDownOpen = false;
            ResetInteractionState();
            InvalidateVisuals();
            return;
        }

        if (_headerRect.Contains(ctx.WorldPoint))
        {
            SetPressedState(true);
            SetHoverState(true);
            _hoverToggle = _toggleButtonRect.Contains(ctx.WorldPoint);
        }
    }

    public override void OnPointerMove(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode)
        {
            ResetInteractionState();
            return;
        }

        bool insideHeader = _headerRect.Contains(ctx.WorldPoint);
        bool insideToggle = _toggleButtonRect.Contains(ctx.WorldPoint);

        SetHoverState(insideHeader);

        if (!insideHeader && _isPressed)
            SetPressedState(false);

        _hoverToggle = insideToggle;

        int hoverIndex = -1;
        if (IsDropDownOpen && _popupRect.Contains(ctx.WorldPoint))
        {
            TryHitTestPopupItem(ctx.WorldPoint, out hoverIndex);
        }

        if (_hoverPopupIndex != hoverIndex)
        {
            _hoverPopupIndex = hoverIndex;
            InvalidateVisuals();
        }

        if (insideHeader)
            Mouse.OverrideCursor = Cursors.Hand;
    }

    public override void OnPointerUp(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;

        bool insideHeader = _headerRect.Contains(ctx.WorldPoint);
        bool commitClick = _isPressed && insideHeader;

        SetPressedState(false);
        SetHoverState(insideHeader);
        _hoverToggle = _toggleButtonRect.Contains(ctx.WorldPoint);

        if (commitClick)
        {
            IsDropDownOpen = !IsDropDownOpen;
            _hoverPopupIndex = -1;
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
        RecalculateOverallHeight();
        _popupItems.Clear();

        bool hasTitle = HasVisibleTitle();
        float titleHeight = hasTitle ? GetMeasuredTitleHeight() : 0f;
        float titleGap = hasTitle ? TitleGap : 0f;
        float headerHeight = GetHeaderRowHeight();

        var titleRect = hasTitle
            ? new SKRect(layout.Left, layout.Top, layout.Right, layout.Top + titleHeight)
            : SKRect.Empty;

        _headerRect = new SKRect(
            layout.Left,
            layout.Top + titleHeight + titleGap,
            layout.Right,
            layout.Top + titleHeight + titleGap + headerHeight
        );

        _toggleButtonRect = new SKRect(
            _headerRect.Right - headerHeight,
            _headerRect.Top,
            _headerRect.Right,
            _headerRect.Bottom
        );

        DrawHeader(canvas, titleRect, _headerRect, ctx, hasTitle);

        if (IsDropDownOpen)
        {
            DrawPopup(canvas, _headerRect, ctx);
        }
    }

    #endregion

    #region === DRAW HELPERS ===

    private void DrawHeader(SKCanvas canvas, SKRect titleRect, SKRect headerRect, RenderContext ctx, bool hasTitle)
    {
        var (fillColor, resolvedBorderColor) = GetHeaderVisualColors(ctx);
        var shadowOptions = GetVisualShadow(ctx);

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: headerRect,
            cornerRadius: GetSafeCornerRadius(),
            fillStyle: FillStyle.Solid,
            fillColor: fillColor,
            borderColor: resolvedBorderColor,
            borderStyle: BorderStyle.Solid,
            shadowOptions: shadowOptions,
            borderWidth: 0.85f
        );

        if (hasTitle)
        {
            TextRenderer.Draw2(
                canvas: canvas,
                text: Title,
                bounds: titleRect,
                fontSize: TitleFontSize,
                color: TitleColor,
                padding: new Thickness(0),
                fontWeight: FontWeights.Normal,
                textAlignment: System.Windows.TextAlignment.Left
            );
        }

        var contentRect = new SKRect(
            headerRect.Left + (float)Padding.Left,
            headerRect.Top,
            _toggleButtonRect.Left - 4f,
            headerRect.Bottom
        );

        bool hasSelection = SelectedIndex >= 0 && SelectedIndex < Items.Count;
        string displayText = hasSelection ? Items[SelectedIndex] : Placeholder;
        Color displayColor = hasSelection ? TextColor : PlaceholderColor;

        TextRenderer.Draw2(
            canvas: canvas,
            text: displayText,
            bounds: contentRect,
            fontSize: FontSize,
            color: displayColor,
            padding: new Thickness(0),
            fontWeight: FontWeight,
            textAlignment: System.Windows.TextAlignment.Left
        );

        DrawChevronButton(canvas, _toggleButtonRect, ctx);
    }

    private void DrawChevronButton(SKCanvas canvas, SKRect rect, RenderContext ctx)
    {
        Color fill = _hoverToggle && ctx.LiveMode
            ? Theme.ControlBG.Darken(0.02f)
            : BackgroundColor;

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: new SKRect(rect.Left + 1f, rect.Top + 1f, rect.Right - 1f, rect.Bottom - 1f),
            cornerRadius: Math.Max(2f, GetSafeCornerRadius() - 1f),
            fillStyle: FillStyle.Solid,
            fillColor: fill,
            borderColor: Colors.Transparent,
            borderStyle: BorderStyle.None,
            shadowOptions: ShadowOptions.Default,
            borderWidth: 0f
        );

        using var paint = new SKPaint
        {
            Color = TextColor.ToSKColor().WithAlpha(180),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        float s = Math.Min(rect.Width, rect.Height) * 0.16f;
        float cx = rect.MidX;
        float cy = rect.MidY + (IsDropDownOpen ? -0.5f : 0.5f);

        using var path = new SKPath();
        if (IsDropDownOpen)
        {
            path.MoveTo(cx - s, cy + s * 0.35f);
            path.LineTo(cx, cy - s);
            path.LineTo(cx + s, cy + s * 0.35f);
            path.Close();
        }
        else
        {
            path.MoveTo(cx - s, cy - s * 0.35f);
            path.LineTo(cx, cy + s);
            path.LineTo(cx + s, cy - s * 0.35f);
            path.Close();
        }

        canvas.DrawPath(path, paint);
    }

    public void DrawPopup(SKCanvas canvas, SKRect headerRect, RenderContext ctx)
    {
        float popupTop = headerRect.Bottom + PopupGap;
        float itemHeight = GetPopupItemHeight();
        float popupHeight = Items.Count * itemHeight + PopupOuterPadding * 2f;

        _popupRect = new SKRect(
            headerRect.Left,
            popupTop,
            headerRect.Right,
            popupTop + popupHeight
        );

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: _popupRect,
            cornerRadius: Math.Max(4f, GetSafeCornerRadius()),
            fillStyle: FillStyle.Solid,
            fillColor: PopupBackgroundColor,
            borderColor: PopupBorderColor,
            borderStyle: BorderStyle.Solid,
            shadowOptions: GetPopupShadow(),
            borderWidth: 0.9f
        );

        float y = _popupRect.Top + PopupOuterPadding;

        for (int i = 0; i < Items.Count; i++)
        {
            var itemRect = new SKRect(
                _popupRect.Left + 4f,
                y,
                _popupRect.Right - 4f,
                y + itemHeight
            );

            _popupItems.Add(new PopupItemHitTarget(itemRect, i));

            bool isSelected = i == SelectedIndex;
            bool isHover = i == _hoverPopupIndex;

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
            else if (isHover && ctx.LiveMode)
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
                padding: new Thickness(10, 0, 10, 0),
                fontWeight: isSelected ? FontWeights.Medium : FontWeight,
                textAlignment: System.Windows.TextAlignment.Left
            );

            y += itemHeight;
        }
    }

    #endregion

    #region === HELPERS ===

    private void ApplySizePreset(ButtonSizePreset preset)
    {
        if (_applyingSizePreset)
            return;

        _applyingSizePreset = true;

        try
        {
            switch (preset)
            {
                case ButtonSizePreset.Small:
                    Height = 26f;
                    MinHeight = 26f;
                    FontSize = 12d;
                    TitleFontSize = 10d;
                    Padding = new Thickness(8, 0, 8, 0);
                    CornerRadius = 4f;
                    break;

                case ButtonSizePreset.Large:
                    Height = 36f;
                    MinHeight = 36f;
                    FontSize = 14d;
                    TitleFontSize = 12d;
                    Padding = new Thickness(12, 0, 12, 0);
                    CornerRadius = 5f;
                    break;

                default:
                    Height = 30f;
                    MinHeight = 30f;
                    FontSize = 13d;
                    TitleFontSize = 11d;
                    Padding = new Thickness(10, 0, 10, 0);
                    CornerRadius = 4f;
                    break;
            }

            if (Width < MinWidth)
                Width = MinWidth;
        }
        finally
        {
            _applyingSizePreset = false;
        }
    }

    private void RecalculateOverallHeight()
    {
        float headerHeight = GetHeaderRowHeight();
        float titleExtra = HasVisibleTitle() ? GetMeasuredTitleHeight() + TitleGap : 0f;
        float desiredHeight = Math.Clamp(headerHeight + titleExtra, MinHeight, MaxHeight);

        if (Math.Abs(Height - desiredHeight) > 0.5f)
            Height = desiredHeight;
    }

    private float GetHeaderRowHeight()
    {
        return SizePreset switch
        {
            ButtonSizePreset.Small => 26f,
            ButtonSizePreset.Large => 36f,
            _ => 30f
        };
    }

    private float GetPopupItemHeight()
    {
        return SizePreset switch
        {
            ButtonSizePreset.Small => 24f,
            ButtonSizePreset.Large => 34f,
            _ => 28f
        };
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

    private void NormalizeSelection()
    {
        if (Items == null || Items.Count == 0)
        {
            SelectedIndex = -1;
            SelectedItem = string.Empty;
            return;
        }

        if (SelectedIndex < 0 || SelectedIndex >= Items.Count)
        {
            SelectedIndex = -1;
            SelectedItem = string.Empty;
            OnPropertyChanged(nameof(SelectedIndex));
            return;
        }

        SelectedItem = Items[SelectedIndex];
    }

    private void SelectPopupIndex(int index)
    {
        if (index < 0 || index >= Items.Count)
            return;

        SelectedIndex = index;
        SelectedItem = Items[index];
        IsDropDownOpen = false;
        _hoverPopupIndex = -1;
        InvalidateVisuals();
    }

    private bool TryHitTestPopupItem(SKPoint point, out int index)
    {
        foreach (var item in _popupItems)
        {
            if (item.Rect.Contains(point))
            {
                index = item.Index;
                return true;
            }
        }

        index = -1;
        return false;
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

        if (_hoverToggle)
        {
            _hoverToggle = false;
            changed = true;
        }

        if (_hoverPopupIndex >= 0)
        {
            _hoverPopupIndex = -1;
            changed = true;
        }

        if (changed)
            InvalidateVisuals();

        Mouse.OverrideCursor = null;
    }

    private void SetHoverState(bool value)
    {
        if (_isHovered == value)
            return;

        _isHovered = value;
        InvalidateVisuals();
    }

    private void SetPressedState(bool value)
    {
        if (_isPressed == value)
            return;

        _isPressed = value;
        InvalidateVisuals();
    }

    private void InvalidateVisuals()
    {
        MSG.UI.InvalidateDesigner();
    }

    private (Color FillColor, Color BorderColor) GetHeaderVisualColors(RenderContext ctx)
    {
        Color fillColor = BackgroundColor;
        Color resolvedBorderColor = BorderColor;

        if (ctx.LiveMode && _isHovered)
        {
            fillColor = fillColor.Darken(0.015f);
            resolvedBorderColor = resolvedBorderColor.Darken(0.04f);
        }

        if (ctx.LiveMode && _isPressed)
        {
            fillColor = fillColor.Darken(0.03f);
            resolvedBorderColor = resolvedBorderColor.Darken(0.08f);
        }

        return (fillColor, resolvedBorderColor);
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

    private ShadowOptions GetPopupShadow()
    {
        return new ShadowOptions
        {
            Color = SKColors.Black.WithAlpha(40),
            Dx = 0f,
            Dy = 2f,
            Sigma = 3f
        };
    }

    private float GetSafeCornerRadius()
    {
        return Math.Clamp(CornerRadius, 0f, 12f);
    }

    #endregion

    #region === PRIVATE TYPES ===

    private readonly record struct PopupItemHitTarget(SKRect Rect, int Index);

    #endregion
}

#endregion










//// ======================================================================================
//// FILE: Mockup.Controls/ComboBox.cs
////
//// PURPOSE:
//// - Modern ComboBox control for the mockup designer.
//// - Visual style aligned with Button / TextBox controls.
//// - Compact header with optional title, selected value / placeholder and chevron button.
//// - Popup list with explicit interactive rects for reliable hit testing.
////
//// NOTES:
//// - This is a visual mockup control, not a real input widget.
//// - Items are simple strings.
//// - IsDropDownOpen is part of the mockup state and controls popup rendering.
//// ======================================================================================

//using CommunityToolkit.Mvvm.ComponentModel;
//using Mockup.ColorSystem;
//using Mockup.Domain.Registry;
//using Mockup.Messages;
//using Mockup.Registry;
//using Mockup.Rendering;
//using SkiaSharp;
//using SkiaSharp.Views.WPF;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.ComponentModel;
//using System.Text.Json.Serialization;
//using System.Windows;
//using System.Windows.Input;
//using System.Windows.Media;

//namespace Mockup.Controls;

//#region === COMBO BOX =====================================================================

//[ControlType(displayName: "ComboBox", group: "Input")]
//public partial class ComboBox : DesignControl
//{
//    #region === CONTENT ===================================================================

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Content")]
//    [property: System.ComponentModel.DisplayName("Title")]
//    private string title = string.Empty;

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Content")]
//    [property: System.ComponentModel.DisplayName("Placeholder")]
//    private string placeholder = "Select...";

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Content")]
//    [property: System.ComponentModel.DisplayName("Selected Index")]
//    private int selectedIndex = -1;

//    partial void OnSelectedIndexChanged(int value)
//    {
//        if (Items == null || Items.Count == 0)
//        {
//            selectedIndex = -1;
//            SelectedItem = string.Empty;
//            return;
//        }

//        int clamped = Math.Clamp(value, -1, Items.Count - 1);

//        if (selectedIndex != clamped)
//        {
//            selectedIndex = clamped;
//            OnPropertyChanged(nameof(SelectedIndex));
//        }

//        SelectedItem = clamped >= 0 ? Items[clamped] : string.Empty;
//    }

//    [ObservableProperty]
//    [property: Browsable(false)]
//    private string selectedItem = string.Empty;

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Content")]
//    [property: System.ComponentModel.DisplayName("Items")]
//    private ObservableCollection<string> items = new() { "Item 1", "Item 2", "Item 3" };

//    partial void OnItemsChanged(ObservableCollection<string> value)
//    {
//        NormalizeSelection();
//    }

//    #endregion

//    #region === APPEARANCE ================================================================

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Background Color")]
//    private Color backgroundColor = Colors.White;

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Border Color")]
//    private Color borderColor = Theme.ControlBorder;

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Text Color")]
//    private Color textColor = Theme.Text;

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Placeholder Color")]
//    private Color placeholderColor = Theme.Text.Lighten(0.45f);

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Title Color")]
//    private Color titleColor = Theme.Text.Lighten(0.20f);

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Popup Background")]
//    private Color popupBackgroundColor = Colors.White;

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Popup Border")]
//    private Color popupBorderColor = Theme.ControlBorder;

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Selected Item Background")]
//    private Color selectedItemBackgroundColor = SkiaRenderer.SelectionColor; 

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Selected Item Color")]
//    private Color selectedItemTextColor = Colors.White;

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Hover Item Background")]
//    private Color hoverItemBackgroundColor = Theme.ControlBG.Darken(0.03f);

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Corner Radius")]
//    private float cornerRadius = 4f;

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Elevation")]
//    private int elevation = 0;

//    #endregion

//    #region === TYPOGRAPHY ================================================================

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Typography")]
//    [property: System.ComponentModel.DisplayName("Font Size")]
//    private double fontSize = 13d;

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Typography")]
//    [property: System.ComponentModel.DisplayName("Title Font Size")]
//    private double titleFontSize = 12d;

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Typography")]
//    [property: System.ComponentModel.DisplayName("Font Weight")]
//    private FontWeight fontWeight = FontWeights.Normal;

//    #endregion

//    #region === LAYOUT ====================================================================

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Layout")]
//    [property: System.ComponentModel.DisplayName("Size")]
//    private ButtonSizePreset sizePreset = ButtonSizePreset.Normal;

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Layout")]
//    [property: System.ComponentModel.DisplayName("Padding")]
//    private Thickness padding = new(10, 0, 10, 0);

//    #endregion

//    #region === BEHAVIOR ==================================================================

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Behavior")]
//    [property: System.ComponentModel.DisplayName("Show Title")]
//    private bool showTitle = true;

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Behavior")]
//    [property: System.ComponentModel.DisplayName("Is DropDown Open")]
//    private bool isDropDownOpen = false;

//    #endregion

//    #region === RUNTIME STATE ==============================================================

//    [JsonIgnore, Browsable(false)]
//    private bool _isHovered;

//    [JsonIgnore, Browsable(false)]
//    private bool _isPressed;

//    [JsonIgnore, Browsable(false)]
//    private bool _hoverToggle;

//    [JsonIgnore, Browsable(false)]
//    private int _hoverPopupIndex = -1;

//    [JsonIgnore, Browsable(false)]
//    private bool _applyingSizePreset;

//    [JsonIgnore, Browsable(false)]
//    private SKRect _headerRect;

//    [JsonIgnore, Browsable(false)]
//    private SKRect _toggleButtonRect;

//    [JsonIgnore, Browsable(false)]
//    private SKRect _popupRect;

//    [JsonIgnore, Browsable(false)]
//    private readonly List<PopupItemHitTarget> _popupItems = new();

//    private const float TitleGap = 2f;
//    private const float PopupGap = 4f;
//    private const float PopupOuterPadding = 4f;

//    #endregion

//    #region === CTOR ======================================================================

//    public ComboBox()
//    {
//        IsActionControl = true;

//        Name = "ComboBox";
//        ResizeStyle = ResizeStyles.WidthOnly;

//        Width = 140f;
//        Height = 30f;

//        MinWidth = 70f;
//        MinHeight = 26f;

//        MaxWidth = 600f;
//        MaxHeight = 220f;

//        ApplySizePreset(SizePreset);
//        RecalculateOverallHeight();
//        NormalizeSelection();
//    }

//    public override string ToString() => string.Empty;

//    #endregion

//    #region === PROPERTY REACTIONS ========================================================

//    partial void OnSizePresetChanged(ButtonSizePreset value)
//    {
//        ApplySizePreset(value);
//        RecalculateOverallHeight();
//    }

//    partial void OnTitleChanged(string value)
//    {
//        RecalculateOverallHeight();
//    }

//    partial void OnShowTitleChanged(bool value)
//    {
//        RecalculateOverallHeight();
//    }

//    partial void OnTitleFontSizeChanged(double value)
//    {
//        RecalculateOverallHeight();
//    }

//    #endregion

//    #region === HIT TEST ==================================================================

//    public bool HitTestToggle(SKPoint point) => _toggleButtonRect.Contains(point);

//    public override bool HitTest(SKPoint point)
//    {
//        if (VisualRect.Contains(point))
//            return true;

//        if (IsDropDownOpen && _popupRect.Contains(point))
//            return true;

//        return false;
//    }

//    #endregion

//    #region === POINTER HOOKS =============================================================

//    //public override void OnPointerDown(in PointerContext ctx)
//    //{
//    //    if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
//    //        return;

//    //    if (IsDropDownOpen)
//    //    {
//    //        //if (_popupRect.Contains(ctx.WorldPoint))
//    //        //{
//    //        //    if (TryHitTestPopupItem(ctx.WorldPoint, out int popupIndex))
//    //        //    {
//    //        //        SelectPopupIndex(popupIndex);
//    //        //        return;
//    //        //    }
//    //        //}

//    //        if (_headerRect.Contains(ctx.WorldPoint))
//    //        {
//    //            SetPressedState(true);
//    //            SetHoverState(true);
//    //            if (_toggleButtonRect.Contains(ctx.WorldPoint))
//    //                _hoverToggle = true;

//    //            return;
//    //        }

//    //        IsDropDownOpen = false;
//    //        ResetInteractionState();
//    //        InvalidateVisuals();
//    //        return;
//    //    }

//    //    if (_headerRect.Contains(ctx.WorldPoint))
//    //    {
//    //        SetPressedState(true);
//    //        SetHoverState(true);
//    //        _hoverToggle = _toggleButtonRect.Contains(ctx.WorldPoint);
//    //    }
//    //}

//    public override void OnPointerDown(in PointerContext ctx)
//    {
//        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
//            return;

//        if (IsDropDownOpen)
//        {
//            if (_headerRect.Contains(ctx.WorldPoint))
//            {
//                SetPressedState(true);
//                SetHoverState(true);

//                if (_toggleButtonRect.Contains(ctx.WorldPoint))
//                    _hoverToggle = true;

//                return;
//            }

//            if (!_popupRect.Contains(ctx.WorldPoint))
//            {
//                IsDropDownOpen = false;
//                ResetInteractionState();
//                InvalidateVisuals();
//            }

//            return;
//        }

//        if (_headerRect.Contains(ctx.WorldPoint))
//        {
//            SetPressedState(true);
//            SetHoverState(true);
//            _hoverToggle = _toggleButtonRect.Contains(ctx.WorldPoint);
//        }
//    }

//    public override void OnPointerMove(in PointerContext ctx)
//    {
//        if (!ctx.IsLiveMode)
//        {
//            ResetInteractionState();
//            return;
//        }

//        bool insideHeader = _headerRect.Contains(ctx.WorldPoint);
//        bool insideToggle = _toggleButtonRect.Contains(ctx.WorldPoint);

//        SetHoverState(insideHeader);

//        if (!insideHeader && _isPressed)
//            SetPressedState(false);

//        _hoverToggle = insideToggle;

//        int hoverIndex = -1;
//        if (IsDropDownOpen && _popupRect.Contains(ctx.WorldPoint))
//        {
//            TryHitTestPopupItem(ctx.WorldPoint, out hoverIndex);
//        }

//        if (_hoverPopupIndex != hoverIndex)
//        {
//            _hoverPopupIndex = hoverIndex;
//            InvalidateVisuals();
//        }

//        if (insideHeader)
//        {
//            Mouse.OverrideCursor = Cursors.Hand;
//        }

//    }

//    //public override void OnPointerUp(in PointerContext ctx)
//    //{
//    //    if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
//    //        return;

//    //    bool insideHeader = _headerRect.Contains(ctx.WorldPoint);
//    //    bool commitClick = _isPressed && insideHeader;

//    //    SetPressedState(false);
//    //    SetHoverState(insideHeader);
//    //    _hoverToggle = _toggleButtonRect.Contains(ctx.WorldPoint);

//    //    if (commitClick)
//    //    {
//    //        IsDropDownOpen = !IsDropDownOpen;
//    //        _hoverPopupIndex = -1;
//    //        InvalidateVisuals();
//    //    }
//    //}

//    public override void OnPointerUp(in PointerContext ctx)
//    {
//        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
//            return;

//        if (IsDropDownOpen)
//        {
//            if (_popupRect.Contains(ctx.WorldPoint))
//            {
//                if (TryHitTestPopupItem(ctx.WorldPoint, out int popupIndex))
//                {
//                    SelectPopupIndex(popupIndex);
//                    return;
//                }
//            }

//            bool insideHeaderWhenOpen = _headerRect.Contains(ctx.WorldPoint);
//            bool commitHeaderClickWhenOpen = _isPressed && insideHeaderWhenOpen;

//            SetPressedState(false);
//            SetHoverState(insideHeaderWhenOpen);
//            _hoverToggle = _toggleButtonRect.Contains(ctx.WorldPoint);

//            if (commitHeaderClickWhenOpen)
//            {
//                IsDropDownOpen = false;
//                _hoverPopupIndex = -1;
//                InvalidateVisuals();
//            }

//            return;
//        }

//        bool insideHeader = _headerRect.Contains(ctx.WorldPoint);
//        bool commitClick = _isPressed && insideHeader;

//        SetPressedState(false);
//        SetHoverState(insideHeader);
//        _hoverToggle = _toggleButtonRect.Contains(ctx.WorldPoint);

//        if (commitClick)
//        {
//            IsDropDownOpen = true;
//            _hoverPopupIndex = -1;
//            InvalidateVisuals();
//        }
//    }

//    public override void OnPointerLeave()
//    {
//        ResetInteractionState();
//    }

//    #endregion

//    #region === RENDER ====================================================================

//    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
//    {
//        RecalculateOverallHeight();
//        _popupItems.Clear();

//        bool hasTitle = HasVisibleTitle();
//        float titleHeight = hasTitle ? GetMeasuredTitleHeight() : 0f;
//        float titleGap = hasTitle ? TitleGap : 0f;
//        float headerHeight = GetHeaderRowHeight();

//        var titleRect = hasTitle
//            ? new SKRect(layout.Left, layout.Top, layout.Right, layout.Top + titleHeight)
//            : SKRect.Empty;

//        _headerRect = new SKRect(
//            layout.Left,
//            layout.Top + titleHeight + titleGap,
//            layout.Right,
//            layout.Top + titleHeight + titleGap + headerHeight
//        );

//        _toggleButtonRect = new SKRect(
//            _headerRect.Right - headerHeight,
//            _headerRect.Top,
//            _headerRect.Right,
//            _headerRect.Bottom
//        );

//        DrawHeader(canvas, titleRect, _headerRect, ctx, hasTitle);

//        if (IsDropDownOpen)
//        {
//            DrawPopup(canvas, _headerRect, ctx);
//        }
//    }

//    #endregion

//    #region === DRAW HELPERS ==============================================================

//    private void DrawHeader(SKCanvas canvas, SKRect titleRect, SKRect headerRect, RenderContext ctx, bool hasTitle)
//    {
//        var (fillColor, resolvedBorderColor) = GetHeaderVisualColors(ctx);
//        var shadowOptions = GetVisualShadow(ctx);

//        SkiaRenderer.DrawRect(
//            canvas: canvas,
//            rect: headerRect,
//            cornerRadius: GetSafeCornerRadius(),
//            fillStyle: FillStyle.Solid,
//            fillColor: fillColor,
//            borderColor: resolvedBorderColor,
//            borderStyle: BorderStyle.Solid,
//            shadowOptions: shadowOptions,
//            borderWidth: 0.85f
//        );

//        if (hasTitle)
//        {
//            TextRenderer.Draw2(
//                canvas: canvas,
//                text: Title,
//                bounds: titleRect,
//                fontSize: TitleFontSize,
//                color: TitleColor,
//                padding: new Thickness(0),
//                fontWeight: FontWeights.Normal,
//                textAlignment: System.Windows.TextAlignment.Left
//            );
//        }

//        var contentRect = new SKRect(
//            headerRect.Left + (float)Padding.Left,
//            headerRect.Top,
//            _toggleButtonRect.Left - 4f,
//            headerRect.Bottom
//        );

//        bool hasSelection = SelectedIndex >= 0 && SelectedIndex < Items.Count;
//        string displayText = hasSelection ? Items[SelectedIndex] : Placeholder;
//        Color displayColor = hasSelection ? TextColor : PlaceholderColor;

//        TextRenderer.Draw2(
//            canvas: canvas,
//            text: displayText,
//            bounds: contentRect,
//            fontSize: FontSize,
//            color: displayColor,
//            padding: new Thickness(0),
//            fontWeight: FontWeight,
//            textAlignment: System.Windows.TextAlignment.Left
//        );

//        DrawChevronButton(canvas, _toggleButtonRect, ctx);
//    }

//    private void DrawChevronButton(SKCanvas canvas, SKRect rect, RenderContext ctx)
//    {
//        Color fill = _hoverToggle && ctx.LiveMode
//            ? Theme.ControlBG.Darken(0.02f)
//            : BackgroundColor;

//        SkiaRenderer.DrawRect(
//            canvas: canvas,
//            rect: new SKRect(rect.Left + 1f, rect.Top + 1f, rect.Right - 1f, rect.Bottom - 1f),
//            cornerRadius: Math.Max(2f, GetSafeCornerRadius() - 1f),
//            fillStyle: FillStyle.Solid,
//            fillColor: fill,
//            borderColor: Colors.Transparent,
//            borderStyle: BorderStyle.None,
//            shadowOptions: ShadowOptions.Default,
//            borderWidth: 0f
//        );

//        using var paint = new SKPaint
//        {
//            Color = TextColor.ToSKColor().WithAlpha(180),
//            Style = SKPaintStyle.Fill,
//            IsAntialias = true
//        };

//        float s = Math.Min(rect.Width, rect.Height) * 0.16f;
//        float cx = rect.MidX;
//        float cy = rect.MidY + (IsDropDownOpen ? -0.5f : 0.5f);

//        using var path = new SKPath();
//        if (IsDropDownOpen)
//        {
//            path.MoveTo(cx - s, cy + s * 0.35f);
//            path.LineTo(cx, cy - s);
//            path.LineTo(cx + s, cy + s * 0.35f);
//            path.Close();
//        }
//        else
//        {
//            path.MoveTo(cx - s, cy - s * 0.35f);
//            path.LineTo(cx, cy + s);
//            path.LineTo(cx + s, cy - s * 0.35f);
//            path.Close();
//        }

//        canvas.DrawPath(path, paint);
//    }

//    public void DrawPopup(SKCanvas canvas, SKRect headerRect, RenderContext ctx)
//    {
//        float popupTop = headerRect.Bottom + PopupGap;
//        float itemHeight = GetPopupItemHeight();
//        float popupHeight = Items.Count * itemHeight + PopupOuterPadding * 2f;

//        _popupRect = new SKRect(
//            headerRect.Left,
//            popupTop,
//            headerRect.Right,
//            popupTop + popupHeight
//        );

//        SkiaRenderer.DrawRect(
//            canvas: canvas,
//            rect: _popupRect,
//            cornerRadius: Math.Max(4f, GetSafeCornerRadius()),
//            fillStyle: FillStyle.Solid,
//            fillColor: PopupBackgroundColor,
//            borderColor: PopupBorderColor,
//            borderStyle: BorderStyle.Solid,
//            shadowOptions: GetPopupShadow(),
//            borderWidth: 0.9f
//        );

//        float y = _popupRect.Top + PopupOuterPadding;

//        for (int i = 0; i < Items.Count; i++)
//        {
//            var itemRect = new SKRect(
//                _popupRect.Left + 4f,
//                y,
//                _popupRect.Right - 4f,
//                y + itemHeight
//            );

//            _popupItems.Add(new PopupItemHitTarget(itemRect, i));

//            bool isSelected = i == SelectedIndex;
//            bool isHover = i == _hoverPopupIndex;

//            if (isSelected)
//            {
//                SkiaRenderer.DrawRect(
//                    canvas: canvas,
//                    rect: itemRect,
//                    cornerRadius: 3f,
//                    fillStyle: FillStyle.Solid,
//                    fillColor: SelectedItemBackgroundColor,
//                    borderColor: Colors.Transparent,
//                    borderStyle: BorderStyle.None,
//                    shadowOptions: ShadowOptions.Default,
//                    borderWidth: 0f
//                );
//            }
//            else if (isHover && ctx.LiveMode)
//            {
//                SkiaRenderer.DrawRect(
//                    canvas: canvas,
//                    rect: itemRect,
//                    cornerRadius: 3f,
//                    fillStyle: FillStyle.Solid,
//                    fillColor: HoverItemBackgroundColor,
//                    borderColor: Colors.Transparent,
//                    borderStyle: BorderStyle.None,
//                    shadowOptions: ShadowOptions.Default,
//                    borderWidth: 0f
//                );
//            }

//            TextRenderer.Draw2(
//                canvas: canvas,
//                text: Items[i],
//                bounds: itemRect,
//                fontSize: FontSize,
//                color: isSelected ? SelectedItemTextColor : TextColor,
//                padding: new Thickness(10, 0, 10, 0),
//                fontWeight: isSelected ? FontWeights.Medium : FontWeight,
//                textAlignment: System.Windows.TextAlignment.Left
//            );

//            y += itemHeight;
//        }
//    }

//    #endregion

//    #region === HELPERS ===================================================================

//    private void ApplySizePreset(ButtonSizePreset preset)
//    {
//        if (_applyingSizePreset)
//            return;

//        _applyingSizePreset = true;

//        try
//        {
//            switch (preset)
//            {
//                case ButtonSizePreset.Small:
//                    Height = 26f;
//                    MinHeight = 26f;
//                    FontSize = 12d;
//                    TitleFontSize = 10d;
//                    Padding = new Thickness(8, 0, 8, 0);
//                    CornerRadius = 4f;
//                    break;

//                case ButtonSizePreset.Large:
//                    Height = 36f;
//                    MinHeight = 36f;
//                    FontSize = 14d;
//                    TitleFontSize = 12d;
//                    Padding = new Thickness(12, 0, 12, 0);
//                    CornerRadius = 5f;
//                    break;

//                default:
//                    Height = 30f;
//                    MinHeight = 30f;
//                    FontSize = 13d;
//                    TitleFontSize = 11d;
//                    Padding = new Thickness(10, 0, 10, 0);
//                    CornerRadius = 4f;
//                    break;
//            }

//            if (Width < MinWidth)
//                Width = MinWidth;
//        }
//        finally
//        {
//            _applyingSizePreset = false;
//        }
//    }

//    private void RecalculateOverallHeight()
//    {
//        float headerHeight = GetHeaderRowHeight();
//        float titleExtra = HasVisibleTitle() ? GetMeasuredTitleHeight() + TitleGap : 0f;
//        float desiredHeight = Math.Clamp(headerHeight + titleExtra, MinHeight, MaxHeight);

//        if (Math.Abs(Height - desiredHeight) > 0.5f)
//            Height = desiredHeight;
//    }

//    private float GetHeaderRowHeight()
//    {
//        return SizePreset switch
//        {
//            ButtonSizePreset.Small => 26f,
//            ButtonSizePreset.Large => 36f,
//            _ => 30f
//        };
//    }

//    private float GetPopupItemHeight()
//    {
//        return SizePreset switch
//        {
//            ButtonSizePreset.Small => 24f,
//            ButtonSizePreset.Large => 34f,
//            _ => 28f
//        };
//    }

//    private bool HasVisibleTitle()
//    {
//        return ShowTitle && !string.IsNullOrWhiteSpace(Title);
//    }

//    private float GetMeasuredTitleHeight()
//    {
//        var style = new Topten.RichTextKit.Style
//        {
//            FontFamily = Theme.FontFamily,
//            FontSize = (float)TitleFontSize,
//            FontWeight = FontWeights.Normal.ToFontWeightValue(),
//            TextColor = TitleColor.ToSKColor()
//        };

//        var tb = new Topten.RichTextKit.TextBlock
//        {
//            MaxWidth = Math.Max(1f, Width),
//            Alignment = Topten.RichTextKit.TextAlignment.Left,
//            EllipsisEnabled = true
//        };

//        tb.AddText(string.IsNullOrWhiteSpace(Title) ? " " : Title, style);
//        tb.Layout();

//        return Math.Max(12f, tb.MeasuredHeight + 2f);
//    }

//    private void NormalizeSelection()
//    {
//        if (Items == null || Items.Count == 0)
//        {
//            selectedIndex = -1;
//            SelectedItem = string.Empty;
//            return;
//        }

//        if (SelectedIndex < 0 || SelectedIndex >= Items.Count)
//        {
//            selectedIndex = -1;
//            SelectedItem = string.Empty;
//            OnPropertyChanged(nameof(SelectedIndex));
//            return;
//        }

//        SelectedItem = Items[SelectedIndex];
//    }

//    private void SelectPopupIndex(int index)
//    {
//        if (index < 0 || index >= Items.Count)
//            return;

//        SelectedIndex = index;
//        SelectedItem = Items[index];
//        IsDropDownOpen = false;
//        _hoverPopupIndex = -1;
//        InvalidateVisuals();
//    }

//    private bool TryHitTestPopupItem(SKPoint point, out int index)
//    {
//        foreach (var item in _popupItems)
//        {
//            if (item.Rect.Contains(point))
//            {
//                index = item.Index;
//                return true;
//            }
//        }

//        index = -1;
//        return false;
//    }

//    private void ResetInteractionState()
//    {
//        bool changed = false;

//        if (_isHovered)
//        {
//            _isHovered = false;
//            changed = true;
//        }

//        if (_isPressed)
//        {
//            _isPressed = false;
//            changed = true;
//        }

//        if (_hoverToggle)
//        {
//            _hoverToggle = false;
//            changed = true;
//        }

//        if (_hoverPopupIndex >= 0)
//        {
//            _hoverPopupIndex = -1;
//            changed = true;
//        }

//        if (changed)
//            InvalidateVisuals();

//        Mouse.OverrideCursor = null;    
//    }

//    private void SetHoverState(bool value)
//    {
//        if (_isHovered == value)
//            return;

//        _isHovered = value;
//        InvalidateVisuals();
//    }

//    private void SetPressedState(bool value)
//    {
//        if (_isPressed == value)
//            return;

//        _isPressed = value;
//        InvalidateVisuals();
//    }

//    private void InvalidateVisuals()
//    {
//        MSG.UI.InvalidateDesigner();
//    }

//    private (Color FillColor, Color BorderColor) GetHeaderVisualColors(RenderContext ctx)
//    {
//        Color fillColor = BackgroundColor;
//        Color resolvedBorderColor = BorderColor;

//        if (ctx.LiveMode && _isHovered)
//        {
//            fillColor = fillColor.Darken(0.015f);
//            resolvedBorderColor = resolvedBorderColor.Darken(0.04f);
//        }

//        if (ctx.LiveMode && _isPressed)
//        {
//            fillColor = fillColor.Darken(0.03f);
//            resolvedBorderColor = resolvedBorderColor.Darken(0.08f);
//        }

//        return (fillColor, resolvedBorderColor);
//    }

//    private ShadowOptions GetVisualShadow(RenderContext ctx)
//    {
//        int safeElevation = Math.Clamp(Elevation, 0, 5);

//        if (safeElevation <= 0)
//            return ShadowOptions.Default;

//        if (ctx.LiveMode && _isPressed)
//            return GetElevation(Math.Max(0, safeElevation - 1));

//        return GetElevation(safeElevation);
//    }

//    private ShadowOptions GetPopupShadow()
//    {
//        return new ShadowOptions
//        {
//            Color = SKColors.Black.WithAlpha(40),
//            Dx = 0f,
//            Dy = 2f,
//            Sigma = 3f
//        };
//    }

//    private float GetSafeCornerRadius()
//    {
//        return Math.Clamp(CornerRadius, 0f, 12f);
//    }

//    #endregion

//    #region === PRIVATE TYPES ==============================================================

//    private readonly record struct PopupItemHitTarget(SKRect Rect, int Index);

//    #endregion
//}

//#endregion
