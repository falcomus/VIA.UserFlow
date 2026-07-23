// ======================================================================================
// FILE: Mockup.Renderer/SkColorExtensions.cs
//
// PURPOSE:
// Erweiterungsmethoden für SKColor, passend zum alten Verhalten.
// Wird benötigt, weil Theme jetzt WPF-Color liefert.
//
// AUTOR: Claus Falkenstein / ChatGPT
// VERSION: 1.0
// ======================================================================================

namespace Mockup.ColorSystem;


public static class SkColorExtensions
{
    /// <summary>
    /// Multipliziert die Helligkeit (Brightness) im HSL-Farbraum.
    /// </summary>
    //public static SKColor WithBrightness(this SKColor color, float factor)
    //{
    //    factor = Math.Clamp(factor, 0f, 2f);

    //    // SKColor → HSL
    //    float[] hsl = color.ToHsl();

    //    // Luminanz skalieren
    //    hsl[2] = Math.Clamp(hsl[2] * factor, 0f, 1f);

    //    // zurück zu SKColor
    //    return SKColor.FromHsl(hsl[0], hsl[1] * 100, hsl[2] * 100, color.Alpha);
    //}

    /// <summary>
    /// Liefert HSL-Werte eines SKColor als float[3].
    /// hsl[0] = Hue (0..360)
    /// hsl[1] = Saturation (0..1)
    /// hsl[2] = Lightness (0..1)
    /// </summary>

    //public static float[] ToHsl(this SKColor c)
    //{
    //    float r = c.Red / 255f;
    //    float g = c.Green / 255f;
    //    float b = c.Blue / 255f;

    //    float max = Math.Max(r, Math.Max(g, b));
    //    float min = Math.Min(r, Math.Min(g, b));

    //    float h = 0f;
    //    float s;
    //    float l = (max + min) / 2f;

    //    if (max != min)
    //    {
    //        float d = max - min;
    //        s = l > 0.5f ? d / (2f - max - min) : d / (max + min);

    //        if (max == r)
    //            h = (g - b) / d + (g < b ? 6 : 0);
    //        else if (max == g)
    //            h = (b - r) / d + 2;
    //        else
    //            h = (r - g) / d + 4;

    //        h /= 6;
    //    }
    //    else
    //    {
    //        s = 0f;
    //    }

    //    return new float[] { h * 360f, s, l };
    //}
}
