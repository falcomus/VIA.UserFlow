// ======================================================================================
// FILE: Mockup/_ControlPool/Input/TextBox.cs
//
// PURPOSE:
// - Single-line input field in the unified light control style.
// - Supports title, placeholder and simple runtime hover / pressed states in LiveMode.
// - Supports an optional selectable left icon.
// - Uses RichTextKit-based text rendering through TextRenderer.
// - Keeps the model lightweight and PropertyGrid-friendly.
//
// NOTES:
// - This is a visual mockup control, not a real text input widget.
// - The input row follows the compact button height logic.
// - If a title is shown, the overall control height grows automatically with the
//   measured title area.
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.Domain.Registry;
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
using RichStyle = Topten.RichTextKit.Style;
using RichTextAlignment = Topten.RichTextKit.TextAlignment;

namespace Mockup.Controls;

#region === TEXT BOX ======================================================================

[ControlType(displayName: "Text Box", group: "Input Fields")]
public partial class TextBox : DesignControl
{
    #region === CONTENT ===================================================================

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Text")]
    private string text = "Text";

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Title")]
    private string title = string.Empty;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Placeholder")]
    private string placeholder = "Placeholder";

    #endregion

    #region === APPEARANCE ================================================================

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

    #endregion

    #region === TYPOGRAPHY ================================================================

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Size")]
    private double fontSize = 13.5d;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Title Font Size")]
    private double titleFontSize = 13d;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Weight")]
    private FontWeight fontWeight = FontWeights.Normal;

    #endregion

    #region === LAYOUT ====================================================================

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

    #region === ICON ======================================================================

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Icon")]
    [property: System.ComponentModel.DisplayName("Icon")]
    private ImageRef? icon;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Icon")]
    [property: System.ComponentModel.DisplayName("Icon Size")]
    private float iconSize = 16f;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Icon")]
    [property: System.ComponentModel.DisplayName("Icon Spacing")]
    private float iconSpacing = 8f;

    [JsonIgnore, Browsable(false)]
    public bool HasIcon => ShowLeftIcon && Icon is not null && !string.IsNullOrWhiteSpace(Icon.Id);

    #endregion

    #region === BEHAVIOR ==================================================================

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Title")]
    private bool showTitle = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Left Icon")]
    private bool showLeftIcon = true;

    #endregion

    #region === RUNTIME INTERACTION =======================================================

    [JsonIgnore, Browsable(false)]
    private bool _isHovered;

    [JsonIgnore, Browsable(false)]
    private bool _isPressed;

    [JsonIgnore, Browsable(false)]
    private bool _applyingSizePreset;

    #endregion

    #region === CTOR ======================================================================

    public TextBox()
    {
        Name = "TextBox";
        ResizeStyle = ResizeStyles.ResizeAll;

        Width = 160f;
        Height = 30f;

        MinWidth = 80f;
        MinHeight = 26f;

        MaxWidth = 600f;
        MaxHeight = 240f;

        ApplySizePreset(SizePreset);
        RecalculateOverallHeight();
    }

    #endregion

    #region === PROPERTY REACTIONS ========================================================

    partial void OnSizePresetChanged(ButtonSizePreset value)
    {
        ApplySizePreset(value);
        RecalculateOverallHeight();
    }

    partial void OnTitleChanged(string value)
    {
        RecalculateOverallHeight();
        InvalidateVisuals();
    }

    partial void OnShowTitleChanged(bool value)
    {
        RecalculateOverallHeight();
        InvalidateVisuals();
    }

    partial void OnTitleFontSizeChanged(double value)
    {
        RecalculateOverallHeight();
        InvalidateVisuals();
    }

    partial void OnTextChanged(string value)
    {
        InvalidateVisuals();
    }

    partial void OnPlaceholderChanged(string value)
    {
        InvalidateVisuals();
    }

    partial void OnIconChanged(ImageRef? value)
    {
        InvalidateVisuals();
    }

    partial void OnShowLeftIconChanged(bool value)
    {
        InvalidateVisuals();
    }

    partial void OnIconSizeChanged(float value)
    {
        IconSize = Math.Clamp(value, 8f, 48f);
        InvalidateVisuals();
    }

    partial void OnIconSpacingChanged(float value)
    {
        IconSpacing = Math.Clamp(value, 0f, 32f);
        InvalidateVisuals();
    }

    #endregion

    #region === POINTER EVENTS ============================================================

    public override void OnPointerDown(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode)
            return;

        if (ctx.Button != MouseButton.Left)
            return;

        if (!VisualRect.Contains(ctx.WorldPoint))
            return;

