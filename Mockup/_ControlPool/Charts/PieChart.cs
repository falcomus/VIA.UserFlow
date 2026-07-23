// ======================================================================================
// FILE: Mockup.Controls/PieChart.cs
//
// PURPOSE:
// - Modern pie / donut chart control for the mockup designer.
// - Supports configurable segments, labels, donut hole and segment stroke.
// - Visual style aligned with the updated light-mode control library.
//
// PROJECT: Mockup.Controls
// GROUP: Data Visualization
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Mockup.Controls;

#region === PIE CHART ===

[ControlType(displayName: "Pie Chart", group: "Charts")]
public partial class PieChart : DesignControl
{
    #region === DATA ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Data")]
    [property: System.ComponentModel.DisplayName("Pie Segments")]
    private ObservableCollection<PieSegment> segments = [];

    #endregion

    #region === APPEARANCE ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Labels")]
    private bool showLabels = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Label Font Size")]
    private float labelFontSize = 10f;

    partial void OnLabelFontSizeChanged(float value)
    {
        labelFontSize = Math.Clamp(value, 6f, 32f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Label Color")]
    private Color labelColor = Colors.White;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Hole Size")]
    private float holeSize = 0.4f;

    partial void OnHoleSizeChanged(float value)
    {
        holeSize = Math.Clamp(value, 0f, 0.85f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Stroke Width")]
    private float strokeWidth = 2f;

    partial void OnStrokeWidthChanged(float value)
    {
        strokeWidth = Math.Clamp(value, 0f, 12f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Stroke Color")]
    private Color strokeColor = Colors.White;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Hole Color")]
    private Color holeColor = Colors.White;

    #endregion

    #region === DEFAULT COLORS ===

    private static readonly Color[] DefaultColors =
    [
        (Color)ColorConverter.ConvertFromString("#4285F4"),
        (Color)ColorConverter.ConvertFromString("#EA4335"),
        (Color)ColorConverter.ConvertFromString("#FBBC05"),
        (Color)ColorConverter.ConvertFromString("#34A853"),
        (Color)ColorConverter.ConvertFromString("#673AB7"),
        (Color)ColorConverter.ConvertFromString("#FF5722"),
        (Color)ColorConverter.ConvertFromString("#009688"),
        (Color)ColorConverter.ConvertFromString("#795548"),
        (Color)ColorConverter.ConvertFromString("#9E9E9E"),
        (Color)ColorConverter.ConvertFromString("#CDDC39")
    ];

    #endregion

    #region === CTOR ===

    public PieChart()
    {
        Name = "PieChart";
        ResizeStyle = ResizeStyles.KeepRatio;

        Width = 170f;
        Height = 170f;

        MinWidth = 100f;
        MinHeight = 100f;

        MaxWidth = 500f;
        MaxHeight = 500f;

        ExplicitePreviewHeight = 100f;
        ExplicitePreviewWidth = 170f;


        Segments =
        [
            new PieSegment { Value = 35, Label = "A" },
            new PieSegment { Value = 25, Label = "B" },
            new PieSegment { Value = 20, Label = "C" },
            new PieSegment { Value = 15, Label = "D" },
            new PieSegment { Value = 10, Label = "E" }
        ];
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === RENDER ===

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        if (Segments == null || Segments.Count == 0)
            return;

        double total = Segments.Sum(s => s.Value);
        if (total <= 0)
            return;

        float diameter = Math.Min(layout.Width, layout.Height);
        float radius = diameter / 2f;
        SKPoint center = new(layout.MidX, layout.MidY);

        float startAngle = -90f;

        for (int i = 0; i < Segments.Count; i++)
        {
            var segment = Segments[i];
            if (segment.Value <= 0)
                continue;

            float sweepAngle = (float)(360d * (segment.Value / total));
            Color color = segment.Color != Colors.Transparent
                ? segment.Color
                : DefaultColors[i % DefaultColors.Length];

            DrawPieSegment(canvas, center, radius, startAngle, sweepAngle, color, segment.Label);
            startAngle += sweepAngle;
        }

        if (HoleSize > 0f)
            DrawHole(canvas, center, radius * HoleSize);
    }

    #endregion

    #region === DRAW HELPERS ===

    private void DrawPieSegment(
        SKCanvas canvas,
        SKPoint center,
        float radius,
        float startAngle,
        float sweepAngle,
        Color color,
        string label)
    {
        using var path = new SKPath();

        path.MoveTo(center);
        path.ArcTo(
            new SKRect(
                center.X - radius,
                center.Y - radius,
                center.X + radius,
                center.Y + radius),
            startAngle,
            sweepAngle,
            false);
        path.Close();

        using (var fillPaint = new SKPaint
        {
            Color = color.ToSKColor(),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        })
        {
            canvas.DrawPath(path, fillPaint);
        }

        if (StrokeWidth > 0f)
        {
            using var strokePaint = new SKPaint
            {
                Color = StrokeColor.ToSKColor(),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = StrokeWidth,
                IsAntialias = true
            };

            canvas.DrawPath(path, strokePaint);
        }

        if (ShowLabels && !string.IsNullOrWhiteSpace(label))
            DrawSegmentLabel(canvas, center, radius, startAngle, sweepAngle, label);
    }

    private void DrawHole(SKCanvas canvas, SKPoint center, float holeRadius)
    {
        using var paint = new SKPaint
        {
            Color = HoleColor.ToSKColor(),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        canvas.DrawCircle(center, holeRadius, paint);
    }

    private void DrawSegmentLabel(
        SKCanvas canvas,
        SKPoint center,
        float radius,
        float startAngle,
        float sweepAngle,
        string label)
    {
        float middleAngle = startAngle + (sweepAngle / 2f);
        float labelRadius = radius * (HoleSize > 0f ? (1f + HoleSize) / 2f : 0.68f);

        float x = center.X + labelRadius * MathF.Cos(DegreesToRadians(middleAngle));
        float y = center.Y + labelRadius * MathF.Sin(DegreesToRadians(middleAngle));

        TextRenderer.Draw(
            canvas: canvas,
            text: label,
            bounds: new SKRect(x - 50f, y - 14f, x + 50f, y + 14f),
            fontSize: LabelFontSize,
            color: LabelColor.ToSKColor(),
            fontFamily: Theme.FontFamily
        );
    }

    private static float DegreesToRadians(float degrees)
    {
        return degrees * MathF.PI / 180f;
    }

    #endregion
}

#endregion

#region === PIE SEGMENT ===

public partial class PieSegment : ObservableObject
{
    [ObservableProperty]
    private double value;

    [ObservableProperty]
    private string label = string.Empty;

    [ObservableProperty]
    private Color color = Colors.Transparent;
}

#endregion
