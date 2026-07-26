// ======================================================================================
// FILE: Mockup.Snapshots/SnapshotLabels.cs
//
// ZWECK:
//   Vordefinierte Label-Konstanten für alle Undo/Redo-Aktionen in UserFlow.
//   Werden beim Push-Aufruf als "label"-Parameter übergeben und in der
//   UI als "Undo: Steuerelement verschoben" angezeigt.
//
// USAGE:
//   SnapshotManager.Push(screen, SnapshotContext.Screen,
//       SnapshotLabels.ControlMoved, screen.Id);
// ======================================================================================

namespace Mockup.Snapshots;

/// <summary>
/// Vordefinierte Labels für alle Undo/Redo-Aktionen in UserFlow.
/// Stellt sicher, dass Bezeichnungen konsistent und zentral gepflegt werden.
/// </summary>
public static class SnapshotLabels
{
    // ─────────────────────────────────────────────────────────────
    //  Screens
    // ─────────────────────────────────────────────────────────────

    /// <summary>Screen-Eigenschaften oder Screen-Aufbau geändert.</summary>
    public const string ScreenChanged = "Bildschirm geändert";

    /// <summary>Screen zur Collection hinzugefügt.</summary>
    public const string ScreenAdded = "Bildschirm hinzugefügt";

    /// <summary>Screen aus der Collection gelöscht.</summary>
    public const string ScreenDeleted = "Bildschirm gelöscht";

    // ─────────────────────────────────────────────────────────────
    //  Controls
    // ─────────────────────────────────────────────────────────────

    /// <summary>Control per Drag-Drop auf dem Designer abgelegt.</summary>
    public const string ControlDropped = "Steuerelement hinzugefügt";

    /// <summary>Control(s) im Designer verschoben.</summary>
    public const string ControlMoved = "Steuerelement verschoben";

    /// <summary>Control im Designer in der Größe verändert.</summary>
    public const string ControlResized = "Steuerelement skaliert";

    /// <summary>Control(s) gelöscht.</summary>
    public const string ControlDeleted = "Steuerelement gelöscht";

    /// <summary>Control-Eigenschaft im PropertyGrid geändert.</summary>
    public const string ControlPropChanged = "Eigenschaft geändert";

    /// <summary>Controls aus der Zwischenablage eingefügt.</summary>
    public const string ControlPasted = "Eingefügt";

    /// <summary>Controls dupliziert.</summary>
    public const string ControlDuplicated = "Dupliziert";

    /// <summary>Controls in der Z-Reihenfolge verschoben.</summary>
    public const string ControlZOrderChanged = "Z-Reihenfolge geändert";

    /// <summary>Controls ausgerichtet (Alignment-Toolbar).</summary>
    public const string ControlsAligned = "Steuerelemente ausgerichtet";

    /// <summary>Controls gruppiert.</summary>
    public const string ControlsGrouped = "Steuerelemente gruppiert";

    /// <summary>Controls entgruppiert.</summary>
    public const string ControlsUngrouped = "Steuerelemente entgruppiert";

    // ─────────────────────────────────────────────────────────────
    //  Bands
    // ─────────────────────────────────────────────────────────────

    /// <summary>Neues Band zum Screen hinzugefügt.</summary>
    public const string BandAdded = "Band hinzugefügt";

    /// <summary>Band gelöscht.</summary>
    public const string BandDeleted = "Band gelöscht";

    /// <summary>Band in der Größe verändert.</summary>
    public const string BandResized = "Band skaliert";

    /// <summary>Band nach oben oder unten verschoben.</summary>
    public const string BandMoved = "Band verschoben";

    /// <summary>Band-Eigenschaft geändert (Titel, Hintergrund, etc.).</summary>
    public const string BandPropChanged = "Band-Eigenschaft geändert";

    /// <summary>Band auf- oder zugeklappt.</summary>
    public const string BandToggled = "Band ein-/ausgeklappt";

    // ─────────────────────────────────────────────────────────────
    //  Pages
    // ─────────────────────────────────────────────────────────────

    /// <summary>Neue Seite zu einem Band hinzugefügt.</summary>
    public const string PageAdded = "Seite hinzugefügt";

    /// <summary>Seite gelöscht.</summary>
    public const string PageDeleted = "Seite gelöscht";

    // ─────────────────────────────────────────────────────────────
    //  Action Areas
    // ─────────────────────────────────────────────────────────────

    /// <summary>ActionArea hinzugefügt oder konfiguriert.</summary>
    public const string ActionAreaChanged = "ActionArea geändert";

    // ─────────────────────────────────────────────────────────────
    //  Templates
    // ─────────────────────────────────────────────────────────────

    /// <summary>Template-Inhalt geändert.</summary>
    public const string TemplateChanged = "Vorlage geändert";

    /// <summary>Template zur Collection hinzugefügt.</summary>
    public const string TemplateAdded = "Vorlage hinzugefügt";

    /// <summary>Template aus der Collection gelöscht.</summary>
    public const string TemplateDeleted = "Vorlage gelöscht";

    // ─────────────────────────────────────────────────────────────
    //  Popups
    // ─────────────────────────────────────────────────────────────

    /// <summary>Popup-Inhalt geändert.</summary>
    public const string PopupChanged = "Popup geändert";

    /// <summary>Popup zur Collection hinzugefügt.</summary>
    public const string PopupAdded = "Popup hinzugefügt";

    /// <summary>Popup aus der Collection gelöscht.</summary>
    public const string PopupDeleted = "Popup gelöscht";

    /// <summary>Popup-Größe verändert.</summary>
    public const string PopupResized = "Popup skaliert";
}

