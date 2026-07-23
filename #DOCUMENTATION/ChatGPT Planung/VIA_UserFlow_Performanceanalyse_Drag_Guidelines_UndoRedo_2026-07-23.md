# VIA.UserFlow – Performanceanalyse für Drag, Guidelines, Undo/Redo und allgemeine Laufzeit

**Stand:** 23.07.2026  
**Analysierter VIA.UserFlow-Stand:** Branch `guideline-restore`, Commit `23fde21811d7bed274cd0b5b92fe01fe5569fd44`  
**Analysierter VIA.WPF-Stand:** Branch `dialog-service-foundation`, Commit `af67b042f5bd2bfbc4bf45c001e5b074d4def20f`  
**Ziel:** Konkrete, quellcodebasierte Maßnahmen für flüssigeres Drag/Resize, effizientere Guidelines, schnelleres Undo/Redo und eine belastbare allgemeine Performance-Strategie.

---

## 1. Ergebnis in einem Satz

Der größte Performancegewinn entsteht nicht durch eine isolierte Mikrooptimierung der Guideline-Berechnung, sondern durch die Entkopplung einer Mausgeste von laufenden Model-, PropertyEditor-, Rendering-, Snapshot- und Speicheroperationen.

Die fachlich beste Zielarchitektur ist:

1. Eine Drag-/Resize-Geste wird als **eine Edit-Transaktion** behandelt.
2. Während der Geste wird möglichst nur transienter Interaktionszustand geändert.
3. Es wird höchstens einmal pro Bildschirmframe gezeichnet.
4. Statischer Canvas-Inhalt wird während der Geste wiederverwendet.
5. Das Model wird auf `MouseUp` einmalig übernommen.
6. Undo erhält genau einen Vorher-Snapshot pro Transaktion.
7. Speichern erfolgt dirty-state-basiert und debounced, nicht synchron innerhalb von Undo/Redo.
8. Guideline-Ziele und Renderreihenfolgen werden pro Interaktion beziehungsweise Revision gecacht.

---

## 2. Priorisierte Hauptbefunde

| Priorität | Befund | Wirkung |
|---|---|---|
| P0 | `PropertyEditor` aktualisiert bei jeder einzelnen `PropertyChanged`-Nachricht alle sichtbaren Property-Einträge | Sehr hohe CPU- und Binding-Last bei Drag/Resize, besonders bei Multiselect |
| P0 | Drag und Resize ändern `X`, `Y`, `Width` und `Height` fortlaufend im Observable Model | Mehrere Notifications pro Control und MouseMove; weitere Folgearbeit in UI und Modell |
| P0 | Jeder Drag-Sample invalidiert den vollständigen Skia-Designer | Vollständiger Renderpass für alle Bands und Controls |
| P0 | `TextRenderer` erzeugt für nahezu jeden Text-Render einen neuen `Style`, `TextBlock` und ein komplettes RichTextKit-Layout | Hohe Allokations- und Layoutkosten in jedem vollständigen Paint |
| P0 | Undo/Redo speichert nach dem Restore synchron Projekt oder Templates | Undo/Redo enthält Dateiserialisierung und I/O auf dem UI-Thread |
| P0 | `SaveProject` ruft `RefreshProjectFiles` auf; dabei werden alle `.ufp`-Dateien gelesen und deserialisiert | Ein Undo eines Screens kann zusätzlich den gesamten Projektordner parsen |
| P1 | Der erste echte Drag-/Resize-Schritt erzeugt synchron JSON, UTF-8, SHA-256 und GZip für den Vorher-Snapshot | Spürbarer Ruck direkt beim Beginn der Geste |
| P1 | `Band.RenderControls` sortiert Controls in jedem Paint mehrfach mit LINQ | Wiederkehrende O(n log n)-Arbeit und Allokationen |
| P1 | Guideline-Ankerlisten und einzelne Ergebnislisten werden pro Auswertung neu angelegt | Vermeidbarer Gen0-Druck, aber meist nicht der primäre Engpass |
| P1 | Das Rendern besitzt keine konsequente Viewport-Culling-Strategie | Auch außerhalb des sichtbaren Bereichs liegende Controls können Renderarbeit verursachen |
| P1 | Globales `InvalidateDesignerMessage` trifft jede registrierte Designer-Instanz | Unnötige Repaints bei Undo/Redo und allgemeinen Änderungen |
| P2 | Thumbnail-Cache wird bei `RefreshVisibleThumbnails` vollständig geleert und der Visual Tree rekursiv durchsucht | Allgemeiner UI-Hotspot beim Wechsel in Projektansichten |
| P2 | Snapshot-Background-Payloads werden erst nach vollständiger JSON-Erzeugung per Regex ersetzt | Große Base64-Daten werden trotzdem zunächst als großer String materialisiert |
| P2 | Snapshot-Background-Cache ist statisch und besitzt kein Byte-Budget oder Referenzzählung | Langfristiges RAM-Wachstum bei bildreichen Projekten |
| P3 | `OnApplyTemplate` hängt Handler an `PART_Canvas`, ohne vorher alte Handler zu entfernen | Risiko mehrfacher Handler nach erneutem Template-Aufbau |
| P3 | VIA.WPF.Mockup ist aktuell nur eine Foundation und noch kein Ersatz für den produktiven UserFlow-Designer | Performanceverbesserungen zuerst in VU beweisen, danach neutral extrahieren |

---

## 3. Aktueller Drag-Hotpath

### 3.1 Control-Drag

Der relevante Ablauf liegt in:

- `Mockup/Designer/BaseDesigner.MouseHandler.cs`
- `HandleControlDrag(SKPoint pt)`
- `Mockup/Designer/BaseDesigner.Guidelines.cs`
- `UpdateAlignmentGuidelinesDuringControlDrag(float dx, float dy)`
- `Mockup/Designer/BaseDesigner.Renderer.cs`
- `OnPaintSurfaceNormal(...)`

Pro relevantem `MouseMove` geschieht aktuell:

1. `dx` und `dy` werden aus Startposition und aktueller Mausposition berechnet.
2. Beim ersten Delta über einem Pixel wird synchron ein Full Snapshot erzeugt.
3. Das Gruppendelta wird erneut über alle ausgewählten Controls begrenzt.
4. Für jedes ausgewählte Control werden `X` und `Y` gesetzt.
5. Die Guideline-Bounds der Auswahl werden erneut über alle ausgewählten Controls berechnet.
6. Die gecachten Zielrechtecke werden gegen die bewegten Anker geprüft.
7. Der gesamte Designer wird invalidiert.
8. Der nächste Paint leert die komplette Fläche und zeichnet Bands, Controls, Selection und Guidelines neu.

