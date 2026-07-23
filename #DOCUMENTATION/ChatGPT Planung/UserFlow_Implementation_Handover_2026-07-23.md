# VIA.UserFlow – Implementierungsübergabe

Stand: 23.07.2026  
Zweck: Arbeitsgrundlage für einen neuen Dokumentations- oder Entwicklungs-Chat.

## Zielbild

VIA.UserFlow wurde von HandyControls auf **VIA.WPF (VW)** als einheitliches UI-Kit migriert. Die Fachlogik und Projektformate bleiben erhalten: Projekte, Screens, Templates, Popups, Designer, Drag & Drop, Guidelines, Toolbox, Eigenschaften, Assets, Action Areas/Flows, Live Preview/Skia-Renderer, Snapshots/Undo/Redo sowie Import/Export.

Gestaltungsprinzip: professionelle, dichte Desktop-Workbench mit klaren Zuständen und den Theme-Tokens von VIA.WPF. Light und Dark Mode sind Teil des Zielbilds.

## Repositories und sichere Basis

| Repository | Stabiler Stand |
|---|---|
| `C:\VIA_DEVELOPMENT\PROJECTS\VIA UserFlow` | `master`, zuletzt `6cfc67a` |
| `C:\VIA_DEVELOPMENT\PROJECTS\VIA.WPF` | `master`, zuletzt `ffdc535` |

Die Arbeitsstände wurden auf die jeweiligen `master`-Branches gepusht. Für neue größere Arbeiten immer einen neuen `codex/...`-Branch anlegen, lokal bauen und erst nach Test/Bestätigung pushen.

## Umgesetzte VIA.WPF-Migration

- UI-Projekte auf `net9.0-windows10.0.19041.0` angehoben und VIA.WPF als lokale Abhängigkeit eingebunden.
- `MainWindow` von HandyControls auf `XWindow` umgestellt, VW-Theme-Grundlage eingebunden.
- HandyControls vollständig aus Produktivcode, Namespaces, Attached Properties, Theme-Dictionaries und Paketen entfernt.
- Benachrichtigungen über eine VIA.WPF-/`XWindow`-Toast-Abstraktion geführt.
- Hauptnavigation, Shell, Projekt-, Screen-, Template- und Popup-Views sowie wiederkehrende Toolbars auf VW-Tokens und VW-Controls umgestellt.
- Projektregel: Wo verfügbar, konsequent `X...`-Controls aus VIA.WPF einsetzen. Fehlende Controls zunächst in VW prüfen/ergänzen, statt normales WPF nur zu stylen.

Wichtige historische Migration-Commits: `1315e93` bis `d85be3e`.

## Shell, Theme und Workbench

- Kompakte Hauptnavigation im dunklen Workbench-Stil.
- Theme-Auswahl im `XWindow`; ein Dark-Mode-Schalter ist sichtbar.
- UserFlow-Brushes werden aus VIA.WPF-Theme-Ressourcen synchronisiert (`UserFlowThemeBridge`), statt Farben pauschal fest zu verdrahten.
- Navigator-, Canvas-, Karten-, Header- und Border-Flächen wurden für Light/Dark nachgezogen.
- Designer- und Projektflächen folgen einem gemeinsamen Workbench-Prinzip: Navigator links, Arbeitsfläche rechts, klarere Flächenhierarchie.
- `Action Flow` ist derzeit absichtlich deaktiviert, nicht `Screen`.
- Listen von Screen/Template/Popup wurden stilistisch an die Toolbox-Liste angeglichen: Gruppen, selektierter Zustand, Aktionen für das jeweils angeklickte Element.

### Noch bei Themes beachten

- Keine zusätzlichen lokalen festen Light-/Dark-Farben einführen, wenn eine passende VW-Resource existiert.
- Border-Tokens waren ein wiederkehrendes Thema. Für sichtbare Panel-Trennungen nur eine bewusst gewählte gemeinsame Brush/Thickness verwenden; starke und subtile Varianten nur gezielt einsetzen.
- `XPanel` ist nicht obsolete. Die irreführende Obsolete-Markierung wurde in VW bereinigt bzw. darf nicht als Begründung dienen, wieder rohe WPF-`Border` einzuführen.

## Projektansicht

