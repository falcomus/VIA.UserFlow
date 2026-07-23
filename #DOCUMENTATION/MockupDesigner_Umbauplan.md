# MOCKUP DESIGNER – SOLIDER UMBAUPLAN (START MORGEN)

Version: **v1.0 – Stabilisierung vor Erweiterung**  
Ziel: **funktionierende Architektur ohne Zustandsduplikation**, schrittweise und überprüfbar

---

## 0. VERBINDLICHE GRUNDREGELN

1. Single Source of Truth (Screen.Bands etc.)
2. Designer sind zustandslos (Render + Interaktion)
3. Persistenz nur im ViewModel
4. Kein Big‑Bang‑Umbau

---

## 1. IST-PROBLEME

- Zustandsduplikation (BaseDesigner.Bands)
- Shadowed DependencyProperties
- Nicht‑persistente Band-Reihenfolge
- Inkonsistente Height-Bindings

---

## 2. ZIELARCHITEKTUR

MockupViewModel  
 └── Screen / Template / Popup (Bands = Wahrheit)  
      └── Designer (Render-only)

---

## 3. PHASE 1 – BASEDESIGNER ENTRÜMPELN

Datei: Mockup.Designer/BaseDesigner.cs

- Entfernen: Bands, Screen-Properties, Persistenz
- Behalten: TemplateParts, Flags, Rendering, Mouse
- Neu: abstrakte GetBands()-Methoden

---

## 4. PHASE 2 – SCREENDESIGNER

Datei: Mockup.Designer/ScreenDesigner.cs

- Eigene Screen DependencyProperty
- Kein Shadowing
- GetBands() liefert direkt Screen.Bands

---

## 5. PHASE 3 – RENDERER

Datei: BaseDesigner.Renderer.cs

- Ausschließlich GetBands()/GetHeaderBand() verwenden
- Keine Model-Kenntnis

---

## 6. PHASE 4 – PERSISTENZ

Datei: MockupViewModel.Commands.cs

- MoveBand(Band, delta) hier implementieren
- ObservableCollection.Move()

---

## 7. PHASE 5 – DESIGNER → REQUEST

- Designer triggert Action oder Messenger
- Keine Collection-Manipulation im Designer

---

## 8. PHASE 6 – DESIGNER CONTROLS

Nur Binding, keine Logik:

Screen="{Binding CurrentScreen}"  
Height="{Binding DesignerHeight}"

---

## 9. IMPLEMENTIERUNGSREIHENFOLGE

1. BaseDesigner
2. ScreenDesigner
3. Renderer
4. ViewModel Persistenz
5. Designer Requests

---

## 10. NO-GOS

- Keine Kopien
- Keine statischen Services
- Kein paralleler Umbau

---

## 11. STARTSATZ FÜR NÄCHSTEN CHAT

„Hier ist meine aktuelle BaseDesigner.cs – bitte Phase 1 umsetzen.“
