# VIA.UserFlow – Änderungen und Erweiterungen

Stand: 26. Juli 2026. Dieses Dokument fasst die in den letzten Arbeitstagen umgesetzten Änderungen in VIA.UserFlow zusammen. Es beschreibt den fachlichen Stand; die zugehörigen, kleinen Korrektur-Commits sind in der Git-Historie nachvollziehbar.

## Oberfläche und VIA.WPF-Integration

- Die gesamte Editor-Chrome verwendet die semantischen VIA.WPF-Theme-Tokens statt lokaler, fester Farben. Das betrifft Light- und Dark-Mode, Oberflächen, Texte, Borders, Auswahlzustände, Navigation und Toolboxen.
- Lokale Farbdefinitionen und redundante Styles wurden dort entfernt, wo VIA.WPF bereits einen semantischen Token oder ein passendes Control bereitstellt.
- Die ProjectView, Listen- und Editoransichten wurden optisch vereinheitlicht; Popup-Listen verwenden nun dieselben Aktionen und Icons wie Screen-Listen.
- Die Projekt-Kacheln und ihre Auswahl-, Home- und Vorschauzustände wurden überarbeitet. Der Home-Screen ist eindeutig: genau ein Screen ist Home, sein Haus-Icon ist weiß; alle anderen Icons sind grau und setzen den betreffenden Screen per Klick als Home.
- Die Theme-Auswahl in XWindow sowie der Sprachumschalter wurden aktiviert und in die App-Integration aufgenommen.

## Themes, Farben und Farbverwaltung

- VIA.UserFlow nutzt die überarbeiteten Light-/Dark-Themes aus VIA.WPF mit klar getrennten semantischen Rollen für Primary, Accent, Success, Warning, Danger und Info.
- Die Theme-Palette zeigt und bearbeitet Light- und Dark-Farben gemeinsam, erzeugt neue Paletten aus bestehenden und aktualisiert die Live-Vorschau.
- Der generische Color-Scheme-Editor aus UserFlow wurde in VIA.WPF überführt und wird von UserFlow wiederverwendet. Dadurch können auch andere VIA.WPF-Anwendungen Farbschemata bearbeiten.
- Lokale Anwendungsfarben sollen nur noch ergänzt werden, wenn der nötige semantische Token in VIA.WPF tatsächlich fehlt.

## Designer und Arbeitsbereich

- Das frühere Designer-Grid samt Raster-Rendering und Raster-Snapping wurde entfernt.
- Smart Guides/Ausrichtungslinien sind als schaltbare Designerfunktion vorhanden.
- Der Zoom arbeitet über den VIA.WPF-`XZoomSlider`, inklusive Reset, frei wählbarer analoger oder diskreter Schritte sowie Zoom-Anker am Mauszeiger oder im Zentrum.
- Screen-, Template- und Popup-Designer teilen sich konsistente Toolbars. Diese bündeln Alignment, Smart Guides, Undo/Redo und Zoom ohne zusätzliche Control-Borders; Separatoren trennen nur Funktionsgruppen.
- Die Designer-Viewport- und Bandbehandlung wurde stabilisiert: Parkbereiche, Screen-Bounds, Screen-Höhen, Scrollverhalten, Band-Drop, Parent-Wechsel und Resize-Handles wurden bereinigt.
- Für Screens werden Device- und Screen-Größe weiterhin sichtbar gehalten; lange Screens bleiben im Preview scrollbar.
- Popups behalten ihren eigenständigen, auf die Popup-Fläche fokussierten Designercharakter.
- Eine Schnell-/Favoritenleiste für häufig verwendete Controls wurde vorbereitet; Favoriten werden in den Designeransichten wiederverwendbar angezeigt.

## Controls und gemeinsame VIA.WPF-Bausteine

- `XZoomSlider` wurde nach VIA.WPF überführt, mit Demo ergänzt und funktional korrigiert (Bewegung, Schrittweite, Anzeige-Badge und Reset).
- `XColorSchemeView` und der Color-Scheme-Editor wurden nach VIA.WPF überführt und für bestehende sowie neue Themes nutzbar gemacht.
- `XUndoRedoBar` wurde als allgemeiner VIA.WPF-Baustein erstellt und ersetzt die frühere lokale UserFlow-Leiste.
- Größen der VIA-Controls wurden vereinheitlicht: Small, Medium und Large bleiben über die Toolbar- und Editorcontrols hinweg konsistent.
- Die Icon-Verwendung wurde auf die VIA-Markup-Extensions vereinheitlicht, zum Beispiel `Icon="{via:MaterialIcon Kind=...}"`.

## Toolbox und Property-Editor

- Die Controls-Toolbox nutzt eine semantische Navigation mit Auswahlindikator, Hover-Zustand und Zählern.
- Die Properties-Toolbox ist an dieselbe Struktur angeglichen: volle Höhe, linke Kategorienavigation ohne Innenabstand, gleiche Auswahl- und Badge-Darstellung sowie gleicher Hintergrundaufbau.
- Der Property-Editor gruppiert Eigenschaften nach fachlichen Kategorien wie All, Appearance, Layout, Typography, Icon, Content oder Behavior.
- Die Badge-Zahl einer Property-Gruppe entspricht der Anzahl der in dieser Gruppe editierbaren Eigenschaften; All zeigt die Gesamtzahl.

## Project, Preview und Optionen

- Die Projektübersicht, Vorschau-Rahmen und Home-Aktion wurden überarbeitet.
- Die Anwendungs-Optionsansicht wurde ergänzt. Einstellungen werden persistent als JSON im Anwendungspfad gespeichert und beim Start geladen.
- Band- und Popup-Demos sowie ihre Listenaktionen wurden angepasst und vereinheitlicht.

## Lokalisierung

- Eine Laufzeit-Grundlage für Deutsch und Englisch wurde ergänzt.
- Sichtbare Texte in den Designeransichten wurden auf Ressourcen umgestellt; die Ressourcenauflösung und die Satelliten-Assembly-Konfiguration wurden korrigiert.
- Die Lokalisierung ist absichtlich als fortsetzbare Grundlage dokumentiert: Weitere sichtbare Texte, Dialoge und Meldungen können schrittweise auf dieselbe Ressourcenbasis migriert werden.

## Performance und Stabilität

- Alignment-Aktionen rendern zuerst und speichern danach, damit Änderungen sofort sichtbar sind.
- Das normale Projektspeichern aktualisiert nicht mehr unnötig die gesamte Projektdateiliste.
- Undo/Redo verwendet wiederhergestellte Snapshot-Payloads erneut und vermeidet bei Folgeschritten unnötiges Serialisieren, Hashen und Komprimieren.
- Die optimierten Pfade wurden mit `dotnet build UserFlow/VIA.UserFlow.csproj --no-restore` geprüft.

## VIA.WPF-Ergänzungen mit Auswirkung auf UserFlow

- Die Icon-Markup-Extensions aller VIA-Packs werden nun im XAML-IntelliSense bei `via:` vorgeschlagen, darunter Material, MaterialDesign, Fluent, Font Awesome, Bootstrap, File, Modern und Phosphor.
- Das Laufzeitverhalten der Icon-Extensions bleibt unverändert; angepasst wurden ausschließlich die Design-Time-Rückgabetyp-Metadaten für den XAML-Editor.
