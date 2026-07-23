# Plan: Popups in Live Preview (Interaktiv, inkl. ActionAreas) – Neuer Chat

Ziel: In der LivePreview (Device-Frame) sollen ScreenPopups (SideMenu/Center/etc.) **über** dem aktuellen Screen angezeigt werden können. Popups enthalten normale Controls und ActionAreas. ActionAreas im Popup müssen in LivePreview vollständig interaktiv sein (Navigate/OpenFile/OpenURL/ShowPopup). Bei Navigation aus einem Popup heraus soll sich das Popup sofort schließen (mindestens das oberste, optional alle).

Dieser Plan ist so formuliert, dass ein Kollege im neuen Chat sofort weiß, welche Dateien benötigt werden und in welcher Reihenfolge gearbeitet wird.

---

## 0) Begrifflichkeiten / Current State (gegeben)

- LivePreview läuft über:
  - `Mockup.Views.LiveView` → bindet `PreviewScreen`, `NavigationTrail`
  - `Mockup.Rendering.LiveViewControl` → setzt `ScreenDesigner.LiveMode = true`, hört auf `ActionAreaTriggerMessage`, führt Navigation/OpenFile aus
  - `Mockup.ViewModel.MockupViewModel` → besitzt `PreviewScreen`, `NavigationTrail`, `PreviewNavigateTo/Home/Back`
- Popups existieren als Domain-Objekte:
  - `Mockup.ScreenPopup` enthält `Bands` (genau 1 Custom Band, 1 Page, Controls in Page)
  - Es existiert ein `PopupDesigner` und `PopupDesignerControl` (Designzeit), d. h. Renderer/HitTest/Drag ist bereits für Popups „grundsätzlich“ vorhanden.
- Designer/Renderer-Grundlage:
  - `Mockup.Designer.BaseDesigner.Renderer.cs` zeichnet Screen-Bands/Controls; Sticky optional.
  - `Mockup.Designer.BaseDesigner.MouseHandler.cs` macht HitTest & Live Mouse Handling.
  - `Mockup.Designer.BaseDesigner.Bands.cs` enthält `UpdateActivePageWorldBounds(Band band)`.

---

## 1) Architektur-Entscheidung (Preview-Runtime)

### 1.1 PreviewPopupStack im ViewModel (Source-of-truth)
- Popups dürfen in der LivePreview nicht „im Screen“ persistiert werden.
- Stattdessen: ViewModel hält einen **PreviewPopupStack**, analog zu `NavigationTrail`.

Vorschlag:
- `ObservableCollection<PreviewPopupInstance> PreviewPopupStack`
- `PreviewPopupInstance` enthält:
  - `ScreenPopup Popup` (Referenz auf Projekt-Popup)
  - `ScreenPopupPosition Position`
  - `bool UseMousePos` (oder implizit bei Position==MousePos)
  - `float AnchorX`, `float AnchorY` (in Preview-World/Device-Koordinaten)
  - optional: `bool CloseOnOutsideClick` (Default true)
  - optional: `bool DimBackground`

API am VM:
- `PreviewShowPopup(ScreenPopup popup, ScreenPopupPosition pos, bool useMousePos, float x, float y)`
- `PreviewCloseTopPopup()`
- `PreviewCloseAllPopups()`

Regel:
- Bei `Navigate/NavigateHome/NavigateBack/OpenFile/OpenUrl` aus einem Popup:
  - `PreviewCloseTopPopup()` (oder „all“, falls UX so gewünscht) und danach Preview invalidieren.

### 1.2 Rendering & HitTesting müssen dieselbe Popup-Layoutfunktion nutzen
- Es muss eine zentrale Methode geben: **ComputePopupRect(...)**,
  - damit Render und HitTest identische Bounds verwenden.
- Diese Methode lebt sinnvoll in `BaseDesigner` (Renderer partial), weil dort Device-Bounds und DPI/World existieren.

---

## 2) Dateien, die der Kollege für den neuen Chat benötigt

Bitte diese Dateien im neuen Chat posten (vollständig, aktueller Stand):

