# Analyse: VIA UserFlow — Mockup Tool

**Datum:** 2026-05-30
**Analysiert von:** Claude Sonnet 4.6

---

**Gesamtnote: 2- (Gut mit merklichen Schwächen)**

---

## Architektur & Design — Note: 2

Das Fundament ist solide und durchdacht:

- **Saubere Schichtentrennung**: Mockup-Library / Snapshot-Library / Host-App (`UserFlow`) sind klar separiert.
- **MVVM mit CommunityToolkit.Mvvm**: Source Generators korrekt eingesetzt (`[ObservableProperty]`, `[ObservableRecipient]`).
- **SkiaSharp-Rendering**: Performance-bewusste Entscheidung für Skia statt WPF-Canvas — richtig für ein Design-Tool.
- **Band-System**: Hierarchie `Project → Screen → Band → BandPage → DesignControl` ist logisch und erweiterbar.
- **ControlRegistry** mit Reflection-basierter Discovery und Plugin-Support (`RegisterAssembly`) — professionell.
- **Partial Classes** für große Klassen (MockupViewModel in 10 Dateien, BaseDesigner in 6 Dateien) — wartbar.
- **Snapshot-System** als eigene Library mit `SnapshotStack` pro Kontext — gutes SoC-Prinzip.

---

## Kritische Fehler — Note: 4+ (Muss behoben werden)

### 1. `OnWidthChanging`/`OnHeightChanging` — Funktionaler Bug

```csharp
// DesignControl.cs
partial void OnWidthChanging(float value)
{
    value = Math.Clamp(value, MinWidth, MaxWidth);  // ❌ kein Effekt!
}
```

`value` ist kein `ref`-Parameter. In CommunityToolkit.Mvvm Source Generators hat die Zuweisung **null Effekt** — das Clamp wird schlicht ignoriert. Min/Max-Grenzen werden nie eingehalten.

### 2. Toter Code — Auskommentierter `OnMouseDown`

In `BaseDesigner.MouseHandler.cs` ist der gesamte `OnMouseDown`-Handler (~120 Zeilen) auskommentiert — inklusive Resize-Handle-Logik und ActionArea-Doppelklick. Unklar, ob der aktive Code woanders liegt oder ob Funktionalität fehlt.

### 3. Doppelte `VM?.CurrentControl`-Zuweisung

```csharp
// MouseHandler.cs — SelectControl()
VM?.CurrentControl = ctrl;
if (DataContext is MockupViewModel vm)  // ❌ redundant
    VM?.CurrentControl = ctrl;          // ❌ identische Zuweisung

// DeselectAllControls()
VM?.CurrentControl = null;
if (DataContext is MockupViewModel vm)  // ❌ redundant
    VM?.CurrentControl = null;          // ❌ identische Zuweisung
```

### 4. Inkonsistente Null-Checks — Potentieller NullReferenceException

```csharp
if (!VM.SelectedControls.Contains(ctrl))   // ❌ kein ?. hier
    VM?.SelectedControls.Add(ctrl);         // ✅ hier schon
```

### 5. `PreviewAnimTimer_Tick` — Veralteter Kommentar/Bug

```csharp
// 600ms toggeln
bool phase = ((elapsed / 10) % 2) == 1;  // 10ms — nicht 600ms!
```

Der auskommentierte Vorgänger hatte `/600`. Entweder ist der Kommentar falsch oder der Timer tickt mit 10ms statt 600ms (also 100x schneller als gewollt).

### 6. `DeselectAllControls` — Doppelte Arbeit

Die Methode iteriert **alle Bands/Pages/Controls** um `IsSelected = false` zu setzen, obwohl `VM.SelectedControls` genau diese Menge bereits enthält. Das ist O(n_gesamt) statt O(n_selektiert).

---

## Sollte behoben werden — Note: 3

### 7. Zwei `ColorToBrushConverter.cs`

Existiert in `ColorSystem/ColorToBrushConverter.cs` **und** `Converter/ColorToBrushConverter.cs` — Duplikat.

### 8. Zwei `ColorExtensions.cs`