Vereinfachte Komplexität pro Drag-Sample:

```text
O(S)          Gruppengrenzen
+ O(S)        X-/Y-Mutationen
+ O(S)        Moving-Bounds für Guidelines
+ O(N * A)    Guideline-Vergleiche
+ O(Render)   kompletter Skia-Paint
```

Dabei ist:

- `S` = Anzahl ausgewählter Controls,
- `N` = Anzahl Guideline-Ziele,
- `A` = Anzahl verglichener Ankerkombinationen,
- `Render` = alle sichtbaren Bands und Controls inklusive Textlayout.

### 3.2 Resize

Bei Resize ist der Ablauf ähnlich, aber es werden je nach Handle bis zu vier Observable Properties geändert:

- `X`
- `Y`
- `Width`
- `Height`

Anschließend werden Positions-Guidelines und zusätzlich unabhängige Größen-Snap-Ziele für Breite und Höhe gesucht.

Die Resize-Logik selbst besteht überwiegend aus einfacher Float-Arithmetik. Teurer sind die ausgelösten Notifications, das PropertyEditor-Refresh, die erneute Guideline-Auswertung und der vollständige Renderpass.

---

## 4. Bestätigter Hauptengpass: PropertyEditor während Drag/Resize

### 4.1 Aktuelles Verhalten

`DesignControl` basiert auf `ObservableRecipient`. Die Layoutwerte sind `ObservableProperty`:

```csharp
X
Y
Width
Height
```

Der `PropertyEditor` registriert sich auf `PropertyChanged` aller aktiven Ziel-Controls.

In `PropertyEditor.Control_PropertyChanged(...)` wird unabhängig vom tatsächlich geänderten Property-Namen immer ausgeführt:

```csharp
RefreshVisibleValues();
```

`RefreshVisibleValues()` iteriert durch alle sichtbaren Gruppen und alle Property-Einträge. Jeder Eintrag liest seinen Wert erneut per Reflection und löst mehrere eigene PropertyChanged-Meldungen aus.

Damit entsteht bei einem Drag eines einzelnen Controls ungefähr:

```text
MouseMove
  -> X geändert
     -> PropertyEditor: alle sichtbaren Werte aktualisieren
  -> Y geändert
     -> PropertyEditor: alle sichtbaren Werte erneut aktualisieren
```

Bei Multiselect wächst die Anzahl näherungsweise mit `2 × S`. Bei Resize können es bis zu `4 × S` vollständige Refreshes pro MouseMove werden.

### 4.2 Sofortmaßnahme

Der PropertyEditor darf nicht alle Werte bei jeder Layoutänderung neu laden.

Empfohlen:

1. Property-Namen zu `PropertyItemTemp` indizieren.
2. Nur das tatsächlich geänderte Property aktualisieren.
3. Mehrere Meldungen innerhalb eines UI-Zyklus zusammenführen.
4. Während einer Designer-Interaktion Layout-Refreshes pausieren.
5. Auf `MouseUp` genau einen vollständigen Abgleich durchführen.

Minimaler Coalescing-Ansatz:

```csharp
private readonly HashSet<string> pendingProperties = new();
private bool refreshQueued;

private void Control_PropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    if (string.IsNullOrWhiteSpace(e.PropertyName))
    {
        QueueFullRefresh();
        return;
    }

    pendingProperties.Add(e.PropertyName);
    QueueRefresh();
}
```

Die Ausführung erfolgt einmalig über `Dispatcher.BeginInvoke`, bevorzugt mit `DispatcherPriority.Background` oder nach dem aktuellen Renderframe.

### 4.3 Bessere Zielarchitektur

Während Drag/Resize sendet der Designer einen leichtgewichtigen Interaktionsstatus:

```csharp
DesignerInteractionStarted
DesignerInteractionCompleted
DesignerInteractionCancelled
```

Der PropertyEditor:

- friert während `Started` seine Model-Abfragen ein,
- kann optional nur eine kleine Positionsanzeige aktualisieren,
- führt bei `Completed` einmal `RefreshVisibleValues()` aus.

Das ist eine sehr wirkungsvolle und relativ risikoarme Optimierung.

---

## 5. Model-Mutationen während einer Geste

### 5.1 Aktueller Zustand

Während Drag werden `X` und `Y` jedes ausgewählten Controls fortlaufend verändert. Während Resize werden Geometrie-Properties fortlaufend verändert.

Das Model ist dadurch zwar jederzeit aktuell, aber jede Zwischenposition wird wie eine echte fachliche Mutation behandelt. Die Zwischenwerte müssen weder persistiert noch von Undo/Redo einzeln gesehen werden.

### 5.2 Empfohlene Interaktionssession

Eine Geste sollte einen transienten Zustand besitzen:

```csharp
internal sealed class DesignerInteractionSession
{
    public DesignerInteractionKind Kind { get; init; }
    public IReadOnlyDictionary<DesignControl, ControlGeometry> StartGeometry { get; init; }
    public SKPoint StartMouseWorld { get; init; }

    public float DeltaX { get; set; }
    public float DeltaY { get; set; }
    public ControlGeometry? ResizePreview { get; set; }
}
```

Während `MouseMove`:

- keine oder möglichst wenige Model-Properties ändern,
- nur `DeltaX`, `DeltaY` oder die Preview-Geometrie aktualisieren,
- Guidelines gegen diese Preview-Geometrie berechnen,
- dynamische Controls an der Preview-Position zeichnen.

Auf `MouseUp`:

1. finalen Snap anwenden,
2. finale Geometrie einmal pro Control ins Model schreiben,
3. Parent-Band/Page auflösen,
4. Z-Order normalisieren,
5. PropertyEditor einmal aktualisieren,
6. Dirty-State setzen,
7. einen Render-Commit auslösen.

Auf `Escape` oder Capture-Verlust:

- Session verwerfen,
- Model bleibt unverändert,
- kein Undo-Eintrag.

### 5.3 Vereinbarkeit mit Single Source of Truth

Der transiente Interaktionszustand ist kein zweites persistentes Modell.

Zulässig ist:

- temporäre Preview-Geometrie nur für die Dauer einer aktiven Geste,
- keine gespiegelten Collections,
- kein Speichern dieser Geometrie,
- eindeutiger Commit oder Cancel.

Nach Ende der Geste bleibt das Model/ViewModel die alleinige Source of Truth.

---

## 6. Rendering-Hotpath

### 6.1 Vollständiger Paint pro Invalidation

`InvalidateDesigner()` ruft direkt `PART_Canvas.InvalidateVisual()` auf.

