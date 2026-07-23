// ======================================================================================
// FILE: Mockup.Guidelines/GuidelineMatch.cs
//
// ZWECK:
//   Beschreibt einen ausgewählten Snap-Treffer auf einer Achse.
// ======================================================================================

namespace VIA.Mockup.Guidelines;

/// <summary>
/// Ergebnis eines Snap-Treffers auf einer Achse.
/// </summary>
public readonly struct GuidelineMatch
{
    public GuidelineMatch(
        GuidelineAxis axis,
        GuidelineAnchorKind movingAnchor,
        GuidelineAnchorKind targetAnchor,
        float movingValue,
        float targetValue,
        float delta,
        float distance,
        long targetId)
    {
        Axis = axis;
        MovingAnchor = movingAnchor;
        TargetAnchor = targetAnchor;
        MovingValue = movingValue;
        TargetValue = targetValue;
        Delta = delta;
        Distance = distance;
        TargetId = targetId;
    }

    public GuidelineAxis Axis { get; }

    public GuidelineAnchorKind MovingAnchor { get; }

    public GuidelineAnchorKind TargetAnchor { get; }

    public float MovingValue { get; }

    public float TargetValue { get; }

    /// <summary>
    /// Der auf die bewegte Selection anzuwendende Delta-Wert.
    /// </summary>
    public float Delta { get; }

    /// <summary>
    /// Absoluter Abstand zwischen bewegtem Anker und Zielanker.
    /// </summary>
    public float Distance { get; }

    /// <summary>
    /// ID des Ziel-Controls, gegen das ausgerichtet wird.
    /// </summary>
    public long TargetId { get; }
}
