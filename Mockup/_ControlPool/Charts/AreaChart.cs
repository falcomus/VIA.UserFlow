// ======================================================================================
// FILE: Mockup.Controls/AreaChart.cs
//
// PURPOSE:
// - Modern area chart control for the mockup designer.
// - Supports axes, grid lines, labels, area fill, line and optional data points.
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
using System.Windows.Media;


namespace Mockup.Controls;

#region === AREA CHART ===

[ControlType(displayName: "Area Chart", group: "Charts")]
public partial class AreaChart : DesignControl
{
    #region === DATA ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Data")]
    [property: System.ComponentModel.DisplayName("Data Points")]
    private List<ChartDataPoint> dataPoints = [];

    #endregion

    #region === APPEARANCE ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Area Color")]
    private Color areaColor = Theme.Primary.WithAlpha(46);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Line Color")]
    private Color lineColor = Theme.Primary;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Axis Color")]
    private Color axisColor = Theme.Text;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Grid Color")]
    private Color gridColor = Theme.Text.WithAlpha(28);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Label Color")]
    private Color labelColor = Theme.Text.Lighten(0.10f);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Point Fill Color")]
    private Color pointFillColor = Theme.Primary;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Point Border Color")]
    private Color pointBorderColor = Colors.White;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Data Points")]
    private bool showDataPoints = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Grid Lines")]
    private bool showGridLines = false;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Axes")]
    private bool showAxes = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Axis Labels")]
    private bool showAxisLabels = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Smooth Line")]
    private bool smoothLine = false;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Line Width")]
    private float lineWidth = 1.8f;

