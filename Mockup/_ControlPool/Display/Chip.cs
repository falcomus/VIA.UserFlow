// ======================================================================================
// FILE: Mockup.Controls/Chip.cs
//
// PURPOSE:
// - Multi-variant chip controls in the unified light control style.
// - Keeps existing Chip type names for registry / JSON compatibility.
// - Chips are always rendered pill-shaped automatically.
// - Variant chips are filled by default.
// - Supports selected / unselected rendering, optional icon + text layout,
//   and lightweight runtime hover / pressed interaction in LiveMode.
//
// NOTES:
// - ChipBase is kept for compatibility with existing derived classes.
// - Existing persisted typeKeys such as ChipPrimary remain valid.
// - GroupId is included as preparation for grouped chip behavior.
// - This file does not yet auto-clear sibling chips inside the same group.
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

#region === CHIP BASE =====================================================================

public partial class ChipBase : DesignControl
{
    #region === CONTENT ===================================================================

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Text")]
    private string text = "Chip";

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
    [property: System.ComponentModel.DisplayName("Selected Background")]
    private Color selectedBackgroundColor = Theme.Primary;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Selected Border")]
    private Color selectedBorderColor = Theme.Primary;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Selected Text")]
    private Color selectedTextColor = Colors.White;

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
    private float iconSize = 14f;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Icon")]
    [property: System.ComponentModel.DisplayName("Icon Spacing")]
    private float iconSpacing = 6f;

    [JsonIgnore, Browsable(false)]
    public bool HasIcon => Icon is not null && !string.IsNullOrWhiteSpace(Icon.Id);

    #endregion

    #region === BEHAVIOR / STATE ==========================================================

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Selectable")]
    private bool isSelectable = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Toggle On Click")]
    private bool toggleOnClick = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Group Id")]
    private string groupId = string.Empty;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Selected")]
    private bool isSelected;

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

    public ChipBase()
    {
        Name = "Chip";
        ResizeStyle = ResizeStyles.ResizeAll;

        Width = 90f;
        Height = 28f;

        MinWidth = 36f;
        MinHeight = 24f;

        MaxWidth = 600f;
        MaxHeight = 56f;

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
        bool shouldCommitClick = _isPressed && isInside;

        SetPressedState(false);
        SetHoverState(isInside);

        if (!shouldCommitClick || !IsSelectable)
            return;

        if (string.IsNullOrWhiteSpace(GroupId))
        {
            if (ToggleOnClick)
                IsSelected = !IsSelected;
            else
                IsSelected = true;
        }
        else
        {
            IsSelected = true;
        }

        InvalidateVisuals();
    }

    public override void OnPointerLeave()
    {
        ResetInteractionState();
    }

    #endregion

    #region === RENDER ====================================================================

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        float pillCornerRadius = GetPillCornerRadius(layout);
        var (fillColor, resolvedBorderColor, resolvedTextColor) = GetVisualColors(ctx);
        var shadowOptions = GetVisualShadow(ctx);

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: layout,
            cornerRadius: pillCornerRadius,
            fillStyle: FillStyle.Solid,
            fillColor: fillColor,
            borderColor: resolvedBorderColor,
            borderStyle: BorderStyle.Solid,
            shadowOptions: shadowOptions,
            borderWidth: 1f
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

    public override string ToString() => string.Empty;

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
                    Height = 24f;
                    MinHeight = 22f;
                    FontSize = 11d;
                    IconSize = 12f;
                    Padding = new Thickness(10, 0, 10, 0);
                    break;

                case ButtonSizePreset.Large:
                    Height = 32f;
                    MinHeight = 28f;
                    FontSize = 14.5d;
                    IconSize = 16f;
                    Padding = new Thickness(14, 0, 14, 0);
                    break;

                default:
                    Height = 28f;
                    MinHeight = 24f;
                    FontSize = 13.5d;
                    IconSize = 14f;
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
        Color fillColor;
        Color borderColor;
        Color textColor;

        if (Variant == ControlVariant.CUSTOM)
        {
            if (IsSelected)
            {
                fillColor = SelectedBackgroundColor;
                borderColor = SelectedBorderColor;
                textColor = SelectedTextColor;
            }
            else
            {
                fillColor = BackgroundColor;
                borderColor = BorderColor;
                textColor = TextColor;
            }
        }
        else
        {
            Color variantFill = GetFillColor(Variant, BackgroundColor);
            Color variantBorder = GetBorderColor(Variant, BorderColor);

            if (IsSelected)
            {
                fillColor = variantFill.Darken(0.08f);
                borderColor = variantBorder.Darken(0.10f);
                textColor = Colors.White;
            }
            else
            {
                fillColor = variantFill;
                borderColor = variantBorder.Darken(0.06f);
                textColor = Colors.White;
            }
        }

        if (ctx.LiveMode && _isHovered)
        {
            fillColor = fillColor.Lighten(0.04f);
            borderColor = borderColor.Darken(0.02f);
        }

        if (ctx.LiveMode && _isPressed)
        {
            fillColor = fillColor.Darken(0.06f);
            borderColor = borderColor.Darken(0.05f);
        }

        return (fillColor, borderColor, textColor);
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

    private static float GetPillCornerRadius(SKRect layout)
    {
        return Math.Max(0f, layout.Height / 2f);
    }

    private float GetSafeIconSize()
    {
        return Math.Clamp(IconSize, 8f, 40f);
    }

    private float GetSafeIconSpacing()
    {
        return Math.Clamp(IconSpacing, 0f, 24f);
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

#region === CHIP VARIANTS =================================================================

[ControlType(displayName: "Chip", group: "Indicators")]
public sealed partial class Chip : ChipBase
{
    public Chip()
    {
        Name = "Chip";
        Variant = ControlVariant.CUSTOM;
        Text = "Custom";
        BackgroundColor = Theme.ControlBG;
        BorderColor = Theme.ControlBorder;
        TextColor = Colors.Black;
    }
}

[ControlType(displayName: "Chip – Primary", group: "Indicators")]
public sealed partial class ChipPrimary : ChipBase
{
    public ChipPrimary()
    {
        Name = "Chip Primary";
        Variant = ControlVariant.Primary;
        Text = "Primary";
        IsSelected = false;
    }
}

[ControlType(displayName: "Chip – Accent", group: "Indicators")]
public sealed partial class ChipAccent : ChipBase
{
    public ChipAccent()
    {
        Name = "Chip Accent";
        Variant = ControlVariant.Accent;
        Text = "Accent";
        IsSelected = false;
    }
}

[ControlType(displayName: "Chip – Info", group: "Indicators")]
public sealed partial class ChipInfo : ChipBase
{
    public ChipInfo()
    {
        Name = "Chip Info";
        Variant = ControlVariant.Info;
        Text = "Info";
        IsSelected = false;
    }
}

[ControlType(displayName: "Chip – Warning", group: "Indicators")]
public sealed partial class ChipWarning : ChipBase
{
    public ChipWarning()
    {
        Name = "Chip Warning";
        Variant = ControlVariant.Warning;
        Text = "Warning";
        IsSelected = false;
    }
}

[ControlType(displayName: "Chip – Error", group: "Indicators")]
public sealed partial class ChipError : ChipBase
{
    public ChipError()
    {
        Name = "Chip Error";
        Variant = ControlVariant.Error;
        Text = "Error";
        IsSelected = false;
    }
}

#endregion

