# VIA.UserFlow – Technical Improvement Masterplan

**Untertitel:** Ergänzungsplan zum `UI_BRUSHUP_MASTERPLAN.md`  
**Basis:** aktueller VS2AI-Export vom 21.07.2026, Branch `toolbox-rail-flyout`  
**Zweck:** Alle technischen und architektonischen Empfehlungen aus dem vorherigen Gesamturteil in einen ausführbaren, priorisierten Plan für einen neuen Chat überführen.

---

## 1. Verhältnis zum UI Brushup Masterplan

Dieser Plan ersetzt den UI-Brushup-Plan nicht.

Die beiden Dokumente haben unterschiedliche Aufgaben:

### `UI_BRUSHUP_MASTERPLAN.md`

Behandelt hauptsächlich:

- ProjectView
- ScreenView
- Project-Neuanlage
- Project-Edit-Dialog
- Workbench-Farben
- Actionsichtbarkeit
- Grid-Entfernung
- Designer-Viewport aus UI-Sicht
- einheitliches Look-and-Feel

### `TECHNICAL_IMPROVEMENT_MASTERPLAN.md`

Behandelt hauptsächlich:

- Daten- und Speichersicherheit
- Snapshot-/Undo-/Redo-Speicher
- Preview- und Thumbnail-Performance
- stabile Persistenzschlüssel
- gezielte Invalidation
- Autosave und Dirty-State
- SkiaSharp-Ressourcen
- FlowView
- Popup-LivePreview
- automatisierte Tests
- Performance-Messung
- Nullable- und Fehlerbehandlung

Beide Pläne müssen aufeinander abgestimmt umgesetzt werden.

---

## 2. Welche Empfehlungen bereits im UI-Plan enthalten sind

Folgende Empfehlungen aus dem Gesamturteil sind im UI-Plan bereits ganz oder teilweise enthalten:

### Enthalten

- vollständiger Designer-Viewport
- zentrierte Device Area
- feste PNG-Previews für Controls
- generierte/cached Thumbnails für Templates
- Virtualisierung der Picklisten
- klare Desktop-Presets
- Projektweite Device-Größe nur read-only in der ScreenView
- bessere Pfaddarstellung im Project-Dialog
- Tests für Mobile- und Desktop-Projekte
- schrittweise Einführung einer zentralen Designer-Transformation
- Grid-Entfernung und Guidelines als primäres Snapping-System

### Nur angerissen, aber technisch noch nicht ausreichend geplant

- Ablage in `%LOCALAPPDATA%`
- atomisches Speichern
- Preview-/Thumbnail-Caches
- Snapshot-History-Limits
- Autosave
- Popup- und Template-Optimierung
- Release-/Performance-Tests

### Noch nicht ausreichend enthalten

- hybrider Snapshot-Speicher
- Begrenzung des BackgroundImage-Payload-Caches
- stabile `TypeKey`-Persistenz
- Speichermethoden mit echtem Ergebnis statt verschluckter Fehler
- gezielte Preview-Invalidierung nach Entity-ID und Revision
- FlowView als echtes User-Flow-Werkzeug
- vollständiger Popup-Stack in LivePreview
- Single-Writer-/Autosave-Pipeline
- systematische SkiaSharp-Dispose-Prüfung
- Eventhandler-Lifecycle
- schrittweise Aktivierung der Nullable-Warnungen
- automatisierte Testprojekte
- Performance-Telemetrie und reproduzierbare Benchmarks

---

## 3. Verbindliche Architekturregeln

### 3.1 Bestehende Architektur erhalten

Die vorhandene Projekttrennung bleibt erhalten:

- `VIA.UserFlow`
- `VIA.Mockup`
- `VIA.Mockup.Snapshots`
- `VIA.Mockup.Guidelines`

Insbesondere:

- `VIA.Mockup.Snapshots` kennt keine Mockup-Domaintypen.
- Die konkrete Serialisierung bleibt in `Mockup/SnapshotIntegration`.
- Guidelines bleiben hostneutral und WPF-frei.
- Designer dürfen keine parallelen Schatten-Collections aufbauen.
- Model/ViewModel bleibt Single Source of Truth.
- Persistierte Device-Koordinaten bleiben unverändert.

### 3.2 Keine Big-Bang-Modernisierung

Jede Phase:

1. aktuelle vollständige Dateien prüfen
2. kleinsten sicheren Änderungssatz planen
3. Änderung umsetzen
4. Debug-Build
5. Release-Build
6. manuelle oder automatisierte Tests
7. erst danach nächste Phase

### 3.3 Datenverlust-Risiken haben höchste Priorität

Vor Performance- und UI-Optimierungen müssen behoben werden:

- verschluckte Speicherfehler
- nicht atomisches Schreiben
- instabile `TypeKey`-Werte
- potenziell unbeschreibbare Speicherorte
- unklare Backup- und Recovery-Strategie

---

## 4. Prioritäten

| Priorität | Thema | Grund |
|---|---|---|
| P0 | Speichersicherheit | mögliches Datenverlustrisiko |
| P0 | `TypeKey`-Stabilität | Projekt-/Template-Kompatibilität |
| P1 | Tests und Messwerte | Grundlage für alle weiteren Umbauten |
| P1 | gezielte Preview-Invalidierung | direkter Performance-Hotspot |
| P1 | Template-/Control-Preview-System | hohe UI-Thread- und Skia-Last |
| P1 | Snapshot-Speicher und Bildpayloads | RAM- und Restore-Verhalten |
| P2 | Autosave-/Save-Pipeline | Bedienbarkeit und Stabilität |
| P2 | Designer-Viewport | große UX-Verbesserung, aber koordinatenkritisch |
| P2 | Popup-LivePreview | zentrale Funktionslücke |
| P2 | FlowView | zentrale Produktfunktion |
| P3 | Skia-/Event-Lifecycle | Stabilität und Leak-Vermeidung |
| P3 | Nullable-/Codehygiene | langfristige Wartbarkeit |