    partial void OnLineWidthChanged(float value)
    {
        lineWidth = Math.Clamp(value, 0.5f, 12f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Point Size")]
    private float pointSize = 4f;

    partial void OnPointSizeChanged(float value)
    {
        pointSize = Math.Clamp(value, 1f, 20f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Axis Width")]
    private float axisWidth = 1.3f;

    partial void OnAxisWidthChanged(float value)
    {
        axisWidth = Math.Clamp(value, 0.5f, 8f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Grid Line Width")]
    private float gridLineWidth = 0.8f;

    partial void OnGridLineWidthChanged(float value)
    {
        gridLineWidth = Math.Clamp(value, 0.5f, 6f);
    }

    #endregion

    #region === TYPOGRAPHY ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Label Font Size")]
    private float labelFontSize = 9f;

    partial void OnLabelFontSizeChanged(float value)
    {
        labelFontSize = Math.Clamp(value, 6f, 24f);
    }

    #endregion

    #region === LAYOUT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Padding Left")]
    private float paddingLeft = 25f;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Padding Right")]
    private float paddingRight = 10f;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Padding Top")]
    private float paddingTop = 10f;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Padding Bottom")]
    private float paddingBottom = 15f;

    #endregion

    #region === CTOR ===

    public AreaChart()
    {
        Name = "Area Chart";
        ResizeStyle = ResizeStyles.ResizeAll;

        Width = 170f;
        Height = 120f;

        MinWidth = 120f;
        MinHeight = 100f;

        MaxWidth = 800f;
        MaxHeight = 600f;

        DataPoints =
        [
            new ChartDataPoint { XValue = 1, YValue = 30 },
            new ChartDataPoint { XValue = 2, YValue = 90 },
            new ChartDataPoint { XValue = 3, YValue = 50 },
            new ChartDataPoint { XValue = 4, YValue = 80 },
            new ChartDataPoint { XValue = 5, YValue = 40 }
        ];
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === RENDER ===

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        if (DataPoints == null || DataPoints.Count < 2)
            return;

        var chartArea = new SKRect(
            layout.Left + PaddingLeft,
            layout.Top + PaddingTop,
            layout.Right - PaddingRight,
            layout.Bottom - PaddingBottom
        );

        if (chartArea.Width <= 8f || chartArea.Height <= 8f)
            return;

        if (ShowGridLines)
            DrawGridLines(canvas, chartArea);

        if (ShowAxes)
            DrawAxes(canvas, chartArea);

        DrawAreaAndLine(canvas, chartArea);

        if (ShowDataPoints)
            DrawDataPoints(canvas, chartArea);

        if (ShowAxisLabels)
            DrawAxisLabels(canvas, chartArea);
    }

    #endregion

    #region === DRAW HELPERS ===

    private void DrawAxes(SKCanvas canvas, SKRect chartArea)
    {
        using var axisPaint = new SKPaint
        {
            Color = AxisColor.ToSKColor(),
            StrokeWidth = AxisWidth,
            IsAntialias = true
        };

        canvas.DrawLine(chartArea.Left, chartArea.Bottom, chartArea.Right, chartArea.Bottom, axisPaint);
        canvas.DrawLine(chartArea.Left, chartArea.Top, chartArea.Left, chartArea.Bottom, axisPaint);
    }

    private void DrawGridLines(SKCanvas canvas, SKRect chartArea)
    {
        using var gridPaint = new SKPaint
        {
            Color = GridColor.ToSKColor(),
            StrokeWidth = GridLineWidth,
            IsAntialias = true
        };

        const int ySteps = 5;
        float yStepSize = chartArea.Height / ySteps;

        for (int i = 0; i <= ySteps; i++)
        {
            float y = chartArea.Bottom - (i * yStepSize);
            canvas.DrawLine(chartArea.Left, y, chartArea.Right, y, gridPaint);
        }

        int xSteps = Math.Max(1, DataPoints.Count - 1);
        float xStepSize = chartArea.Width / xSteps;

        for (int i = 0; i <= xSteps; i++)
        {
            float x = chartArea.Left + (i * xStepSize);
            canvas.DrawLine(x, chartArea.Top, x, chartArea.Bottom, gridPaint);
        }
    }

    private void DrawAxisLabels(SKCanvas canvas, SKRect chartArea)
    {
        double minY = GetMinY();
        double maxY = GetMaxY();
        double yRange = Math.Max(0.0001, maxY - minY);

        const int ySteps = 5;
        float yStepSize = chartArea.Height / ySteps;

        for (int i = 0; i <= ySteps; i++)
        {
            float y = chartArea.Bottom - (i * yStepSize);
            double value = minY + (yRange / ySteps) * i;

            TextRenderer.Draw(
                canvas: canvas,
                text: value.ToString("0"),
                bounds: new SKRect(chartArea.Left - 28f, y - 10f, chartArea.Left - 2f, y + 10f),
                fontSize: LabelFontSize,
                color: LabelColor.ToSKColor(),
                fontFamily: Theme.FontFamily,
                padding: 0f
            );
        }

        float xStepSize = chartArea.Width / Math.Max(1, DataPoints.Count - 1);

        for (int i = 0; i < DataPoints.Count; i++)
        {
            float x = chartArea.Left + (i * xStepSize);

            TextRenderer.Draw(
                canvas: canvas,
                text: DataPoints[i].XValue.ToString(),
                bounds: new SKRect(x - 18f, chartArea.Bottom + 2f, x + 18f, chartArea.Bottom + 22f),
                fontSize: LabelFontSize,
                color: LabelColor.ToSKColor(),
                fontFamily: Theme.FontFamily,
                padding: 0f
            );
        }
    }

    private void DrawAreaAndLine(SKCanvas canvas, SKRect chartArea)
    {
        var points = GetChartPoints(chartArea);
        if (points.Count < 2)
            return;

        using var areaPath = new SKPath();
        using var linePath = new SKPath();

        areaPath.MoveTo(points[0].X, chartArea.Bottom);
        areaPath.LineTo(points[0]);

        if (SmoothLine && points.Count >= 3)
        {
            linePath.MoveTo(points[0]);

            for (int i = 0; i < points.Count - 1; i++)
            {
                var current = points[i];
                var next = points[i + 1];
                float midX = (current.X + next.X) / 2f;

                linePath.CubicTo(
                    midX, current.Y,
                    midX, next.Y,
                    next.X, next.Y
                );

                areaPath.CubicTo(
                    midX, current.Y,
                    midX, next.Y,
                    next.X, next.Y
                );
            }
        }
        else
        {
            linePath.MoveTo(points[0]);

            for (int i = 1; i < points.Count; i++)
            {
                linePath.LineTo(points[i]);
                areaPath.LineTo(points[i]);
            }
        }

        areaPath.LineTo(points[^1].X, chartArea.Bottom);
        areaPath.Close();

        using var areaPaint = new SKPaint
        {
            Color = AreaColor.ToSKColor(),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        canvas.DrawPath(areaPath, areaPaint);

        using var linePaint = new SKPaint
        {
            Color = LineColor.ToSKColor(),
            StrokeWidth = LineWidth,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

        canvas.DrawPath(linePath, linePaint);
    }

    private void DrawDataPoints(SKCanvas canvas, SKRect chartArea)
    {
        var points = GetChartPoints(chartArea);
        if (points.Count == 0)
            return;

        using var borderPaint = new SKPaint
        {
            Color = PointBorderColor.ToSKColor(),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        using var fillPaint = new SKPaint
        {
            Color = PointFillColor.ToSKColor(),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        foreach (var p in points)
        {
            canvas.DrawCircle(p.X, p.Y, PointSize + 1f, borderPaint);
            canvas.DrawCircle(p.X, p.Y, PointSize, fillPaint);
        }
    }

    #endregion

    #region === HELPERS ===

    private List<SKPoint> GetChartPoints(SKRect chartArea)
    {
        var result = new List<SKPoint>();

        if (DataPoints == null || DataPoints.Count == 0)
            return result;

        double minY = GetMinY();
        double maxY = GetMaxY();
        double yRange = Math.Max(0.0001, maxY - minY);
        float xStepSize = chartArea.Width / Math.Max(1, DataPoints.Count - 1);

        for (int i = 0; i < DataPoints.Count; i++)
        {
            float x = chartArea.Left + (i * xStepSize);
            float y = chartArea.Bottom - (float)((DataPoints[i].YValue - minY) / yRange * chartArea.Height);
            result.Add(new SKPoint(x, y));
        }

        return result;
    }

    private double GetMinY()
    {
        return DataPoints.Count == 0 ? 0d : DataPoints.Min(p => p.YValue);
    }

    private double GetMaxY()
    {
        return DataPoints.Count == 0 ? 1d : DataPoints.Max(p => p.YValue);
    }

    #endregion
}

#endregion

#region === CHART DATA POINT ===

public class ChartDataPoint
{
    public double XValue { get; set; }
    public double YValue { get; set; }
}

#endregion
