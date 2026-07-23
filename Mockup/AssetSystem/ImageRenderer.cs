// ============================================================================
// FILE: Mockup.AssetSystem/ImageRenderer.cs
// PURPOSE:
//   Low-level Rendering für SVG & PNG zu SKBitmap, inklusive optionalem Tint
//   (SVG; PNG bleibt unverändert). Skaliert proportional auf Zielgröße.
//   Zusätzlich: Wrapper für Draw(...) und CreateCubicSampling(), damit
//   bestehende Renderer (ImageButton/OutlineButton) weiter kompilieren.
//
// REQUIREMENTS:
//   - SkiaSharp
//   - Svg.Skia (SKSvg)
// AUTHOR: ChatGPT (XMOCKUP2 / MO27)
// VERSION: 1.2
// ============================================================================
using SkiaSharp;
using Svg.Skia;
using System.Collections.Concurrent;
using System.IO;


namespace Mockup.AssetSystem;

public static class ImageRenderer
{
    private const int MaxPngCacheEntries = 64;

    private static readonly Lock _pngEvictionLock = new();
    private static readonly ConcurrentDictionary<string, Lazy<SKPicture?>> _svgCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, Lazy<SKBitmap?>> _pngCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentQueue<string> _pngCacheOrder = new();

    // -------------------------- PUBLIC: PREVIEW --------------------------