---

# TEIL A – DATEN- UND SPEICHERSICHERHEIT

## 5. Speichermethoden dürfen Fehler nicht verschlucken

Im aktuellen Stand existieren Speichermethoden mit leeren `catch`-Blöcken. Das ist nicht akzeptabel.

Beispielhafte Risiken:

- Speichern schlägt fehl, aber die Anwendung arbeitet weiter.
- Der Benutzer erhält keine klare Meldung.
- Ein Erfolgshinweis kann trotz Fehler erscheinen.
- Autosave kann über längere Zeit still nicht funktionieren.
- beschädigte oder schreibgeschützte Speicherorte bleiben unbemerkt.

### Zielarchitektur

Alle Speicheroperationen geben ein strukturiertes Ergebnis zurück:

```csharp
public sealed record StorageResult(
    bool Success,
    string? FilePath,
    string? ErrorMessage,
    Exception? Exception = null);
```

Oder generisch:

```csharp
public sealed record StorageResult<T>(
    bool Success,
    T? Value,
    string? ErrorMessage,
    Exception? Exception = null);
```

### Verbindliche Regeln

- kein leerer `catch`
- Fehler mindestens loggen
- UI erhält Erfolg oder Fehler eindeutig
- Erfolgsmeldung nur nach tatsächlichem Erfolg
- Exit-Speichern darf Fehler protokollieren, aber nicht falschen Erfolg melden
- Autosave-Fehler werden gedrosselt angezeigt, nicht bei jedem Timerlauf erneut

### Betroffene Dateien

- `Mockup/ViewModel/MockupViewModel.Storage.cs`
- `Mockup/ViewModel/MockupViewModel.Commands.cs`
- `UserFlow/App.xaml.cs`
- `Mockup/Services/XNotifications.cs`
- optional neuer Storage-Hilfstyp

### Abnahmekriterien

- schreibgeschützter Ordner erzeugt sichtbaren Fehler
- voller/ungültiger Pfad erzeugt sichtbaren Fehler
- Erfolg wird nur nach erfolgreichem Replace angezeigt
- keine leeren Catch-Blöcke in produktiven Storage-Pfaden

---

## 6. Atomisches Speichern

Direktes `File.WriteAllText` auf die produktive Datei darf nicht die endgültige Strategie bleiben.

### Zielablauf

1. Zielverzeichnis prüfen/anlegen
2. JSON vollständig erzeugen
3. in temporäre Datei im selben Zielverzeichnis schreiben
4. Stream flushen
5. optional bestehende Datei als `.bak` sichern
6. temporäre Datei atomisch auf Zieldatei ersetzen
7. Backup erst nach erfolgreichem Replace verwalten
8. Ergebnis zurückgeben

### Empfohlene API

```csharp
Task<StorageResult> WriteAtomicAsync(
    string targetPath,
    ReadOnlyMemory<byte> content,
    CancellationToken cancellationToken);
```

Für die erste sichere Phase kann synchron gearbeitet werden. Erst nach korrekter Funktion soll die Pipeline asynchron werden.

### Backup-Regel

- maximal 1–3 rotierende Backups pro Projekt
- Backups im Projektordner oder expliziten Backup-Ordner
- eindeutige Erweiterung, zum Beispiel:
  - `.bak`
  - `.bak1`
  - `.bak2`
- Backup darf nie die einzige intakte Datei überschreiben

### Recovery

Beim Laden prüfen:

- Hauptdatei gültig
- falls ungültig: verfügbares Backup anbieten
- keine automatische stille Wiederherstellung
- Originaldatei vor Recovery nicht löschen

### Tests

- Prozessabbruch vor Replace
- Ausnahme beim Schreiben
- defekte JSON-Hauptdatei
- gültiges Backup
- kein Backup vorhanden
- Read-only-Datei
- Dateipfad nicht mehr vorhanden

---

## 7. Speicherorte korrigieren

Beschreibbare Benutzerdaten dürfen nicht standardmäßig unterhalb des Anwendungsverzeichnisses liegen.

### Zielstruktur

```text
%LOCALAPPDATA%\VIA\UserFlow\
├── Settings\
├── Cache\
│   ├── Thumbnails\
│   ├── Templates\
│   └── Assets\
├── Undo\
├── Backups\
├── Logs\
└── Temp\
```

### Projekte

Benutzerprojekte gehören nicht zwingend unter `%LOCALAPPDATA%`.

Empfehlung:

- Benutzer wählt Projektpfad bei Neuanlage.
- Default:
  - `%USERPROFILE%\Documents\VIA\UserFlow Projects`
- zuletzt verwendeter Pfad wird gespeichert.
- portables Szenario kann später explizit unterstützt werden.

### Templates

Trennen:

- mitgelieferte Templates: read-only Ressourcen
- Benutzer-Templates: `%LOCALAPPDATA%` oder Dokumente
- projektbezogene Templates: im Projekt, falls später fachlich vorgesehen

### Logs

Crash- und Diagnose-Logs:

```text
%LOCALAPPDATA%\VIA\UserFlow\Logs
```

Nicht:

```text
AppDomain.CurrentDomain.BaseDirectory\Logs
```

### Migration

Beim ersten Start nach Umstellung:

1. alte Datenpfade erkennen
2. Benutzer informieren
3. Daten kopieren, nicht sofort verschieben
4. erfolgreiche Migration prüfen
5. erst später alte Daten optional bereinigen

---

## 8. `TypeKey` muss stabil sein

`TypeKey` ist Teil des Persistenzvertrags.

Ein CLR-Typname ist kein stabiler Persistenzschlüssel.

