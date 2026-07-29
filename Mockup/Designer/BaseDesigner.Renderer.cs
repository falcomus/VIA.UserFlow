// ======================================================================================
// FILE: Mockup.Designer/BaseDesigner.Renderer.cs
//
// Sticky Bands (optional):
// - Alternative Render/Layout Methoden mit Suffix "Sticky"
// - Umschaltung über bool StickyBands (Default: false)
// - Sticky-Verhalten gilt für Custom-Bands innerhalb des scrollbaren Bereichs (zwischen Header/Footer)
//   -> "pinned header" + "push-off" durch den nächsten Band-Header
// ======================================================================================

using Mockup.Actions;
using Mockup.Rendering;
using Mockup.ViewModel;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TextAlignment = Topten.RichTextKit.TextAlignment;

namespace Mockup.Designer;

public abstract partial class BaseDesigner : Control
{
    #region === STICKY MODE SWITCH ===

    protected virtual float GetStickyHeaderHeight(Band band) => band.HeaderHeight;

    #endregion === STICKY MODE SWITCH ===

    #region === BRUSHES MULTISELECTION BORDER ===

    private static readonly SKPaint _multiFrameBorderPaint = new()
    {
        Color = SKColors.DodgerBlue,
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.5f,
        PathEffect = SKPathEffect.CreateDash([3, 2], 0),
    };

    private static readonly SKPaint _multiFrameFillPaint = new()
    {
        Color = SKColors.DodgerBlue.WithAlpha(10),
        IsAntialias = false,
        Style = SKPaintStyle.Fill,
    };

    private static readonly SKPaint _multiItemFrameBorderPaint = new()
    {
        Color = SKColors.DodgerBlue,
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2f,
        PathEffect = SKPathEffect.CreateDash([2, 2], 0),
    };

    private static readonly SKPaint _multiItemFrameFillPaint = new()
    {
        Color = SKColors.Black.WithAlpha(60),
        IsAntialias = false,
        Style = SKPaintStyle.Fill,
    };

    private static readonly SKPaint _rubberbandBorderPaint = new()
    {
        Color = SKColors.DodgerBlue,
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.25f,
        PathEffect = SKPathEffect.CreateDash([4, 2], 0),
    };

    private static readonly SKPaint _rubberbandFillPaint = new()
    {
        Color = SKColors.DodgerBlue.WithAlpha(18),
        IsAntialias = false,
        Style = SKPaintStyle.Fill,
    };