- Project View als Übersicht aller Screens neu gestaltet.
- Linke Projektinformation enthält Device, Grid/Guides, Kennzahlen für Screens/Templates/Popups/Controls, Sharing, Pfad mit Tooltip sowie Beschreibung.
- Farbschema-Auswahl als untere Card; Actions liegen in der Card-Kopfzeile.
- Obere Projektaktionen reduziert: Open, Save, neues Projekt sowie More-Menü statt einer breiten blauen Buttonleiste.
- Screen-Karten als Telefon-/Geräterahmen mit Preview, Home-Status, Auswahlzustand und Schatten.
- Hauptbereich nutzt eine eigenständige, dezent getönte Canvas-Fläche, damit Screen-Karten besser stehen.
- Kartenmenü wurde funktional angebunden: Edit Screen, Go to Designer, Delete Screen; es erscheint bei Hover und wurde optisch modernisiert.
- Fixes: Aktionen in Template-/Popup-Listen bearbeiten/löschen das tatsächlich angeklickte Element, nicht mehr nur das vorher selektierte.
- Projektwechsel zeigt einen Lade-/Fortschrittsdialog, ebenso Start mit Projektladen.

## Screen-, Template-, Popup- und Preview-Views

- Einheitliche zweizeilige View-Kopfzeilen: Titel plus Untertitel (z. B. `Projekt-Navigator` bzw. `Designer`).
- Navigationsspalten für Screen, Template und Popup in gleicher Breite vorbereitet und visuell angeglichen.
- Designer-Arbeitsflächen für Screen, Template und Popup abgestimmt.
- Toolbox als schmale rechte Rail mit flyout-artigem großen Panel für Controls/Templates/Properties.
- Toolbox kann geöffnet/pinned werden; Hamburger-/Toggle-Zustand wurde über VIA.WPF-Controls angebunden.
- Toolbox-Previews wurden kompakter und übersichtlicher aufgebaut: Kategorie-Liste, Suche, Card-Previews, bessere Padding- und Textbehandlung.
- Preview-/Designer-Flächen, Top-Panels und Schatten wurden mehrfach vereinheitlicht.

## Dialoge und Editor

### Dialog-Designmuster

TemplateDialog wurde als Muster aufgebaut und danach Popup-, Project- und Screen-Dialog nachgezogen:

- `ModalDialogWindow`/`XWindow`-basierte VW-Dialoge mit konsistentem Titelbereich, Innenabständen, Footer und Primary/Secondary-Aktionen.
- Größerer, links ausgerichteter Titel; keine künstliche Subtitle, wenn sie keinen Informationswert bringt.
- Alle Eingaben mit VIA.WPF-Controls (`XTextBox`, `XComboBox`, `XCheckBox`, `XRadioButton`, `XTabControl`, ...), soweit vorhanden.
- ScreenDialog: Header/Footer/Bands & Pages modernisiert; Grid-Column-Filter-/Sortier-Leichen entfernt.
- ProjectDialog: Device-Presets mit konkreteren Gerätenamen ergänzt (u. a. aktuelle iPhone- und verbreitete Android-Formate).
- `XTextBox` für Multiline-Use-Case verbessert: explizite Höhe wird auch im Designer respektiert.

### ColorPicker und Farbschemata

- `XColorPickerDialog` modernisiert, Light/Dark geprüft, Höhe gestrafft (Ende unter „Recent Colors“).
- Alpha-Kanal ergänzt und die Übertragung in Skia-/Property-Controls repariert; Transparenz von Card-Füllungen bleibt erhalten.
- PropertyEditor-ColorPicker setzt einen Owner, damit der Dialog vor dem passenden Fenster erscheint.
- Color-/`SKColor`-PropertyItems müssen vom `PropertyItemEditorTemplateSelector` als ColorEditor behandelt werden, nicht readonly.
- ColorSchemeDialog/-Editor auf VW-Controls und Light/Dark-Optik umgestellt.
- PropertyEditor weitgehend auf VW-Controls modernisiert; verbleibende alte Controls bitte bei weiterer Arbeit gezielt prüfen.

### Dialog-Overlay – wichtige aktuelle Regression