### Problem

Beim Speichern darf nicht pauschal gelten:

```csharp
ctrl.TypeKey = ctrl.GetType().Name;
```

Risiken:

- Klassenname wird geändert
- Namespace/Typ wird refaktoriert
- Registry-Key weicht vom Klassennamen ab
- alte Projekte können Controls nicht mehr auflösen
- unterschiedliche Controls können ähnlich benannt sein
- externe Erweiterungen werden erschwert

### Ziel

`TypeKey` stammt ausschließlich aus der Registry bzw. dem Descriptor.

Beispiel:

```csharp
ControlDescriptor descriptor = ControlRegistry.GetByType(control.GetType());
control.TypeKey = descriptor.TypeKey;
```

Noch besser:

- `TypeKey` wird beim Erzeugen gesetzt.
- Speichern verändert ihn nicht.
- Laden validiert ihn.
- Registry unterstützt Aliase für alte Keys.

### Kompatibilitätsstrategie

```csharp
ControlRegistry.RegisterAlias("OldButtonName", "button");
```

### Tests

Für jeden registrierten Controltyp:

1. Instanz erzeugen
2. serialisieren
3. deserialisieren
4. exakten Typ prüfen
5. `TypeKey` prüfen
6. erneut serialisieren
7. semantische Gleichheit prüfen

### Abnahmekriterien

- Umbenennen einer C#-Klasse erfordert keine Projektmigration
- Registry-Key bleibt stabil
- alte TypeKeys können über Alias geladen werden
- Speichern überschreibt keinen gültigen Key mit dem CLR-Namen

---

# TEIL B – SNAPSHOTS UND UNDO/REDO

## 9. Full-Snapshot-Prinzip beibehalten

Das bestehende Memento-/Full-Snapshot-Prinzip bleibt erhalten.

Keine Umstellung auf:

- Command Replay
- Delta-Patches
- Event Sourcing
- komplexe inverse Commands

Grund:

- Objektgraphen sind komplex
- Full Snapshots sind robuster
- bestehende Serializer können weiterverwendet werden
- Undo/Redo bleibt verständlich und reproduzierbar

Optimiert wird der Speicher, nicht das Grundprinzip.

---

## 10. Reiner Temp-Datei-Speicher wird nicht empfohlen

Alle Snapshots ausschließlich auf Disk zu schreiben wäre kein ausreichender Gewinn.

Es reduziert zwar den dauerhaft belegten RAM, verursacht aber:

- zusätzlichen I/O
- langsamere Undo-/Redo-Zugriffe
- Defender-/Antivirus-Zugriffe
- Session-Cleanup
- Fehlerfälle bei Dateioperationen
- keine Lösung für die eigentliche Serialisierungskosten
- weiterhin temporäre JSON-/UTF8-Allokationen

### Ziel: hybrider Speicher

- neue und kleine Snapshots im RAM
- alte und große Snapshots auf Disk
- globales Byte-Budget
- LRU-/FIFO-Auslagerung
- Disk-Payloads pro Session
- sofortiges Löschen nicht mehr referenzierter Payloads

---

## 11. Snapshot-Payload-Abstraktion

Neue generische Abstraktion im Snapshot-Projekt:

```csharp
public interface ISnapshotPayloadStore : IDisposable
{
    SnapshotPayloadHandle Store(ReadOnlyMemory<byte> payload);
    byte[] Load(SnapshotPayloadHandle handle);
    void Delete(SnapshotPayloadHandle handle);

    long InMemoryBytes { get; }
    long OnDiskBytes { get; }
}
```

Handle:

```csharp
public sealed record SnapshotPayloadHandle(
    Guid Id,
    int OriginalByteCount,
    int StoredByteCount,
    SnapshotStorageKind StorageKind);
```

StorageKind:

```csharp
public enum SnapshotStorageKind
{
    Memory,
    Disk
}
```

### Architekturgrenze

`VIA.Mockup.Snapshots` kennt weiterhin keine Mockup-Typen.

---

## 12. Speicherstrategie

### Startwerte, später per Messung anpassen

- RAM-Budget: 64 MB
- Disk-Budget pro Session: 512 MB
- einzelne Payload ab 2–4 MB direkt auf Disk
- neueste 5–10 Snapshots bevorzugt im RAM
- älteste RAM-Snapshots zuerst auslagern
- bei Überschreiten des Disk-Budgets älteste History-Einträge entfernen

### History-Limits je Kontext

Nicht ein globaler Wert für alles.

Vorschlag:

| Kontext | Startlimit |
|---|---:|
| Project | 10 |
| Screen | 40 |
| Templates Collection | 10 |
| Template | 30 |
| Popup | 30 |

Die tatsächlichen Werte werden nach Messung festgelegt.

### History pro Objekt-ID

Entscheidung explizit treffen:

#### Variante A – aktuelle History

Beim Objektwechsel wird History verworfen.

Vorteil:

- geringer Speicher
- einfacher

Nachteil:

- überraschendes UX-Verhalten

#### Variante B – History pro Objekt-ID

Empfohlen für ein professionelles Designwerkzeug:

```text
ScreenHistory[ScreenId]
TemplateHistory[TemplateId]
PopupHistory[PopupId]
```

Dazu:

- globales Budget statt unbegrenzter Stacks
- LRU für inaktive Objekt-Historien
- History beim Löschen eines Objekts entfernen

---

## 13. Snapshot-Metriken

Vor und nach jedem Umbau messen:

- Serialisierungszeit
- Kompressionszeit
- komprimierte Bytes
- unkomprimierte Bytes
- Restore-Zeit
- Anzahl Snapshots
- RAM-Payloads
- Disk-Payloads
- BackgroundImage-Payloads
- Duplikat-Unterdrückungen
- History-Evictions

