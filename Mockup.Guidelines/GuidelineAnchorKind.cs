// ======================================================================================
// FILE: Mockup.Guidelines/GuidelineAnchorKind.cs
//
// ZWECK:
//   Definiert die ausrichtbaren Ankerpunkte eines Rechtecks.
// ======================================================================================

namespace VIA.Mockup.Guidelines;

/// <summary>
/// Ausrichtbarer Anker eines Rechtecks.
/// </summary>
public enum GuidelineAnchorKind
{
    /// <summary>Linke Kante.</summary>
    Left,

    /// <summary>Horizontale Mitte.</summary>
    CenterX,

    /// <summary>Rechte Kante.</summary>
    Right,

    /// <summary>Obere Kante.</summary>
    Top,

    /// <summary>Vertikale Mitte.</summary>
    CenterY,

    /// <summary>Untere Kante.</summary>
    Bottom,
}
