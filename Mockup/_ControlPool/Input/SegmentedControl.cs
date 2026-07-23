// ======================================================================================
// FILE: Mockup.Controls/SegmentedControl.cs
//
// PURPOSE:
// - Modern segmented control for the mockup designer.
// - Visual style aligned with Button / ComboBox / ListBox / ToggleSwitch controls.
// - Supports a list of text segments with a single selected segment.
// - Uses explicit segment rects for rendering and hit testing.
//
// NOTES:
// - This is a visual mockup control, not a native platform segmented control.
// - Selection changes only in LiveMode on left mouse click.
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

#region === SEGMENTED CONTROL ===

[ControlType(displayName: "Segmented Control", group: "Selection")]
public partial class SegmentedControl : DesignControl
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
    private ObservableCollection<string> items = new() { "First", "Second", "Third" };

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

        int clamped = Math.Clamp(value, 0, Items.Count - 1);

        if (selectedIndex != clamped)
        {
            selectedIndex = clamped;
            OnPropertyChanged(nameof(SelectedIndex));
        }
    }

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
    private Color borderColor = Color.FromRgb(235, 235, 235); //Theme.ControlBorder;

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
    [property: System.ComponentModel.DisplayName("Selected Segment Background")]
    private Color selectedSegmentBackgroundColor = SkiaRenderer.SelectionColor;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Selected Segment Color")]
    private Color selectedSegmentTextColor = Colors.White;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Hover Segment Background")]
    private Color hoverSegmentBackgroundColor = Theme.ControlBG.Darken(0.03f);

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
    private Thickness padding = new(2, 2, 2, 2);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Segment Spacing")]
    private float segmentSpacing = 2f;

    partial void OnSegmentSpacingChanged(float value)
    {
        segmentSpacing = Math.Clamp(value, 0f, 12f);
    }

    #endregion

    #region === BEHAVIOR ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Title")]
    private bool showTitle = true;

    #endregion

    #region === RUNTIME STATE ===

    [JsonIgnore, Browsable(false)]
    private bool _isPressed;

    [JsonIgnore, Browsable(false)]
    private int _hoverSegmentIndex = -1;

    [JsonIgnore, Browsable(false)]
    private bool _applyingSizePreset;

    [JsonIgnore, Browsable(false)]
    private readonly List<SegmentHitTarget> _segmentRects = new();

    private const float TitleGap = 2f;

    #endregion

    #region === CTOR ===

    public SegmentedControl()
    {
        IsActionControl = true;

        Name = "SegmentedControl";
        ResizeStyle = ResizeStyles.WidthOnly;

        Width = 180f;
        Height = 30f;

        MinWidth = 90f;
        MinHeight = 26f;

        MaxWidth = 700f;
        MaxHeight = 160f;

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

        int hoverIndex = HitTestSegmentIndex(ctx.WorldPoint);
        if (_hoverSegmentIndex != hoverIndex)
        {
            _hoverSegmentIndex = hoverIndex;
            Mouse.OverrideCursor = Cursors.Hand;
            InvalidateVisuals();
        }
    }

    public override void OnPointerUp(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;

        int hitIndex = HitTestSegmentIndex(ctx.WorldPoint);
        bool commitClick = _isPressed && hitIndex >= 0;

        _isPressed = false;

        if (commitClick)
        {
            SelectedIndex = hitIndex;
            InvalidateVisuals();
        }
        else
        {
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
        _segmentRects.Clear();

        bool hasTitle = HasVisibleTitle();
        float titleHeight = hasTitle ? GetMeasuredTitleHeight() : 0f;
        float titleGap = hasTitle ? TitleGap : 0f;
        float bodyHeight = GetBodyHeight();

        var titleRect = hasTitle
            ? new SKRect(layout.Left + 2, layout.Top, layout.Right, layout.Top + titleHeight)
            : SKRect.Empty;

        var bodyRect = new SKRect(
            layout.Left,
            layout.Top + titleHeight + titleGap,
            layout.Right,
            layout.Top + titleHeight + titleGap + bodyHeight
        );

        DrawBody(canvas, bodyRect, ctx);

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

        if (Items == null || Items.Count == 0)
            return;

        DrawSegments(canvas, bodyRect, ctx);
    }

    #endregion

    #region === DRAW HELPERS ===

    private void DrawBody(SKCanvas canvas, SKRect bodyRect, RenderContext ctx)
    {
        Color fillColor = BackgroundColor;
        Color resolvedBorderColor = BorderColor;

        if (ctx.LiveMode && _hoverSegmentIndex >= 0)
        {
            resolvedBorderColor = resolvedBorderColor.Darken(0.03f);
        }

        ShadowOptions shadowOptions = Elevation > 0 ? GetElevation(Elevation) : ShadowOptions.Default;

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: bodyRect,
            cornerRadius: GetSafeCornerRadius(),
            fillStyle: FillStyle.Solid,
            fillColor: fillColor,
            borderColor: resolvedBorderColor,
            borderStyle: BorderStyle.Solid,
            shadowOptions: shadowOptions,
            borderWidth: 0.85f
        );
    }

    private void DrawSegments(SKCanvas canvas, SKRect bodyRect, RenderContext ctx)
    {
        int count = Items.Count;
        float leftInset = (float)Padding.Left;
        float topInset = (float)Padding.Top;
        float rightInset = (float)Padding.Right;
        float bottomInset = (float)Padding.Bottom;

        float contentLeft = bodyRect.Left + leftInset;
        float contentTop = bodyRect.Top + topInset;
        float contentRight = bodyRect.Right - rightInset;
        float contentBottom = bodyRect.Bottom - bottomInset;
        float contentWidth = contentRight - contentLeft;

        if (contentWidth <= 1f || contentBottom - contentTop <= 1f)
            return;

        float totalSpacing = SegmentSpacing * Math.Max(0, count - 1);
        float segmentWidth = (contentWidth - totalSpacing) / count;

        if (segmentWidth <= 1f)
            return;

        float x = contentLeft;

        for (int i = 0; i < count; i++)
        {
            var segmentRect = new SKRect(
                x,
                contentTop,
                x + segmentWidth,
                contentBottom
            );

            _segmentRects.Add(new SegmentHitTarget(segmentRect, i));

            bool isSelected = i == SelectedIndex;
            bool isHover = ctx.LiveMode && i == _hoverSegmentIndex;

            if (isSelected)
            {
                SkiaRenderer.DrawRect(
                    canvas: canvas,
                    rect: segmentRect,
                    cornerRadius: Math.Max(3f, GetSafeCornerRadius() - 1f),
                    fillStyle: FillStyle.Solid,
                    fillColor: SelectedSegmentBackgroundColor,
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
                    rect: segmentRect,
                    cornerRadius: Math.Max(3f, GetSafeCornerRadius() - 1f),
                    fillStyle: FillStyle.Solid,
                    fillColor: HoverSegmentBackgroundColor,
                    borderColor: Colors.Transparent,
                    borderStyle: BorderStyle.None,
                    shadowOptions: ShadowOptions.Default,
                    borderWidth: 0f
                );
            }

            var textRect = new SKRect(
                           x,
                           contentTop + 2,
                           x + segmentWidth,
                           contentBottom
                       );
            TextRenderer.Draw2(
                canvas: canvas,
                text: Items[i],
                bounds: textRect,
                fontSize: FontSize,
                color: isSelected ? SelectedSegmentTextColor : TextColor,
                padding: new Thickness(8, 0, 8, 0),
                fontWeight: isSelected ? FontWeights.Medium : FontWeight,
                textAlignment: System.Windows.TextAlignment.Center
            );

            x += segmentWidth + SegmentSpacing;
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
                    TitleFontSize = 11d;
                    Padding = new Thickness(2, 2, 2, 2);
                    CornerRadius = 4f;
                    break;

                case ButtonSizePreset.Large:
                    Height = 36f;
                    MinHeight = 36f;
                    FontSize = 14d;
                    TitleFontSize = 12d;
                    Padding = new Thickness(2, 2, 2, 2);
                    CornerRadius = 5f;
                    break;

                default:
                    Height = 30f;
                    MinHeight = 30f;
                    FontSize = 13d;
                    TitleFontSize = 12d;
                    Padding = new Thickness(2, 2, 2, 2);
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
        float bodyHeight = GetBodyHeight();
        float titleExtra = HasVisibleTitle() ? GetMeasuredTitleHeight() + TitleGap : 0f;
        float desiredHeight = Math.Clamp(bodyHeight + titleExtra, MinHeight, MaxHeight);

        if (Math.Abs(Height - desiredHeight) > 0.5f)
            Height = desiredHeight;
    }

    private float GetBodyHeight()
    {
        return SizePreset switch
        {
            ButtonSizePreset.Small => 26f,
            ButtonSizePreset.Large => 36f,
            _ => 30f
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
            return;
        }

        if (SelectedIndex < 0 || SelectedIndex >= Items.Count)
        {
            SelectedIndex = 0;
            OnPropertyChanged(nameof(SelectedIndex));
        }
    }

    private int HitTestSegmentIndex(SKPoint point)
    {
        foreach (var segment in _segmentRects)
        {
            if (segment.Rect.Contains(point))
                return segment.Index;
        }

        return -1;
    }

    private void ResetInteractionState()
    {
        bool changed = false;

        if (_hoverSegmentIndex >= 0)
        {
            _hoverSegmentIndex = -1;
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

    private float GetSafeCornerRadius()
    {
        return Math.Clamp(CornerRadius, 0f, 12f);
    }

    #endregion

    #region === PRIVATE TYPES ===

    private readonly record struct SegmentHitTarget(SKRect Rect, int Index);

    #endregion
}

#endregion
