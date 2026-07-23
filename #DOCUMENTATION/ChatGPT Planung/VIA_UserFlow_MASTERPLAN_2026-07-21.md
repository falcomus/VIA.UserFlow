# VIA.UserFlow – Masterplan und Übergabe für den nächsten Chat

**Stand:** 21.07.2026  
**Projekt:** VIA.UserFlow / VIA.Mockup, ergänzend VIA.WPF  
**Framework:** .NET 9, WPF  
**Aktueller UserFlow-Branch:** `toolbox-rail-flyout`  
**Technische Basis:** neuester VS2AI-Export `VIA UserFlow.20260721-221140.vs2ai.context.md`  
**Export-Commit:** `718ea2c494b73c36607bd48e8137e29bcd036758`

---

## 1. Zweck

Dieses Dokument ist die Übergabe und der Arbeitsplan für einen neuen Chat.

Weitere Analyse-, Aufgaben- und Planungsdokumente können anschließend ergänzt werden. Der neue Chat soll diese Dokumente nicht einfach anhängen, sondern in diesen Masterplan integrieren:

1. doppelte Aufgaben zusammenführen,
2. bereits erledigte Punkte von offenen Aufgaben trennen,
3. Widersprüche sichtbar markieren,
4. technische Aussagen gegen den aktuellen Code prüfen,
5. Prioritäten und Abhängigkeiten neu ordnen,
6. für jede Phase klare Test- und Abnahmekriterien festlegen.

Der neueste VS2AI-Export ist die technische Wahrheit, solange noch kein direkter Repositoryzugriff besteht. Später hat der direkt gelesene aktuelle Repositorystand Vorrang.

---

## 2. Pflichtstart im neuen Chat

Zuerst lesen:

1. `#CHAT-OVERVIEW/CHAT-OVERVIEW.md`
2. dieses Masterplan-Dokument
3. den neuesten VS2AI-Export
4. alle neu hochgeladenen Aufgaben- und Planungsdokumente
5. bei VIA.WPF-Arbeiten die aktuellen vollständigen VIA.WPF-Dateien oder den direkten Repositorystand

Danach kurz feststellen:

- welcher Stand technisch maßgeblich ist,
- welche Punkte sicher erledigt sind,
- welche Änderungen nur vorbereitet, aber noch nicht bestätigt wurden,
- welche Aufgabe als Nächstes ansteht,
- welche aktuellen vollständigen Dateien dafür benötigt werden,
- welche Risiken oder Unklarheiten bestehen.

---

## 3. Verbindliche Projektregeln

- Keine Annahmen bei Codeänderungen, wenn die aktuelle vollständige Datei nicht vorliegt.
- Änderungen nur auf Basis aktueller vollständiger Dateien.
- Bei Änderungen vollständige Dateien liefern, außer ausdrücklich ein Patch verlangt wurde.
- Keine Platzhalter, ausgelassenen Methoden oder gekürzten Regionen.
- Nichts entfernen, nur weil es vermeintlich nicht mehr benötigt wird, ohne Zustimmung oder eindeutige aktuelle Referenzprüfung.
- Keine stillen Umbenennungen.
- Keine neuen Abhängigkeiten ohne Zustimmung.
- Keine Architekturänderungen ohne Zustimmung.
- Keine Änderungen an Public API, Persistenz oder Dateiformaten ohne Zustimmung.
- Schrittweise arbeiten, keine Big-Bang-Umbauten.
- Nach jeder Phase lokal bauen und gezielt testen lassen.
- Erst nach Bestätigung mit der nächsten Phase fortfahren.
- Immer kurz nennen: was geändert wurde, wo und warum.
- Build oder Test nur behaupten, wenn er tatsächlich ausgeführt wurde.
- Lokale Änderungen des Benutzers erhalten.
- Keine pauschalen Export-Hashprüfungen, wenn aktuelle vollständige Dateien vorliegen.
- Backup und automatischer Rollback bei Buildfehler bleiben sinnvoll.
- Für Installerhinweise immer schreiben: **„VIA.UserFlow vorher schließen.“**
- Keine nicht betroffenen Bereiche anfassen.

