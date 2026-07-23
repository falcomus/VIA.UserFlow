# CHAT-OVERVIEW.md

# Projekt: VIA.UserFlow

## 1. Zweck dieses Dokuments

Dieses Dokument ist der kompakte Übergabe-Merkzettel für neue ChatGPT-Chats zur Weiterentwicklung von **VIA.UserFlow**.

Es ersetzt nicht den Quellcode und nicht den VS2AI-Export. Es erklärt aber schnell:

- was VIA.UserFlow ist,
- wie die Solution aufgebaut ist,
- welche Architekturregeln zwingend gelten,
- welche Bereiche stabil sind,
- welche offenen Baustellen bekannt sind,
- wie gebaut und getestet werden soll,
- welche Dateien bei Folgearbeiten zusätzlich benötigt werden.

Beim Start eines neuen Chats immer zusammen bereitstellen:

1. dieses `CHAT-OVERVIEW.md`,
2. den neuesten VS2AI-Export,
3. optional das aktuelle Projekt-ZIP oder Repository.

Der neueste VS2AI-Export ist die technische Wahrheit. Dieses Dokument ist nur der Arbeitskompass.

---

## 2. Kurzbeschreibung

**VIA.UserFlow** ist eine WPF/.NET-Desktop-Anwendung zur Erstellung und Vorschau von Mockups, Screens, Templates, Popups und UI-Flows.

Das Projekt enthält einen komplexen Mockup-Designer mit:

- Screen-, Template- und Popup-Bearbeitung,
- Bands, Pages und DesignControls,
- SkiaSharp-basiertem Rendering,
- Live Preview,
- ActionAreas für Navigation und Interaktion,
- Asset-System für SVG/PNG,
- Undo/Redo über Snapshots,
- Guidelines/Snapping,
- JSON-basierter Projektpersistenz,
- Thumbnail-/Preview-System.

Ziel ist ein interaktives Mockup-/Wireframing-Werkzeug, das mobile/desktopartige Oberflächen entwerfen, speichern, rendern und später als klickbare User-Flows simulieren kann.

---

## 3. Aktueller technischer Stand

Aktueller Stand laut VS2AI-Export:

- Solution: `VIA.UserFlow.sln`
- Exportdatum: `2026-07-19 12:18:55`
- Branch: `master`
- Commit: `4c54306ea053ec02198037ba3d9d70428c2b44c5`
- Zielplattform: Windows / WPF
- Framework: `net9.0-windows`
- Hauptprojekte:
  - `VIA.UserFlow`
  - `VIA.Mockup`
  - `VIA.Mockup.Snapshots`
  - `VIA.Mockup.Guidelines`

Wichtig: Einige Bereiche wurden in den letzten Optimierungsphasen angepasst. Der Export vom 19.07.2026 ist maßgeblich, nicht ältere Chat-Aussagen.

---

## 4. Technischer Stack

- Sprache: C#
- UI: WPF
- Framework: .NET 9 / `net9.0-windows`
- Architektur: WPF-Anwendung plus separate Mockup-Librarys
- Pattern: MVVM, ObservableObject, Commands, DependencyProperties, Messenger
- Rendering: SkiaSharp / SkiaSharp.Views.WPF
- SVG: Svg.Skia
- JSON: System.Text.Json mit eigenen Convertern
- MVVM: CommunityToolkit.Mvvm
- Drag & Drop: gong-wpf-dragdrop
- UI/Controls: HandyControls, MaterialDesign/MahApps-Ressourcen in Teilen
- Virtualisierung: VirtualizingWrapPanel im Asset-Dialog
- Snapshot-Kompression: GZip + SHA-256 Payload-Vergleich

Wichtige Abhängigkeiten müssen vor einer Veröffentlichung in Third-Party-/Lizenzhinweisen geprüft und genannt werden.

---

## 5. Solution- und Modulübersicht

### `UserFlow/` – Anwendung

WPF-Startprojekt mit `App.xaml`, `MainWindow`, Splashscreen und Application Bootstrap.

Wichtig:

- startet die eigentliche Anwendung,
- referenziert `VIA.Mockup`,
- Release läuft x64-orientiert,
- UI-Sprache ist noch nicht final vereinheitlicht.

### `Mockup/` – Hauptlibrary

Zentraler Funktionsumfang des Designers.

