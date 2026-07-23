# VIA.UserFlow – UI Brushup Masterplan

**Status:** Umsetzungsplan für einen neuen Chat  
**Basis:** aktueller VS2AI-Export vom 21.07.2026, Branch `toolbox-rail-flyout`  
**Ziel:** Bestehendes VIA.UserFlow-UI sichtbar professioneller, klarer und desktopfähiger machen, ohne die bestehende Fachlogik oder den Wiedererkennungswert der Anwendung durch ein fremdes SaaS-Design zu ersetzen.

---

## 1. Auftrag an den umsetzenden Chat

Lies zuerst:

1. `#CHAT-OVERVIEW/CHAT-OVERVIEW.md`
2. den neuesten VS2AI-Export
3. diesen `UI_BRUSHUP_MASTERPLAN.md`

Danach soll die UI in kleinen, kompilierbaren Phasen umgestellt werden.

### Verbindliche Regeln

- Bestehende VIA-WPF-Controls weiterverwenden.
- Wenn ein passendes `via:X...`-Control existiert, kein normales WPF-Control mit nachgebautem Styling einsetzen.
- Keine Projekt-, Screen-, Template-, Popup- oder Control-Datenformate still ändern.
- Keine Big-Bang-Änderung.
- Jede Phase separat bauen und manuell prüfen.
- Bei geänderten Dateien vollständige Dateien liefern, sofern nicht ausdrücklich ein Patch verlangt wird.
- Keine wichtigen Aktionen nur über Tooltips oder versteckte Kontextmenüs erreichbar machen.
- Projektaktionen, Screenaktionen und Designeraktionen klar voneinander trennen.
- Das UI bleibt eine professionelle Windows-Desktop-Anwendung und wird nicht in ein browserartiges SaaS-Dashboard umgebaut.
- Light Theme bleibt zunächst der verbindliche Zielzustand.
- Die Mockup-Farbschemata dürfen nicht die Farben des Workbench-Chromes verändern.

---

## 2. Verbindliche Produktentscheidungen

### 2.1 ProjectView bleibt die Projektzentrale

Die `ProjectView` zeigt:

- Projektübersicht
- Projektaktionen
- Projektinformationen
- Screens als Karten
- Suche, Filter und Sortierung
- kompakten Hinweis auf das aktive Mockup-Farbschema

Sie wird nicht zu einer beliebigen Dashboard-Startseite umgebaut.

### 2.2 ScreenView bleibt der eigentliche Designer

Die `ScreenView` zeigt:

- Screenliste links
- Designer-Viewport in der Mitte
- Controls/Templates/Properties rechts
- Screen- und Designeraktionen oben
- Zoom, Undo/Redo und Ansichtsstatus

### 2.3 Device-/Canvas-Größe ist eine Projekteigenschaft

Die gemeinsame Breite und Höhe des Projekts darf in der `ScreenView` nicht direkt bearbeitet werden.

In der `ScreenView` wird nur ein nicht editierbarer Kontext angezeigt:

```text
Desktop · 1440 × 900
```

Ein Klick auf diesen Kontext öffnet die Seite **Device & Layout** im Project-Edit-Dialog.

Für vorhandene Projekte gilt zunächst:

- Größe nur anzeigen
- keine direkte Änderung
- stattdessen Aktion `Change Canvas Size...`

Solange kein sicherer Migrationsdialog implementiert ist, bleibt diese Aktion deaktiviert oder zeigt einen erklärenden Hinweis.

Eine spätere Größenänderung muss eine explizite Projektoperation sein:

1. neue Größe wählen
2. Auswirkung anzeigen
3. Strategie wählen:
   - Positionen und Größen beibehalten
   - Inhalte proportional skalieren
   - abbrechen
4. Projekt-Snapshot erzeugen
5. Änderung atomar anwenden
6. außerhalb liegende Controls melden

Es darf niemals eine stille Skalierung aller Screens stattfinden.

### 2.4 Bestehender Wiedererkennungswert bleibt erhalten

Beibehalten werden:

- dunkle VIA-Hauptnavigation
- VIA-Logo
- helle Arbeitsflächen
- blaue Primäraktionen
- linke Navigations-/Projektspalte
- mittlere Arbeitsfläche
- rechte Toolbox-Rail mit Flyout
- kompakter WPF-Workbench-Charakter

Verbessert werden:

- visuelle Hierarchie
- Abstände
- Aktionssichtbarkeit
- Karten
- Zustände
- Beschriftungen
- Dialogstruktur
- Desktop-Layout-Unterstützung
- Farbdisziplin
- einheitliche Größen und Icons

---

## 3. Entscheidung zum Grid

## 3.1 Empfehlung

**Das bestehende Pixel-Grid soll vollständig aus dem normalen Designer entfernt werden.**

Das ist für ein Mockup- und Wireframing-Tool fachlich vertretbar und in der aktuellen Architektur sogar sinnvoll.

Nach Einführung der Alignment-Guidelines stehen bereits bessere Hilfen zur Verfügung:

