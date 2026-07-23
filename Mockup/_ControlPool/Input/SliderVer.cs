// ======================================================================================
// FILE: Mockup.Controls/VerSlider.cs
//
// PURPOSE:
// - Modern vertical slider control for the mockup designer.
// - Supports preview interaction by dragging the thumb or clicking the track.
// - Visual style aligned with the updated light-mode control library.
//
// PROJECT: Mockup.Controls
// GROUP: Miscellaneous
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

#region === VER SLIDER CONTROL ===

[ControlType(displayName: "Slider – Vertical", group: "Pickers & Sliders")]
public partial class SliderVer : DesignControl
{
    #region === VALUE ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Minimum")]
    private float minimum = 0f;

    partial void OnMinimumChanged(float value)
    {
        if (maximum < value)
            maximum = value;

        this.value = Math.Clamp(this.value, minimum, maximum);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Maximum")]
    private float maximum = 100f;

    partial void OnMaximumChanged(float value)
    {
        if (value < minimum)
            maximum = minimum;

        this.value = Math.Clamp(this.value, minimum, maximum);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Value")]
    private float value = 50f;

    partial void OnValueChanged(float value)
    {
        this.value = Math.Clamp(value, Minimum, Maximum);
    }

    #endregion

    #region === APPEARANCE ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Track Color")]
    private Color trackColor = Color.FromRgb(220, 221, 223);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Progress Color")]
    private Color progressColor = Theme.Primary;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Thumb Color")]
    private Color thumbColor = Theme.Primary;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Track Width")]
    private float trackWidth = 6f;

    partial void OnTrackWidthChanged(float value)
    {
        trackWidth = Math.Clamp(value, 2f, 20f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Thumb Size")]
    private float thumbSize = 18f;

    partial void OnThumbSizeChanged(float value)
    {
        thumbSize = Math.Clamp(value, 10f, 36f);
    }

    #endregion

    #region === RUNTIME STATE ===

    [JsonIgnore, Browsable(false)]
    private bool _isHovered;

    [JsonIgnore, Browsable(false)]
    private bool _isPressed;

    [JsonIgnore, Browsable(false)]
    private bool _isDragging;

    [JsonIgnore, Browsable(false)]
    private SKRect _trackRect;

    [JsonIgnore, Browsable(false)]
    private SKRect _thumbRect;

    #endregion

    #region === CTOR ===

    public SliderVer()
    {
        IsActionControl = true;

        Name = "VerSlider";
        ResizeStyle = ResizeStyles.HeightOnly;

        ExplicitePreviewWidth = 30f;
        ExplicitePreviewHeight = 120f;

        Width = 30f;
        Height = 120f;

        MinWidth = 20f;
        MinHeight = 50f;

        MaxWidth = 50f;
        MaxHeight = 500f;
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === POINTER HOOKS ===

    public override void OnPointerDown(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode || ctx.Button != MouseButton.Left)
            return;

        if (!_trackRect.Contains(ctx.WorldPoint) && !_thumbRect.Contains(ctx.WorldPoint))
            return;

        _isPressed = true;
        _isHovered = true;
        _isDragging = true;

        SetValueFromPoint(ctx.WorldPoint.Y);
        InvalidateVisuals();
    }

    public override void OnPointerMove(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode)
        {
            ResetInteractionState();
            return;
        }

        bool isOverThumb = _thumbRect.Contains(ctx.WorldPoint);
        bool isOverTrack = _trackRect.Contains(ctx.WorldPoint);
        bool isInside = isOverThumb || isOverTrack;

        if (_isHovered != isInside)
        {
            _isHovered = isInside;
            InvalidateVisuals();
        }

        if (isInside)
            Mouse.OverrideCursor = Cursors.Hand;

        if (_isDragging)
        {
            SetValueFromPoint(ctx.WorldPoint.Y);
            InvalidateVisuals();
            return;
        }

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

        bool isInside = _trackRect.Contains(ctx.WorldPoint) || _thumbRect.Contains(ctx.WorldPoint);

        if (_isDragging)
            SetValueFromPoint(ctx.WorldPoint.Y);

        _isPressed = false;
        _isDragging = false;
        _isHovered = isInside;

        InvalidateVisuals();
    }

    public override void OnPointerLeave()
    {
        if (_isDragging)
            return;

        ResetInteractionState();
    }

    #endregion

    #region === RENDER ===

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        float safeThumbSize = Math.Clamp(ThumbSize, 10f, Math.Min(layout.Width, layout.Height));
        float safeTrackWidth = Math.Clamp(TrackWidth, 2f, safeThumbSize);

        float trackLeft = layout.Left + (layout.Width - safeTrackWidth) / 2f;

        _trackRect = new SKRect(
            trackLeft,
            layout.Top + safeThumbSize / 2f,
            trackLeft + safeTrackWidth,
            layout.Bottom - safeThumbSize / 2f
        );

        float range = Math.Max(0.0001f, Maximum - Minimum);
        float progress = (Value - Minimum) / range;
        progress = Math.Clamp(progress, 0f, 1f);

        float progressHeight = _trackRect.Height * progress;

        var progressRect = new SKRect(
            _trackRect.Left,
            _trackRect.Bottom - progressHeight,
            _trackRect.Right,
            _trackRect.Bottom
        );

        float thumbCenterY = _trackRect.Bottom - progressHeight;

        _thumbRect = new SKRect(
            layout.Left + (layout.Width - safeThumbSize) / 2f,
            thumbCenterY - safeThumbSize / 2f,
            layout.Left + (layout.Width + safeThumbSize) / 2f,
            thumbCenterY + safeThumbSize / 2f
        );

        DrawTrack(canvas, _trackRect);
        DrawProgress(canvas, progressRect);
        DrawThumb(canvas, _thumbRect, ctx);
    }

    #endregion

    #region === DRAW HELPERS ===

    private void DrawTrack(SKCanvas canvas, SKRect trackRect)
    {
        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: trackRect,
            cornerRadius: trackRect.Width / 2f,
            fillColor: TrackColor,
            fillStyle: FillStyle.Solid
        );
    }

    private void DrawProgress(SKCanvas canvas, SKRect progressRect)
    {
        if (progressRect.Height <= 0.5f)
            return;

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: progressRect,
            cornerRadius: progressRect.Width / 2f,
            fillColor: ProgressColor,
            fillStyle: FillStyle.Solid
        );
    }

    private void DrawThumb(SKCanvas canvas, SKRect thumbRect, RenderContext ctx)
    {
        Color fill = ThumbColor;

        if (ctx.LiveMode && _isHovered)
            fill = fill.Lighten(0.03f);

        if (ctx.LiveMode && (_isPressed || _isDragging))
            fill = fill.Darken(0.04f);

        using var fillPaint = new SKPaint
        {
            Color = fill.ToSKColor(),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        canvas.DrawRoundRect(thumbRect, thumbRect.Width / 2f, thumbRect.Width / 2f, fillPaint);

        using var borderPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 0.75f
        };

        canvas.DrawRoundRect(thumbRect, thumbRect.Width / 2f, thumbRect.Width / 2f, borderPaint);
    }

    #endregion

    #region === HELPERS ===

    private void SetValueFromPoint(float y)
    {
        if (_trackRect.Height <= 0.001f)
            return;

        float clampedY = Math.Clamp(y, _trackRect.Top, _trackRect.Bottom);
        float progress = (_trackRect.Bottom - clampedY) / _trackRect.Height;
        float newValue = Minimum + (Maximum - Minimum) * progress;

        Value = newValue;
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

        if (_isDragging)
        {
            _isDragging = false;
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
