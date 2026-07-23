using SkiaSharp;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Media;

namespace Mockup.Extensions;

public static class ColorExtensions
{
    public static Color ToColor(this string hex)
    {
        hex = PrepareHex(hex);
        return Color.FromArgb(
            a: (byte)(Convert.ToUInt32(hex.Substring(0, 2), 16)),
            r: (byte)(Convert.ToUInt32(hex.Substring(2, 2), 16)),
            g: (byte)(Convert.ToUInt32(hex.Substring(4, 2), 16)),
            b: (byte)(Convert.ToUInt32(hex.Substring(6, 2), 16))
        );
    }

    public static SKColor ToSKColor(this string hex)
    {
        hex = PrepareHex(hex);
        return new SKColor(
            red: (byte)Convert.ToUInt32(hex.Substring(2, 2), 16),
            green: (byte)Convert.ToUInt32(hex.Substring(4, 2), 16),
            blue: (byte)Convert.ToUInt32(hex.Substring(6, 2), 16),
            alpha: (byte)Convert.ToUInt32(hex.Substring(0, 2), 16)
        );
    }

    /// <summary>
    /// Wandelt einen Hex-String (#RRGGBB oder #AARRGGBB) sicher in SKColor um.
    /// </summary>
    public static SKColor ToSKColorSafe(this string? hex, SKColor fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return fallback;

        try
        {
            if (hex.StartsWith("#"))
                hex = hex.Substring(1);

            if (hex.Length == 6)
            {
                var r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                var g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                var b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                return new SKColor(r, g, b);
            }
            else if (hex.Length == 8)
            {
                var a = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                var r = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                var g = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                var b = byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
                return new SKColor(r, g, b, a);
            }
        }
        catch { }

        return fallback;
    }

    public static SolidColorBrush ToSolidColorBrush(this string hex)
    {
        return new SolidColorBrush(hex.ToColor());
    }

    private static string PrepareHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            hex = Colors.Fuchsia.ToString();
            Debug.WriteLine("Ungültiger Hex-String");
        }

        // Entferne '#'
        hex = hex.Replace("#", "");

        // Normalisiere Länge
        return hex.Length switch
        {
            3 => $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}FF",
            4 => $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}{hex[3]}{hex[3]}",
            6 => "FF" + hex,
            8 => hex,
            _ => throw new FormatException("Ungültige Hex-Länge")
        };
    }

    // Konvertiert einen Farbnamen (z.B. "DodgerBlue") in Hex-String
    public static string ToHexColor(this string colorName)
    {
        if (string.IsNullOrWhiteSpace(colorName))
            throw new ArgumentException("Farbname darf nicht leer sein");

        // Versuche vordefinierte WPF-Farben zu nutzen
        var colorProperty = typeof(Colors).GetProperty(
            colorName.Trim(),
            System.Reflection.BindingFlags.IgnoreCase |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Static
        );

        if (colorProperty != null)
        {
            var colorValue = colorProperty.GetValue(null);
            if (colorValue is Color color)
            {
                return color.ToHexColor();
            }
        }

        // Alternativ: ColorConverter für nicht vordefinierte Farben
        try
        {
            var converted = ColorConverter.ConvertFromString(colorName);
            if (converted != null && converted is Color c)
                return c.ToHexColor();
        }
        catch
        {
            // Fehler wird weiter unten behandelt
        }

        throw new ArgumentException($"Unbekannte Farbe: {colorName}");
    }

    // Konvertiert WPF-Color in Hex-String
    public static string ToHexColor(this Color color)
    {
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    // Konvertiert SkiaSharp SKColor in Hex-String
    public static string ToHexColor(this SKColor color)
    {
        return $"#{color.Alpha:X2}{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
    }

    // Konvertiert SolidColorBrush in Hex-String
    public static string ToHexColor(this SolidColorBrush brush)
    {
        if (brush == null)
            throw new ArgumentNullException(nameof(brush));

        return brush.Color.ToHexColor();
    }


    /// <summary>
    /// Gibt RGBA-Werte eines Hex-Strings zurück.
    /// </summary>
    public static (byte R, byte G, byte B, byte A) ToRgba(this string hex)
    {
        var color = hex.ToSKColor();
        return (color.Red, color.Green, color.Blue, color.Alpha);
    }
}