`OnPaintSurfaceNormal(...)` führt anschließend unter anderem aus:

1. komplette Fläche löschen,
2. DPI ermitteln und Canvas skalieren,
3. Layout-Prepass,
4. Bandlisten materialisieren,
5. alle Band-Hintergründe zeichnen,
6. alle Controls zeichnen,
7. Selection-Overlay zeichnen,
8. Rubberband und Designer-Overlay zeichnen,
9. Guidelines zeichnen.

Das ist funktional korrekt, skaliert aber bei textreichen oder controlreichen Screens schlecht.

### 6.2 Maximal ein Paint pro Displayframe

Mouse-Events können häufiger eintreffen als Bildschirmframes. Der Designer sollte Renderanforderungen zusammenführen.

Empfohlene API:

```csharp
private bool renderQueued;

public void RequestDesignerFrame()
{
    if (renderQueued || PART_Canvas == null)
        return;

    renderQueued = true;

    Dispatcher.BeginInvoke(
        () =>
        {
            renderQueued = false;
            PART_Canvas.InvalidateVisual();
        },
        DispatcherPriority.Render);
}
```

Alle Hotpath-Aufrufe während Drag/Resize verwenden `RequestDesignerFrame()` statt direktem `InvalidateVisual()`.

Wichtig: WPF coalesziert bereits einen Teil der Invalidierungen. Daher muss diese Änderung gemessen werden. Der explizite Scheduler bietet aber zusätzlich:

- kontrollierbare Framerate,
- Diagnosezähler,
- später adaptive Reduktion auf 30 FPS bei sehr großen Screens,
- eindeutige Trennung zwischen „State geändert“ und „Frame anfordern“.

### 6.3 Unveränderte Samples überspringen

Das Model wird auf ganze Werte gerundet. Mehrere Mausereignisse können deshalb zur identischen finalen Geometrie führen.

Vor Guideline-Auswertung und Renderanforderung prüfen:

```text
roundedDx == lastRoundedDx
&& roundedDy == lastRoundedDy
```

Bei Resize sollte `ApplyControlResize` einen `bool changed` zurückgeben. Nur bei tatsächlich geänderter Preview-Geometrie werden Guidelines und Paint angefordert.

### 6.4 Renderreihenfolge nicht pro Frame sortieren

`Band.RenderControls(...)` verwendet aktuell getrennte LINQ-Pipelines:

```csharp
Where(...).OrderBy(...)
OfType<ActionArea>().OrderBy(...)
```

Das erzeugt pro Band und Paint Iteratoren und Sortierarbeit.

Empfohlen ist ein abgeleiteter Cache pro `BandPage`:

```csharp
internal sealed class BandPageRenderCache
{
    public int Revision { get; init; }
    public DesignControl[] NormalControlsByZ { get; init; }
    public ActionArea[] ActionAreasByZ { get; init; }
}
```

Invalidierung nur bei:

- CollectionChanged,
- `ZIndex` geändert,
- Controltyp/ActionArea-Zugehörigkeit geändert.

Der Cache ist eine abgeleitete Renderansicht und keine zweite fachliche Collection.

### 6.5 Konsequentes Culling

Aktuell wird primär geprüft, ob ein Control oberhalb des sichtbaren Bereichs endet. Ergänzt werden sollte ein vollständiger Intersection-Test:

```csharp
if (!viewport.IntersectsWith(ctrl.VisualRect))
    continue;
```

Culling sollte gelten für:

- Controls,
- ActionAreas,
- komplexe Charts,
- Bilder,
- textreiche Controls,
- Band-Inhalte.

`RenderContext` sollte dazu eine `ViewportWorldBounds` enthalten.

### 6.6 Statischer Szenencache während Drag/Resize

Der größte langfristige Renderinghebel ist die Trennung in:

- **statische Szene:** Screen, Bands, nicht bewegte Controls,
- **dynamische Ebene:** bewegte/resizte Controls, Selection, Guidelines.

Beim Start der Geste:

1. statische Szene ohne betroffene Controls in `SKPicture` oder `SKImage` erfassen,
2. Cache an Canvasgröße, DPI, Zoom, Scrolloffset, Theme-Revision und Content-Revision binden.

Pro Frame:

1. statische Szene wiedergeben,
2. nur bewegte Controls an Preview-Geometrie zeichnen,
3. Selection und Guidelines zeichnen.

Auf Commit/Cancel:

- Cache disposen,
- vollständigen Paint anfordern.

`SKPicture` ist für vektorielle Wiederholung interessant. `SKImage` kann bei sehr komplexen Controls schneller sein, benötigt aber ein klares Pixel-/Byte-Budget. Die Entscheidung muss anhand einer Messung erfolgen.

### 6.7 Zwei-Surface-Variante

Langfristig ist eine separate Interaktionsfläche noch sauberer:

```text
Base SKElement
  - statische Szene
  - wird nur bei Content-Commit neu gezeichnet

Overlay SKElement
  - Drag/Resize-Preview
  - Selection
  - Rubberband
  - Guidelines
  - Pointer-Hints
```

Diese Lösung sollte erst nach den risikoärmeren Maßnahmen umgesetzt werden.

---

## 7. Textlayout als Renderkostenmultiplikator

### 7.1 Bestätigtes Verhalten

`TextRenderer.DrawInternal(...)` erzeugt bei jedem Aufruf:

1. einen neuen RichTextKit-`Style`,
2. einen neuen `TextBlock`,
3. Textinhalt,
4. ein vollständiges `Layout()`.

Da während Drag der gesamte Designer neu gerendert wird, wird unveränderter Text immer wieder neu gelayoutet.

Der Quellcode enthält zahlreiche `TextRenderer`-Aufrufstellen in Buttons, Inputs, Display-Controls, Charts und Band-UI. Ein textreicher Screen multipliziert damit die Paint-Kosten stark.

### 7.2 Empfohlener Layoutcache

Cache-Key mindestens:

```text
Text
FontFamily
FontSize
FontWeight
Italic
Alignment
AvailableWidth
AvailableHeight
ThemeRevision
RendererVersion
```

Mögliche Ownership:

- bevorzugt pro `DesignControl`, wenn ein Control nur wenige Texte besitzt,
- zentraler bounded LRU-Cache für wiederkehrende Standardtexte,
- Hybrid: Control besitzt zuletzt verwendetes Layout; zentraler Cache für gemeinsame Styles.

Regeln:

- Cache nicht unbegrenzt statisch halten,
- Revision bei text- oder layoutrelevanter Property-Änderung erhöhen,
- Theme-/Fontwechsel invalidiert den Cache,
- keine bereits disposeten nativen Ressourcen referenzieren.