    private static readonly SKPaint _interactionHintShadowPaint = new()
    {
        Color = SKColors.Black.WithAlpha(24),
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    private static readonly SKPaint _interactionHintFillPaint = new()
    {
        Color = new SKColor(32, 36, 43, 218),
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    private static readonly SKPaint _interactionHintBorderPaint = new()
    {
        Color = SKColors.White.WithAlpha(42),
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 0.75f,
    };

    private static readonly SKPaint _interactionHintMeasurePaint = new()
    {
        Color = SKColors.White,
        IsAntialias = true,
        TextSize = 10.5f,
        Typeface = SKTypeface.Default,
    };

    #endregion === BRUSHES ===

    #region === RENDER ENTRY POINT (OnPaintSurface) ===

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        if (DesignerHeight <= 0 || PART_Canvas == null)
            return;

        OnPaintSurfaceNormal(sender, e);
    }

    private void OnPaintSurfaceNormal(object? sender, SKPaintSurfaceEventArgs e)
    {
        if (DesignerHeight <= 0 || PART_Canvas == null)
            return;

        if (MockupService.Mockup.CurrentProject == null)
            return;

        var viewModel = (MockupViewModel)DataContext;

        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.White);

        DpiScale(canvas);
        LayoutPrepass();

        bool preview = LiveMode && DesignerKind == DesignerKind.Screen;
        bool popupPreview = DesignerKind == DesignerKind.Popup && IsPreviewHost;

        var ctx = new RenderContext
        {
            LiveMode = LiveMode,
            MouseMode = _mouseState.Mode,
            SelectedBand = SelectedBand,
            SelectedPage = null,
            SelectedScreen = MockupService.Mockup.CurrentScreen,
            SelectedControls = VM.SelectedControls,
        };

        ctx.ShowActionAreas = !IsPreview || (IsPreview && Keyboard.IsKeyDown(Key.Space));

        RenderScreenBackground(canvas, ctx);
        RenderPopupHeaderIfNeeded(canvas);

        var headerBand = GetHeaderBand();
        var footerBand = GetFooterBand();
        var customBands = GetCustomBands()?.ToList() ?? [];

        float clipTop = headerBand != null ? headerBand.WorldBounds.Bottom : 0f;
        float clipBottom =
            footerBand != null ? footerBand.WorldBounds.Top : (float)PART_Canvas.ActualHeight;

        // ============================================================
        // PASS 1: BACKGROUNDS
        // ============================================================

        if (preview)
        {
            canvas.Save();
            canvas.ClipRect(
                new SKRect(0, clipTop, (float)PART_Canvas.ActualWidth, clipBottom),
                SKClipOperation.Intersect,
                true
            );
        }

        for (int i = 0; i < customBands.Count; i++)
        {
            var band = customBands[i];
            band.RenderBackground(canvas, ctx);
        }

        if (preview)
            canvas.Restore();

        if (headerBand != null)
        {
            headerBand.RenderBackground(canvas, ctx);
        }

        if (footerBand != null)
        {
            footerBand.RenderBackground(canvas, ctx);
        }

        // ============================================================
        // PASS 2: CONTROLS
        // ============================================================

        if (preview)
        {
            canvas.Save();
            canvas.ClipRect(
                new SKRect(0, clipTop, (float)PART_Canvas.ActualWidth, clipBottom),
                SKClipOperation.Intersect,
                true
            );
        }

        for (int i = 0; i < customBands.Count; i++)
        {
            var band = customBands[i];
            band.RenderControls(canvas, ctx);
        }

        if (preview)
            canvas.Restore();

        headerBand?.RenderControls(canvas, ctx);
        footerBand?.RenderControls(canvas, ctx);

        if (!preview)
        {
            if (headerBand != null)
                RenderControlZIndexDebug(canvas, headerBand);

            if (footerBand != null)
                RenderControlZIndexDebug(canvas, footerBand);
        }

        if (preview)
        {
            var stickyBands = customBands.Where(b => b.IsSticky && b.HasVisibleHeader).ToList();

            if (stickyBands.Count > 0)
                RenderStickyBandHeaderOverlay(
                    canvas,
                    ctx,
                    stickyBands,
                    customBands,
                    clipTop,
                    clipBottom
                );
        }

        // ============================================================
        // PASS 4: SELECTION OVERLAY
        // ============================================================

        RenderSelectedControlsTopmost(canvas, ctx, preview, clipTop, clipBottom);

        // ============================================================
        // PASS 5: RENDER RUBBERBAND
        // ============================================================

        RenderRubberbandSelectionOverlay(canvas);

        // ============================================================
        // PASS 5: DESIGNER OVERLAY
        // ============================================================

        RenderDesignerOverlay(canvas, ctx);

        if (!preview && Keyboard.IsKeyDown(Key.Space))
        {
            foreach (var band in customBands)
                RenderControlZIndexDebug(canvas, band);

            if (headerBand != null)
                RenderControlZIndexDebug(canvas, headerBand);

            if (footerBand != null)
                RenderControlZIndexDebug(canvas, footerBand);
        }

        RenderAlignmentGuidelines(canvas);
        RenderDesignerInteractionHint(canvas);
    }

    #endregion === RENDER ENTRY POINT (OnPaintSurface) ===

    #region === DESIGNER INTERACTION HINT ===

