// ======================================================================================
// FILE: Mockup.Guidelines/AlignmentGuidelineManager.cs
//
// ZWECK:
//   Reine Berechnungslogik für temporäre Alignment-Guides während Drag.
//   Keine WPF-Abhängigkeit, keine Designer-Abhängigkeit, keine Collection-Mutation.
// ======================================================================================

namespace VIA.Mockup.Guidelines;

/// <summary>
/// Berechnet temporäre Alignment-Guides und finale Snap-Deltas.
/// </summary>
public sealed class AlignmentGuidelineManager
{
    /// <summary>
    /// Berechnet aktive Hilfslinien und Snap-Deltas für die aktuelle Drag-Position.
    /// </summary>
    /// <param name="movingBounds">Bounds des bewegten Controls oder der gesamten Multiselect-Auswahl.</param>
    /// <param name="targetRects">Alle nicht selektierten Ziel-Controls im gleichen Designer-Kontext.</param>
    /// <param name="options">Optionale Berechnungsoptionen. Null verwendet <see cref="GuidelineOptions.Default"/>.</param>
    public GuidelineResult Evaluate(
        GuidelineRect movingBounds,
        IReadOnlyList<GuidelineRect> targetRects,
        GuidelineOptions? options = null)
    {
        return EvaluateInternal(
            movingBounds,
            targetRects,
            options,
            movingXAnchors: null,
            movingYAnchors: null,
            evaluateX: true,
            evaluateY: true);
    }

    /// <summary>
    /// Berechnet Hilfslinien für eine Control-Größenänderung.
    /// Pro Achse wird ausschließlich die vom aktiven Resize-Handle bewegte Kante bewertet.
    /// Ziel-Controls stellen weiterhin Kanten und Mitte als Snap-Ziele bereit.
    /// </summary>
    /// <param name="resizingBounds">Aktuelle Bounds des zu skalierenden Controls.</param>
    /// <param name="targetRects">Alle nicht selektierten Ziel-Controls im gleichen Designer-Kontext.</param>
    /// <param name="movingXAnchor">Aktive linke oder rechte Kante; null, wenn horizontal nicht skaliert wird.</param>
    /// <param name="movingYAnchor">Aktive obere oder untere Kante; null, wenn vertikal nicht skaliert wird.</param>
    /// <param name="options">Optionale Berechnungsoptionen.</param>
    public GuidelineResult EvaluateResize(
        GuidelineRect resizingBounds,
        IReadOnlyList<GuidelineRect> targetRects,
        GuidelineAnchorKind? movingXAnchor,
        GuidelineAnchorKind? movingYAnchor,
        GuidelineOptions? options = null)
    {
        IReadOnlyList<GuidelineAnchorKind>? movingXAnchors =
            movingXAnchor.HasValue
                ? new[] { movingXAnchor.Value }
                : null;

        IReadOnlyList<GuidelineAnchorKind>? movingYAnchors =
            movingYAnchor.HasValue
                ? new[] { movingYAnchor.Value }
                : null;

        return EvaluateInternal(
            resizingBounds,
            targetRects,
            options,
            movingXAnchors,
            movingYAnchors,
            evaluateX: movingXAnchor.HasValue,
            evaluateY: movingYAnchor.HasValue);
    }