---

## 4. Projektüberblick

VIA.UserFlow ist eine WPF-Desktopanwendung zum Erstellen, Bearbeiten, Speichern und interaktiven Vorschauen von:

- Screens,
- Templates,
- Popups,
- UI-Controls,
- ActionAreas,
- User-Flows.

Wichtige technische Bereiche:

- WPF und MVVM
- SkiaSharp-basiertes Rendering
- Screen-, Template- und Popup-Designer
- JSON-basierte Projektpersistenz
- Undo/Redo über vollständige Snapshots
- Guidelines und Snapping
- Asset-System für PNG und SVG
- Screen-Thumbnails und LiveView
- Control-Toolbox
- VIA.WPF als separate UI-Control-Bibliothek

Architekturgrundsatz: Model und ViewModel sind die Single Source of Truth. Designer rendern, hit-testen und senden Requests. Sie sollen keine parallelen dauerhaften Zustände oder Collections pflegen.

---

## 5. Aktueller Arbeitsstand

### 5.1 Sicher umgesetzt oder im aktuellen Export nachvollziehbar

#### Toolbox-Rail und Gruppen

Bereits bearbeitet wurden:

- Rail-/Flyout-Grundstruktur
- Auswahl nur beim tatsächlichen Öffnen
- alphabetische Gruppen
- Social-Gruppe
- Popups-Bereich
- PNG-basierte Control-Previews

#### Preview-Performance

Die Control-Previews wurden von runtime-basiertem SVG-/Skia-Laden auf gecachte PNG-Vorschauen umgestellt.

Vom Benutzer bestätigt:

- erstes Öffnen der Toolbox ungefähr eine Sekunde,
- spätere Öffnungen praktisch unmittelbar.

#### Preview-Invalidation

Die alte globale `InvalidatePreviewMessage`-Logik wurde entfernt beziehungsweise ersetzt.

Der aktuelle Export zeigt:

- keine produktive `InvalidatePreviewMessage`,
- keine alten `MSG.UI.InvalidatePreview()`-Aufrufe,
- gezielte Aktualisierung sichtbarer `ScreenThumbnail`s,
- gezielte LiveView-Aktualisierung,
- `ScreenThumbnail` bleibt ausdrücklich erhalten.

#### SelectionIndicator in VIA.WPF

Der blaue SelectionIndicator ist bereits vorhanden.

`XListBox` besitzt:

- `ShowSelectionIndicator`
- `SelectionIndicatorBrush`
- `SelectionIndicatorWidth`
- `SelectionIndicatorCornerRadius`

`XNavigationList` besitzt dieselben Eigenschaften. Der Indicator darf nicht parallel ein zweites Mal eingebaut werden.

#### XNavigationList-Standarddarstellung

`XNavigationList` unterstützt bereits weitgehend:

- `Title`
- `SubTitle`
- Icon
- Badge
- Edit- und Delete-Aktionen
- Commands und CommandParameter
- automatische Konventionsbindung an gleichnamige Eigenschaften des Datenobjekts
- Sichtbarkeit der Aktionen bei Hover beziehungsweise Auswahl

### 5.2 Vorbereitet, aber noch nicht als erfolgreich bestätigt

#### UserFlow Preview-Cleanup

Vorbereitet wurde ein gezielter Cleanup ohne pauschale Export-Zustandsprüfungen:

- alte ControlPreview-SVG-Ressourcen entfernen,
- alten SVG-Projekteintrag entfernen,
- auskommentierten doppelten Preview-HitTest-Code entfernen,
- lokale Toolbox-Änderungen wie `VerticalContentAlignment="Center"` erhalten.

Der endgültige lokale Erfolg wurde im letzten Chat nicht bestätigt.

#### ToolboxView-Code-behind-Cleanup