    private void RenderDesignerInteractionHint(SKCanvas canvas)
    {
        if (PART_Canvas == null || string.IsNullOrWhiteSpace(_designerInteractionHintText))
            return;

        const float horizontalPadding = 6f;
        const float verticalPadding = 2.5f;
        const float cornerRadius = 3f;
        const float outerMargin = 4f;
        const float targetGap = 8f;
        const float widthSafety = 3f;

        float textWidth = _interactionHintMeasurePaint.MeasureText(
            _designerInteractionHintText);
        _interactionHintMeasurePaint.GetFontMetrics(out SKFontMetrics fontMetrics);
        float textHeight = fontMetrics.Descent - fontMetrics.Ascent;
        float width = MathF.Ceiling(
            textWidth + horizontalPadding * 2f + widthSafety);
        float height = MathF.Ceiling(
            textHeight + verticalPadding * 2f);

        float canvasWidth = (float)PART_Canvas.ActualWidth;
        float canvasHeight = (float)PART_Canvas.ActualHeight;

        float x = _designerInteractionHintAnchor.X - width / 2f;
        float y = _designerInteractionHintAnchor.Y - height - targetGap;

        if (y < outerMargin)
            y = _designerInteractionHintFallbackY + targetGap;

        x = Math.Clamp(x, outerMargin, Math.Max(outerMargin, canvasWidth - width - outerMargin));
        y = Math.Clamp(y, outerMargin, Math.Max(outerMargin, canvasHeight - height - outerMargin));

        var rect = new SKRect(x, y, x + width, y + height);
        var shadowRect = rect;
        shadowRect.Offset(0f, 1f);

        canvas.DrawRoundRect(
            shadowRect,
            cornerRadius,
            cornerRadius,
            _interactionHintShadowPaint);

        canvas.DrawRoundRect(
            rect,
            cornerRadius,
            cornerRadius,
            _interactionHintFillPaint);

        canvas.DrawRoundRect(
            rect,
            cornerRadius,
            cornerRadius,
            _interactionHintBorderPaint);

        float textBaseline = rect.MidY - (fontMetrics.Ascent + fontMetrics.Descent) / 2f;
        canvas.DrawText(
            _designerInteractionHintText,
            rect.Left + horizontalPadding,
            textBaseline,
            _interactionHintMeasurePaint);
    }

    #endregion === DESIGNER INTERACTION HINT ===

    #region === DESIGNER OVERLAY HOOK ===

    protected virtual void RenderDesignerOverlay(SKCanvas canvas, RenderContext ctx) { }

    #endregion === DESIGNER OVERLAY HOOK ===

    #region === STICKY: OVERLAY RENDER ===

    private void RenderStickyBandHeaderOverlay(
        SKCanvas canvas,
        RenderContext ctx,
        IList<Band> stickyBands,
        IList<Band> allCustomBands,
        float clipTop,
        float clipBottom
    )
    {
        if (PART_Canvas == null)
            return;

        if (stickyBands.Count == 0 || allCustomBands.Count == 0)
            return;

        float Round(float v) => MathF.Round(v);

        float pinY = Round(clipTop);
        float w = Round((float)PART_Canvas.ActualWidth);

        if (pinY >= clipBottom)
            return;

        int activeIndex = -1;
        float probeY = pinY + 0.5f;

        for (int i = 0; i < stickyBands.Count; i++)
        {
            var b = stickyBands[i];
            if (b.WorldBounds.Top <= probeY && b.WorldBounds.Bottom > probeY)
            {
                activeIndex = i;
                break;
            }
        }

        if (activeIndex < 0)
            activeIndex = stickyBands[0].WorldBounds.Top > probeY ? 0 : stickyBands.Count - 1;

        var active = stickyBands[activeIndex];

        if (active.HeaderRect.IsEmpty)
            return;

        if (!active.IsSticky || !active.HasVisibleHeader)
            return;

        float headerH = Round(GetStickyHeaderHeight(active));
        if (headerH <= 0)
            return;

        float pinnedY = pinY;

        Band? next = null;
        float nextTop = float.NaN;
        float push = 0;

        int activeIndexInAll = -1;

        for (int i = 0; i < allCustomBands.Count; i++)
        {
            if (ReferenceEquals(allCustomBands[i], active))
            {
                activeIndexInAll = i;
                break;
            }
        }

        if (activeIndexInAll >= 0)
        {
            for (int i = activeIndexInAll + 1; i < allCustomBands.Count; i++)
            {
                var candidate = allCustomBands[i];

                if (!candidate.HasVisibleHeader)
                    continue;

                next = candidate;
                nextTop = Round(candidate.WorldBounds.Top);
                break;
            }
        }

        if (next != null)
        {
            push = nextTop - (pinY + headerH);
            if (push < 0)
                pinnedY = pinY + push;
        }

        canvas.Save();

        float dy = Round(pinnedY - Round(active.WorldBounds.Top));

        float clipTopAdj = dy < 0 ? (pinY + dy) : pinY;
        float clipBottomAdj = Math.Min(pinY + headerH, clipBottom);

        if (clipTopAdj < 0)
            clipTopAdj = 0;

        if (next != null && push < 0)
            clipBottomAdj = Math.Min(clipBottomAdj, nextTop);

        if (clipBottomAdj <= clipTopAdj + 0.5f)
        {
            canvas.Restore();
            return;
        }

        var clip = new SKRect(0, clipTopAdj, w, clipBottomAdj);
        canvas.ClipRect(clip, SKClipOperation.Intersect, true);

        canvas.Translate(0, dy);

        active.RenderBackground(canvas, ctx);
        active.RenderControls(canvas, ctx);

        canvas.Restore();
    }