    /// <summary>
    /// Ermittelt für eine Resize-Achse ein räumlich unabhängiges Ziel-Control
    /// mit nahezu identischer Breite beziehungsweise Höhe.
    /// Die Rückgabe ist ausschließlich ein Größen-Snap-Kandidat und erzeugt
    /// bewusst keine Positions-Hilfslinie.
    /// </summary>
    /// <param name="axis">
    /// X vergleicht Breiten, Y vergleicht Höhen.
    /// </param>
    /// <param name="resizingBounds">Aktuelle Bounds des zu skalierenden Controls.</param>
    /// <param name="targetRects">Alle nicht selektierten Ziel-Controls im gleichen Designer-Kontext.</param>
    /// <param name="options">Optionale Berechnungsoptionen.</param>
    /// <returns>Das passendste Ziel-Control oder null, wenn kein Kandidat innerhalb des Thresholds liegt.</returns>
    public GuidelineRect? FindBestResizeSizeTarget(
        GuidelineAxis axis,
        GuidelineRect resizingBounds,
        IReadOnlyList<GuidelineRect> targetRects,
        GuidelineOptions? options = null)
    {
        options ??= GuidelineOptions.Default;

        if (!resizingBounds.IsValid || targetRects == null || targetRects.Count == 0)
            return null;

        float resizingSize = axis == GuidelineAxis.X
            ? resizingBounds.Width
            : resizingBounds.Height;

        GuidelineRect? best = null;
        float bestDistance = float.MaxValue;
        const float epsilon = 0.001f;

        foreach (var target in targetRects)
        {
            if (!target.IsValid)
                continue;

            // Virtuelle Designer-/Screen-Grenzen sind reine Positionsziele.
            // Sie dürfen niemals als Größen-Snap-Ziel dienen.
            if (target.Id < 0)
                continue;

            if (target.Id != 0 && target.Id == resizingBounds.Id)
                continue;

            float targetSize = axis == GuidelineAxis.X
                ? target.Width
                : target.Height;

            float distance = Math.Abs(targetSize - resizingSize);
            if (distance > options.Threshold)
                continue;

            if (!best.HasValue
                || distance < bestDistance - epsilon
                || (Math.Abs(distance - bestDistance) <= epsilon && target.Id < best.Value.Id))
            {
                best = target;
                bestDistance = distance;
            }
        }

        return best;
    }

    private GuidelineResult EvaluateInternal(
        GuidelineRect movingBounds,
        IReadOnlyList<GuidelineRect> targetRects,
        GuidelineOptions? options,
        IReadOnlyList<GuidelineAnchorKind>? movingXAnchors,
        IReadOnlyList<GuidelineAnchorKind>? movingYAnchors,
        bool evaluateX,
        bool evaluateY)
    {
        options ??= GuidelineOptions.Default;

        if (!movingBounds.IsValid || targetRects == null || targetRects.Count == 0)
            return GuidelineResult.Empty;

        var xMatch = evaluateX
            ? FindBestMatch(
                GuidelineAxis.X,
                movingBounds,
                targetRects,
                options,
                movingXAnchors)
            : null;

        var yMatch = evaluateY
            ? FindBestMatch(
                GuidelineAxis.Y,
                movingBounds,
                targetRects,
                options,
                movingYAnchors)
            : null;

        if (!xMatch.HasValue && !yMatch.HasValue)
            return GuidelineResult.Empty;

        var lines = new List<GuidelineLine>(2);
        var targetHighlightRects = new List<GuidelineRect>(2);

        if (xMatch.HasValue)
        {
            var target = FindTarget(xMatch.Value.TargetId, targetRects);
            lines.Add(CreateLine(xMatch.Value, movingBounds, target));
            AddTargetHighlightRect(targetHighlightRects, target);
        }

        if (yMatch.HasValue)
        {
            var target = FindTarget(yMatch.Value.TargetId, targetRects);
            lines.Add(CreateLine(yMatch.Value, movingBounds, target));
            AddTargetHighlightRect(targetHighlightRects, target);
        }

        return new GuidelineResult(xMatch, yMatch, lines, targetHighlightRects);
    }

    private static GuidelineMatch? FindBestMatch(
        GuidelineAxis axis,
        GuidelineRect movingBounds,
        IReadOnlyList<GuidelineRect> targetRects,
        GuidelineOptions options,
        IReadOnlyList<GuidelineAnchorKind>? movingAnchorsOverride)
    {
        var movingAnchors = movingAnchorsOverride ?? GetAnchors(axis, options);
        var targetAnchors = GetAnchors(axis, options);

        if (movingAnchors.Count == 0 || targetAnchors.Count == 0)
            return null;

        GuidelineMatch? best = null;

        foreach (var target in targetRects)
        {
            if (!target.IsValid)
                continue;

            if (target.Id != 0 && target.Id == movingBounds.Id)
                continue;

            foreach (var movingAnchor in movingAnchors)
            {
                float movingValue = movingBounds.GetAnchorValue(movingAnchor);

                foreach (var targetAnchor in targetAnchors)
                {
                    float targetValue = target.GetAnchorValue(targetAnchor);
                    float delta = targetValue - movingValue;
                    float distance = Math.Abs(delta);

                    if (distance > options.Threshold)
                        continue;

                    var candidate = new GuidelineMatch(
                        axis,
                        movingAnchor,
                        targetAnchor,
                        movingValue,
                        targetValue,
                        delta,
                        distance,
                        target.Id);

                    if (IsBetter(candidate, best))
                        best = candidate;
                }
            }
        }

        return best;
    }