        SetPressedState(true);
        SetHoverState(true);
    }

    public override void OnPointerMove(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode)
        {
            ResetInteractionState();
            return;
        }

        bool isInside = VisualRect.Contains(ctx.WorldPoint);
        SetHoverState(isInside);

        if (!isInside && _isPressed)
            SetPressedState(false);

        if (isInside)
            Mouse.OverrideCursor = Cursors.Hand;
    }

    public override void OnPointerUp(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode)
        {
            ResetInteractionState();
            return;
        }

        bool isInside = VisualRect.Contains(ctx.WorldPoint);

        SetPressedState(false);
        SetHoverState(isInside);
    }

    public override void OnPointerLeave()
    {
        ResetInteractionState();
    }

    #endregion

    #region === RENDER ====================================================================

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        RecalculateOverallHeight();

        bool hasTitle = HasVisibleTitle();
        float titleHeight = hasTitle ? GetMeasuredTitleHeight() : 0f;
        float titleGap = hasTitle ? 2f : 0f;

        var titleRect = hasTitle
            ? new SKRect(layout.Left, layout.Top, layout.Right, layout.Top + titleHeight)
            : SKRect.Empty;

        var inputRect = new SKRect(
            layout.Left,
            layout.Top + titleHeight + titleGap,
            layout.Right,
            layout.Bottom
        );

        if (inputRect.Height < 8f)
            return;

        var (fillColor, resolvedBorderColor) = GetVisualColors(ctx);
        var shadowOptions = GetVisualShadow(ctx);

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: inputRect,
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

        var contentRect = CreateContentRect(inputRect, Padding);
        if (!IsUsableRect(contentRect))
            return;

        bool hasText = !string.IsNullOrWhiteSpace(Text);
        bool hasIcon = HasIcon;

        if (!hasText && !hasIcon && string.IsNullOrWhiteSpace(Placeholder))
            return;

        float resolvedIconSize = MathF.Min(GetSafeIconSize(), Math.Max(8f, contentRect.Height));
        float spacing = hasIcon ? GetSafeIconSpacing() : 0f;

        SKRect textRect = contentRect;

        if (hasIcon)
        {
            var iconRect = new SKRect(
                contentRect.Left,
                contentRect.MidY - resolvedIconSize / 2f,
                contentRect.Left + resolvedIconSize,
                contentRect.MidY + resolvedIconSize / 2f
            );

            if (IsUsableRect(iconRect))
            {
                SkiaRenderer.RenderSVGIcon(
                    canvas,
                    iconRect,
                    Icon,
                    Colors.Transparent,
                    Colors.Transparent,
                    IconColor,
                    1,
                    false,
                    0,
                    0
                );
            }

            textRect = new SKRect(
                iconRect.Right + spacing,
                contentRect.Top,
                contentRect.Right,
                contentRect.Bottom
            );
        }

        if (!IsUsableRect(textRect))
            return;

        TextRenderer.Draw2(
            canvas: canvas,
            text: hasText ? Text : Placeholder,
            bounds: textRect,
            fontSize: FontSize,
            color: hasText ? TextColor : PlaceholderColor,
            padding: new Thickness(0),
            fontWeight: FontWeight,
            textAlignment: System.Windows.TextAlignment.Left
        );
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === HELPERS ===================================================================

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
                    FontSize = 12.5d;
                    TitleFontSize = 12d;
                    IconSize = 14f;
                    Padding = new Thickness(8, 0, 8, 0);
                    CornerRadius = 4f;
                    break;

                case ButtonSizePreset.Large:
                    Height = 36f;
                    MinHeight = 36f;
                    FontSize = 14.5d;
                    TitleFontSize = 14d;
                    IconSize = 18f;
                    Padding = new Thickness(12, 0, 12, 0);
                    CornerRadius = 5f;
                    break;

                default:
                    Height = 30f;
                    MinHeight = 30f;
                    FontSize = 13.5d;
                    TitleFontSize = 13d;
                    IconSize = 16f;
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
        float inputRowHeight = GetInputRowHeight();
        float titleExtra = 0f;

        if (HasVisibleTitle())
            titleExtra = GetMeasuredTitleHeight() + 2f;

        float desiredHeight = inputRowHeight + titleExtra;
        desiredHeight = Math.Clamp(desiredHeight, MinHeight, MaxHeight);

        if (Math.Abs(Height - desiredHeight) > 0.5f)
            Height = desiredHeight;
    }

    private bool HasVisibleTitle()
    {
        return ShowTitle && !string.IsNullOrWhiteSpace(Title);
    }

    private float GetInputRowHeight()
    {
        return SizePreset switch
        {
            ButtonSizePreset.Small => 26f,
            ButtonSizePreset.Large => 36f,
            _ => 30f
        };
    }

    private float GetMeasuredTitleHeight()
    {
        var style = new RichStyle
        {
            FontFamily = Theme.FontFamily,
            FontSize = (float)TitleFontSize,
            FontWeight = FontWeights.Normal.ToFontWeightValue(),
            TextColor = TitleColor.ToSKColor()
        };

        var textBlock = new Topten.RichTextKit.TextBlock
        {
            MaxWidth = Math.Max(1f, Width),
            Alignment = RichTextAlignment.Left,
            EllipsisEnabled = true
        };

        textBlock.AddText(string.IsNullOrWhiteSpace(Title) ? " " : Title, style);
        textBlock.Layout();

        return Math.Max(12f, textBlock.MeasuredHeight + 2f);
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

    private (Color FillColor, Color BorderColor) GetVisualColors(RenderContext ctx)
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

    private float GetSafeIconSize()
    {
        return Math.Clamp(IconSize, 8f, 48f);
    }

    private float GetSafeIconSpacing()
    {
        return Math.Clamp(IconSpacing, 0f, 32f);
    }

    private static SKRect CreateContentRect(SKRect layout, Thickness padding)
    {
        return new SKRect(
            layout.Left + (float)padding.Left,
            layout.Top + (float)padding.Top,
            layout.Right - (float)padding.Right,
            layout.Bottom - (float)padding.Bottom
        );
    }

    private static bool IsUsableRect(SKRect rect)
    {
        return rect.Width > 1f && rect.Height > 1f;
    }

    #endregion
}

