# Mockup.Snapshots — Undo/Redo Library für UserFlow

## Übersicht

`Mockup.Snapshots` ist eine eigenständige Library, die das vollständige Undo/Redo-System
für UserFlow bereitstellt. Sie kennt **keine** Mockup-Typen direkt — die Anbindung
erfolgt über das Interface `ISnapshotSerializer`, das in der Mockup-Library implementiert wird.

---

## Dateistruktur

```
Mockup.Snapshots/
├── Mockup.Snapshots.csproj   ← Library-Projektdatei
├── SnapshotEntry.cs          ← Datenklasse: ein Snapshot-Eintrag
├── SnapshotStack.cs          ← Kern-Engine: Undo/Redo-Stacks
├── SnapshotManager.cs        ← Zentrale Fassade (statisch)
├── SnapshotResult.cs         ← Rückgabewert von Undo/Redo
├── SnapshotLabels.cs         ← Label-Konstanten für alle Aktionen
└── ISnapshotSerializer.cs    ← Interface für die Mockup-Library

Integration/  (diese Dateien kommen in die Mockup-Library)
├── MockupSnapshotSerializer.cs   → Mockup/Services/
├── MockupViewModel.Snapshots.cs  → Mockup/ViewModel/
└── UserFlow.sln                  → aktualisierte Solution-Datei
```

---

## Einbindung — Schritt für Schritt

### 1. Library zur Solution hinzufügen

Den Ordner `Mockup.Snapshots/` neben `Mockup/` und `UserFlow/` ablegen.
Die aktualisierte `UserFlow.sln` enthält bereits den neuen Projekt-Eintrag.

### 2. Projekt-Referenz in Mockup.csproj eintragen

```xml
<ItemGroup>
    <ProjectReference Include="..\Mockup.Snapshots\Mockup.Snapshots.csproj" />
</ItemGroup>
```

### 3. Integration-Dateien in die Mockup-Library kopieren

```
Integration/MockupSnapshotSerializer.cs  →  Mockup/Services/
Integration/MockupViewModel.Snapshots.cs →  Mockup/ViewModel/
```

### 4. InitializeSnapshots() in LoadAll() aufrufen

In `MockupViewModel.Storage.cs` → `LoadAll()` am Ende:

```csharp
public void LoadAll()
{
    // ... bestehender Code ...
    InitializeSnapshots();   // ← NEU
}
```

### 5. Stack bei Screen-Wechsel leeren

In `MockupViewModel.Collections.cs` → `OnCurrentScreenChanged()`:

```csharp
partial void OnCurrentScreenChanged(Screen? value)
{
    SnapshotManager.Clear(SnapshotContext.Screen);   // ← NEU
    // ... bestehender Code ...
}
```

Analog für Template und Popup.

### 6. Keyboard-Shortcuts in BaseDesigner einbauen

In `BaseDesigner.cs` → `OnPreviewKeyDown()`:

```csharp
protected virtual void OnPreviewKeyDown(object sender, KeyEventArgs e)
{
    bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

    if (ctrl && e.Key == Key.Z)
    {
        VM?.UndoCommand.Execute(null);
        e.Handled = true;
        return;
    }

    if (ctrl && e.Key == Key.Y)
    {
        VM?.RedoCommand.Execute(null);
        e.Handled = true;
        return;
    }
}
```

### 7. Tab-Index in MockupViewModel.Snapshots.cs anpassen

In `GetActiveSnapshotContext()` die Tab-Indizes auf deine Reihenfolge anpassen:

```csharp
return MainTabSelectedIndex switch
{
    0 when CurrentScreen   != null => SnapshotContext.Screen,
    1 when CurrentTemplate != null => SnapshotContext.Template,
    2 when CurrentPopup    != null => SnapshotContext.Popup,
    _                              => null,
};
```

---

## Verwendung im Designer (Push vor Mutationen)

```csharp
// In ScreenDesigner.DragDrop.cs — vor dem Hinzufügen eines Controls:
MockupService.Mockup.PushSnapshot(SnapshotContext.Screen, SnapshotLabels.ControlDropped);

// In BaseDesigner.MouseHandler.cs — nach dem Loslassen beim Verschieben:
MockupService.Mockup.PushSnapshot(SnapshotContext.Screen, SnapshotLabels.ControlMoved);

// In MockupViewModel.ContextMenuCommands.cs — vor dem Löschen:
MockupService.Mockup.PushSnapshot(SnapshotContext.Screen, SnapshotLabels.ControlDeleted);

// Im PropertyGrid — bei Eigenschaftsänderung:
MockupService.Mockup.PushSnapshot(SnapshotContext.Screen, SnapshotLabels.ControlPropChanged);
```

---

## Alle verfügbaren Labels (SnapshotLabels.cs)

| Konstante                    | Anzeige                    |
|------------------------------|----------------------------|
| `ControlDropped`             | Control hinzugefügt        |
| `ControlMoved`               | Control verschoben         |
| `ControlResized`             | Control skaliert           |
| `ControlDeleted`             | Control gelöscht           |
| `ControlPropChanged`         | Eigenschaft geändert       |
| `ControlPasted`              | Eingefügt                  |
| `ControlDuplicated`          | Dupliziert                 |
| `ControlZOrderChanged`       | Z-Reihenfolge geändert     |
| `ControlsAligned`            | Controls ausgerichtet      |
| `ControlsGrouped`            | Controls gruppiert         |
| `ControlsUngrouped`          | Controls entgruppiert      |
| `BandAdded`                  | Band hinzugefügt           |
| `BandDeleted`                | Band gelöscht              |
| `BandResized`                | Band skaliert              |
| `BandMoved`                  | Band verschoben            |
| `BandPropChanged`            | Band-Eigenschaft geändert  |
| `BandToggled`                | Band ein-/ausgeklappt      |
| `PageAdded`                  | Seite hinzugefügt          |
| `PageDeleted`                | Seite gelöscht             |
| `ActionAreaChanged`          | ActionArea geändert        |
| `TemplateChanged`            | Template geändert          |
| `PopupChanged`               | Popup geändert             |
| `PopupResized`               | Popup skaliert             |

---

## Menü-Integration (optional)

Für "Rückgängig: Control verschoben" in der Menüleiste:

```xml
<MenuItem Header="{Binding UndoLabel}" Command="{Binding UndoCommand}" />
<MenuItem Header="{Binding RedoLabel}" Command="{Binding RedoCommand}" />
```

Im ViewModel:

```csharp
public string UndoLabel =>
    SnapshotManager.NextUndoLabel(SnapshotContext.Screen) is string l
        ? $"Rückgängig: {l}"
        : "Rückgängig";

public string RedoLabel =>
    SnapshotManager.NextRedoLabel(SnapshotContext.Screen) is string l
        ? $"Wiederholen: {l}"
        : "Wiederholen";
```
