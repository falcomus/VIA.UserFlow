using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mockup.JsonConverters;

public sealed class TemplateConverter : JsonConverter<ScreenTemplate>
{
    public override ScreenTemplate Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var template = new ScreenTemplate();

        // ------------------------------------------------------------
        // Basis
        // ------------------------------------------------------------

        if (root.TryGetProperty("id", out var idProp))
            template.Id = idProp.GetInt64();

        if (root.TryGetProperty("name", out var nameProp))
            template.Name = nameProp.GetString() ?? "Template";

        // ------------------------------------------------------------
        // Metadaten
        // ------------------------------------------------------------

        if (root.TryGetProperty("description", out var descProp))
            template.Description = descProp.GetString();

        if (root.TryGetProperty("groupName", out var catProp))
            template.GroupName = catProp.GetString() ?? "General";

        // ------------------------------------------------------------
        // WICHTIG: Größe (Design-Time)
        // ------------------------------------------------------------

        if (root.TryGetProperty("width", out var widthProp))
            template.Width = (float)widthProp.GetDouble();

        if (root.TryGetProperty("height", out var heightProp))
            template.Height = (float)heightProp.GetDouble();

        // ------------------------------------------------------------
        // Bands (ohne Parent!)
        // ------------------------------------------------------------

        if (root.TryGetProperty("bands", out var bandsProp) &&
            bandsProp.ValueKind == JsonValueKind.Array)
        {
            template.Bands.Clear();

            foreach (var bandEl in bandsProp.EnumerateArray())
            {
                var band = bandEl.Deserialize<Band>(options);
                if (band == null)
                    continue;

                band.EnsureInitialPage();
                template.Bands.Add(band);
            }
        }

        return template;
    }

    public override void Write(
        Utf8JsonWriter writer,
        ScreenTemplate value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteNumber("id", value.Id);
        writer.WriteString("name", value.Name);
        writer.WriteString("type", "Template");

        if (!string.IsNullOrWhiteSpace(value.Description))
            writer.WriteString("description", value.Description);

        writer.WriteString("groupName", value.GroupName);

        // ------------------------------------------------------------
        // WICHTIG: Größe persistieren
        // ------------------------------------------------------------

        writer.WriteNumber("width", value.Width);
        writer.WriteNumber("height", value.Height);

        // ------------------------------------------------------------
        // Bands
        // ------------------------------------------------------------

        writer.WritePropertyName("bands");
        writer.WriteStartArray();

        foreach (var band in value.Bands)
            JsonSerializer.Serialize(writer, band, options);

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
