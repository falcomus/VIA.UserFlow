// ======================================================================================
// FILE: Mockup.Controls/ToggleSwitch.cs
//
// PURPOSE:
// - Modern ToggleSwitch control for the mockup designer.
// - Visual style aligned with CheckBox / RadioButton / TextBox / ComboBox controls.
// - Supports on/off state and hover in LiveMode.
// - Compact light-mode style with clear track and thumb rendering.
//
// NOTES:
// - This is a visual mockup control, not a native WPF toggle.
// - The control toggles only in LiveMode on left mouse click.
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

#region === TOGGLE SWITCH ===

[ControlType(displayName: "Toggle Switch", group: "Selection")]
public partial class ToggleSwitch : DesignControl
{
    #region === APPEARANCE ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Variant")]
    private ControlVariant variant = ControlVariant.Primary;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Track Off Background")]
    private Color trackOffBackgroundColor = Colors.White;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Track Off Border")]
    private Color trackOffBorderColor = Color.FromRgb(150, 150, 150);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Thumb Color")]
    private Color thumbColor = Colors.White;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Thumb Off Color")]
    private Color thumbOffColor = Color.FromRgb(160, 161, 163);

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

    #region === LAYOUT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Thumb Padding X")]
    private float thumbPaddingX = 0f;

    partial void OnThumbPaddingXChanged(float value)
    {
        thumbPaddingX = Math.Clamp(value, 0f, 8f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Thumb Padding Y")]
    private float thumbPaddingY = 2f;

    partial void OnThumbPaddingYChanged(float value)
    {
        thumbPaddingY = Math.Clamp(value, 0f, 8f);
    }

    #endregion

    #region === BEHAVIOR ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Is On")]
    private bool isOn = true;

    #endregion

    #region === RUNTIME STATE ===

    [JsonIgnore, Browsable(false)]
    private bool _isHovered;

    [JsonIgnore, Browsable(false)]
    private bool _isPressed;

    [JsonIgnore, Browsable(false)]
    private SKRect _trackRect;

    [JsonIgnore, Browsable(false)]
    private SKRect _thumbRect;

    #endregion

    #region === CTOR ===

    public ToggleSwitch()
    {
        IsActionControl = true;

        Name = "ToggleSwitch";
        ResizeStyle = ResizeStyles.None;

        Width = 35f;
        Height = 20f;

        MinWidth = 35f;
        MinHeight = 20f;

        MaxWidth = 35f;
        MaxHeight = 20f;
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === MOUSEEVENT HOOKS ===

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

        if (!isInside && _isPressed)
        {
            _isPressed = false;
            InvalidateVisuals();
        }

        if (isInside)
        {
            Mouse.OverrideCursor = Cursors.Hand;
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
        _trackRect = new SKRect(
            layout.Left + ThumbPaddingX,
            layout.Top + ThumbPaddingY,
            layout.Right - ThumbPaddingX,
            layout.Bottom - ThumbPaddingY
        );

        DrawTrack(canvas, ctx);
        DrawThumb(canvas, ctx);
    }

    #endregion

    #region === DRAW HELPERS ===

    private void DrawTrack(SKCanvas canvas, RenderContext ctx)
    {
        var (trackFill, trackBorder) = GetTrackVisualColors(ctx);
        float cornerRadius = Math.Max(2f, _trackRect.Height / 2f);

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: _trackRect,
            cornerRadius: cornerRadius,
            fillColor: trackFill,
            fillStyle: FillStyle.Solid,
            borderStyle: BorderStyle.Solid,
            borderColor: trackBorder,
            borderWidth: 1f,
            shadowOptions: Elevation > 0 ? GetElevation(Elevation) : ShadowOptions.Default,
            innerBorder: true
        );
    }

    private void DrawThumb(SKCanvas canvas, RenderContext ctx)
    {
        float thumbSize = _trackRect.Height;
        float thumbLeft = IsOn
            ? _trackRect.Right - thumbSize
            : _trackRect.Left;

        _thumbRect = new SKRect(
            thumbLeft,
            _trackRect.Top,
            thumbLeft + thumbSize,
            _trackRect.Bottom
        );

        var rect = _thumbRect;
        rect.Inflate(-1.5f, -1.5f);

        Color fill = IsOn
            ? Variant == ControlVariant.CUSTOM
                ? Color.FromRgb(80, 81, 83)
                : ThumbColor
            : ThumbOffColor;

        if (ctx.LiveMode && _isHovered)
            fill = fill.Lighten(0.03f);

        if (ctx.LiveMode && _isPressed)
            fill = fill.Darken(0.04f);

        using var paint = new SKPaint
        {
            Color = fill.ToSKColor(),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        canvas.DrawRoundRect(rect, rect.Height / 2f, rect.Height / 2f, paint);
    }

    private (Color Fill, Color Border) GetTrackVisualColors(RenderContext ctx)
    {
        Color fill;
        Color border;

        if (IsOn)
        {
            fill = Variant == ControlVariant.CUSTOM
                ? SkiaRenderer.SelectionColor
                : GetFillColor(Variant, SkiaRenderer.SelectionColor);

            border = Variant == ControlVariant.CUSTOM
                ? fill.Darken(0.10f)
                : GetBorderColor(Variant, SkiaRenderer.SelectionColor).Darken(0.06f);
        }
        else
        {
            fill = TrackOffBackgroundColor;
            border = TrackOffBorderColor;
        }

        if (ctx.LiveMode && _isHovered)
        {
            fill = fill.Lighten(0.02f);
            border = border.Darken(0.03f);
        }

        if (ctx.LiveMode && _isPressed)
        {
            fill = fill.Darken(0.04f);
            border = border.Darken(0.06f);
        }

        return (fill, border);
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





//using CommunityToolkit.Mvvm.ComponentModel;
//using Mockup.ColorSystem;
//using Mockup.Messages;
//using Mockup.Registry;
//using Mockup.Rendering;
//using SkiaSharp;
//using System.Windows.Media;


//namespace Mockup.Controls;

//[ControlType(displayName: "ToggleSwitch", group: "Input")]
//public partial class ToggleSwitch : DesignControl
//{
//    private double _thumbPosition = 0;

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Variant")]
//    private ControlVariant variant = ControlVariant.Primary;

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("IsOn")]
//    private bool isOn = true;

//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Elevation")]
//    private int elevation = 0;
//    partial void OnElevationChanged(int value) => Elevation = Math.Clamp(value, 0, 5);


//    public ToggleSwitch()
//    {
//        IsActionControl = true;

//        Name = "ToggleSwitch";
//        ResizeStyle = ResizeStyles.None;

//        Width = 35;
//        Height = 20;

//        MinWidth = 35;
//        MinHeight = 20;

//        MaxWidth = 35;
//        MaxHeight = 20;
//    }


//    #region === MOUSEEVENT HOOKS ===

//    public override void OnPointerDown(in PointerContext ctx)
//    {
//        IsOn = !IsOn;
//        MSG.UI.InvalidateDesigner();
//    }

//    public override void OnPointerUp(in PointerContext ctx) { }

//    public override void OnPointerMove(in PointerContext ctx) { }

//    #endregion === MOUSEEVENT HOOKS ===



//    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
//    {

//        float thumbPaddingX = 0f;
//        float thumbPaddingY = 2f;
//        float cornerRadius = 10f;
//        string thumbColor = SKColors.White.ToString();

//        // Calculate track dimensions
//        var trackHeight = layout.Height - (2 * thumbPaddingY);
//        var trackRect = new SKRect(
//            layout.Left + thumbPaddingX,
//            layout.Top + thumbPaddingY,
//            layout.Right - thumbPaddingX,
//            layout.Bottom - thumbPaddingY
//        );

//        // Draw track
//        var trackColor = GetFillColor(Variant, Theme.ControlBG);

//        if (!IsOn)
//        {
//            trackColor = Colors.White;
//        }

//        SkiaRenderer.DrawRect(
//            canvas: canvas,
//            rect: trackRect,
//            cornerRadius: cornerRadius,
//            fillColor: trackColor,
//            fillStyle: FillStyle.Solid,
//            borderStyle: BorderStyle.Solid,
//            borderColor: Color.FromRgb(150, 150, 150),
//            borderWidth: 1f,
//            shadowOptions: DesignControl.GetElevation(Elevation),
//            innerBorder: true);

//        // Calculate thumb position (animated)
//        float thumbSize = trackRect.Height;
//        float maxThumbX = trackRect.Right - thumbSize;
//        float thumbX;

//        if (IsOn)
//        {
//            thumbX = (float)(trackRect.Right - thumbSize);
//        }
//        else
//        {
//            thumbX = (float)(trackRect.Left + (_thumbPosition * (maxThumbX - trackRect.Left)));
//        }

//        // Draw thumb

//        var thumbRect = new SKRect(
//            thumbX,
//            trackRect.Top,
//            thumbX + thumbSize,
//            trackRect.Bottom
//        );

//        using (var paint = new SKPaint
//        {
//            Color = IsOn ? Variant == ControlVariant.CUSTOM ? SKColor.Parse("#505153") : SKColors.White : SKColor.Parse("#A0A1A3"),
//            IsAntialias = true,
//            Style = SKPaintStyle.Fill
//        })
//        {
//            thumbRect.Inflate(-1.5f, -1.5f);

//            canvas.DrawRoundRect(thumbRect, thumbSize / 2, thumbSize / 2, paint);
//        }
//    }

//}