Eine korrigierte vollständige Datei wurde vorbereitet, nachdem ein Regex-Patch wegen einer übrig gebliebenen Klammer einen Buildfehler verursacht hatte.

Der endgültige lokale Erfolg der korrigierten Version wurde nicht bestätigt.

#### XListBox Phase A2: Title und SubTitle

Vorbereitet wurde ein VIA.WPF-Patch für:

- `Title`
- optionales `SubTitle`
- ersatzweise `Description`
- automatische ein- oder zweizeilige Darstellung
- Erhalt von `ItemTemplate` und `ContentTemplate`

Der erste Installer brach wegen zwei Solution-Dateien ab. Version 2 verwendet ausdrücklich:

`C:\VIA_DEVELOPMENT\PROJECTS\VIA.WPF\VIA.WPF\VIA.WPF.slnx`

Die tatsächliche Installation und der Build von Version 2 wurden noch nicht bestätigt.

---

## 6. UI-Verbesserungsvorschläge aus dem neuen Screenshot

Der bereitgestellte Screenshot enthält folgende gewünschte UI-Richtung. Diese Punkte sind Planungsanforderungen und noch nicht automatisch umgesetzt.

### 6.1 Toolbox nach rechts verlegen

Die Toolbox soll nicht mehr links neben dem Designer liegen, sondern rechts.

Ziel:

- mehr zusammenhängende Arbeitsfläche links und in der Mitte,
- Eigenschaften und Toolbox-Funktionen räumlich zusammenführen,
- bestehende rechte Rail als zentrale vertikale Werkzeugleiste verwenden.

Vor Umsetzung prüfen:

- Auswirkungen auf Hauptlayout, Breiten, Grid-Spalten und Flyout-Richtung,
- Persistenz vorhandener UI-Zustände,
- Verhalten bei kleinen Fensterbreiten,
- Drag-and-Drop aus der Toolbox in den Designer.

### 6.2 Rail-Buttons vollständig untereinander

Alle Buttons der rechten Rail sollen in einer durchgehenden vertikalen Reihenfolge angeordnet werden.

Dabei soll es keine optisch getrennten oder weit auseinanderliegenden Button-Gruppen geben, sofern keine fachliche Gruppierung nötig ist.

### 6.3 ActionBar-Buttons ganz oben

Die Buttons der ActionBar sollen im rechten vertikalen Bereich ganz oben stehen.

Zu klären:

- welche Buttons exakt zur ActionBar gehören,
- ob sie permanent oder kontextabhängig sichtbar sind,
- ob Tooltips und aktive Zustände erhalten bleiben.

### 6.4 Designer-Buttons vertikal darunter

Die derzeit horizontal angeordneten Designer-Buttons sollen vertikal unter den ActionBar-Buttons angeordnet werden.

Dazu gehören voraussichtlich Ausrichten, Verteilen und weitere Designeraktionen.

Vor Umsetzung müssen die aktuellen Commands und Tooltips ermittelt werden. Keine Funktion darf beim Umbau verloren gehen.

### 6.5 Unterstes Werkzeug vertikal an der Toolbox

Ein derzeit unten horizontal angeordnetes Bedienelement soll ganz unten vertikal an der Toolbox beziehungsweise Rail sitzen.

Der Screenshot deutet auf ein dauerhaft unten verankertes Element hin. Vor dem Umbau muss eindeutig bestimmt werden, welches Control gemeint ist, wahrscheinlich Zoom oder ein vergleichbares Designerwerkzeug.

### 6.6 Auswahl „Properties“ entfällt

Eine separate Auswahl beziehungsweise Umschaltung zu „Properties“ soll entfallen.

Eigenschaften sollen automatisch angezeigt werden, sobald ein Item ausgewählt ist.

Zu prüfen:

- aktueller Selection-State,
- Verhalten ohne Auswahl,
- Verhalten bei Mehrfachauswahl,
- Screen-, Band-, Control-, Template- und Popup-Auswahl,
- ob die Properties-Fläche bei fehlender Auswahl verborgen oder leer angezeigt wird.