- Kanten ausrichten
- Mittelpunkte ausrichten
- gleiche Breite und Höhe erkennen
- sichtbare Snap-Linien
- Ziel-Control hervorheben
- finale Snap-Korrektur erst am Ende der Interaktion
- AlignmentToolbar für exakte Mehrfachausrichtung
- numerische Werte im Property Editor
- Pixel-Nudging per Tastatur

Das alte Grid erzeugt dagegen zwei Probleme:

1. Es überlagert die Arbeitsfläche visuell und konkurriert mit den Guidelines.
2. Eine Änderung der `GridSize` ändert nachträglich das Raster für bereits positionierte Controls. Beim nächsten gridbezogenen Vorgang werden bisherige Positionen auf neue Rasterwerte quantisiert. Controls wirken dadurch, als würden sie bei Auswahl oder Bearbeitung unerwartet springen.

Dieses Verhalten ist mathematisch erwartbar, aber aus UX-Sicht schlecht. Ein Grid darf bestehende Inhalte nicht nachträglich und überraschend verschieben.

## 3.2 Was entfernt werden soll

Aus der produktiven UI entfernen:

- `Show Grid`
- `Grid Size`
- Grid-Slider
- Grid-Darstellung im Designer
- automatisches Snap-to-Grid beim Drop
- automatisches Snap-to-Grid bei Auswahl oder Move
- Grid-Snap beim Resize
- Grid-Abhängigkeit beim Nudge
- Grid-Optionen aus Project-Neuanlage und Project-Edit-Dialog
- Grid-Buttons aus Toolbars und Optionsseiten

Technisch zu prüfen und schrittweise zu entfernen:

- `Project.ShowGrid`
- `Project.GridSize`
- `RenderContext.ShowGrid`
- `RenderContext.GridSize`
- `Band.RenderGrid(...)`
- Grid-Pass in `BaseDesigner.Renderer.cs`
- `TrySnapSelectionToGrid()`
- `TrySnapControlToGridOnDrop(...)`
- `IsGridResizeSnapEnabled(...)`
- `SnapResizeValueToGrid(...)`
- `SnapWorldToGrid(...)`
- `SnapLocalToGrid(...)`
- alle Grid-Prüfungen in:
  - `ScreenDesigner.DragDrop.cs`
  - `TemplateDesigner.DragDrop.cs`
  - `PopupDesigner.DragDrop.cs`
  - `BaseDesigner.MouseHandler.cs`
  - `BaseDesigner.Renderer.cs`

## 3.3 Persistenzsichere Entfernung

Die Grid-Felder nicht im ersten Schritt hart aus dem JSON-Vertrag löschen.

Empfohlene Übergangsstrategie:

### Release A

- UI vollständig entfernen
- Grid-Rendering und Grid-Snapping deaktivieren
- bestehende JSON-Werte weiterhin einlesen
- Properties intern als Legacy/no-op behalten
- neue Projekte schreiben die bisherigen Defaultwerte oder lassen die Properties über den bestehenden Serializer unverändert
- alte Projekte laden ohne Migration

### Release B

Nach Prüfung aller eigenen JSON-Converter entscheiden:

- Properties endgültig entfernen, sofern unbekannte Felder sicher ignoriert werden
- oder aus Kompatibilitätsgründen dauerhaft als ausgeblendete Legacy-Properties behalten

Keine Projektdatei darf wegen der Grid-Entfernung unlesbar werden.

## 3.4 Ersatz für das Grid

Verbindlicher Ersatz:

- Alignment-Guidelines standardmäßig aktiv
- Pfeiltasten: 1 px
- `Shift` + Pfeiltaste: 10 px
- `Alt` während Drag/Resize: temporäres Abschalten des Guideline-Snaps
- Property Editor für exakte X/Y/W/H-Werte
- AlignmentToolbar für links/rechts/oben/unten/zentriert
- Distribute Horizontal/Vertical für Mehrfachauswahl
- optional später: Abstandsanzeigen zwischen Controls

Nicht als Ersatz einführen:

- kein zweites unsichtbares Raster
- kein verstecktes automatisches Quantisieren
- kein Snap beim bloßen Selektieren
- kein Ändern von Modellkoordinaten ohne tatsächliche Move-/Resize-Aktion

## 3.5 Optionaler späterer Desktop-Layout-Grid

Ein späteres **Layout Grid** wäre eine andere Funktion als das bisherige Pixel-Grid:

- 12-Spalten-Grid
- definierte Margins und Gutters
- nur als visuelle Layout-Hilfe
- projekt- oder screenbezogen
- optionales, bewusstes Snapping
- bestehende Controls werden beim Ändern des Layout-Grids nicht verschoben

Diese Funktion gehört nicht in den aktuellen Brushup und darf nicht als Grund dienen, das alte Pixel-Grid zu behalten.

---

## 4. Farbsystem

## 4.1 Grundregel: Workbench und Mockup-Inhalt trennen

Es gibt zwei vollständig getrennte Farbsysteme:

### A. Workbench-Farben

Für:

- Hauptnavigation
- Toolbars
- Dialoge
- Panels
- Karten
- Auswahlzustände
- Statusleiste
- Systemmeldungen

