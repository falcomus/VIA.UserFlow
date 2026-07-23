// ======================================================================================
// FILE: Mockup/DesignControl.cs
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows.Input;
using System.Windows.Media;

namespace Mockup;

public partial class DesignControl : ObservableRecipient
{
    #region === Konstruktor ================================================================

    public DesignControl() { }

    #endregion

    #region === TYPE KEY ===================================================================

    /// <summary>
    /// Persistenter Schlüssel des Controls für JSON.
    /// Wird ausschließlich vom ControlRegistry gesetzt.
    /// </summary>
    [JsonPropertyName("typeKey")]
    public string TypeKey { get; internal set; } = string.Empty;

    #endregion

    #region === Visual Runtime State ========================================================

    [property: JsonIgnore, Browsable(false)]
    public SKRect VisualRect { get; set; }

    [property: JsonIgnore, Browsable(false)]
    public SKRect VisualContentRect { get; private set; }

    [ObservableProperty]
    [property: JsonIgnore, Browsable(false)]
    private float explicitePreviewHeight = 0f;

    [ObservableProperty]
    [property: JsonIgnore, Browsable(false)]
    private float explicitePreviewWidth = 0f;

    #endregion

    #region === Identity ===================================================================

    [ObservableProperty]
    [property: Browsable(false)]
    private long id = ControlIdGenerator.NewID;

    [ObservableProperty]
    [property: System.ComponentModel.Category("Name")]
    private string name = string.Empty;

    public bool IsActionControl { get; internal set; } = false;

    #endregion

    #region === Preview Interaction =========================================================

    /// <summary>
    /// Kennzeichnet, ob das Control grundsätzlich Preview-/Live-Interaktion unterstützt.
    /// Standard: folgt dem bisherigen IsActionControl-Verhalten.
    /// </summary>
    [JsonIgnore, Browsable(false)]
    public virtual bool SupportsPreviewInteraction => IsActionControl;

    /// <summary>
    /// Kennzeichnet, ob das Control im Preview einen Hint/Tooltip liefern möchte.
    /// Standard: nein. ActionArea kann dies überschreiben.
    /// </summary>
    [JsonIgnore, Browsable(false)]
    public virtual bool SupportsPreviewHint => false;

    /// <summary>
    /// Liefert optional die Hint-Quelle für Preview-Hover.
    /// Standard: null. ActionArea kann hier sich selbst zurückgeben.
    /// </summary>
    public virtual object? GetPreviewHintSource() => null;

    #endregion

    #region === Position & Size =============================================================

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    private float x;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    private float y;

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    private float width;

    partial void OnWidthChanged(float value)
    {
        float clamped = Math.Clamp(value, MinWidth, MaxWidth);

        if (Math.Abs(value - clamped) > 0.0001f)
            Width = clamped;
    }

    [ObservableProperty]
    [property: ControlProp]
    [property: System.ComponentModel.Category("Layout")]
    private float height = 130;

    partial void OnHeightChanged(float value)
    {
        float clamped = Math.Clamp(value, MinHeight, MaxHeight);

        if (Math.Abs(value - clamped) > 0.0001f)
            Height = clamped;
    }

    [ObservableProperty]
    [property: ControlProp]
    //[property: Browsable(false)]
    [property: System.ComponentModel.Category("Layout")]
    private float minWidth = 20;

    [ObservableProperty]
    [property: ControlProp]
    //[property: Browsable(false)]
    [property: System.ComponentModel.Category("Layout")]
    private float minHeight = 20;

    [ObservableProperty]
    [property: ControlProp]
    //[property: Browsable(false)]
    [property: System.ComponentModel.Category("Layout")]
    private float maxWidth = 9999;

    [ObservableProperty]
    [property: ControlProp]
    //[property: Browsable(false)]
    [property: System.ComponentModel.Category("Layout")]
    private float maxHeight = 9999;

    #endregion

    #region === Visual & Behaviour ==========================================================

