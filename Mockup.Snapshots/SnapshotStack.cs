// ======================================================================================
// FILE: Mockup.Snapshots/SnapshotStack.cs
//
// ZWECK:
//   Kern-Engine des Undo/Redo-Systems. Verwaltet zwei Stacks (Undo/Redo)
//   und kapselt die gesamte Zustandsverwaltung.
//
// DESIGN-ENTSCHEIDUNGEN:
//   - Generisch über ISnapshotTarget<T>: funktioniert für Screen, Template, Popup
//   - MaxHistory begrenzt Speicherverbrauch (Default: 50 Einträge)
//   - Redo-Stack wird bei jeder neuen Aktion geleert (Standard-Verhalten)
//   - Thread-safe über lock (UI-Thread-Only wäre ausreichend, aber sicherer so)
// ======================================================================================

namespace Mockup.Snapshots;

/// <summary>
/// Generischer Undo/Redo-Stack für einen einzelnen Designer-Kontext.
/// Verwaltet Snapshots als <see cref="SnapshotEntry"/>-Objekte.
/// </summary>
public sealed class SnapshotStack
{
    // ─────────────────────────────────────────────────────────────
    //  Felder
    // ─────────────────────────────────────────────────────────────

    private readonly Stack<SnapshotEntry> _undoStack = new();
    private readonly Stack<SnapshotEntry> _redoStack = new();
    private readonly Dictionary<SnapshotEntry, long> _entryBytes = new();
    private readonly object _lock = new();

    private long _undoBytes;
    private long _redoBytes;

    // ─────────────────────────────────────────────────────────────
    //  Konfiguration
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Maximale Anzahl an Undo-Einträgen. Ältere werden verworfen.
    /// Standard: 50.
    /// </summary>
    public int MaxHistory { get; set; } = 50;

    // ─────────────────────────────────────────────────────────────
    //  Status-Properties
    // ─────────────────────────────────────────────────────────────

    /// <summary>Gibt an, ob ein Undo möglich ist.</summary>
    public bool CanUndo
    {
        get { lock (_lock) return _undoStack.Count > 0; }
    }

    /// <summary>Gibt an, ob ein Redo möglich ist.</summary>
    public bool CanRedo
    {
        get { lock (_lock) return _redoStack.Count > 0; }
    }

    /// <summary>Anzahl der Einträge im Undo-Stack.</summary>
    public int UndoCount
    {
        get { lock (_lock) return _undoStack.Count; }
    }

    /// <summary>Anzahl der Einträge im Redo-Stack.</summary>
    public int RedoCount
    {
        get { lock (_lock) return _redoStack.Count; }
    }

    /// <summary>
    /// Größe der Undo-Einträge in UTF-8-Bytes.
    /// Für UI/Statusanzeige, nicht für Persistenzlogik.
    /// </summary>
    public long UndoBytes
    {
        get { lock (_lock) return _undoBytes; }
    }

    /// <summary>
    /// Größe der Redo-Einträge in UTF-8-Bytes.
    /// Für UI/Statusanzeige, nicht für Persistenzlogik.
    /// </summary>
    public long RedoBytes
    {
        get { lock (_lock) return _redoBytes; }
    }

    /// <summary>
    /// Gesamtgröße von Undo + Redo in UTF-8-Bytes.
    /// </summary>
    public long TotalBytes
    {
        get { lock (_lock) return _undoBytes + _redoBytes; }
    }

    /// <summary>
    /// Kompatibilitätsalias: Gesamtgröße von Undo + Redo in UTF-8-Bytes.
    /// </summary>
    public long TotalUtf8Bytes
    {
        get { lock (_lock) return _undoBytes + _redoBytes; }
    }

    /// <summary>
    /// Gesamtgröße von Undo + Redo in Kilobytes.
    /// </summary>
    public double TotalKilobytes
    {
        get { lock (_lock) return (_undoBytes + _redoBytes) / 1024.0; }
    }

    /// <summary>
    /// Beschriftung der nächsten Undo-Aktion (für Menü-Anzeige).
    /// Null, wenn kein Undo verfügbar.
    /// </summary>
    public string? NextUndoLabel
    {
        get { lock (_lock) return _undoStack.TryPeek(out var e) ? e.Label : null; }
    }

    /// <summary>
    /// Beschriftung der nächsten Redo-Aktion (für Menü-Anzeige).
    /// Null, wenn kein Redo verfügbar.
    /// </summary>
    public string? NextRedoLabel
    {
        get { lock (_lock) return _redoStack.TryPeek(out var e) ? e.Label : null; }
    }

    // ─────────────────────────────────────────────────────────────
    //  History-Zugriff (für optionale History-Anzeige in der UI)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Gibt alle Undo-Einträge zurück (neueste zuerst).
    /// Nur für Anzeige — nicht für Restore verwenden.
    /// </summary>
    public IReadOnlyList<SnapshotEntry> UndoHistory
    {
        get { lock (_lock) return _undoStack.ToArray(); }
    }

