using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mockup.JsonConverters;

public sealed class ProjectConverter : JsonConverter<Project>
{
    public override Project Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var project = new Project();

        // ============================================================
        // BASISDATEN
        // ============================================================

        if (root.TryGetProperty("id", out var idProp))
            project.Id = idProp.GetInt64();

        if (root.TryGetProperty("name", out var nameProp))
            project.Name = nameProp.GetString() ?? "Unnamed Project";

        if (root.TryGetProperty("description", out var descProp))
            project.Description = descProp.GetString() ?? string.Empty;

        // ⬇️ korrektes Mapping auf aktuelles Domain-Modell
        if (root.TryGetProperty("deviceWidth", out var widthProp))
            project.DeviceWidth = widthProp.GetSingle();

        if (root.TryGetProperty("deviceHeight", out var heightProp))
            project.DeviceHeight = heightProp.GetSingle();

        if (root.TryGetProperty("showAlignmentGuidelines", out var showAlignmentGuidelines))
            project.ShowAlignmentGuidelines = showAlignmentGuidelines.GetBoolean();

        if (root.TryGetProperty("isShared", out var isShared))
            project.IsShared = isShared.GetBoolean();

        if (root.TryGetProperty("isSharedReadonly", out var isSharedReadonly))
            project.IsSharedReadonly = isSharedReadonly.GetBoolean();

        if (root.TryGetProperty("colorSchemeKey", out var csProp))
            project.ColorSchemaKey = csProp.GetString() ?? "Default";

        if (root.TryGetProperty("projectZoomPercent", out var projectZoomPercent))
            project.ProjectZoomPercent = projectZoomPercent.GetDouble();

        if (root.TryGetProperty("screenZoomPercent", out var screenZoomPercent))
            project.ScreenZoomPercent = screenZoomPercent.GetDouble();

        if (root.TryGetProperty("templateZoomPercent", out var templateZoomPercent))
            project.TemplateZoomPercent = templateZoomPercent.GetDouble();

        if (root.TryGetProperty("popupZoomPercent", out var popupZoomPercent))
            project.PopupZoomPercent = popupZoomPercent.GetDouble();

        if (root.TryGetProperty("previewZoomPercent", out var previewZoomPercent))
            project.PreviewZoomPercent = previewZoomPercent.GetDouble();

        if (root.TryGetProperty("lastOpenedUtc", out var lastOpened))
            project.LastOpenedUtc = lastOpened.GetDateTime();

        // ============================================================
        // SCREENS (Templates bewusst ignorieren)
        // ============================================================

        if (root.TryGetProperty("screens", out var screensProp) &&
            screensProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var s in screensProp.EnumerateArray())
            {
                var screen = s.Deserialize<Screen>(options);
                if (screen == null)
                    continue;

                screen.Project = project;
                project.Screens.Add(screen);
            }
        }

        // ============================================================
        // POPUPS
        // ============================================================

        if (root.TryGetProperty("popups", out var popupsProp) &&
            popupsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var p in popupsProp.EnumerateArray())
            {
                var popup = p.Deserialize<ScreenPopup>(options);
                if (popup != null)
                    project.Popups.Add(popup);
            }
        }

        return project;
    }

    private static bool LooksLikeTemplate(JsonElement el)
    {
        if (el.TryGetProperty("type", out var typeProp) &&
            typeProp.GetString() == "Template")
            return true;

        if (el.TryGetProperty("showHeader", out var sh) &&
            el.TryGetProperty("showFooter", out var sf) &&
            !sh.GetBoolean() && !sf.GetBoolean())
            return true;

        return false;
    }

    public override void Write(Utf8JsonWriter writer, Project value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteNumber("id", value.Id);
        writer.WriteString("name", value.Name);
        writer.WriteString("description", value.Description);

        // ⬇️ korrektes Mapping zurück ins JSON
        writer.WriteNumber("deviceWidth", value.DeviceWidth);
        writer.WriteNumber("deviceHeight", value.DeviceHeight);
        writer.WriteBoolean("showAlignmentGuidelines", value.ShowAlignmentGuidelines);

        writer.WriteBoolean("isShared", value.IsShared);
        writer.WriteBoolean("isSharedReadonly", value.IsSharedReadonly);
        writer.WriteString("colorSchemeKey", value.ColorSchemaKey);

        writer.WriteNumber("projectZoomPercent", value.ProjectZoomPercent);
        writer.WriteNumber("screenZoomPercent", value.ScreenZoomPercent);
        writer.WriteNumber("templateZoomPercent", value.TemplateZoomPercent);
        writer.WriteNumber("popupZoomPercent", value.PopupZoomPercent);
        writer.WriteNumber("previewZoomPercent", value.PreviewZoomPercent);

        writer.WriteString("lastOpenedUtc", value.LastOpenedUtc);

        writer.WritePropertyName("screens");
        writer.WriteStartArray();
        foreach (var screen in value.Screens)
            JsonSerializer.Serialize(writer, screen, options);
        writer.WriteEndArray();

        writer.WritePropertyName("popups");
        writer.WriteStartArray();
        foreach (var popup in value.Popups)
            JsonSerializer.Serialize(writer, popup, options);
        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}