### B. Project ColorSchema

Für:

- Controls im Mockup
- Preview
- Screen- und Template-Inhalte
- benutzerdefinierte Designfarben

`PrimaryBrush` aus dem Project ColorSchema darf nicht mehr unkontrolliert für App-Schaltflächen und Workbench-Zustände verwendet werden.

Neue Workbench-Tokens sollen eindeutige Namen erhalten, zum Beispiel:

```text
WorkbenchAccentBrush
WorkbenchAccentHoverBrush
WorkbenchSurfaceBrush
WorkbenchCanvasBrush
WorkbenchSelectionBrush
WorkbenchBorderBrush
```

## 4.2 Verbindliche Light-Palette

| Bereich / Token | Farbe | Zweck |
|---|---:|---|
| `WorkbenchShellBrush` | `#20242B` | Hauptnavigation und Statusleiste |
| `WorkbenchShellHoverBrush` | `#2A3039` | Hover in dunkler Navigation |
| `WorkbenchShellSelectedBrush` | `#343C48` | ausgewählter Hauptbereich |
| `WorkbenchShellBorderBrush` | `#303640` | Linien im dunklen Shell-Bereich |
| `WorkbenchNavTextBrush` | `#C9D2DE` | inaktive Navigation |
| `WorkbenchNavTextActiveBrush` | `#FFFFFF` | aktive Navigation |
| `WorkbenchAccentBrush` | `#256AA0` | Primäraktionen |
| `WorkbenchAccentHoverBrush` | `#1E5A89` | Hover Primäraktion |
| `WorkbenchAccentPressedBrush` | `#194B73` | gedrückte Primäraktion |
| `WorkbenchAccentSoftBrush` | `#EAF3FA` | Auswahlflächen und Info-Hintergrund |
| `WorkbenchWindowBrush` | `#F3F6F9` | allgemeiner Fensterhintergrund |
| `WorkbenchSurfaceBrush` | `#FFFFFF` | Panels, Karten, Dialoginhalte |
| `WorkbenchSubHeaderBrush` | `#F7F9FC` | Kontextköpfe und Panelheader |
| `WorkbenchCanvasBrush` | `#EEF2F6` | Workspace außerhalb der Device Area |
| `WorkbenchBorderBrush` | `#D8E0EA` | normale Rahmen |
| `WorkbenchDividerBrush` | `#E6EBF1` | dezente Trenner |
| `WorkbenchTextPrimaryBrush` | `#17202A` | Haupttext |
| `WorkbenchTextSecondaryBrush` | `#52606D` | Beschreibungen |
| `WorkbenchTextMutedBrush` | `#7B8794` | Metadaten |
| `WorkbenchDisabledSurfaceBrush` | `#EEF1F4` | deaktivierte Controls |
| `WorkbenchDisabledTextBrush` | `#98A2B3` | deaktivierter Text |
| `WorkbenchSelectionBrush` | `#E8F2FA` | selektierte Listeneinträge |
| `WorkbenchSelectionBorderBrush` | `#256AA0` | Auswahlrahmen |
| `WorkbenchHoverBrush` | `#F4F8FB` | Karten-/Zeilen-Hover |
| `WorkbenchFocusBrush` | `#3B82C4` | Tastaturfokus |
| `WorkbenchSuccessBrush` | `#27865F` | Erfolg |
| `WorkbenchWarningBrush` | `#C87410` | Warnung |
| `WorkbenchDangerBrush` | `#D92D20` | Löschen/Fehler |
| `WorkbenchInfoBrush` | `#256AA0` | Information |
| `DesignerDeviceSurfaceBrush` | `#FFFFFF` | Device-/Canvas-Inhalt |
| `DesignerDeviceBorderBrush` | `#252B33` | Device-Rahmen |
| `DesignerGuidelineBrush` | `#1687D9` | Alignment-Guidelines |
| `DesignerGuidelineSoftBrush` | `#1F1687D9` | Guideline-Zielhervorhebung |
| `DesignerActionAreaBrush` | `#E5484D` | ActionArea-Rahmen |
| `DesignerActionAreaSoftBrush` | `#18E5484D` | ActionArea-Füllung |

### Farbregeln

- Primärblau nur für Primäraktion, aktive Auswahl und Fokus.
- Rot nur für destruktive Aktion, Fehler und ActionArea.
- Orange nur für Warnungen.
- Grün nur für Erfolg und gespeicherten Zustand.
- Keine zufälligen lokalen Hexwerte in Views.
- Keine kräftigen Farbflächen als reine Dekoration.
- Projektfarben nur innerhalb von Mockup-Vorschauen und im ColorSchema-Bereich.
- Keine Gradients im Workbench-Chrome.
- Schatten sparsam verwenden.

---

## 5. Layout- und Style-Tokens

Einheitliche Maße:

| Token | Wert |
|---|---:|
| kleinster Abstand | 4 px |
| kompakter Abstand | 8 px |
| Standardabstand | 12 px |
| Panel-Innenabstand | 16 px |
| Abschnittsabstand | 24 px |
| große Trennung | 32 px |
| kompakte Buttonhöhe | 30 px |
| Standard-Buttonhöhe | 34 px |
| Hauptaktionshöhe | 36 px |
| Icon klein | 14 px |
| Icon normal | 16 px |
| Icon prominent | 18 px |
| kleiner Radius | 4 px |
| Standardradius | 6 px |
| Kartenradius | 8 px |
| normale Border | 1 px |

### Schatten

Nur verwenden für:

- Dialog
- Flyout
- aktive/angehobene Karte
- Device Area im Workspace

Keine Schatten auf:

- jeder Toolbar
- jeder Zeile
- jedem einfachen Panel
- jeder kleinen Schaltfläche

---

## 6. MainWindow und Hauptnavigation

### Ziel

Die bestehende dunkle VIA-Navigation bleibt erhalten, wird aber konsistenter.

### Änderungen

- einheitliche Höhe von 54 px beibehalten
- aktive Seite klarer markieren:
  - weißer Text
  - dezente dunklere Auswahlfläche oder 2-px-Unterlinie
- inaktive Texte etwas heller als aktuell, aber nicht weiß
- Hover klar erkennbar
- gleiche Abstände zwischen allen Haupttabs
- `OPTIONS`, `HELP`, `ABOUT` visuell als sekundäre Gruppe trennen
- Theme-Auswahl rechts kompakt lassen
- keine zusätzlichen Dashboard-Icons in die Hauptnavigation einführen
- Statusleiste unten beibehalten:
  - links Projektstatistik
  - Mitte Dateipfad optional gekürzt
  - rechts Speicherkstatus mit Zeitstempel
  - Erfolg nur als kleines grünes Icon, nicht als dauerhaft kräftige Fläche

### Betroffene Dateien

- `UserFlow/MainWindow.xaml`
- `UserFlow/MainWindow.xaml.cs`
- globale Ressourcen in `App.xaml`
- `Mockup/Styles/ViaWorkbenchControls.xaml`

---

## 7. ProjectView

## 7.1 Zielaufteilung

```text
┌─────────────────────────────────────────────────────────────────────┐
│ Project context / project actions                                  │
├───────────────────┬─────────────────────────────────────────────────┤
│ Project panel     │ Screens header + search/filter/sort            │
│ 320–360 px        ├─────────────────────────────────────────────────┤
│                   │ responsive screen-card grid                     │
│                   │                                                 │
└───────────────────┴─────────────────────────────────────────────────┘
```

Die linke Spalte soll nicht mehr pauschal 450 px breit sein.

Empfehlung:

- Standard: 336 px
- Minimum: 300 px
- Maximum: 380 px
- optional über Splitter anpassbar

## 7.2 Projektkopf links

Oben:

- Projektname
- kleines Dropdown für zuletzt verwendete Projekte
- klar sichtbarer Zustand bei keinem geöffneten Projekt
- `New Project` als beschriftete Primäraktion
- `Open` als beschriftete Sekundäraktion

Danach kompakte Aktionsleiste:

- Save
- Save As
- Edit Project Settings
- Duplicate
- Export
- Overflow-Menü

`Delete Project` gehört ausschließlich in das Overflow-Menü und bleibt rot.

Nicht mehrere identische Pencil-Icons nebeneinander ohne Text zeigen.

## 7.3 Projektinformationen

Als kompakte Karte:

- Description
- Target: Mobile / Tablet / Desktop / Custom
- Canvas Size
- Screens
- Templates
- Popups
- Last Modified
- File Location
- Shared / Read-only nur anzeigen, wenn fachlich relevant

Aktion:

```text
Edit Project Settings
```

Diese Aktion öffnet den Project-Edit-Dialog.

## 7.4 Farbschema in ProjectView

Die große dauerhafte Farbliste am unteren linken Rand wird reduziert.

Anzeigen:

- Name des aktiven Schemas
- kompakte Reihe der wichtigsten Farbswatches
- Aktion `Edit Theme & Colors`

Die vollständige Bearbeitung liegt auf der Dialogseite **Theme & Colors**.

## 7.5 Screenbereich rechts

Header:

```text
Screens (52)                                    + Add Screen
```

Darunter eine gemeinsame Werkzeugzeile:

- Suche
- Filter: All / Home / Other
- Sortierung: Name / Created / Modified
- Karten-/Listenansicht
- Thumbnail-Zoom

`Add Screen` bleibt als beschriftete Primäraktion sichtbar.

## 7.6 Screenkarten

Jede Karte enthält:

- Screenname
- Typ-/Home-Indikator
- Preview
- Canvas-Größe
- optional Änderungsstatus
- Overflow-Menü

Aktionen im Overflow:

- Open/Edit
- Duplicate
- Rename
- Set as Home
- Export Screenshot
- Delete

Desktop-Screens dürfen nicht in eine erzwungene Hochformatvorschau gepresst werden.

Die Previewfläche verwendet `Stretch=Uniform` und eine maximale Vorschauhöhe. Die Karte passt sich an Mobile-, Tablet- und Desktop-Seitenverhältnisse an.

Zustände:

