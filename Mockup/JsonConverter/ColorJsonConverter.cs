using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace Mockup.JsonConverters;

/// <summary>
/// JSON converter for WPF Color objects (System.Windows.Media.Color).
/// Converts Color to/from string representation in "#AARRGGBB" or named color format.
/// </summary>
public sealed class ColorJsonConverter : JsonConverter<Color>
{
    /// <summary>
    /// Reads a Color from JSON string representation.
    /// Supports formats: "#AARRGGBB", "#RRGGBB", named colors (e.g., "Red", "Transparent").
    /// Returns Colors.Transparent if conversion fails.
    /// </summary>
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Handle null or empty values
        if (reader.TokenType == JsonTokenType.Null)
            return Colors.Transparent;

        // Get the string value
        var colorString = reader.GetString();

        if (string.IsNullOrWhiteSpace(colorString))
            return Colors.Transparent;

        try
        {
            // Handle different color formats
            if (colorString.StartsWith("#"))
            {
                // Ensure proper format for ColorConverter
                if (colorString.Length == 7) // #RRGGBB
                {
                    // Convert to #AARRGGBB with full opacity
                    colorString = "#FF" + colorString.Substring(1);
                }
                else if (colorString.Length == 9) // #AARRGGBB
                {
                    // Already in correct format
                }
            }

            var converted = ColorConverter.ConvertFromString(colorString);
            if (converted is Color color)
                return color;
        }
        catch (Exception ex)
        {
            // Log conversion error (optional)
            System.Diagnostics.Debug.WriteLine($"Color conversion failed for '{colorString}': {ex.Message}");

            // Fallback: Try manual parsing for common formats
            if (TryParseColorManually(colorString, out var fallbackColor))
                return fallbackColor;
        }

        return Colors.Transparent;
    }

    /// <summary>
    /// Writes a Color to JSON as string representation.
    /// Uses standard "#AARRGGBB" format for consistency.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        // Use standard #AARRGGBB format for all colors except Transparent
        if (value == Colors.Transparent || value.A == 0)
        {
            writer.WriteStringValue("Transparent");
        }
        else
        {
            // Use #AARRGGBB format for all opaque/semi-transparent colors
            string hexString = $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
            writer.WriteStringValue(hexString);
        }
    }

    /// <summary>
    /// Manual color parsing for fallback when ColorConverter fails.
    /// Supports basic formats and named colors.
    /// </summary>
    private bool TryParseColorManually(string colorString, out Color color)
    {
        color = Colors.Transparent;

        if (string.IsNullOrWhiteSpace(colorString))
            return false;

        // Trim and convert to uppercase for easier comparison
        colorString = colorString.Trim().ToUpperInvariant();

        try
        {
            // Handle hex formats
            if (colorString.StartsWith("#"))
            {
                // Remove # prefix
                string hex = colorString.Substring(1);

                // Parse based on length
                switch (hex.Length)
                {
                    case 6: // RRGGBB
                        var r = Convert.ToByte(hex.Substring(0, 2), 16);
                        var g = Convert.ToByte(hex.Substring(2, 2), 16);
                        var b = Convert.ToByte(hex.Substring(4, 2), 16);
                        color = Color.FromRgb(r, g, b);
                        return true;

                    case 8: // AARRGGBB
                        var a = Convert.ToByte(hex.Substring(0, 2), 16);
                        r = Convert.ToByte(hex.Substring(2, 2), 16);
                        g = Convert.ToByte(hex.Substring(4, 2), 16);
                        b = Convert.ToByte(hex.Substring(6, 2), 16);
                        color = Color.FromArgb(a, r, g, b);
                        return true;

                    case 3: // RGB (short form)
                        r = Convert.ToByte(new string(hex[0], 2), 16);
                        g = Convert.ToByte(new string(hex[1], 2), 16);
                        b = Convert.ToByte(new string(hex[2], 2), 16);
                        color = Color.FromRgb(r, g, b);
                        return true;

                    case 4: // ARGB (short form)
                        a = Convert.ToByte(new string(hex[0], 2), 16);
                        r = Convert.ToByte(new string(hex[1], 2), 16);
                        g = Convert.ToByte(new string(hex[2], 2), 16);
                        b = Convert.ToByte(new string(hex[3], 2), 16);
                        color = Color.FromArgb(a, r, g, b);
                        return true;
                }
            }

            // Handle "sc#" format (sc#1.0,0.5,0.0,1.0)
            if (colorString.StartsWith("SC#"))
            {
                string values = colorString.Substring(3);
                string[] parts = values.Split(',');

                if (parts.Length == 4)
                {
                    float scA = float.Parse(parts[0]);
                    float scR = float.Parse(parts[1]);
                    float scG = float.Parse(parts[2]);
                    float scB = float.Parse(parts[3]);

                    color = Color.FromScRgb(scA, scR, scG, scB);
                    return true;
                }
            }
        }
        catch
        {
            // Parsing failed
            return false;
        }

        return false;
    }
}

/// <summary>
/// Optional: Converter for nullable Color values.
/// Useful when properties can be null.
/// </summary>
public sealed class NullableColorJsonConverter : JsonConverter<Color?>
{
    public override Color? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var converter = new ColorJsonConverter();
        return converter.Read(ref reader, typeof(Color), options);
    }

    public override void Write(Utf8JsonWriter writer, Color? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            var converter = new ColorJsonConverter();
            converter.Write(writer, value.Value, options);
        }
    }
}