    #endregion === STICKY: OVERLAY RENDER ===

    #region === RENDER SELECTED CONTROLS TOPMOST ===

    private void RenderSelectedControlsTopmost(
        SKCanvas canvas,
        RenderContext ctx,
        bool preview,
        float clipTop,
        float clipBottom
    )
    {
        var sel = ctx.SelectedControls;
        if (sel == null || sel.Count == 0 || PART_Canvas == null)
            return;

        float w = (float)PART_Canvas.ActualWidth;

        if (sel.Count == 1)
        {
            var ctrl = sel[0];
            if (ctrl == null)
                return;

            var band = ctrl.ParentBand;
            if (band == null)
                return;

            if (band.IsExpandable && !band.IsExpanded)
                return;

            UpdateSelectedControlVisualRect(ctrl, ctx);

            bool isCustomBand = band.BandType == BandType.Custom;

            if (preview && isCustomBand)
            {
                canvas.Save();
                canvas.ClipRect(
                    new SKRect(0, clipTop, w, clipBottom),
                    SKClipOperation.Intersect,
                    true
                );
                RenderSingleSelectionOverlay(canvas, ctx, ctrl);
                canvas.Restore();
                return;
            }

            RenderSingleSelectionOverlay(canvas, ctx, ctrl);
            return;
        }

        bool clipped = false;

        if (preview)
        {
            canvas.Save();
            canvas.ClipRect(new SKRect(0, clipTop, w, clipBottom), SKClipOperation.Intersect, true);
            clipped = true;
        }

        foreach (var ctrl in sel)
        {
            if (ctrl == null)
                continue;

            var band = ctrl.ParentBand;
            if (band == null || band.BandType != BandType.Custom)
                continue;

            if (band.IsExpandable && !band.IsExpanded)
                continue;

            UpdateSelectedControlVisualRect(ctrl, ctx);
            RenderMultiSelectionItemOverlay(canvas, ctx, ctrl);
        }

        RenderMultiSelectionFrame(canvas, sel);

        if (clipped)
            canvas.Restore();

        foreach (var ctrl in sel)
        {
            if (ctrl == null)
                continue;

            var band = ctrl.ParentBand;
            if (band == null || band.BandType == BandType.Custom)
                continue;

            if (band.IsExpandable && !band.IsExpanded)
                continue;

            UpdateSelectedControlVisualRect(ctrl, ctx);
            RenderMultiSelectionItemOverlay(canvas, ctx, ctrl);
        }

        RenderMultiSelectionFrame(canvas, sel);
    }

    private void UpdateSelectedControlVisualRect(DesignControl ctrl, RenderContext ctx)
    {
        var band = ctrl.ParentBand;
        if (band == null)
            return;

        var page = ctrl.ParentBandPage;
        if (page == null)
            return;

        var content = page.WorldBounds;

        ctrl.VisualRect = new SKRect(
            content.Left + ctrl.X,
            content.Top + ctrl.Y,
            content.Left + ctrl.X + ctrl.Width,
            content.Top + ctrl.Y + ctrl.Height
        );
    }

    private void RenderSingleSelectionOverlay(
        SKCanvas canvas,
        RenderContext ctx,
        DesignControl ctrl
    )
    {
        if (ctx.LiveMode && ctrl is ActionArea && !ctx.ShowActionAreas)
            return;

        ctrl.RenderFrameAndResizeHandles(canvas, ctrl.VisualRect, ctx);

        if (ctrl is ActionArea area)
            area.RenderActionCircle(canvas, ctrl.VisualRect, ctx);
    }

