// ======================================================================================
// FILE: Mockup/_ControlPool/Buttons/Button.cs
//
// PURPOSE:
// - Unified filled button base for the control library.
// - Keeps the current Variant-based workflow intact.
// - Provides compact default sizing closer to the target mockup style.
// - Supports runtime hover / pressed interaction in LiveMode.
// - Centers icon + text as one content group and respects Padding + IconSpacing.
//
// NOTES:
// - Variant is intentionally kept for compatibility with existing editors.
// - SizePreset is a lightweight first step towards a consistent size system across controls.
// - Elevation remains available, but defaults to 0 and should be used sparingly.
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.Domain.Registry;
using Mockup.Messages;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Mockup.Controls;

#region === BASE BUTTON ===================================================================

[ControlType(displayName: "Button", group: "Buttons", previewImage: "Buttons/Button.png")]
public partial class Button : DesignControl
{
    #region === CONTENT ===================================================================

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Text")]
    private string text = "Button";

    #endregion

    #region === APPEARANCE ================================================================

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Variant")]
    private ControlVariant variant = ControlVariant.CUSTOM;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Background Color")]
    private Color backgroundColor = Theme.ControlBG;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Border Color")]
    private Color borderColor = Theme.ControlBorder;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Text Color")]
    private Color textColor = Colors.Black;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Corner Radius")]
    private float cornerRadius = 6f;

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
    private double fontSize = 12.5d;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Font Weight")]
    private FontWeight fontWeight = FontWeights.Medium;

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
    private Thickness padding = new(12, 0, 12, 0);

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
    [property: System.ComponentModel.DisplayName("Icon Alignment")]
    private HorizontalImageAlignment imageAlignment = HorizontalImageAlignment.Left;

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
    public bool HasIcon => Icon is not null && !string.IsNullOrWhiteSpace(Icon.Id);

    [JsonIgnore, Browsable(false)]
    public float EffectiveIconWidth => HasIcon ? GetSafeIconSize() + GetSafeIconSpacing() : 0f;

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

    public Button()
    {
        Name = "Button";
        ResizeStyle = ResizeStyles.ResizeAll;

        ExplicitePreviewHeight = 35;

        Width = 96f;
        Height = 30f;

        MinWidth = 64f;
        MinHeight = 26f;

        MaxWidth = 480f;
        MaxHeight = 64f;

        Text = "Button";
        Elevation = 0;

        ApplySizePreset(SizePreset);
    }

    #endregion

    #region === PROPERTY REACTIONS ========================================================

    partial void OnSizePresetChanged(ButtonSizePreset value)
    {
        ApplySizePreset(value);
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
        {
            Mouse.OverrideCursor = Cursors.Hand;
        }
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
        float safeCornerRadius = GetSafeCornerRadius();
        var (fillColor, resolvedBorderColor, resolvedTextColor) = GetVisualColors(ctx);
        var shadowOptions = GetVisualShadow(ctx);

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: layout,
            cornerRadius: safeCornerRadius,
            fillStyle: FillStyle.Solid,
            fillColor: fillColor,
            borderColor: resolvedBorderColor,
            borderStyle: BorderStyle.Solid,
            shadowOptions: shadowOptions,
            borderWidth: 0.75f
        );

        var contentRect = CreateContentRect(layout, Padding);
        if (!IsUsableRect(contentRect))
            return;

        bool hasText = !string.IsNullOrWhiteSpace(Text);
        bool hasIcon = HasIcon;

        if (!hasText && !hasIcon)
            return;

        float resolvedIconSize = MathF.Min(GetSafeIconSize(), Math.Max(8f, contentRect.Height));
        float spacing = hasIcon && hasText ? GetSafeIconSpacing() : 0f;
        float textWidth = hasText
            ? TextRenderer.MeasureTextWidth(Text, FontSize, fontWeight: FontWeight)
            : 0f;

        if (!hasIcon)
        {
            DrawText(canvas, contentRect, resolvedTextColor, System.Windows.TextAlignment.Center);
            return;
        }

        float groupWidth = resolvedIconSize + spacing + textWidth;
        float freeSpace = Math.Max(0f, contentRect.Width - groupWidth);
        float offset = freeSpace / 2f;

        SKRect iconRect;
        SKRect textRect;
        System.Windows.TextAlignment textAlignment;

        if (ImageAlignment == HorizontalImageAlignment.Left)
        {
            float iconLeft = contentRect.Left + offset;

            iconRect = new SKRect(
                iconLeft,
                contentRect.MidY - resolvedIconSize / 2f,
                iconLeft + resolvedIconSize,
                contentRect.MidY + resolvedIconSize / 2f
            );

            textRect = new SKRect(
                iconRect.Right + spacing,
                contentRect.Top,
                contentRect.Right,
                contentRect.Bottom
            );

            textAlignment = System.Windows.TextAlignment.Left;
        }
        else
        {
            float iconRight = contentRect.Right - offset;

            iconRect = new SKRect(
                iconRight - resolvedIconSize,
                contentRect.MidY - resolvedIconSize / 2f,
                iconRight,
                contentRect.MidY + resolvedIconSize / 2f
            );

            textRect = new SKRect(
                contentRect.Left,
                contentRect.Top,
                iconRect.Left - spacing,
                contentRect.Bottom
            );

            textAlignment = System.Windows.TextAlignment.Right;
        }