### 7.3 Reihenfolge

Textlayout-Caching sollte nach dem PropertyEditor-Coalescing und Frame-Scheduler kommen. Es ist wahrscheinlich ein großer Hebel, betrifft aber viele Controls und benötigt gute Regressionstests für:

- Ellipsis,
- Padding,
- Wrap,
- Alignment,
- FontWeight,
- FontFamily,
- DPI,
- Größenänderung.

---

## 8. Guideline-Analyse

### 8.1 Bereits gut gelöst

Der aktuelle Stand besitzt wichtige positive Eigenschaften:

- Guidelines sind hostneutral und WPF-frei.
- Zielrechtecke werden während einer Control-Geste gecacht.
- Die Collection wird durch Guideline-Berechnung nicht verändert.
- Der eigentliche Snap wird bei Drag erst auf `MouseUp` angewendet.
- Statische `SKPaint`-Instanzen werden für Guideline-Linien verwendet.
- Resize prüft nur die tatsächlich bewegten Kanten als Moving-Anker.

Diese Entscheidungen sollten erhalten bleiben.

### 8.2 Aktuelle Kosten

Für normalen Drag werden pro Ziel typischerweise verglichen:

```text
3 X-Anker × 3 X-Zielanker
+
3 Y-Anker × 3 Y-Zielanker
=
18 Vergleiche pro Ziel und Sample
```

Bei 100 Ziel-Controls sind das grob 1.800 sehr einfache Float-Vergleiche. Das ist normalerweise günstiger als ein vollständiges Skia-Rendering mit Textlayout.

Zusätzliche Kosten:

- `GetAnchors(...)` erzeugt neue Listen,
- Resize erzeugt kleine Arrays für einzelne Moving-Anker,
- nach einem Treffer wird das Ziel erneut linear per ID gesucht,
- Resize scannt die Targets zusätzlich für Breiten- und Höhen-Snap,
- Ergebnislisten und LINQ-Arrays werden teilweise neu angelegt.

### 8.3 Niedrigrisiko-Optimierungen

1. Statische Anker-Arrays verwenden:

```csharp
private static readonly GuidelineAnchorKind[] XAnchors =
[
    GuidelineAnchorKind.Left,
    GuidelineAnchorKind.Right,
    GuidelineAnchorKind.CenterX
];
```

2. `EvaluateResize` ohne temporäre Ein-Element-Arrays implementieren.
3. Ziel-ID direkt zusammen mit dem Match beziehungsweise Zielrechteck transportieren.
4. Für maximal zwei Linien kleine Arrays statt `List<T>` verwenden.
5. `CreatePositionGuidelineResult` ohne LINQ/`ToArray()` implementieren.
6. Selection für Target-Erzeugung einmal in ein `HashSet<DesignControl>` überführen.
7. Guideline-Ergebnis nur ersetzen und neu zeichnen, wenn es sich tatsächlich geändert hat.

### 8.4 Index für große Screens

Für sehr große Designer kann pro Interaktion ein Index aufgebaut werden:

```text
X:
  Left-Werte sortiert
  Right-Werte sortiert
  CenterX-Werte sortiert

Y:
  Top-Werte sortiert
  Bottom-Werte sortiert
  CenterY-Werte sortiert

Zusätzlich:
  Dictionary<TargetId, GuidelineRect>
```

Die Suche erfolgt per Binary Search nur im Wertebereich:

```text
movingValue - threshold
bis
movingValue + threshold
```

Damit wird aus einer Vollsuche näherungsweise:

```text
O(log N + K)
```

statt:

```text
O(N)
```

`K` ist die kleine Anzahl Kandidaten innerhalb des Thresholds.

Diese Optimierung ist erst sinnvoll, wenn Messungen zeigen, dass Guideline-Auswertung bei großen Screens tatsächlich relevant ist. Für normale Screens haben PropertyEditor und Rendering voraussichtlich deutlich höhere Priorität.

### 8.5 Keine Guideline-Berechnung auf Background-Thread

Die Berechnung selbst ist zwar hostneutral, aber die Datenquelle wird aus aktuell veränderlichen Model-Collections aufgebaut. Ein Background-Thread würde zusätzliche Synchronisations- und Stalenessprobleme erzeugen.

Besser:

- Targets auf UI-Thread einmalig erfassen,
- Index synchron erstellen; bei sehr großen Daten optional aus einer immutable Kopie,
- Auswertung pro Frame schnell und synchron durchführen.

---

## 9. Undo/Redo-Analyse

### 9.1 Aktueller Snapshot-Push

Beim ersten echten Drag- oder Resize-Delta wird synchron ausgeführt:

1. kompletten Kontext als JSON-String serialisieren,
2. Background-Base64 per Regex suchen und ersetzen,
3. JSON erneut als UTF-8-Bytearray erzeugen,
4. SHA-256 berechnen,
5. GZip komprimieren,
6. komprimierte Bytes per `MemoryStream.ToArray()` kopieren,
7. Eintrag in Stack übernehmen.

Positiv:

- nur ein Snapshot pro Geste,
- Full-Snapshot/Memento bleibt robust,
- Duplikatvergleich verwendet Länge und SHA-256 ohne Dekompression,
- Payload wird mit `CompressionLevel.Fastest` komprimiert.

Problem:

- die gesamte Arbeit liegt auf dem UI-Thread,
- sie tritt genau beim Beginn der sichtbaren Geste auf,
- bei bildreichen Screens wird zunächst trotzdem das vollständige Base64-JSON erzeugt.

### 9.2 Aktueller Undo-/Redo-Ablauf

Undo und Redo:

1. serialisieren und komprimieren den aktuellen Zustand für den Gegenstack,
2. dekomprimieren den Ziel-Snapshot,
3. expandieren Background-Tokens,
4. deserialisieren einen neuen Objektgraphen,
5. rekonstruieren Parent-/Project-Referenzen,
6. ersetzen Screen/Template/Popup beziehungsweise das Projekt,
7. speichern Projekt oder Templates synchron,
8. invalidieren Designer global.

Bei `Screen` und `Popup` wird nach dem Restore das gesamte Projekt gespeichert.

### 9.3 Versteckter Zusatzaufwand in `SaveProject`

`SaveProject(...)`:

1. führt Projektkorrekturen über alle Screens/Bands aus,
2. serialisiert das gesamte Projekt,
3. schreibt die Datei synchron,
4. aktualisiert Last-Opened-Settings,
5. ruft `RefreshProjectFiles()` auf.

`RefreshProjectFiles()`:

1. enumeriert alle `.ufp`-Dateien,
2. liest jede Datei vollständig,
3. deserialisiert jedes Projekt, um den Anzeigenamen zu bestimmen,
4. sortiert die Liste,
5. ersetzt die ObservableCollection.

