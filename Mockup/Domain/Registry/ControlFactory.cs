// ======================================================================================
// FILE: Mockup.Domain/Registry/ControlFactory.cs
//
// ZWECK:
//  Kleine Hilfsklasse rund um die ControlRegistry.
//  Dient als "Factory-Fassade", damit andere Schichten (Serializer, UI)
//  nicht direkt alle Details der Registry kennen müssen.
//
// FUNKTIONALITÄTEN:
//  - Erzeugen eines DesignControls anhand des TypeKey
//  - Generische Varianten (Cast auf T)
//  - TryCreate-Helpers mit bool-Rückgabe
//
// HINWEIS:
//  - Alte Implementationen benutzten ResolveType / ControlPropSchema – die gibt
//    es im neuen System nicht mehr.
//  - ControlRegistry ist jetzt die einzige Wahrheit für TypeKeys.
// ======================================================================================

namespace Mockup.Registry;

/// <summary>
/// Komfort-Fassade rund um <see cref="ControlRegistry"/>.
/// </summary>
public static class ControlFactory
{
    /// <summary>
    /// Erzeugt ein neues <see cref="DesignControl"/> anhand eines TypeKeys.
    /// Gibt <c>null</c> zurück, wenn der Typ unbekannt ist.
    /// </summary>
    public static DesignControl? Create(string typeKey)
    {
        if (string.IsNullOrWhiteSpace(typeKey))
            return null;

        return ControlRegistry.Create(typeKey);
    }

    /// <summary>
    /// Span-Variante, falls der Aufrufer schon mit <see cref="ReadOnlySpan{Char}"/>
    /// arbeitet (z. B. bei JSON-Parsing).
    /// </summary>
    public static DesignControl? Create(ReadOnlySpan<char> typeKey)
    {
        if (typeKey.IsEmpty)
            return null;

        return ControlRegistry.Create(typeKey.ToString());
    }

    /// <summary>
    /// Erzeugt ein Control und versucht, es auf <typeparamref name="T"/> zu casten.
    /// </summary>
    public static T? Create<T>(string typeKey) where T : DesignControl
    {
        return Create(typeKey) as T;
    }

    /// <summary>
    /// Try-Variant mit bool-Resultat.
    /// </summary>
    public static bool TryCreate(string typeKey, out DesignControl? control)
    {
        control = Create(typeKey);
        return control is not null;
    }

    /// <summary>
    /// Try-Variant mit generischem Zieltyp.
    /// </summary>
    public static bool TryCreate<T>(string typeKey, out T? control) where T : DesignControl
    {
        control = Create(typeKey) as T;
        return control is not null;
    }
}