#endregion





//// ======================================================================================
//// FILE: Mockup/_ControlPool/Input/TextBox.cs
////
//// PURPOSE:
//// - Single-line input field in the unified light control style.
//// - Supports title, placeholder and simple runtime hover / pressed states in LiveMode.
//// - Uses RichTextKit-based text rendering through TextRenderer.
//// - Keeps the model lightweight and PropertyGrid-friendly.
////
//// NOTES:
//// - This is a visual mockup control, not a real text input widget.
//// - The input row follows the compact button height logic.
//// - If a title is shown, the overall control height grows automatically with the
////   measured title area.
//// ======================================================================================

//using CommunityToolkit.Mvvm.ComponentModel;
//using Mockup.ColorSystem;
//using Mockup.Messages;
//using Mockup.Registry;
//using Mockup.Rendering;
//using SkiaSharp;
//using SkiaSharp.Views.WPF;
//using System.ComponentModel;
//using System.Text.Json.Serialization;
//using System.Windows;
//using System.Windows.Input;
//using System.Windows.Media;
//using RichStyle = Topten.RichTextKit.Style;
//using RichTextAlignment = Topten.RichTextKit.TextAlignment;

//namespace Mockup.Controls;

//#region === TEXT BOX ======================================================================

//[ControlType(displayName: "TextBox", group: "Input")]
//public partial class TextBox : DesignControl
//{
//    #region === CONTENT ===================================================================

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Content")]
//    [property: System.ComponentModel.DisplayName("Text")]
//    private string text = "Text";

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Content")]
//    [property: System.ComponentModel.DisplayName("Title")]
//    private string title = string.Empty;

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Content")]
//    [property: System.ComponentModel.DisplayName("Placeholder")]
//    private string placeholder = "Placeholder";

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
//    private double titleFontSize = 11d;

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

//    #endregion

//    #region === RUNTIME INTERACTION =======================================================

//    [JsonIgnore, Browsable(false)]
//    private bool _isHovered;

//    [JsonIgnore, Browsable(false)]
//    private bool _isPressed;

//    [JsonIgnore, Browsable(false)]
//    private bool _applyingSizePreset;

//    #endregion

//    #region === CTOR ======================================================================

//    public TextBox()
//    {
//        Name = "TextBox";
//        ResizeStyle = ResizeStyles.ResizeAll;

//        Width = 140f;
//        Height = 30f;

//        MinWidth = 60f;
//        MinHeight = 26f;

//        MaxWidth = 600f;
//        MaxHeight = 240f;

//        ApplySizePreset(SizePreset);
//        RecalculateOverallHeight();
//    }

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

//    #region === POINTER EVENTS ============================================================

//    public override void OnPointerDown(in PointerContext ctx)
//    {
//        if (!ctx.IsLiveMode)
//            return;

//        if (ctx.Button != MouseButton.Left)
//            return;

//        if (!VisualRect.Contains(ctx.WorldPoint))
//            return;

