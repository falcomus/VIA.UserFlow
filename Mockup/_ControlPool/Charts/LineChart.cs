// ======================================================================================
// FILE: Mockup.Controls/LineChartControl.cs
//
// PURPOSE:
// - Modern line chart control for the mockup designer.
// - Supports line, area fill, grid, axes, labels and data points.
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

#region === LINE CHART CONTROL ===

[ControlType(displayName: "Line Chart", group: "Charts")]
public partial class LineChartControl : DesignControl
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
    [property: System.ComponentModel.DisplayName("Line Color")]
    private Color lineColor = Theme.Primary;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Area Color")]
    private Color areaColor = Theme.Primary.WithAlpha(20);

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
    [property: System.ComponentModel.DisplayName("Show Area")]
    private bool showArea = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Grid")]
    private bool showGrid = false;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Axis Labels")]
    private bool showAxisLabels = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Axes")]
    private bool showAxes = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Smooth Line")]
    private bool smoothLine = false;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Point Size")]
    private float pointSize = 4.5f;

    partial void OnPointSizeChanged(float value)
    {
        pointSize = Math.Clamp(value, 1f, 20f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Line Width")]
    private float lineWidth = 2.5f;

    partial void OnLineWidthChanged(float value)
    {
        lineWidth = Math.Clamp(value, 0.5f, 12f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Grid Line Width")]
    private float gridLineWidth = 1.0f;

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
    private float labelFontSize = 10f;

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
    private float paddingLeft = 30f;

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
    private float paddingBottom = 20f;

    #endregion

    #region === CTOR ===

    public LineChartControl()
    {
        Name = "LineChart";
        ResizeStyle = ResizeStyles.ResizeAll;

        Width = 170f;
        Height = 120f;

        MinWidth = 150f;
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

        if (chartArea.Width <= 5f || chartArea.Height <= 5f)
            return;

        if (ShowGrid)
            DrawGridLines(canvas, chartArea);

        if (ShowAxes)
            DrawAxes(canvas, chartArea);

        if (ShowArea)
            DrawArea(canvas, chartArea);

        DrawLine(canvas, chartArea);

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
            StrokeWidth = 1.2f,
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

        int ySteps = 5;
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

        int ySteps = 5;
        float yStepSize = chartArea.Height / ySteps;

        for (int i = 0; i <= ySteps; i++)
        {
            float y = chartArea.Bottom - (i * yStepSize);
            double value = minY + (yRange / ySteps) * i;

            TextRenderer.Draw(
                canvas: canvas,
                text: value.ToString("0"),
                bounds: new SKRect(chartArea.Left - 28f, y - 10f, chartArea.Left - 4f, y + 10f),
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
                bounds: new SKRect(x - 18f, chartArea.Bottom + 2f, x + 18f, chartArea.Bottom + 20f),
                fontSize: LabelFontSize,
                color: LabelColor.ToSKColor(),
                fontFamily: Theme.FontFamily,
                padding: 0f
            );
        }
    }

    private void DrawArea(SKCanvas canvas, SKRect chartArea)
    {
        double minY = GetMinY();
        double maxY = GetMaxY();
        double yRange = Math.Max(0.0001, maxY - minY);

        using var path = new SKPath();
        float xStepSize = chartArea.Width / Math.Max(1, DataPoints.Count - 1);

        path.MoveTo(chartArea.Left, chartArea.Bottom);

        for (int i = 0; i < DataPoints.Count; i++)
        {
            float x = chartArea.Left + (i * xStepSize);
            float y = chartArea.Bottom - (float)((DataPoints[i].YValue - minY) / yRange * chartArea.Height);
            path.LineTo(x, y);
        }

        path.LineTo(chartArea.Right, chartArea.Bottom);
        path.Close();

        using var paint = new SKPaint
        {
            Color = AreaColor.ToSKColor(),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        canvas.DrawPath(path, paint);
    }

    private void DrawLine(SKCanvas canvas, SKRect chartArea)
    {
        var points = GetChartPoints(chartArea);
        if (points.Count < 2)
            return;

        using var path = new SKPath();

        if (SmoothLine && points.Count >= 3)
        {
            path.MoveTo(points[0]);

            for (int i = 0; i < points.Count - 1; i++)
            {
                var current = points[i];
                var next = points[i + 1];
                float midX = (current.X + next.X) / 2f;

                path.CubicTo(
                    midX, current.Y,
                    midX, next.Y,
                    next.X, next.Y
                );
            }
        }
        else
        {
            path.MoveTo(points[0]);
            for (int i = 1; i < points.Count; i++)
                path.LineTo(points[i]);
        }

        using var paint = new SKPaint
        {
            Color = LineColor.ToSKColor(),
            StrokeWidth = LineWidth,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

        canvas.DrawPath(path, paint);
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

    #region === DATA POINT ===

    public class ChartDataPoint
    {
        public double XValue { get; set; }
        public double YValue { get; set; }
    }

    #endregion
}

#endregion






//using CommunityToolkit.Mvvm.ComponentModel;
//using Mockup.ColorSystem;
//using Mockup.Registry;
//using Mockup.Rendering;
//using SkiaSharp;
//using SkiaSharp.Views.WPF;
//using System.Windows.Media;

//namespace Mockup.Controls;

//[ControlType(displayName: "Line Chart", group: "Data Visualization")]

//public partial class LineChartControl : DesignControl
//{
//    // ────────────────────────────────
//    // Property DataPoints
//    // ────────────────────────────────
//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Data")]
//    [property: System.ComponentModel.DisplayName("DataPoints")]
//    private List<ChartDataPoint> dataPoints = [];

//    // ────────────────────────────────
//    // Property LineColor
//    // ────────────────────────────────
//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Line Color")]
//    private Color lineColor = Theme.Primary;

//    // ────────────────────────────────
//    // Property AreaColor
//    // ────────────────────────────────
//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Area Color")]
//    private Color areaColor = Theme.Primary.WithAlpha(20);

//    // ────────────────────────────────
//    // Property AxisColor
//    // ────────────────────────────────
//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Axis Color")]
//    private Color axisColor = Theme.Text;

//    // ────────────────────────────────
//    // Property ShowDataPoints
//    // ────────────────────────────────
//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Show DataPoints")]
//    private bool showDataPoints = true;

//    // ────────────────────────────────
//    // Property ShowArea
//    // ────────────────────────────────
//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Show Area")]
//    private bool showArea = true;

//    // ────────────────────────────────
//    // Property PointSize
//    // ────────────────────────────────
//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Point Size")]
//    private float pointSize = 5.0f;

//    // ────────────────────────────────
//    // Property LineWidth
//    // ────────────────────────────────
//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Line Width")]
//    private float lineWidth = 3.0f;

//    // ────────────────────────────────
//    // Property GridLineWidth
//    // ────────────────────────────────
//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("GridLine Width")]
//    private float gridLineWidth = 1.0f;

//    // ────────────────────────────────
//    // Property ShowGrid
//    // ────────────────────────────────
//    [ObservableProperty]
//    [property: ControlProp]
//    [property: System.ComponentModel.Category("Appearance")]
//    [property: System.ComponentModel.DisplayName("Show Grid")]
//    private bool showGrid = false;


//    // Padding innerhalb des Controls
//    private const float paddingLeft = 30f;
//    private const float paddingRight = 10f;
//    private const float paddingTop = 10f;
//    private const float paddingBottom = 20f;

//    public LineChartControl()
//    {
//        Name = "LineChart";
//        ResizeStyle = ResizeStyles.ResizeAll;

//        Width = 170;
//        Height = 120;

//        MinWidth = 150;
//        MinHeight = 100;

//        MaxWidth = 800;
//        MaxHeight = 600;

//        // Beispiel-Daten
//        DataPoints = new List<ChartDataPoint>
//        {
//            new ChartDataPoint { XValue = 1, YValue = 30 },
//            new ChartDataPoint { XValue = 2, YValue = 90 },
//            new ChartDataPoint { XValue = 3, YValue = 50 },
//            new ChartDataPoint { XValue = 4, YValue = 80 },
//            new ChartDataPoint { XValue = 5, YValue = 40 }
//        };
//    }

//    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
//    {
//        if (DataPoints == null || DataPoints.Count < 2) return;

//        // Zeichenbereich berechnen (ohne Padding)
//        var chartArea = new SKRect(
//            layout.Left + paddingLeft,
//            layout.Top + paddingTop,
//            layout.Right - paddingRight,
//            layout.Bottom - paddingBottom
//        );

//        // Achsen und Grid zeichnen
//        DrawAxesAndGrid(canvas, chartArea);

//        // Fläche zeichnen (wenn aktiviert)
//        if (ShowArea)
//        {
//            DrawArea(canvas, chartArea);
//        }

//        // Linie zeichnen
//        DrawLine(canvas, chartArea);

//        // Datenpunkte zeichnen (wenn aktiviert)
//        if (ShowDataPoints)
//        {
//            DrawDataPoints(canvas, chartArea);
//        }
//    }

//    private void DrawAxesAndGrid(SKCanvas canvas, SKRect chartArea)
//    {
//        using var axisPaint = new SKPaint
//        {
//            Color = AxisColor.ToSKColor(),
//            StrokeWidth = 1.5f,
//            IsAntialias = true
//        };

//        // X-Achse
//        canvas.DrawLine(
//            chartArea.Left, chartArea.Bottom,
//            chartArea.Right, chartArea.Bottom,
//            axisPaint);

//        // Y-Achse
//        canvas.DrawLine(
//            chartArea.Left, chartArea.Top,
//            chartArea.Left, chartArea.Bottom,
//            axisPaint);

//        // Grid-Linien zeichnen
//        if (ShowGrid)
//        {
//            DrawGridLines(canvas, chartArea);
//        }

//        // Achsenbeschriftung
//        DrawAxisLabels(canvas, chartArea);
//    }

//    private void DrawGridLines(SKCanvas canvas, SKRect chartArea)
//    {
//        using var gridPaint = new SKPaint
//        {
//            Color = AxisColor.ToSKColor().WithAlpha(0x20),
//            StrokeWidth = GridLineWidth,
//            IsAntialias = true
//        };

//        double maxY = DataPoints.Max(p => p.YValue);
//        int ySteps = 5;
//        float yStepSize = chartArea.Height / ySteps;

//        // Horizontale Grid-Linien
//        for (int i = 0; i <= ySteps; i++)
//        {
//            float y = chartArea.Bottom - (i * yStepSize);
//            canvas.DrawLine(
//                chartArea.Left, y,
//                chartArea.Right, y,
//                gridPaint);
//        }

//        // Vertikale Grid-Linien
//        int xSteps = DataPoints.Count - 1;
//        float xStepSize = chartArea.Width / xSteps;
//        for (int i = 0; i <= xSteps; i++)
//        {
//            float x = chartArea.Left + (i * xStepSize);
//            canvas.DrawLine(
//                x, chartArea.Top,
//                x, chartArea.Bottom,
//                gridPaint);
//        }
//    }

//    private void DrawAxisLabels(SKCanvas canvas, SKRect chartArea)
//    {
//        // Y-Achsen-Labels
//        double maxY = DataPoints.Max(p => p.YValue);
//        int ySteps = 5;
//        float yStepSize = chartArea.Height / ySteps;

//        for (int i = 0; i <= ySteps; i++)
//        {
//            float y = chartArea.Bottom - (i * yStepSize);
//            double value = i * (maxY / ySteps);

//            TextRenderer.Draw(
//                canvas: canvas,
//                text: value.ToString("0"),
//                bounds: new SKRect(chartArea.Left - 25, y - 10, chartArea.Left - 5, y + 10),
//                fontSize: 10f,
//                color: AxisColor.ToSKColor(),
//                fontFamily: Theme.FontFamily,
//                padding: 0f);
//        }

//        // X-Achsen-Labels
//        float xStepSize = chartArea.Width / (DataPoints.Count - 1);
//        for (int i = 0; i < DataPoints.Count; i++)
//        {
//            float x = chartArea.Left + (i * xStepSize);

//            TextRenderer.Draw(
//                canvas: canvas,
//                text: DataPoints[i].XValue.ToString(),
//                bounds: new SKRect(x - 15, chartArea.Bottom + 0, x + 15, chartArea.Bottom + 25),
//                fontSize: 10f,
//                color: AxisColor.ToSKColor(),
//                fontFamily: Theme.FontFamily,
//                padding: 0f);
//        }
//    }

//    private void DrawArea(SKCanvas canvas, SKRect chartArea)
//    {
//        double maxY = DataPoints.Max(p => p.YValue);
//        if (maxY <= 0) return;

//        using var path = new SKPath();
//        float xStepSize = chartArea.Width / (DataPoints.Count - 1);

//        path.MoveTo(chartArea.Left, chartArea.Bottom);

//        for (int i = 0; i < DataPoints.Count; i++)
//        {
//            float x = chartArea.Left + (i * xStepSize);
//            float y = chartArea.Bottom - (float)(DataPoints[i].YValue / maxY * chartArea.Height);
//            path.LineTo(x, y);
//        }

//        path.LineTo(chartArea.Right, chartArea.Bottom);
//        path.Close();

//        using var paint = new SKPaint
//        {
//            Color = AreaColor.ToSKColor(),
//            Style = SKPaintStyle.Fill,
//            IsAntialias = true
//        };
//        canvas.DrawPath(path, paint);
//    }

//    private void DrawLine(SKCanvas canvas, SKRect chartArea)
//    {
//        double maxY = DataPoints.Max(p => p.YValue);
//        if (maxY <= 0) return;

//        using var path = new SKPath();
//        float xStepSize = chartArea.Width / (DataPoints.Count - 1);

//        for (int i = 0; i < DataPoints.Count; i++)
//        {
//            float x = chartArea.Left + (i * xStepSize);
//            float y = chartArea.Bottom - (float)(DataPoints[i].YValue / maxY * chartArea.Height);

//            if (i == 0)
//                path.MoveTo(x, y);
//            else
//                path.LineTo(x, y);
//        }

//        using var paint = new SKPaint
//        {
//            Color = LineColor.ToSKColor(),
//            StrokeWidth = LineWidth,
//            Style = SKPaintStyle.Stroke,
//            IsAntialias = true,
//            StrokeCap = SKStrokeCap.Round,
//            StrokeJoin = SKStrokeJoin.Round
//        };
//        canvas.DrawPath(path, paint);
//    }

//    private void DrawDataPoints(SKCanvas canvas, SKRect chartArea)
//    {
//        double maxY = DataPoints.Max(p => p.YValue);
//        if (maxY <= 0) return;

//        float xStepSize = chartArea.Width / (DataPoints.Count - 1);

//        using var paint = new SKPaint
//        {
//            Color = LineColor.ToSKColor(),
//            Style = SKPaintStyle.Fill,
//            IsAntialias = true
//        };

//        for (int i = 0; i < DataPoints.Count; i++)
//        {
//            float x = chartArea.Left + (i * xStepSize);
//            float y = chartArea.Bottom - (float)(DataPoints[i].YValue / maxY * chartArea.Height);

//            // Äußeren Kreis (Rand)
//            canvas.DrawCircle(x, y, PointSize + 1, new SKPaint
//            {
//                Color = SKColors.White,
//                Style = SKPaintStyle.Fill,
//                IsAntialias = true
//            });

//            // Inneren Kreis (Farbe)
//            canvas.DrawCircle(x, y, PointSize, paint);
//        }
//    }

//    public class ChartDataPoint
//    {
//        public double XValue { get; set; }
//        public double YValue { get; set; }
//    }

//}