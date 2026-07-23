// ======================================================================================
// FILE: Mockup.Controls/Gauge.cs
//
// PURPOSE:
// - Modern gauge / radial progress control for the mockup designer.
// - Visual style aligned with the updated light-mode control library.
// - Supports value range, progress arc, optional value text and configurable angles.
//
// PROJECT: Mockup.Controls
// GROUP: Miscellaneous
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Windows;
using System.Windows.Media;

namespace Mockup.Controls;

#region === GAUGE ===

[ControlType(displayName: "Gauge", group: "Charts")]
public partial class Gauge : DesignControl
{
    #region === VALUE ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Min Value")]
    private float minValue = 0f;

    partial void OnMinValueChanged(float value)
    {
        if (maxValue < value)
            maxValue = value;

        this.value = Math.Clamp(this.value, minValue, maxValue);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Max Value")]
    private float maxValue = 100f;

    partial void OnMaxValueChanged(float value)
    {
        if (value < minValue)
            maxValue = minValue;

        this.value = Math.Clamp(this.value, minValue, maxValue);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Value")]
    private float value = 50f;

    partial void OnValueChanged(float value)
    {
        this.value = Math.Clamp(value, MinValue, MaxValue);
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
    [property: System.ComponentModel.DisplayName("Text Color")]
    private Color textColor = Theme.Primary;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Gauge Width")]
    private float gaugeWidth = 10f;

    partial void OnGaugeWidthChanged(float value)
    {
        gaugeWidth = Math.Clamp(value, 2f, 40f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Round Caps")]
    private bool roundCaps = true;

    #endregion

    #region === Behavior,Typography, Content  ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Value")]
    private bool showValue = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Value Font Size")]
    private float valueFontSize = 16f;

    partial void OnValueFontSizeChanged(float value)
    {
        valueFontSize = Math.Clamp(value, 8f, 48f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Value Font Weight")]
    private FontWeight valueFontWeight = FontWeights.SemiBold;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Content")]
    [property: System.ComponentModel.DisplayName("Suffix")]
    private string suffix = string.Empty;

    #endregion

    #region === LAYOUT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Start Angle")]
    private float startAngle = 135f;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Sweep Angle")]
    private float sweepAngle = 270f;

    partial void OnSweepAngleChanged(float value)
    {
        sweepAngle = Math.Clamp(value, 1f, 360f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Inner Offset Y")]
    private float innerOffsetY = 3f;

    #endregion

    #region === CTOR ===

    public Gauge()
    {
        Name = "Gauge";

        ResizeStyle = ResizeStyles.KeepRatio;

        Width = 80f;
        Height = 80f;

        MinWidth = 50f;
        MinHeight = 50f;

        MaxWidth = 300f;
        MaxHeight = 300f;
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === RENDER ===

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        float safeValue = Math.Clamp(Value, MinValue, MaxValue);
        float range = Math.Max(0.0001f, MaxValue - MinValue);
        float progress = (safeValue - MinValue) / range;
        progress = Math.Clamp(progress, 0f, 1f);

        float safeGaugeWidth = Math.Clamp(GaugeWidth, 2f, Math.Min(layout.Width, layout.Height) / 2f - 2f);
        float diameter = Math.Max(4f, Math.Min(layout.Width, layout.Height) - safeGaugeWidth - 4f);
        float radius = diameter / 2f;

        SKPoint center = new(layout.MidX, layout.MidY + InnerOffsetY);
        float progressAngle = SweepAngle * progress;

        using var trackPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = TrackColor.ToSKColor(),
            StrokeWidth = safeGaugeWidth,
            IsAntialias = true,
            StrokeCap = RoundCaps ? SKStrokeCap.Round : SKStrokeCap.Butt
        };

        using var progressPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = ProgressColor.ToSKColor(),
            StrokeWidth = safeGaugeWidth,
            IsAntialias = true,
            StrokeCap = RoundCaps ? SKStrokeCap.Round : SKStrokeCap.Butt
        };

        DrawArc(canvas, center, radius, StartAngle, SweepAngle, trackPaint);
        DrawArc(canvas, center, radius, StartAngle, progressAngle, progressPaint);

        if (ShowValue)
        {
            string text = safeValue.ToString("0") + Suffix;

            var textRect = new SKRect(
                layout.Left,
                layout.MidY - ValueFontSize,
                layout.Right,
                layout.MidY + ValueFontSize + 4f
            );

            TextRenderer.Draw(
                canvas: canvas,
                text: text,
                bounds: textRect,
                fontSize: ValueFontSize,
                color: TextColor.ToSKColor(),
                fontFamily: Theme.FontFamily,
                fontWeight: ValueFontWeight
            );
        }
    }

    #endregion

    #region === HELPERS ===

    private static void DrawArc(SKCanvas canvas, SKPoint center, float radius, float startAngle, float sweepAngle, SKPaint paint)
    {
        var rect = new SKRect(
            center.X - radius,
            center.Y - radius,
            center.X + radius,
            center.Y + radius
        );

        canvas.DrawArc(rect, startAngle, sweepAngle, false, paint);
    }

    #endregion
}

#endregion
