// ======================================================================================
// FILE: Mockup.Controls/SearchBox.cs
//
// PURPOSE:
// - Modern SearchBox control for the mockup designer.
// - Visual style aligned with TextBox / ComboBox controls.
// - Supports optional title, placeholder, search icon and optional clear button.
// - Compact light-mode style with hover feedback in LiveMode.
//
// PROJECT: Mockup.Controls
// GROUP: Input
//
// NOTES:
// - This is a visual mockup control, not a real editable input.
// - The control itself does not edit text in preview; it only renders state.
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

#region === SEARCH BOX ===

[ControlType(displayName: "Search Box", group: "Input Fields")]
public partial class SearchBox : DesignControl
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
    [property: System.ComponentModel.DisplayName("Text")]
    private string text = string.Empty;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Placeholder")]
    private string placeholder = "Search...";

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
    [property: System.ComponentModel.DisplayName("Icon Color")]
    private Color iconColor = Theme.Text.Lighten(0.10f);

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
    private double titleFontSize = 11d;

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
    private Thickness padding = new(10, 0, 2, 0);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Icon Spacing")]
    private float iconSpacing = 8f;

    partial void OnIconSpacingChanged(float value)
    {
        iconSpacing = Math.Clamp(value, 2f, 20f);
    }

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
    [property: System.ComponentModel.DisplayName("Show Search Icon")]
    private bool showSearchIcon = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Clear Button")]
    private bool showClearButton = true;

    #endregion

    #region === RUNTIME STATE ===

    [JsonIgnore, Browsable(false)]
    private bool _isHovered;

    [JsonIgnore, Browsable(false)]
    private bool _isPressed;

    [JsonIgnore, Browsable(false)]
    private bool _hoverClearButton;

    [JsonIgnore, Browsable(false)]
    private bool _applyingSizePreset;

    [JsonIgnore, Browsable(false)]
    private SKRect _headerRect;

    [JsonIgnore, Browsable(false)]
    private SKRect _clearButtonRect;

    [JsonIgnore, Browsable(false)]
    private SKRect _searchIconRect;

    private const float TitleGap = 2f;

    #endregion

    #region === CTOR ===

    public SearchBox()
    {
        IsActionControl = true;

        Name = "SearchBox";
        ResizeStyle = ResizeStyles.WidthOnly;

        Width = 160f;
        Height = 30f;

        MinWidth = 80f;
        MinHeight = 26f;

        MaxWidth = 600f;
        MaxHeight = 120f;

        ApplySizePreset(SizePreset);
        RecalculateOverallHeight();
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

        if (!_headerRect.Contains(ctx.WorldPoint))
            return;

        _isPressed = true;
        _isHovered = true;
        _hoverClearButton = CanShowClearButton() && _clearButtonRect.Contains(ctx.WorldPoint);
        InvalidateVisuals();
    }

    public override void OnPointerMove(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode)
        {
            ResetInteractionState();
            return;
        }

        bool insideHeader = _headerRect.Contains(ctx.WorldPoint);
        bool hoverClear = CanShowClearButton() && _clearButtonRect.Contains(ctx.WorldPoint);

        if (_isHovered != insideHeader)
        {
            _isHovered = insideHeader;
            InvalidateVisuals();
        }

        if (_hoverClearButton != hoverClear)
        {
            _hoverClearButton = hoverClear;
            InvalidateVisuals();
        }

        if (insideHeader)
            Mouse.OverrideCursor = Cursors.Hand;

        if (!insideHeader && _isPressed)
        {
            _isPressed = false;
            InvalidateVisuals();
        }
    }

    public override void OnPointerUp(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;

        bool insideHeader = _headerRect.Contains(ctx.WorldPoint);
        bool commitClick = _isPressed && insideHeader;
        bool hitClear = CanShowClearButton() && _clearButtonRect.Contains(ctx.WorldPoint);

        _isPressed = false;
        _isHovered = insideHeader;
        _hoverClearButton = hitClear;

        if (commitClick && hitClear)
        {
            Text = string.Empty;
        }

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

        BuildInteractiveRects(_headerRect);

        DrawHeader(canvas, titleRect, _headerRect, ctx, hasTitle);
    }

    #endregion

    #region === DRAW HELPERS ===

    private void DrawHeader(SKCanvas canvas, SKRect titleRect, SKRect headerRect, RenderContext ctx, bool hasTitle)
    {
        var (fillColor, resolvedBorderColor) = GetHeaderVisualColors(ctx);

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: headerRect,
            cornerRadius: GetSafeCornerRadius(),
            fillStyle: FillStyle.Solid,
            fillColor: fillColor,
            borderColor: resolvedBorderColor,
            borderStyle: BorderStyle.Solid,
            shadowOptions: GetVisualShadow(ctx),
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

        DrawSearchIcon(canvas);

        if (CanShowClearButton())
            DrawClearButton(canvas, ctx);

        var contentRect = new SKRect(
            ShowSearchIcon ? _searchIconRect.Right + IconSpacing : headerRect.Left + (float)Padding.Left,
            headerRect.Top,
            CanShowClearButton() ? _clearButtonRect.Left - IconSpacing : headerRect.Right - (float)Padding.Right,
            headerRect.Bottom
        );

        bool hasText = !string.IsNullOrWhiteSpace(Text);
        string displayText = hasText ? Text : Placeholder;
        Color displayColor = hasText ? TextColor : PlaceholderColor;

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
    }

    private void DrawSearchIcon(SKCanvas canvas)
    {
        if (!ShowSearchIcon)
            return;

        using var stroke = new SKPaint
        {
            Color = IconColor.ToSKColor().WithAlpha(185),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.4f,
            IsAntialias = true
        };

        float radius = _searchIconRect.Width * 0.25f;
        float cx = _searchIconRect.Left + _searchIconRect.Width * 0.2f;
        float cy = _searchIconRect.Top + _searchIconRect.Height * 0.45f;

        canvas.DrawCircle(cx, cy, radius, stroke);
        canvas.DrawLine(
            cx + radius * 0.8f,
            cy + radius * 0.35f,
            _searchIconRect.Right - _searchIconRect.Width * 0.18f,
            _searchIconRect.Bottom - _searchIconRect.Height * 0.28f,
            stroke
        );
    }

    private void DrawClearButton(SKCanvas canvas, RenderContext ctx)
    {
        if (!CanShowClearButton())
            return;

        Color fill = _hoverClearButton && ctx.LiveMode
            ? Theme.ControlBG.Darken(0.03f)
            : Colors.Transparent;

        if (fill.A > 0)
        {
            SkiaRenderer.DrawRect(
                canvas: canvas,
                rect: _clearButtonRect,
                cornerRadius: Math.Max(2f, GetSafeCornerRadius() - 1f),
                fillStyle: FillStyle.Solid,
                fillColor: fill,
                borderColor: Colors.Transparent,
                borderStyle: BorderStyle.None,
                shadowOptions: ShadowOptions.Default,
                borderWidth: 0f
            );
        }

        using var paint = new SKPaint
        {
            Color = Colors.Red.ToSKColor().WithAlpha(185),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f,
            StrokeCap = SKStrokeCap.Round,
            IsAntialias = true
        };

        float inset = _clearButtonRect.Width * 0.35f;

        canvas.DrawLine(
            _clearButtonRect.Left + inset,
            _clearButtonRect.Top + inset,
            _clearButtonRect.Right - inset,
            _clearButtonRect.Bottom - inset,
            paint
        );

        canvas.DrawLine(
            _clearButtonRect.Right - inset,
            _clearButtonRect.Top + inset,
            _clearButtonRect.Left + inset,
            _clearButtonRect.Bottom - inset,
            paint
        );
    }

    private void BuildInteractiveRects(SKRect headerRect)
    {
        float insetY = 3f;
        float side = Math.Max(10f, headerRect.Height - insetY * 2f);
        float left = headerRect.Left + (float)Padding.Left;

        _searchIconRect = new SKRect(
            left,
            headerRect.Top + insetY,
            left + side,
            headerRect.Bottom - insetY
        );

        float clearRight = headerRect.Right - (float)Padding.Right;
        _clearButtonRect = new SKRect(
            clearRight - side,
            headerRect.Top + insetY,
            clearRight,
            headerRect.Bottom - insetY
        );
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
                    Padding = new Thickness(8, 0, 2, 0);
                    CornerRadius = 4f;
                    break;

                case ButtonSizePreset.Large:
                    Height = 36f;
                    MinHeight = 36f;
                    FontSize = 14d;
                    TitleFontSize = 12d;
                    Padding = new Thickness(12, 0, 2, 0);
                    CornerRadius = 5f;
                    break;

                default:
                    Height = 30f;
                    MinHeight = 30f;
                    FontSize = 13d;
                    TitleFontSize = 11d;
                    Padding = new Thickness(10, 0, 2, 0);
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

    private bool HasVisibleTitle()
    {
        return ShowTitle && !string.IsNullOrWhiteSpace(Title);
    }

    private bool CanShowClearButton()
    {
        return ShowClearButton && !string.IsNullOrWhiteSpace(Text);
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

        if (_hoverClearButton)
        {
            _hoverClearButton = false;
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

    private float GetSafeCornerRadius()
    {
        return Math.Clamp(CornerRadius, 0f, 12f);
    }

    #endregion
}

#endregion