Wichtige Ordner:

- `Domain/`  
  Modelle wie `Project`, `Screen`, `Band`, `BandPage`, `DesignControl`, `ScreenTemplate`, `ScreenPopup`.

- `Designer/`  
  Basisklassen und konkrete Designer: `BaseDesigner`, `ScreenDesigner`, `TemplateDesigner`, `PopupDesigner`, Mouse Handling, Rendering-Teilklassen, DragDrop.

- `Rendering/`  
  `SkiaRenderer`, `ScreenThumbnail`, `LiveViewControl`, Preview Controls, TextRenderer.

- `ViewModel/`  
  `MockupViewModel` als zentrale VM-Partial-Klasse für Commands, Collections, State, Storage, Messaging, Band-Logik.

- `SnapshotIntegration/`  
  `MockupSnapshotSerializer` und `MockupViewModel.Snapshots`. Diese Dateien gehören ausdrücklich in `VIA.Mockup`, nicht in `VIA.Mockup.Snapshots`.

- `JsonConverter/`  
  eigene Converter für Project, Screen, Band, Pages, Templates, Controls.

- `AssetSystem/`  
  Hybrid-Asset-Katalog für Embedded/Custom SVG und PNG, inklusive Preview-Rendering und Caching.

- `_ActionArea/`  
  ActionAreas und ActionDefinitionen für Navigation, externe Aktionen und perspektivisch Popups.

- `_ControlPool/`  
  vorhandene Mockup-Control-Typen für Buttons, Inputs, Charts, Display und Layout.

- `Views/`  
  Hauptansichten: Project, Screen, Template, Popup, LiveView, FlowView, Toolbox, Options, Help, About.

- `UIControls/`  
  PropertyEditor, UndoRedoBar, AlignmentToolbar, BreadcrumbBar, ZoomSlider usw.

### `Mockup.Snapshots/` – Undo/Redo-Engine

Eigenständige Snapshot-Library ohne direkte Abhängigkeit auf Mockup-Typen.

Enthält:

- `ISnapshotSerializer`
- `SnapshotEntry`
- `SnapshotStack`
- `SnapshotManager`
- `SnapshotResult`
- `SnapshotLabels`

Architekturentscheidung: Diese Library kennt keine `Screen`, `Project`, `ScreenTemplate` oder `ScreenPopup`. Die konkrete Serialisierung liegt in `Mockup/SnapshotIntegration/MockupSnapshotSerializer.cs`.

### `Mockup.Guidelines/` – Alignment/Snapping

Hostneutrale Berechnungslogik für temporäre Alignment-Guides.

Enthält:

- `AlignmentGuidelineManager`
- `GuidelineRect`
- `GuidelineLine`
- `GuidelineMatch`
- `GuidelineResult`
- `GuidelineOptions`

Wichtig: Keine WPF- oder Designer-Abhängigkeit. Diese Library berechnet nur, rendert aber nichts.

### `#DOCUMENTATION/`

Enthält Planungsdokumente:

- `MockupDesigner_Umbauplan.md`
- `#Plan Popups Livepreview für neuen Chat.md`
- `Mockup_ActionAreas_Wireframing_Plan.md`
- `selection_rendering_draworder_refactor.md`

Diese Dokumente sind Planungsgrundlage, aber nicht automatisch umgesetzt. Immer gegen aktuellen Code prüfen.

---

## 6. Was aktuell funktioniert

Nach aktuellem Stand funktionieren grundsätzlich:

- Projektstart und WPF-Anwendung,
- Screen-/Template-/Popup-Grundbearbeitung,
- Band/Page/Control-Modell,
- SkiaSharp-Rendering,
- Screen-Thumbnails,
- Asset-Katalog für SVG/PNG,
- ImageRef-Auswahl,
- Drag/Resize/Selection im Designer,
- Alignment Guidelines/Snapping,
- JSON-Speichern/Laden,
- Undo/Redo über SnapshotManager,
- Snapshot-Kompression mit GZip,
- schneller Snapshot-Duplikatvergleich über Hash/Länge,
- Preview-/Designer-Invalidation grundsätzlich über Messenger,
- LiveView-Grundstruktur mit `PreviewScreen`/NavigationTrail.