- Hover: `WorkbenchHoverBrush`
- Selected: `WorkbenchSelectionBrush` + 2-px-Akzentrahmen
- Home: kleines blaues Badge
- Warning: kleines oranges Badge
- Delete nie als dauerhaft sichtbarer roter Kartenbutton

## 7.7 Leere Zustände

Ohne Projekt:

- verständlicher Hinweis
- `Create New Project`
- `Open Existing Project`

Projekt ohne Screens:

- `This project has no screens yet`
- `Add First Screen`

Keine leeren weißen Flächen ohne Handlungsoption.

### Betroffene Dateien

- `Mockup/Views/ProjectView.xaml`
- `Mockup/Views/ProjectView.xaml.cs`
- `Mockup/ViewModel/MockupViewModel.Commands.cs`
- `Mockup/ViewModel/MockupViewModel.Collections.cs`
- `Mockup/ViewModel/MockupViewModel.State.cs`
- `Mockup/Rendering/ScreenThumbnail.xaml`
- `Mockup/Rendering/ScreenThumbnail.xaml.cs`
- Karten-/Listenstyles in `Mockup/Styles/*`

---

## 8. ScreenView

## 8.1 Zielaufteilung

```text
┌─────────────────────────────────────────────────────────────────────┐
│ Screen context + designer toolbar                                  │
├───────────────┬───────────────────────────────┬─────────────────────┤
│ Screen list   │ full designer viewport        │ toolbox/properties  │
│ 260–300 px    │ centered device/canvas        │ rail + flyout       │
└───────────────┴───────────────────────────────┴─────────────────────┘
```

## 8.2 Linke Screenliste

Kopf:

- aktueller Screenname
- `+` Add Screen
- optional Screenmenü

Darunter:

- Suche
- Gruppen
- kompakte Thumbnails
- Screenname
- Home-Badge
- Beschreibung nur für selektierten oder aufgeklappten Screen
- Overflow-Menü je Eintrag

Die Liste soll weniger Text und mehr klare visuelle Orientierung bieten.

## 8.3 Screen-/Designer-Toolbar

Links:

- Screenname
- Home-Badge
- Screenaktionen:
  - Edit Screen
  - Duplicate
  - Set as Home
  - Delete im Overflow

Mitte:

- nicht editierbarer Projektkontext:

```text
Desktop · 1440 × 900
```

- Klick öffnet `Project Settings > Device & Layout`

Nicht anzeigen:

- direkt editierbare Width-/Height-Felder
- Device-Preset als editierbare ComboBox
- Grid-Button
- Grid-Size-Slider

Rechts:

- Undo
- Redo
- Zoom Out
- Zoom Slider / Prozent
- Zoom In
- Fit to View
- Actual Size
- optional Toolbox/Properties ein-/ausblenden

## 8.4 Designer-Viewport

Der Designer belegt den verfügbaren Viewport.

Die Device Area liegt darin:

- horizontal zentriert
- bei genügend Platz vertikal zentriert
- zoombar
- später panbar
- klar vom Workspace getrennt

Farben:

- Workspace: `#EEF2F6`
- Device: `#FFFFFF`
- Device Border: `#252B33`
- Device Shadow: sehr dezent
- Guidelines: `#1687D9`
- Selection: Workbench-Akzentblau
- ActionAreas: `#E5484D`

Bei Desktop-Projekten:

- keine irreführende mobile Geräteoptik
- Kontextlabel `Desktop Canvas`
- Maße sichtbar
- Device-Top/-Bottom-Hinweise nur anzeigen, wenn sie fachlich relevant sind
- Header-/Footer-Bands bleiben fachliche Screenbestandteile, keine simulierte Betriebssystemleiste

## 8.5 Toolbox und Properties

Bestehende rechte Rail beibehalten.

Verbessern:

- aktive Rail-Schaltfläche klar markieren
- Flyout-Header mit Titel und Close/Pin
- Suche immer oben
- Kategorien links oder als klarer Accordion-Bereich
- einheitliche Previewkarten
- feste PNG-Previews für Controltypen
- gecachte generierte Thumbnails für Templates
- Properties logisch gruppieren:
  - Position & Size
  - Layout
  - Appearance
  - Content
  - Interaction
  - Advanced

### Betroffene Dateien

- `Mockup/Views/ScreenView.xaml`
- `Mockup/Views/ScreenView.xaml.cs`
- `Mockup/Views/ToolboxView.xaml`
- `Mockup/UIControls/PropertyEditor/*`
- `Mockup/UIControls/UndoRedoBar/*`
- `Mockup/UIControls/XZoomSlider/*`
- `Mockup/Designer/DesignerControls/ScreenDesignerControl.xaml`
- `Mockup/Designer/BaseDesigner.Coordinates.cs`
- `Mockup/Designer/BaseDesigner.MouseHandler.cs`
- `Mockup/Designer/BaseDesigner.Renderer.cs`
- `Mockup/Designer/BaseDesigner.Guidelines.cs`
- `Mockup/Designer/DragDrop/*`

---

## 9. Project-Neuanlage und Project-Edit-Dialog

## 9.1 Gemeinsame Dialog-Shell