Diagnosemodell:

```csharp
public sealed record SnapshotDiagnostics(
    int EntryCount,
    long InMemoryBytes,
    long OnDiskBytes,
    long BackgroundPayloadBytes,
    TimeSpan LastSerializeDuration,
    TimeSpan LastRestoreDuration);
```

Keine detaillierte Telemetrie extern versenden. Zunächst rein lokal und optional sichtbar unter Diagnostics.

---

## 14. BackgroundImage-Payload-Cache begrenzen

Der Snapshot-Serializer ersetzt Base64-Bilder durch Tokens und hält die Payloads in einem statischen Cache.

Dieses Prinzip reduziert Snapshot-Duplikate, benötigt aber einen klaren Lifecycle.

### Problemfelder

- statischer Cache kann über Projektwechsel hinweg wachsen
- keine offensichtliche Byte-Begrenzung
- keine Referenzzählung
- Payload kann im Cache bleiben, obwohl kein Snapshot mehr darauf verweist
- Disk-Auslagerung der JSON-Payload löst dieses Problem nicht

### Ziel: Content-Addressable Blob Store

Key:

```text
SHA-256 + ByteLength
```

Blob:

```csharp
public sealed record SnapshotBlobHandle(
    string Hash,
    int ByteLength,
    int ReferenceCount,
    SnapshotStorageKind StorageKind);
```

### Regeln

- identische Bilder werden nur einmal gespeichert
- Referenzzählung beim Snapshot Push/Remove
- Null-Referenzen werden entfernt
- große Bilder auf Disk
- Store wird beim Projektwechsel/Shutdown sauber geschlossen
- verwaiste Session-Blobs werden beim nächsten Start entfernt

### Nicht tun

- `BackgroundImageBase64` global aus Projektpersistenz entfernen
- normales Projektformat beschädigen
- Token aus Snapshot-JSON in normale Projektdateien übernehmen

---

## 15. Snapshot-Ausführung und UI-Thread

Die Vollserialisierung ist der eigentliche Hotspot.

### Sichere Reihenfolge

#### Phase 1

- nur messen
- bestehendes synchrones Verhalten beibehalten
- unnötige doppelte Serialisierung entfernen

#### Phase 2

- Snapshot-Datenmodell unter UI-Thread konsistent erfassen
- danach Kompression/Hashing optional im Hintergrund
- kein paralleles Iterieren veränderlicher ObservableCollections

#### Phase 3

Optional:

- dedizierte Snapshot-DTOs oder immutable Capture-Struktur
- erst nach gründlichen Tests

### Nicht tun

- aktuelle Model-Collections ungeschützt auf Background-Thread serialisieren
- Snapshot nach Mutation erstellen, wenn Undo den Vorzustand benötigt
- MouseMove bei jedem Pixel als Full Snapshot erfassen

---

# TEIL C – PREVIEWS, TOOLBOX UND RENDERING

## 16. Control-Previews

### Entscheidung

Controltypen erhalten feste, versionierte Previewbilder.

Empfohlen:

- PNG für kleine realistische Preview
- optional SVG, wenn der vorhandene Image-Pfad stabil unterstützt wird
- einheitliche Previewgröße
- Light-Theme als Baseline
- Dark-Variante nur, wenn die Toolbox später wirklich ein Dark Theme erhält

### Gründe

- keine DesignControl-Instanzen in der Toolbox
- kein Skia-Paint pro Listeneintrag
- keine Property-Initialisierung
- konsistente Vorschau
- weniger UI-Thread-Arbeit

### Build-/Pflegeprozess

Langfristig nicht manuell Screenshots pflegen.

Optionales internes Tool:

```text
VIA.UserFlow.PreviewGenerator
```

Es erzeugt alle Previewbilder reproduzierbar aus Registry und Defaultwerten.

### Cache

`BitmapImage`:

- einmal laden
- `CacheOption=OnLoad`
- `Freeze()`
- keyed cache nach Pack-URI

---

## 17. Template-Previews

Manuell fest gespeicherte PNGs sind für bearbeitbare Templates ungeeignet.

### Ziel

Templates erhalten generierte, gecachte Thumbnails.

Ablauf:

1. Template-Inhalt ändert sich.
2. Template-Revision erhöhen.
3. Thumbnail-Service erhält gezielte Invalidation.
4. Thumbnail einmalig rendern.
5. eingefrorenes `BitmapSource` cachen.
6. Pickliste zeigt normales `Image`.

### Cache-Key

```text
TemplateId
+ ContentRevision
+ PixelWidth
+ PixelHeight
+ ThemeVariant
+ RendererVersion
```

### Persistenter Cache

Optional:

```text
%LOCALAPPDATA%\VIA\UserFlow\Cache\Templates
```

Cache-Datei ist abgeleitet und darf jederzeit gelöscht werden.

### Verbindliche Änderung

`TemplateScreenPreview` mit einem eigenen `SKElement` je Eintrag soll langfristig aus der Pickliste verschwinden.

Der Renderer darf als Service weiterexistieren.

---

## 18. Screen-Thumbnails

### Problem

Globale `InvalidatePreviewMessage`-Nachrichten führen dazu, dass jede Thumbnail-Instanz reagiert und der gemeinsame Cache vollständig geleert werden kann.

### Ziel: Entity-bezogene Invalidierung

Neue Message:

```csharp
public sealed record InvalidatePreviewMessage(
    PreviewEntityKind EntityKind,
    long EntityId,
    long Revision,
    PreviewInvalidationReason Reason);
```

EntityKind:

```csharp
Screen,
Template,
Popup,
Project,
All
```

### Regeln