Bei allen Punkten gilt: Funktionsstand ist laut Export plausibel, aber vor Release muss gezielt getestet werden.

---

## 7. Offene oder kritische Themen

### Release-Vorbereitung

Vor Veröffentlichung fehlen noch:

- vollständige Release-Tests,
- einheitliche UI-Sprache Deutsch oder Englisch,
- Third-Party-Library- und Lizenzhinweise,
- EULA/Nutzungs-/Datenschutz-/Copyright-Texte, soweit erforderlich,
- Performance-Profiling in Release/x64,
- dokumentierter Smoke-Test,
- Entscheidung, ob VIA.UserFlow vor VIA.App.Designer-Umbauten veröffentlicht werden soll.

### Performance

Bekannte Hotspots:

- Snapshot-Erzeugung bei großen Project-Snapshots,
- `BackgroundImageBase64` in Projekt-/Screen-Serialisierung,
- Thumbnail-/Preview-Invalidation,
- komplette Thumbnail-Cache-Leerung bei `InvalidatePreviewMessage`,
- SkiaSharp native Ressourcen korrekt disposen,
- unnötige Allocations bei Rendering/Preview.

### Live Preview / Popups

Popups existieren als Domain- und Designzeitobjekte. Die vollständige interaktive LivePreview-Popup-Integration ist laut Plan noch ein offener Ausbau.

Ziel laut Plan:

- `PreviewPopupStack` im ViewModel,
- Popups rendern als Overlay,
- HitTesting im Popup priorisieren,
- ActionAreas im Popup interaktiv machen,
- Popup bei Navigation schließen.

### Designer-Architektur

Es gab Planungen zur weiteren Entkopplung:

- Designer sollen möglichst rendern, hit-testen und Requests senden.
- Collections bleiben im Model/ViewModel.
- Keine Schatten-DependencyProperties.
- Kein paralleles Band- oder Screen-Listenmodell im Designer.

Vor jedem Umbau prüfen, wie weit das im aktuellen Code bereits umgesetzt ist.

---

## 8. Wichtige Architekturentscheidungen

### Single Source of Truth

Collections existieren nur im Model/ViewModel, z. B. `Screen.Bands`, Template-Bands oder Popup-Bands.

Designer dürfen:

- rendern,
- hit-testen,
- Interaktionen auswerten,
- Requests senden.

Designer dürfen nicht eigenständig Collections spiegeln, synchronisieren oder dauerhaft mutieren.

Warum: Frühere Probleme entstanden durch Zustandsduplikation, nicht persistente Reihenfolgen und inkonsistente Designer-/Model-Zustände.

### Persistenz-Regel

Jede UI-Aktion mit Zustandsänderung muss im Model/ViewModel landen.

Beispiele:

- MoveBand,
- Resize,
- Toggle,
- Reorder,
- Control Move/Resize,
- Property-Änderung.

Nach Screen-/Template-/Popup-Wechsel muss der Zustand identisch sein. Sonst ist es ein Bug.

### Snapshot-System als Full Snapshot / Memento

Es wurde bewusst beim Full-Snapshot/Memento-Prinzip geblieben.

Warum:

- weniger fehleranfällig als Delta-/Command-Replay,
- stabiler für Undo/Redo über komplexe Objektgraphen,
- einfacher mit bestehender JSON-Persistenz kombinierbar.

Optimierungen wurden über Kompression, History-Limit, Hash-Vergleich und Payload-Größe gemacht, nicht über Architekturwechsel.

### `Mockup.Snapshots` kennt keine Mockup-Typen

`VIA.Mockup.Snapshots` bleibt generisch. Die konkrete Serialisierung liegt in `VIA.Mockup`.

Wichtig:

- `MockupSnapshotSerializer.cs` gehört nach `Mockup/SnapshotIntegration/`, nicht nach `Mockup.Snapshots/`.
- Keine ProjectReference von `Mockup.Snapshots` auf `Mockup` hinzufügen.

### Rendering und HitTesting müssen dieselben Bounds nutzen

Bei Screen, Popup, ActionArea und Preview gilt: Was gerendert wird, muss mit denselben Koordinaten hit-testbar sein.

Besonders wichtig für geplante Popup-LivePreview und ActionAreas.

### Preview-Invalidation nicht im Hotpath missbrauchen