### 6.7 Durch das rechte Toolbox-Konzept überflüssige Buttons entfernen

Der Screenshot markiert obere Bedienelemente als möglicherweise überflüssig, sobald die Toolbox rechts angeordnet ist.

Diese Elemente dürfen nicht pauschal entfernt werden. Zuerst müssen ihre Commands, Zustände und bisherigen Aufgaben identifiziert werden. Danach ist zu entscheiden, ob sie:

- entfallen,
- in die rechte Rail wandern,
- kontextabhängig eingeblendet werden,
- oder weiterhin benötigt werden.

### 6.8 Abnahmekriterien für den UI-Umbau

- Alle bisherigen Commands bleiben erreichbar.
- Keine Designerfunktion geht verloren.
- Toolbox-Drag-and-Drop funktioniert weiterhin.
- Eigenschaften erscheinen zuverlässig bei Auswahl.
- Kein doppelter Properties-Schalter bleibt bestehen.
- Rechte Rail ist klar, vertikal und konsistent aufgebaut.
- Layout funktioniert bei normaler und kleiner Fensterbreite.
- Light- und Dark-Theme werden geprüft.
- Erst nach einem isolierten Layout-Prototyp die eigentliche Verdrahtung ändern.

---

## 7. Nächste Arbeitsreihenfolge

### Phase 0 – Morgen: direkter Repositoryzugriff für den Chat

Vor weiteren größeren Umbauten soll ein lokales Tool gebaut werden, über das der Chat aktuelle Projektdateien direkt lesen und später kontrolliert schreiben kann.

Details stehen in Abschnitt 8.

### Phase 1 – Offene vorbereitete Patches abschließen

1. XListBox Title/SubTitle Installer v2 ausführen.
2. VIA.WPF-Haupt-Solution bauen.
3. XListBox im Showcase beziehungsweise einer kleinen Testansicht prüfen.
4. Erst nach Bestätigung fortfahren.

Danach:

5. Status des UserFlow Preview-Cleanup prüfen.
6. Status des ToolboxView-Code-behind-Cleanups prüfen.
7. Nur tatsächlich fehlende Änderungen erneut auf Basis aktueller Dateien vorbereiten.

### Phase 2 – XListBox Standarddarstellung vervollständigen

Nach erfolgreicher Phase A2:

- Badge
- BadgeContent und Variante
- Edit-Button und Command
- Delete-Button und Command
- CommandParameter
- Sichtbarkeit bei Hover beziehungsweise Auswahl
- Aktionsbuttons rechts in der Title-Zeile
- Spezialdarstellungen über `ItemTemplate` weiterhin ermöglichen

Vorher prüfen, ob ein Teil bereits im aktuellen VIA.WPF-Code umgesetzt wurde.

### Phase 3 – Toolbox funktional fertigstellen

Offene Punkte:

- spontanes Flyout-Schließen reproduzieren und beheben,
- Suche in Controls, Templates und Popups prüfen,
- Template- und Popup-Karten prüfen,
- Drag-and-Drop für Templates und Popups prüfen,
- Layout-Karten höher und proportionaler gestalten,
- Section-Badge für vertikale Separatoren prüfen,
- ActionArea-Vorschau und Handles verbessern,
- Previews nur dort behalten, wo sie funktional benötigt werden.

### Phase 4 – UI-Umbau nach Screenshot

In kleinen Teilphasen:

1. Layout-Prototyp Toolbox rechts
2. Rail-Buttons untereinander
3. ActionBar oben
4. Designerbuttons vertikal darunter
5. unterstes Werkzeug fest unten
6. Properties automatisch bei Auswahl
7. überflüssige Umschalter nach Funktionsprüfung entfernen
8. Theme-, Größen- und Interaktionstest

Nach jeder Teilphase bauen und manuell testen.

### Phase 5 – Technische Stabilisierung

Mit weiteren Planungsdokumenten abgleichen:

- Preview- und Rendering-Lebenszyklen
- Snapshot-Performance
- BackgroundImageBase64 und große Payloads
- Skia-Ressourcen und Disposal
- Cache-Grenzen
- Designer-Hotpaths
- persistente Zustände und Single Source of Truth
- gezielte Smoke-Tests

Keine Architekturänderung ohne separate Entscheidung.

### Phase 6 – Größere Funktionsblöcke

Erst nach Stabilisierung:

- interaktive Popups in LiveView
- Popup-Stack zur Laufzeit
- Popup-HitTesting vor Screen-HitTesting
- ActionAreas in Popups
- Navigation und Popup-Schließen
- Wireframing und ActionFlow
- spätere VIA.App.Designer-Integration

### Phase 7 – Release-Vorbereitung

- vollständige Debug- und Release-Tests
- Release/x64-Profiling
- einheitliche UI-Sprache
- Third-Party- und Lizenzhinweise
- Copyright, EULA und Datenschutz
- Benutzer- und Installationsdokumentation
- bekannte Einschränkungen
- reproduzierbarer Smoke-Test

---

## 8. Erinnerung für morgen: VIA Project Bridge bauen

### Ziel

Morgen soll ein lokales Tool entstehen, das dem Chat kontrollierten Zugriff auf aktuelle Projektdateien ermöglicht.

Der Hauptnutzen:

- nicht mehr bei jeder Änderung einen riesigen VS2AI-Export parsen,
- aktuelle Dateien direkt lesen,
- lokale Änderungen sofort sehen,
- `git status` und `git diff` prüfen,
- Builds starten und vollständige Compilerfehler erhalten,
- Änderungen kontrolliert schreiben,
- bei Fehlern automatisch zurückrollen.

### Wichtige technische Voraussetzung

Ein normales lokales Konsolenprogramm allein genügt nicht, weil der Chat es nicht selbst aufrufen kann.

Das Tool muss als echte Schnittstelle verbunden werden, vorzugsweise als:

- lokaler MCP-Server,
- kompatibler Connector,
- oder eine vergleichbare freigegebene Tool-Integration.

### Empfohlener MVP

Zunächst nur lesend:

- `list_files`
- `read_file`
- `search_text`
- `git_status`
- `git_diff`
- `build_solution`
- `read_build_log`

Danach kontrollierte Schreibfunktionen:

- `write_file`
- `apply_patch`
- `create_backup`
- `restore_backup`

### Sicherheitsregeln

- feste Allowlist für freigegebene Repository-Ordner,
- keine Pfade außerhalb dieser Wurzeln,
- standardmäßig nur Lesezugriff,
- Schreibaktionen nur nach ausdrücklicher Bestätigung,
- vor jeder Schreibaktion Diff und Backup,
- keine Löschaktion ohne gesonderte Zustimmung,
- Buildausgabe vollständig zurückgeben,
- alle Aktionen protokollieren,
- keine beliebigen Shell-Befehle,
- nur klar definierte Funktionen.

### Empfohlene Entwicklungsreihenfolge

1. .NET-9-MCP-Projekt außerhalb der Produkt-Solutions anlegen.
2. Konfiguration für erlaubte Repository-Wurzeln.
3. sichere Pfadnormalisierung und Traversal-Schutz.
4. `list_files`, `read_file`, `search_text`.
5. `git_status`, `git_diff`.
6. `build_solution` mit vollständigem Log.
7. Verbindung mit dem Chat testen.
8. erst danach Schreibzugriff ergänzen.
9. Backup, Diff und Rollback integrieren.
10. mit einer kleinen ungefährlichen Datei testen.

### Konkrete freizugebende Repositories

Mindestens:

- `C:\VIA_DEVELOPMENT\PROJECTS\VIA UserFlow`
- `C:\VIA_DEVELOPMENT\PROJECTS\VIA.WPF`

Später optional weitere VIA-Repositories.

---

## 9. Benötigte Dateien nach Themengebiet

