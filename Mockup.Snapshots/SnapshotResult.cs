// ======================================================================================
// FILE: Mockup.Snapshots/SnapshotResult.cs
//
// ZWECK:
//   Rückgabewert der Undo/Redo-Operationen.
//   Enthält das wiederhergestellte Objekt, den Snapshot-Eintrag
//   sowie Status-Informationen für Fehlerbehandlung und UI-Updates.
// ======================================================================================

namespace Mockup.Snapshots;

/// <summary>
/// Ergebnis einer Undo- oder Redo-Operation.
/// </summary>
public sealed class SnapshotResult
{
    // ─────────────────────────────────────────────────────────────
    //  Properties
    // ─────────────────────────────────────────────────────────────

    /// <summary>True, wenn die Operation erfolgreich war.</summary>
    public bool Success { get; private init; }

    /// <summary>
    /// Das wiederhergestellte Objekt (Screen, ScreenTemplate oder ScreenPopup).
    /// Nur gesetzt, wenn <see cref="Success"/> true ist.
    /// Muss von der Mockup-Library gecastet werden.
    /// </summary>
    public object? RestoredObject { get; private init; }

    /// <summary>
    /// Der Snapshot-Eintrag, der wiederhergestellt wurde.
    /// Enthält Label, TargetId, CreatedAt.
    /// </summary>
    public SnapshotEntry? Entry { get; private init; }

    /// <summary>Kontext der Operation (Screen/Template/Popup).</summary>
    public SnapshotContext Context { get; private init; }

    /// <summary>
    /// Fehlermeldung, wenn <see cref="Success"/> false ist.
    /// Leer, wenn kein Undo/Redo verfügbar war (NothingToDo).
    /// </summary>
    public string ErrorMessage { get; private init; } = string.Empty;

    /// <summary>
    /// Gibt an, ob kein Undo/Redo verfügbar war (Stack leer).
    /// </summary>
    public bool NothingToDo { get; private init; }

    // ─────────────────────────────────────────────────────────────
    //  Factory-Methoden
    // ─────────────────────────────────────────────────────────────

    internal static SnapshotResult Ok(
        SnapshotContext context,
        object restoredObject,
        SnapshotEntry entry) => new()
        {
            Success = true,
            RestoredObject = restoredObject,
            Entry = entry,
            Context = context,
            NothingToDo = false,
        };

    internal static SnapshotResult NotAvailable(SnapshotContext context) => new()
    {
        Success = false,
        Context = context,
        NothingToDo = true,
        ErrorMessage = string.Empty,
    };

    internal static SnapshotResult Failure(SnapshotContext context, string message) => new()
    {
        Success = false,
        Context = context,
        NothingToDo = false,
        ErrorMessage = message,
    };

    // ─────────────────────────────────────────────────────────────
    //  Convenience
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Gibt das wiederhergestellte Objekt gecastet zurück.
    /// Wirft InvalidCastException, wenn der Typ nicht stimmt.
    /// </summary>
    public T GetRestored<T>() where T : class
    {
        if (!Success || RestoredObject == null)
            throw new InvalidOperationException("SnapshotResult ist nicht erfolgreich — kein Objekt verfügbar.");

        return (T)RestoredObject;
    }

    /// <summary>
    /// Versucht das wiederhergestellte Objekt zu casten.
    /// Gibt false zurück, wenn nicht erfolgreich oder falscher Typ.
    /// </summary>
    public bool TryGetRestored<T>(out T? result) where T : class
    {
        if (Success && RestoredObject is T typed)
        {
            result = typed;
            return true;
        }

        result = null;
        return false;
    }

    public override string ToString()
    {
        if (NothingToDo) return $"[{Context}] NothingToDo";
        if (!Success) return $"[{Context}] FAILED: {ErrorMessage}";
        return $"[{Context}] OK: {Entry?.Label}";
    }
}