Designer-Live-Rendering während Drag/Resize ist nötig. Globale Preview-/Thumbnail-Invalidation während jedes MouseMove ist teuer.

Prinzip:

- `InvalidateDesigner()` darf während Drag/Resize laufen.
- `InvalidatePreview()` möglichst erst am Ende einer abgeschlossenen Aktion, z. B. MouseUp.

---

## 9. Bereits gelöste oder entschärfte Probleme

Diese Punkte sind wichtig, damit sie nicht versehentlich zurückgebaut werden:

- `ImageRenderer` hat SVG/PNG-Caching; PNG-Cache ist begrenzt.
- `AssetCatalog.AllAssets` wurde gecacht.
- `SkBitmapToImageSource` verwendet Pointerzugriff statt unnötiger `.ToArray()`-Kopie.
- `ScreenThumbnail` wurde reduziert auf kleinere Cache-Grenzen, aber globale Cache-Leerung bei Preview-Invalidate ist weiterhin ein Thema.
- `SnapshotEntry` speichert JSON intern GZip-komprimiert.
- Snapshot-Duplikaterkennung erfolgt über ursprüngliche UTF8-Länge + SHA-256-Hash, nicht über dekomprimierte JSON-Strings.
- `SnapshotManager.Initialize` verwendet aktuell ein reduziertes History-Limit von 20.
- `Screen.BackgroundImage` selbst ist `[JsonIgnore]`; relevant ist `BackgroundImageBase64`.
- Owner-Crash beim ImageRef/Dialog wurde behandelt: Dialog-Owner darf nie der Dialog selbst sein.
- SkiaSharp-`SKPaint`/Bitmap-Ressourcen müssen konsequent mit `using` oder Dispose behandelt werden.
- `MockupSnapshotSerializer.cs` darf nicht im Snapshot-Projekt liegen, sonst fehlen Mockup-Typen wie `ScreenTemplate`.

Unsicher: Ob alle lokal erstellten Fixes im aktuell hochgeladenen Export bereits vollständig enthalten sind, muss bei jeder Datei anhand des Exportinhalts geprüft werden.

---

## 10. Bekannte Stolperfallen

- Keine kompletten Dateien aus altem Chat übernehmen, wenn sie nicht zum aktuellen Export passen.
- Keine Methoden, Regionen oder Kommentare stillschweigend entfernen.
- Keine DependencyProperty mit gleichem Namen in Base und Derived erneut registrieren.
- Keine neuen Typen, Dateien, Properties oder Enums ohne ausdrückliche Freigabe.
- Keine Snapshot-Integration in `Mockup.Snapshots` verschieben.
- Keine normale Projektpersistenz beschädigen, wenn Snapshot-JSON optimiert wird.
- `BackgroundImageBase64` darf nicht einfach global `[JsonIgnore]` bekommen, weil Projektdateien sonst Bilddaten verlieren können.
- Thumbnail-Invalidation kann Performance stark beeinflussen.
- SkiaSharp-Objekte sind native Ressourcen; fehlendes Dispose kann reale Leaks verursachen.
- WPF-Dialoge dürfen keinen falschen Owner bekommen.
- Debug- und Release-Konfiguration unterscheiden sich; Performance immer Release/x64 prüfen.
- VS2AI-Export kann sehr groß sein; bei Codeänderungen gezielt die betroffenen vollständigen Dateien verwenden.

---

## 11. Projektregeln für zukünftige ChatGPT-Chats

Für neue Chats gelten diese Regeln strikt:

- Immer zuerst `CHAT-OVERVIEW.md` und den neuesten VS2AI-Export lesen.
- Der neueste VS2AI-Export ist die technische Wahrheit.
- Keine Annahmen bei Codeänderungen, wenn die aktuelle Datei nicht vorliegt.
- Änderungen nur auf Basis der aktuellen Datei.
- Wenn eine Datei geändert wird: komplette Datei liefern, außer ausdrücklich Patch verlangt.
- Keine Platzhalter, keine ausgelassenen Methoden, keine gekürzten Regionen.
- Nichts entfernen, weil es „nicht mehr gebraucht“ erscheint, ohne Zustimmung.
- Keine stillen Umbenennungen.
- Keine neuen Abhängigkeiten ohne Zustimmung.
- Keine Architekturänderungen ohne Zustimmung.
- Keine Public-API-/Persistenz-/Dateiformatänderungen ohne Zustimmung.
- Schrittweise arbeiten, keine Big-Bang-Umbauten.
- Nach jeder Phase testen lassen und erst nach Bestätigung weiterarbeiten.
- Immer kurz sagen: was geändert wurde, wo, warum.
- Build/Test nur behaupten, wenn tatsächlich ausgeführt.
- Bei Unsicherheit diese klar markieren.