    /// <summary>
    /// Gibt alle Redo-Einträge zurück (neueste zuerst).
    /// </summary>
    public IReadOnlyList<SnapshotEntry> RedoHistory
    {
        get { lock (_lock) return _redoStack.ToArray(); }
    }

    // ─────────────────────────────────────────────────────────────
    //  Haupt-API
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Legt einen neuen Snapshot auf den Undo-Stack.
    /// Leert dabei den Redo-Stack (neue Aktion bricht Redo-Kette ab).
    /// Begrenzt den Stack auf <see cref="MaxHistory"/> Einträge.
    /// </summary>
    /// <param name="entry">Der zu speichernde Snapshot.</param>
    public void Push(SnapshotEntry entry)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        if (!entry.HasJson) return;

        lock (_lock)
        {
            if (_undoStack.TryPeek(out var currentTop)
                && currentTop.Context == entry.Context
                && currentTop.TargetId == entry.TargetId
                && currentTop.HasSamePayload(entry))
            {
                return;
            }

            PushUndoEntry(entry);
            ClearRedoStack();
            TrimToMaxHistory();
        }
    }

    /// <summary>
    /// Führt Undo durch:
    /// - Legt <paramref name="currentEntry"/> auf den Redo-Stack
    /// - Gibt den letzten Undo-Eintrag zurück
    /// </summary>
    /// <param name="currentEntry">Snapshot des aktuellen Zustands (für Redo).</param>
    /// <returns>Der wiederherzustellende Undo-Eintrag, oder null wenn Stack leer.</returns>
    public SnapshotEntry? PopUndo(SnapshotEntry currentEntry)
    {
        if (currentEntry == null) throw new ArgumentNullException(nameof(currentEntry));

        lock (_lock)
        {
            if (_undoStack.Count == 0)
                return null;

            PushRedoEntry(currentEntry);

            var undoEntry = _undoStack.Pop();
            RemoveUndoEntry(undoEntry);
            return undoEntry;
        }
    }

    /// <summary>
    /// Führt Redo durch:
    /// - Legt <paramref name="currentEntry"/> auf den Undo-Stack
    /// - Gibt den letzten Redo-Eintrag zurück
    /// </summary>
    /// <param name="currentEntry">Snapshot des aktuellen Zustands (für Undo).</param>
    /// <returns>Der wiederherzustellende Redo-Eintrag, oder null wenn Stack leer.</returns>
    public SnapshotEntry? PopRedo(SnapshotEntry currentEntry)
    {
        if (currentEntry == null) throw new ArgumentNullException(nameof(currentEntry));

        lock (_lock)
        {
            if (_redoStack.Count == 0)
                return null;

            PushUndoEntry(currentEntry);

            var redoEntry = _redoStack.Pop();
            RemoveRedoEntry(redoEntry);
            return redoEntry;
        }
    }

    /// <summary>
    /// Leert beide Stacks vollständig.
    /// Wird beim Laden eines neuen Projekts / Wechsel des Screens aufgerufen.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _entryBytes.Clear();
            _undoBytes = 0;
            _redoBytes = 0;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Hilfsmethoden
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Begrenzt den Undo-Stack auf <see cref="MaxHistory"/> Einträge.
    /// Älteste Einträge werden verworfen.
    /// </summary>
    private void TrimToMaxHistory()
    {
        if (_undoStack.Count <= MaxHistory)
            return;

        // Stack in Array, neueste MaxHistory-Einträge behalten
        var entries = _undoStack.ToArray();
        _undoStack.Clear();

        foreach (var entry in entries)
            _entryBytes.Remove(entry);

        _undoBytes = 0;

        int keepCount = Math.Max(0, Math.Min(entries.Length, MaxHistory));

        // ToArray() liefert "neueste zuerst" → wir nehmen die ersten MaxHistory
        for (int i = keepCount - 1; i >= 0; i--)
            PushUndoEntry(entries[i]);
    }

    private void PushUndoEntry(SnapshotEntry entry)
    {
        _undoStack.Push(entry);
        _undoBytes += GetOrAddByteCount(entry);
    }

    private void PushRedoEntry(SnapshotEntry entry)
    {
        _redoStack.Push(entry);
        _redoBytes += GetOrAddByteCount(entry);
    }

    private void RemoveUndoEntry(SnapshotEntry entry)
    {
        _undoBytes -= GetOrAddByteCount(entry);
        _entryBytes.Remove(entry);
    }

    private void RemoveRedoEntry(SnapshotEntry entry)
    {
        _redoBytes -= GetOrAddByteCount(entry);
        _entryBytes.Remove(entry);
    }

    private void ClearRedoStack()
    {
        foreach (var entry in _redoStack)
            _entryBytes.Remove(entry);

        _redoStack.Clear();
        _redoBytes = 0;
    }

    private long GetOrAddByteCount(SnapshotEntry entry)
    {
        if (_entryBytes.TryGetValue(entry, out long byteCount))
            return byteCount;

        byteCount = CalculateEntryBytes(entry);
        _entryBytes[entry] = byteCount;
        return byteCount;
    }

    private static long CalculateEntryBytes(SnapshotEntry entry)
    {
        return entry.StoredByteCount;
    }
}


//TODO: REMOVE