    private void RenderMultiSelectionItemOverlay(
        SKCanvas canvas,
        RenderContext ctx,
        DesignControl ctrl
    )
    {
        if (ctx.LiveMode && ctrl is ActionArea && !ctx.ShowActionAreas)
            return;

        var rect = ctrl.VisualRect;
        if (rect.IsEmpty)
            return;

        rect.Inflate(1f, 1f);

        canvas.DrawRect(rect, _multiItemFrameFillPaint);
        canvas.DrawRect(rect, _multiItemFrameBorderPaint);
    }

    #endregion

    #region === DEBUG: RENDER CONTROL ZINDEX ===

    private void RenderControlZIndexDebug(SKCanvas canvas, Band band)
    {
        var page = band.ActivePage;
        if (page == null || page.Controls.Count == 0)
            return;

        foreach (var ctrl in page.Controls)
        {
            if (ctrl == null)
                continue;

            if (band.IsExpandable && !band.IsExpanded)
                continue;

            RenderControlZIndexDebug(canvas, ctrl);
        }
    }

    private void RenderControlZIndexDebug(SKCanvas canvas, DesignControl ctrl)
    {
        var visRect = ctrl.VisualRect;

        if (visRect.IsEmpty)
            return;

        using var fillPaint = new SKPaint
        {
            Color = SKColors.Gold,
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };

        using var borderPaint = new SKPaint
        {
            Color = SKColors.Black.WithAlpha(100),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
        };

        float rectSize = 12;

        var rectX = visRect.Right - rectSize / 2;
        var rectY = visRect.Top - rectSize / 2;
        var cornerRadius = 2f;

        canvas.DrawRoundRect(
            rectX,
            rectY,
            rectSize,
            rectSize,
            cornerRadius,
            cornerRadius,
            fillPaint
        );
        canvas.DrawRoundRect(
            rectX,
            rectY,
            rectSize,
            rectSize,
            cornerRadius,
            cornerRadius,
            borderPaint
        );

        var textRect = new SKRect(rectX, rectY + 1, rectX + rectSize, rectY + rectSize + 1);

        TextRenderer.Draw(
            canvas,
            ctrl.ZIndex.ToString(),
            textRect,
            8f,
            SKColors.Black,
            textAlignment: TextAlignment.Center,
            padding: 0f,
            fontWeight: FontWeight.FromOpenTypeWeight(500)
        );
    }

    #endregion === DEBUG: RENDER CONTROL ZINDEX ===

    #region === RENDER SCREEN BACKGROUND ===

    private void RenderScreenBackground(SKCanvas canvas, RenderContext ctx)
    {
        var screen = Screen;
        if (screen == null)
            return;

        using var paint = new SKPaint
        {
            Color = screen.Background.ToSKColor(),
            IsAntialias = false,
        };

        canvas.DrawRect(new SKRect(0, 0, (float)PART_Canvas.ActualWidth, DesignerHeight), paint);

        var backgroundImage = screen.BackgroundImage;
        if (backgroundImage != null)
        {
            var dest = new SKRect(0, 0, (float)PART_Canvas.ActualWidth, DesignerHeight);
            canvas.DrawBitmap(backgroundImage, dest);
        }
    }

    #endregion === RENDER SCREEN BACKGROUND ===

    #region === RENDER CONTROL SELECTION OVERLAY ===

    private void RenderControlSelectionOverlay(SKCanvas canvas, RenderContext ctx)
    {
        if (ctx.SelectedControls == null || ctx.SelectedControls.Count == 0)
            return;

        if (ctx.SelectedControls.Count == 1)
        {
            var ctrl = ctx.SelectedControls[0];

            if (
                !ctrl.ParentBand.IsExpandable
                || (ctrl.ParentBand.IsExpandable && ctrl.ParentBand.IsExpanded)
            )
            {
                ctrl.RenderFrameAndResizeHandles(canvas, ctrl.VisualRect, ctx);

                if (ctrl is ActionArea area)
                    area.RenderActionCircle(canvas, ctrl.VisualRect, ctx);
            }

            return;
        }

        foreach (var ctrl in ctx.SelectedControls)
        {
            if (ctrl == null)
                continue;

            if (ctrl.ParentBand.IsExpandable && !ctrl.ParentBand.IsExpanded)
                continue;

            RenderMultiSelectionItemOverlay(canvas, ctx, ctrl);
        }

        RenderMultiSelectionFrame(canvas, ctx.SelectedControls);
    }