Damit kann ein einzelnes Undo zusätzlich alle Projektdateien im Ordner parsen. Diese Arbeit gehört nicht in den Undo-/Redo-Hotpath.

### 9.4 P0-Maßnahme für Undo/Redo

Restore darf nicht direkt synchron speichern.

Stattdessen:

```text
Undo/Redo Restore
  -> Model ersetzen
  -> IsDirty = true
  -> gezielt UI invalidieren
  -> debounced Save anfordern
```

Speichern nach 2–5 Sekunden Ruhephase oder durch explizites Speichern.

Wichtig:

- Undo/Redo muss sofort im UI sichtbar sein.
- Ein fehlgeschlagenes späteres Speichern lässt `IsDirty = true`.
- Beim Shutdown muss die Save-Pipeline geordnet flushen.
- Es darf nur einen Writer geben.

### 9.5 Projektlisten-Refresh entkoppeln

`RefreshProjectFiles()` nur bei:

- Projekt erstellt,
- Projekt gelöscht,
- Projekt umbenannt,
- Save As,
- externem Refresh,
- Anwendung/Projektansicht initial geladen.

Nach normalem Speichern reicht:

- Zeitstempel des aktuellen `ProjectFileEntry` aktualisieren,
- optional Anzeigenamen des aktuellen Eintrags aktualisieren,
- keine anderen Projektdateien lesen.

### 9.6 Snapshot-Checkpoint statt Snapshot beim ersten Pixel

Der erste Drag-Ruck lässt sich strukturell vermeiden, indem der Vorher-Zustand bereits vorliegt.

Zielkonzept:

```text
Nach jedem Commit:
  aktuellen Kontext als Checkpoint erfassen

Beim Beginn der nächsten Edit-Transaktion:
  vorhandenen Checkpoint als Vorher-Snapshot übernehmen
  keine neue Vollserialisierung im ersten MouseMove

Nach Commit:
  neuen Checkpoint bei Idle erfassen
```

Fallback:

- ist kein gültiger Checkpoint vorhanden, synchron erfassen,
- Checkpoint-Key enthält Kontext, TargetId und ContentRevision.

Dadurch bleibt Full-Snapshot/Memento erhalten, aber die teure Arbeit liegt nicht mehr im ersten sichtbaren Bewegungsschritt.

### 9.7 Snapshot-Payload direkt als UTF-8

Langfristig sollte die Snapshot-Schnittstelle Byte-Payloads unterstützen:

```csharp
ReadOnlyMemory<byte>? SerializeToUtf8(object target, SnapshotContext context);
object? Deserialize(ReadOnlySpan<byte> payload, SnapshotContext context);
```

Vorteile:

- kein JSON-String plus anschließende UTF-8-Kopie,
- `JsonSerializer.SerializeToUtf8Bytes` beziehungsweise `Utf8JsonWriter`,
- Hash und Kompression arbeiten direkt auf Bytes.

Das ist eine API-Änderung in `VIA.Mockup.Snapshots` und muss als eigene Phase erfolgen.

### 9.8 BackgroundImage-Payloads vor der JSON-Materialisierung auslagern

Die aktuelle Regex-Kompaktierung erfolgt zu spät. Das vollständige Base64 wird bereits serialisiert.

Besser:

- snapshot-spezifischer Converter,
- snapshot-spezifisches DTO,
- oder expliziter Blob-Store.

Der Serializer schreibt direkt den Token. Normale Projektpersistenz bleibt unverändert.

Blob-Store-Anforderungen:

- Content Address: SHA-256 + ByteLength,
- Referenzzählung,
- Byte-Budget,
- optional große Blobs auf Disk,
- Clear bei Projektwechsel,
- Cleanup beim Shutdown,
- keine Snapshot-Tokens in normalen Projektdateien.

### 9.9 History-Limit und Budget

Der aktuelle Initialisierungswert ist im analysierten Quellstand `50` für alle Kontexte.

Empfohlen sind kontextspezifische Werte und zusätzlich ein Byte-Budget:

| Kontext | Startwert zur Messung |
|---|---:|
| Project | 10 |
| Screen | 30–40 |
| Templates Collection | 10 |
| Template | 20–30 |
| Popup | 20–30 |

Nicht allein nach Anzahl begrenzen. Ein bildreicher Screen-Snapshot kann um Größenordnungen größer als ein einfacher Popup-Snapshot sein.

---

## 10. Allgemeine Performance-Hotspots

### 10.1 Globale Designer-Invalidierung

Jede geladene `BaseDesigner`-Instanz registriert sich auf `InvalidateDesignerMessage`. Eine globale Nachricht invalidiert alle Empfänger.

Empfohlen:

```csharp
InvalidateDesignerMessage(
    DesignerKind Kind,
    long EntityId,
    DesignerInvalidationReason Reason)
```

Oder lokale direkte Invalidierung, wenn der Sender den betroffenen Designer bereits kennt.

Globale Invalidierung nur bei:

- Themewechsel,
- DPI-/Rendererwechsel,
- wirklich globalen Projektänderungen.

### 10.2 HitTesting

`HitTestControl(...)` sortiert Controls bei Hover über `OrderByDescending(c => c.ZIndex)`.

Optimierung:

- denselben Z-Order-Cache wie beim Rendern verwenden,
- rückwärts durch das bereits sortierte Array laufen,
- keine LINQ-Sortierung pro Hover-Sample.

### 10.3 Thumbnail-System

Positiv:

- Rendering wird per Dispatcher coalesced,
- Cache besitzt ein Entry-Limit,
- `BitmapSource` wird eingefroren.

Probleme:

- Cache-Key enthält keine ContentRevision,
- `RefreshVisibleThumbnails` leert den gesamten Cache,
- der gesamte Visual Tree wird rekursiv nach sichtbaren Thumbnails durchsucht,
- Inhalt kann ohne Key-Änderung stale werden.

Ziel:

```text
ScreenId
+ ContentRevision
+ PixelWidth
+ PixelHeight
+ ThemeRevision
+ RendererVersion
```

Gezielte Invalidierung eines Screens statt globalem Clear.

### 10.4 Asset- und Bildcache

Das aktuelle `ImageRenderer`-Caching ist grundsätzlich sinnvoll:

- SVG-`SKPicture`-Cache,
- begrenzter PNG-Cache,
- Lazy-Loading,
- zentrale Clear-Funktion.

Ergänzen:

- echte Byte-Metriken statt nur Entry-Anzahl,
- definierter Owner/Lifecycle,
- Theme-/Projektwechsel berücksichtigen,
- Native-Dispose-Audit.