    /// <summary>Rendert ein Preview zu einer SKBitmap. Caller ist Eigentümer der Bitmap.</summary>
    public static SKBitmap? RenderPreview(AssetCatalog.AssetInfo info, SKColor? tint, int targetSize)
    {
        try
        {
            return info.Kind == AssetCatalog.AssetKind.Svg
                ? RenderSvgToBitmap(info, tint, targetSize, targetSize)
                : RenderPngToBitmap(info, targetSize, targetSize);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Leert die nativen SVG/PNG-Render-Caches und gibt Skia-Ressourcen frei.</summary>
    public static void ClearCache()
    {
        ClearSvgCache();
        ClearPngCache();
    }

    // -------------------------- PUBLIC: WRAPPERS (Kompatibilität) --------------------------

    /// <summary>Kompatibler Cubic-Sampling-Wrapper für ältere Renderer.</summary>
    public static SKSamplingOptions CreateCubicSampling()
        => SKSamplingOptions.Default;

    /// <summary>Zeichnet Asset in Zielrechteck. Überladung für AssetId.</summary>
    public static void Draw(SKCanvas canvas, string assetId, SKRect dest, SKColor? tint = null, SKSamplingOptions? sampling = null)
    {
        var info = AssetCatalog.TryGet(assetId);
        if (info is null) return;
        Draw(canvas, info, dest, tint, sampling);
    }

    /// <summary>Zeichnet Asset in Zielrechteck. Überladung für XYWH.</summary>
    public static void Draw(SKCanvas canvas, string assetId, float x, float y, float w, float h, SKColor? tint = null, SKSamplingOptions? sampling = null)
        => Draw(canvas, assetId, new SKRect(x, y, x + w, y + h), tint, sampling);

    /// <summary>Zeichnet AssetInfo in Zielrechteck.</summary>
    public static void Draw(SKCanvas canvas, AssetCatalog.AssetInfo info, SKRect dest, SKColor? tint = null, SKSamplingOptions? sampling = null)
    {
        if (dest.Width <= 0 || dest.Height <= 0)
            return;

        if (info.Kind == AssetCatalog.AssetKind.Svg)
        {
            var pic = GetSvgPicture(info);
            if (pic is null) return;

            // Fit: skaliere Picture in dest (AspectFit)
            var bounds = pic.CullRect;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            var scale = ComputeFitScale(bounds.Width, bounds.Height, dest.Width, dest.Height);
            var tx = dest.MidX - (bounds.Width * scale) / 2f;
            var ty = dest.MidY - (bounds.Height * scale) / 2f;

            using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
            if (tint.HasValue)
                paint.ColorFilter = SKColorFilter.CreateBlendMode(tint.Value, SKBlendMode.SrcIn);

            canvas.Save();
            canvas.Translate(tx, ty);
            canvas.Scale(scale, scale);
            canvas.DrawPicture(pic, paint);
            canvas.Restore();
        }
        else
        {
            var bmp = GetPngBitmap(info);
            if (bmp is null) return;

            var src = new SKRect(0, 0, bmp.Width, bmp.Height);
            using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
#if HAS_SAMPLINGOPTIONS
            if (sampling.HasValue)
                paint.Sampler = sampling.Value; // nur falls API-Version verfügbar
#endif
            canvas.DrawBitmap(bmp, src, dest, paint);
        }
    }

    // -------------------------- PRIVATE: CACHE --------------------------

    private static SKPicture? GetSvgPicture(AssetCatalog.AssetInfo info)
    {
        var key = GetCacheKey(info);
        var lazy = _svgCache.GetOrAdd(key, _ => new Lazy<SKPicture?>(() => LoadSvgPicture(info), isThreadSafe: true));
        return lazy.Value;
    }

    private static SKBitmap? GetPngBitmap(AssetCatalog.AssetInfo info)
    {
        var key = GetCacheKey(info);
        var createdLazy = new Lazy<SKBitmap?>(() => LoadPngBitmap(info), isThreadSafe: true);
        var lazy = _pngCache.GetOrAdd(key, createdLazy);

        if (ReferenceEquals(lazy, createdLazy))
        {
            _pngCacheOrder.Enqueue(key);
            TrimPngCacheIfNeeded();
        }

        return lazy.Value;
    }

    private static SKPicture? LoadSvgPicture(AssetCatalog.AssetInfo info)
    {
        using var stream = OpenStream(info);
        if (stream is null) return null;

        var svg = new SKSvg();
        svg.Load(stream);
        return svg.Picture;
    }

    private static SKBitmap? LoadPngBitmap(AssetCatalog.AssetInfo info)
    {
        using var stream = OpenStream(info);
        if (stream is null) return null;

        var bmp = SKBitmap.Decode(stream);
        return bmp;
    }

    private static void ClearSvgCache()
    {
        foreach (var item in _svgCache.Values)
        {
            if (!item.IsValueCreated)
                continue;

            item.Value?.Dispose();
        }

        _svgCache.Clear();
    }

    private static void ClearPngCache()
    {
        foreach (var item in _pngCache.Values)
        {
            if (!item.IsValueCreated)
                continue;

            item.Value?.Dispose();
        }

        _pngCache.Clear();

        while (_pngCacheOrder.TryDequeue(out _))
        {
        }
    }

    private static void TrimPngCacheIfNeeded()
    {
        if (_pngCache.Count <= MaxPngCacheEntries)
            return;

        lock (_pngEvictionLock)
        {
            while (_pngCache.Count > MaxPngCacheEntries && _pngCacheOrder.TryDequeue(out var oldKey))
            {
                if (!_pngCache.TryRemove(oldKey, out var oldLazy))
                    continue;

                if (!oldLazy.IsValueCreated)
                    continue;

                oldLazy.Value?.Dispose();
            }
        }
    }

    private static string GetCacheKey(AssetCatalog.AssetInfo info)
    {
        if (info.IsEmbedded)
            return $"embedded|{info.Kind}|{info.ResourceAssembly?.FullName}|{info.ResourceName}";

        return $"file|{info.Kind}|{info.FilePath}";
    }

    // -------------------------- PRIVATE: RENDER-HELPER --------------------------

    private static SKBitmap? RenderSvgToBitmap(AssetCatalog.AssetInfo info, SKColor? tint, int targetW, int targetH)
    {
        var pic = GetSvgPicture(info);
        if (pic is null) return null;

        var bounds = pic.CullRect;
        var scale = ComputeFitScale(bounds.Width, bounds.Height, targetW, targetH);

        var w = (int)Math.Ceiling(bounds.Width * scale);
        var h = (int)Math.Ceiling(bounds.Height * scale);
        w = Math.Max(1, w); h = Math.Max(1, h);

        var bmp = new SKBitmap(w, h, true);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.Transparent);

        using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
        if (tint.HasValue)
            paint.ColorFilter = SKColorFilter.CreateBlendMode(tint.Value, SKBlendMode.SrcIn);

        canvas.Scale(scale, scale);
        canvas.DrawPicture(pic, paint);
        canvas.Flush();
        return bmp;
    }

    private static SKBitmap? RenderPngToBitmap(AssetCatalog.AssetInfo info, int targetW, int targetH)
    {
        var original = GetPngBitmap(info);
        if (original is null) return null;

        var scale = ComputeFitScale(original.Width, original.Height, targetW, targetH);
        var w = (int)Math.Max(1, Math.Round(original.Width * scale));
        var h = (int)Math.Max(1, Math.Round(original.Height * scale));

        if (w == original.Width && h == original.Height)
            return original.Copy();

        var bmp = new SKBitmap(w, h, true);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.Transparent);
        var src = new SKRect(0, 0, original.Width, original.Height);
        var dst = new SKRect(0, 0, w, h);
        using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
        canvas.DrawBitmap(original, src, dst, paint);
        canvas.Flush();
        return bmp;
    }

    private static Stream? OpenStream(AssetCatalog.AssetInfo info)
    {
        if (info.IsEmbedded)
        {
            if (info.ResourceAssembly is null || string.IsNullOrWhiteSpace(info.ResourceName))
                return null;
            return info.ResourceAssembly.GetManifestResourceStream(info.ResourceName!);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(info.FilePath) || !File.Exists(info.FilePath))
                return null;
            return File.OpenRead(info.FilePath!);
        }
    }

    private static float ComputeFitScale(float w, float h, float targetW, float targetH)
    {
        if (w <= 0 || h <= 0) return 1f;
        var sx = targetW / w;
        var sy = targetH / h;
        var s = Math.Min(sx, sy);
        return (float)Math.Max(0.01, Math.Min(s, 10)); // clamp
    }
}