---

## 12. Build, Test und Start

Empfohlene Befehle im Solution Root:

```powershell
dotnet restore .\VIA.UserFlow.sln
```

Debug-Build:

```powershell
dotnet build .\VIA.UserFlow.sln -c Debug
```

Release-Build:

```powershell
dotnet build .\VIA.UserFlow.sln -c Release
```

Start der Anwendung:

```powershell
dotnet run --project .\UserFlow\VIA.UserFlow.csproj -c Debug
```

Release-Start:

```powershell
dotnet run --project .\UserFlow\VIA.UserFlow.csproj -c Release
```

Hinweis: Im Export ist kein separates Testprojekt sichtbar. Wenn Tests ergänzt werden, sollten sie als eigene Projekte aufgenommen und hier dokumentiert werden.

Manuelle Mindesttests vor Veröffentlichung:

1. Anwendung startet ohne Exception.
2. Projekt laden/speichern.
3. Screen anlegen, löschen, umbenennen.
4. Control einfügen, bewegen, resizen, löschen.
5. Undo/Redo für Move/Resize/Delete/Add prüfen.
6. Band resize/toggle/reorder prüfen.
7. Template und Popup öffnen/bearbeiten/speichern.
8. Asset auswählen/importieren.
9. Hintergrundbild setzen und Projekt neu laden.
10. LiveView/Preview prüfen.
11. Thumbnail-Ansichten prüfen.
12. Release/x64 Performance-Profiling durchführen.

---

## 13. Wichtige Begriffe und Abkürzungen

- **Project**: gesamtes Mockup-Projekt mit Screens, Templates, Popups usw.
- **Screen**: einzelne Oberfläche/Seite im Mockup.
- **Band**: vertikaler Layoutbereich eines Screens, z. B. Header, Content, Footer, Custom.
- **BandPage**: Seite innerhalb eines Bands.
- **DesignControl**: Basisklasse für platzierte UI-Elemente.
- **Template** / `ScreenTemplate`: wiederverwendbare Screen-Struktur.
- **Popup** / `ScreenPopup`: eigenständiges Popup-Layout mit eigenen Bands/Controls.
- **ActionArea / AA**: unsichtbares interaktives Element für Navigation/Aktionen.
- **LivePreview**: interaktive Vorschau eines Screens.
- **Thumbnail**: statische kleine Vorschau eines Screens.
- **Snapshot**: kompletter serialisierter Zustand für Undo/Redo.
- **Memento**: Architekturprinzip des Snapshot-Undo/Redo.
- **MSG.UI**: Messenger-Hilfszugriff für UI-Invalidation/Overlay.
- **InvalidateDesigner**: Designer neu zeichnen.
- **InvalidatePreview**: Preview/Thumbnail-System neu auslösen.
- **SSOT**: Single Source of Truth.

---

## 14. Offene nächste Schritte in sinnvoller Reihenfolge

1. Release-Ziel klären: interne Testversion oder öffentliche Veröffentlichung.
2. UI-Sprache festlegen: Deutsch oder Englisch.
3. Clean Release-Build ausführen.
4. Smoke-Test mit echter Bedienung durchführen.
5. Ara/Simran-Testfeedback einsammeln.
6. Kritische Bugs beheben, keine neuen Großumbauten vor Release.
7. Performance-Profiling Release/x64 wiederholen.
8. Thumbnail-/Preview-Invalidation weiter entschärfen, falls Messwerte schlecht bleiben.
9. `BackgroundImageBase64` in Snapshots weiter prüfen, ohne Projektpersistenz zu beschädigen.
10. Third-Party-Library-Liste und Lizenzhinweise erstellen.
11. EULA/Release-/Copyright-/Datenschutzhinweise klären.
12. Dokumentation für Installation, Bedienung und bekannte Einschränkungen erstellen.
13. Danach erst größere Themen wie LivePreview-Popups, Wireframing/ActionFlow oder VIA.App.Designer-Integration angehen.