//        SetPressedState(true);
//        SetHoverState(true);
//    }

//    public override void OnPointerMove(in PointerContext ctx)
//    {
//        if (!ctx.IsLiveMode)
//        {
//            ResetInteractionState();
//            return;
//        }

//        bool isInside = VisualRect.Contains(ctx.WorldPoint);
//        SetHoverState(isInside);

//        if (!isInside && _isPressed)
//            SetPressedState(false);

//        if (isInside)
//        {
//            Mouse.OverrideCursor = Cursors.Hand;
//        }
//    }

//    public override void OnPointerUp(in PointerContext ctx)
//    {
//        if (!ctx.IsLiveMode)
//        {
//            ResetInteractionState();
//            return;
//        }

//        bool isInside = VisualRect.Contains(ctx.WorldPoint);

//        SetPressedState(false);
//        SetHoverState(isInside);
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

//        bool hasTitle = HasVisibleTitle();
//        float titleHeight = hasTitle ? GetMeasuredTitleHeight() : 0f;
//        float titleGap = hasTitle ? 2f : 0f;

//        var titleRect = hasTitle
//            ? new SKRect(layout.Left, layout.Top, layout.Right, layout.Top + titleHeight)
//            : SKRect.Empty;

//        var inputRect = new SKRect(
//            layout.Left,
//            layout.Top + titleHeight + titleGap,
//            layout.Right,
//            layout.Bottom
//        );

//        if (inputRect.Height < 8f)
//            return;

//        var (fillColor, resolvedBorderColor) = GetVisualColors(ctx);
//        var shadowOptions = GetVisualShadow(ctx);

//        SkiaRenderer.DrawRect(
//            canvas: canvas,
//            rect: inputRect,
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

//        var contentRect = CreateContentRect(inputRect, Padding);
//        if (contentRect.Width <= 1f || contentRect.Height <= 1f)
//            return;

//        bool hasText = !string.IsNullOrWhiteSpace(Text);

//        TextRenderer.Draw2(
//            canvas: canvas,
//            text: hasText ? Text : Placeholder,
//            bounds: contentRect,
//            fontSize: FontSize,
//            color: hasText ? TextColor : PlaceholderColor,
//            padding: new Thickness(0),
//            fontWeight: FontWeight,
//            textAlignment: System.Windows.TextAlignment.Left
//        );
//    }

//    public override string ToString() => string.Empty;

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
//        float inputRowHeight = GetInputRowHeight();
//        float titleExtra = 0f;

//        if (HasVisibleTitle())
//        {
//            titleExtra = GetMeasuredTitleHeight() + 2f;
//        }

//        float desiredHeight = inputRowHeight + titleExtra;
//        desiredHeight = Math.Clamp(desiredHeight, MinHeight, MaxHeight);

//        if (Math.Abs(Height - desiredHeight) > 0.5f)
//        {
//            Height = desiredHeight;
//        }
//    }

//    private bool HasVisibleTitle()
//    {
//        return ShowTitle && !string.IsNullOrWhiteSpace(Title);
//    }

//    private float GetInputRowHeight()
//    {
//        return SizePreset switch
//        {
//            ButtonSizePreset.Small => 26f,
//            ButtonSizePreset.Large => 36f,
//            _ => 30f
//        };
//    }

//    private float GetMeasuredTitleHeight()
//    {
//        var style = new RichStyle
//        {
//            FontFamily = Theme.FontFamily,
//            FontSize = (float)TitleFontSize,
//            FontWeight = FontWeights.Normal.ToFontWeightValue(),
//            TextColor = TitleColor.ToSKColor()
//        };

//        var textBlock = new Topten.RichTextKit.TextBlock
//        {
//            MaxWidth = Math.Max(1f, Width),
//            Alignment = RichTextAlignment.Left,
//            EllipsisEnabled = true
//        };

//        textBlock.AddText(string.IsNullOrWhiteSpace(Title) ? " " : Title, style);
//        textBlock.Layout();

//        return Math.Max(12f, textBlock.MeasuredHeight + 2f);
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

//    private (Color FillColor, Color BorderColor) GetVisualColors(RenderContext ctx)
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

//    private float GetSafeCornerRadius()
//    {
//        return Math.Clamp(CornerRadius, 0f, 12f);
//    }

//    private static SKRect CreateContentRect(SKRect layout, Thickness padding)
//    {
//        return new SKRect(
//            layout.Left + (float)padding.Left,
//            layout.Top + (float)padding.Top,
//            layout.Right - (float)padding.Right,
//            layout.Bottom - (float)padding.Bottom
//        );
//    }

//    #endregion
//}

//#endregion
