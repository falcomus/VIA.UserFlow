using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.Messages;
using Mockup.Registry;
using Mockup.Rendering;
using SkiaSharp;
using System.Diagnostics;
using System.Windows;

namespace Mockup.Actions;

/// <summary>
/// 🔷 ActionArea – unsichtbarer Interaktionsbereich.
/// - Enthält eine Liste von ActionDefinition (je Trigger eine).
/// - Wird im Designer sichtbar, im LiveMode interaktiv.
/// - Hat keine eigene Logik für Popups → Auswertung passiert im Preview/Runtime.
/// </summary>
// HINWEIS: DAS NUR AKTIVIEREN, WENN ACTIONAREA IN TOOLBOX ANGEZEIGT WREDEN SOLL!
[ControlType(displayName: "Action Area", group: "Actions")]
public partial class ActionArea : DesignControl
{
    #region === PROPERTIES ===

    /// <summary>
    /// Liste an Aktionen, die von dieser ActionArea ausgelöst werden können.
    /// (z. B. Tap → Navigate, Tap → ShowPopup etc.)
    /// </summary>
    [ObservableProperty]
    [property: ControlCategory("Actions")]
    [property: ControlProp]
    private List<ActionDefinition> actions = [];

    /// <summary>
    /// Nur Designer-Farbe – wird nicht angezeigt im Preview.
    /// </summary>
    [ObservableProperty]
    private string color = "#3D7AFE";

    private static SKPaint BorderPaint = new()
    {
        Style = SKPaintStyle.Stroke,
        Color = SKColors.Red,
        StrokeWidth = 1.5f,
        IsAntialias = true,
    };
    private static SKPaint CirclePaint = new()
    {
        Style = SKPaintStyle.Fill,
        Color = SKColors.Red,
    };
    private static SKPaint CircleBorderPaint = new()
    {
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1f,
        Color = SKColors.White,
        IsAntialias = true,
    };
    private static SKPaint CircleBorderPaint2 = new()
    {
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1f,
        Color = SKColors.Maroon,
        IsAntialias = true,
    };

    #endregion

    #region === PREVIEW INTERACTION ===

    public override bool SupportsPreviewHint => true;

    public override object? GetPreviewHintSource() => this;

    #endregion

    #region === CTOR ===

    public ActionArea()
    {
        IsActionControl = true;

        Name = "ActionArea";

        ZIndex = 999;

        Width = 90;
        Height = 60;

        MinWidth = 30;
        MinHeight = 30;
        MaxWidth = float.MaxValue;
        MaxHeight = float.MaxValue;

        ResizeStyle = ResizeStyles.ResizeAll;

    }

    public override string ToString() => "ACTION AREA";

    #endregion

    #region === HITTEST ===

    public override bool HitTest(SKPoint point)
    {
        return HitTestBounds(point);
    }
    public bool HitTestBounds(SKPoint point)
    {
        return HitTestCircle(point) || HitTestBorder(point);
    }

    public bool HitTestLive(SKPoint point)
    {
        return HitTestContentArea(point) || HitTestCircle(point);
    }

    private bool HitTestCircle(SKPoint point)
    {
        float interactiveRadius = CircleRadius + 3f;

        var center = new SKPoint(VisualRect.MidX, VisualRect.Top);

        var circleBounds = new SKRect(
            center.X - interactiveRadius,
            center.Y - interactiveRadius,
            center.X + interactiveRadius,
            center.Y + interactiveRadius
        );

        return circleBounds.Contains(point);
    }

    private bool HitTestBorder(SKPoint point)
    {
        const float borderTolerance = 12f;

        var outer = new SKRect(
            VisualRect.Left - borderTolerance,
            VisualRect.Top - borderTolerance,
            VisualRect.Right + borderTolerance,
            VisualRect.Bottom + borderTolerance
        );

        if (!outer.Contains(point))
            return false;

        var inner = new SKRect(
            VisualRect.Left + borderTolerance,
            VisualRect.Top + borderTolerance,
            VisualRect.Right - borderTolerance,
            VisualRect.Bottom - borderTolerance
        );

        return !inner.Contains(point);
    }

    private bool HitTestContentArea(SKPoint point)
        => VisualRect.Contains(point);

    #endregion

    #region === MOUSEEVENT HOOKS ===

    public override void OnPointerDown(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode)
            return;