New und Edit verwenden dieselbe visuelle Shell.

Aufbau:

```text
┌──────────────────────────────────────────────────────┐
│ NEW PROJECT / EDIT PROJECT                       ×   │
├───────────────┬──────────────────────────────────────┤
│ General       │ Inhalt der aktiven Seite             │
│ Device        │                                      │
│ Guidelines    │                                      │
│ Theme         │                                      │
│ Paths         │                                      │
│ Advanced      │                                      │
├───────────────┴──────────────────────────────────────┤
│                                    Cancel  Primary   │
└──────────────────────────────────────────────────────┘
```

Empfohlene Größe:

- 760–880 px breit
- 560–680 px hoch
- bei kleinen Bildschirmen scrollbar
- links 160–180 px Navigation
- Inhalt 520–650 px

Dialogkopf:

- dunkel wie bestehender VIA-Dialogkopf
- Titel links
- Projektname optional als Untertitel
- Close rechts

Footer:

- `Cancel`
- New Mode: `Create Project`
- Edit Mode: `Save Changes`

Kein generisches `OK`, wenn eine spezifische Aktion möglich ist.

## 9.2 Seite General

Felder:

- Project Name
- Description
- Project Location nur bei New
- Project File Name/Key optional automatisch erzeugt
- Shared
- Read-only nur anzeigen, wenn fachlich tatsächlich funktionsfähig

Hinweise:

- Pflichtfelder kennzeichnen
- Inline-Validierung
- keine Fehlermeldung erst nach Klick auf Create
- Dateipfad nicht in einer schmalen TextBox abschneiden
- Browse-Button mit Text oder eindeutigem Folder-Icon

## 9.3 Seite Device & Layout

### New Project

Auswahl als deutliche Kacheln:

- Mobile
- Tablet
- Desktop
- Custom

Desktop-Presets:

- Desktop App — 1280 × 800
- HD Desktop — 1366 × 768
- Workbench — 1440 × 900
- Wide Desktop — 1600 × 900
- Full HD — 1920 × 1080
- QHD — 2560 × 1440
- Custom

Mobile-/Tablet-Presets weiterhin aus dem bestehenden Katalog laden.

Anzeigen:

- Preset
- Width
- Height
- Orientation
- Verhältnis
- kleine proportionale Canvas-Vorschau
- erklärender Hinweis:

```text
This size defines the base coordinate system for all screens in the project.
```

### Edit Project

- aktuelle Größe zunächst read-only
- Badge `Project-wide`
- Hinweis, dass alle Screens dasselbe Basiskoordinatensystem verwenden
- Aktion `Change Canvas Size...`
- keine sofort editierbaren NumberBoxes

## 9.4 Seite Guidelines

Da das Pixel-Grid entfernt wird, heißt die Seite nicht mehr `Grid & Guides`, sondern:

```text
Alignment & Guides
```

Optionen:

- Enable alignment guidelines
- Snap to edges
- Snap to centers
- Snap to equal width/height
- Show target highlight
- Snap threshold, z. B. 4–12 px
- optional später: show spacing hints

Empfohlener Default:

- Guidelines aktiv
- Kanten aktiv
- Mittelpunkte aktiv
- gleiche Größe aktiv
- Target Highlight aktiv
- Threshold 6 px

Keine Grid-Optionen mehr.

## 9.5 Seite Theme & Colors

Anzeigen:

- Schemaauswahl
- kompakte Farbvorschau
- Primary
- Accent
- Info
- Success
- Warning
- Error
- Neutral
- Text
- Control Background
- Control Border
- Card Border

Aktionen:

- Edit Color Scheme
- Duplicate Scheme
- Reset to Preset
- Delete Custom Scheme

Wichtig:

- Diese Farben verändern Mockup-Inhalte.
- Sie verändern nicht die Workbench-Farben des Programms.

## 9.6 Seite Paths

Anzeigen:

- Project File
- Asset Folder
- Export Folder
- optional Autosave/Backup Folder

Je Pfad:

- vollständiger Pfad
- Browse
- Open in Explorer
- Reset to Default

Keine Pfade unter `Program Files` oder im Anwendungsverzeichnis als Standard für beschreibbare Benutzerdaten.

## 9.7 Seite Advanced

Nur seltene Einstellungen:

- Autosave interval
- Backup on save
- Undo history limit
- Preview/thumbnail cache settings nur bei wirklichem Bedarf
- Diagnostic information
- `Reset UI Layout`
- `Clear Undo History` mit Bestätigung

Keine normalen Projekteinstellungen in Advanced verstecken.

### Betroffene Dateien

- `Mockup/Dialogs/ProjectDialog.xaml`
- `Mockup/Dialogs/ProjectDialog.xaml.cs`
- `Mockup/Dialogs/Service/DialogService.cs`
- `Mockup/Dialogs/Service/CloneProfiles.cs`
- `Mockup/Domain/Project.cs`
- `Mockup/ViewModel/MockupViewModel.Settings.cs`
- `Mockup/ViewModel/MockupViewModel.Commands.cs`
- `Mockup/Resources/Json/mobile.json`
- ColorSchema-Dateien
- Path-/Storage-Dateien