//// ======================================================================================
//// FILE: Mockup.Snapshots/SnapshotStack.cs
////
//// ZWECK:
////   Kern-Engine des Undo/Redo-Systems. Verwaltet zwei Stacks (Undo/Redo)
////   und kapselt die gesamte Zustandsverwaltung.
////
//// DESIGN-ENTSCHEIDUNGEN:
////   - Generisch über ISnapshotTarget<T>: funktioniert für Screen, Template, Popup
////   - MaxHistory begrenzt Speicherverbrauch (Default: 50 Einträge)
////   - Redo-Stack wird bei jeder neuen Aktion geleert (Standard-Verhalten)
////   - Thread-safe über lock (UI-Thread-Only wäre ausreichend, aber sicherer so)
//// ======================================================================================

////namespace Mockup.Snapshots;

///// <summary>
///// Generischer Undo/Redo-Stack für einen einzelnen Designer-Kontext.
///// Verwaltet Snapshots als <see cref="SnapshotEntry"/>-Objekte.
///// </summary>
//public sealed class SnapshotStack
//{
//    // ─────────────────────────────────────────────────────────────
//    //  Felder
//    // ─────────────────────────────────────────────────────────────

//    private readonly Stack<SnapshotEntry> _undoStack = new();
//    private readonly Stack<SnapshotEntry> _redoStack = new();
//    private readonly object _lock = new();

//    // ─────────────────────────────────────────────────────────────
//    //  Konfiguration
//    // ─────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Maximale Anzahl an Undo-Einträgen. Ältere werden verworfen.
//    /// Standard: 50.
//    /// </summary>
//    public int MaxHistory { get; set; } = 50;

//    // ─────────────────────────────────────────────────────────────
//    //  Status-Properties
//    // ─────────────────────────────────────────────────────────────

//    /// <summary>Gibt an, ob ein Undo möglich ist.</summary>
//    public bool CanUndo
//    {
//        get { lock (_lock) return _undoStack.Count > 0; }
//    }

//    /// <summary>Gibt an, ob ein Redo möglich ist.</summary>
//    public bool CanRedo
//    {
//        get { lock (_lock) return _redoStack.Count > 0; }
//    }

//    /// <summary>Anzahl der Einträge im Undo-Stack.</summary>
//    public int UndoCount
//    {
//        get { lock (_lock) return _undoStack.Count; }
//    }

//    /// <summary>Anzahl der Einträge im Redo-Stack.</summary>
//    public int RedoCount
//    {
//        get { lock (_lock) return _redoStack.Count; }
//    }

//    /// <summary>
//    /// Größe der Undo-Einträge in UTF-8-Bytes.
//    /// Für UI/Statusanzeige, nicht für Persistenzlogik.
//    /// </summary>
//    public long UndoBytes
//    {
//        get { lock (_lock) return CalculateBytes(_undoStack); }
//    }

//    /// <summary>
//    /// Größe der Redo-Einträge in UTF-8-Bytes.
//    /// Für UI/Statusanzeige, nicht für Persistenzlogik.
//    /// </summary>
//    public long RedoBytes
//    {
//        get { lock (_lock) return CalculateBytes(_redoStack); }
//    }

//    /// <summary>
//    /// Gesamtgröße von Undo + Redo in UTF-8-Bytes.
//    /// </summary>
//    public long TotalBytes
//    {
//        get { lock (_lock) return CalculateBytes(_undoStack) + CalculateBytes(_redoStack); }
//    }

//    /// <summary>
//    /// Kompatibilitätsalias: Gesamtgröße von Undo + Redo in UTF-8-Bytes.
//    /// </summary>
//    public long TotalUtf8Bytes
//    {
//        get { lock (_lock) return TotalBytes; }
//    }

//    /// <summary>
//    /// Gesamtgröße von Undo + Redo in Kilobytes.
//    /// </summary>
//    public double TotalKilobytes
//    {
//        get { lock (_lock) return TotalBytes / 1024.0; }
//    }

//    /// <summary>
//    /// Beschriftung der nächsten Undo-Aktion (für Menü-Anzeige).
//    /// Null, wenn kein Undo verfügbar.
//    /// </summary>
//    public string? NextUndoLabel
//    {
//        get { lock (_lock) return _undoStack.TryPeek(out var e) ? e.Label : null; }
//    }

//    /// <summary>
//    /// Beschriftung der nächsten Redo-Aktion (für Menü-Anzeige).
//    /// Null, wenn kein Redo verfügbar.
//    /// </summary>
//    public string? NextRedoLabel
//    {
//        get { lock (_lock) return _redoStack.TryPeek(out var e) ? e.Label : null; }
//    }

//    // ─────────────────────────────────────────────────────────────
//    //  History-Zugriff (für optionale History-Anzeige in der UI)
//    // ─────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Gibt alle Undo-Einträge zurück (neueste zuerst).
//    /// Nur für Anzeige — nicht für Restore verwenden.
//    /// </summary>
//    public IReadOnlyList<SnapshotEntry> UndoHistory
//    {
//        get { lock (_lock) return _undoStack.ToArray(); }
//    }