- Screenänderung invalidiert nur diesen Screen
- Templateänderung nur dieses Template
- Themeänderung invalidiert betroffene Inhalte
- globale Invalidierung nur bei seltenen globalen Änderungen
- MouseMove invalidiert nur Designer, nicht alle Thumbnails
- Preview wird bevorzugt bei abgeschlossener Aktion auf MouseUp aktualisiert

### Cache-Key

Der Cache-Key darf nicht nur einige Screen-Basiseigenschaften enthalten.

Empfehlung:

```text
EntityId
+ ContentRevision
+ PixelSize
+ ThemeRevision
+ RendererVersion
```

### ContentRevision

Jede relevante Modelländerung erhöht eine nicht persistierte oder bewusst persistierte Revision.

Alternative:

- zentraler PreviewRevisionProvider im ViewModel

Wichtig:

- keine teuren Objektgraph-Hashes bei jedem Paint
- Revision bei Mutation aktualisieren

---

## 19. Virtualisierung der Picklisten

PNG-/Bitmap-Previews allein reichen nicht.

### Prüfen

- äußerer `ScrollViewer`
- `ItemsControl` statt virtualisierbarer `ListBox`
- Gruppierung
- WrapPanel
- unbegrenzte Measure-Pässe
- verschachtelte ScrollViewer
- Recycling

### Zielwerte

```xml
VirtualizingPanel.IsVirtualizing="True"
VirtualizingPanel.IsVirtualizingWhenGrouping="True"
VirtualizingPanel.VirtualizationMode="Recycling"
ScrollViewer.CanContentScroll="True"
```

Für Wrap-Layouts:

- vorhandenen `VirtualizingWrapPanel` konsequent verwenden
- nur einen Scroll-Owner
- keine unendliche Höhe vom äußeren Layout

### Tests

- 70 Controls
- 200 Templates
- schnelles Scrollen
- Gruppen auf/zu
- Suche und Filter
- Speicher vor/nach 20 Scroll-Durchläufen
- keine stetig wachsende Anzahl visueller Elemente

---

## 20. Renderer- und Cache-Lifecycle

Jeder Cache benötigt:

- eindeutigen Owner
- Größenlimit
- Byte- oder Entry-Budget
- Eviction-Strategie
- Clear bei Projekt-/Themewechsel
- Dispose nativer Ressourcen
- Diagnosewerte

Keine statischen, unbegrenzten Dictionaries ohne Lifecycle.

---

# TEIL D – AUTOSAVE UND DIRTY-STATE

## 21. Dirty-State einführen

Aktuell soll Speichern nicht reflexartig nach jeder kleinen Mutation erfolgen.

### Ziel

Getrennte Zustände:

- `IsDirty`
- `IsSaving`
- `LastSavedAt`
- `LastSaveError`
- `HasExternalFileChange`

### Verhalten

- Mutation setzt `IsDirty=true`
- Undo/Redo setzt `IsDirty=true`
- erfolgreicher Save setzt `IsDirty=false`
- fehlgeschlagener Save lässt `IsDirty=true`
- Statusleiste zeigt:
  - Saved
  - Unsaved changes
  - Saving...
  - Save failed

---

## 22. Debounced Autosave

### Ziel

Autosave nach Ruhephase, nicht nach jeder Aktion.

Vorschlag:

- 2–5 Sekunden nach letzter abgeschlossener Mutation
- während Drag/Resize kein Save
- MouseUp markiert Änderung abgeschlossen
- neuer Save ersetzt noch nicht gestarteten Autosave
- laufender Save wird nicht parallel dupliziert

### Single-Writer-Regel

Nur eine Save-Pipeline schreibt eine Projektdatei.

Konzept:

```csharp
IProjectSaveCoordinator
```

Verantwortung:

- Requests koaleszieren
- Snapshot/DTO konsistent erfassen
- atomisch schreiben
- Dirty-State verwalten
- Fehler melden
- Shutdown-Flush

### Wichtig

Kein paralleles Serialisieren veränderlicher Collections.

Sichere Varianten:

1. UI-Thread erzeugt immutable DTO, Background schreibt.
2. UI-Thread serialisiert, Background schreibt Bytes.
3. zunächst komplett synchron, aber debounced.

Mit Variante 3 beginnen, danach messen.

---

## 23. Externe Dateiänderungen

Optional später:

- `FileSystemWatcher`
- extern geänderte Projektdatei erkennen
- nicht automatisch überschreiben
- Dialog:
  - Reload
  - Save As
  - Ignore
  - Compare später

Nicht Bestandteil der ersten Save-Phase.

---

# TEIL E – DESIGNER-VIEWPORT UND KOORDINATEN

## 24. Verweis auf UI- und Viewport-Plan

Die fachliche Entscheidung ist bereits getroffen:

- vollständiger Viewport
- zentrierte Device Area
- zoombarer Workspace
- persistierte Control-Koordinaten bleiben Device-lokal

Verbindliche Detailplanung steht in:

- `UI_BRUSHUP_MASTERPLAN.md`
- `#DOCUMENTATION/Designer_Viewport_Umbau.md`

Dieser technische Plan ergänzt nur die Sicherheitsregeln.

---

## 25. Zentrale Transformationsquelle

Eine Instanz ist verantwortlich für:

- Viewport → Workspace
- Workspace → Device
- Device → Workspace
- Workspace → Viewport
- Zoom
- Device-Offset
- optional Pan

### Alle Pfade müssen dieselbe Quelle verwenden

- Rendering
- HitTesting
- Drag/Drop
- Resize
- Rubberband
- Guidelines
- Selection Adorner
- Context Menu
- Mouse position
- ActionArea
- Popup
- Template Designer

### Tests

Parameterisiert für:

- Zoom 25 %, 50 %, 100 %, 150 %, 200 %
- Device-Offset
- Scroll/Pan
- negative Device-Koordinaten
- Controls außerhalb der Device Area
- Mobile und Desktop

---