### 2.1 View + Preview Host
1. `Mockup.Views/LiveView.xaml`
2. `Mockup.Views/LiveView.xaml.cs`
3. `Mockup.Rendering/LiveViewControl.xaml`
4. `Mockup.Rendering/LiveViewControl.xaml.cs`

### 2.2 ViewModel (Preview Navigation + neue Popup-Stack-API)
5. `Mockup.ViewModel/MockupViewModel.Collections.cs` (oder die Datei, wo `NavigationTrail`/`PreviewScreen` liegt)
6. Falls Preview-Navigation in einer anderen Partial-Datei liegt: diese ebenfalls.

### 2.3 Messaging / ActionArea
7. `Mockup.Messages/DesignerMessages.cs` (enthält `ActionAreaTriggerMessage`, `MSG.Navigation.*`)
8. `Mockup.Actions/ActionDefinition.cs`
9. `Mockup.Actions/ActionArea.cs`

### 2.4 Designer (Renderer + Mouse in Preview)
10. `Mockup.Designer/BaseDesigner.cs`
11. `Mockup.Designer/BaseDesigner.Bands.cs`
12. `Mockup.Designer/BaseDesigner.Renderer.cs`
13. `Mockup.Designer/BaseDesigner.MouseHandler.cs`
14. `Mockup.Designer/ScreenDesigner.cs`

### 2.5 Domain Popup + Project lookup
15. `Mockup/ScreenPopup.cs`
16. `Mockup/Project.cs` (Popup-Collection + `GetPopupById`)
17. `Mockup/Screen.cs` (nur falls notwendig für DesignerWorldBounds/Preview)

Optional (falls Popups schon irgendwo „previewmäßig“ referenziert werden):
- `Mockup.Designer/PopupDesigner.cs`, `PopupDesignerControl.cs`

---

## 3) Implementierungs-Schritte (Reihenfolge)

### Step A: ViewModel – PreviewPopupStack + Commands
1. Neue Klasse/Record `PreviewPopupInstance`.
2. `PreviewPopupStack` als ObservableCollection im `MockupViewModel`.
3. Neue Methoden:
   - `PreviewShowPopup(...)` (Push + ggf. dedupe; setze Position/Anchor)
   - `PreviewCloseTopPopup()` (Pop)
   - `PreviewCloseAllPopups()`
4. Ereignisse:
   - Bei `PreviewNavigateTo/Home/Back`: Optional `PreviewCloseAllPopups()` (entscheidbar).
5. UI-Invaliation:
   - Nach Änderungen: `MSG.UI.InvalidatePreview()` (und ggf. Designer, wenn notwendig).

Akzeptanz:
- Der Stack verändert sich in der UI (Debugger/Binding), ohne Crash.
- `PreviewScreen` bleibt unverändert funktionsfähig.

---

### Step B: ActionAreaTriggerMessage um Klick-Position erweitern
Problem: `ShowPopup(MousePos)` braucht die Click-Position im Preview-World.
Derzeit: `ActionAreaTriggerMessage` enthält nur `(Area, Trigger)`.

Änderung:
- `ActionAreaTriggerMessage` erweitert auf:
  - `(ActionArea Area, ActionTrigger Trigger, float X, float Y)` **oder**
  - `(ActionArea Area, ActionTrigger Trigger, SKPoint WorldPoint)` (wenn Skia in Messages ok ist)

Änderungen:
- In `ActionArea.OnPointerUp(...)`: `MSG.AA.Trigger(this, trigger, ctx.WorldPoint)`
- In `MSG.AA.Trigger(...)`: neue Overload mit Point.

Akzeptanz:
- LiveViewControl bekommt die Koordinate, ohne zusätzliche globale Maus-Abfrage.

---

### Step C: LiveViewControl – Action-Ausführung + ShowPopup
In `LiveViewControl.OnActionAreaClicked`:
- Ergänze `ActionType.ShowPopup`:
  1) `var popup = vm.CurrentProject?.GetPopupById(action.PopupId ?? 0)`
  2) `var pos = action.PopupPosition ?? popup.Position` (Fallback)
  3) `var useMouse = action.UseMousePos == true || pos == MousePos`
  4) `vm.PreviewShowPopup(popup, pos, useMouse, clickX, clickY)`
  5) `MSG.UI.InvalidatePreview()`

