# VIA.UserFlow – Migration auf VIA.WPF

Stand: 20.07.2026  
Migrationsbranch: `codex/userflow-vw-migration`  
Ausgangspunkt: `master` bei `f799016`  
Sicherungstag: `userflow-pre-vw`

## Ziel und Ergebnis

VIA.UserFlow und VIA.Mockup verwenden VIA.WPF als gemeinsames UI-Kit. Die direkte und transitive HandyControls-Abhängigkeit ist entfernt. Host, Hauptnavigation, Dokumentansichten, Projektarbeitsbereich, Dialoge, Toolbox, Eigenschaften, Toolbars, Action Areas, Benachrichtigungen und MessageBox-Aufrufe wurden auf VIA.WPF oder schlanke WPF-Kompatibilitätsstile umgestellt.

Die Migration verändert keine Projekt-, Screen-, Template-, Popup- oder Control-Datenmodelle und keine JSON-Feldnamen. Speichern, Laden, Import und Export behalten daher ihre bestehenden Datenformate. Änderungen in Storage- und Command-Dateien betreffen ausschließlich Benachrichtigungen und Bestätigungsdialoge.

## Technische Grundlage

- UI-Projekte auf `net9.0-windows10.0.19041.0` angehoben.
- VIA.WPF als lokale Projektabhängigkeit in Host und Mockup eingebunden.
- `MainWindow` von HandyControls `Window` auf VIA.WPF `XWindow` umgestellt.
- VIA.WPF Theme-, Controls-, Windowing-, Icons- und MVVM-Projekte werden gemeinsam mit der Solution gebaut.
- VIA.WPF Theme-Service wird beim Start initialisiert.
- UserFlow startet ausschließlich im Light Theme.
- Der Dark-Mode-Umschalter des `XWindow` ist deaktiviert (`ShowThemeModeButton="False"`).
- Die integrierte VIA.WPF Theme-Auswahl bleibt sichtbar (`ShowThemeSelector="True"`).

## Ersetzungen

| Bisher | Jetzt |
|---|---|
| HandyControls Window | VIA.WPF `XWindow` |
| Growl | `XNotifications` über den aktiven `XWindow` |
| HandyControls MessageBox | `XDialogs` auf Basis von VIA.WPF `XMessageBoxService` |
| SearchBar | VIA.WPF `XSearchBox` |
| Card | VIA.WPF `XBorder` |
| UniformSpacingPanel | VIA.WPF `XStackPanel` |
| NumericUpDown | VIA.WPF `XNumberBox` |
| Slider | VIA.WPF `XSlider` |
| ComboBox/Dropdown-Sonderfälle | VIA.WPF `XComboBox` und `XToggleDropDown` |
| ProgressBarFlat im Splash | VIA.WPF `XProgressBar` |
| HandyControls Attached Properties | VIA.WPF-Properties oder reguläre WPF-Layouts |
| HandyControls Basisstile | `ViaWorkbenchControls.xaml` mit Workbench-Tokens |

`ViaWorkbenchControls.xaml` stellt bewusst nur die noch benötigten, anwendungsweiten Kompatibilitätskeys bereit. Dazu gehören Buttons, TextBox, ComboBox, ListBoxItem, DataGrid, StatusBar, ToggleButton, ContextMenu, MenuItem, TabItem und drei abgestufte Schatten. Neue Oberflächen sollen nach Möglichkeit direkt VIA.WPF Controls und Tokens verwenden.

## Migrierte Bereiche

- Host-Shell, Navigation und Statusbereich
- Projektansicht einschließlich Projektaktionen und Farbschema
- Screen-, Template- und Popup-Ansichten
- Projekt-, Screen-, Template-, Popup- und Farbauswahldialoge
- Toolbox und gruppierte Listen
- Property Editor einschließlich Live-Updates von Zahlenwerten
- Alignment-, Undo/Redo- und Zoom-Toolbars
- Live Preview und Color Scheme View
- Action-Area-Editor, -Hinweis und -Items
- Image Picker, Splash und About
- produktive Notifications, Warnungen und Bestätigungsdialoge

Die Property-Editor-Migration erhält die bisherige Live-Snapshot-Semantik: Da `XNumberBox` kein passendes `ValueChanged`-Event bereitstellt, wird die Value-DependencyProperty während Loaded/Unloaded beobachtet.

## Entfernte Abhängigkeiten

- `HandyControl` PackageReference aus `VIA.UserFlow.csproj` entfernt.
- `HandyControl` PackageReference aus `VIA.Mockup.csproj` entfernt.
- HandyControls Theme- und Design-Time-Dictionaries entfernt.
- `xmlns:hc`, HandyControls-Typen, Attached Properties und produktive Aufrufe entfernt.
- Paketgraph und `project.assets.json` enthalten kein HandyControls-Paket mehr.

## Verifikation

Am 20.07.2026 wurden folgende Prüfungen erfolgreich ausgeführt:

- Quellcode-Audit außerhalb von `bin`, `obj` und historischer Dokumentation: keine Treffer für `HandyControl` oder `hc:`.
- Paketgraph von VIA.UserFlow und VIA.Mockup einschließlich transitiver Pakete: kein HandyControls-Paket.
- Debug-Build der gesamten Solution: 0 Warnungen, 0 Fehler.
- Release-Build der gesamten Solution: 0 Warnungen, 0 Fehler.
- `dotnet test VIA.UserFlow.sln`: erfolgreich; aktuell sind keine automatisierten Testprojekte registriert.
- Runtime-Smoke-Test: Splash und MainWindow vollständig geladen; Prozess nach 10 Sekunden stabil aktiv.

Der Runtime-Smoke-Test fand und beseitigte Ressourcenfehler, die der XAML-Compiler nicht erkennen konnte: `ProgressBarFlat`, `ButtonDanger.Small`, `EffectShadow1/2/3`, `Boolean2VisibilityConverter`, `String2VisibilityConverter` und `BaseStyle` wurden durch lokale/VIA.WPF-Äquivalente ersetzt.

## Empfohlene manuelle Abnahme

Vor einem Release sollte einmal interaktiv geprüft werden:

1. Projekt neu/anlegen/öffnen/speichern/speichern unter sowie Import/Export.
2. Screens, Templates und Popups anlegen, ändern, duplizieren und löschen.
3. Toolbox-Drag/Drop, Mehrfachauswahl, Rubberband, Guidelines und Ausrichtung.
4. Property Editor, Assets, Action Areas und Flows.
5. Live Preview, Skia-Rendering und Popup-Navigation.
6. Snapshots sowie Undo/Redo über mehrere Operationen.
7. Einstellungen, Hilfe und About.
8. VIA.WPF Theme-Auswahl bei weiterhin festem Light Mode.

## Lokale Commit-Reihe

- `1315e93` – VIA.WPF migration foundation
- `5f16ccd` – Notifications über VIA.WPF
- `54d7e64` – Hauptnavigation und Tokens
- `7e66ab2` – Host-Shell
- `3a11402` – Dokumentansichten
- `81cb3c2` – Projektarbeitsbereich
- `05ad0e3` – Editordialoge
- `6f224ef` – Workbench-Tools
- `4d894fb` – restliche HandyControls-Interaktionen
- `d85be3` – Paket und Dictionaries entfernt
- `c91d5d6` – veraltete XAML-Namespaces entfernt
- `893f3ca` – Runtime-Ressourcen nach Paketentfernung korrigiert

Es wurde nichts zu GitHub gepusht.
