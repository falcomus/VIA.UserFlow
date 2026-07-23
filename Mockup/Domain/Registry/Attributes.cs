// ======================================================================================
// FILE: Mockup/ControlTypeAttribute.cs
// 
// ZWECK: 
// Attribute zur Metadaten-Deklaration für designierbare Controls (Unterklassen von DesignControl).
// Diese Metadaten werden von der Registry verwendet, um stabile Type-Keys und UI-Informationen zu erstellen.
//
// FUNKTIONALITÄTEN:
// - Stabile Type-Keys für JSON-Persistierung
// - UI-Metadaten für Toolbox-Anzeige und Gruppierung
// - Steuerung der Sichtbarkeit in Toolbox und Pickern
// - Serialisierbare Design-Properties für Controls
//
// AUTOR: Claus Falkenstein
// VERSION: 1.0
// ======================================================================================

namespace Mockup.Registry;

/// <summary>
/// Deklariert Metadaten für ein designierbares Control (eine Unterklasse von <see cref="DesignControl"/>).
/// Diese Metadaten werden von der Registry verwendet, um einen stabilen Type-Key und UI-Informationen zu erstellen.
/// 
/// Design-Hinweise:
/// - <see cref="Key"/> sollte stabil und eindeutig sein. Wird in JSON persistiert!
/// - Wenn weggelassen, fällt die Registry auf den CLR-Typnamen zurück (weniger portabel).
/// - <see cref="HiddenInToolbox"/> ermöglicht es der UI, ein Control in der Toolbox auszublenden
///   (z.B. TemplateRef) ohne Load/Save zu brechen.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ControlTypeAttribute : Attribute
{
    /// <summary>
    /// Stabiler, eindeutiger Type-Key (wird in JSON persistiert). Wenn leer, verwendet die Registry
    /// den CLR-Typnamen. Bevorzuge einen expliziten, versionsstabilen Key (z.B. "button").
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Menschenlesbarer Anzeigename für UIs (Toolbox, Picker, etc.). Optional.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Logische Gruppe in UIs (z.B. "Basic", "Inputs", "Layout"). Optional.
    /// </summary>
    public string Group { get; }

    /// <summary>
    /// Wenn true, bleibt das Control registriert (kann aus JSON erstellt/geladen werden)
    /// sollte aber standardmäßig in Toolbox/Pickern versteckt werden.
    /// </summary>
    public bool HiddenInToolbox { get; }

    /// <summary>
    /// Relativer Pfad zum kuratierten Toolbox-Vorschaubild. Optional.
    /// Der Pfad wird relativ zum zentralen Control-Preview-Ordner aufgelöst.
    /// </summary>
    public string PreviewImage { get; }

    /// <summary>
    /// Initialisiert eine neue Instanz des ControlTypeAttribute
    /// </summary>
    /// <param name="key">Stabiler, eindeutiger Type-Key</param>
    /// <param name="displayName">Menschenlesbarer Anzeigename</param>
    /// <param name="group">Logische Gruppe für UI</param>
    /// <param name="hiddenInToolbox">In Toolbox verstecken</param>
    /// <param name="previewImage">Relativer Pfad zum Toolbox-Vorschaubild</param>
    public ControlTypeAttribute(
        string key = "",
        string displayName = "",
        string group = "",
        bool hiddenInToolbox = false,
        string previewImage = "")
    {
        Key = key;
        DisplayName = displayName;
        Group = group;
        HiddenInToolbox = hiddenInToolbox;
        PreviewImage = previewImage;
    }
}

/// <summary>
/// Markiert eine Property auf einem <see cref="DesignControl"/> als serialisierbare "Design-Property".
/// Die Registry baut ein Schema aus diesen Properties und verwendet es zum Lesen/Schreiben von JSON.
/// 
/// Hinweise:
/// - <see cref="Key"/> ermöglicht das Überschreiben des JSON-Property-Namens; wenn null, wird der CLR-Name verwendet.
/// - Attribut wird vererbt, sodass abgeleitete Controls Basis-Control-Properties wiederverwenden können.
/// - Tatsächliche Getter/Setter-Delegates werden vom Schema-Cache aufgelöst (nicht hier).
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class ControlPropAttribute : Attribute
{
    /// <summary>
    /// Optionaler JSON-Key-Override. Wenn null, wird der CLR-Property-Name verwendet.
    /// </summary>
    public string? Key { get; }

    /// <summary>
    /// Initialisiert eine neue Instanz des ControlPropAttribute
    /// </summary>
    /// <param name="key">Optionaler JSON-Property-Name</param>
    public ControlPropAttribute(string? key = null) => Key = key;
}


