// ======================================================================================
// FILE: Mockup.Snapshots/ISnapshotSerializer.cs
//
// ZWECK:
//   Interface, das die Mockup-Library implementieren muss, um ihre Typen
//   (Screen, Template, Popup) in JSON zu serialisieren und wiederherzustellen.
//
//   Warum ein Interface statt direkter Abhängigkeit?
//   → Mockup.Snapshots kennt keine Mockup-Typen (keine Rückwärtsreferenz).
//   → Die Mockup-Library registriert ihre eigene Implementierung beim Start.
//   → Testbar: kann mit Mock-Implementierungen getestet werden.
// ======================================================================================

namespace Mockup.Snapshots;

/// <summary>
/// Abstraktion für die Serialisierung und Deserialisierung von Snapshot-Objekten.
/// Wird von der Mockup-Library implementiert und beim <see cref="SnapshotManager"/>
/// registriert.
/// </summary>
public interface ISnapshotSerializer
{
    /// <summary>
    /// Serialisiert ein Objekt (Screen, Template oder Popup) in einen JSON-String.
    /// </summary>
    /// <param name="target">Das zu serialisierende Objekt.</param>
    /// <param name="context">Der Kontext (Screen/Template/Popup).</param>
    /// <returns>JSON-String, oder null bei Fehler.</returns>
    string? Serialize(object target, SnapshotContext context);

    /// <summary>
    /// Deserialisiert einen JSON-String zurück in ein Objekt.
    /// </summary>
    /// <param name="json">Gespeicherter JSON-String.</param>
    /// <param name="context">Kontext zur Auswahl des richtigen Typs.</param>
    /// <returns>Wiederhergestelltes Objekt, oder null bei Fehler.</returns>
    object? Deserialize(string json, SnapshotContext context);
}