    #endregion === RENDER CONTROL SELECTION OVERLAY ===

    #region === RENDER MULTISELECTION FRAME ===

    private void RenderMultiSelectionFrame(SKCanvas canvas, IEnumerable<DesignControl> controls)
    {
        var list = controls as IList<DesignControl> ?? controls.ToList();
        if (list.Count < 2)
            return;

        float left = float.MaxValue;
        float top = float.MaxValue;
        float right = float.MinValue;
        float bottom = float.MinValue;

        foreach (var ctrl in list)
        {
            var r = ctrl.VisualRect;

            if (r.IsEmpty)
                continue;

            if (ctrl.ParentBand.IsExpandable && !ctrl.ParentBand.IsExpanded)
                continue;

            left = Math.Min(left, r.Left);
            top = Math.Min(top, r.Top);
            right = Math.Max(right, r.Right);
            bottom = Math.Max(bottom, r.Bottom);
        }

        if (left == float.MaxValue)
            return;

        var frame = new SKRect(left, top, right, bottom);
        frame.Inflate(2f, 2f);

        canvas.DrawRect(frame, _multiFrameFillPaint);
        canvas.DrawRect(frame, _multiFrameBorderPaint);
    }

    #endregion === RENDER MULTISELECTION FRAME ===

    #region === RENDER RUBBERBAND SELECTION OVERLAY ===
    private void RenderRubberbandSelectionOverlay(SKCanvas canvas)
    {
        if (!_isRubberbandSelecting || _rubberbandWorldRect.IsEmpty)
            return;

        canvas.DrawRect(_rubberbandWorldRect, _rubberbandFillPaint);
        canvas.DrawRect(_rubberbandWorldRect, _rubberbandBorderPaint);
    }

    #endregion

    #region === RENDER POPUP HEADER ===

    private void RenderPopupHeaderIfNeeded(SKCanvas canvas)
    {
        if (LiveMode)
            return;

        if (DesignerKind != DesignerKind.Popup)
            return;

        if (this is not PopupDesigner popupDesigner)
            return;

        if (popupDesigner.ScreenPopup == null)
            return;

        if (!popupDesigner.ScreenPopup.HasHeader)
            return;

        float headerHeight = popupDesigner.PopupHeaderHeight;
        if (headerHeight <= 0)
            return;

        float width = popupDesigner.PopupOuterWidth;
        if (width <= 0)
            return;

        var headerRect = new SKRect(0, 0, width, headerHeight);

        using var fillPaint = new SKPaint
        {
            Color = new SKColor(246, 246, 246),
            IsAntialias = false,
            Style = SKPaintStyle.Fill,
        };

        using var linePaint = new SKPaint
        {
            Color = new SKColor(24, 0, 0, 0),
            IsAntialias = false,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
        };

        canvas.DrawRect(headerRect, fillPaint);
        canvas.DrawLine(0, headerRect.Bottom - 0.5f, width, headerRect.Bottom - 0.5f, linePaint);

        var title =
            !string.IsNullOrWhiteSpace(popupDesigner.ScreenPopup.Title)
                ? popupDesigner.ScreenPopup.Title
                : !string.IsNullOrWhiteSpace(popupDesigner.ScreenPopup.Name)
                    ? popupDesigner.ScreenPopup.Name
                    : "Popup";

        var textRect = SKRect.Create(
            headerRect.Left + 10,
            headerRect.Top + 1,
            Math.Max(0, headerRect.Width - 20),
            headerRect.Height
        );

        TextRenderer.Draw(
            canvas,
            title,
            textRect,
            15,
            color: SKColors.Black,
            textAlignment: TextAlignment.Left
        );
    }

    #endregion

    #region === LAYOUT PREPASS ===

