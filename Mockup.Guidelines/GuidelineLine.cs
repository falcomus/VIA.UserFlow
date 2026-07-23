// ======================================================================================
// FILE: Mockup.Guidelines/GuidelineLine.cs
//
// ZWECK:
//   Beschreibt eine temporär zu rendernde Hilfslinie.
//   Die tatsächliche Darstellung bleibt Aufgabe des Designers.
// ======================================================================================

namespace VIA.Mockup.Guidelines;

/// <summary>
/// Temporäre Alignment-Hilfslinie.
/// </summary>
public readonly struct GuidelineLine
{
    public GuidelineLine(GuidelineAxis axis, float position, float start, float end, GuidelineMatch match)
    {
        Axis = axis;
        Position = position;
        Start = start;
        End = end;
        Match = match;
    }

    /// <summary>X bedeutet vertikale Linie, Y bedeutet horizontale Linie.</summary>
    public GuidelineAxis Axis { get; }

    /// <summary>X- oder Y-Position der Linie.</summary>
    public float Position { get; }

    /// <summary>Startwert auf der Gegenachse.</summary>
    public float Start { get; }

    /// <summary>Endwert auf der Gegenachse.</summary>
    public float End { get; }

    /// <summary>Der fachliche Snap-Treffer zu dieser Linie.</summary>
    public GuidelineMatch Match { get; }
}
