// ======================================================================================
// FILE: Mockup.Designer/BaseDesigner.Guidelines.cs
//
// ZWECK:
//   Temporäre Alignment-Guidelines während Control-Drag, -Resize und Toolbox-Drop.
//   Diese Partial-Datei hält nur UI-State, berechnet Guideline-Ergebnisse
//   und rendert Linien inklusive Ziel-Control-Hervorhebung.
//
// WICHTIG:
//   - Keine Collection-Mutation.
//   - Snap erfolgt erst final bei MouseUp.
// ======================================================================================

using Mockup.ViewModel;
using SkiaSharp;
using VIA.Mockup.Guidelines;

namespace Mockup.Designer;

public abstract partial class BaseDesigner
{
    #region === ALIGNMENT GUIDELINES STATE ===

    private readonly AlignmentGuidelineManager _alignmentGuidelineManager = new();

    private GuidelineResult _activeAlignmentGuidelines = GuidelineResult.Empty;

    // Größen-Snap-Kandidaten werden getrennt gehalten, weil sie keine gemeinsame
    // X-/Y-Position mit dem Target haben und daher keine Positionslinie erzeugen.
    private GuidelineRect? _activeResizeWidthGuidelineTarget;
    private GuidelineRect? _activeResizeHeightGuidelineTarget;
    private IReadOnlyList<GuidelineRect>? _cachedAlignmentGuidelineTargets;

    private const long DesignerBoundsGuidelineTargetId = long.MinValue;

    private static readonly GuidelineOptions _alignmentGuidelineOptions = new()
    {
        Threshold = 4f,
        IncludeEdges = true,
        IncludeCenters = true,
    };

    private static readonly SKPaint _alignmentGuidelinePaint = new()
    {
        Color = SKColors.DodgerBlue.WithAlpha(150),
        IsAntialias = false,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.5f,
        PathEffect = SKPathEffect.CreateDash([3f, 2f], 0f),
    };

