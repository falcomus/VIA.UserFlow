// ======================================================================================
// FILE: Mockup/JsonConverters/ScreenConverter.cs
//
// ZWECK:
// System.Text.Json-Converter für Screen.
// - Color wird als Hex-String "#AARRGGBB" gespeichert
// - Keine Width / Height (kommen nicht aus Screen)
// - UserHeight ist die einzige höhenrelevante Persistenz
// - Bands werden über BandConverter serialisiert
// ======================================================================================

using Mockup.ViewModel;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace Mockup.JsonConverters;

public sealed class ScreenConverter : JsonConverter<Screen>
{
    public override Screen Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var screen = new Screen();
        float? userHeight = null;

        // ------------------------------------------------------------
        // Basis
        // ------------------------------------------------------------

        if (root.TryGetProperty("id", out var idProp))
            screen.Id = idProp.GetInt64();

        if (root.TryGetProperty("name", out var nameProp))
            screen.Name = nameProp.GetString() ?? "";

        if (root.TryGetProperty("groupName", out var groupNameProp))
            screen.GroupName =
                groupNameProp.GetString() ?? MockupViewModel.DEFAULT_SCREEN_GROUPNAME;

        if (root.TryGetProperty("descr", out var descrProp))
            screen.Descr = descrProp.GetString() ?? "";

        // ------------------------------------------------------------
        // Background
        // ------------------------------------------------------------

        if (root.TryGetProperty("background", out var bgProp))
        {
            var s = bgProp.GetString();
            if (!string.IsNullOrWhiteSpace(s))
            {
                try
                {
                    screen.Background = (Color)ColorConverter.ConvertFromString(s)!;
                }
                catch
                {
                    // ignore invalid color
                }
            }
        }

        if (root.TryGetProperty("backgroundImageFilename", out var bgImgFilename))
        {
            screen.BackgroundImageFilename = bgImgFilename.GetString();
        }

        if (root.TryGetProperty("backgroundImageBase64", out var bgImgProp))
        {
            screen.BackgroundImageBase64 =
                bgImgProp.ValueKind == JsonValueKind.Null ? null : bgImgProp.GetString();
        }

        // ------------------------------------------------------------
        // Size
        // ------------------------------------------------------------

        if (root.TryGetProperty("userHeight", out var uhProp))
            userHeight = uhProp.GetSingle();

        // ------------------------------------------------------------
        // Flags
        // ------------------------------------------------------------

        if (root.TryGetProperty("showHeader", out var shProp))
            screen.ShowHeader = shProp.GetBoolean();

        if (root.TryGetProperty("showFooter", out var sfProp))
            screen.ShowFooter = sfProp.GetBoolean();

        if (root.TryGetProperty("showBackButton", out var sBB))
            screen.ShowBackButton = sBB.GetBoolean();

        if (root.TryGetProperty("showHamburgerButton", out var sHB))
            screen.ShowHamburgerButton = sHB.GetBoolean();

        if (root.TryGetProperty("isHomeScreen", out var sHS))
            screen.IsHomeScreen = sHS.GetBoolean();

        // ------------------------------------------------------------
        // Bands
        // ------------------------------------------------------------

        if (
            root.TryGetProperty("bands", out var bandsProp)
            && bandsProp.ValueKind == JsonValueKind.Array
        )
        {
            try
            {
                var bands = JsonSerializer.Deserialize<ObservableCollection<Band>>(
                    bandsProp.GetRawText(),
                    options
                );

                screen.Bands = bands ?? new ObservableCollection<Band>();
            }
            catch
            {
                screen.Bands = new ObservableCollection<Band>();
            }
        }
        else
        {
            screen.Bands = new ObservableCollection<Band>();
        }

        if (userHeight.HasValue)
            screen.UserHeight = userHeight.Value;

        return screen;
    }

    public override void Write(Utf8JsonWriter writer, Screen value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteNumber("id", value.Id);
        writer.WriteString("name", value.Name);
        writer.WriteString("groupName", value.GroupName);
        writer.WriteString("descr", value.Descr);

        // ------------------------------------------------------------
        // Background
        // ------------------------------------------------------------

        writer.WriteString("background", value.Background.ToString());
        writer.WriteString("backgroundImageFilename", value.BackgroundImageFilename);
        writer.WriteString("backgroundImageBase64", value.BackgroundImageBase64);

        // ------------------------------------------------------------
        // Size
        // ------------------------------------------------------------

        writer.WriteNumber("userHeight", value.UserHeight);

        // ------------------------------------------------------------
        // Flags
        // ------------------------------------------------------------

        writer.WriteBoolean("showHeader", value.ShowHeader);
        writer.WriteBoolean("showFooter", value.ShowFooter);
        writer.WriteBoolean("showBackButton", value.ShowBackButton);
        writer.WriteBoolean("showHamburgerButton", value.ShowHamburgerButton);
        writer.WriteBoolean("isHomeScreen", value.IsHomeScreen);

        // ------------------------------------------------------------
        // Bands
        // ------------------------------------------------------------

        writer.WritePropertyName("bands");
        JsonSerializer.Serialize(writer, value.Bands, options);

        writer.WriteEndObject();
    }
}