# TEIL F – LIVE PREVIEW UND POPUPS

## 26. Popup-LivePreview vervollständigen

Der bereits vorhandene Popup-Plan wird verbindlich umgesetzt.

### Ziel

```csharp
ObservableCollection<PreviewPopupInstance> PreviewPopupStack
```

Instanz enthält:

- Popup-ID oder Referenz
- Position
- Anchor
- CloseOnOutsideClick
- DimBackground
- optional Z-/Modal-Verhalten

### Verbindliche Regeln

- Runtime-Stack wird nicht in normale Projektdatei persistiert.
- Render und HitTest verwenden dieselbe `ComputePopupRect`-Methode.
- Topmost Popup erhält HitTest zuerst.
- Klick außerhalb kann Top-Popup schließen.
- Navigation aus Popup schließt Popup nach definierter Regel.
- Popup kann weiteres Popup öffnen.
- Screen darunter ist bei modalem Popup nicht klickbar.

### Triggerposition

`ActionAreaTriggerMessage` muss bei `MousePos` die Preview-/World-Koordinate transportieren.

Keine globale Abfrage der aktuellen Mausposition als Ersatz.

### Tests

- Center
- Left
- Right
- Top
- Bottom
- MousePos
- Clamp
- Outside click
- Popup in Popup
- Navigate
- Home
- Back
- Open URL/File
- Stack schließen

---

# TEIL G – FLOWVIEW ALS PRODUKTFUNKTION

## 27. Zielbild

`FlowView` wird vom Platzhalter zur eigentlichen User-Flow-Übersicht.

### MVP

- Screen-Knoten
- Screenname
- Thumbnail
- Home-Badge
- Kanten aus ActionAreas
- Kantenbeschriftung mit Aktion/Trigger
- Zoom und Pan
- Auswahl eines Knotens öffnet Screen
- Doppelklick wechselt zum Designer

### Validierungen

- fehlendes Ziel
- gelöschter Screen referenziert
- nicht erreichbare Screens
- kein Home-Screen
- mehrere Home-Screens
- Action ohne Ziel
- Popup-ID ungültig
- externe Datei/URL leer
- zyklische Navigation nur als Information, nicht automatisch Fehler

### Phasen

#### Flow MVP

- automatische Anordnung
- read-only Graph
- Navigation zum Screen

#### Flow Editing

Später:

- manuelles Positionieren
- gespeicherte Flow-Positionen
- Kanten bearbeiten
- ActionArea direkt öffnen

#### Flow Analysis

Später:

- Reachability
- Dead Ends
- Orphans
- Pfad zum Ziel
- Export als Bild/PDF

### Persistenz

Graphpositionen sind Metadaten, nicht Screen-Koordinaten.

Vor Einführung klären:

- projektweit persistieren
- oder reine View-Einstellung

Keine Schatten-Navigation unabhängig von ActionAreas aufbauen.

---

# TEIL H – SKIASHARP UND EVENT-LIFECYCLE

## 28. SkiaSharp-Ressourcen-Audit

Alle nativen Objekte prüfen:

- `SKPaint`
- `SKPath`
- `SKBitmap`
- `SKImage`
- `SKSurface`
- `SKData`
- `SKShader`
- `SKImageFilter`
- `SKPathEffect`
- `SKTypeface`
- `SKFont`

### Regeln

- lokale temporäre Ressourcen mit `using`
- gecachte Ressourcen mit klarer Dispose-Verantwortung
- statische native Ressourcen vermeiden oder App-Lifecycle zuordnen
- Bitmap/Image nicht gleichzeitig mehrfach besitzen
- keine dispose-ten Objekte im Cache halten

### Audit-Ergebnis dokumentieren

Tabelle:

| Datei | Ressource | Owner | Dispose-Ort | Risiko |
|---|---|---|---|---|

---

## 29. Eventhandler und Messenger

Prüfen:

- `OnApplyTemplate`
- `Loaded`
- `Unloaded`
- `SizeChanged`
- `PaintSurface`
- Mouse Events
- Messenger-Registrierungen
- Timer
- FileSystemWatcher später

### Regeln

- alte `PART_Canvas`-Handler vor neuem Template entfernen
- Events nicht mit anonymen Lambdas registrieren, wenn sie entfernt werden müssen
- `PaintSurface` bei Dispose/Unload abmelden, wenn Control wiederverwendet werden kann
- Messenger bei Unload abmelden
- AutoSaveTimer bei Shutdown stoppen
- keine mehrfachen Registrierungen nach erneutem Load

### Tests

- View 100-mal wechseln
- Designer-Template erneut anwenden
- Toolbox öffnen/schließen
- Template-Liste wechseln
- Speicher und Handleranzahl beobachten
- keine mehrfach ausgelösten Aktionen

---

# TEIL I – NULLABLE, FEHLERBEHANDLUNG UND CODEHYGIENE

## 30. Nullable-Warnungen schrittweise aktivieren

Keine globale Sofortumstellung des gesamten Projekts.

### Reihenfolge

1. `Mockup.Snapshots`
2. `Mockup.Guidelines`
3. `SnapshotIntegration`
4. `Storage`
5. `Registry`
6. `Rendering`
7. `Designer`
8. ViewModel
9. Views/Code-behind

### Regeln

- Warnungen nicht pauschal erneut unterdrücken
- `!` nur mit nachvollziehbarer Invariante
- Try-Pattern für unsichere Lookups
- CurrentProject/CurrentScreen-Zustände explizit modellieren
- `ArgumentNullException.ThrowIfNull` an echten API-Grenzen
- keine unnötigen Nullprüfungen in Hotpaths ohne Messung

---

## 31. TODO- und toter Code

Kommentierte komplette Altimplementierungen entfernen, sobald:

