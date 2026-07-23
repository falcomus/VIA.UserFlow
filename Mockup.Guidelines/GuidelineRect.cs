// ======================================================================================
// FILE: Mockup.Guidelines/GuidelineRect.cs
//
// ZWECK:
//   Rechteck-Datenmodell für die Guideline-Berechnung.
//   Die Library arbeitet bewusst ohne WPF-/Designer-Abhängigkeiten.
// ======================================================================================

namespace VIA.Mockup.Guidelines;

/// <summary>
/// Rechteck in logischen Designer-Koordinaten.
/// </summary>
public readonly struct GuidelineRect
{
    public GuidelineRect(long id, float x, float y, float width, float height)
    {
        Id = id;
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>ID des Ursprungselements. Bei Selection-Bounds kann 0 verwendet werden.</summary>
    public long Id { get; }

    /// <summary>Linke Position.</summary>
    public float X { get; }

    /// <summary>Obere Position.</summary>
    public float Y { get; }

    /// <summary>Breite.</summary>
    public float Width { get; }

    /// <summary>Höhe.</summary>
    public float Height { get; }

    public float Left => X;

    public float CenterX => X + Width / 2f;

    public float Right => X + Width;

    public float Top => Y;

    public float CenterY => Y + Height / 2f;

    public float Bottom => Y + Height;

    public bool IsValid => Width >= 0f && Height >= 0f;

    public float GetAnchorValue(GuidelineAnchorKind anchor) => anchor switch
    {
        GuidelineAnchorKind.Left => Left,
        GuidelineAnchorKind.CenterX => CenterX,
        GuidelineAnchorKind.Right => Right,
        GuidelineAnchorKind.Top => Top,
        GuidelineAnchorKind.CenterY => CenterY,
        GuidelineAnchorKind.Bottom => Bottom,
        _ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null),
    };
}