    [ObservableProperty]
    [property: Browsable(false)]
    private int zIndex;

    [ObservableProperty]
    [property: Browsable(false)]
    private bool isSelected;

    [ObservableProperty]
    [property: Browsable(false)]
    private bool isActive;

    [ObservableProperty]
    [property: Browsable(false)]
    private ResizeStyles resizeStyle = ResizeStyles.ResizeAll;

    #endregion

    #region === Band-Verknüpfung ============================================================

    [property: JsonIgnore, Browsable(false)]
    public Band? ParentBand { get; set; }

    [property: JsonIgnore, Browsable(false)]
    public BandPage? ParentBandPage { get; set; }

    #endregion

    #region === World Bounds (NEU – REQUIRED) ===============================================

    /// <summary>
    // Controls liegen relativ zum Band - Contentbereich
    // nicht zu irgendeiner abstrakten Page.
    /// </summary>
    public void UpdateWorldBounds(float pageWorldX, float pageWorldY)
    {
        VisualRect = new SKRect(
            pageWorldX + X,
            pageWorldY + Y,
            pageWorldX + X + Width,
            pageWorldY + Y + Height
        );

        // Aktuell identisch – vorbereitet für Padding / ContentRect
        VisualContentRect = VisualRect;
    }

    #endregion

    #region === MOUSEEVENT HOOKS ===

    public readonly record struct PointerContext(
        SKPoint WorldPoint,
        MouseButton? Button,
        int ClickCount,
        bool IsLiveMode,
        bool Ctrl,
        bool Shift,
        bool Alt
    );

    public virtual void OnPointerDown(in PointerContext ctx) { }

    public virtual void OnPointerMove(in PointerContext ctx) { }

    public virtual void OnPointerLeave() { }

    public virtual void OnPointerUp(in PointerContext ctx) { }

    #endregion === MOUSEEVENT HOOKS ===

    #region === RENDER ===

    public virtual void Render(SKCanvas canvas, SKRect layout, RenderContext ctx) { }

    public virtual void RenderAt(SKCanvas canvas, SKPoint pos)
    {
        var rect = new SKRect(pos.X, pos.Y, pos.X + Width, pos.Y + Height);
        Render(canvas, rect, RenderContext.Default);
    }

    #endregion === Render ===

    #region === HitTest / Bounds ===

    public virtual bool HitTest(SKPoint point)
    {
        return VisualRect.Contains(point);
    }

    [JsonIgnore, Browsable(false)]
    public SKRect Bounds => SKRect.Create(X, Y, Width, Height);

    public virtual ControlClickAction GetClickAction(SKPoint point) => ControlClickAction.None;

    public override string ToString() => $"{GetType().Name} ({Width}x{Height})";

    #endregion === HitTest / Bounds ===

    #region === HitTest ResizeHandles ===

