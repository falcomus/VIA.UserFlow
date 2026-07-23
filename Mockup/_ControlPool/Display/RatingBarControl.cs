using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Windows.Media;


namespace Mockup.Controls;

[ControlType(displayName: "Rating Bar", group: "Indicators")]
public partial class RatingBarControl : DesignControl
{
    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Max Stars")]
    private int maxStars = 5; // Maximal 8 Sterne
    partial void OnMaxStarsChanged(int value) => MaxStars = Math.Clamp(value, 1, 8);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Rating")]
    private float rating = 2.5f; // Kann z.B. 3.5 für halbe Sterne sein
    partial void OnRatingChanged(float value) => Rating = Math.Clamp(value, 0, MaxStars);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Empty Star Color")]
    private Color emptyStarColor = Color.FromRgb(220, 221, 223);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Appearance")]
    [property: System.ComponentModel.DisplayName("Filled Star Color")]
    private Color filledStarColor = Theme.Primary;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Star Size")]
    private float starSize = 20f;
    partial void OnStarSizeChanged(float value) => StarSize = Math.Clamp(value, 10, 50);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Value")]
    [property: System.ComponentModel.DisplayName("Star Spacing")]
    private float starSpacing = 5f;
    partial void OnStarSpacingChanged(float value) => StarSpacing = Math.Clamp(value, 0, 20);

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Behavior")]
    [property: System.ComponentModel.DisplayName("Allow Half Stars")]
    private bool allowHalfStars = true;

    public RatingBarControl()
    {
        Name = "Rating Bar";

        ResizeStyle = ResizeStyles.None; // Größe wird automatisch berechnet

        // Größe basierend auf Sternen
        Width = (5 * StarSize) + (4 * StarSpacing); // Maximalgröße für 5 Sterne
        Height = StarSize;

        MinWidth = StarSize;
        MinHeight = StarSize;

        MaxWidth = float.MaxValue;
        MaxHeight = float.MaxValue;
    }

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        // Wert begrenzen
        Rating = (float)Math.Clamp(Rating, 0.0, (float)MaxStars);
        MaxStars = Math.Clamp(MaxStars, 1, 8); // Maximal 8 Sterne

        float xPos = layout.Left;

        for (int i = 0; i < MaxStars; i++)
        {
            var starRect = new SKRect(
                xPos,
                layout.Top + (layout.Height - StarSize) / 2,
                xPos + StarSize,
                layout.Top + (layout.Height + StarSize) / 2
            );

            // Entscheiden wie viel vom Stern gefüllt werden soll
            float fillPercentage = (float)Math.Clamp(Rating - i, 0, 1);

            // Stern zeichnen
            DrawStar(canvas, starRect, fillPercentage);

            xPos += StarSize + StarSpacing;
        }

        float calcWidth = (MaxStars * StarSize) + ((MaxStars - 1) * StarSpacing);
        float calcHeight = StarSize - 2;

        if (Width != calcWidth)
            Width = calcWidth;

        if (Height != calcHeight)
            Height = calcHeight;
    }

    private void DrawStar(SKCanvas canvas, SKRect rect, float fillPercentage)
    {
        // Stern-Pfad erstellen
        var starPath = new SKPath();
        float centerX = rect.MidX;
        float centerY = rect.MidY;
        float radius = rect.Width / 2;

        // Stern mit 5 Zacken
        for (int i = 0; i < 10; i++)
        {
            float angle = (float)(i * Math.PI / 5) - (float)(Math.PI / 2);
            float r = (i % 2 == 0) ? radius : radius * 0.4f;
            float x = centerX + r * (float)Math.Cos(angle);
            float y = centerY + r * (float)Math.Sin(angle);

            if (i == 0)
                starPath.MoveTo(x, y);
            else
                starPath.LineTo(x, y);
        }
        starPath.Close();

        // Leeren Stern zeichnen
        using (var paint = new SKPaint
        {
            Color = EmptyStarColor.ToSKColor(),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        })
        {
            canvas.DrawPath(starPath, paint);
        }

        // Gefüllten Teil zeichnen
        if (fillPercentage > 0)
        {
            // Clip für den gefüllten Teil
            var clipRect = new SKRect(
                rect.Left,
                rect.Top,
                rect.Left + (rect.Width * fillPercentage),
                rect.Bottom
            );

            canvas.Save();
            canvas.ClipRect(clipRect);

            using (var paint = new SKPaint
            {
                Color = FilledStarColor.ToSKColor(),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            })
            {
                canvas.DrawPath(starPath, paint);
            }

            canvas.Restore();
        }
    }


}