// ======================================================================================
// FILE: Mockup.Guidelines/GuidelineOptions.cs
//
// ZWECK:
//   Konfiguration für die Alignment-Guideline-Berechnung.
// ======================================================================================

namespace VIA.Mockup.Guidelines;

/// <summary>
/// Optionen für die Guideline-Berechnung.
/// </summary>
public sealed class GuidelineOptions
{
    /// <summary>
    /// Standard-Optionen für Drag-Alignment.
    /// </summary>
    public static GuidelineOptions Default { get; } = new();

    /// <summary>
    /// Maximaler Abstand in logischen Pixeln, bei dem ein Snap-Kandidat gültig ist.
    /// </summary>
    public float Threshold { get; init; } = 6f;

    /// <summary>
    /// Gibt an, ob linke/rechte/obere/untere Kanten berücksichtigt werden.
    /// </summary>
    public bool IncludeEdges { get; init; } = true;

    /// <summary>
    /// Gibt an, ob horizontale und vertikale Mitten berücksichtigt werden.
    /// </summary>
    public bool IncludeCenters { get; init; } = true;
}
