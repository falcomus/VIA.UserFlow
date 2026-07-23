// ======================================================================================
// FILE: Mockup.Controls/HamburgerButton.cs
//
// PURPOSE:
// - Small light-mode hamburger button for the mockup designer.
// - Visual style aligned with the modern misc button controls.
// - Supports hover / pressed feedback in LiveMode.
// - Toggles between hamburger and close state.
//
// PROJECT: Mockup.Controls
// GROUP: Buttons [Misc]
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
using System.Windows.Input;
using System.Windows.Media;

namespace Mockup.Controls;

#region === HAMBURGER BUTTON ===

[ControlType(displayName: "Menu Toggle Button", group: "Icon Buttons")]
public partial class HamburgerButton : DesignControl
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
    [property: System.ComponentModel.DisplayName("Icon Color")]
    private Color iconColor = Theme.Text;

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

    #endregion

    #region === LAYOUT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Icon Padding")]
    private float iconPadding = 0.22f;

    partial void OnIconPaddingChanged(float value)
    {
        iconPadding = Math.Clamp(value, 0.05f, 0.40f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Stroke Width")]
    private float strokeWidth = 1.6f;

    partial void OnStrokeWidthChanged(float value)
    {
        strokeWidth = Math.Clamp(value, 1f, 3f);
    }

    #endregion

    #region === BEHAVIOR ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Is On")]
    private bool isOn = false;

    #endregion

    #region === RUNTIME STATE ===

    [JsonIgnore, Browsable(false)]
    private bool _isHovered;

    [JsonIgnore, Browsable(false)]
    private bool _isPressed;

    #endregion

    #region === CTOR ===

    public HamburgerButton()
    {
        IsActionControl = true;

        Name = "HamburgerButton";
        ResizeStyle = ResizeStyles.KeepRatio;

        ExplicitePreviewHeight = 50f;
        ExplicitePreviewWidth = 50f;

        Width = 25f;
        Height = 30f;

        MinWidth = 25f;
        MinHeight = 25f;

        MaxWidth = 50f;
        MaxHeight = 50f;
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

        bool isInside = VisualRect.Contains(ctx.WorldPoint);
        bool commitClick = _isPressed && isInside;

        _isPressed = false;
        _isHovered = isInside;

        if (commitClick)
            IsOn = !IsOn;

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
        //DrawBackground(canvas, layout);
        DrawIcon(canvas, layout);
    }

    #endregion

    #region === DRAW HELPERS ===

    private void DrawBackground(SKCanvas canvas, SKRect layout)
    {
        Color fill = BackgroundColor;
        Color border = BorderColor;

        if (_isHovered)
        {
            fill = HoverBackgroundColor;
            border = border.Darken(0.04f);
        }

        if (_isPressed)
        {
            fill = PressedBackgroundColor;
            border = border.Darken(0.08f);
        }

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: layout,
            cornerRadius: Math.Clamp(CornerRadius, 0f, 10f),
            fillStyle: FillStyle.Solid,
            fillColor: fill,
            borderColor: border,
            borderStyle: BorderStyle.None,
            shadowOptions: ShadowOptions.Default,
            borderWidth: 0f
        );
    }

    private void DrawIcon(SKCanvas canvas, SKRect layout)
    {
        float contentSize = Math.Min(layout.Width, layout.Height) * (1f - IconPadding * 2f);
        float centerX = layout.MidX;
        float centerY = layout.MidY;

        using var paint = new SKPaint
        {
            Color = IconColor.ToSKColor().WithAlpha(190),
            StrokeWidth = StrokeWidth,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round
        };

        if (IsOn)
        {
            float offset = contentSize * 0.34f;

            canvas.DrawLine(
                centerX - offset,
                centerY - offset,
                centerX + offset,
                centerY + offset,
                paint
            );

            canvas.DrawLine(
                centerX - offset,
                centerY + offset,
                centerX + offset,
                centerY - offset,
                paint
            );
        }
        else
        {
            float halfWidth = contentSize * 0.5f;
            float spacing = contentSize * 0.32f;

            float y1 = centerY - spacing;
            float y2 = centerY;
            float y3 = centerY + spacing;

            float x1 = centerX - halfWidth;
            float x2 = centerX + halfWidth;

            canvas.DrawLine(x1, y1, x2, y1, paint);
            canvas.DrawLine(x1, y2, x2, y2, paint);
            canvas.DrawLine(x1, y3, x2, y3, paint);
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

