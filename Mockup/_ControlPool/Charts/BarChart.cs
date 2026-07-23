// ======================================================================================
// FILE: Mockup.Controls/BarChart.cs
//
// PURPOSE:
// - Modern bar chart control for the mockup designer.
// - Supports axes, grid/help lines, labels, values and configurable bar appearance.
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

#region === BAR CHART ===

[ControlType(displayName: "Bar Chart", group: "Charts")]
public partial class BarChart : DesignControl
{
    #region === DATA ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Data")]
    [property: System.ComponentModel.DisplayName("Items")]
    private List<BarChartItem> items = [];

    #endregion

    #region === APPEARANCE ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Bar Color")]
    private Color barColor = Theme.Primary;

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
    [property: System.ComponentModel.DisplayName("Value Color")]
    private Color valueColor = Theme.Primary.Darken(0.10f);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Values")]
    private bool showValues = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Help Lines")]
    private bool showHelpLines = false;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Show Axes")]
    private bool showAxes = true;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Bar Spacing")]
    private float barSpacing = 12f;

    partial void OnBarSpacingChanged(float value)
    {
        barSpacing = Math.Clamp(value, 0f, 80f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Axis Width")]
    private float axisWidth = 1.5f;

    partial void OnAxisWidthChanged(float value)
    {
        axisWidth = Math.Clamp(value, 0.5f, 8f);
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

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Bar Corner Radius")]
    private float barCornerRadius = 3f;

    partial void OnBarCornerRadiusChanged(float value)
    {
        barCornerRadius = Math.Clamp(value, 0f, 40f);
    }

    #endregion

    #region === TYPOGRAPHY ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Value Font Size")]
    private float valueFontSize = 10f;

    partial void OnValueFontSizeChanged(float value)
    {
        valueFontSize = Math.Clamp(value, 6f, 30f);
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Typography")]
    [property: System.ComponentModel.DisplayName("Label Font Size")]
    private float labelFontSize = 11f;

    partial void OnLabelFontSizeChanged(float value)
    {
        labelFontSize = Math.Clamp(value, 6f, 30f);
    }

    #endregion

    #region === LAYOUT ===

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Padding Left")]
    private float paddingLeft = 18f;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Padding Right")]
    private float paddingRight = 8f;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Padding Top")]
    private float paddingTop = 8f;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    [property: System.ComponentModel.DisplayName("Padding Bottom")]
    private float paddingBottom = 18f;

    #endregion

    #region === CTOR ===

    public BarChart()
    {
        Name = "BarChart";
        ResizeStyle = ResizeStyles.ResizeAll;

        Width = 170f;
        Height = 120f;

        MinWidth = 150f;
        MinHeight = 100f;

        MaxWidth = 800f;
        MaxHeight = 600f;

        Items =
        [
            new BarChartItem { Label = "Jan", Value = 45 },
            new BarChartItem { Label = "Feb", Value = 80 },
            new BarChartItem { Label = "Mar", Value = 60 }
        ];
    }

    public override string ToString() => string.Empty;

    #endregion

    #region === RENDER ===

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        if (Items == null || Items.Count == 0)
            return;

        var chartArea = new SKRect(
            layout.Left + PaddingLeft,
            layout.Top + PaddingTop,
            layout.Right - PaddingRight,
            layout.Bottom - PaddingBottom
        );

        if (chartArea.Width <= 8f || chartArea.Height <= 8f)
            return;

        if (ShowHelpLines)
            DrawGridLines(canvas, chartArea);

        if (ShowAxes)
            DrawAxes(canvas, chartArea);

        DrawBars(canvas, chartArea);
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

        const int steps = 5;
        float stepHeight = chartArea.Height / steps;

        for (int i = 0; i <= steps; i++)
        {
            float y = chartArea.Bottom - (i * stepHeight);
            canvas.DrawLine(chartArea.Left, y, chartArea.Right, y, gridPaint);
        }
    }

    private void DrawAxisLabels(SKCanvas canvas, SKRect chartArea)
    {
        if (Items.Count == 0)
            return;

        float safeBarWidth = GetBarWidth(chartArea);
        float xPos = chartArea.Left + safeBarWidth / 2f;
        float step = safeBarWidth + BarSpacing;

        foreach (var item in Items)
        {
            TextRenderer.Draw(
                canvas: canvas,
                text: item.Label,
                bounds: new SKRect(xPos - 30f, chartArea.Bottom + 1f, xPos + 30f, chartArea.Bottom + 22f),
                fontSize: LabelFontSize,
                color: LabelColor.ToSKColor(),
                fontFamily: Theme.FontFamily,
                padding: 0f
            );

            xPos += step;
        }

        double maxValue = GetMaxValue();
        const int ySteps = 5;
        float yStepHeight = chartArea.Height / ySteps;

        for (int i = 0; i <= ySteps; i++)
        {
            float y = chartArea.Bottom - (i * yStepHeight);
            double value = i * (maxValue / ySteps);

            TextRenderer.Draw(
                canvas: canvas,
                text: value.ToString("0"),
                bounds: new SKRect(chartArea.Left - 30f, y - 10f, chartArea.Left + 4f, y + 10f),
                fontSize: LabelFontSize,
                color: LabelColor.ToSKColor(),
                fontFamily: Theme.FontFamily,
                padding: 0f
            );
        }
    }

    private void DrawBars(SKCanvas canvas, SKRect chartArea)
    {
        if (Items.Count == 0)
            return;

        double maxValue = GetMaxValue();
        if (maxValue <= 0)
            return;

        float barWidth = GetBarWidth(chartArea);
        float xPos = chartArea.Left + 1f;

        foreach (var item in Items)
        {
            float barHeight = (float)(item.Value / maxValue * chartArea.Height);

            var barRect = new SKRect(
                xPos,
                chartArea.Bottom - barHeight,
                xPos + barWidth,
                chartArea.Bottom - 1f
            );

            SkiaRenderer.DrawRect(
                canvas: canvas,
                rect: barRect,
                cornerRadius: Math.Clamp(BarCornerRadius, 0f, Math.Min(barRect.Width, barRect.Height) / 2f),
                fillStyle: FillStyle.Solid,
                fillColor: BarColor,
                borderStyle: BorderStyle.Solid,
                borderColor: BarColor
            );

            if (ShowValues)
            {
                TextRenderer.Draw(
                    canvas: canvas,
                    text: item.Value.ToString("0"),
                    bounds: new SKRect(barRect.Left - 6f, barRect.Top - 20f, barRect.Right + 6f, barRect.Top),
                    fontSize: ValueFontSize,
                    color: ValueColor.ToSKColor(),
                    fontFamily: Theme.FontFamily,
                    padding: 0f
                );
            }

            xPos += barWidth + BarSpacing;
        }
    }

    #endregion

    #region === HELPERS ===

    private float GetBarWidth(SKRect chartArea)
    {
        if (Items.Count <= 0)
            return 0f;

        float totalSpacing = Math.Max(0, Items.Count - 1) * BarSpacing;
        float width = (chartArea.Width - totalSpacing) / Items.Count;
        return Math.Max(2f, width - 0.5f);
    }

    private double GetMaxValue()
    {
        double max = 0d;

        foreach (var item in Items)
        {
            if (item.Value > max)
                max = item.Value;
        }

        return max <= 0d ? 1d : max;
    }

    #endregion
}

#endregion

#region === BAR CHART ITEM ===

public class BarChartItem
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
}

#endregion