- neue Implementierung bestätigt ist
- Git-Historie verfügbar ist
- keine noch benötigten Details enthalten sind

Beispielbereiche:

- alte Thumbnail-Implementierungen
- überholte Renderer
- auskommentierte XAML-Blöcke
- Placeholder-Commands

Nicht während einer funktionalen Hauptphase nebenbei großflächig bereinigen.

Eigene Cleanup-Commits verwenden.

---

# TEIL J – TESTS UND MESSUNGEN

## 32. Testprojekte anlegen

Empfohlene Projekte:

```text
VIA.Mockup.Snapshots.Tests
VIA.Mockup.Guidelines.Tests
VIA.Mockup.Serialization.Tests
VIA.Mockup.Rendering.Tests
VIA.Mockup.ViewModel.Tests
```

Nicht jedes Projekt muss sofort entstehen.

Start:

1. Snapshots
2. Guidelines
3. Serialization
4. Storage
5. Transformationslogik

### Framework

Vor neuer Dependency Entscheidung prüfen:

- bestehende Firmenstandards
- MSTest, xUnit oder NUnit

Keine Testframework-Wahl still treffen, wenn im Unternehmen ein Standard existiert.

---

## 33. Pflicht-Testgruppen

### Snapshot

- Push
- Undo
- Redo
- Duplikaterkennung
- History-Limit
- RAM-Budget
- Disk-Spill
- Eviction
- Session-Cleanup
- BackgroundImage-Deduplizierung
- Referenzzählung

### Serialisierung

- Project Roundtrip
- Screen Roundtrip
- Template Roundtrip
- Popup Roundtrip
- alle Controltypen
- TypeKey-Alias
- alte JSON-Felder
- unbekannte Properties
- BackgroundImageBase64

### Storage

- atomisches Schreiben
- Backup
- Recovery
- Read-only
- ungültiger Pfad
- fehlender Ordner
- SaveResult
- Dirty-State

### DesignerTransform

- alle Koordinatenrichtungen
- Zoom
- Offset
- Roundtrip
- HitTest
- Drag/Drop
- Resize
- Guidelines

### Preview

- gezielte Invalidation
- Cache-Key
- Revision
- Themewechsel
- Cache-Eviction
- kein globales Neurendern bei Screenänderung

---

## 34. Performance-Baseline

Vor Optimierung messen:

- App-Startzeit
- Projektladezeit
- ProjectView mit 10/50/200 Screens
- Toolbox mit allen Controls
- Templates mit 10/50/200 Einträgen
- Snapshot Project
- Snapshot Screen
- Undo Project
- Undo Screen
- Save
- Autosave
- Preview-Invalidierung
- Memory nach 10 Minuten Bearbeitung
- Memory nach 100 Undo-Schritten
- Memory nach 100 View-Wechseln

### Testprojekte

- Small
- Medium
- Large
- Image-heavy
- Template-heavy
- Desktop-heavy

### Messregeln

- Release
- x64
- gleicher Rechner
- gleiche Daten
- Warm- und Cold-Run unterscheiden
- mindestens mehrere Wiederholungen
- Median statt Einzelwert

### Keine erfundenen Prozentwerte

Performancegewinn erst nach Messung dokumentieren.

---

# TEIL K – UMSETZUNGSREIHENFOLGE

## 35. Empfohlene Gesamtphasen

## Phase T0 – Baseline und Testdaten

- aktuelle Builds
- Testprojekte sichern
- Performance-Baseline
- Storage-/Snapshot-Diagnose ergänzen
- keine Verhaltensänderung

## Phase T1 – Speichersicherheit

- leere Catch-Blöcke entfernen
- `StorageResult`
- atomisches Schreiben
- Backup
- korrekte Erfolgsmeldungen
- Logs nach LocalAppData

## Phase T2 – `TypeKey` und Serialisierung

- Registry als Quelle
- kein CLR-Name beim Speichern
- Aliase
- Roundtrip-Tests
- alte Projekte prüfen

## Phase T3 – Benutzerpfade und Migration

- LocalAppData-Struktur
- Documents-Projektordner
- Migration alter Daten
- Path-UI im ProjectDialog

## Phase T4 – gezielte Preview-Invalidierung

- EntityKind/EntityId/Revision
- ScreenThumbnail-Cache
- keine globalen Clears
- Tests und Messung

## Phase T5 – Picklisten-Previews

- vollständige Control-PNGs
- TemplateThumbnailService
- `TemplateScreenPreview` aus Pickliste entfernen
- Virtualisierung
- Cache-Budgets

## Phase T6 – Snapshot-Metriken und Blob-Store

- Byte-Metriken
- BackgroundImage Blob Store
- Referenzzählung
- Cleanup
- noch kein Disk-Spill, bis Messwerte vorliegen

## Phase T7 – hybrider Snapshot-Store

- Payload-Abstraktion
- RAM-Budget
- Disk-Spill
- Session-Verzeichnis
- Eviction
- History pro Objekt-ID entscheiden

## Phase T8 – Autosave und Dirty-State

- Dirty-State
- Debounce
- Single Writer
- Shutdown Flush
- Save-Fehlerzustand

## Phase T9 – UI Brushup

- `UI_BRUSHUP_MASTERPLAN.md`
- Workbench-Tokens
- ProjectView
- ProjectDialog
- Grid entfernen
- ScreenView

Hinweis:

- P0-Speichersicherheit sollte vor großem UI-Umbau erfolgen.
- Rein visuelle Token-Arbeit kann parallel vorbereitet werden.
- Grid-Entfernung und Designer-Viewport erst nach Baseline-Tests.

## Phase T10 – Designer-Viewport

- zentraler Transform
- ScreenDesigner
- TemplateDesigner
- PopupDesigner
- Regressionstests

## Phase T11 – Popup-LivePreview