### 10.5 Eventhandler-Lifecycle

`BaseDesigner.OnApplyTemplate()` registriert mehrere Handler, darunter auch anonyme Lambdas. Vor einer erneuten Template-Anwendung werden alte Handler nicht sichtbar abgemeldet.

Risiken:

- doppelte Mouse-/Paint-Ausführung,
- schwer messbare Mehrfachinvalidierungen,
- gehaltene alte Template-Parts.

Empfohlen:

1. alten `PART_Canvas` merken,
2. alle benannten Handler entfernen,
3. anonyme Lambdas durch benannte Methoden ersetzen,
4. neue Parts holen,
5. einmalig registrieren.

### 10.6 Storage und Autosave

Der aktuelle `DispatcherTimer` ruft `SaveAll()` auf dem UI-Thread auf. `SaveAll()` serialisiert Settings, Templates und Projekt synchron.

Ziel:

- Dirty-State je Bereich,
- nur geänderte Bereiche speichern,
- debounced statt starres Vollspeichern,
- Single Writer,
- atomische Dateioperation,
- keine Erfolgsmeldung im `finally`,
- kein Projektordner-Rescan nach jedem Save.

### 10.7 Collections und UI-Virtualisierung

Für Listen mit Screens, Templates, Popups und Assets prüfen:

- nur ein ScrollOwner,
- Recycling-Virtualisierung,
- Gruppierung mit `IsVirtualizingWhenGrouping`,
- keine unbeschränkte Measure-Höhe,
- keine Live-Skia-Fläche pro Listeneintrag.

---

## 11. Empfohlene Zielarchitektur

```text
Pointer Input
    |
    v
DesignerInteractionController
    |-- Begin()
    |     |-- StartGeometry erfassen
    |     |-- Undo-Checkpoint referenzieren
    |     |-- GuidelineTargetIndex erstellen
    |     `-- StaticSceneCache erstellen
    |
    |-- Update(pointer)
    |     |-- Preview-Geometrie berechnen
    |     |-- unveränderte Samples verwerfen
    |     |-- Guidelines berechnen
    |     `-- maximal einen Frame anfordern
    |
    |-- Commit()
    |     |-- finalen Snap berechnen
    |     |-- Model einmalig ändern
    |     |-- Undo-Transaktion übernehmen
    |     |-- ContentRevision erhöhen
    |     |-- IsDirty setzen
    |     |-- PropertyEditor einmal aktualisieren
    |     `-- Save debounce anfordern
    |
    `-- Cancel()
          |-- Model unverändert lassen
          `-- transienten Zustand verwerfen
```

Rendering:

```text
DesignerFrameScheduler
    |
    +-- StaticSceneCache wiedergeben
    +-- dynamische Controls zeichnen
    +-- Selection zeichnen
    +-- Guidelines zeichnen
    `-- Diagnosedaten erfassen
```

Persistenz:

```text
Dirty-State
    |
    v
ProjectSaveCoordinator
    |-- Requests koaleszieren
    |-- nur einen Writer zulassen
    |-- konsistent serialisieren
    |-- atomisch schreiben
    `-- Status/Fehler zurückmelden
```

---

## 12. Umsetzungsphasen

## Phase P0 – Messbarkeit ohne Verhaltensänderung

### Dateien

- `BaseDesigner.MouseHandler.cs`
- `BaseDesigner.Renderer.cs`
- `BaseDesigner.Guidelines.cs`
- `PropertyEditor.xaml.cs`
- `MockupSnapshotSerializer.cs`
- `SnapshotEntry.cs`
- `SnapshotManager.cs`
- `MockupViewModel.Snapshots.cs`
- `MockupViewModel.Storage.cs`

### Metriken

Drag/Resize:

- MouseMove-Samples,
- verworfene unveränderte Samples,
- Model-Update-Zeit,
- Anzahl PropertyChanged,
- PropertyEditor-Refresh-Zeit,
- Guideline-Zeit,
- Paint-Zeit,
- Paint-Allokationen,
- Frames pro Sekunde,
- längster Frame.

Snapshot:

- JSON-Serialisierung,
- Background-Kompaktierung,
- UTF-8-Erzeugung,
- Hash,
- Kompression,
- Dekompression,
- Expand,
- Deserialisierung,
- Reconstruct,
- Save,
- `RefreshProjectFiles`.

Messmittel:

- `Stopwatch.GetTimestamp()`,
- `GC.GetAllocatedBytesForCurrentThread()`,
- Visual Studio CPU Usage,
- Visual Studio .NET Object Allocation,
- `dotnet-trace` bei Bedarf.

Keine dauerhaften `Debug.WriteLine`-Fluten pro MouseMove. Zähler sammeln und nach Ende der Geste als eine Zeile ausgeben.

## Phase P1 – Niedrigrisiko-Hotpath-Entlastung

1. PropertyEditor-Refresh coalescen und property-spezifisch machen.
2. PropertyEditor während aktiver Designer-Geste pausieren.
3. Unveränderte gerundete Drag-/Resize-Samples verwerfen.
4. `RequestDesignerFrame()` einführen.
5. LINQ-Sortierung im Paint durch revisionsbasierten Rendercache ersetzen.
6. vollständiges Viewport-Culling ergänzen.
7. HitTest nutzt denselben Z-Order-Cache.
8. globales Invalidate nur dort verwenden, wo wirklich nötig.

Nach jedem Schritt separat messen.

## Phase P2 – Undo/Redo und Save-Hotpath

1. synchrones Speichern aus Restore entfernen.
2. `IsDirty` und debounced Save einführen.
3. Projektordner-Rescan von normalem Save entkoppeln.
4. Snapshot-Phasen diagnostisch aufteilen.
5. History kontextspezifisch und bytebasiert begrenzen.
6. Background-Payload-Lifecycle definieren.

Diese Phase bringt wahrscheinlich den größten sichtbaren Undo-/Redo-Gewinn.

## Phase P3 – Transiente Drag-/Resize-Session

1. `DesignerInteractionSession`.
2. Model während MouseMove nicht mehr mutieren.
3. Rendern aus Startgeometrie + Delta.
4. finaler Model-Commit auf MouseUp.
5. Cancel auf Escape/CaptureLost.
6. Parent-/ZOrder-Logik nach Commit.
7. genau ein PropertyEditor-Refresh.
8. genau eine Dirty-State-Mutation.

## Phase P4 – Statische Szene und Textlayoutcache

1. Textlayout-Metriken und Cache-Key definieren.
2. zunächst nur häufige einfache TextControls cachen.
3. Cache auf weitere Controls ausweiten.
4. `SKPicture` gegen `SKImage` für Interaktionscache messen.
5. dynamisches Overlay einführen, falls nötig.

## Phase P5 – Snapshot-Checkpoint und Byte-Payload

1. ContentRevision pro Snapshot-Kontext.
2. vorserialisierten Checkpoint verwalten.
3. Edit-Transaktion übernimmt Checkpoint als Vorher-Snapshot.
4. Snapshot-Schnittstelle optional auf UTF-8-Bytes erweitern.
5. Blob-Store für BackgroundImages.
6. erst bei nachgewiesenem Bedarf Hybrid-RAM/Disk-Store.

## Phase P6 – VIA.WPF-Extraktion

Erst nach stabiler VU-Abnahme prüfen, welche Teile neutral nach VW gehören:

- Frame-Scheduler,
- Interaktionssession-Verträge,
- Geometrie-/Viewport-Typen,
- Rendercache-Verträge,
- Performance-Diagnostics,
- optional hostneutrale Guideline-Indizes.

Nicht nach VW verschieben:

- UserFlow-Domain,
- Screen/Band/ActionArea-spezifische Mutation,
- UserFlow-Persistenz,
- UserFlow-Snapshot-Serializer.

---

## 13. Benchmark-Matrix

### Testdaten

| Set | Controls | Eigenschaften |
|---|---:|---|
| Small | 20 | gemischt, wenige Texte |
| Medium | 100 | textreich, mehrere Bands |
| Large | 500 | Text, Inputs, Charts, Images |
| MultiSelect | 100 ausgewählt | gemeinsame Bewegung |
| ImageHeavy | 100 + Background | große Base64-Hintergründe |
| TextHeavy | 200 | verschiedene Fonts/Größen |
| ProjectHeavy | 100 Screens | Undo + Save + Thumbnail |
| FolderHeavy | 100 `.ufp`-Dateien | Nachweis des Projektlisten-Hotspots |

### Szenarien

1. Control 5 Sekunden frei ziehen.
2. Control an mehreren Targets entlangziehen.
3. 20 und 100 Controls als Multiselect ziehen.
4. Control über jede Ecke resizen.
5. KeepRatio-Control resizen.
6. Toolbox-Control bewegen und droppen.
7. 30 Undo-/Redo-Wechsel hintereinander.
8. Undo bei bildreichem Screen.
9. Undo bei 100 Projektdateien im Projektordner.
10. Viewwechsel Project → Screen → Template → Popup.
11. Projektansicht mit vielen Thumbnails öffnen.
12. 100-mal Template neu anwenden beziehungsweise View neu laden.

### Messwerte

```text
Build:
Configuration:
Runtime:
Machine:
Dataset:
Scenario:

