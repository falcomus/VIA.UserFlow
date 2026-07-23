using SkiaSharp;
using System.Windows.Media;

namespace Mockup.ColorSystem;

public static class ColorExtensions
{
    public static Color WithAlpha(this Color c, byte alpha)
        => Color.FromArgb(alpha, c.R, c.G, c.B);

    public static Color WithAlpha(this Color c, int alpha)
        => Color.FromArgb((byte)Math.Clamp(alpha, 0, 255), c.R, c.G, c.B);

    public static Color Lighten(this Color c, float amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        byte r = (byte)Math.Clamp(c.R + 255 * amount, 0, 255);
        byte g = (byte)Math.Clamp(c.G + 255 * amount, 0, 255);
        byte b = (byte)Math.Clamp(c.B + 255 * amount, 0, 255);
        return Color.FromArgb(c.A, r, g, b);
    }

    public static Color Darken(this Color c, float amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        byte r = (byte)Math.Clamp(c.R - 255 * amount, 0, 255);
        byte g = (byte)Math.Clamp(c.G - 255 * amount, 0, 255);
        byte b = (byte)Math.Clamp(c.B - 255 * amount, 0, 255);
        return Color.FromArgb(c.A, r, g, b);
    }

    public static SKColor ToSk(this Color c)
        => new SKColor(c.R, c.G, c.B, c.A);

    public static SolidColorBrush ToBrush(this Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}