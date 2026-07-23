// ======================================================================================
// FILE: Mockup.ColorSystem/Theme.cs
//
// PURPOSE:
// Kompatibilitätsschicht für alte Controls, die Theme.Primary usw.
// verwenden. Delegiert an ThemeService.
//
// AUTHOR: Claus Falkenstein / ChatGPT
// VERSION: 1.0
// ======================================================================================

using System.Windows;
using System.Windows.Media;

namespace Mockup.ColorSystem;

public static class Theme
{
    public static Color Primary => ThemeService.Primary;
    public static Color Accent => ThemeService.Accent;
    public static Color Info => ThemeService.Info;
    public static Color Warning => ThemeService.Warning;
    public static Color Error => ThemeService.Error;
    public static Color Success => ThemeService.Success;
    public static Color Neutral => ThemeService.Neutral;

    public static Color Text => ThemeService.Text;
    public static Color ControlBG => ThemeService.ControlBG;
    public static Color ControlBorder => ThemeService.ControlBorder;

    public static float CornerRadius => ThemeService.CornerRadius;

    public static string FontFamily => ThemeService.FontFamily;

    public static FontWeight FontWeightNormal => ThemeService.FontWeightNormal;
    public static FontWeight FontWeightBold => ThemeService.FontWeightBold;
    public static FontWeight FontWeightLight => ThemeService.FontWeightLight;
}