    public bool HitTestResizeHandle(SKPoint p, out ControlResizeHandle handle)
    {
        handle = ControlResizeHandle.None;

        // Kein Resize → keine Handles
        if (ResizeStyle == ResizeStyles.None)
            return false;

        const float radius = 4f; // visueller Ball
        const float tolerance = 2f; // UX-Toleranz
        float r = radius + tolerance;

        bool allowWidth =
            ResizeStyle == ResizeStyles.ResizeAll
            || ResizeStyle == ResizeStyles.WidthOnly
            || ResizeStyle == ResizeStyles.KeepRatio;

        bool allowHeight =
            ResizeStyle == ResizeStyles.ResizeAll
            || ResizeStyle == ResizeStyles.HeightOnly
            || ResizeStyle == ResizeStyles.KeepRatio;

        // KeepRatio: nur BottomRight
        if (ResizeStyle == ResizeStyles.KeepRatio)
        {
            return HitCircle(
                p,
                VisualRect.Right,
                VisualRect.Bottom,
                r,
                ControlResizeHandle.BottomRight,
                out handle
            );
        }

        // Ecken (nur wenn Breite + Höhe erlaubt)
        if (allowWidth && allowHeight)
        {
            if (
                HitCircle(
                    p,
                    VisualRect.Left,
                    VisualRect.Top,
                    r,
                    ControlResizeHandle.TopLeft,
                    out handle
                )
            )
                return true;
            if (
                HitCircle(
                    p,
                    VisualRect.Right,
                    VisualRect.Top,
                    r,
                    ControlResizeHandle.TopRight,
                    out handle
                )
            )
                return true;
            if (
                HitCircle(
                    p,
                    VisualRect.Left,
                    VisualRect.Bottom,
                    r,
                    ControlResizeHandle.BottomLeft,
                    out handle
                )
            )
                return true;
            if (
                HitCircle(
                    p,
                    VisualRect.Right,
                    VisualRect.Bottom,
                    r,
                    ControlResizeHandle.BottomRight,
                    out handle
                )
            )
                return true;
        }

        // Seiten
        if (allowWidth)
        {
            if (
                HitCircle(
                    p,
                    VisualRect.Left,
                    VisualRect.MidY,
                    r,
                    ControlResizeHandle.Left,
                    out handle
                )
            )
                return true;
            if (
                HitCircle(
                    p,
                    VisualRect.Right,
                    VisualRect.MidY,
                    r,
                    ControlResizeHandle.Right,
                    out handle
                )
            )
                return true;
        }

        if (allowHeight)
        {
            if (
                HitCircle(
                    p,
                    VisualRect.MidX,
                    VisualRect.Top,
                    r,
                    ControlResizeHandle.Top,
                    out handle
                )
            )
                return true;
            if (
                HitCircle(
                    p,
                    VisualRect.MidX,
                    VisualRect.Bottom,
                    r,
                    ControlResizeHandle.Bottom,
                    out handle
                )
            )
                return true;
        }

        return false;
    }

    private static bool HitCircle(
        SKPoint p,
        float cx,
        float cy,
        float radius,
        ControlResizeHandle value,
        out ControlResizeHandle handle
    )
    {
        float dx = p.X - cx;
        float dy = p.Y - cy;

        if (dx * dx + dy * dy <= radius * radius)
        {
            handle = value;
            return true;
        }

        handle = ControlResizeHandle.None;
        return false;
    }

    #endregion === HitTest ResizeHandles ===

    #region === Flow (Legacy) ===============================================================

    [JsonIgnore, Browsable(false)]
    public virtual float FlowContribution => 0f;

    #endregion

    #region === PropertyChanged =============================================================

    //protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    //{
    //    MockupService.Mockup.PushSnapshot(SnapshotContext.Screen, SnapshotLabels.ControlPropChanged);
    //}

    #endregion

    #region === Cloning =====================================================================

    public virtual DesignControl DeepClone()
    {
        var clone = ControlRegistry.Create(TypeKey) ?? new DesignControl();

        clone.TypeKey = TypeKey;
        clone.Id = IdGenerator.NewID;
        clone.Name = Name;
        clone.X = X;
        clone.Y = Y;
        clone.Width = Width;
        clone.Height = Height;
        clone.ZIndex = ZIndex;

        ControlPropSchemaCache.CopyProps(this, clone);

        // ParentBand / ParentBandPage werden EXTERN gesetzt

        return clone;
    }

    #endregion

    #region === COLORS ======================================================================

    public virtual Color GetFillColor(object? variant, Color customColor)
    {
        return variant switch
        {
            ControlVariant.CUSTOM => customColor,
            ControlVariant.Primary => Theme.Primary,
            ControlVariant.Accent => Theme.Accent,
            ControlVariant.Info => Theme.Info,
            ControlVariant.Warning => Theme.Warning,
            ControlVariant.Error => Theme.Error,
            _ => Theme.Neutral,
        };
    }