        BeginGesture(ctx.WorldPoint);
    }

    public override void OnPointerUp(in PointerContext ctx)
    {
        if (!ctx.IsLiveMode)
            return;

        var trigger = EndGesture(ctx.WorldPoint);

        if (trigger != null)
        {
            MSG.AA.Trigger(this, trigger.Value);
        }
    }

    public override void OnPointerMove(in PointerContext ctx) { }

    #endregion

    #region === GESTURE DETECTION ===

    private Stopwatch? _gestureWatch;
    private SKPoint _gestureDown;

    public void BeginGesture(SKPoint point)
    {
        _gestureDown = point;
        _gestureWatch = Stopwatch.StartNew();
    }

    public ActionTrigger? EndGesture(SKPoint point)
    {
        if (_gestureWatch == null)
            return null;

        _gestureWatch.Stop();
        long ms = _gestureWatch.ElapsedMilliseconds;

        return GestureDetection.DetectTrigger(this, _gestureDown, point, ms);
    }

    #endregion

    #region === ACTION LOOKUP ===

    /// <summary>
    /// Liefert die ActionDefinition für einen Trigger (oder null).
    /// </summary>
    public ActionDefinition? GetActionForTrigger(ActionTrigger trigger) =>
        Actions.FirstOrDefault(a => a.Trigger == trigger);

    #endregion

    #region === GET CLICK ACTION ===

    public override ControlClickAction GetClickAction(SKPoint point)
    {
        return Bounds.Contains(point) ? ControlClickAction.Edit : ControlClickAction.None;
    }

    #endregion

    #region === RENDER ===
    public override void Render(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        layout.Inflate(-1.5f, -1.5f);

        if (IsSelected)
        {
            DrawInnerHalo(canvas, layout);

            using var weakFill = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.Red.WithAlpha(8),
            };
            canvas.DrawRect(layout, weakFill);
        }

        canvas.DrawRect(layout, BorderPaint);
    }

    private static void DrawInnerHalo(SKCanvas canvas, SKRect layout)
    {
        const int haloSteps = 4;

        for (int i = 0; i < haloSteps; i++)
        {
            float inset = 2f + (i * 2f);

            if (layout.Width <= inset * 2f || layout.Height <= inset * 2f)
                break;

            var haloRect = new SKRect(
                layout.Left + inset,
                layout.Top + inset,
                layout.Right - inset,
                layout.Bottom - inset
            );

            using var haloPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Red.WithAlpha((byte)(42 - (i * 8))),
                StrokeWidth = 2f,
                IsAntialias = true,
            };

            canvas.DrawRect(haloRect, haloPaint);
        }
    }

    private const float CircleRadius = 12f;

    public void RenderActionCircle(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        if (ctx.LiveMode)
            return;

        DrawActionCircle(canvas, layout, ctx);
    }

    public static void DrawActionCircle(SKCanvas canvas, SKRect layout, RenderContext ctx)
    {
        canvas.DrawCircle(layout.MidX, layout.Top, CircleRadius, CirclePaint);
        canvas.DrawCircle(layout.MidX, layout.Top, CircleRadius, CircleBorderPaint2);
        canvas.DrawCircle(layout.MidX, layout.Top, CircleRadius - 1.5f, CircleBorderPaint);

        float size = 2 * CircleRadius;
        var textRect = SKRect.Create(
            layout.MidX - size / 2f,
            layout.Top - size / 2f + 0.5f,
            size,
            size
        );

        TextRenderer.Draw(
            canvas,
            "A",
            textRect,
            10f,
            SKColors.White,
            fontWeight: FontWeight.FromOpenTypeWeight(700)
        );
    }

    #endregion
}

#region === CLASS DRAGFORMATS & ACTION DRAGDATA ===

public static class DragFormats
{
    public const string ActionArea = "Mockup/ActionArea";
}

public sealed class ActionDragData
{
    public string Action { get; init; } = "Navigate:Settings";
    public ActionTrigger Trigger { get; init; } = ActionTrigger.Tap;
    public float DefaultWidth { get; init; } = 120;
    public float DefaultHeight { get; init; } = 60;
}

#endregion

#region === CLASS GESTURE DETECTION ===

public static class GestureDetection
{
    public const float TAP_MAX_DIST = 4f;
    public const float SWIPE_MIN_DIST = 30f;
    public const int LONGPRESS_MIN_MS = 400;
    public const int TAP_MAX_MS = 300;

    public static ActionTrigger? DetectTrigger(ActionArea area, SKPoint from, SKPoint to, long ms)
    {
        float dx = to.X - from.X;
        float dy = to.Y - from.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        if (dist < TAP_MAX_DIST && ms >= LONGPRESS_MIN_MS)
        {
            ResetLastTap();
            return ActionTrigger.LongPress;
        }

        if (dist >= SWIPE_MIN_DIST)
        {
            ResetLastTap();
            return Math.Abs(dx) > Math.Abs(dy)
                ? (dx > 0 ? ActionTrigger.SwipeRight : ActionTrigger.SwipeLeft)
                : (dy > 0 ? ActionTrigger.SwipeDown : ActionTrigger.SwipeUp);
        }

        if (dist < TAP_MAX_DIST && ms < TAP_MAX_MS)
        {
            ResetLastTap();
            return ActionTrigger.Tap;
        }

        ResetLastTap();
        return null;
    }

    public static void ResetLastTap() { }
}

#endregion