//    /// <summary>
//    /// Gibt alle Redo-Einträge zurück (neueste zuerst).
//    /// </summary>
//    public IReadOnlyList<SnapshotEntry> RedoHistory
//    {
//        get { lock (_lock) return _redoStack.ToArray(); }
//    }

//    // ─────────────────────────────────────────────────────────────
//    //  Haupt-API
//    // ─────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Legt einen neuen Snapshot auf den Undo-Stack.
//    /// Leert dabei den Redo-Stack (neue Aktion bricht Redo-Kette ab).
//    /// Begrenzt den Stack auf <see cref="MaxHistory"/> Einträge.
//    /// </summary>
//    /// <param name="entry">Der zu speichernde Snapshot.</param>
//    public void Push(SnapshotEntry entry)
//    {
//        if (entry == null) throw new ArgumentNullException(nameof(entry));
//        if (string.IsNullOrEmpty(entry.Json)) return;

//        lock (_lock)
//        {
//            if (_undoStack.TryPeek(out var currentTop)
//                && currentTop.Context == entry.Context
//                && currentTop.TargetId == entry.TargetId
//                && string.Equals(currentTop.Json, entry.Json, StringComparison.Ordinal))
//            {
//                return;
//            }

//            _undoStack.Push(entry);
//            _redoStack.Clear();
//            TrimToMaxHistory();
//        }
//    }

//    /// <summary>
//    /// Führt Undo durch:
//    /// - Legt <paramref name="currentEntry"/> auf den Redo-Stack
//    /// - Gibt den letzten Undo-Eintrag zurück
//    /// </summary>
//    /// <param name="currentEntry">Snapshot des aktuellen Zustands (für Redo).</param>
//    /// <returns>Der wiederherzustellende Undo-Eintrag, oder null wenn Stack leer.</returns>
//    public SnapshotEntry? PopUndo(SnapshotEntry currentEntry)
//    {
//        if (currentEntry == null) throw new ArgumentNullException(nameof(currentEntry));

//        lock (_lock)
//        {
//            if (_undoStack.Count == 0)
//                return null;

//            _redoStack.Push(currentEntry);
//            return _undoStack.Pop();
//        }
//    }

//    /// <summary>
//    /// Führt Redo durch:
//    /// - Legt <paramref name="currentEntry"/> auf den Undo-Stack
//    /// - Gibt den letzten Redo-Eintrag zurück
//    /// </summary>
//    /// <param name="currentEntry">Snapshot des aktuellen Zustands (für Undo).</param>
//    /// <returns>Der wiederherzustellende Redo-Eintrag, oder null wenn Stack leer.</returns>
//    public SnapshotEntry? PopRedo(SnapshotEntry currentEntry)
//    {
//        if (currentEntry == null) throw new ArgumentNullException(nameof(currentEntry));

//        lock (_lock)
//        {
//            if (_redoStack.Count == 0)
//                return null;

//            _undoStack.Push(currentEntry);
//            return _redoStack.Pop();
//        }
//    }

//    /// <summary>
//    /// Leert beide Stacks vollständig.
//    /// Wird beim Laden eines neuen Projekts / Wechsel des Screens aufgerufen.
//    /// </summary>
//    public void Clear()
//    {
//        lock (_lock)
//        {
//            _undoStack.Clear();
//            _redoStack.Clear();
//        }
//    }

//    // ─────────────────────────────────────────────────────────────
//    //  Hilfsmethoden
//    // ─────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Begrenzt den Undo-Stack auf <see cref="MaxHistory"/> Einträge.
//    /// Älteste Einträge werden verworfen.
//    /// </summary>
//    private void TrimToMaxHistory()
//    {
//        if (_undoStack.Count <= MaxHistory)
//            return;

//        // Stack in Array, neueste MaxHistory-Einträge behalten
//        var entries = _undoStack.ToArray();
//        _undoStack.Clear();

//        // ToArray() liefert "neueste zuerst" → wir nehmen die ersten MaxHistory
//        for (int i = Math.Min(entries.Length, MaxHistory) - 1; i >= 0; i--)
//            _undoStack.Push(entries[i]);
//    }

//    private static long CalculateBytes(IEnumerable<SnapshotEntry> entries)
//    {
//        long total = 0;

//        foreach (var entry in entries)
//        {
//            if (!string.IsNullOrEmpty(entry.Json))
//                total += Encoding.UTF8.GetByteCount(entry.Json);
//        }

//        return total;
//    }
//}



//TODO: REMOVE

//// ======================================================================================
//// FILE: Mockup.Snapshots/SnapshotStack.cs
////
//// ZWECK:
////   Kern-Engine des Undo/Redo-Systems. Verwaltet zwei Stacks (Undo/Redo)
////   und kapselt die gesamte Zustandsverwaltung.
////
//// DESIGN-ENTSCHEIDUNGEN:
////   - Generisch über ISnapshotTarget<T>: funktioniert für Screen, Template, Popup
////   - MaxHistory begrenzt Speicherverbrauch (Default: 50 Einträge)
////   - Redo-Stack wird bei jeder neuen Aktion geleert (Standard-Verhalten)
////   - Thread-safe über lock (UI-Thread-Only wäre ausreichend, aber sicherer so)
//// ======================================================================================

