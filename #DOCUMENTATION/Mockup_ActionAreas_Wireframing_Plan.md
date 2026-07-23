# Mockup Tool – ActionAreas, Popups & Wireframing (GraphX)

## Ziel
Dieses Dokument beschreibt den geplanten Ausbau des Mockup Tools um **ActionAreas**, **Popup-Aktionen** und eine **grafische Wireframing-Ansicht** auf Basis von **GraphX**.  
Es dient als Arbeitsgrundlage für einen neuen Chat und ist bewusst detailliert gehalten.

---

## 1. ActionArea – Grundkonzept

### Definition
Eine **ActionArea (AA)** ist ein spezielles, transparentes Interaction-Element, das über einem beliebigen Control oder Bild liegt.

Eigenschaften:
- Unsichtbar (oder optional leicht hervorgehoben im Edit‑Modus)
- Reagiert auf:
  - Tap / Click
  - DoubleTap
  - LongPress
  - Swipe (Up / Down / Left / Right)
- Löst **genau eine definierte Action** aus

### Motivation
- Trennung von **Visual Design** und **Interaktion**
- Screens können 1:1 aus Figma importiert werden
- Interaktionen werden rein logisch über ActionAreas definiert

---

## 2. Action-Typen

### Navigation Actions
- `NavigateToScreen(ScreenId)`
- `NavigateBack()`
- `NavigateToRoot()`

Optional:
- NavigationStack (History)
- Breadcrumb-Anzeige (Header)

### Popup Actions
- `ShowPopup(PopupId, TargetScreenId)`
- `ClosePopup(PopupId)`
- `TogglePopup(PopupId)`

Wichtig:
- **Popup selbst definiert Position & Layout**
- Screen kennt nur die Referenz (keine Layoutlogik)

### Future (optional)
- SetVariable / Condition
- Trigger Animation
- Call External Action

---

## 3. Datenmodell (geplant)

### ActionArea
```csharp
class ActionArea : DesignControl
{
    ActionType ActionType;
    long? TargetScreenId;
    long? TargetPopupId;
    GestureType Trigger;
}
```

### ActionDefinition
```csharp
class ActionDefinition
{
    ActionType Type;
    long? ScreenId;
    long? PopupId;
}
```

### Screen / Popup
- Screens enthalten **ActionAreas**
- Popups sind eigenständige Assets (ähnlich ScreenTemplate)

---

## 4. Designer‑Integration

### Edit‑Modus
- ActionAreas sichtbar (Rahmen / Overlay)
- Können:
  - verschoben
  - resized
  - kopiert
  - gelöscht werden
- Z‑Order unabhängig von Controls

### Live‑Preview
- ActionAreas unsichtbar
- Events werden aktiv ausgewertet
- Navigation / Popups werden ausgelöst

---

## 5. Navigation Service

Geplant:
```csharp
class NavigationService
{
    Stack<long> ScreenStack;

    void NavigateTo(long screenId);
    void NavigateBack();
}
```

- Integration in ScreenDesigner
- Header kann Back‑Button anzeigen
- Optional: Breadcrumbs

---

## 6. Wireframing / ActionFlow View

### Ziel
Grafische Darstellung aller durch ActionAreas definierten Navigationen:

- Screens = Nodes
- Popups = spezielle Nodes
- ActionAreas = Edges

Beispiel:
```
Screen A --(Tap)--> Screen B
Screen B --(Tap)--> Popup X
Popup X --(Close)--> Screen B
```

---

## 7. GraphX Einsatz

### Warum GraphX?
- Automatische Layouts (orthogonal, hierarchisch)
- Minimiert Linienkreuzungen
- Drag & Drop fähig
- WPF‑native

### Darstellung
- Rechteckige Nodes (Screen‑Preview)
- Kleinere Nodes für Popups
- Orthogonale Verbindungslinien
- Pfeilspitzen für Richtung

### Layout‑Strategie
- Sugiyama / Hierarchical
- Left → Right Flow
- Root Screen links

---

## 8. Interaktives Wireframing (optional)

### Drag & Drop
- Verbindung von Screen A → Screen B per Drag
- Erzeugt automatisch:
  - ActionArea
  - NavigationAction

### Klick auf Edge
- Öffnet Action‑Editor
- Ziel ändern
- Action löschen

---

## 9. Aufwandsschätzung (mit KI‑Support)

| Modul | Dauer |
|-----|------|
| ActionArea Modell & Rendering | 1 Tag |
| Popup‑Action Integration | 0.5 Tage |
| NavigationService + Stack | 0.5 Tage |
| ActionFlow View (GraphX) | 1.5 Tage |
| Drag & Drop Wireframing | 1 Tag |

**Gesamt:** ca. **4–5 Tage**

---

## 10. Fazit

Mit ActionAreas + GraphX entsteht:
- Eine echte **App‑Simulation**
- Extrem schnelles Prototyping
- Saubere Trennung von Design & Logik
- Grundlage für klickbare UX‑Flows

---

## Status
👉 Bereit für Umsetzung in neuem Chat