    private static readonly SKPaint _alignmentTargetHighlightFillPaint = new()
    {
        Color = SKColors.DodgerBlue.WithAlpha(24),
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    private static readonly SKPaint _alignmentTargetHighlightBorderPaint = new()
    {
        Color = SKColors.DodgerBlue.WithAlpha(150),
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.25f,
    };

    #endregion === ALIGNMENT GUIDELINES STATE ===

    #region === ALIGNMENT GUIDELINES UPDATE ===

    protected void ClearAlignmentGuidelines()
    {
        if (!_activeAlignmentGuidelines.HasAnyMatch
            && _activeAlignmentGuidelines.Lines.Count == 0
            && _activeAlignmentGuidelines.TargetHighlightRects.Count == 0
            && !_activeResizeWidthGuidelineTarget.HasValue
            && !_activeResizeHeightGuidelineTarget.HasValue
            && _cachedAlignmentGuidelineTargets is null)
        {
            return;
        }

        _activeAlignmentGuidelines = GuidelineResult.Empty;
        _activeResizeWidthGuidelineTarget = null;
        _activeResizeHeightGuidelineTarget = null;
        _cachedAlignmentGuidelineTargets = null;
    }

    private void UpdateAlignmentGuidelinesDuringControlDrag(float dx, float dy)
    {
        if (VM?.SelectedControls == null || VM.SelectedControls.Count == 0)
        {
            ClearAlignmentGuidelines();
            return;
        }

        var movingBounds = CreateMovingSelectionGuidelineRect(dx, dy);
        if (!movingBounds.HasValue)
        {
            ClearAlignmentGuidelines();
            return;
        }

        var targets = GetCachedAlignmentGuidelineTargets();
        if (targets.Count == 0)
        {
            ClearAlignmentGuidelines();
            return;
        }

        _activeAlignmentGuidelines = _alignmentGuidelineManager.Evaluate(
            movingBounds.Value,
            targets,
            _alignmentGuidelineOptions);
    }

    private void UpdateAlignmentGuidelinesDuringControlResize(
        DesignControl ctrl,
        ControlResizeHandle handle)
    {
        if (ctrl == null)
        {
            ClearAlignmentGuidelines();
            return;
        }

        var resizingBounds = CreateGuidelineRect(ctrl);
        if (!resizingBounds.HasValue)
        {
            ClearAlignmentGuidelines();
            return;
        }

        var movingXAnchor = CanResizeWidth(ctrl)
            ? GetResizeGuidelineXAnchor(handle)
            : null;

        var movingYAnchor = CanResizeHeight(ctrl)
            ? GetResizeGuidelineYAnchor(handle)
            : null;

        if (!movingXAnchor.HasValue && !movingYAnchor.HasValue)
        {
            ClearAlignmentGuidelines();
            return;
        }

        var targets = GetCachedAlignmentGuidelineTargets();
        if (targets.Count == 0)
        {
            ClearAlignmentGuidelines();
            return;
        }

        var positionGuidelines = _alignmentGuidelineManager.EvaluateResize(
            resizingBounds.Value,
            targets,
            movingXAnchor,
            movingYAnchor,
            _alignmentGuidelineOptions);

        var widthTarget = movingXAnchor.HasValue
            ? _alignmentGuidelineManager.FindBestResizeSizeTarget(
                GuidelineAxis.X,
                resizingBounds.Value,
                targets,
                _alignmentGuidelineOptions)
            : null;

        var heightTarget = movingYAnchor.HasValue
            ? _alignmentGuidelineManager.FindBestResizeSizeTarget(
                GuidelineAxis.Y,
                resizingBounds.Value,
                targets,
                _alignmentGuidelineOptions)
            : null;

        bool useWidthTarget = ShouldUseResizeSizeGuideline(
            GuidelineAxis.X,
            resizingBounds.Value,
            widthTarget,
            positionGuidelines.XMatch);

        bool useHeightTarget = ShouldUseResizeSizeGuideline(
            GuidelineAxis.Y,
            resizingBounds.Value,
            heightTarget,
            positionGuidelines.YMatch);

        _activeResizeWidthGuidelineTarget = useWidthTarget ? widthTarget : null;
        _activeResizeHeightGuidelineTarget = useHeightTarget ? heightTarget : null;

        _activeAlignmentGuidelines = CreatePositionGuidelineResult(
            positionGuidelines,
            targets,
            keepXMatch: !useWidthTarget,
            keepYMatch: !useHeightTarget);
    }

    /// <summary>
    /// Aktualisiert die temporären Alignment-Guidelines für ein noch nicht persistiertes
    /// Toolbox-Control. Das Preview-Control ist nicht Teil einer Collection.
    /// </summary>
    protected void UpdateAlignmentGuidelinesDuringToolboxControlDrop(DesignControl previewCtrl)
    {
        ResolveToolboxControlDropAlignment(previewCtrl);
    }

    /// <summary>
    /// Wendet die gleiche Alignment-Auflösung wie das Toolbox-Preview auf das neu
    /// erzeugte Control an. Die Collection wird dabei nicht verändert.
    /// </summary>
    protected bool TryApplyAlignmentGuidelineSnapToToolboxControlDrop(DesignControl ctrl)
    {
        return ResolveToolboxControlDropAlignment(ctrl);
    }

    private bool ResolveToolboxControlDropAlignment(DesignControl ctrl)
    {
        var page = ctrl?.ParentBandPage;

        if (ctrl == null || page == null)
        {
            ClearAlignmentGuidelines();
            return false;
        }

        var targets = CreateToolboxDropGuidelineTargetRects(page);
        if (targets.Count == 0)
        {
            ClearAlignmentGuidelines();
            return false;
        }

        var initialBounds = CreateGuidelineRect(ctrl);
        if (!initialBounds.HasValue)
        {
            ClearAlignmentGuidelines();
            return false;
        }

        ResolveToolboxDropSizeTargets(
            ctrl,
            initialBounds.Value,
            targets,
            out var widthTarget,
            out var heightTarget);

        bool changed = ApplyToolboxDropSizeTargets(
            ctrl,
            page,
            widthTarget,
            heightTarget);

        changed |= ClampToolboxDropControlToPageBounds(ctrl, page);

        // Ein Target wird nur angezeigt und beim Drop verwendet, wenn seine
        // exakte Größe innerhalb der Control- und Page-Grenzen erreichbar ist.
        if (widthTarget.HasValue
            && Math.Abs(ctrl.Width - widthTarget.Value.Width) > 0.0001f)
        {
            widthTarget = null;
        }

        if (heightTarget.HasValue
            && Math.Abs(ctrl.Height - heightTarget.Value.Height) > 0.0001f)
        {
            heightTarget = null;
        }

        var boundsBeforePositionSnap = CreateGuidelineRect(ctrl);
        if (!boundsBeforePositionSnap.HasValue)
        {
            ClearAlignmentGuidelines();
            return changed;
        }

        var preliminaryPositionGuidelines = _alignmentGuidelineManager.Evaluate(
            boundsBeforePositionSnap.Value,
            targets,
            _alignmentGuidelineOptions);

        changed |= ApplyToolboxDropPositionSnap(
            ctrl,
            page,
            preliminaryPositionGuidelines);

        var finalBounds = CreateGuidelineRect(ctrl);
        if (!finalBounds.HasValue)
        {
            ClearAlignmentGuidelines();
            return changed;
        }

        // Nach dem Anwenden noch einmal bewerten, damit Linien und Preview exakt
        // die tatsächlich resultierende Position repräsentieren.
        _activeAlignmentGuidelines = _alignmentGuidelineManager.Evaluate(
            finalBounds.Value,
            targets,
            _alignmentGuidelineOptions);

        _activeResizeWidthGuidelineTarget = widthTarget;
        _activeResizeHeightGuidelineTarget = heightTarget;

        return changed;
    }

    private void ResolveToolboxDropSizeTargets(
        DesignControl ctrl,
        GuidelineRect movingBounds,
        IReadOnlyList<GuidelineRect> targets,
        out GuidelineRect? widthTarget,
        out GuidelineRect? heightTarget)
    {
        widthTarget = CanResizeWidth(ctrl)
            ? _alignmentGuidelineManager.FindBestResizeSizeTarget(
                GuidelineAxis.X,
                movingBounds,
                targets,
                _alignmentGuidelineOptions)
            : null;

        heightTarget = CanResizeHeight(ctrl)
            ? _alignmentGuidelineManager.FindBestResizeSizeTarget(
                GuidelineAxis.Y,
                movingBounds,
                targets,
                _alignmentGuidelineOptions)
            : null;

        if (ctrl.ResizeStyle != ResizeStyles.KeepRatio
            || !widthTarget.HasValue
            || !heightTarget.HasValue)
        {
            return;
        }

        float widthDistance = Math.Abs(widthTarget.Value.Width - movingBounds.Width);
        float heightDistance = Math.Abs(heightTarget.Value.Height - movingBounds.Height);

        // KeepRatio kann nur eine Achse exakt übernehmen. Bei Gleichstand bleibt
        // die Breite vorrangig, analog zur X-vor-Y-Auswertung der Guidelines.
        if (widthDistance <= heightDistance)
            heightTarget = null;
        else
            widthTarget = null;
    }

    private static bool ApplyToolboxDropSizeTargets(
        DesignControl ctrl,
        BandPage page,
        GuidelineRect? widthTarget,
        GuidelineRect? heightTarget)
    {
        float originalWidth = ctrl.Width;
        float originalHeight = ctrl.Height;

        float pageWidth = Math.Max(0f, page.WorldBounds.Width);
        float pageHeight = Math.Max(0f, page.WorldBounds.Height);

        if (ctrl.ResizeStyle == ResizeStyles.KeepRatio)
        {
            float ratio = ctrl.Height <= 0.0001f
                ? 1f
                : ctrl.Width / ctrl.Height;

            if (ratio > 0.0001f && float.IsFinite(ratio))
            {
                float? targetWidth = widthTarget?.Width;

                if (!targetWidth.HasValue && heightTarget.HasValue)
                    targetWidth = heightTarget.Value.Height * ratio;

                if (targetWidth.HasValue)
                {
                    float minWidth = Math.Max(ctrl.MinWidth, ctrl.MinHeight * ratio);
                    float maxWidth = Math.Min(ctrl.MaxWidth, ctrl.MaxHeight * ratio);

                    if (pageWidth > 0f)
                        maxWidth = Math.Min(maxWidth, pageWidth);

                    if (pageHeight > 0f)
                        maxWidth = Math.Min(maxWidth, pageHeight * ratio);

                    if (maxWidth > 0f)
                    {
                        minWidth = Math.Min(minWidth, maxWidth);

                        float width = Math.Clamp(targetWidth.Value, minWidth, maxWidth);
                        float height = width / ratio;

                        ctrl.Width = width;
                        ctrl.Height = height;
                    }
                }
            }
        }
        else
        {
            if (widthTarget.HasValue)
            {
                float maxWidth = pageWidth > 0f
                    ? Math.Min(ctrl.MaxWidth, pageWidth)
                    : ctrl.MaxWidth;

                if (maxWidth > 0f)
                {
                    float minWidth = Math.Min(ctrl.MinWidth, maxWidth);
                    ctrl.Width = Math.Clamp(widthTarget.Value.Width, minWidth, maxWidth);
                }
            }

            if (heightTarget.HasValue)
            {
                float maxHeight = pageHeight > 0f
                    ? Math.Min(ctrl.MaxHeight, pageHeight)
                    : ctrl.MaxHeight;

                if (maxHeight > 0f)
                {
                    float minHeight = Math.Min(ctrl.MinHeight, maxHeight);
                    ctrl.Height = Math.Clamp(heightTarget.Value.Height, minHeight, maxHeight);
                }
            }
        }

        return Math.Abs(ctrl.Width - originalWidth) > 0.0001f
            || Math.Abs(ctrl.Height - originalHeight) > 0.0001f;
    }

    private static bool ApplyToolboxDropPositionSnap(
        DesignControl ctrl,
        BandPage page,
        GuidelineResult guidelines)
    {
        float originalX = ctrl.X;
        float originalY = ctrl.Y;

        float newX = ctrl.X + guidelines.SnapDeltaX;
        float newY = ctrl.Y + guidelines.SnapDeltaY;

        float maxX = Math.Max(0f, page.WorldBounds.Width - ctrl.Width);
        float maxY = Math.Max(0f, page.WorldBounds.Height - ctrl.Height);

        ctrl.X = Math.Clamp(newX, 0f, maxX);
        ctrl.Y = Math.Clamp(newY, 0f, maxY);

        return Math.Abs(ctrl.X - originalX) > 0.0001f
            || Math.Abs(ctrl.Y - originalY) > 0.0001f;
    }

    private static bool ClampToolboxDropControlToPageBounds(
        DesignControl ctrl,
        BandPage page)
    {
        float originalX = ctrl.X;
        float originalY = ctrl.Y;

        float maxX = Math.Max(0f, page.WorldBounds.Width - ctrl.Width);
        float maxY = Math.Max(0f, page.WorldBounds.Height - ctrl.Height);

        ctrl.X = Math.Clamp(ctrl.X, 0f, maxX);
        ctrl.Y = Math.Clamp(ctrl.Y, 0f, maxY);

        return Math.Abs(ctrl.X - originalX) > 0.0001f
            || Math.Abs(ctrl.Y - originalY) > 0.0001f;
    }

    private static bool CanResizeWidth(DesignControl ctrl)
    {
        return ctrl.ResizeStyle == ResizeStyles.ResizeAll
            || ctrl.ResizeStyle == ResizeStyles.WidthOnly
            || ctrl.ResizeStyle == ResizeStyles.KeepRatio;
    }

    private static bool CanResizeHeight(DesignControl ctrl)
    {
        return ctrl.ResizeStyle == ResizeStyles.ResizeAll
            || ctrl.ResizeStyle == ResizeStyles.HeightOnly
            || ctrl.ResizeStyle == ResizeStyles.KeepRatio;
    }

    private static bool ShouldUseResizeSizeGuideline(
        GuidelineAxis axis,
        GuidelineRect resizingBounds,
        GuidelineRect? sizeTarget,
        GuidelineMatch? positionMatch)
    {
        if (!sizeTarget.HasValue)
            return false;

        float resizingSize = axis == GuidelineAxis.X
            ? resizingBounds.Width
            : resizingBounds.Height;

        float targetSize = axis == GuidelineAxis.X
            ? sizeTarget.Value.Width
            : sizeTarget.Value.Height;

        float sizeDistance = Math.Abs(targetSize - resizingSize);
        if (!positionMatch.HasValue)
            return true;

        // Bei Gleichstand bleibt der Positions-Snap vorrangig,
        // weil seine Auswirkung über die sichtbare Linie eindeutig ist.
        return sizeDistance < positionMatch.Value.Distance - 0.001f;
    }

    private static GuidelineResult CreatePositionGuidelineResult(
        GuidelineResult source,
        IReadOnlyList<GuidelineRect> targetRects,
        bool keepXMatch,
        bool keepYMatch)
    {
        GuidelineMatch? xMatch = keepXMatch ? source.XMatch : null;
        GuidelineMatch? yMatch = keepYMatch ? source.YMatch : null;

        if (!xMatch.HasValue && !yMatch.HasValue)
            return GuidelineResult.Empty;

        var lines = source.Lines
            .Where(line => line.Axis != GuidelineAxis.X || xMatch.HasValue)
            .Where(line => line.Axis != GuidelineAxis.Y || yMatch.HasValue)
            .ToArray();

        var highlights = new List<GuidelineRect>(2);
        AddPositionGuidelineTargetHighlight(highlights, xMatch, targetRects);
        AddPositionGuidelineTargetHighlight(highlights, yMatch, targetRects);

        return new GuidelineResult(xMatch, yMatch, lines, highlights);
    }

    private static void AddPositionGuidelineTargetHighlight(
        List<GuidelineRect> highlights,
        GuidelineMatch? match,
        IReadOnlyList<GuidelineRect> targetRects)
    {
        if (!match.HasValue)
            return;

        foreach (var target in targetRects)
        {
            if (target.Id != match.Value.TargetId)
                continue;

            if (highlights.Any(existing => existing.Id == target.Id))
                return;

            highlights.Add(target);
            return;
        }
    }

    private static GuidelineAnchorKind? GetResizeGuidelineXAnchor(ControlResizeHandle handle)
    {
        if (IsLeftResizeHandle(handle))
            return GuidelineAnchorKind.Left;

        if (IsRightResizeHandle(handle))
            return GuidelineAnchorKind.Right;

        return null;
    }

    private static GuidelineAnchorKind? GetResizeGuidelineYAnchor(ControlResizeHandle handle)
    {
        if (IsTopResizeHandle(handle))
            return GuidelineAnchorKind.Top;

        if (IsBottomResizeHandle(handle))
            return GuidelineAnchorKind.Bottom;

        return null;
    }

    private GuidelineRect? CreateMovingSelectionGuidelineRect(float dx, float dy)
    {
        if (_controlDragStartLocal.Count == 0)
            return null;

        float left = float.MaxValue;
        float top = float.MaxValue;
        float right = float.MinValue;
        float bottom = float.MinValue;

        foreach (var kv in _controlDragStartLocal)
        {
            var ctrl = kv.Key;
            var start = kv.Value;
            var page = ctrl.ParentBandPage;

            if (page == null)
                continue;

            float x = page.WorldBounds.Left + start.X + dx;
            float y = page.WorldBounds.Top + start.Y + dy;

            left = Math.Min(left, x);
            top = Math.Min(top, y);
            right = Math.Max(right, x + ctrl.Width);
            bottom = Math.Max(bottom, y + ctrl.Height);
        }

        if (left == float.MaxValue)
            return null;

        return new GuidelineRect(
            0,
            left,
            top,
            Math.Max(0f, right - left),
            Math.Max(0f, bottom - top));
    }

    private IReadOnlyList<GuidelineRect> CreateGuidelineTargetRects()
    {
        if (VM?.SelectedControls == null)
            return Array.Empty<GuidelineRect>();

        var result = new List<GuidelineRect>();
        var designerBounds = GetDesignerWorldBounds();

        if (designerBounds.Width > 0f && designerBounds.Height > 0f)
        {
            result.Add(new GuidelineRect(
                DesignerBoundsGuidelineTargetId,
                designerBounds.Left,
                designerBounds.Top,
                designerBounds.Width,
                designerBounds.Height));
        }
        var selectedControls = VM.SelectedControls;

        foreach (var band in GetAllBands())
        {
            if (!IsBandVisibleForGuidelines(band))
                continue;

            var page = band.ActivePage;
            if (page?.Controls == null)
                continue;

            foreach (var ctrl in page.Controls)
            {
                if (ctrl == null)
                    continue;

                if (selectedControls.Contains(ctrl))
                    continue;

                var rect = CreateGuidelineRect(ctrl);
                if (rect.HasValue)
                    result.Add(rect.Value);
            }
        }

        return result;
    }

    private IReadOnlyList<GuidelineRect> GetCachedAlignmentGuidelineTargets()
        => _cachedAlignmentGuidelineTargets ??= CreateGuidelineTargetRects();

    private static IReadOnlyList<GuidelineRect> CreateToolboxDropGuidelineTargetRects(BandPage targetPage)
    {
        if (targetPage?.Controls == null || targetPage.Controls.Count == 0)
            return Array.Empty<GuidelineRect>();

        var result = new List<GuidelineRect>(targetPage.Controls.Count);

        foreach (var ctrl in targetPage.Controls)
        {
            if (ctrl == null)
                continue;

            var rect = CreateGuidelineRect(ctrl);
            if (rect.HasValue)
                result.Add(rect.Value);
        }

        return result;
    }

    private bool IsBandVisibleForGuidelines(Band band)
    {
        if (band == null)
            return false;

        if (Screen != null)
        {
            if (band.BandType == BandType.Header && !Screen.ShowHeader)
                return false;

            if (band.BandType == BandType.Footer && !Screen.ShowFooter)
                return false;
        }

        if (band.IsExpandable && !band.IsExpanded)
            return false;

        return band.ActivePage != null;
    }

    private static GuidelineRect? CreateGuidelineRect(DesignControl ctrl)
    {
        var page = ctrl.ParentBandPage;
        if (page == null)
            return null;

        if (ctrl.Width < 0f || ctrl.Height < 0f)
            return null;

        return new GuidelineRect(
            ctrl.Id,
            page.WorldBounds.Left + ctrl.X,
            page.WorldBounds.Top + ctrl.Y,
            ctrl.Width,
            ctrl.Height);
    }
    private bool TryApplyAlignmentGuidelineSnapAfterControlDrag()
    {
        if (!_activeAlignmentGuidelines.HasAnyMatch)
            return false;

        if (VM?.SelectedControls == null || VM.SelectedControls.Count == 0)
            return false;

        float dx = _activeAlignmentGuidelines.SnapDeltaX;
        float dy = _activeAlignmentGuidelines.SnapDeltaY;

        if (Math.Abs(dx) < 0.0001f && Math.Abs(dy) < 0.0001f)
            return false;

        var startLocal = new Dictionary<DesignControl, SKPoint>();

        foreach (var ctrl in VM.SelectedControls)
        {
            if (ctrl == null)
                continue;

            startLocal[ctrl] = new SKPoint(ctrl.X, ctrl.Y);
        }

        if (startLocal.Count == 0)
            return false;

        ClampGroupDeltaToBoundsFromStart(startLocal, ref dx, ref dy);

        if (Math.Abs(dx) < 0.0001f && Math.Abs(dy) < 0.0001f)
            return false;

        foreach (var kv in startLocal)
        {
            kv.Key.X = MathF.Round(kv.Value.X + dx);
            kv.Key.Y = MathF.Round(kv.Value.Y + dy);
        }

        return true;
    }

    private bool TryApplyAlignmentGuidelineSnapAfterControlResize()
    {
        if (!_controlResizeSnapshotPushed)
            return false;

        var ctrl = _activeResizeControl;
        var page = ctrl?.ParentBandPage;

        if (ctrl == null || page == null)
            return false;

        var handle = NormalizeResizeHandleForControl(ctrl, _activeResizeHandle);
        if (handle == ControlResizeHandle.None)
            return false;

        float left = page.WorldBounds.Left + ctrl.X;
        float top = page.WorldBounds.Top + ctrl.Y;
        float right = left + ctrl.Width;
        float bottom = top + ctrl.Height;

        float originalLeft = left;
        float originalTop = top;
        float originalRight = right;
        float originalBottom = bottom;

        bool hasXPositionMatch = _activeAlignmentGuidelines.XMatch.HasValue
            && (IsLeftResizeHandle(handle) || IsRightResizeHandle(handle));

        bool hasYPositionMatch = _activeAlignmentGuidelines.YMatch.HasValue
            && (IsTopResizeHandle(handle) || IsBottomResizeHandle(handle));

        bool hasWidthSizeMatch = _activeResizeWidthGuidelineTarget.HasValue
            && (IsLeftResizeHandle(handle) || IsRightResizeHandle(handle));

        bool hasHeightSizeMatch = _activeResizeHeightGuidelineTarget.HasValue
            && (IsTopResizeHandle(handle) || IsBottomResizeHandle(handle));

        if (!hasXPositionMatch
            && !hasYPositionMatch
            && !hasWidthSizeMatch
            && !hasHeightSizeMatch)
        {
            return false;
        }

        if (ctrl.ResizeStyle == ResizeStyles.KeepRatio)
        {
            return TryApplyKeepRatioResizeGuidelineSnap(
                ctrl,
                page,
                hasXPositionMatch,
                hasYPositionMatch,
                hasWidthSizeMatch,
                hasHeightSizeMatch,
                originalLeft,
                originalTop,
                originalRight,
                originalBottom);
        }

        if (hasXPositionMatch)
        {
            float deltaX = _activeAlignmentGuidelines.SnapDeltaX;

            if (IsLeftResizeHandle(handle))
                left += deltaX;
            else if (IsRightResizeHandle(handle))
                right += deltaX;

            ApplyHorizontalResizeBounds(
                ctrl,
                page,
                handle,
                originalLeft,
                originalRight,
                ref left,
                ref right);
        }
        else if (hasWidthSizeMatch)
        {
            float targetWidth = _activeResizeWidthGuidelineTarget!.Value.Width;

            if (IsLeftResizeHandle(handle))
                left = originalRight - targetWidth;
            else if (IsRightResizeHandle(handle))
                right = originalLeft + targetWidth;

            ApplyHorizontalResizeBounds(
                ctrl,
                page,
                handle,
                originalLeft,
                originalRight,
                ref left,
                ref right);
        }

        if (hasYPositionMatch)
        {
            float deltaY = _activeAlignmentGuidelines.SnapDeltaY;

            if (IsTopResizeHandle(handle))
                top += deltaY;
            else if (IsBottomResizeHandle(handle))
                bottom += deltaY;

            ApplyVerticalResizeBounds(
                ctrl,
                page,
                handle,
                originalTop,
                originalBottom,
                ref top,
                ref bottom);
        }
        else if (hasHeightSizeMatch)
        {
            float targetHeight = _activeResizeHeightGuidelineTarget!.Value.Height;

            if (IsTopResizeHandle(handle))
                top = originalBottom - targetHeight;
            else if (IsBottomResizeHandle(handle))
                bottom = originalTop + targetHeight;

            ApplyVerticalResizeBounds(
                ctrl,
                page,
                handle,
                originalTop,
                originalBottom,
                ref top,
                ref bottom);
        }

        float snappedWidth = right - left;
        float snappedHeight = bottom - top;

        if (snappedWidth <= 0f || snappedHeight <= 0f)
            return false;

        float localX = Math.Clamp(left - page.WorldBounds.Left, 0f, Math.Max(0f, page.WorldBounds.Width - snappedWidth));
        float localY = Math.Clamp(top - page.WorldBounds.Top, 0f, Math.Max(0f, page.WorldBounds.Height - snappedHeight));

        bool changed = Math.Abs(ctrl.X - localX) > 0.0001f
            || Math.Abs(ctrl.Y - localY) > 0.0001f
            || Math.Abs(ctrl.Width - snappedWidth) > 0.0001f
            || Math.Abs(ctrl.Height - snappedHeight) > 0.0001f;

        if (!changed)
            return false;

        ctrl.X = MathF.Round(localX);
        ctrl.Y = MathF.Round(localY);
        ctrl.Width = MathF.Round(snappedWidth);
        ctrl.Height = MathF.Round(snappedHeight);

        return true;
    }

    private bool TryApplyKeepRatioResizeGuidelineSnap(
        DesignControl ctrl,
        BandPage page,
        bool hasXPositionMatch,
        bool hasYPositionMatch,
        bool hasWidthSizeMatch,
        bool hasHeightSizeMatch,
        float left,
        float top,
        float right,
        float bottom)
    {
        float ratio = _resizeStartRect.Height <= 0.0001f
            ? 1f
            : _resizeStartRect.Width / _resizeStartRect.Height;

        if (ratio <= 0.0001f || !float.IsFinite(ratio))
            return false;

        float currentWidth = right - left;
        float currentHeight = bottom - top;

        float width = 0f;
        float bestDistance = float.MaxValue;
        bool hasCandidate = false;

        void Consider(float candidateWidth, float distance)
        {
            if (candidateWidth <= 0f || !float.IsFinite(candidateWidth))
                return;

            if (!hasCandidate || distance < bestDistance - 0.001f)
            {
                width = candidateWidth;
                bestDistance = distance;
                hasCandidate = true;
            }
        }

        if (hasXPositionMatch)
        {
            Consider(
                currentWidth + _activeAlignmentGuidelines.SnapDeltaX,
                _activeAlignmentGuidelines.XMatch!.Value.Distance);
        }

        if (hasWidthSizeMatch)
        {
            float targetWidth = _activeResizeWidthGuidelineTarget!.Value.Width;
            Consider(targetWidth, Math.Abs(targetWidth - currentWidth));
        }

        if (hasYPositionMatch)
        {
            Consider(
                (currentHeight + _activeAlignmentGuidelines.SnapDeltaY) * ratio,
                _activeAlignmentGuidelines.YMatch!.Value.Distance);
        }

        if (hasHeightSizeMatch)
        {
            float targetHeight = _activeResizeHeightGuidelineTarget!.Value.Height;
            Consider(targetHeight * ratio, Math.Abs(targetHeight - currentHeight));
        }

        if (!hasCandidate)
            return false;

        float minWidth = Math.Max(ctrl.MinWidth, ctrl.MinHeight * ratio);
        float maxWidth = Math.Min(ctrl.MaxWidth, ctrl.MaxHeight * ratio);
        float maxWidthInPage = Math.Min(page.WorldBounds.Right - left, (page.WorldBounds.Bottom - top) * ratio);

        maxWidth = Math.Min(maxWidth, maxWidthInPage);
        if (maxWidth <= 0f)
            return false;

        minWidth = Math.Min(minWidth, maxWidth);
        width = Math.Clamp(width, minWidth, maxWidth);

        float height = width / ratio;

        bool changed = Math.Abs(ctrl.Width - width) > 0.0001f
            || Math.Abs(ctrl.Height - height) > 0.0001f;

        if (!changed)
            return false;

        ctrl.Width = MathF.Round(width);
        ctrl.Height = MathF.Round(height);
        return true;
    }

    private static void ApplyHorizontalResizeBounds(
        DesignControl ctrl,
        BandPage page,
        ControlResizeHandle handle,
        float originalLeft,
        float originalRight,
        ref float left,
        ref float right)
    {
        if (IsLeftResizeHandle(handle))
        {
            float width = Math.Clamp(originalRight - left, ctrl.MinWidth, ctrl.MaxWidth);
            width = Math.Min(width, Math.Max(0f, originalRight - page.WorldBounds.Left));
            left = originalRight - width;
            right = originalRight;
            return;
        }

        if (IsRightResizeHandle(handle))
        {
            float width = Math.Clamp(right - originalLeft, ctrl.MinWidth, ctrl.MaxWidth);
            width = Math.Min(width, Math.Max(0f, page.WorldBounds.Right - originalLeft));
            left = originalLeft;
            right = originalLeft + width;
        }
    }

    private static void ApplyVerticalResizeBounds(
        DesignControl ctrl,
        BandPage page,
        ControlResizeHandle handle,
        float originalTop,
        float originalBottom,
        ref float top,
        ref float bottom)
    {
        if (IsTopResizeHandle(handle))
        {
            float height = Math.Clamp(originalBottom - top, ctrl.MinHeight, ctrl.MaxHeight);
            height = Math.Min(height, Math.Max(0f, originalBottom - page.WorldBounds.Top));
            top = originalBottom - height;
            bottom = originalBottom;
            return;
        }

        if (IsBottomResizeHandle(handle))
        {
            float height = Math.Clamp(bottom - originalTop, ctrl.MinHeight, ctrl.MaxHeight);
            height = Math.Min(height, Math.Max(0f, page.WorldBounds.Bottom - originalTop));
            top = originalTop;
            bottom = originalTop + height;
        }
    }


    #endregion === ALIGNMENT GUIDELINES UPDATE ===

    #region === ALIGNMENT GUIDELINES RENDER ===

    private void RenderAlignmentGuidelines(SKCanvas canvas)
    {
        if (canvas == null)
            return;

        if (_activeAlignmentGuidelines.Lines.Count == 0
            && _activeAlignmentGuidelines.TargetHighlightRects.Count == 0
            && !_activeResizeWidthGuidelineTarget.HasValue
            && !_activeResizeHeightGuidelineTarget.HasValue)
        {
            return;
        }

        float width = DesignerWidth;
        float height = DesignerHeight;

        if (width <= 0f || height <= 0f)
            return;

        RenderAlignmentGuidelineTargetHighlights(canvas);
        RenderAlignmentGuidelineLines(canvas, width, height);
    }

    private void RenderAlignmentGuidelineTargetHighlights(SKCanvas canvas)
    {
        foreach (var rect in _activeAlignmentGuidelines.TargetHighlightRects)
            RenderAlignmentGuidelineTargetHighlight(canvas, rect);

        if (_activeResizeWidthGuidelineTarget.HasValue
            && !IsPositionGuidelineTarget(_activeResizeWidthGuidelineTarget.Value))
        {
            RenderAlignmentGuidelineTargetHighlight(canvas, _activeResizeWidthGuidelineTarget.Value);
        }

        if (_activeResizeHeightGuidelineTarget.HasValue
            && !IsPositionGuidelineTarget(_activeResizeHeightGuidelineTarget.Value)
            && (!_activeResizeWidthGuidelineTarget.HasValue
                || _activeResizeHeightGuidelineTarget.Value.Id != _activeResizeWidthGuidelineTarget.Value.Id))
        {
            RenderAlignmentGuidelineTargetHighlight(canvas, _activeResizeHeightGuidelineTarget.Value);
        }
    }

    private bool IsPositionGuidelineTarget(GuidelineRect target)
    {
        foreach (var existing in _activeAlignmentGuidelines.TargetHighlightRects)
        {
            if (existing.Id == target.Id)
                return true;
        }

        return false;
    }

    private static void RenderAlignmentGuidelineTargetHighlight(SKCanvas canvas, GuidelineRect rect)
    {
        if (!rect.IsValid)
            return;

        if (!float.IsFinite(rect.Left)
            || !float.IsFinite(rect.Top)
            || !float.IsFinite(rect.Width)
            || !float.IsFinite(rect.Height))
        {
            return;
        }

        var skRect = SKRect.Create(
            MathF.Round(rect.Left),
            MathF.Round(rect.Top),
            MathF.Round(rect.Width),
            MathF.Round(rect.Height));

        if (skRect.Width <= 0f || skRect.Height <= 0f)
            return;

        canvas.DrawRoundRect(skRect, 3f, 3f, _alignmentTargetHighlightFillPaint);
        canvas.DrawRoundRect(skRect, 3f, 3f, _alignmentTargetHighlightBorderPaint);
    }

    private void RenderAlignmentGuidelineLines(SKCanvas canvas, float width, float height)
    {
        foreach (var line in _activeAlignmentGuidelines.Lines)
        {
            if (!float.IsFinite(line.Position)
                || !float.IsFinite(line.Start)
                || !float.IsFinite(line.End))
            {
                continue;
            }

            float position = MathF.Round(line.Position) + 0.5f;
            float start = Math.Min(line.Start, line.End);
            float end = Math.Max(line.Start, line.End);

            if (line.Axis == GuidelineAxis.X)
            {
                start = Math.Clamp(start, 0f, height);
                end = Math.Clamp(end, 0f, height);

                if (end <= start)
                    continue;

                canvas.DrawLine(position, start, position, end, _alignmentGuidelinePaint);
            }
            else
            {
                start = Math.Clamp(start, 0f, width);
                end = Math.Clamp(end, 0f, width);

                if (end <= start)
                    continue;

                canvas.DrawLine(start, position, end, position, _alignmentGuidelinePaint);
            }
        }
    }

    #endregion === ALIGNMENT GUIDELINES RENDER ===
}