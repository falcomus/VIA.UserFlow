# Designer-Viewport-Umbau

Status: geplanter Folgeschritt auf eigenem Branch `codex/designer-umbau`  
Basis: abgeschlossener Stand von `codex/userflow-vw-migration`

## Zielbild

Der Designer belegt den vollständigen verfügbaren Viewport. Die Device Area ist eine mittig angeordnete, zoombare Arbeitsfläche innerhalb dieses Viewports und nicht länger selbst so groß wie der Designer.

Dadurch entstehen drei klar getrennte Ebenen:

1. **Viewport:** sichtbarer, nicht persistierter Arbeitsbereich.
2. **Workspace:** große beziehungsweise virtuell erweiterbare Fläche für Pan, Zoom, Rubberband und geparkte Controls.
3. **Device Area:** mittig platzierte Screen-Fläche mit den bestehenden Geräteabmessungen und Band-Grenzen.

## Nutzen

- bessere räumliche Orientierung bei kleinen und großen Zoomstufen;
- Rubberband-Auswahl kann außerhalb der Device Area beginnen;
- Controls können vorübergehend außerhalb des Geräts geparkt werden;
- mehr Platz für Guidelines, Mehrfachauswahl und künftige Canvas-Werkzeuge;
- Device-Grenzen werden visuell eindeutiger.

## Technischer Ansatz

- Screen-, Template- und Popup-Designer erhalten einen gemeinsamen Viewport-Container.
- Zoom wird auf den Workspace/Device-Transform angewendet, nicht auf die äußere View-Größe.
- Device Area wird bei verfügbarem Platz horizontal und vertikal zentriert.
- Koordinatenkonvertierung wird explizit zwischen Viewport-, Workspace- und Device-Koordinaten geführt.
- Rendering, Hit-Testing, Drag/Drop, Rubberband, Selection Overlays und Guidelines verwenden dieselbe Transformationsquelle.
- Die vorhandenen Device-Koordinaten der Controls bleiben unverändert, damit Projektdateien kompatibel bleiben.
- Geparkte Controls benötigen eine klar definierte Persistenzregel. Bevorzugt werden weiterhin Device-Koordinaten, wobei negative beziehungsweise über die Device-Grenze hinausgehende Positionen erlaubt sind.

## Umsetzung in kleinen Schritten

1. Gemeinsamen Viewport- und Transform-Vertrag festlegen.
2. ScreenView zuerst auf zentrierte Device Area umstellen.
3. Mauskoordinaten, Hit-Testing, Drag/Drop und Zoom prüfen.
4. Rubberband und Auswahl-Overlays auf Workspace-Grenzen erweitern.
5. Parken außerhalb der Device Area ermöglichen und Speichern/Laden prüfen.
6. Guidelines und Snap-Verhalten an Device-Grenzen definieren.
7. TemplateView und PopupView auf denselben Container umstellen.
8. Regressionstest für Live Preview, Snapshots und Undo/Redo.

## Abnahmekriterien

- Device Area ist bei Start und Größenänderung sichtbar zentriert.
- Zoom ändert nicht die Größe des äußeren Viewports.
- Ctrl + Mausrad und Zoom-Slider bleiben synchron.
- Auswahl und Drag/Drop treffen bei jeder Zoomstufe dieselben Controls.
- Rubberband funktioniert innerhalb und außerhalb der Device Area.
- Controls außerhalb der Device Area sind auswählbar, verschiebbar und speicherbar.
- Live Preview rendert weiterhin ausschließlich die Device Area.
- Bestehende Projektdateien laden ohne Migration und speichern im bisherigen Format.
- Undo/Redo und Snapshots enthalten keine Viewport- oder Pan-Zustände.

Der Umbau ist gut machbar, berührt aber zentrale Koordinaten- und Interaktionspfade. Deshalb erfolgt er getrennt von der UI-Kit-Migration und in überprüfbaren Teil-Commits.