//using System.Text;

//namespace Mockup.Snapshots;

///// <summary>
///// Generischer Undo/Redo-Stack für einen einzelnen Designer-Kontext.
///// Verwaltet Snapshots als <see cref="SnapshotEntry"/>-Objekte.
///// </summary>
//public sealed class SnapshotStack
//{
//    // ─────────────────────────────────────────────────────────────
//    //  Felder
//    // ─────────────────────────────────────────────────────────────

//    private readonly Stack<SnapshotEntry> _undoStack = new();
//    private readonly Stack<SnapshotEntry> _redoStack = new();
//    private readonly Dictionary<SnapshotEntry, long> _entryBytes = new();
//    private readonly object _lock = new();

//    private long _undoBytes;
//    private long _redoBytes;

//    // ─────────────────────────────────────────────────────────────
//    //  Konfiguration
//    // ─────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Maximale Anzahl an Undo-Einträgen. Ältere werden verworfen.
//    /// Standard: 50.
//    /// </summary>
//    public int MaxHistory { get; set; } = 50;

//    // ─────────────────────────────────────────────────────────────
//    //  Status-Properties
//    // ─────────────────────────────────────────────────────────────

//    /// <summary>Gibt an, ob ein Undo möglich ist.</summary>
//    public bool CanUndo
//    {
//        get { lock (_lock) return _undoStack.Count > 0; }
//    }

//    /// <summary>Gibt an, ob ein Redo möglich ist.</summary>
//    public bool CanRedo
//    {
//        get { lock (_lock) return _redoStack.Count > 0; }
//    }

//    /// <summary>Anzahl der Einträge im Undo-Stack.</summary>
//    public int UndoCount
//    {
//        get { lock (_lock) return _undoStack.Count; }
//    }

//    /// <summary>Anzahl der Einträge im Redo-Stack.</summary>
//    public int RedoCount
//    {
//        get { lock (_lock) return _redoStack.Count; }
//    }

//    /// <summary>
//    /// Größe der Undo-Einträge in UTF-8-Bytes.
//    /// Für UI/Statusanzeige, nicht für Persistenzlogik.
//    /// </summary>
//    public long UndoBytes
//    {
//        get { lock (_lock) return _undoBytes; }
//    }

//    /// <summary>
//    /// Größe der Redo-Einträge in UTF-8-Bytes.
//    /// Für UI/Statusanzeige, nicht für Persistenzlogik.
//    /// </summary>
//    public long RedoBytes
//    {
//        get { lock (_lock) return _redoBytes; }
//    }

//    /// <summary>
//    /// Gesamtgröße von Undo + Redo in UTF-8-Bytes.
//    /// </summary>
//    public long TotalBytes
//    {
//        get { lock (_lock) return _undoBytes + _redoBytes; }
//    }

//    /// <summary>
//    /// Kompatibilitätsalias: Gesamtgröße von Undo + Redo in UTF-8-Bytes.
//    /// </summary>
//    public long TotalUtf8Bytes
//    {
//        get { lock (_lock) return _undoBytes + _redoBytes; }
//    }

//    /// <summary>
//    /// Gesamtgröße von Undo + Redo in Kilobytes.
//    /// </summary>
//    public double TotalKilobytes
//    {
//        get { lock (_lock) return (_undoBytes + _redoBytes) / 1024.0; }
//    }

//    /// <summary>
//    /// Beschriftung der nächsten Undo-Aktion (für Menü-Anzeige).
//    /// Null, wenn kein Undo verfügbar.
//    /// </summary>
//    public string? NextUndoLabel
//    {
//        get { lock (_lock) return _undoStack.TryPeek(out var e) ? e.Label : null; }
//    }

//    /// <summary>
//    /// Beschriftung der nächsten Redo-Aktion (für Menü-Anzeige).
//    /// Null, wenn kein Redo verfügbar.
//    /// </summary>
//    public string? NextRedoLabel
//    {
//        get { lock (_lock) return _redoStack.TryPeek(out var e) ? e.Label : null; }
//    }

//    // ─────────────────────────────────────────────────────────────
//    //  History-Zugriff (für optionale History-Anzeige in der UI)
//    // ─────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Gibt alle Undo-Einträge zurück (neueste zuerst).
//    /// Nur für Anzeige — nicht für Restore verwenden.
//    /// </summary>
//    public IReadOnlyList<SnapshotEntry> UndoHistory
//    {
//        get { lock (_lock) return _undoStack.ToArray(); }
//    }

//    /// <summary>
//    /// Gibt alle Redo-Einträge zurück (neueste zuerst).
//    /// </summary>
//    public IReadOnlyList<SnapshotEntry> RedoHistory
//    {
//        get { lock (_lock) return _redoStack.ToArray(); }
//    }

