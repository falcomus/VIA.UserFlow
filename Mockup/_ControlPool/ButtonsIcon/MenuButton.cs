using Mockup.ColorSystem;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace Mockup.Controls;

[ControlType(displayName: "Overflow Menu Button", group: "Icon Buttons")]
public class MenuButton : DesignControl
{

    public MenuButton()
    {
        Name = "MenuButton";

        ResizeStyle = ResizeStyles.None;

        ExplicitePreviewHeight = 50f;
        ExplicitePreviewWidth = 50f;

        Width = 25;
        Height = 30;

        MinWidth = 25;
        MinHeight = 30;

        MaxWidth = 25;
        MaxHeight = 30;
    }

    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        float padding = 0.3f;
        float dotsize = 1.3f;

        float contentSize = Math.Min(layout.Width, layout.Height) * (1 - padding * 2);
        float centerX = layout.MidX;
        float centerY = layout.MidY;

        // Berechne vertikale Positionen für die drei Punkte
        float startY = centerY - contentSize / 1.5f + dotsize;
        float middleY = centerY;
        float endY = centerY + contentSize / 1.5f - dotsize;

        using var paint = new SKPaint
        {
            Color = Theme.Text.ToSKColor(),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        // Drei Punkte zeichnen
        canvas.DrawCircle(centerX, startY, dotsize, paint);
        canvas.DrawCircle(centerX, middleY, dotsize, paint);
        canvas.DrawCircle(centerX, endY, dotsize, paint);

    }


}