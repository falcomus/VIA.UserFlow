// ======================================================================================
// FILE: Mockup.Snapshots/SnapshotManager.cs
//
// ZWECK:
//   Zentrale Fassade der Mockup.Snapshots-Library.
//   Verwaltet je einen SnapshotStack pro Kontext (Project, Screen, Templates, Template, Popup)
//   und stellt die vollständige Undo/Redo-API bereit.
//
// USAGE IN MOCKUP-LIBRARY:
//
//   // 1) Einmalig beim App-Start registrieren:
//   SnapshotManager.Initialize(mySerializer, maxHistory: 50);
//
//   // 2) Vor jeder Mutation einen Snapshot pushen:
//   SnapshotManager.Push(currentScreen, SnapshotContext.Screen, "Control verschoben");
//
//   // 3) Undo ausführen:
//   var result = SnapshotManager.Undo(currentScreen, SnapshotContext.Screen);
//   if (result.Success) ReplaceScreen(result.RestoredObject as Screen);
//
//   // 4) Redo ausführen:
//   var result = SnapshotManager.Redo(currentScreen, SnapshotContext.Screen);
//   if (result.Success) ReplaceScreen(result.RestoredObject as Screen);
//
//   // 5) Stack leeren (z.B. bei Projekt-Wechsel):
//   SnapshotManager.Clear(SnapshotContext.Screen);
//   SnapshotManager.ClearAll();
// ======================================================================================

namespace Mockup.Snapshots;

/// <summary>
/// Zentrale Fassade der Undo/Redo-Engine.
/// Wird einmalig beim App-Start initialisiert und danach projektübergreifend genutzt.
/// </summary>
public static class SnapshotManager
{
    // A restored object is exactly represented by its source entry until the next
    // mutation. ConditionalWeakTable keeps this optimisation from extending the
    // lifetime of designer objects.
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, SnapshotEntry>
        _restoredObjectEntries = new();
    // ─────────────────────────────────────────────────────────────
    //  Stacks pro Kontext
    // ─────────────────────────────────────────────────────────────

    private static readonly SnapshotStack _projectStack = new();
    private static readonly SnapshotStack _screenStack = new();
    private static readonly SnapshotStack _templatesStack = new();
    private static readonly SnapshotStack _templateStack = new();
    private static readonly SnapshotStack _popupStack = new();

    // ─────────────────────────────────────────────────────────────
    //  Serializer (wird von Mockup-Library gesetzt)
    // ─────────────────────────────────────────────────────────────

    private static ISnapshotSerializer? _serializer;

    // ─────────────────────────────────────────────────────────────
    //  Initialisierung
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialisiert den SnapshotManager.
    /// Muss einmalig beim App-Start aufgerufen werden, bevor Push/Undo/Redo genutzt werden.
    /// </summary>
    /// <param name="serializer">
    ///     Die Serializer-Implementierung der Mockup-Library
    ///     (kennt Screen, Template, Popup und die JsonOptions).
    /// </param>
    /// <param name="maxHistory">
    ///     Maximale Anzahl an Undo-Schritten pro Kontext. Standard: 50.
    /// </param>
    public static void Initialize(ISnapshotSerializer serializer, int maxHistory = 20)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