//    // ─────────────────────────────────────────────────────────────
//    //  Haupt-API
//    // ─────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Legt einen neuen Snapshot auf den Undo-Stack.
//    /// Leert dabei den Redo-Stack (neue Aktion bricht Redo-Kette ab).
//    /// Begrenzt den Stack auf <see cref="MaxHistory"/> Einträge.
//    /// </summary>
//    /// <param name="entry">Der zu speichernde Snapshot.</param>
//    public void Push(SnapshotEntry entry)
//    {
//        if (entry == null) throw new ArgumentNullException(nameof(entry));
//        if (!entry.HasJson) return;

//        lock (_lock)
//        {
//            if (_undoStack.TryPeek(out var currentTop)
//                && currentTop.Context == entry.Context
//                && currentTop.TargetId == entry.TargetId
//                && currentTop.JsonEquals(entry))
//            {
//                return;
//            }

//            PushUndoEntry(entry);
//            ClearRedoStack();
//            TrimToMaxHistory();
//        }
//    }

//    /// <summary>
//    /// Führt Undo durch:
//    /// - Legt <paramref name="currentEntry"/> auf den Redo-Stack
//    /// - Gibt den letzten Undo-Eintrag zurück
//    /// </summary>
//    /// <param name="currentEntry">Snapshot des aktuellen Zustands (für Redo).</param>
//    /// <returns>Der wiederherzustellende Undo-Eintrag, oder null wenn Stack leer.</returns>
//    public SnapshotEntry? PopUndo(SnapshotEntry currentEntry)
//    {
//        if (currentEntry == null) throw new ArgumentNullException(nameof(currentEntry));

//        lock (_lock)
//        {
//            if (_undoStack.Count == 0)
//                return null;

//            PushRedoEntry(currentEntry);

//            var undoEntry = _undoStack.Pop();
//            RemoveUndoEntry(undoEntry);
//            return undoEntry;
//        }
//    }

//    /// <summary>
//    /// Führt Redo durch:
//    /// - Legt <paramref name="currentEntry"/> auf den Undo-Stack
//    /// - Gibt den letzten Redo-Eintrag zurück
//    /// </summary>
//    /// <param name="currentEntry">Snapshot des aktuellen Zustands (für Undo).</param>
//    /// <returns>Der wiederherzustellende Redo-Eintrag, oder null wenn Stack leer.</returns>
//    public SnapshotEntry? PopRedo(SnapshotEntry currentEntry)
//    {
//        if (currentEntry == null) throw new ArgumentNullException(nameof(currentEntry));

//        lock (_lock)
//        {
//            if (_redoStack.Count == 0)
//                return null;

//            PushUndoEntry(currentEntry);

//            var redoEntry = _redoStack.Pop();
//            RemoveRedoEntry(redoEntry);
//            return redoEntry;
//        }
//    }

//    /// <summary>
//    /// Leert beide Stacks vollständig.
//    /// Wird beim Laden eines neuen Projekts / Wechsel des Screens aufgerufen.
//    /// </summary>
//    public void Clear()
//    {
//        lock (_lock)
//        {
//            _undoStack.Clear();
//            _redoStack.Clear();
//            _entryBytes.Clear();
//            _undoBytes = 0;
//            _redoBytes = 0;
//        }
//    }

//    // ─────────────────────────────────────────────────────────────
//    //  Hilfsmethoden
//    // ─────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Begrenzt den Undo-Stack auf <see cref="MaxHistory"/> Einträge.
//    /// Älteste Einträge werden verworfen.
//    /// </summary>
//    private void TrimToMaxHistory()
//    {
//        if (_undoStack.Count <= MaxHistory)
//            return;

//        // Stack in Array, neueste MaxHistory-Einträge behalten
//        var entries = _undoStack.ToArray();
//        _undoStack.Clear();

//        foreach (var entry in entries)
//            _entryBytes.Remove(entry);

//        _undoBytes = 0;

//        int keepCount = Math.Max(0, Math.Min(entries.Length, MaxHistory));

//        // ToArray() liefert "neueste zuerst" → wir nehmen die ersten MaxHistory
//        for (int i = keepCount - 1; i >= 0; i--)
//            PushUndoEntry(entries[i]);
//    }

//    private void PushUndoEntry(SnapshotEntry entry)
//    {
//        _undoStack.Push(entry);
//        _undoBytes += GetOrAddByteCount(entry);
//    }

//    private void PushRedoEntry(SnapshotEntry entry)
//    {
//        _redoStack.Push(entry);
//        _redoBytes += GetOrAddByteCount(entry);
//    }

//    private void RemoveUndoEntry(SnapshotEntry entry)
//    {
//        _undoBytes -= GetOrAddByteCount(entry);
//        _entryBytes.Remove(entry);
//    }

//    private void RemoveRedoEntry(SnapshotEntry entry)
//    {
//        _redoBytes -= GetOrAddByteCount(entry);
//        _entryBytes.Remove(entry);
//    }

//    private void ClearRedoStack()
//    {
//        foreach (var entry in _redoStack)
//            _entryBytes.Remove(entry);

//        _redoStack.Clear();
//        _redoBytes = 0;
//    }

//    private long GetOrAddByteCount(SnapshotEntry entry)
//    {
//        if (_entryBytes.TryGetValue(entry, out long byteCount))
//            return byteCount;