Zusatzregel (Close on Navigate/Open*):
- Bei `Navigate`, `NavigateBack`, `NavigateHome`, `OpenFile`, `OpenURL`:
  - falls ein Popup offen ist: `vm.PreviewCloseTopPopup()`
  - dann Navigation/Open ausführen.

Akzeptanz:
- Klick auf ActionArea mit ShowPopup öffnet Popup im Preview.
- Klick auf Navigate im Popup navigiert und Popup verschwindet sofort.

---

### Step D: BaseDesigner.Renderer – Popup Overlay rendern
Nur in Preview (`LiveMode && DesignerKind == Screen`):
- Nach Screen-Render (nach Sticky-Header) Popups zeichnen.
- Implementiere:
  - `ComputePopupRect(ScreenPopup popup, PreviewPopupInstance inst, SKRect deviceBounds)`:
    - Left: x=0
    - Right: x=deviceW - popupW
    - Top: y=0
    - Bottom: y=deviceH - popupH
    - Center: x=(deviceW-popupW)/2; y=(deviceH-popupH)/2
    - MousePos: x=inst.AnchorX; y=inst.AnchorY (clamp)
  - `RenderPreviewPopups(SKCanvas canvas, RenderContext ctx)`:
    - optional: dim overlay (einmal) wenn Stack nicht leer
    - foreach popup in Stack (bottom→top):
      - band = popup.RootBand
      - band.Width/Height = popup.Width/Height (nicht persistieren, nur render-time, falls nötig)
      - band.UpdateBandWorldBounds(rect.Left, rect.Top)
      - UpdateActivePageWorldBounds(band)
      - band.RenderBackground(canvas, ctx)
      - band.RenderControls(canvas, ctx)

Akzeptanz:
- Popup wird sichtbar im Preview (über Screen).
- Position stimmt (Left/Right/Center/MousePos).

---

### Step E: BaseDesigner.MouseHandler – Popup HitTest in LivePreview priorisieren
In `LiveMouseDown/Move/Up`:
- Wenn `vm.PreviewPopupStack.Count > 0`:
  1) Topmost Popup zuerst prüfen (reverse stack)
  2) Per `ComputePopupRect(...)` bounds bestimmen
  3) Wenn Click innerhalb Popup:
     - HitTest ActionControls innerhalb popup.Page.Controls (ZIndex desc)
     - setze `_previewActiveActionControl` und forwarde OnPointerDown/Up
     - return true
  4) Wenn Click außerhalb Popup:
     - falls `CloseOnOutsideClick`: `vm.PreviewCloseTopPopup()` + invalidate
     - return true (damit Klick nicht Screen drunter trifft)

Wichtig:
- Render und HitTest müssen dieselbe Rect-Berechnung nutzen.

Akzeptanz:
- Popup fängt Klicks ab.
- Screen darunter ist nicht klickbar solange Popup offen ist (typisch).
- ActionAreas im Popup reagieren wie gewohnt.

---

## 4) ZOrder / Overlay / Selection (Hinweis)
- In LivePreview sind Selections/ResizeHandles normalerweise aus.
- Für Popups gilt: sie werden nach allem anderen gerendert → topmost.

---

## 5) Testfälle (Definition of Done)

1) ShowPopup(Center): Tap → Popup erscheint mittig.  
2) ShowPopup(Left/Right/Top/Bottom): Docking korrekt.  
3) ShowPopup(MousePos): nahe Klickpunkt, clamp in Device.  
4) Popup interaktiv: Navigate/OpenFile im Popup funktioniert + Popup schließt.  
5) Outside click: Klick neben Popup schließt Popup (wenn aktiviert).  
6) Stack: Popup aus Popup öffnet weiteren; CloseTop/Back/Home konsistent.

---

## 6) Was NICHT anfassen
- Persistenz/JSON der Screens/Popups: PreviewPopupStack ist rein runtime.
- PopupDesigner (Designzeit) bleibt unverändert.