        if (IsUsableRect(iconRect))
        {
            SkiaRenderer.RenderSVGIcon(
                canvas,
                iconRect,
                Icon,
                Colors.Transparent,
                Colors.Transparent,
                resolvedTextColor,
                1,
                false,
                0,
                0
            );
        }

        if (hasText && IsUsableRect(textRect))
        {
            DrawText(canvas, textRect, resolvedTextColor, textAlignment);
        }
    }

    public override string ToString() => string.IsNullOrWhiteSpace(Text) ? Name : Text;

    #endregion

    #region === STYLE / VISUAL HELPERS ====================================================

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
                    MinHeight = 24f;
                    MaxHeight = Math.Max(MaxHeight, 52f);
                    CornerRadius = 5f;
                    FontSize = 11.5d;
                    IconSize = 14f;
                    Padding = new Thickness(10, 0, 10, 0);
                    break;

                case ButtonSizePreset.Large:
                    Height = 36f;
                    MinHeight = 32f;
                    MaxHeight = Math.Max(MaxHeight, 72f);
                    CornerRadius = 7f;
                    FontSize = 13.5d;
                    IconSize = 18f;
                    Padding = new Thickness(14, 0, 14, 0);
                    break;

                default:
                    Height = 30f;
                    MinHeight = 26f;
                    MaxHeight = Math.Max(MaxHeight, 64f);
                    CornerRadius = 6f;
                    FontSize = 12.5d;
                    IconSize = 16f;
                    Padding = new Thickness(12, 0, 12, 0);
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

    private (Color FillColor, Color BorderColor, Color TextColor) GetVisualColors(RenderContext ctx)
    {
        Color fillColor = GetFillColor(Variant, BackgroundColor);
        Color resolvedBorderColor = GetBorderColor(Variant, BorderColor).Darken(0.12f);
        Color resolvedTextColor = GetTextColor(Variant, TextColor);

        if (ctx.LiveMode && _isHovered)
        {
            fillColor = fillColor.Lighten(0.06f);
            resolvedBorderColor = resolvedBorderColor.Darken(0.04f);
        }

        if (ctx.LiveMode && _isPressed)
        {
            fillColor = fillColor.Darken(0.07f);
            resolvedBorderColor = resolvedBorderColor.Darken(0.06f);
        }

        return (fillColor, resolvedBorderColor, resolvedTextColor);
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
        return Math.Clamp(CornerRadius, 0f, 20f);
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

    private void DrawText(
        SKCanvas canvas,
        SKRect bounds,
        Color color,
        System.Windows.TextAlignment textAlignment
    )
    {
        TextRenderer.Draw2(
            canvas: canvas,
            text: Text,
            bounds: bounds,
            fontSize: FontSize,
            color: color,
            padding: new Thickness(0),
            fontWeight: FontWeight,
            textAlignment: textAlignment
        );
    }

    #endregion
}

#endregion

#region === BUTTON VARIANTS ==============================================================

[ControlType(displayName: "Button – Primary", group: "Buttons", previewImage: "Buttons/ButtonPrimary.png")]
public sealed class ButtonPrimary : Button
{
    public ButtonPrimary()
    {
        Name = "Button Primary";
        Variant = ControlVariant.Primary;
        Text = "Primary";
    }
}

[ControlType(displayName: "Button – Accent", group: "Buttons", previewImage: "Buttons/ButtonAccent.png")]
public sealed class ButtonAccent : Button
{
    public ButtonAccent()
    {
        Name = "Button Accent";
        Variant = ControlVariant.Accent;
        Text = "Accent";
    }
}

[ControlType(displayName: "Button – Info", group: "Buttons", previewImage: "Buttons/ButtonInfo.png")]
public sealed class ButtonInfo : Button
{
    public ButtonInfo()
    {
        Name = "Button Info";
        Variant = ControlVariant.CUSTOM;
        Text = "Info";
        TextColor = Colors.Black;
        BackgroundColor = Theme.Info;
    }
}

[ControlType(displayName: "Button – Warning", group: "Buttons", previewImage: "Buttons/ButtonWarning.png")]
public sealed class ButtonWarning : Button
{
    public ButtonWarning()
    {
        Name = "Button Warning";
        Variant = ControlVariant.Warning;
        Text = "Warning";
        TextColor = Colors.White;
    }
}

[ControlType(displayName: "Button – Error", group: "Buttons", previewImage: "Buttons/ButtonError.png")]
public sealed class ButtonError : Button
{
    public ButtonError()
    {
        Name = "Button Error";
        Variant = ControlVariant.Error;
        Text = "Error";
        TextColor = Colors.White;
    }
}

#endregion