//        byteCount = CalculateEntryBytes(entry);
//        _entryBytes[entry] = byteCount;
//        return byteCount;
//    }

//    private static long CalculateEntryBytes(SnapshotEntry entry)
//    {
//        return entry.StoredByteCount;
//    }
//}


////TODO: REMOVE

////// ======================================================================================
////// FILE: Mockup.Snapshots/SnapshotStack.cs
//////
////// ZWECK:
//////   Kern-Engine des Undo/Redo-Systems. Verwaltet zwei Stacks (Undo/Redo)
//////   und kapselt die gesamte Zustandsverwaltung.
//////
////// DESIGN-ENTSCHEIDUNGEN:
//////   - Generisch über ISnapshotTarget<T>: funktioniert für Screen, Template, Popup
//////   - MaxHistory begrenzt Speicherverbrauch (Default: 50 Einträge)
//////   - Redo-Stack wird bei jeder neuen Aktion geleert (Standard-Verhalten)
//////   - Thread-safe über lock (UI-Thread-Only wäre ausreichend, aber sicherer so)
////// ======================================================================================

////using System.Text;

////namespace Mockup.Snapshots;

/////// <summary>
/////// Generischer Undo/Redo-Stack für einen einzelnen Designer-Kontext.
/////// Verwaltet Snapshots als <see cref="SnapshotEntry"/>-Objekte.
/////// </summary>
////public sealed class SnapshotStack
////{
////    // ─────────────────────────────────────────────────────────────
////    //  Felder
////    // ─────────────────────────────────────────────────────────────

////    private readonly Stack<SnapshotEntry> _undoStack = new();
////    private readonly Stack<SnapshotEntry> _redoStack = new();
////    private readonly object _lock = new();

////    // ─────────────────────────────────────────────────────────────
////    //  Konfiguration
////    // ─────────────────────────────────────────────────────────────

////    /// <summary>
////    /// Maximale Anzahl an Undo-Einträgen. Ältere werden verworfen.
////    /// Standard: 50.
////    /// </summary>
////    public int MaxHistory { get; set; } = 50;

////    // ─────────────────────────────────────────────────────────────
////    //  Status-Properties
////    // ─────────────────────────────────────────────────────────────

////    /// <summary>Gibt an, ob ein Undo möglich ist.</summary>
////    public bool CanUndo
////    {
////        get { lock (_lock) return _undoStack.Count > 0; }
////    }

////    /// <summary>Gibt an, ob ein Redo möglich ist.</summary>
////    public bool CanRedo
////    {
////        get { lock (_lock) return _redoStack.Count > 0; }
////    }

////    /// <summary>Anzahl der Einträge im Undo-Stack.</summary>
////    public int UndoCount
////    {
////        get { lock (_lock) return _undoStack.Count; }
////    }

////    /// <summary>Anzahl der Einträge im Redo-Stack.</summary>
////    public int RedoCount
////    {
////        get { lock (_lock) return _redoStack.Count; }
////    }

////    /// <summary>
////    /// Größe der Undo-Einträge in UTF-8-Bytes.
////    /// Für UI/Statusanzeige, nicht für Persistenzlogik.
////    /// </summary>
////    public long UndoBytes
////    {
////        get { lock (_lock) return CalculateBytes(_undoStack); }
////    }

////    /// <summary>
////    /// Größe der Redo-Einträge in UTF-8-Bytes.
////    /// Für UI/Statusanzeige, nicht für Persistenzlogik.
////    /// </summary>
////    public long RedoBytes
////    {
////        get { lock (_lock) return CalculateBytes(_redoStack); }
////    }

////    /// <summary>
////    /// Gesamtgröße von Undo + Redo in UTF-8-Bytes.
////    /// </summary>
////    public long TotalBytes
////    {
////        get { lock (_lock) return CalculateBytes(_undoStack) + CalculateBytes(_redoStack); }
////    }

////    /// <summary>
////    /// Kompatibilitätsalias: Gesamtgröße von Undo + Redo in UTF-8-Bytes.
////    /// </summary>
////    public long TotalUtf8Bytes
////    {
////        get { lock (_lock) return TotalBytes; }
////    }

////    /// <summary>
////    /// Gesamtgröße von Undo + Redo in Kilobytes.
////    /// </summary>
////    public double TotalKilobytes
////    {
////        get { lock (_lock) return TotalBytes / 1024.0; }
////    }

////    /// <summary>
////    /// Beschriftung der nächsten Undo-Aktion (für Menü-Anzeige).
////    /// Null, wenn kein Undo verfügbar.
////    /// </summary>
////    public string? NextUndoLabel
////    {
////        get { lock (_lock) return _undoStack.TryPeek(out var e) ? e.Label : null; }
////    }

////    /// <summary>
////    /// Beschriftung der nächsten Redo-Aktion (für Menü-Anzeige).
////    /// Null, wenn kein Redo verfügbar.
////    /// </summary>
////    public string? NextRedoLabel
////    {
////        get { lock (_lock) return _redoStack.TryPeek(out var e) ? e.Label : null; }
////    }

