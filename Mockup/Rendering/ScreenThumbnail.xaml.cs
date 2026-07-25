using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Mockup.Rendering;

/// <summary>
/// Zeichnet eine skalierte, statische Vorschau eines Screens (ohne Scroll).
/// Nutzt Bands → Pages → Controls (read-only, non-live).
/// Rendering folgt dem gleichen Band-Pass-Konzept wie im Designer:
/// Background (Screen) → Band-Backgrounds → Band-Controls.
/// </summary>
public partial class ScreenThumbnail : UserControl
{
    private const int MaxThumbnailBitmapEdge = 500;
    private const int MaxThumbnailCacheEntries = 30;

    private static readonly ConcurrentDictionary<string, ImageSource> ThumbnailCache = new();
    private static readonly ConcurrentQueue<string> ThumbnailCacheOrder = new();
    private static readonly ConditionalWeakTable<Screen, ScreenCacheState> ScreenCacheStates = new();

    private static readonly object ActiveThumbnailsSync = new();
    private static readonly HashSet<ScreenThumbnail> ActiveThumbnails = [];

    private static long _nextScreenCacheIdentity;

    private bool _renderQueued;

    public ScreenThumbnail()
    {
        InitializeComponent();

        Loaded += ScreenThumbnail_Loaded;
        Unloaded += ScreenThumbnail_Unloaded;
        SizeChanged += ScreenThumbnail_SizeChanged;
    }

    private void ScreenThumbnail_Loaded(object sender, RoutedEventArgs e)
    {
        lock (ActiveThumbnailsSync)
            ActiveThumbnails.Add(this);

        QueueRenderThumbnail();
    }

    private void ScreenThumbnail_Unloaded(object sender, RoutedEventArgs e)
    {
        lock (ActiveThumbnailsSync)
            ActiveThumbnails.Remove(this);
    }