- PopupStack
- Triggerposition
- Render/HitTest
- Interaktionen
- Tests

## Phase T12 – FlowView MVP

- Graphmodell aus ActionAreas
- Knoten/Kanten
- Validierung
- Navigation zum Designer

## Phase T13 – Resource-/Nullable-Audit

- Skia
- Events
- Messenger
- Timer
- Nullable schrittweise
- toter Code

## Phase T14 – Release-Härtung

- vollständiger Release/x64-Test
- Langzeittest
- Datenmigration
- Backup/Recovery
- Performancevergleich
- Dokumentation

---

## 36. Was parallel möglich ist

### Parallel möglich

- Farbtoken-Entwurf und Snapshot-Metriken
- Control-PNG-Erstellung und Storage-Tests
- FlowView-Konzept und TypeKey-Tests
- Dialog-Mockups und Cache-Diagnose

### Nicht parallel im gleichen Codebereich

- Designer-Viewport und große MouseHandler-Refactorings
- Grid-Entfernung und Transformationsumbau ohne Zwischenbuild
- Snapshot-Disk-Spill und Serializer-Formatänderung
- Autosave-Pipeline und atomisches Speichern im selben ungetesteten Schritt
- globale Preview-Message-Änderung und Thumbnail-/Template-Komplettumbau ohne Zwischenphase

---

# TEIL L – DEFINITION OF DONE

## 37. Technische Gesamtabnahme

Der technische Verbesserungsplan gilt als abgeschlossen, wenn:

### Daten

- Speicherfehler werden nicht verschluckt
- Projektdateien werden atomisch geschrieben
- Backups können wiederhergestellt werden
- Benutzerpfade liegen an geeigneten Orten
- bestehende Projekte laden weiterhin

### Persistenz

- `TypeKey` ist stabil
- CLR-Klassennamen sind keine primäre Persistenz-IDs
- Roundtrip-Tests decken alle Controltypen ab

### Snapshots

- Full-Snapshot-Prinzip bleibt erhalten
- RAM-Verbrauch ist budgetiert
- große/alte Payloads können ausgelagert werden
- BackgroundImage-Payloads sind begrenzt und dedupliziert
- Undo/Redo ist reproduzierbar
- History-Verhalten bei Objektwechsel ist bewusst definiert

### Previews

- Control-Pickliste nutzt feste Bilder
- Template-Pickliste nutzt gecachte Thumbnails
- Virtualisierung funktioniert
- Änderung eines Screens invalidiert nicht alle Screens
- Cachegrößen sind begrenzt

### Autosave

- Dirty-State ist korrekt
- keine parallelen Writer
- Autosave blockiert nicht bei jeder kleinen Aktion
- Save-Fehler bleiben sichtbar

### Designer

- Viewport und Device Area sind getrennt
- Transformationsquelle ist zentral
- HitTest/Rendering/DragDrop stimmen bei allen Zoomstufen überein
- alte Koordinaten bleiben kompatibel

### Produktfunktionen

- Popup-LivePreview ist stackfähig
- FlowView zeigt reale Navigationen
- ungültige Flows werden erkannt

### Stabilität

- Skia-Ressourcen besitzen klare Owner
- Eventhandler werden korrekt abgemeldet
- wiederholter Viewwechsel erzeugt keine Mehrfachaktionen
- Nullable-Warnungen werden schrittweise reduziert

### Qualität

- automatisierte Tests existieren für kritische Kernbereiche
- Performance wurde in Release/x64 gemessen
- Verbesserungen sind durch Vorher-/Nachher-Werte belegt
- keine großen ungetesteten Sammelcommits

---

# TEIL M – STARTPROMPT FÜR EINEN NEUEN CHAT

```text
Bitte lies zuerst:

1. #CHAT-OVERVIEW/CHAT-OVERVIEW.md
2. den neuesten VS2AI-Export
3. UI_BRUSHUP_MASTERPLAN.md
4. TECHNICAL_IMPROVEMENT_MASTERPLAN.md
5. Designer_Viewport_Umbau.md

Der aktuelle VS2AI-Export ist die technische Wahrheit.

Wir arbeiten strikt phasenweise. Beginne nur mit Phase T0:
Baseline, Testdaten und Diagnosewerte. Ändere noch kein sichtbares Verhalten.

Wichtige Architekturregeln:

- VIA.Mockup.Snapshots bleibt mockup-typfrei.
- Full-Snapshot/Memento bleibt erhalten.
- Keine Persistenz- oder Public-API-Änderung ohne Rückfrage.
- Keine leeren Catch-Blöcke in Storage-Pfaden.
- Keine falschen Erfolgsmeldungen.
- TypeKey stammt aus Registry/Descriptor, nicht aus dem CLR-Klassennamen.
- Keine globale Preview-Invalidierung im Hotpath.
- Control-Pickliste verwendet feste Images.
- Template-Pickliste verwendet generierte gecachte Thumbnails.
- Snapshot-Speicher wird später hybrid, nicht vollständig diskbasiert.
- Projektdateien müssen atomisch gespeichert werden.
- Bestehende Projektdateien müssen unverändert ladbar bleiben.
- BackgroundImageBase64 darf nicht aus der normalen Projektpersistenz verschwinden.
- UI-Thread-Sicherheit bei Serialisierung und Autosave beachten.
- Designer-Viewport erst nach zentraler Transformationsschicht umbauen.
- Build/Test nur als erfolgreich melden, wenn tatsächlich ausgeführt.

Prüfe zuerst die vollständigen aktuellen Dateien für T0 und nenne:

1. den kleinsten sicheren Änderungssatz,
2. die benötigten Dateien,
3. die Messwerte, die ergänzt werden,
4. die manuellen Testschritte,
5. mögliche Risiken.

Setze danach ausschließlich T0 um.
```
