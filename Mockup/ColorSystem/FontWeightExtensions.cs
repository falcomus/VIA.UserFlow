using System.Windows;

namespace Mockup.ColorSystem;

public static class FontWeightExtensions
{
    public static int ToFontWeightValue(this FontWeight w)
    {
        if (w == FontWeights.Black) return 900;
        if (w == FontWeights.ExtraBold) return 800;
        if (w == FontWeights.Bold) return 700;
        if (w == FontWeights.SemiBold) return 600;
        if (w == FontWeights.Medium) return 500;
        if (w == FontWeights.Normal) return 400;
        if (w == FontWeights.Regular) return 400;
        if (w == FontWeights.Light) return 300;
        if (w == FontWeights.Thin) return 200;
        if (w == FontWeights.ExtraLight) return 200;

        return 400;
    }
}