Solange der direkte Repositoryzugriff noch nicht besteht:

### Toolbox

- `Mockup/Views/ToolboxView.xaml`
- `Mockup/Views/ToolboxView.xaml.cs`
- `Mockup/ViewModel/MockupViewModel.ToolboxFiltering.cs`
- relevante Grouping-Klassen
- relevante DragDrop-Klassen

### VIA.WPF XListBox

- `XListBox.cs`
- `XListBoxItem.cs`
- `XListBox.xaml`
- Theme- und Brush-Ressourcen bei visuellen Änderungen

### Hauptlayout und rechte Toolbox

- `MainWindow.xaml`
- betroffene Shell- oder Hauptview-Dateien
- ToolboxView
- ActionBar
- AlignmentToolbar
- Zoom-Control
- Properties-Host
- zugehörige Code-behind- und ViewModel-Dateien

### LiveView und Popups

- `Mockup/Rendering/LiveViewControl.xaml`
- `Mockup/Rendering/LiveViewControl.xaml.cs`
- `Mockup/Views/LiveView.xaml`
- `Mockup/Views/LiveView.xaml.cs`
- `Mockup/_ActionArea/ActionArea.cs`
- `Mockup/_ActionArea/ActionDefinition.cs`
- `Mockup/Domain/ScreenPopup.cs`
- `Mockup/Domain/Project.cs`
- relevante ViewModel-Partial-Dateien

---

## 10. Startprompt für den neuen Chat

```text
Bitte lies zuerst:

1. #CHAT-OVERVIEW/CHAT-OVERVIEW.md
2. VIA_UserFlow_MASTERPLAN_Neuer_Chat_2026-07-21.md
3. den neuesten VS2AI-Export
4. alle zusätzlich hochgeladenen Aufgaben- und Planungsdokumente

Der neueste Export ist die technische Wahrheit, solange noch kein direkter Repositoryzugriff besteht.

Fasse zusätzliche Dokumente in den Masterplan ein:
- Duplikate zusammenführen,
- erledigte und offene Punkte trennen,
- Widersprüche markieren,
- Aufgaben technisch gegen den aktuellen Code prüfen,
- Prioritäten und Abhängigkeiten ordnen.

Halte die Projektregeln strikt ein:
- keine Annahmen ohne aktuelle vollständige Dateien,
- komplette geänderte Dateien,
- keine Platzhalter oder ausgelassenen Methoden,
- keine stillen Umbenennungen,
- keine neuen Abhängigkeiten, Architektur-, Public-API-, Persistenz- oder Dateiformatänderungen ohne Zustimmung,
- schrittweise arbeiten und nach jeder Phase testen lassen,
- Build nur behaupten, wenn er tatsächlich ausgeführt wurde.

Wichtiger Termin:
Morgen zuerst einen lokalen VIA Project Bridge als MCP-Tool planen und als read-only MVP umsetzen, damit aktuelle Dateien direkt gelesen, gesucht, gebaut und später kontrolliert geschrieben werden können.

Aktueller nächster Codepunkt:
Den vorbereiteten VIA.WPF-XListBox-Title/SubTitle-Patch v2 lokal ausführen und testen. Erst nach bestätigtem Build mit Badge sowie Edit/Delete fortfahren.

Beziehe außerdem die UI-Vorschläge aus dem Screenshot ein:
- Toolbox rechts,
- alle Rail-Buttons untereinander,
- ActionBar-Buttons ganz oben,
- Designerbuttons vertikal darunter,
- unterstes Werkzeug ganz unten an der Toolbox,
- separate Properties-Auswahl entfernen,
- Properties automatisch bei Item-Auswahl anzeigen,
- durch das neue rechte Layout überflüssige Buttons erst nach Funktionsprüfung entfernen.
```

---

## 11. Leitsatz

**Kleine, nachvollziehbare und lokal getestete Änderungen sind wichtiger als schnelle große Umbauten. Aktueller Code hat Vorrang vor Planungsannahmen.**