////    // ─────────────────────────────────────────────────────────────
////    //  History-Zugriff (für optionale History-Anzeige in der UI)
////    // ─────────────────────────────────────────────────────────────

////    /// <summary>
////    /// Gibt alle Undo-Einträge zurück (neueste zuerst).
////    /// Nur für Anzeige — nicht für Restore verwenden.
////    /// </summary>
////    public IReadOnlyList<SnapshotEntry> UndoHistory
////    {
////        get { lock (_lock) return _undoStack.ToArray(); }
////    }

////    /// <summary>
////    /// Gibt alle Redo-Einträge zurück (neueste zuerst).
////    /// </summary>
////    public IReadOnlyList<SnapshotEntry> RedoHistory
////    {
////        get { lock (_lock) return _redoStack.ToArray(); }
////    }

////    // ─────────────────────────────────────────────────────────────
////    //  Haupt-API
////    // ─────────────────────────────────────────────────────────────

////    /// <summary>
////    /// Legt einen neuen Snapshot auf den Undo-Stack.
////    /// Leert dabei den Redo-Stack (neue Aktion bricht Redo-Kette ab).
////    /// Begrenzt den Stack auf <see cref="MaxHistory"/> Einträge.
////    /// </summary>
////    /// <param name="entry">Der zu speichernde Snapshot.</param>
////    public void Push(SnapshotEntry entry)
////    {
////        if (entry == null) throw new ArgumentNullException(nameof(entry));
////        if (string.IsNullOrEmpty(entry.Json)) return;

////        lock (_lock)
////        {
////            if (_undoStack.TryPeek(out var currentTop)
////                && currentTop.Context == entry.Context
////                && currentTop.TargetId == entry.TargetId
////                && string.Equals(currentTop.Json, entry.Json, StringComparison.Ordinal))
////            {
////                return;
////            }

////            _undoStack.Push(entry);
////            _redoStack.Clear();
////            TrimToMaxHistory();
////        }
////    }

////    /// <summary>
////    /// Führt Undo durch:
////    /// - Legt <paramref name="currentEntry"/> auf den Redo-Stack
////    /// - Gibt den letzten Undo-Eintrag zurück
////    /// </summary>
////    /// <param name="currentEntry">Snapshot des aktuellen Zustands (für Redo).</param>
////    /// <returns>Der wiederherzustellende Undo-Eintrag, oder null wenn Stack leer.</returns>
////    public SnapshotEntry? PopUndo(SnapshotEntry currentEntry)
////    {
////        if (currentEntry == null) throw new ArgumentNullException(nameof(currentEntry));

////        lock (_lock)
////        {
////            if (_undoStack.Count == 0)
////                return null;

////            _redoStack.Push(currentEntry);
////            return _undoStack.Pop();
////        }
////    }

////    /// <summary>
////    /// Führt Redo durch:
////    /// - Legt <paramref name="currentEntry"/> auf den Undo-Stack
////    /// - Gibt den letzten Redo-Eintrag zurück
////    /// </summary>
////    /// <param name="currentEntry">Snapshot des aktuellen Zustands (für Undo).</param>
////    /// <returns>Der wiederherzustellende Redo-Eintrag, oder null wenn Stack leer.</returns>
////    public SnapshotEntry? PopRedo(SnapshotEntry currentEntry)
////    {
////        if (currentEntry == null) throw new ArgumentNullException(nameof(currentEntry));

////        lock (_lock)
////        {
////            if (_redoStack.Count == 0)
////                return null;

////            _undoStack.Push(currentEntry);
////            return _redoStack.Pop();
////        }
////    }

////    /// <summary>
////    /// Leert beide Stacks vollständig.
////    /// Wird beim Laden eines neuen Projekts / Wechsel des Screens aufgerufen.
////    /// </summary>
////    public void Clear()
////    {
////        lock (_lock)
////        {
////            _undoStack.Clear();
////            _redoStack.Clear();
////        }
////    }

////    // ─────────────────────────────────────────────────────────────
////    //  Hilfsmethoden
////    // ─────────────────────────────────────────────────────────────

////    /// <summary>
////    /// Begrenzt den Undo-Stack auf <see cref="MaxHistory"/> Einträge.
////    /// Älteste Einträge werden verworfen.
////    /// </summary>
////    private void TrimToMaxHistory()
////    {
////        if (_undoStack.Count <= MaxHistory)
////            return;

////        // Stack in Array, neueste MaxHistory-Einträge behalten
////        var entries = _undoStack.ToArray();
////        _undoStack.Clear();

////        // ToArray() liefert "neueste zuerst" → wir nehmen die ersten MaxHistory
////        for (int i = Math.Min(entries.Length, MaxHistory) - 1; i >= 0; i--)
////            _undoStack.Push(entries[i]);
////    }

////    private static long CalculateBytes(IEnumerable<SnapshotEntry> entries)
////    {
////        long total = 0;

////        foreach (var entry in entries)
////        {
////            if (!string.IsNullOrEmpty(entry.Json))
////                total += Encoding.UTF8.GetByteCount(entry.Json);
////        }

////        return total;
////    }
////}