    private void ScreenThumbnail_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        QueueRenderThumbnail();
    }

    // =====================================================================
    // DP: Screen
    // =====================================================================

    public static readonly DependencyProperty ScreenProperty = DependencyProperty.Register(
        nameof(Screen),
        typeof(Screen),
        typeof(ScreenThumbnail),
        new PropertyMetadata(null, OnScreenChanged)
    );

    private static void OnScreenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScreenThumbnail preview)
            return;

        ClearCachedScreen(e.OldValue as Screen);
        ClearCachedScreen(e.NewValue as Screen);
        preview.QueueRenderThumbnail();
    }

    public Screen? Screen
    {
        get => (Screen?)GetValue(ScreenProperty);
        set => SetValue(ScreenProperty, value);
    }

    // =====================================================================
    // THUMBNAIL CACHE / UPDATE
    // =====================================================================

    /// <summary>
    /// Invalidiert nur die aktuell geladenen ScreenThumbnail-Instanzen unterhalb
    /// des angegebenen Visual-Tree-Knotens und rendert diese erneut.
    /// </summary>
    public static void RefreshVisibleThumbnails(DependencyObject root)
    {
        ArgumentNullException.ThrowIfNull(root);

        if (!root.Dispatcher.CheckAccess())
        {
            root.Dispatcher.BeginInvoke(
                new Action(() => RefreshVisibleThumbnails(root)),
                DispatcherPriority.Background);
            return;
        }

        RefreshVisibleThumbnailsCore(root);
    }

    private static void RefreshVisibleThumbnailsCore(DependencyObject root)
    {
        ScreenThumbnail[] activeThumbnails;

        lock (ActiveThumbnailsSync)
            activeThumbnails = [.. ActiveThumbnails];

        var visibleThumbnails = new List<ScreenThumbnail>();
        var screensToInvalidate = new HashSet<Screen>(ReferenceEqualityComparer.Instance);

        foreach (var thumbnail in activeThumbnails)
        {
            if (!thumbnail.IsLoaded || !IsVisualDescendantOf(thumbnail, root))
                continue;

            visibleThumbnails.Add(thumbnail);

            if (thumbnail.Screen != null)
                screensToInvalidate.Add(thumbnail.Screen);
        }

        foreach (var screen in screensToInvalidate)
            ClearCachedScreen(screen);

        foreach (var thumbnail in visibleThumbnails)
            thumbnail.QueueRenderThumbnail();
    }

    private static bool IsVisualDescendantOf(
        DependencyObject descendant,
        DependencyObject ancestor)
    {
        DependencyObject? current = descendant;

        while (current != null)
        {
            if (ReferenceEquals(current, ancestor))
                return true;

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private void QueueRenderThumbnail()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(QueueRenderThumbnail), DispatcherPriority.Background);
            return;
        }

        if (_renderQueued)
            return;

        _renderQueued = true;

        Dispatcher.BeginInvoke(
            new Action(() =>
            {
                _renderQueued = false;
                RenderThumbnailImage();
            }),
            DispatcherPriority.Background);
    }

    private void RenderThumbnailImage()
    {
        var screen = Screen;
        if (screen == null)
        {
            PreviewImage.Source = null;
            return;
        }

        var (pixelW, pixelH) = GetTargetBitmapSize(ActualWidth, ActualHeight);
        if (pixelW <= 1 || pixelH <= 1)
        {
            PreviewImage.Source = null;
            return;
        }

        string cacheKey = CreateCacheKey(screen, pixelW, pixelH);

        if (!ThumbnailCache.TryGetValue(cacheKey, out var image))
        {
            image = CreateThumbnailImage(screen, pixelW, pixelH);

            if (ThumbnailCache.TryAdd(cacheKey, image))
            {
                ThumbnailCacheOrder.Enqueue(cacheKey);
                EvictOldThumbnailCacheEntries();
            }
            else if (ThumbnailCache.TryGetValue(cacheKey, out var existingImage))
            {
                image = existingImage;
            }
        }

        PreviewImage.Source = image;
    }

    private static (int Width, int Height) GetTargetBitmapSize(double actualWidth, double actualHeight)
    {
        if (actualWidth <= 1 || actualHeight <= 1)
            return (0, 0);

        double scale = 1.0d;
        double maxEdge = Math.Max(actualWidth, actualHeight);

        if (maxEdge > MaxThumbnailBitmapEdge)
            scale = MaxThumbnailBitmapEdge / maxEdge;

        int width = Math.Max(1, (int)Math.Round(actualWidth * scale));
        int height = Math.Max(1, (int)Math.Round(actualHeight * scale));

        return (width, height);
    }

    private static string CreateCacheKey(Screen screen, int pixelW, int pixelH)
    {
        ScreenCacheState cacheState = GetScreenCacheState(screen);

        return string.Join(
            "|",
            cacheState.Identity,
            Volatile.Read(ref cacheState.Revision),
            screen.Id,
            pixelW,
            pixelH,
            MathF.Round(screen.Width),
            MathF.Round(screen.ScreenHeight),
            screen.ShowHeader,
            screen.ShowFooter);
    }

    private static ScreenCacheState GetScreenCacheState(Screen screen)
    {
        return ScreenCacheStates.GetValue(
            screen,
            static _ => new ScreenCacheState(
                Interlocked.Increment(ref _nextScreenCacheIdentity)));
    }

    private static void ClearThumbnailCache()
    {
        ThumbnailCache.Clear();

        while (ThumbnailCacheOrder.TryDequeue(out _))
        {
        }
    }

    private static void ClearCachedScreen(Screen? screen)
    {
        if (screen == null)
            return;

        ScreenCacheState cacheState = GetScreenCacheState(screen);
        Interlocked.Increment(ref cacheState.Revision);
    }

    private static void EvictOldThumbnailCacheEntries()
    {
        if (ThumbnailCache.Count <= MaxThumbnailCacheEntries)
            return;

        int targetCount = Math.Max(1, MaxThumbnailCacheEntries / 2);

        while (ThumbnailCache.Count > targetCount
            && ThumbnailCacheOrder.TryDequeue(out var oldKey))
        {
            ThumbnailCache.TryRemove(oldKey, out _);
        }
    }

    private sealed class ScreenCacheState
    {
        public ScreenCacheState(long identity)
        {
            Identity = identity;
        }

        public long Identity { get; }

        public long Revision;
    }

    // =====================================================================
    // RENDERING
    // =====================================================================

    private static ImageSource CreateThumbnailImage(Screen screen, int pixelW, int pixelH)
    {
        using var bitmap = RenderScreenToBitmap(screen, pixelW, pixelH);

        var source = BitmapSource.Create(
            bitmap.Width,
            bitmap.Height,
            96,
            96,
            PixelFormats.Pbgra32,
            null,
            bitmap.GetPixels(),
            bitmap.RowBytes * bitmap.Height,
            bitmap.RowBytes);

        source.Freeze();
        return source;
    }

    private static SKBitmap RenderScreenToBitmap(Screen screen, int pixelW, int pixelH)
    {
        var info = new SKImageInfo(pixelW, pixelH, SKColorType.Bgra8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(info);

        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        float targetW = pixelW;
        float targetH = pixelH;

        float modelW = screen.Width; // DeviceWidth
        float modelH = screen.ScreenHeight; // kompletter Content

        if (modelW <= 1 || modelH <= 1)
            return bitmap;

        // Modus (2): in feste Thumbnail-Fläche einpassen (UserHeight komplett sichtbar)
        float scaleW = targetW / modelW;
        float scaleH = targetH / modelH;
        float scale = Math.Min(scaleW, scaleH);

        if (scale <= 0)
            return bitmap;

        float drawW = modelW * scale;
        float drawH = modelH * scale;

        float offsetPxX = (targetW - drawW) * 0.5f;
        float offsetPxY = (targetH - drawH) * 0.5f;

        // Layout in Model-Units (hier: volle UserHeight)
        float layoutH = LayoutBandsStatic(screen, modelW, modelH);
        if (layoutH <= 0)
            return bitmap;

        canvas.Save();

        canvas.Scale(scale);
        canvas.Translate(offsetPxX / scale, offsetPxY / scale);

        var ctx = new RenderContext
        {
            LiveMode = false,
            MouseMode = default,
            SelectedBand = null,
            SelectedPage = null,
            SelectedScreen = screen,
            SelectedControls = null,
            ShowGrid = false,
            GridSize = 0,
            ShowBandBorders = false,
        };

        RenderScreenBackground(canvas, screen, modelW, layoutH);

        var header = screen.ShowHeader
            ? screen.Bands.FirstOrDefault(b => b.BandType == BandType.Header)
            : null;
        var footer = screen.ShowFooter
            ? screen.Bands.FirstOrDefault(b => b.BandType == BandType.Footer)
            : null;

        var customBands = screen.Bands.Where(b => b.BandType == BandType.Custom).ToList();

        for (int i = 0; i < customBands.Count; i++)
            customBands[i].RenderBackground(canvas, ctx);

        header?.RenderBackground(canvas, ctx);
        footer?.RenderBackground(canvas, ctx);

        for (int i = 0; i < customBands.Count; i++)
            customBands[i].RenderControls(canvas, ctx);

        header?.RenderControls(canvas, ctx);
        footer?.RenderControls(canvas, ctx);

        canvas.Restore();
        canvas.Flush();

        return bitmap;
    }

    private static float LayoutBandsStatic(Screen screen, float screenW, float designerH)
    {
        float Round(float v) => MathF.Round(v);

        bool IsBandVisible(Band b) =>
            (b.BandType != BandType.Header || screen.ShowHeader)
            && (b.BandType != BandType.Footer || screen.ShowFooter);

        float h = Round(designerH);
        if (h <= 1)
            h = 1;

        var header = screen.Bands.FirstOrDefault(b => b.BandType == BandType.Header);
        var footer = screen.Bands.FirstOrDefault(b => b.BandType == BandType.Footer);

        foreach (var b in screen.Bands)
        {
            b.Width = Round(screenW);
            b.X = 0;
        }

        float headerH =
            (header != null && IsBandVisible(header)) ? Round(header.EffectiveHeight) : 0f;
        float footerH =
            (footer != null && IsBandVisible(footer)) ? Round(footer.EffectiveHeight) : 0f;

        // Unsichtbare Bands wegschieben
        foreach (var band in screen.Bands)
        {
            if (!IsBandVisible(band))
            {
                band.UpdateBandWorldBounds(0, -10000);
                UpdateActivePageWorldBounds(band);
            }
        }

        // Header oben
        if (header != null && IsBandVisible(header))
        {
            header.UpdateBandWorldBounds(0, 0);
            UpdateActivePageWorldBounds(header);
        }

        var customBands = screen
            .Bands.Where(b => b.BandType == BandType.Custom && IsBandVisible(b))
            .ToList();

        float y = Round(headerH);

        for (int i = 0; i < customBands.Count; i++)
        {
            var band = customBands[i];

            band.UpdateBandWorldBounds(0, Round(y));
            UpdateActivePageWorldBounds(band);

            y = Round(y + band.EffectiveHeight);
        }

        // Footer unten (im Thumbnail ohne +1, damit nichts "zu tief" wirkt)
        if (footer != null && IsBandVisible(footer))
        {
            float fy = Round(h - footerH);
            footer.UpdateBandWorldBounds(0, fy);
            UpdateActivePageWorldBounds(footer);
        }

        return h;
    }

    private static void UpdateActivePageWorldBounds(Band band)
    {
        var page = band.ActivePage;
        if (page == null)
            return;

        page.WorldBounds = band.ContentRect;

        foreach (var c in page.Controls)
        {
            c.UpdateWorldBounds(page.WorldBounds.Left, page.WorldBounds.Top);
        }
    }

    // =====================================================================
    // SCREEN BACKGROUND
    // =====================================================================

    private static void RenderScreenBackground(
        SKCanvas canvas,
        Screen screen,
        float screenW,
        float screenH
    )
    {
        using var paint = new SKPaint
        {
            Color = screen.Background.ToSKColor(),
            IsAntialias = false,
        };

        canvas.DrawRect(new SKRect(0, 0, screenW, screenH), paint);

        var backgroundImage = screen.BackgroundImage;
        if (backgroundImage != null)
        {
            var dest = new SKRect(0, 0, screenW, screenH);
            canvas.DrawBitmap(backgroundImage, dest);
        }
    }
}