    private static IReadOnlyList<GuidelineAnchorKind> GetAnchors(GuidelineAxis axis, GuidelineOptions options)
    {
        var anchors = new List<GuidelineAnchorKind>(3);

        if (axis == GuidelineAxis.X)
        {
            if (options.IncludeEdges)
            {
                anchors.Add(GuidelineAnchorKind.Left);
                anchors.Add(GuidelineAnchorKind.Right);
            }

            if (options.IncludeCenters)
                anchors.Add(GuidelineAnchorKind.CenterX);
        }
        else
        {
            if (options.IncludeEdges)
            {
                anchors.Add(GuidelineAnchorKind.Top);
                anchors.Add(GuidelineAnchorKind.Bottom);
            }

            if (options.IncludeCenters)
                anchors.Add(GuidelineAnchorKind.CenterY);
        }

        return anchors;
    }

    private static bool IsBetter(GuidelineMatch candidate, GuidelineMatch? currentBest)
    {
        if (!currentBest.HasValue)
            return true;

        var best = currentBest.Value;
        const float epsilon = 0.001f;

        if (candidate.Distance < best.Distance - epsilon)
            return true;

        if (candidate.Distance > best.Distance + epsilon)
            return false;

        int candidatePriority = GetAnchorPriority(candidate.MovingAnchor) + GetAnchorPriority(candidate.TargetAnchor);
        int bestPriority = GetAnchorPriority(best.MovingAnchor) + GetAnchorPriority(best.TargetAnchor);

        if (candidatePriority != bestPriority)
            return candidatePriority > bestPriority;

        return candidate.TargetId < best.TargetId;
    }

    private static int GetAnchorPriority(GuidelineAnchorKind anchor) => anchor switch
    {
        GuidelineAnchorKind.Left => 3,
        GuidelineAnchorKind.Right => 3,
        GuidelineAnchorKind.Top => 3,
        GuidelineAnchorKind.Bottom => 3,
        GuidelineAnchorKind.CenterX => 1,
        GuidelineAnchorKind.CenterY => 1,
        _ => 0,
    };

    private static GuidelineLine CreateLine(
        GuidelineMatch match,
        GuidelineRect movingBounds,
        GuidelineRect target)
    {
        if (match.Axis == GuidelineAxis.X)
        {
            float start = Math.Min(movingBounds.Top, target.Top);
            float end = Math.Max(movingBounds.Bottom, target.Bottom);
            return new GuidelineLine(GuidelineAxis.X, match.TargetValue, start, end, match);
        }

        float lineStart = Math.Min(movingBounds.Left, target.Left);
        float lineEnd = Math.Max(movingBounds.Right, target.Right);
        return new GuidelineLine(GuidelineAxis.Y, match.TargetValue, lineStart, lineEnd, match);
    }

    private static GuidelineRect FindTarget(long targetId, IReadOnlyList<GuidelineRect> targetRects)
    {
        foreach (var target in targetRects)
        {
            if (target.Id == targetId)
                return target;
        }

        return targetRects.Count > 0
            ? targetRects[0]
            : new GuidelineRect(0, 0, 0, 0, 0);
    }

    private static void AddTargetHighlightRect(List<GuidelineRect> targetHighlightRects, GuidelineRect target)
    {
        // Negative IDs kennzeichnen virtuelle Ziele wie die Designer-/Screen-Grenzen.
        // Für sie werden Hilfslinien, aber keine flächigen Ziel-Highlights gezeichnet.
        if (!target.IsValid || target.Id < 0)
            return;

        foreach (var existing in targetHighlightRects)
        {
            if (existing.Id == target.Id)
                return;
        }

        targetHighlightRects.Add(target);
    }
}
