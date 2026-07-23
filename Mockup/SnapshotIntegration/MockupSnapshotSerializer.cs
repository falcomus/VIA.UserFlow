// ======================================================================================
// FILE: Mockup/Services/MockupSnapshotSerializer.cs
//
// ZWECK:
//   Implementierung von ISnapshotSerializer für die Mockup-Library.
//   Kennt Project, Screen, Template-Collection, ScreenTemplate, ScreenPopup und die JsonOptions.
//   Wird einmalig beim App-Start bei SnapshotManager.Initialize() registriert.
//
// WICHTIG NACH DEM RESTORE:
//   Screen.Reconstruct(project) muss nach der Deserialisierung aufgerufen werden,
//   damit ParentBand, ParentBandPage und Project-Referenzen korrekt gesetzt sind.
//   Dies geschieht im MockupViewModel (MockupViewModel.Snapshots.cs), NICHT hier.
// ======================================================================================

using Mockup.JsonConverters;
using Mockup.Snapshots;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Mockup.Services;

/// <summary>
/// Serialisiert und deserialisiert Project, Screen, Template-Collection, ScreenTemplate und ScreenPopup
/// für den SnapshotManager — unter Verwendung der bestehenden JsonOptions.
/// </summary>
public sealed class MockupSnapshotSerializer : ISnapshotSerializer
{
    // ─────────────────────────────────────────────────────────────
    //  JsonOptions (kompakt für Snapshots)
    // ─────────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions _projectOptions = CreateProjectOptions();
    private static readonly JsonSerializerOptions _screenAndPopupOptions = CreateScreenAndPopupOptions();
    private static readonly JsonSerializerOptions _templateOptions = CreateTemplateOptions();

    // BackgroundImageBase64 wird für Snapshots ausgelagert:
    // Projektdateien bleiben unverändert, aber Undo/Redo-JSON hält nur Tokens.
    private const string BackgroundImageTokenPrefix = "__VIA_SNAPSHOT_BACKGROUND_IMAGE__:";
    private static readonly ConcurrentDictionary<string, string> _backgroundImagePayloadCache = new(StringComparer.Ordinal);

    private static readonly Regex _backgroundImageBase64Regex = new(
        "\"backgroundImageBase64\"\\s*:\\s*\"(?<value>[^\"]*)\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static JsonSerializerOptions CreateProjectOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true,
        };

        options.Converters.Add(new ProjectConverter());
        options.Converters.Add(new ScreenConverter());
        options.Converters.Add(new TemplateConverter());
        options.Converters.Add(new BandConverter());
        options.Converters.Add(new BandPageConverter());
        options.Converters.Add(new DesignControlConverter());
        options.Converters.Add(new ColorJsonConverter());

        return options;
    }

    private static JsonSerializerOptions CreateScreenAndPopupOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true,
        };

        options.Converters.Add(new ScreenConverter());
        options.Converters.Add(new BandConverter());
        options.Converters.Add(new BandPageConverter());
        options.Converters.Add(new DesignControlConverter());
        options.Converters.Add(new ColorJsonConverter());

        return options;
    }

    private static JsonSerializerOptions CreateTemplateOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true,
        };

        options.Converters.Add(new TemplateConverter());
        options.Converters.Add(new BandConverter());
        options.Converters.Add(new BandPageConverter());
        options.Converters.Add(new DesignControlConverter());
        options.Converters.Add(new ColorJsonConverter());

        return options;
    }

    // ─────────────────────────────────────────────────────────────
    //  ISnapshotSerializer
    // ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public string? Serialize(object target, SnapshotContext context)
    {
        try
        {
            var json = context switch
            {
                SnapshotContext.Project => JsonSerializer.Serialize((Project)target, _projectOptions),
                SnapshotContext.Screen => JsonSerializer.Serialize((Screen)target, _screenAndPopupOptions),
                SnapshotContext.Templates => SerializeTemplates(target),
                SnapshotContext.Template => JsonSerializer.Serialize((ScreenTemplate)target, _templateOptions),
                SnapshotContext.Popup => JsonSerializer.Serialize((ScreenPopup)target, _screenAndPopupOptions),
                _ => null,
            };

            return string.IsNullOrEmpty(json)
                ? json
                : CompactBackgroundImagePayloads(json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MockupSnapshotSerializer] Serialize failed ({context}): {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc/>
    public object? Deserialize(string json, SnapshotContext context)
    {
        try
        {
            var expandedJson = ExpandBackgroundImagePayloads(json);

            return context switch
            {
                SnapshotContext.Project => JsonSerializer.Deserialize<Project>(expandedJson, _projectOptions),
                SnapshotContext.Screen => JsonSerializer.Deserialize<Screen>(expandedJson, _screenAndPopupOptions),
                SnapshotContext.Templates => DeserializeTemplates(expandedJson),
                SnapshotContext.Template => JsonSerializer.Deserialize<ScreenTemplate>(expandedJson, _templateOptions),
                SnapshotContext.Popup => JsonSerializer.Deserialize<ScreenPopup>(expandedJson, _screenAndPopupOptions),
                _ => null,
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MockupSnapshotSerializer] Deserialize failed ({context}): {ex.Message}");
            return null;
        }
    }

    private static string? SerializeTemplates(object target)
    {
        if (target is IEnumerable<ScreenTemplate> templates)
            return JsonSerializer.Serialize(templates.ToArray(), _templateOptions);

        return null;
    }

    private static ObservableCollection<ScreenTemplate> DeserializeTemplates(string json)
    {
        var templates = JsonSerializer.Deserialize<ScreenTemplate[]>(json, _templateOptions)
                        ?? Array.Empty<ScreenTemplate>();

        return new ObservableCollection<ScreenTemplate>(templates);
    }

    private static string CompactBackgroundImagePayloads(string json)
    {
        if (string.IsNullOrEmpty(json) || json.IndexOf("backgroundImageBase64", StringComparison.Ordinal) < 0)
            return json;

        return _backgroundImageBase64Regex.Replace(json, match =>
        {
            var payload = match.Groups["value"].Value;

            if (string.IsNullOrEmpty(payload))
                return match.Value;

            if (payload.StartsWith(BackgroundImageTokenPrefix, StringComparison.Ordinal))
                return match.Value;

            var token = CreateBackgroundImageToken(payload);
            _backgroundImagePayloadCache.TryAdd(token, payload);

            return $"\"backgroundImageBase64\":\"{token}\"";
        });
    }

    private static string ExpandBackgroundImagePayloads(string json)
    {
        if (string.IsNullOrEmpty(json) || json.IndexOf(BackgroundImageTokenPrefix, StringComparison.Ordinal) < 0)
            return json;

        return _backgroundImageBase64Regex.Replace(json, match =>
        {
            var value = match.Groups["value"].Value;

            if (!value.StartsWith(BackgroundImageTokenPrefix, StringComparison.Ordinal))
                return match.Value;

            if (!_backgroundImagePayloadCache.TryGetValue(value, out var payload))
                return match.Value;

            return $"\"backgroundImageBase64\":\"{payload}\"";
        });
    }

    private static string CreateBackgroundImageToken(string payload)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        var hash = SHA256.HashData(bytes);

        return $"{BackgroundImageTokenPrefix}{Convert.ToHexString(hash)}:{bytes.Length}";
    }
}
