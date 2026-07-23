// ============================================================================
// FILE: Mockup.JsonConverters/BandPageConverter.cs
// NEW MODEL: still persists Page.Height; MinHeight is derived
// ============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mockup.JsonConverters;

public sealed class BandPageConverter : JsonConverter<BandPage>
{
    public override BandPage Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var page = new BandPage();

        if (root.TryGetProperty("id", out var idProp))
            page.Id = idProp.GetInt64();

        if (root.TryGetProperty("name", out var nameProp))
            page.Name = nameProp.GetString() ?? "Page";

        if (root.TryGetProperty("title", out var titleProp))
            page.Title = titleProp.GetString();

        if (root.TryGetProperty("height", out var heightProp))
            page.Height = heightProp.GetSingle();

        if (root.TryGetProperty("controls", out var controlsProp) &&
            controlsProp.ValueKind == JsonValueKind.Array)
        {
            page.Controls.Clear();

            foreach (var ctrlElem in controlsProp.EnumerateArray())
            {
                var control = JsonSerializer.Deserialize<DesignControl>(
                    ctrlElem.GetRawText(), options);

                if (control != null)
                {
                    control.ParentBandPage = page;
                    page.Controls.Add(control);
                }
            }
        }

        page.InvalidateMinHeight();
        page.EnsureMinHeight();

        return page;
    }

    public override void Write(
        Utf8JsonWriter writer,
        BandPage value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteNumber("id", value.Id);
        writer.WriteString("name", value.Name);
        writer.WriteString("title", value.Title ?? string.Empty);
        writer.WriteNumber("height", value.Height);

        writer.WritePropertyName("controls");
        writer.WriteStartArray();
        foreach (var control in value.Controls)
            JsonSerializer.Serialize(writer, control, options);
        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}
