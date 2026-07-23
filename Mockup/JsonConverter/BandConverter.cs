// ============================================================================
// FILE: Mockup.JsonConverters/BandConverter.cs
// NEW MODEL: persists saved expanded height for fixed-height restore
// Persists: width, title, bandType, isExpandable, isExpanded, savedExpandedHeight, activePageIndex, pageIndicatorStyle, pages
// ============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace Mockup.JsonConverters;

public sealed class BandConverter : JsonConverter<Band>
{
    public override Band Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        float width = root.TryGetProperty("width", out var w) ? w.GetSingle() : 0f;

        int activeIndex = root.TryGetProperty("activePageIndex", out var api) ? api.GetInt32() : 0;
        float savedExpandedHeight = root.TryGetProperty("savedExpandedHeight", out var seh)
            ? seh.GetSingle()
            : 0f;

        var band = new Band
        {
            IsSticky = root.TryGetProperty("isSticky", out var sticky) && sticky.GetBoolean(),
            Width = width,
            Name = root.TryGetProperty("name", out var n) ? (n.GetString() ?? "") : "",
            Title = root.TryGetProperty("title", out var t) ? (t.GetString() ?? "") : "",
            BandType = root.TryGetProperty("bandType", out var bt)
                ? (BandType)bt.GetInt32()
                : BandType.Custom,
            IsExpandable = root.TryGetProperty("isExpandable", out var ie) && ie.GetBoolean(),
            IsExpanded = root.TryGetProperty("isExpanded", out var ix) && ix.GetBoolean(),
            UniformPageHeight =
                root.TryGetProperty("uniformPageHeight", out var uph) && uph.GetBoolean(),
            ShowTabs = root.TryGetProperty("showTabs", out var st) && st.GetBoolean(),
            X = 0,
            Y = 0,
        };

        if (root.TryGetProperty("bandBackground", out var bandBGProp))
        {
            var s = bandBGProp.GetString();
            if (!string.IsNullOrWhiteSpace(s))
            {
                try
                {
                    band.BandBackground = (Color)ColorConverter.ConvertFromString(s)!;
                }
                catch
                {
                    // ignore invalid color
                }
            }
        }

        if (root.TryGetProperty("headerBackground", out var headerBGProp))
        {
            var s = headerBGProp.GetString();
            if (!string.IsNullOrWhiteSpace(s))
            {
                try
                {
                    band.HeaderBackground = (Color)ColorConverter.ConvertFromString(s)!;
                }
                catch
                {
                    // ignore invalid color
                }
            }
        }

        if (root.TryGetProperty("footerBackground", out var footerBGProp))
        {
            var s = footerBGProp.GetString();
            if (!string.IsNullOrWhiteSpace(s))
            {
                try
                {
                    band.FooterBackground = (Color)ColorConverter.ConvertFromString(s)!;
                }
                catch
                {
                    // ignore invalid color
                }
            }
        }

        // If not expandable, expanded must be false.
        if (!band.IsExpandable)
            band.IsExpanded = false;

        band.Pages.Clear();

        if (
            root.TryGetProperty("pages", out var pagesProp)
            && pagesProp.ValueKind == JsonValueKind.Array
        )
        {
            foreach (var pageElement in pagesProp.EnumerateArray())
            {
                var page = JsonSerializer.Deserialize<BandPage>(pageElement.GetRawText(), options);
                if (page == null)
                    continue;

                page.ParentBand = band;

                foreach (var ctrl in page.Controls)
                {
                    ctrl.ParentBand = band;
                    ctrl.ParentBandPage = page;
                }

                page.InvalidateMinHeight();
                page.EnsureMinHeight();

                band.Pages.Add(page);
            }
        }

        band.EnsureInitialPage();

        band.ActivePageIndex =
            (band.Pages.Count == 0) ? 0 : Math.Clamp(activeIndex, 0, band.Pages.Count - 1);

        band.ActivePage?.EnsureMinHeight();

        if (savedExpandedHeight > 0)
            band.SavedExpandedHeight = savedExpandedHeight;

        return band;
    }

    public override void Write(Utf8JsonWriter writer, Band band, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteBoolean("isSticky", band.IsSticky);
        writer.WriteNumber("width", band.Width);
        writer.WriteString("bandBackground", band.BandBackground.ToString());
        writer.WriteString("headerBackground", band.HeaderBackground.ToString());
        writer.WriteString("footerBackground", band.FooterBackground.ToString());
        writer.WriteString("name", band.Name);
        writer.WriteString("title", band.Title);
        writer.WriteNumber("bandType", (int)band.BandType);

        writer.WriteBoolean("isExpandable", band.IsExpandable);
        writer.WriteBoolean("isExpanded", band.IsExpanded);
        writer.WriteNumber("savedExpandedHeight", band.SavedExpandedHeight);

        writer.WriteNumber("activePageIndex", band.ActivePageIndex);
        writer.WriteBoolean("showTabs", band.ShowTabs);
        writer.WriteBoolean("uniformPageHeight", band.UniformPageHeight);

        writer.WritePropertyName("pages");
        writer.WriteStartArray();
        foreach (var page in band.Pages)
            JsonSerializer.Serialize(writer, page, options);
        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}