    protected void LayoutPrepass()
    {
        if (PART_Canvas == null)
            return;

        float Round(float v) => MathF.Round(v);

        float designerW = Round((float)PART_Canvas.ActualWidth);
        float designerH = Round((float)PART_Canvas.ActualHeight);

        if (!float.IsFinite(designerW) || !float.IsFinite(designerH))
            return;

        if (designerW <= 0 || designerH <= 0)
            return;

        if (DesignerKind == DesignerKind.Popup)
        {
            var band = GetCustomBands()?.FirstOrDefault();
            var popupDesigner = this as PopupDesigner;

            if (band == null || popupDesigner == null)
                return;

            band.Width = designerW;

            float outerHeight = Round(popupDesigner.PopupOuterHeight);
            float contentHeight = Round(popupDesigner.PopupContentHeight);
            float contentTop = Round(popupDesigner.PopupContentTop);

            band.Height = popupDesigner.IsPreviewHost ? contentHeight : outerHeight;

            if (band.ActivePage != null)
                band.ActivePage.Height = contentHeight;

            float visibleContentHeight = popupDesigner.IsPreviewHost
                ? Math.Max(0, designerH)
                : Math.Max(0, designerH - contentTop);

            bool needsScroll = contentHeight > visibleContentHeight;
            float minScroll = needsScroll
                ? -Math.Max(0, Round(contentHeight - visibleContentHeight))
                : 0f;

            ScrollOffsetY = Math.Clamp(ScrollOffsetY, minScroll, 0);

            band.UpdateBandWorldBounds(0, 0);

            if (band.ActivePage != null)
            {
                float pageTop = popupDesigner.IsPreviewHost
                    ? ScrollOffsetY
                    : contentTop + ScrollOffsetY;

                band.ActivePage.UpdateWorldBounds(
                    band.WorldBounds.Left,
                    band.WorldBounds.Top + pageTop,
                    designerW
                );
            }

            return;
        }

        var bands = GetAllBands()?.ToList() ?? [];
        var headerBand = GetHeaderBand();
        var footerBand = GetFooterBand();
        var customBands = GetCustomBands()?.ToList() ?? [];

        foreach (var b in bands)
            b.Width = designerW;

        bool showHeader = Screen?.ShowHeader == true && headerBand != null;
        bool showFooter = Screen?.ShowFooter == true && footerBand != null;

        float headerH = showHeader ? Round(headerBand!.EffectiveHeight) : 0f;
        float footerH = showFooter ? Round(footerBand!.EffectiveHeight) : 0f;

        float visibleCustomArea = designerH - headerH - footerH;
        if (visibleCustomArea < 0)
            visibleCustomArea = 0;

        float naturalHeight = Round(customBands.Sum(b => b.EffectiveHeight));

        bool previewScroll = LiveMode && DesignerKind == DesignerKind.Screen;

        float minScrollY = Math.Min(0, Round(visibleCustomArea - naturalHeight));

        if (previewScroll)
            PreviewScrollOffsetY = Math.Clamp(PreviewScrollOffsetY, minScrollY, 0);
        else
            ScrollOffsetY = Math.Clamp(ScrollOffsetY, minScrollY, 0);

        if (headerBand != null)
        {
            if (showHeader)
                headerBand.UpdateBandWorldBounds(0, 0);
            else
                headerBand.UpdateBandWorldBounds(0, -10000);

            UpdateActivePageWorldBounds(headerBand);
        }

        float scrollY = previewScroll ? PreviewScrollOffsetY : ScrollOffsetY;
        float y = Round(headerH + scrollY);

        for (int i = 0; i < customBands.Count; i++)
        {
            var band = customBands[i];

            band.UpdateBandWorldBounds(0, y);
            UpdateActivePageWorldBounds(band);

            y = Round(y + band.EffectiveHeight);
        }

        if (footerBand != null)
        {
            if (showFooter)
            {
                float fy = Round(designerH - footerH + 1);
                footerBand.UpdateBandWorldBounds(0, fy);
            }
            else
            {
                footerBand.UpdateBandWorldBounds(0, -10000);
            }

            UpdateActivePageWorldBounds(footerBand);
        }
    }

    #endregion === LAYOUT PREPASS ===
}
