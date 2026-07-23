# Mockup Designer -- Selection Rendering Refactor (Draw Order Change)

Dieses Dokument beschreibt **nur die Umstellung der Zeichenreihenfolge
selektierter Controls**. Ziel ist es, dass große selektierte Container
(z. B. Border, Panel) **keine darunterliegenden Controls mehr
verdecken**.

------------------------------------------------------------------------

# Ziel der Änderung

Aktuelles Verhalten:

Render-Pipeline (vereinfacht):

1.  Controls normal rendern
2.  SelectedControls erneut rendern (Topmost)
3.  Selection Overlay rendern

Problem:

Wenn ein großes Control selektiert ist:

Panel selected → Buttons darunter verschwinden visuell

Das erschwert präzises Layouting.

------------------------------------------------------------------------

# Zielverhalten nach Umbau

Neue Pipeline:

1.  Controls normal rendern (inkl. selektierter Controls)
2.  danach nur Selection-Overlay rendern

Also **kein zweites Rendern selektierter Controls mehr**.

Damit bleibt sichtbar:

echte Z-Reihenfolge\
darunterliegende Controls\
Überdeckungen\
Layout-Beziehungen

------------------------------------------------------------------------

# Betroffene Datei

BaseDesigner.Renderer.cs

Methode:

RenderSelectedControlsTopmost()

Diese Methode muss angepasst werden.

------------------------------------------------------------------------

# Was entfernt werden muss

Folgende Calls entfernen:

ctrl.Render(canvas, ctrl.VisualRect, ctx);

Diese sorgen für das erneute Topmost-Zeichnen.

------------------------------------------------------------------------

# Was erhalten bleiben soll

Diese Calls bleiben:

ctrl.RenderFrameAndResizeHandles(canvas, ctrl.VisualRect, ctx);

optional zusätzlich:

RenderMultiSelectionFrame(...)

Damit bleiben sichtbar:

Resize Handles\
Selection Frame\
Multi-Selection Frame

------------------------------------------------------------------------

# Empfohlene Zielstruktur der Methode

RenderSelectedControlsTopmost() soll künftig nur noch:

Selection Frames rendern\
Resize Handles rendern\
ActionCircle rendern

aber keine Controls mehr selbst zeichnen.

------------------------------------------------------------------------

# Rendering-Reihenfolge nach Umbau

Neue Reihenfolge:

PASS 1: Band.RenderBackground()

PASS 2: Band.RenderGrid()

PASS 3: Band.RenderControls()

PASS 4: RenderControlSelectionOverlay()

kein zusätzlicher Topmost-Control-Render mehr

------------------------------------------------------------------------

# Optional (empfohlen): schwaches Selection Fill Overlay

Um Selektion besser sichtbar zu machen:

leichte transparente Overlay-Farbe hinzufügen

Beispiel:

Alpha ≈ 15--25

Nur bei Single Selection sinnvoll.

Nicht bei MultiSelection.

------------------------------------------------------------------------

# Vorteile der neuen Lösung

Unterliegende Controls bleiben sichtbar\
Z-Order bleibt korrekt interpretierbar\
Container lassen sich präziser dimensionieren\
Rendering wird einfacher\
kein doppelt gezeichnetes Control mehr

------------------------------------------------------------------------

# Risikoabschätzung

Gering.

Nur lokale Änderung in:

BaseDesigner.Renderer.cs

keine Layoutlogik betroffen\
kein HitTest betroffen\
kein Band-System betroffen

------------------------------------------------------------------------

# Abschlusscheck nach Umbau

Testen:

Single Selection sichtbar? MultiSelection sichtbar? Resize Handles
korrekt? Drag sichtbar? ActionAreas korrekt? Preview Mode unverändert?

Wenn alles passt → Umbau abgeschlossen.