        _projectStack.MaxHistory = maxHistory;
        _screenStack.MaxHistory = maxHistory;
        _templatesStack.MaxHistory = maxHistory;
        _templateStack.MaxHistory = maxHistory;
        _popupStack.MaxHistory = maxHistory;
    }

    // ─────────────────────────────────────────────────────────────
    //  Status-Properties
    // ─────────────────────────────────────────────────────────────

    /// <summary>Ob im Project-Kontext ein Undo verfügbar ist.</summary>
    public static bool CanUndoProject => _projectStack.CanUndo;

    /// <summary>Ob im Screen-Kontext ein Undo verfügbar ist.</summary>
    public static bool CanUndoScreen => _screenStack.CanUndo;

    /// <summary>Ob im Templates-Kontext ein Undo verfügbar ist.</summary>
    public static bool CanUndoTemplates => _templatesStack.CanUndo;

    /// <summary>Ob im Template-Kontext ein Undo verfügbar ist.</summary>
    public static bool CanUndoTemplate => _templateStack.CanUndo;

    /// <summary>Ob im Popup-Kontext ein Undo verfügbar ist.</summary>
    public static bool CanUndoPopup => _popupStack.CanUndo;

    /// <summary>Ob im Project-Kontext ein Redo verfügbar ist.</summary>
    public static bool CanRedoProject => _projectStack.CanRedo;

    /// <summary>Ob im Screen-Kontext ein Redo verfügbar ist.</summary>
    public static bool CanRedoScreen => _screenStack.CanRedo;

    /// <summary>Ob im Templates-Kontext ein Redo verfügbar ist.</summary>
    public static bool CanRedoTemplates => _templatesStack.CanRedo;

    /// <summary>Ob im Template-Kontext ein Redo verfügbar ist.</summary>
    public static bool CanRedoTemplate => _templateStack.CanRedo;

    /// <summary>Ob im Popup-Kontext ein Redo verfügbar ist.</summary>
    public static bool CanRedoPopup => _popupStack.CanRedo;

    /// <summary>
    /// Gibt an, ob für den angegebenen Kontext ein Undo verfügbar ist.
    /// </summary>
    public static bool CanUndo(SnapshotContext context) => GetStack(context).CanUndo;

    /// <summary>
    /// Gibt an, ob für den angegebenen Kontext ein Redo verfügbar ist.
    /// </summary>
    public static bool CanRedo(SnapshotContext context) => GetStack(context).CanRedo;

    /// <summary>
    /// Anzahl der Undo-Einträge im angegebenen Kontext.
    /// </summary>
    public static int GetUndoCount(SnapshotContext context) => GetStack(context).UndoCount;

    /// <summary>
    /// Anzahl der Redo-Einträge im angegebenen Kontext.
    /// </summary>
    public static int GetRedoCount(SnapshotContext context) => GetStack(context).RedoCount;

    /// <summary>
    /// Gesamtgröße von Undo + Redo im angegebenen Kontext in UTF-8-Bytes.
    /// </summary>
    public static long GetTotalBytes(SnapshotContext context) => GetStack(context).TotalBytes;

    /// <summary>
    /// Gesamtgröße von Undo + Redo im angegebenen Kontext in Kilobytes.
    /// </summary>
    public static double GetTotalKilobytes(SnapshotContext context) => GetStack(context).TotalKilobytes;

    /// <summary>
    /// Alias für bestehende Aufrufer: Anzahl der Undo-Einträge im angegebenen Kontext.
    /// </summary>
    public static int UndoCount(SnapshotContext context) => GetUndoCount(context);

    /// <summary>
    /// Alias für bestehende Aufrufer: Anzahl der Redo-Einträge im angegebenen Kontext.
    /// </summary>
    public static int RedoCount(SnapshotContext context) => GetRedoCount(context);

    /// <summary>
    /// Alias für bestehende Aufrufer: Gesamtgröße in UTF-8-Bytes.
    /// </summary>
    public static long TotalBytes(SnapshotContext context) => GetTotalBytes(context);

    /// <summary>
    /// Alias für bestehende Aufrufer: Gesamtgröße in Kilobytes.
    /// </summary>
    public static double TotalKilobytes(SnapshotContext context) => GetTotalKilobytes(context);

    /// <summary>
    /// Kompatibilitätsalias: Gesamtgröße in UTF-8-Bytes.
    /// </summary>
    public static long TotalUtf8Bytes(SnapshotContext context) => GetTotalBytes(context);

    /// <summary>
    /// Formatiert eine Byte-Anzahl für die UI.
    /// </summary>
    public static string FormatSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";

        double kb = bytes / 1024.0;
        if (kb < 1024)
            return $"{kb:0.#} KB";

        double mb = kb / 1024.0;
        return $"{mb:0.##} MB";
    }

    /// <summary>
    /// Beschriftung der nächsten Undo-Aktion im angegebenen Kontext (für Menü-Anzeige).
    /// </summary>
    public static string? NextUndoLabel(SnapshotContext context) => GetStack(context).NextUndoLabel;

    /// <summary>
    /// Beschriftung der nächsten Redo-Aktion im angegebenen Kontext (für Menü-Anzeige).
    /// </summary>
    public static string? NextRedoLabel(SnapshotContext context) => GetStack(context).NextRedoLabel;

    // ─────────────────────────────────────────────────────────────
    //  Haupt-API: Push / Undo / Redo
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Erzeugt einen Snapshot des aktuellen Zustands und legt ihn auf den Undo-Stack.
    /// Muss VOR jeder Mutation aufgerufen werden.
    /// </summary>
    /// <param name="target">
    ///     Das zu snapshottende Objekt (Screen, ScreenTemplate oder ScreenPopup).
    /// </param>
    /// <param name="context">Kontext des Snapshots.</param>
    /// <param name="label">
    ///     Beschreibung der Aktion, die gleich danach folgt
    ///     (z.B. "Control verschoben", "Band gelöscht").
    /// </param>
    /// <param name="targetId">ID des Zielobjekts (Screen.Id, Template.Id, Popup.Id).</param>
    /// <returns>True, wenn der Snapshot erfolgreich gespeichert wurde.</returns>
    public static bool Push(object target, SnapshotContext context, string label, long targetId)
    {
        EnsureInitialized();

        // A new user action changes the object after this snapshot is taken, so a
        // previously remembered restore payload may no longer represent it.
        _restoredObjectEntries.Remove(target);

        var json = _serializer!.Serialize(target, context);
        if (string.IsNullOrEmpty(json))
            return false;

        var entry = SnapshotEntry.FromJson(
            json,
            label,
            context,
            targetId);

        GetStack(context).Push(entry);
        return true;
    }

    /// <summary>
    /// Führt Undo aus.
    /// Der aktuelle Zustand wird auf den Redo-Stack gelegt,
    /// der letzte Undo-Eintrag wird deserialisiert und zurückgegeben.
    /// </summary>
    /// <param name="currentTarget">
    ///     Das aktuell angezeigte Objekt (wird für Redo gesichert).
    /// </param>
    /// <param name="context">Der Kontext (Screen/Template/Popup).</param>
    /// <param name="currentLabel">
    ///     Beschreibung des aktuellen Zustands (für den Redo-Eintrag).
    /// </param>
    /// <param name="currentTargetId">ID des aktuellen Zielobjekts.</param>
    /// <returns>Ergebnis mit dem wiederhergestellten Objekt.</returns>
    public static SnapshotResult Undo(
        object currentTarget,
        SnapshotContext context,
        string currentLabel,
        long currentTargetId)
    {
        EnsureInitialized();

        var stack = GetStack(context);
        if (!stack.CanUndo)
            return SnapshotResult.NotAvailable(context);

        var currentEntry = CreateCurrentStateEntry(
            currentTarget,
            context,
            currentLabel,
            currentTargetId);
        if (currentEntry == null)
            return SnapshotResult.Failure(context, "Aktueller Zustand konnte nicht serialisiert werden.");

        var undoEntry = stack.PopUndo(currentEntry);
        if (undoEntry == null)
            return SnapshotResult.NotAvailable(context);

        // Deserialisieren
        var restored = _serializer.Deserialize(undoEntry.Json, context);
        if (restored == null)
            return SnapshotResult.Failure(context, "Snapshot konnte nicht deserialisiert werden.");

        RememberRestoredObject(restored, undoEntry);

        return SnapshotResult.Ok(context, restored, undoEntry);
    }

    /// <summary>
    /// Führt Redo aus.
    /// Der aktuelle Zustand wird auf den Undo-Stack gelegt,
    /// der letzte Redo-Eintrag wird deserialisiert und zurückgegeben.
    /// </summary>
    /// <param name="currentTarget">Das aktuell angezeigte Objekt (wird für Undo gesichert).</param>
    /// <param name="context">Der Kontext (Screen/Template/Popup).</param>
    /// <param name="currentLabel">Beschreibung des aktuellen Zustands.</param>
    /// <param name="currentTargetId">ID des aktuellen Zielobjekts.</param>
    /// <returns>Ergebnis mit dem wiederhergestellten Objekt.</returns>
    public static SnapshotResult Redo(
        object currentTarget,
        SnapshotContext context,
        string currentLabel,
        long currentTargetId)
    {
        EnsureInitialized();

        var stack = GetStack(context);
        if (!stack.CanRedo)
            return SnapshotResult.NotAvailable(context);

        var currentEntry = CreateCurrentStateEntry(
            currentTarget,
            context,
            currentLabel,
            currentTargetId);
        if (currentEntry == null)
            return SnapshotResult.Failure(context, "Aktueller Zustand konnte nicht serialisiert werden.");

        var redoEntry = stack.PopRedo(currentEntry);
        if (redoEntry == null)
            return SnapshotResult.NotAvailable(context);

        var restored = _serializer.Deserialize(redoEntry.Json, context);
        if (restored == null)
            return SnapshotResult.Failure(context, "Snapshot konnte nicht deserialisiert werden.");

        RememberRestoredObject(restored, redoEntry);

        return SnapshotResult.Ok(context, restored, redoEntry);
    }

    private static SnapshotEntry? CreateCurrentStateEntry(
        object currentTarget,
        SnapshotContext context,
        string label,
        long targetId)
    {
        if (_restoredObjectEntries.TryGetValue(currentTarget, out var restoredEntry)
            && restoredEntry.Context == context
            && restoredEntry.TargetId == targetId)
        {
            return restoredEntry.CreateHistoryCopy(label, context, targetId);
        }

        var currentJson = _serializer!.Serialize(currentTarget, context);
        return string.IsNullOrEmpty(currentJson)
            ? null
            : SnapshotEntry.FromJson(currentJson, label, context, targetId);
    }

    private static void RememberRestoredObject(object restoredObject, SnapshotEntry sourceEntry)
    {
        _restoredObjectEntries.Remove(restoredObject);
        _restoredObjectEntries.Add(restoredObject, sourceEntry);
    }

    // ─────────────────────────────────────────────────────────────
    //  Stack-Verwaltung
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Leert den Stack des angegebenen Kontexts.
    /// Aufrufen bei: Wechsel des aktuellen Screens/Templates/Popups,
    /// Laden eines neuen Projekts.
    /// </summary>
    public static void Clear(SnapshotContext context) => GetStack(context).Clear();

    /// <summary>
    /// Leert alle Stacks (Project, Screen, Templates, Template, Popup).
    /// Aufrufen bei: Laden eines neuen Projekts, App-Reset.
    /// </summary>
    public static void ClearAll()
    {
        _projectStack.Clear();
        _screenStack.Clear();
        _templatesStack.Clear();
        _templateStack.Clear();
        _popupStack.Clear();
    }

    /// <summary>
    /// Gibt den Stack für den angegebenen Kontext zurück.
    /// </summary>
    public static SnapshotStack GetStack(SnapshotContext context) => context switch
    {
        SnapshotContext.Project => _projectStack,
        SnapshotContext.Screen => _screenStack,
        SnapshotContext.Templates => _templatesStack,
        SnapshotContext.Template => _templateStack,
        SnapshotContext.Popup => _popupStack,
        _ => throw new ArgumentOutOfRangeException(nameof(context)),
    };

    // ─────────────────────────────────────────────────────────────
    //  Guard
    // ─────────────────────────────────────────────────────────────

    private static void EnsureInitialized()
    {
        if (_serializer == null)
            throw new InvalidOperationException(
                "SnapshotManager ist nicht initialisiert. " +
                "Rufe SnapshotManager.Initialize(serializer) beim App-Start auf.");
    }
}
