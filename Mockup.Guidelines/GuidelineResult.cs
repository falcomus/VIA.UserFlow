// ======================================================================================
// FILE: Mockup.Guidelines/GuidelineResult.cs
//
// ZWECK:
//   Gesamtergebnis einer Guideline-Berechnung.
// ======================================================================================

namespace VIA.Mockup.Guidelines;

/// <summary>
/// Ergebnis der Guideline-Berechnung für einen Drag-Zustand.
/// </summary>
public sealed class GuidelineResult
{
    public static GuidelineResult Empty { get; } = new(null, null, Array.Empty<GuidelineLine>(), Array.Empty<GuidelineRect>());

    public GuidelineResult(GuidelineMatch? xMatch, GuidelineMatch? yMatch, IReadOnlyList<GuidelineLine> lines)
        : this(xMatch, yMatch, lines, Array.Empty<GuidelineRect>())
    {
    }

    public GuidelineResult(
        GuidelineMatch? xMatch,
        GuidelineMatch? yMatch,
        IReadOnlyList<GuidelineLine> lines,
        IReadOnlyList<GuidelineRect> targetHighlightRects)
    {
        XMatch = xMatch;
        YMatch = yMatch;
        Lines = lines ?? Array.Empty<GuidelineLine>();
        TargetHighlightRects = targetHighlightRects ?? Array.Empty<GuidelineRect>();
    }

    /// <summary>Snap-Treffer auf der X-Achse, erzeugt eine vertikale Linie.</summary>
    public GuidelineMatch? XMatch { get; }

    /// <summary>Snap-Treffer auf der Y-Achse, erzeugt eine horizontale Linie.</summary>
    public GuidelineMatch? YMatch { get; }

    /// <summary>Temporär zu rendernde Hilfslinien.</summary>
    public IReadOnlyList<GuidelineLine> Lines { get; }

    /// <summary>
    /// Ziel-Rechtecke, gegen die aktuell ausgerichtet wird.
    /// Der Designer kann diese Rechtecke optisch hervorheben.
    /// </summary>
    public IReadOnlyList<GuidelineRect> TargetHighlightRects { get; }

    public bool HasAnyMatch => XMatch.HasValue || YMatch.HasValue;

    public float SnapDeltaX => XMatch?.Delta ?? 0f;

    public float SnapDeltaY => YMatch?.Delta ?? 0f;
}
