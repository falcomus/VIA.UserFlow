// ============================================================================
// FILE: Mockup.AssetSystem/AssetCatalog.cs
// PURPOSE:
//   Zentraler, statischer Hybrid-Katalog für Embedded + Custom Assets (SVG/PNG).
//   - Sucht Embedded-Resources: "Mockup.Assets.SVG.*" / "Mockup.Assets.PNG.*"
//   - Sucht Filesystem: "<Base>\Assets\Custom\SVG\**\*.svg", "<Base>\Assets\Custom\PNG\**\*.png"
//   - Vergibt eindeutige Asset-Ids (Dateiname ohne Extension; Custom > Embedded)
//   - Lazy Rendering + Thread-sicheres Caching von ImageSource-Previews
//   - Hilfs-API für XAML (MarkupExtension) und ViewModels (Converter)
//   - Kompatibilität: Type-Extensions, damit alter Code wie AppServices.Assets.ImportFile(...) weiterläuft.
//
// AUTHOR: ChatGPT (XMOCKUP2 / MO27)
// VERSION: 1.1
// ============================================================================
using SkiaSharp;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Mockup.AssetSystem;

public static class AssetCatalog
{
    // ---------- Konfiguration ----------
    public const string EmbeddedSvgPrefix = "Mockup.AssetSystem.Resources.SVG.";
    public const string EmbeddedPngPrefix = "Mockup.AssetSystem.Resources.Image.";

    public static readonly string CustomRoot = Path.Combine(AppContext.BaseDirectory, "Assets");
    public static readonly string CustomSvgRoot = Path.Combine(CustomRoot, "SVG");
    public static readonly string CustomPngRoot = Path.Combine(CustomRoot, "PNG");

    // Standard-Previewgröße
    public const int DefaultPreviewSize = 24;

    // ---------- Modelle ----------
    public enum AssetKind { Svg, Png }

    public sealed class AssetInfo
    {
        public required string Id { get; init; }                 // z.B. "icon_home"
        public required AssetKind Kind { get; init; }            // Svg | Png
        public required bool IsEmbedded { get; init; }           // true = Resource, false = File
        public Assembly? ResourceAssembly { get; init; }         // bei Embedded
        public string? ResourceName { get; init; }               // bei Embedded
        public string? FilePath { get; init; }                   // bei Filesystem
        public override string ToString() => $"{Id} ({Kind}) {(IsEmbedded ? "[embedded]" : FilePath)}";
    }

    // ---------- Speicher ----------
    private static readonly object _scanLock = new();
    private static volatile bool _scanned;

    // Katalog: Id -> AssetInfo  (Custom überschreibt Embedded)
    private static readonly ConcurrentDictionary<string, AssetInfo> _assets = new(StringComparer.OrdinalIgnoreCase);

    // Sortierter AllAssets-Cache. Wird bei jeder Katalogänderung invalidiert.
    private static AssetInfo[]? _allAssetsCache;

    // Preview-Cache: Key = $"{id}|{size}|{tintARGB}"
    private static readonly ConcurrentDictionary<string, Lazy<ImageSource?>> _previewCache = new();

    // ---------- Public API ----------

    /// <summary>Erzwingt (Neu-)Scan von Embedded & Custom und baut den Katalog auf.</summary>
    public static void EnsureScanned()
    {
        if (_scanned) return;
        lock (_scanLock)
        {
            if (_scanned) return;
            _assets.Clear();
            InvalidateAllAssetsCache();

            ScanEmbedded();
            ScanCustom();

            _scanned = true;
        }
    }