Der frühere ColorPicker-Fehler zeigte: Ein globales MainWindow-Overlay darf nicht aus dem Dialog selbst aktiviert werden, wenn der Dialog als Owned Window erscheint. Sonst kann ein Dialog modal blockieren und hinter dem Overlay liegen.

Aktueller Arbeitsbranch: `codex/dialog-editing-regression`.

- Dort ist ein **noch zu testender, nicht commiteter Fix** vorhanden: `ModalDialogWindow` aktiviert das Overlay nicht mehr selbst.
- Die aufrufenden Commands um Project/Screen/Template/Popup nutzen bereits `MSG.UI.ShowOverlay()`/`HideOverlay()` um `DialogService.EditEntity(...)`.
- Zusätzlich erhalten im ScreenDialog verschachtelte `XColorPickerDialog`-Instanzen den Owner `this`.
- Build dieses Branches: erfolgreich, 0 Fehler; zwei bekannte Nullability-Warnungen in `UserFlowThemeBridge.cs`.
- Vor Commit testen: New/Edit Project, Screen, Template und Popup; danach OK/Cancel, Theme Light/Dark und verschachtete Farbauswahl.

## Guidelines und Designer-Geometrie

Bestehende Designer-Grundlagen: Drag & Drop, Snapshots, Undo/Redo, Skia-Livepreview, Bands/Pages und Guidelines.

Branch `codex/guideline-screen-snap` (nicht gemergt/pushed) enthält einen separaten, gebauten Entwurf:

- virtuelle Guidelines für linken/rechten Rand sowie horizontale/vertikale Bildschirmmitte;
- Screen-Guidelines gelten auch ohne andere Controls;
- Snap-Entfernung von 6 auf 4 reduziert;
- Guideline-Ziellisten werden während eines Drags gecacht, um wiederholte aufwendige Berechnungen und „Einfrieren“ zu vermeiden;
- Größenänderung auf sichtbare Designer-Grenzen begrenzt.

Noch bewusst offen: Grundsatzentscheidung „Objekte außerhalb des Screens parken und sichtbar machen“ gegenüber konsequenter Begrenzung innerhalb des Screens. Der größere Designer-Viewport-Umbau ist separat geplant, siehe `#DOCUMENTATION\Designer_Viewport_Umbau.md`.

## Offene bzw. nächste sinnvolle Arbeiten

1. Aktuellen Dialog-Regression-Branch testen, committen und erst danach integrieren.
2. Guideline-Branch fachlich testen und entscheiden, ob externe Parkfläche / unbeschränkte Sichtbarkeit umgesetzt wird.
3. Restliche Dialoge und PropertyEditor vollständig auf VW-Controls und beide Modi auditieren.
4. Einheitliche Border-/Surface-Token in VIA.WPF weiter konsolidieren, ohne bestehende Theme-Änderungen zu überschreiben.
5. Mehrsprachigkeit DE/EN als eigenes Vorhaben planen: Ressourcen für UI-Texte und Messages, Sprachwahl optional im XWindow neben der Theme-Combobox.
6. Eigene VW-MessageBox prüfen/entwickeln; Toasts bevorzugen, wenn keine Entscheidung des Nutzers erforderlich ist.
7. Action Flow erst reaktivieren, wenn die dazugehörige View wieder produktionsreif ist.

## Referenzdokumente

- `#DOCUMENTATION\VIA_WPF_Migration_2026-07-20.md`
- `#DOCUMENTATION\Dialog_Modernisierung_Notiz.md`
- `#DOCUMENTATION\Designer_Viewport_Umbau.md`
- `#DOCUMENTATION\ChatGPT Planung\VIA_UserFlow_MASTERPLAN_2026-07-21.md`
- `#DOCUMENTATION\ChatGPT Planung\UI_BRUSHUP_MASTERPLAN.md`
- `#DOCUMENTATION\Images\#VIA UserFlow auf Monitor.png`

## Arbeitskonventionen

- Deutsch, konkret und möglichst knapp kommunizieren.
- Vor größeren UI-Umbauten zunächst Zielbild und offene Entscheidungen abstimmen.
- Bei jedem größeren sicheren Schritt: bauen, testen, lokal committen.
- `master` in UserFlow und VIA.WPF soll als wiederherstellbarer Stand sauber bleiben.
- Kein GitHub-Push ohne ausdrückliche Bestätigung.