Input samples:
Frames:
Dropped/coalesced samples:
Median frame:
P95 frame:
Maximum frame:
Guideline median/P95:
PropertyEditor median/P95:
Allocated bytes/frame:
Gen0 collections:
Snapshot serialize:
Snapshot compress:
Snapshot restore:
Save:
Project list refresh:
Working set before/after:
Notes:
```

### Vorgeschlagene Budgets

Die Werte sind Ziele, keine behaupteten Istwerte:

- Medium Drag: P95-Paint unter 16 ms.
- MouseMove-Logik ohne Paint: P95 unter 2 ms.
- Keine vollständige PropertyEditor-Aktualisierung pro Zwischenproperty.
- Keine synchrone Dateischreiboperation innerhalb von Drag/Resize.
- Keine synchrone Dateischreiboperation innerhalb des sichtbaren Undo-Restores.
- Guideline-Auswertung Medium: P95 deutlich unter 1 ms.
- Keine stetig wachsenden Snapshot-Background-Payloads nach Projektwechsel.
- Kein mehrfacher Paint-Handler nach wiederholtem Template-Aufbau.

Für Large können separate Budgets festgelegt werden. Entscheidend sind reproduzierbare Vorher-/Nachher-Werte.

---

## 14. Konkrete Datei-Maßnahmen

| Datei | Maßnahme |
|---|---|
| `Mockup/Designer/BaseDesigner.MouseHandler.cs` | Interaktionssession, unveränderte Samples überspringen, nur final committen |
| `Mockup/Designer/BaseDesigner.Guidelines.cs` | statische Anker, Ergebnisvergleich, optional Guideline-Index |
| `Mockup.Guidelines/AlignmentGuidelineManager.cs` | allocationsarme Overloads, direktes Target im Ergebnis, optional binärer Index |
| `Mockup/Designer/BaseDesigner.Renderer.cs` | Frame-Scheduler, Culling, Rendercache, statische Szene |
| `Mockup/Domain/Band.cs` | LINQ-Sortierung aus Paint entfernen, Cache verwenden |
| `Mockup/Domain/BandPage.cs` | Render-/ContentRevision und Cache-Invalidierung |
| `Mockup/Domain/DesignControl.cs` | optionale RenderRevision; keine unkontrollierten Hotpath-Notifications |
| `Mockup/UIControls/PropertyEditor/PropertyEditor.xaml.cs` | property-spezifischer, coalesced und pausierbarer Refresh |
| `Mockup/Rendering/TextRenderer.cs` | bounded Layoutcache oder control-owned Layouts |
| `Mockup/Rendering/RenderContext.cs` | Viewport, InteractionState und Revisionsdaten |
| `Mockup/SnapshotIntegration/MockupViewModel.Snapshots.cs` | Restore ohne direkten Save; Edit-/Checkpoint-Integration |
| `Mockup/SnapshotIntegration/MockupSnapshotSerializer.cs` | direkte Token-Serialisierung, Blob-Lifecycle, Diagnostik |
| `Mockup.Snapshots/SnapshotEntry.cs` | optional Byte-Payload; getrennte Zeit-/Byte-Metriken |
| `Mockup.Snapshots/SnapshotManager.cs` | kontextspezifische Limits, Byte-Budget, Checkpoints |
| `Mockup/ViewModel/MockupViewModel.Storage.cs` | Dirty-State, Debounce, Single Writer, kein Rescan pro Save |
| `Mockup/ViewModel/MockupViewModel.ContextMenuCommands.cs` | `RefreshProjectFiles` nur bei strukturellen Dateiänderungen |
| `Mockup/Rendering/ScreenThumbnail.xaml.cs` | Entity-/Revision-Key, gezielte Invalidation |
| `Mockup/Designer/BaseDesigner.cs` | Handler sauber ab-/anmelden, gezielte Messages |
| `VIA.WPF/docs/PERFORMANCE_BASELINE.md` | Mockup-/Skia-Benchmarkbereich ergänzen |
| `VIA.WPF.Mockup.Wpf/XMockupCanvas.cs` | erst später neutralen Scheduler und Lifecycle übernehmen |

---

## 15. Was ausdrücklich nicht empfohlen wird

### Keine Snapshots pro MouseMove

Das würde Serialisierung, Kompression und History explodieren lassen. Eine Geste bleibt genau ein Undo-Schritt.

### Keine ObservableCollections auf Background-Threads serialisieren

Die Collections können währenddessen verändert werden. Zuerst muss ein konsistenter immutable Payload auf dem UI-Thread erfasst werden.

### Nicht sofort auf Delta-Undo umstellen

Das Full-Snapshot/Memento-Prinzip ist für den komplexen Objektgraphen robust. Zuerst Snapshot-Zeitpunkt, Payload und Save-Pipeline optimieren.

### Keine unbegrenzten statischen Caches

Jeder Cache benötigt:

- Owner,
- Revision,
- Größenlimit,
- Eviction,
- Clear-Lifecycle,
- Native-Dispose-Regeln,
- Diagnosewerte.

### Keine vorzeitige Komplettmigration nach VIA.WPF

`VIA.WPF.Mockup` ist aktuell eine frühe Foundation. Die produktive Drag-, Guideline- und Undo-Logik liegt in VU. Erst messen und stabilisieren, dann neutrale Bestandteile extrahieren.

### Keine erfundenen Prozentwerte

Verbesserungen ausschließlich mit reproduzierbarer Release/x64-Baseline dokumentieren.

---

## 16. Empfohlene tatsächliche Reihenfolge

Die beste Reihenfolge nach Wirkung, Risiko und Abhängigkeiten:

1. **Performance-Diagnose einbauen.**
2. **PropertyEditor während Drag/Resize entschärfen.**
3. **Unveränderte Samples verwerfen und Frames coalescen.**
4. **Renderreihenfolge cachen und Culling ergänzen.**
5. **Synchrones Speichern und Projektlisten-Rescan aus Undo/Redo entfernen.**
6. **Dirty-State und SaveCoordinator einführen.**
7. **Transiente InteractionSession statt Model-Mutation pro Sample.**
8. **Textlayout cachen.**
9. **Statische Szene während Interaktion cachen.**
10. **Snapshot-Checkpoint und direkte UTF-8-Payloads.**
11. **Guideline-Index nur bei nachgewiesenem Bedarf.**
12. **Bewährte neutrale Komponenten nach VIA.WPF übernehmen.**

---

## 17. Definition of Done

Die Performance-Arbeit ist für die erste Stufe abgeschlossen, wenn:

- Drag/Resize erzeugt keinen vollständigen PropertyEditor-Refresh pro Layoutproperty.
- Mehrere MouseMove-Samples werden auf höchstens einen Renderframe koalesziert.
- Identische gerundete Geometrien erzeugen keine neue Guideline-/Paint-Arbeit.
- Renderreihenfolge wird nicht in jedem Frame neu sortiert.
- Unsichtbare Controls werden nicht teuer gerendert.
- Undo/Redo schreibt nicht synchron während des sichtbaren Restores auf Disk.
- Ein normaler Save liest nicht alle Projektdateien erneut ein.
- Ein Drag/Resize erzeugt genau einen Undo-Schritt.
- Full-Snapshot/Memento bleibt erhalten.
- Snapshot-, Save-, Guideline- und Renderzeiten sind messbar.
- Caches besitzen Limits und Lifecycle.
- Build und Funktionstests für Screen, Template und Popup sind erfolgreich.
- Verbesserungen sind durch Release/x64-Vorher-/Nachher-Werte belegt.

---

## 18. Startauftrag für einen umsetzenden Chat

```text
Lies zuerst den aktuellen VIA.UserFlow-VS2AI-Export und danach:

