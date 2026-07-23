// ======================================================================================
// FILE: Mockup.Controls/AddButton.cs
//
// PURPOSE:
// - Small light-mode toggle button for the mockup designer.
// - Visual style aligned with the modern button/input controls.
// - Supports hover / pressed feedback in LiveMode.
// - Toggles between On / Off state and renders separate images for both states.
//
// PROJECT: Mockup.Controls
// GROUP: Buttons [Misc]
//
// NOTES:
// - This is a small icon toggle button.
// - The control toggles itself in LiveMode on left mouse click.
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
using System.Windows.Input;
using System.Windows.Media;

namespace Mockup.Controls;

#region === ADD BUTTON ===

[ControlType(displayName: "Add Button", group: "Icon Buttons")]
public partial class AddButton : DesignControl
{
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
    [property: System.ComponentModel.DisplayName("Hover Background")]
    private Color hoverBackgroundColor = Theme.ControlBG.Darken(0.03f);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Pressed Background")]
    private Color pressedBackgroundColor = Theme.ControlBG.Darken(0.06f);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Corner Radius")]
    private float cornerRadius = 4f;

    partial void OnCornerRadiusChanged(float value)
    {
        cornerRadius = Math.Clamp(value, 0f, 20f);
    }

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

    #region === ICONS ===

    private ImageRef? image;
    private Color imageColor = Colors.Black;

    #endregion

    #region === LAYOUT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Size")]
    private ButtonSizePreset sizePreset = ButtonSizePreset.Normal;

    partial void OnSizePresetChanged(ButtonSizePreset value)
    {
        ApplySizePreset(value);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Icon Size")]
    private float iconSize = 12f;

    partial void OnIconSizeChanged(float value)
    {
        iconSize = Math.Clamp(value, 6f, 50f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Inner Padding")]
    private float innerPadding = 6f;

    partial void OnInnerPaddingChanged(float value)
    {
        innerPadding = Math.Clamp(value, 1f, 12f);
    }

    #endregion


    #region === RUNTIME STATE ===

    [JsonIgnore, Browsable(false)]
    private bool _isHovered;

    [JsonIgnore, Browsable(false)]
    private bool _isPressed;

    [JsonIgnore, Browsable(false)]
    private bool _applyingSizePreset;

    #endregion

    #region === CTOR ===

    public AddButton()
    {
        IsActionControl = true;

        Name = "AddButton";
        ResizeStyle = ResizeStyles.None;

        ExplicitePreviewHeight = 50f;
        ExplicitePreviewWidth = 50f;

        Width = 30f;
        Height = 30f;

        MinWidth = 25f;
        MinHeight = 25f;

        MaxWidth = 50f;
        MaxHeight = 50f;


        image = new ImageRef("plus", ImageFormat.Svg);
        imageColor = Colors.Black;

        ApplySizePreset(SizePreset);
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === POINTER HOOKS ===

    public override void OnPointerDown(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;

        if (!VisualRect.Contains(ctx.WorldPoint))
            return;

        _isPressed = true;
        _isHovered = true;
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

        if (_isHovered != isInside)
        {
            _isHovered = isInside;
            InvalidateVisuals();
        }

        if (isInside)
            Mouse.OverrideCursor = Cursors.Hand;

        if (!isInside && _isPressed)
        {
            _isPressed = false;
            InvalidateVisuals();
        }
    }

    public override void OnPointerUp(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;
    }

    public override void OnPointerLeave()
    {
        ResetInteractionState();
    }

    #endregion

    #region === RENDER ===

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        DrawBackground(canvas, layout, ctx);
        DrawCurrentImage(canvas, layout);
    }

    #endregion

    #region === DRAW HELPERS ===

    private void DrawBackground(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        Color fill = BackgroundColor;
        Color border = BorderColor;

        if (ctx.LiveMode && _isHovered)
        {
            fill = HoverBackgroundColor;
            border = border.Darken(0.04f);
        }

        if (ctx.LiveMode && _isPressed)
        {
            fill = PressedBackgroundColor;
            border = border.Darken(0.08f);
        }

        ShadowOptions shadow = Elevation > 0 ? GetElevation(Elevation) : ShadowOptions.Default;

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: layout,
            cornerRadius: Math.Clamp(CornerRadius, 0f, 10f),
            fillStyle: FillStyle.Solid,
            fillColor: fill,
            borderColor: border,
            borderStyle: BorderStyle.Solid,
            shadowOptions: shadow,
            borderWidth: 0.85f
        );
    }

    private void DrawCurrentImage(SKCanvas canvas, SKRect layout)
    {
        if (image == null || string.IsNullOrWhiteSpace(image.Id))
            return;

        float safePadding = Math.Clamp(InnerPadding, 1f, 12f);
        float maxSize = Math.Min(layout.Width, layout.Height) - safePadding * 2f;

        if (maxSize <= 1f)
            return;

        float safeSize = Math.Clamp(IconSize, 1f, maxSize);

        float left = layout.MidX - safeSize / 2f;
        float top = layout.MidY - safeSize / 2f;

        var iconRect = new SKRect(
            left,
            top,
            left + safeSize,
            top + safeSize
        );

        SkiaRenderer.RenderSVGIcon(
            canvas,
            iconRect,
            image,
            Colors.Transparent,
            Colors.Transparent,
            imageColor,
            1,
            false,
            0,
            0
        );
    }

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
                    Width = 26f;
                    Height = 26f;
                    MinWidth = 26f;
                    MinHeight = 26f;
                    IconSize = 14f;
                    InnerPadding = 2f;
                    CornerRadius = 4f;
                    break;

                case ButtonSizePreset.Large:
                    Width = 36f;
                    Height = 36f;
                    MinWidth = 36f;
                    MinHeight = 36f;
                    IconSize = 20f;
                    InnerPadding = 3f;
                    CornerRadius = 5f;
                    break;

                default:
                    Width = 30f;
                    Height = 30f;
                    MinWidth = 30f;
                    MinHeight = 30f;
                    IconSize = 18f;
                    InnerPadding = 2f;
                    CornerRadius = 4f;
                    break;
            }

            MaxWidth = 50f;
            MaxHeight = 50f;
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

    private void InvalidateVisuals()
    {
        MSG.UI.InvalidateDesigner();
    }

    #endregion
}

#endregion