---

## 10. Sichtbarkeit und Priorität von Aktionen

### Primär sichtbar

ProjectView:

- New Project
- Open
- Save
- Edit Project Settings
- Add Screen

ScreenView:

- Add Screen
- Edit Screen
- Undo
- Redo
- Zoom/Fit
- Preview

### Sekundär sichtbar oder im Dropdown

- Save As
- Duplicate
- Export
- Set as Home
- Rename

### Nur im Overflow / Kontextmenü

- Delete Project
- Delete Screen
- Diagnoseaktionen
- Cache löschen
- Reset

### Regeln

- Primäraktionen haben Icon und Text.
- Icon-only nur für bekannte, häufig verwendete Designeraktionen.
- Destruktive Aktionen nicht neben `New` oder `Save` ohne Abstand platzieren.
- Gleiche Aktion überall mit gleichem Icon und gleicher Beschriftung.
- Tooltips ergänzen, ersetzen aber keine notwendige Beschriftung.

---

## 11. Technische Reihenfolge

## Phase 0 – Bestandsaufnahme

- Branch anlegen: `ui-brushup`
- Screenshots aller Hauptansichten erstellen
- Build Debug und Release
- aktuelle Projektdatei als Testfixture sichern
- Liste aller lokalen Hexfarben in produktiven Views erstellen
- Liste aller Standard-WPF-Controls erstellen, für die VIA-WPF-Pendants existieren

Abnahme:

- unveränderter Baseline-Build
- dokumentierte Screenshots
- Testprojekt mit Mobile- und Desktop-Screens

## Phase 1 – Workbench-Tokens

- neue Workbench-Farb- und Maßtokens anlegen
- bestehende globale Ressourcen konsolidieren
- lokale `BorderBrush`-/`HeaderBackgroundBrush`-Duplikate entfernen
- Workbench-Farben von Project ColorSchema entkoppeln
- Buttons, Panelheader, Auswahl, Karten und Statusleiste vereinheitlichen

Abnahme:

- keine fachliche Änderung
- Project-Farbschema ändert nicht das App-Chrome
- keine fehlenden DynamicResources

## Phase 2 – ProjectView

- linke Spalte verkleinern
- Projektaktionen neu gruppieren
- Projektinformationen als kompakte Karte
- Farbschema kompakt darstellen
- Screenheader, Suche, Filter, Sortierung und Add Screen
- Kartenlayout für unterschiedliche Seitenverhältnisse
- leere Zustände
- Hover/Selection/Overflow vereinheitlichen

Abnahme:

- Mobile- und Desktop-Thumbnails sehen korrekt aus
- Hauptaktionen ohne Kontextmenü erreichbar
- Delete nicht versehentlich auslösbar
- Virtualisierung bleibt aktiv

## Phase 3 – ProjectDialog

- gemeinsame Shell und Seitennavigation
- General
- Device & Layout
- Alignment & Guides
- Theme & Colors
- Paths
- Advanced
- New-/Edit-Modus
- spezifische Footer-Aktionen
- Device-Größe in Edit Mode absichern

Abnahme:

- New Project vollständig möglich
- Edit verändert keine Größe unbemerkt
- Escape schließt mit Cancel
- Enter löst nur bei validem Formular die Primäraktion aus
- alle Pfade und Fehler verständlich

## Phase 4 – Grid entfernen

- UI-Optionen entfernen
- Renderer-Pass entfernen
- Snap-to-Grid aus Drop/Move/Resize entfernen
- Nudge unabhängig vom Grid machen
- Legacy-Properties zunächst kompatibel lassen
- Guidelines als Standard aktivieren
- Tests für keine Positionsänderung bei Auswahl

Abnahme:

- kein Grid sichtbar
- Auswahl verändert niemals X/Y/W/H
- Drop snappt nur über Guidelines
- Alt deaktiviert Guideline-Snap temporär
- alte Projekte laden
- Projekt speichern/laden erhält alle Controls unverändert

## Phase 5 – ScreenView Brushup

- Screenliste
- Toolbar
- read-only Device-/Canvas-Kontext
- Undo/Redo und Zoom
- rechte Rail/Flyouts
- Workspace-Farben
- Desktop-Hinweise
- keine Device-Größenbearbeitung in der Toolbar

Abnahme:

- Designerfläche gewinnt Platz
- Device-Kontext ist eindeutig
- keine missverständlichen Width-/Height-Editoren
- Toolbox und Properties bleiben erreichbar

## Phase 6 – Designer-Viewport

Auf separatem Teilbranch oder in klar getrennten Commits:

- gemeinsame Transformation
- zentrierte Device Area
- Hit-Test, Drag/Drop, Rubberband und Guidelines
- Template und Popup danach migrieren
- bestehende Device-Koordinaten beibehalten

Abnahme nach `Designer_Viewport_Umbau.md`.

## Phase 7 – TemplateView und PopupView

- gleiche Header-/Toolbarlogik
- gleiche Farben und Abstände
- gleiche Karten
- gleiche read-only Projektgröße
- dieselben Dialog- und Overflow-Regeln

