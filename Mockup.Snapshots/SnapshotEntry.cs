// ======================================================================================
// FILE: Mockup.Snapshots/SnapshotEntry.cs
//
// ZWECK:
//   Repräsentiert einen einzelnen Snapshot-Eintrag im Undo/Redo-Stack.
//   Enthält den serialisierten JSON-Zustand sowie Metadaten für die UI
//   (Beschriftung, Zeitstempel, Kontext).
// ======================================================================================

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Mockup.Snapshots;

/// <summary>
/// Ein einzelner Snapshot-Eintrag im Undo/Redo-Stack.
/// </summary>
public sealed class SnapshotEntry
{
    private byte[] _compressedJson = Array.Empty<byte>();
    private byte[] _jsonHash = Array.Empty<byte>();
    private int _originalUtf8ByteCount;

    /// <summary>
    /// Serialisierter JSON-Zustand des Objekts zum Zeitpunkt des Snapshots.
    /// Kompatibilitäts-Property: Der Stack hält intern komprimierte UTF-8-Daten.
    /// Beim Lesen wird der JSON-String bei Bedarf dekomprimiert.
    /// </summary>
    public string Json
    {
        get => GetJson();
        init => SetJson(value);
    }

    /// <summary>
    /// Größe des dauerhaft gespeicherten Snapshot-Payloads in Bytes.
    /// Aktuell entspricht das der GZip-komprimierten UTF-8-JSON-Größe.
    /// </summary>
    public int CompressedSize => _compressedJson.Length;

    /// <summary>
    /// Größe des ursprünglichen UTF-8-JSON-Payloads in Bytes.
    /// Wird für die schnelle Duplikaterkennung ohne Dekompression genutzt.
    /// </summary>
    internal int OriginalUtf8ByteCount => _originalUtf8ByteCount;

    /// <summary>
    /// SHA-256-Hash des ursprünglichen UTF-8-JSON-Payloads.
    /// Wird für die schnelle Duplikaterkennung ohne Dekompression genutzt.
    /// </summary>
    internal byte[] JsonHash => _jsonHash;

    /// <summary>
    /// Größe des dauerhaft gespeicherten Snapshot-Payloads in Bytes.
    /// Wird vom SnapshotStack für Statusanzeigen verwendet.
    /// </summary>
    internal int StoredByteCount => _compressedJson.Length;

    /// <summary>
    /// Gibt an, ob dieser Eintrag einen Snapshot-Payload enthält.
    /// </summary>
    internal bool HasJson => _compressedJson.Length > 0;

    /// <summary>
    /// Lesbare Beschreibung der Aktion, die diesen Snapshot ausgelöst hat.
    /// Wird in der UI angezeigt (z.B. "Control verschoben", "Band gelöscht").
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Kontext des Snapshots: auf welchen Designer-Bereich bezieht er sich.
    /// </summary>
    public SnapshotContext Context { get; init; } = SnapshotContext.Screen;

    /// <summary>
    /// ID des Objekts, das gesnapshottert wurde (Screen.Id, Template.Id, Popup.Id).
    /// Wird beim Restore verwendet, um das richtige Objekt in der Collection zu finden.
    /// </summary>
    public long TargetId { get; init; }

    /// <summary>
    /// Zeitstempel des Snapshots (für Debugging und optionale History-Anzeige).
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    /// <summary>
    /// Erstellt einen SnapshotEntry aus einem JSON-String.
    /// Der JSON-Inhalt wird sofort GZip-komprimiert gespeichert.
    /// </summary>
    public static SnapshotEntry FromJson(
        string json,
        string label,
        SnapshotContext context,
        long targetId) => new()
        {
            Json = json,
            Label = label,
            Context = context,
            TargetId = targetId,
            CreatedAt = DateTime.Now,
        };

    /// <summary>
    /// Creates a new history entry from an already compressed payload. This is used when
    /// Undo/Redo switches away from an object that was restored from this snapshot and
    /// has not been changed since. Reusing the payload avoids a complete JSON serialize,
    /// hash and compression pass on every history step.
    /// </summary>
    internal SnapshotEntry CreateHistoryCopy(string label, SnapshotContext context, long targetId) => new()
    {
        _compressedJson = (byte[])_compressedJson.Clone(),
        _jsonHash = (byte[])_jsonHash.Clone(),
        _originalUtf8ByteCount = _originalUtf8ByteCount,
        Label = label,
        Context = context,
        TargetId = targetId,
        CreatedAt = DateTime.Now,
    };

    /// <summary>
    /// Vergleicht zwei Snapshot-Payloads ohne Dekompression.
    /// </summary>
    internal bool HasSamePayload(SnapshotEntry other)
    {
        if (other == null)
            return false;

        return _originalUtf8ByteCount == other._originalUtf8ByteCount
               && _jsonHash.AsSpan().SequenceEqual(other._jsonHash);
    }

    /// <summary>
    /// Vergleicht den dekomprimierten JSON-Inhalt zweier Snapshot-Einträge.
    /// Nur für Kompatibilität behalten. Nicht im Hotpath verwenden.
    /// </summary>
    internal bool JsonEquals(SnapshotEntry other)
    {
        if (other == null)
            return false;

        return string.Equals(Json, other.Json, StringComparison.Ordinal);
    }

    private string GetJson()
    {
        if (_compressedJson.Length == 0)
            return string.Empty;

        using var input = new MemoryStream(_compressedJson);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8, detectEncodingFromByteOrderMarks: false);

        return reader.ReadToEnd();
    }

    private void SetJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            _compressedJson = Array.Empty<byte>();
            _jsonHash = Array.Empty<byte>();
            _originalUtf8ByteCount = 0;
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(json);
        _originalUtf8ByteCount = bytes.Length;
        _jsonHash = SHA256.HashData(bytes);

        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }

        _compressedJson = output.ToArray();
    }

    public override string ToString() => $"[{Context}] {Label} ({CreatedAt:HH:mm:ss})";
}

/// <summary>
/// Gibt an, auf welchen Designer-Bereich sich ein Snapshot bezieht.
/// </summary>
public enum SnapshotContext
{
    /// <summary>Projektweite Collection-Änderungen, z.B. Screen oder Popup hinzugefügt/gelöscht.</summary>
    Project,

    /// <summary>Screen-Designer: Snapshot eines einzelnen Screens.</summary>
    Screen,

    /// <summary>Template-Collection: Template hinzugefügt/gelöscht.</summary>
    Templates,

    /// <summary>Template-Designer: Snapshot eines ScreenTemplates.</summary>
    Template,

    /// <summary>Popup-Designer: Snapshot eines ScreenPopups.</summary>
    Popup,
}