    public virtual Color GetBorderColor(object? variant, Color customColor)
    {
        return variant switch
        {
            ControlVariant.CUSTOM => customColor,
            ControlVariant.Primary => Theme.Primary,
            ControlVariant.Accent => Theme.Accent,
            ControlVariant.Info => Theme.Info,
            ControlVariant.Warning => Theme.Warning,
            ControlVariant.Error => Theme.Error,
            _ => Theme.Neutral,
        };
    }

    public virtual Color GetTextColor(ControlVariant? variant, Color customColor) =>
        variant == ControlVariant.CUSTOM ? customColor : Colors.White;

    #endregion

    #region === ELEVATIONS / SHADOWS ========================================================

    public static readonly ShadowOptions Elevation1 = new()
    {
        Color = SKColors.Black.WithAlpha(140),
        Dx = 0f,
        Dy = 2f,
        Sigma = 2f,
    };

    public static readonly ShadowOptions Elevation2 = new()
    {
        Color = SKColors.Black.WithAlpha(140),
        Dx = 0f,
        Dy = 2.5f,
        Sigma = 3f,
    };

    public static readonly ShadowOptions Elevation3 = new()
    {
        Color = SKColors.Black.WithAlpha(140),
        Dx = 0f,
        Dy = 3f,
        Sigma = 4f,
    };

    public static readonly ShadowOptions Elevation4 = new()
    {
        Color = SKColors.Black.WithAlpha(150),
        Dx = 0f,
        Dy = 4f,
        Sigma = 5f,
    };

    public static readonly ShadowOptions Elevation5 = new()
    {
        Color = SKColors.Black.WithAlpha(150),
        Dx = 0f,
        Dy = 5f,
        Sigma = 7f,
    };

    public static ShadowOptions GetElevation(int elevation)
    {
        return elevation switch
        {
            1 => Elevation1,
            2 => Elevation2,
            3 => Elevation3,
            4 => Elevation4,
            5 => Elevation5,
            _ => ShadowOptions.Default,
        };
    }

    #endregion

    #region === RESIZEHANDLE BRUSHES ===

    private const float RESIZEHANDLE_SIZE = 3.5f;

    private static readonly SKPaint _framePaint = new()
    {
        Color = SKColors.DodgerBlue,
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.5f,
    };

    private static readonly SKPaint _handlePaint = new()
    {
        Color = SKColors.DodgerBlue,
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    // ============================================================
    // Fancy circular resize handle paints (cached per radius)
    // ============================================================

    private static float _cachedRadius = -1f;

    // Keep shaders only if you want explicit handles; otherwise paint.Dispose() is enough
    private static SKShader? _fillShader;
    private static SKShader? _highlightSoftShader;

    // Paints (created once per radius)
    private static SKPaint? _fillPaint;
    private static SKPaint? _highlightSoftPaint;
    private static SKPaint? _highlightHardPaint;
    private static SKPaint? _innerStrokePaint;
    private static SKPaint? _outerStrokePaint;

    public static void EnsureResizeHandlePaints(float radius)
    {
        // already valid
        if (
            Math.Abs(radius - _cachedRadius) < 0.001f
            && _fillPaint != null
            && _highlightSoftPaint != null
            && _highlightHardPaint != null
            && _innerStrokePaint != null
            && _outerStrokePaint != null
        )
            return;

        _cachedRadius = radius;

        // Dispose old paints/shaders
        _fillPaint?.Dispose();
        _highlightSoftPaint?.Dispose();
        _highlightHardPaint?.Dispose();
        _innerStrokePaint?.Dispose();
        _outerStrokePaint?.Dispose();

        _fillShader?.Dispose();
        _highlightSoftShader?.Dispose();

        // 1) Main 3D gradient (light top-left, dark rim)
        _fillShader = SKShader.CreateRadialGradient(
            new SKPoint(-radius * 0.2f, -radius * 0.2f),
            radius * 1.4f,
            new[]
            {
                SKColor.Parse("#63B8FF"), // light
                SKColor.Parse("#1E90FF"), // mid (dodger)
                SKColor.Parse("#0B5FA4"), // rim (dark)
            },
            new[] { 0f, 0.6f, 1f },
            SKShaderTileMode.Clamp
        );

        _fillPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Shader = _fillShader,
        };

        // 2) Soft highlight glow
        _highlightSoftShader = SKShader.CreateRadialGradient(
            new SKPoint(-radius * 0.3f, -radius * 0.3f),
            radius * 0.8f,
            new[] { SKColors.White.WithAlpha(120), SKColors.Transparent },
            null,
            SKShaderTileMode.Clamp
        );

        _highlightSoftPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Shader = _highlightSoftShader,
        };