## Phase 8 – Cleanup

- ungenutzte Styles entfernen
- auskommentierte XAML-Blöcke löschen, nachdem Git den alten Stand hält
- gemischte Sprachen bereinigen
- Accessibility und Tastaturbedienung prüfen
- Release/x64 testen
- Screenshots aktualisieren

---

## 12. Tests

### Projekt

- New Project Mobile
- New Project Desktop 1440 × 900
- Custom Size
- Edit General
- Edit Theme
- Edit Paths
- Cancel verwirft Änderungen
- Save Changes übernimmt Änderungen
- bestehende Projektdatei bleibt lesbar

### Grid-Entfernung

- altes Projekt mit `ShowGrid=true` laden
- kein Grid wird angezeigt
- Control auswählen: keine Koordinatenänderung
- Control bewegen: keine Rasterquantisierung
- Control resizen: keine Rasterquantisierung
- Toolbox-Drop: kein Grid-Snap
- Guidelines funktionieren
- Alt deaktiviert Snap
- Arrow = 1 px
- Shift+Arrow = 10 px
- Undo/Redo korrekt

### ProjectView

- 1, 10, 50 und 200 Screens
- Mobile-, Tablet- und Desktop-Seitenverhältnisse
- Suche
- Filter
- Sortierung
- Karten-/Listenansicht
- Zoom
- Virtualisierung
- Auswahl und Kontextmenü

### ScreenView

- Zoom 25–200 %
- Fit to View
- Resize des Fensters
- Toolbox offen/geschlossen
- Properties offen/geschlossen
- Screenwechsel
- Undo/Redo
- Desktop-Canvas
- Mobile-Canvas

### Farben

- Kontrast aktiver/inaktiver Navigation
- Disabled-Zustände
- Hover
- Focus
- Selection
- Danger
- Success
- kein Project ColorSchema färbt die Workbench um

---

## 13. Definition of Done

Der UI Brushup ist abgeschlossen, wenn:

- das UI weiterhin eindeutig wie VIA.UserFlow aussieht
- ProjectView und ScreenView dieselbe visuelle Sprache verwenden
- die wichtigsten Aktionen sofort erkennbar sind
- Desktop-Projekte bei Neuanlage klar angeboten werden
- Projektweite Device-/Canvas-Größe nicht in ScreenView editierbar ist
- der Project-Edit-Dialog klar gegliedert ist
- das alte Pixel-Grid weder sichtbar noch verhaltenswirksam ist
- Alignment-Guidelines die zentrale Snap-Hilfe sind
- Auswahl allein niemals Controls verschiebt
- alle Workbench-Farben über zentrale Tokens laufen
- Project ColorSchema und Workbench-Chrome getrennt sind
- alte Projekte ohne Migration laden
- Speichern/Laden/Undo/Redo unverändert funktionieren
- Debug- und Release-Build erfolgreich sind
- die manuellen Abnahmetests dokumentiert wurden

---

## 14. Nicht tun

- keine komplett neue SaaS-Oberfläche
- keine weiße Hauptnavigation anstelle des VIA-Shells
- keine Device-Größe direkt in ScreenView ändern
- kein Grid nur aus Gewohnheit behalten
- kein unsichtbares Grid-Snapping
- keine harten Hexfarben verteilt in Views
- keine Projektfarben für App-Buttons
- keine neuen NuGet-Abhängigkeiten für reines Styling
- kein Big-Bang-Refactoring von Designer und UI gleichzeitig
- keine stillen Änderungen am JSON-Format
- keine automatische Skalierung bestehender Screens
- keine großen Schatten und übertrieben runden Karten
- keine primären Aktionen nur als unbeschriftete Icons

---

## 15. Startprompt für den umsetzenden Chat

```text
Bitte lies zuerst CHAT-OVERVIEW.md, den aktuellen VS2AI-Export,
Designer_Viewport_Umbau.md und UI_BRUSHUP_MASTERPLAN.md.

Der aktuelle Export ist die technische Wahrheit.

Wir setzen den UI Brushup in kleinen Phasen um.
Beginne ausschließlich mit Phase 0 und Phase 1:
Bestandsaufnahme sowie zentrale Workbench-Farb- und Layout-Tokens.

Wichtige Entscheidungen:
- Bestehender VIA-Look bleibt erhalten.
- VIA.WPF X-Controls verwenden.
- Workbench-Farben strikt vom Project ColorSchema trennen.
- Device-/Canvas-Größe ist projektweit und in ScreenView nur read-only.
- Das bisherige Pixel-Grid wird entfernt.
- Alignment-Guidelines bleiben die zentrale Snap-Hilfe.
- Alte Projektdateien müssen unverändert ladbar bleiben.
- Keine Architektur-, Persistenz- oder Public-API-Änderung ohne Rückfrage.
- Keine vollständigen Folgephasen vorziehen.
- Nach jeder Phase Build und manuelle Prüfschritte nennen.

Prüfe zuerst die aktuell betroffenen vollständigen Dateien und nenne danach
den kleinsten sicheren Änderungssatz für Phase 1.
```