    /// <summary>Alle Assets (kombiniert, Custom > Embedded). Aufruf erzeugt Scan bei Bedarf.</summary>
    public static IReadOnlyCollection<AssetInfo> AllAssets
    {
        get
        {
            EnsureScanned();

            lock (_scanLock)
            {
                return _allAssetsCache ??= _assets.Values
                    .OrderBy(a => a.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }
    }

    /// <summary>Gibt AssetInfo für eine Id zurück oder null.</summary>
    public static AssetInfo? TryGet(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        EnsureScanned();
        _assets.TryGetValue(id, out var info);
        return info;
    }

    /// <summary>
    /// Rendert ein WPF-ImageSource Preview (lazy + cached).
    /// SVGs unterstützen Tint; PNGs werden transparent und ungetönt angezeigt.
    /// </summary>
    public static ImageSource? GetPreview(string id, Color? tint = null, int size = DefaultPreviewSize)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var info = TryGet(id);
        if (info is null) return null;

        var tintArgb = tint.HasValue ? (uint)((tint.Value.A << 24) | (tint.Value.R << 16) | (tint.Value.G << 8) | tint.Value.B) : 0u;
        var key = $"{info.Id}|{size}|{tintArgb}";

        var lazy = _previewCache.GetOrAdd(key, _ => new Lazy<ImageSource?>(() =>
        {
            using var bmp = ImageRenderer.RenderPreview(info, ToSkColorOrNull(tint), size);
            return bmp is null ? null : SkBitmapToImageSource(bmp);
        }, isThreadSafe: true));

        return lazy.Value;
    }

    /// <summary>Leert den Preview-Cache (z.B. nach Asset-Änderungen).</summary>
    public static void ClearPreviewCache()
    {
        _previewCache.Clear();
        ImageRenderer.ClearCache();
    }

    /// <summary>Erzwingt Re-Scan + Cache-Clear (für Dev/Hot-Reload-Szenarien).</summary>
    public static void Refresh()
    {
        lock (_scanLock)
        {
            _scanned = false;
            ClearPreviewCache();
        }
        EnsureScanned();
    }

    /// <summary>
    /// Importiert eine externe Datei (SVG/PNG) in den Custom-Ordner und aktualisiert den Katalog.
    /// Gibt die zugehörige AssetInfo zurück.
    /// </summary>
    public static AssetInfo? ImportFile(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            return null;

        var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
        var id = Path.GetFileNameWithoutExtension(sourcePath);

        Directory.CreateDirectory(CustomSvgRoot);
        Directory.CreateDirectory(CustomPngRoot);

        string target;
        AssetKind kind;
        if (ext == ".svg")
        {
            target = Path.Combine(CustomSvgRoot, Path.GetFileName(sourcePath));
            kind = AssetKind.Svg;
        }
        else if (ext == ".png")
        {
            target = Path.Combine(CustomPngRoot, Path.GetFileName(sourcePath));
            kind = AssetKind.Png;
        }
        else
        {
            return null;
        }

        File.Copy(sourcePath, target, overwrite: true);

        // Katalog aktualisieren (Custom > Embedded)
        var info = new AssetInfo
        {
            Id = id,
            Kind = kind,
            IsEmbedded = false,
            FilePath = target
        };

        Put(overrideIfExists: true, info);
        ClearPreviewCache();

        return info;
    }

    // ---------- Private: Scans ----------

    //private static void ScanEmbedded()
    //{
    //    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
    //    foreach (var asm in assemblies)
    //    {
    //        string[]? names;
    //        try { names = asm.GetManifestResourceNames(); }
    //        catch { continue; }
    //        if (names is null || names.Length == 0) continue;

    //        foreach (var res in names)
    //        {
    //            if (res.StartsWith(EmbeddedSvgPrefix, StringComparison.Ordinal))
    //            {
    //                var id = Path.GetFileNameWithoutExtension(res.AsSpan(EmbeddedSvgPrefix.Length).ToString());
    //                Put(overrideIfExists: false, new AssetInfo
    //                {
    //                    Id = id,
    //                    Kind = AssetKind.Svg,
    //                    IsEmbedded = true,
    //                    ResourceAssembly = asm,
    //                    ResourceName = res
    //                });
    //            }
    //            else if (res.StartsWith(EmbeddedPngPrefix, StringComparison.Ordinal))
    //            {
    //                var id = Path.GetFileNameWithoutExtension(res.AsSpan(EmbeddedPngPrefix.Length).ToString());
    //                Put(overrideIfExists: false, new AssetInfo
    //                {
    //                    Id = id,
    //                    Kind = AssetKind.Png,
    //                    IsEmbedded = true,
    //                    ResourceAssembly = asm,
    //                    ResourceName = res
    //                });
    //            }
    //        }
    //    }
    //}

    // Sammelt alle Embedded-Resources mit .svg / .png – unabhängig vom Prefix/Namensraum
    private static void ScanEmbedded()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var asm in assemblies)
        {
            string[]? names;
            try { names = asm.GetManifestResourceNames(); }
            catch { continue; }
            if (names is null || names.Length == 0) continue;

            foreach (var res in names)
            {
                // Wir wollen nur echte Dateien, keine .resources oder sat. Assemblies
                if (res.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                {
                    var id = GetIdFromResourceName(res); // z.B. ...BackButton.svg → BackButton
                    if (string.IsNullOrWhiteSpace(id)) continue;

                    Put(overrideIfExists: false, new AssetInfo
                    {
                        Id = id,
                        Kind = AssetKind.Svg,
                        IsEmbedded = true,
                        ResourceAssembly = asm,
                        ResourceName = res
                    });
                }
                else if (res.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    var id = GetIdFromResourceName(res);
                    if (string.IsNullOrWhiteSpace(id)) continue;

                    Put(overrideIfExists: false, new AssetInfo
                    {
                        Id = id,
                        Kind = AssetKind.Png,
                        IsEmbedded = true,
                        ResourceAssembly = asm,
                        ResourceName = res
                    });
                }
            }
        }
    }

