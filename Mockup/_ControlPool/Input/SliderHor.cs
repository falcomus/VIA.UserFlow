// ======================================================================================
// FILE: Mockup.Controls/HorSliderControl.cs
//
// PURPOSE:
// - Modern horizontal slider control for the mockup designer.
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

#region === HOR SLIDER CONTROL ===

[ControlType(displayName: "Slider – Horizontal", group: "Pickers & Sliders")]
public partial class SliderHorControl : DesignControl
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
    [property: System.ComponentModel.DisplayName("Track Height")]
    private float trackHeight = 6f;

    partial void OnTrackHeightChanged(float value)
    {
        trackHeight = Math.Clamp(value, 2f, 20f);
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

    public SliderHorControl()
    {
        IsActionControl = true;

        Name = "HorSlider";
        ResizeStyle = ResizeStyles.WidthOnly;

        ExplicitePreviewWidth = 120f;
        ExplicitePreviewHeight = 30f;

        Width = 120f;
        Height = 30f;

        MinWidth = 50f;
        MinHeight = 20f;

        MaxWidth = 500f;
        MaxHeight = 50f;
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

        SetValueFromPoint(ctx.WorldPoint.X);
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
            SetValueFromPoint(ctx.WorldPoint.X);
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
            SetValueFromPoint(ctx.WorldPoint.X);

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
        float safeTrackHeight = Math.Clamp(TrackHeight, 2f, safeThumbSize);

        float trackTop = layout.Top + (layout.Height - safeTrackHeight) / 2f;

        _trackRect = new SKRect(
            layout.Left + safeThumbSize / 2f,
            trackTop,
            layout.Right - safeThumbSize / 2f,
            trackTop + safeTrackHeight
        );

        float range = Math.Max(0.0001f, Maximum - Minimum);
        float progress = (Value - Minimum) / range;
        progress = Math.Clamp(progress, 0f, 1f);

        float progressWidth = _trackRect.Width * progress;

        var progressRect = new SKRect(
            _trackRect.Left,
            _trackRect.Top,
            _trackRect.Left + progressWidth,
            _trackRect.Bottom
        );

        float thumbCenterX = _trackRect.Left + progressWidth;

        _thumbRect = new SKRect(
            thumbCenterX - safeThumbSize / 2f,
            layout.Top + (layout.Height - safeThumbSize) / 2f,
            thumbCenterX + safeThumbSize / 2f,
            layout.Top + (layout.Height + safeThumbSize) / 2f
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
            cornerRadius: trackRect.Height / 2f,
            fillColor: TrackColor,
            fillStyle: FillStyle.Solid
        );
    }

    private void DrawProgress(SKCanvas canvas, SKRect progressRect)
    {
        if (progressRect.Width <= 0.5f)
            return;

        SkiaRenderer.DrawRect(
            canvas: canvas,
            rect: progressRect,
            cornerRadius: progressRect.Height / 2f,
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

        canvas.DrawRoundRect(thumbRect, thumbRect.Height / 2f, thumbRect.Height / 2f, fillPaint);

        using var borderPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 0.75f
        };

        canvas.DrawRoundRect(thumbRect, thumbRect.Height / 2f, thumbRect.Height / 2f, borderPaint);
    }

    #endregion

    #region === HELPERS ===

    private void SetValueFromPoint(float x)
    {
        if (_trackRect.Width <= 0.001f)
            return;

        float clampedX = Math.Clamp(x, _trackRect.Left, _trackRect.Right);
        float progress = (clampedX - _trackRect.Left) / _trackRect.Width;
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
