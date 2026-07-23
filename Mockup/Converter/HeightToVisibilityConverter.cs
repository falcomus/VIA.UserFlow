// Datei: Mockup.Designer/Converters/HeightToVisibilityConverter.cs
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mockup.Converter;

/// <summary>
/// Wandelt eine Höhe (float/double/int/decimal/String) in Visibility um.
/// Standard: value > 0 → Visible, sonst Collapsed.
/// - ConverterParameter (optional): numerischer Schwellenwert, z. B. "1"
/// - Invert (optional): kehrt die Logik um (per Property im XAML-Resource-Eintrag)
/// </summary>
[ValueConversion(typeof(double), typeof(Visibility))]
public sealed class HeightToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Optional: Sichtbarkeitslogik invertieren (Visible ⇄ Collapsed)
    /// </summary>
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // 1) Höhe robust nach double konvertieren
        var h = ToDouble(value);

        // 2) Schwellenwert aus Parameter lesen (Default = 0.0)
        var threshold = ToDouble(parameter);

        // 3) Sichtbarkeitsentscheidung
        bool isVisible = h > threshold;

        if (Invert) isVisible = !isVisible;

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Für diesen Anwendungsfall nicht benötigt.
        return Binding.DoNothing;
    }

    private static double ToDouble(object? obj)
    {
        if (obj is null) return 0.0;
        return obj switch
        {
            double d => d,
            float f => f,
            int i => i,
            long l => l,
            decimal m => (double)m,
            string s => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0.0,
            _ => 0.0
        };
    }
}