VIA_UserFlow_Performanceanalyse_Drag_Guidelines_UndoRedo_2026-07-23.md

Der Export ist die technische Wahrheit.

Beginne ausschließlich mit Phase P0 und danach P1. Noch keine transiente
InteractionSession, kein Snapshot-API-Umbau und keine VIA.WPF-Migration.

Ziele der ersten Umsetzung:

1. Messpunkte für Drag, Guidelines, Paint, PropertyEditor, Snapshot und Save.
2. PropertyEditor-Refresh property-spezifisch und coalesced machen.
3. PropertyEditor während aktiver Drag-/Resize-Geste pausieren und auf MouseUp einmal aktualisieren.
4. unveränderte gerundete Drag-/Resize-Samples verwerfen.
5. Designer-Invalidierungen pro UI-Frame coalescen.
6. keine sichtbare Verhaltensänderung an Snapping, Multiselect, Parent-Wechsel,
   ZOrder, Undo/Redo oder Persistenz.

Verbindliche Regeln:

- Keine Snapshot-Erzeugung pro MouseMove.
- Full-Snapshot/Memento bleibt bestehen.
- Keine ObservableCollection auf einem Background-Thread serialisieren.
- Keine persistente Schattengeometrie im Designer.
- Keine Public-API-, JSON- oder Persistenzänderung in P0/P1.
- Jede Phase separat bauen und manuell testen.
- Performancegewinne nur anhand gemessener Vorher-/Nachher-Werte angeben.
- Vollständige geänderte Dateien liefern.
```

---

## 19. Schlussbewertung

Die Guidelines sind im aktuellen Stand nicht schlecht aufgebaut. Ihre Zielrechtecke werden bereits pro Interaktion gecacht, der Snap wird final angewendet und die Berechnungsbibliothek ist sauber hostneutral.

Die sichtbar schlechte Drag-Performance wird voraussichtlich primär durch das Zusammenspiel folgender Faktoren verursacht:

1. Observable Model-Mutationen pro MouseMove,
2. vollständige PropertyEditor-Refreshes pro einzelner Property-Änderung,
3. vollständige Skia-Paints,
4. wiederholtes Textlayout,
5. Sortier- und LINQ-Arbeit in jedem Paint,
6. synchrone Snapshot-Erzeugung beim ersten Drag-Schritt.

Bei Undo/Redo ist der wichtigste Befund noch eindeutiger: Der Restore wird derzeit durch vollständiges synchrones Speichern und teilweise sogar das erneute Einlesen aller Projektdateien verlängert.

Deshalb sollte die Arbeit nicht mit einem komplexen Guideline-Index beginnen. Die höchste Wirkung bei überschaubarem Risiko liefern zuerst PropertyEditor-Coalescing, Frame-Coalescing, unveränderte-Sample-Erkennung sowie die Entkopplung von Undo/Redo und synchronem Save.