    // Liefert den Dateinamen ohne Extension aus einem Manifest-Resourcename.
    // Beispiel: "Mockup.Resources.Image.BackButton.png" → "BackButton"
    private static string GetIdFromResourceName(string manifestName)
    {
        if (string.IsNullOrWhiteSpace(manifestName))
            return string.Empty;

        // Letztes Segment nach Punkten holen: ... .BackButton.png → BackButton.png
        var lastDot = manifestName.LastIndexOf('.');
        if (lastDot <= 0) return Path.GetFileNameWithoutExtension(manifestName);

        // Suche das Segment vor der Extension: ... .BackButton.png → BackButton
        // Dazu das vorletzte '.' finden:
        var beforeExt = manifestName.LastIndexOf('.', lastDot - 1);
        if (beforeExt < 0) return Path.GetFileNameWithoutExtension(manifestName);

        var fileWithExt = manifestName.Substring(beforeExt + 1);
        return Path.GetFileNameWithoutExtension(fileWithExt);
    }


    private static void ScanCustom()
    {
        try
        {
            if (Directory.Exists(CustomSvgRoot))
            {
                foreach (var file in Directory.EnumerateFiles(CustomSvgRoot, "*.svg", SearchOption.AllDirectories))
                {
                    var id = Path.GetFileNameWithoutExtension(file);
                    Put(overrideIfExists: true, new AssetInfo
                    {
                        Id = id,
                        Kind = AssetKind.Svg,
                        IsEmbedded = false,
                        FilePath = file
                    });
                }
            }

            if (Directory.Exists(CustomPngRoot))
            {
                foreach (var file in Directory.EnumerateFiles(CustomPngRoot, "*.png", SearchOption.AllDirectories))
                {
                    var id = Path.GetFileNameWithoutExtension(file);
                    Put(overrideIfExists: true, new AssetInfo
                    {
                        Id = id,
                        Kind = AssetKind.Png,
                        IsEmbedded = false,
                        FilePath = file
                    });
                }
            }
        }
        catch
        {
            // bewusst still — in Tool-Szenarien sollen fehlende Ordner nicht crashen
        }
    }

    private static void Put(bool overrideIfExists, AssetInfo info)
    {
        _assets.AddOrUpdate(info.Id,
            addValueFactory: _ => info,
            updateValueFactory: (_, existing) => overrideIfExists ? info : existing);

        InvalidateAllAssetsCache();
    }

    private static void InvalidateAllAssetsCache()
    {
        _allAssetsCache = null;
    }

    // ---------- Utils ----------

    private static SKColor? ToSkColorOrNull(Color? c)
        => c.HasValue ? new SKColor(c.Value.R, c.Value.G, c.Value.B, c.Value.A) : null;

    private static ImageSource SkBitmapToImageSource(SKBitmap bmp)
    {
        var info = bmp.Info;
        var bs = BitmapSource.Create(
            info.Width, info.Height, 96, 96, PixelFormats.Pbgra32, null,
            bmp.GetPixels(), info.RowBytes * info.Height, info.RowBytes);
        bs.Freeze();
        return bs;
    }
}



// ============================================================================
// SHIM: Type-Erweiterungen für rückwärtskompatible Aufrufe wie
// AppServices.Assets.ImportFile(...), AppServices.Assets.GetOrLoad(...)
// ============================================================================
public static class AssetCatalogTypeExtensions
{
    /// <summary>
    /// Gibt ein WPF-ImageSource für das angegebene Asset zurück
    /// (kompatibel zu altem Aufrufschema).
    /// </summary>
    public static ImageSource? GetOrLoad(this Type _, string id, ImageFormat format)
    {
        AssetCatalog.EnsureScanned();
        var info = AssetCatalog.TryGet(id);
        if (info is null) return null;

        var tint = (Color?)null;
        return AssetCatalog.GetPreview(id, tint, AssetCatalog.DefaultPreviewSize);
    }

    /// <summary>
    /// Importiert eine externe Datei in den Custom-Ordner
    /// und gibt die zugehörige Asset-Id zurück (für Legacy-Kompatibilität).
    /// </summary>
    public static string? ImportFile(this Type _, string sourcePath)
    {
        var info = AssetCatalog.ImportFile(sourcePath);
        return info?.Id;
    }

    /// <summary>
    /// Direkter Zugriff auf vollständige AssetInfo (neuere Nutzung).
    /// </summary>
    public static AssetCatalog.AssetInfo? ImportFileFull(this Type _, string sourcePath)
        => AssetCatalog.ImportFile(sourcePath);

    /// <summary>Leert den Preview-Cache.</summary>
    public static void ClearCache(this Type _)
        => AssetCatalog.ClearPreviewCache();

    /// <summary>Gibt alle bekannten Assets zurück.</summary>
    public static IEnumerable<AssetCatalog.AssetInfo> All(this Type _)
        => AssetCatalog.AllAssets;
}