        // 3) Small hard highlight dot
        _highlightHardPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = SKColors.White.WithAlpha(180),
        };

        // 4) Inner white-ish stroke
        _innerStrokePaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 0.5f,
            Color = SKColors.White.WithAlpha(120),
        };

        // 5) Outer dark stroke
        _outerStrokePaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            Color = SKColor.Parse("#0080C0"),
        };
    }

    #endregion === RESIZEHANDLE BRUSHES ===

    #region === RENDER FRAME AND RESIZE HANDLES ===

    public virtual void RenderFrameAndResizeHandles(SKCanvas canvas, SKRect rect, RenderContext ctx)
    {
        //Draw Frame
        canvas.DrawRect(rect, _framePaint);

        //Draw ResizeHandles

        if (ResizeStyle == ResizeStyles.None)
            return;

        if (ResizeStyle == ResizeStyles.KeepRatio)
        {
            DrawCircleHandle(canvas, rect.Right, rect.Bottom);
            return;
        }

        bool allowWidth =
            ResizeStyle == ResizeStyles.ResizeAll
            || ResizeStyle == ResizeStyles.WidthOnly
            || ResizeStyle == ResizeStyles.KeepRatio;

        bool allowHeight =
            ResizeStyle == ResizeStyles.ResizeAll
            || ResizeStyle == ResizeStyles.HeightOnly
            || ResizeStyle == ResizeStyles.KeepRatio;

        // Ecken (nur wenn beides erlaubt)
        if (allowWidth && allowHeight)
        {
            DrawCircleHandle(canvas, rect.Left, rect.Top);
            DrawCircleHandle(canvas, rect.Right, rect.Top);
            DrawCircleHandle(canvas, rect.Left, rect.Bottom);
            DrawCircleHandle(canvas, rect.Right, rect.Bottom);
        }

        // Seiten
        if (allowWidth)
        {
            DrawCircleHandle(canvas, rect.Left, rect.MidY);
            DrawCircleHandle(canvas, rect.Right, rect.MidY);
        }

        if (allowHeight)
        {
            DrawCircleHandle(canvas, rect.MidX, rect.Top);
            DrawCircleHandle(canvas, rect.MidX, rect.Bottom);
        }
    }

    private void DrawCircleHandle(SKCanvas canvas, float x, float y)
    {
        EnsureResizeHandlePaints(RESIZEHANDLE_SIZE);
        float r = RESIZEHANDLE_SIZE;

        using (new SKAutoCanvasRestore(canvas))
        {
            canvas.Translate(x, y);

            canvas.DrawCircle(0, 0, r, _fillPaint!);
            canvas.DrawCircle(0, 0, r, _highlightSoftPaint!);
            canvas.DrawCircle(-r * 0.35f, -r * 0.35f, r * 0.18f, _highlightHardPaint!);
            canvas.DrawCircle(0, 0, r - 0.3f, _innerStrokePaint!);
            canvas.DrawCircle(0, 0, r, _outerStrokePaint!);
        }
    }

    #endregion === RENDER FRAME AND RESIZE HANDLES ===
}

#region === CLASS ID GENERATOR ===

public static class ControlIdGenerator
{
    public static long NewID
    {
        get
        {
            long v = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0);
            return v < 0 ? -v : v;
        }
    }
}

#endregion === CLASS ID GENERATOR ===