Existiert in `ColorSystem/ColorExtensions.cs` **und** `Extensions/ColorExtensions.cs` — Duplikat.

### 9. 30+ winzige Converter-Klassen

`BoolToVisibilityConverter`, `InversBoolToVisibilityConverter`, `NullToVisibilityConverter`, `NullToInversVisibilityConverter` etc. — WPF 6+ bietet viele davon Built-in. Viele könnten als `MarkupExtension` zusammengefasst werden.

### 10. `SnapshotManager` — API-Proliferation

Für dieselbe Funktion existieren Duplikat-Aliases: `UndoCount()` und `GetUndoCount()`, `TotalBytes()` und `GetTotalBytes()` und `TotalUtf8Bytes()` und `TotalKilobytes()` und `GetTotalKilobytes()` — 7 Methoden für 2 Konzepte. Das ist Ballast.

### 11. `ControlRegistry.RegisterType` instanziiert Controls als Seiteneffekt

Um DefaultWidth/Height zu lesen, wird im Registry-Init ein echtes Control erstellt (`Activator.CreateInstance`). Hat das Control Konstruktor-Seiteneffekte (Messaging, globaler State), kann das beim Start crashen oder falsche Zustände erzeugen.

### 12. Statische `SKPaint`-Objekte in `Renderer.cs`

```csharp
private static readonly SKPaint _multiFrameBorderPaint = new() { ... };
```

Statische SKPaint-Instanzen mit `SKPathEffect` können bei App-Unloading Memory Leaks verursachen (nativer SkiaSharp-Speicher wird nicht freigegeben).

### 13. `MouseWorldPoint` ist sinnlos

```csharp
public SKPoint MouseWorldPoint => MouseViewPoint;  // identisch!
```

Wenn View- und World-Koordinaten identisch sind (kein Zoom-Transform?), ist diese Abstraktion leer. Wenn Zoom korrekt wäre, müsste hier ein Transform stattfinden.

### 14. `PropertyGridTemp` — "Temp" im Produktionscode

Der Ordner `UIControls/PropertyGridTemp/` signalisiert unfertige Implementierung. Entweder fertigstellen und umbenennen oder dokumentieren warum.

---

## Stilfragen — Note: 2-3

### 15. `Band.DEFAULT_NAME` — falsche Naming-Convention

```csharp
public static string DEFAULT_NAME = "CustomBand";  // ❌ nicht const, ALL_CAPS
```

In C# sollte das `public const string DefaultName = "CustomBand"` sein.

### 16. `CUSTOM` in `ControlVariant`-Enum

Alle anderen Werte sind PascalCase (`Primary`, `Accent`) — `CUSTOM` bricht die Konvention.

### 17. Übermäßig ausführliche Region-Kommentare

Jede Datei beginnt mit einem 80-Zeichen-Trennstrich-Kommentar-Block der das wiederholt, was der Dateiname bereits sagt. Das ist Rauschen.

### 18. Keine sichtbaren Tests

Kein Test-Projekt im Solution-Tree. Das Snapshot-System, ControlRegistry und Domain-Modell wären gut unit-testbar.

---

## Zusammenfassung

| Bereich | Note |
|---|---|
| Gesamtarchitektur | **2** |
| Domain-Modell | **2** |
| Snapshot/Undo-System | **2+** |
| ControlRegistry | **2** |
| Renderer/Skia | **2-** |
| MouseHandler | **4** (toter Code, Bugs) |
| Converter-Sammelsurium | **4** |
| Code-Hygiene (Duplikate, Kommentare) | **3** |
| Tests | **6** (nicht vorhanden) |
| **Gesamt** | **2-** |

---

## Das Wichtigste sofort zu beheben

1. **`OnWidthChanging` Bug** — Controls respektieren MinWidth/MaxWidth nicht
2. **Auskommentierten `OnMouseDown`** aufräumen — ist das aktiv oder tot?
3. **`PreviewAnimTimer`** — 10ms vs 600ms klären
4. **Doppelte `VM?.CurrentControl`-Zuweisungen** entfernen
