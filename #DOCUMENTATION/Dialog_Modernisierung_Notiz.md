# Dialog-Modernisierung Notiz

Status: Dialog-Grundmodernisierung umgesetzt; weiterer View-Audit offen.

## Anlass

Der Designer-Viewport-Umbau wird spaeter separat fortgesetzt. Vorher wurden die fachlich wichtigen Dialoge auf den neuen VW-Workbench-Look und VIA.WPF Controls umgestellt.

## Projektregel ab 2026-07-20

- Ueberall, wo ein passendes VIA.WPF-Control vorhanden ist, wird das `via:X...` Control verwendet.
- Kein "normales WPF-Control plus etwas Styling" fuer produktive Oberflaechen, wenn ein X-Pendant existiert.
- Vor jeder neuen WPF-Standard-Control-Verwendung pruefen:
  1. Gibt es das Control bereits in VIA.WPF?
  2. Falls ja: X-Control verwenden.
  3. Falls nein: bewusst dokumentieren, ob ein neues VIA.WPF-Control erstellt werden soll.
- Ausnahmen sind technische WPF-Strukturen wie `Grid`, `DataTemplate`, `ControlTemplate`, `ItemsPanelTemplate`, `Window`, `UserControl`, `ResourceDictionary` und Template-Bausteine innerhalb von Styles.

## ScreenDialog

- Band- und Page-Handling neu strukturiert.
- DataGrid-basierte technische Eingabemaske entfernt.
- Bands/Pages verwenden `via:XListBox`.
- Tabs verwenden `via:XTabControl` und `via:XTabItem`.
- Eingaben, Buttons, Checkboxen, Labels und Rahmen verwenden VIA.WPF X-Controls.
- ResourceDictionaries lokal beziehungsweise eindeutig geladen, damit StaticResource-Aufloesung unabhaengig vom Aufrufkontext stabil bleibt.

## Weitere Dialoge pruefen

- ProjectDialog: auf X-Controls umgestellt, inklusive `via:XListBox` fuer ScreenSize-Auswahl.
- TemplateDialog: auf X-Controls umgestellt, inklusive `via:XComboBox`.
- PopupDialog: auf X-Controls umgestellt, inklusive `via:XComboBox`.
- XColorPickerDialog: Dialogbuttons auf `via:XButton`.
- Action-Area-Dialoge: Editor, Hint und ItemControl auf X-Controls umgestellt.
- ImageRefDialog: auf X-Controls umgestellt, inklusive `via:XListBox`.
- ColorSchemaEditor: auf X-Controls umgestellt, inklusive `via:XListBox`; Glossy-Chips entfernt.

## Noch offener grosser Audit

Die Dialoge sind bereinigt. In produktiven Views und eigenen UIControls gibt es noch Standard-WPF-Control-Verwendungen, die separat und schrittweise migriert werden muessen, unter anderem:

- `UserFlow/MainWindow.xaml`: Navigation/Status/Overlay-Reste pruefen, insbesondere alte auskommentierte XAML-Bloecke entfernen.
- `Mockup/Views/*`: Projekt-, Screen-, Template-, Popup-, Live- und Toolbox-Views systematisch auf X-Controls pruefen.
- `Mockup/UIControls/*`: eigene Controls wie ColorPicker, ImagePicker, UndoRedoBar, AlignmentToolbar pruefen; wo moeglich X-Controls verwenden.
- `Mockup/Styles/*`: nicht blind ersetzen; ControlTemplates duerfen WPF-Primitives enthalten, sollten aber langfristig gegen VIA.WPF Theme-/Token-Standards geprueft werden.