---

## 15. Dateien, die ein neuer Chat zusätzlich bekommen sollte

Minimal:

- `CHAT-OVERVIEW.md`
- aktueller VS2AI-Export
- aktuelles Projekt-ZIP oder Repositoryzugriff

Für Snapshot-/Undo-Redo-Arbeiten:

- `Mockup.Snapshots/SnapshotEntry.cs`
- `Mockup.Snapshots/SnapshotStack.cs`
- `Mockup.Snapshots/SnapshotManager.cs`
- `Mockup.Snapshots/ISnapshotSerializer.cs`
- `Mockup/SnapshotIntegration/MockupSnapshotSerializer.cs`
- `Mockup/SnapshotIntegration/MockupViewModel.Snapshots.cs`
- `Mockup/ViewModel/MockupViewModel.Storage.cs`
- relevante Domain-Dateien, z. B. `Screen.cs`, `Project.cs`, `ScreenTemplate.cs`, `ScreenPopup.cs`

Für Designer-/Band-/Drag-/Resize-Arbeiten:

- `Mockup/Designer/BaseDesigner.cs`
- `Mockup/Designer/BaseDesigner.MouseHandler.cs`
- `Mockup/Designer/BaseDesigner.Renderer.cs`
- `Mockup/Designer/BaseDesigner.Bands.cs`
- `Mockup/Designer/ScreenDesigner.cs`
- `Mockup/Domain/Screen.cs`
- `Mockup/Domain/Band.cs`
- `Mockup/Domain/BandPage.cs`
- `Mockup/ViewModel/MockupViewModel.Band.cs`
- `Mockup/ViewModel/MockupViewModel.Commands.cs`

Für Rendering-/Preview-/Performance-Arbeiten:

- `Mockup/Rendering/SkiaRenderer.cs`
- `Mockup/Rendering/ScreenThumbnail.xaml.cs`
- `Mockup/Rendering/LiveViewControl.xaml.cs`
- `Mockup/AssetSystem/ImageRenderer.cs`
- `Mockup/AssetSystem/AssetCatalog.cs`
- `Mockup/Messages/Message.cs`

Für ActionArea/LivePreview/Popup-Arbeiten:

- `Mockup/_ActionArea/ActionArea.cs`
- `Mockup/_ActionArea/ActionDefinition.cs`
- `Mockup/Rendering/LiveViewControl.xaml.cs`
- `Mockup/Views/LiveView.xaml`
- `Mockup/ViewModel/MockupViewModel.Collections.cs`
- `Mockup/ViewModel/MockupViewModel.Messaging.cs`
- `Mockup/Domain/ScreenPopup.cs`
- `Mockup/Domain/Project.cs`
- Plan: `#DOCUMENTATION/#Plan Popups Livepreview für neuen Chat.md`

---

## 16. Startprompt für neue Chats

```text
Bitte lies zuerst CHAT-OVERVIEW.md und danach den aktuellen VS2AI-Export.

Behandle den VS2AI-Export als technische Wahrheit.
Halte Dich strikt an die Projektregeln aus CHAT-OVERVIEW.md.

Projekt: VIA.UserFlow / VIA.Mockup.
Framework: .NET 9 WPF.

Wichtig:
- Keine Annahmen bei Codeänderungen.
- Änderungen nur anhand der aktuellen vollständigen Datei.
- Wenn eine Datei geändert wird, vollständige Datei liefern.
- Keine Platzhalter, keine stillen Umbenennungen, keine entfernten Methoden/Regionen.
- Keine Architektur-, Persistenz- oder Public-API-Änderung ohne ausdrückliche Zustimmung.
- Single Source of Truth beachten: Collections liegen im Model/ViewModel, Designer rendern/hit-testen/senden Requests.

Wir machen bei den offenen nächsten Schritten weiter.
Bitte zuerst kurz prüfen, welche Dateien für die konkrete Aufgabe benötigt werden.
```

---

## 17. Leitsatz

**VIA.UserFlow ist kein kleiner Dialogeditor, sondern ein komplexes WPF/SkiaSharp-Mockup- und Flow-Designer-System. Änderungen müssen klein, nachvollziehbar, persistenzsicher und messbar erfolgen.**
