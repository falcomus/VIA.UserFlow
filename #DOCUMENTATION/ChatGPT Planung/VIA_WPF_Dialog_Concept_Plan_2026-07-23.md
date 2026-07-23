# VIA.WPF – Zentrales Dialogkonzept: Architektur- und Migrationsplan

Stand: 23.07.2026  
Status: **Planung – keine Implementierung durch dieses Dokument**

## 1. Ausgangslage und Problem

Dialoge werden derzeit aus mehreren Schichten gesteuert:

```text
UserFlow Command
    -> MSG.UI.ShowOverlay()
    -> UserFlow DialogService.EditEntity()
    -> Project/Screen/Template/PopupDialog
    -> Window.ShowDialog()
```

Hinzu kommen Sonderwege für verschachtelte Dialoge, insbesondere den `XColorPickerDialog`. Die Zuständigkeiten für Owner, Modalität, Visual Overlay und fachliches Commit sind dadurch verteilt.

Konkrete Risiken:

- Ein Dialog kann vor dem falschen Fenster oder hinter dem Overlay erscheinen.
- Ein modaler Dialog blockiert das Hauptfenster, ist aber nicht sichtbar.
- `ShowOverlay()`/`HideOverlay()` werden aus mehreren Ebenen aufgerufen.
- Der globale Overlay-Zähler (`MSG.UI`) kennt keinen konkreten Owner.
- Verschachtelte Dialoge benötigen Sonderbehandlung für `Owner`.
- Fachlogik (Clone, Validierung, Commit, Snapshot) kann sich unabsichtlich mit UI-Lifecycle vermischen.

Das Ergebnis darf nicht von der Reihenfolge einzelner Aufrufe abhängen.

## 2. Zielbild

VIA.WPF stellt einen kleinen, domänenneutralen Dialogdienst bereit. Jede App kann ihn ohne Kenntnis der Fenster-/Overlay-Details verwenden.

```text
UserFlow DialogService.EditEntity<T>
    ├─ Clone erzeugen
    ├─ VW XDialogService.ShowModal(...)
    ├─ bei OK: validieren, Snapshot, Commit
    └─ bei Cancel: Clone verwerfen

VIA.WPF XDialogService
    ├─ Owner bestimmen
    ├─ Owner-Overlay anzeigen
    ├─ Dialog aktivieren und modal anzeigen
    ├─ verschachtelte Dialoge behandeln
    └─ Overlay/Fokus in finally zuverlässig aufräumen
```

Wichtige Trennung:

| Thema | Verantwortlich |
|---|---|
| Owner, Fensterposition, Modalität, Fokus, Dialog-Overlay | VIA.WPF |
| Dialoginhalt, `DataContext`, Validierung | jeweilige Anwendung / Dialog |
| Clone, Commit, Snapshots, Persistenz, Undo/Redo | UserFlow |
| Busy-/Loading-Overlay | Anwendung bzw. eigenständiger Busy-Service |
| Flyout-/Toolbox-Overlay | jeweiliges Control, nicht der Dialogdienst |

## 3. Grundregeln

1. **Ein Dialog aktiviert nie selbst ein globales Overlay.**
2. **Ein Overlay gehört genau zu einem konkreten Owner-Fenster.**
3. **`try/finally` ist Pflicht**: Overlay und Fokus werden auch bei Exceptions, Cancel und Window-Close aufgeräumt.
4. **Ein Owner wird vor `ShowDialog()` gesetzt.**
5. **Dialoge ohne Owner bleiben funktionsfähig** und werden zentriert auf dem Bildschirm angezeigt.
6. **Keine `Topmost`-Lösung.** Sie löst Z-Order-Probleme nicht sauber und erzeugt neue.
7. **Keine Clone-Logik in VIA.WPF.** VW kennt weder `Project`, `Screen` noch den fachlichen Unterschied zwischen OK und Commit.
8. **Busy, Modal und Flyout sind getrennte Zustände.** Kein gemeinsamer globaler Zähler.

## 4. Geplante öffentliche VIA.WPF-API

### 4.1 `IXDialogService`

Vorschlag für den öffentlichen Einstieg:

```csharp
public interface IXDialogService
{
    bool? ShowModal(
        Window dialog,
        DependencyObject? ownerSource = null,
        XDialogOptions? options = null);

    Task<bool?> ShowModalAsync(
        Window dialog,
        DependencyObject? ownerSource = null,
        XDialogOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

`ShowModalAsync` ist optional für Phase 1. Sie sollte erst ergänzt werden, wenn sie sauber ohne parallele modale Fenster desselben Owners implementiert werden kann. Die synchrone API ist für WPF-Dialoge der robuste Kern.

### 4.2 `XDialogOptions`

```csharp
public sealed class XDialogOptions
{
    public bool DimOwner { get; init; } = true;
    public double OverlayOpacity { get; init; } = 0.42;
    public bool RestoreOwnerFocus { get; init; } = true;
    public bool ActivateDialog { get; init; } = true;
    public WindowStartupLocation? StartupLocation { get; init; }
}
```

Absichtlich klein halten. Kein Dialogtitel, keine Größe, keine Buttons und kein `DataContext`: Das sind Eigenschaften des Dialogs selbst.

### 4.3 Komfort-Overload (optional)

Nur ergänzen, falls er zwei reale Aufrufer vereinfacht:

```csharp
bool? ShowModal<TDialog>(
    Func<TDialog> createDialog,
    DependencyObject? ownerSource = null,
    XDialogOptions? options = null)
    where TDialog : Window;
```

Der Standard bleibt die Übergabe der bereits konfigurierten Dialoginstanz. Das macht Bindings, Owner-Debugging und Tests transparent.

## 5. Owner-Auflösung

Die Auflösung erfolgt deterministisch in dieser Reihenfolge:

1. Ist `dialog.Owner` bereits gesetzt, bleibt es unverändert.
2. `ownerSource` über `Window.GetWindow(ownerSource)` auflösen.
3. Falls `ownerSource` selbst ein `Window` ist, dieses verwenden.
4. Aktives sichtbares Anwendungsfenster auf dem UI-Thread verwenden.
5. `Application.Current.MainWindow`, sofern sichtbar und nicht der Dialog selbst.
6. Kein Owner: `WindowStartupLocation = CenterScreen`.

Für verschachtelte Dialoge wird immer der aktuell sichtbare Dialog als `ownerSource` übergeben:

```csharp
var picker = new XColorPickerDialog { SelectedColor = currentColor };
bool? accepted = dialogs.ShowModal(picker, ownerSource: this);
```

Dadurch dimmt der ColorPicker den ScreenDialog und nicht erneut das MainWindow.

## 6. Owner-lokales Dialog-Overlay

### 6.0 Vorhandene VW-Infrastruktur richtig einordnen

VIA.WPF besitzt bereits zwei verwandte Bausteine:

- `XViewContainer` samt `ShowEditorOverlayMessage`/`HideEditorOverlayMessage` für **View-interne Editor-Overlays**;
- `IXMessageBoxService`/`XMessageBoxService` für Meldungen, aktuell noch mit WPF-Standard-`MessageBox`.

Der neue Dialogdienst darf `XViewContainer` nicht direkt wiederverwenden: dessen Overlay gehört zu einem konkreten ViewContainer und seinem Detail-Editor, nicht zum Lebenszyklus eines eigenständigen `Window.ShowDialog()`-Fensters. Die bestehenden Theme-Tokens (insbesondere `Scrim`) und das Prinzip eines owner-lokalen Hosts können aber bewusst übernommen werden.

`IXMessageBoxService` bleibt eine eigene, darüber aufbauende Funktion. In einer späteren Phase soll es eine gestylte `XMessageBoxWindow` über denselben `IXDialogService` anzeigen. Normale Informationsfälle bleiben weiterhin bevorzugt Toasts.

### 6.1 Fähigkeit in `XWindow`

`XWindow` erhält eine interne, explizite Fähigkeit für ein Dialog-Overlay, beispielsweise:

```csharp
internal IDisposable BeginModalOverlay(XDialogOverlayOptions options);
```

oder über ein internes Interface:

```csharp
internal interface IXDialogOverlayHost
{
    IDisposable AcquireDialogOverlay(XDialogOverlayOptions options);
}
```

Das Overlay muss:

- innerhalb des konkreten `XWindow` liegen;
- oberhalb des Window-Inhalts, aber unter einem Owned Dialog gerendert werden;
- bei mehreren verschachtelten Dialogen pro Owner referenzgezählt werden;
- visuell dimmen und Interaktion im Owner verhindern;
- keine globale Messenger-Nachricht benötigen.

### 6.2 Nicht-`XWindow`-Owner

Falls eine Anwendung ein normales WPF-`Window` als Owner verwendet:

- Modalität durch `ShowDialog()` funktioniert weiterhin.
- Das visuelle Dimmen wird in Phase 1 ausgelassen oder über einen neutralen Adorner-Layer realisiert.
- Keine fragilen Template-Annahmen über fremde Fenster treffen.

Für UserFlow ist der Hauptfall `XWindow`, sodass das vollständige Erlebnis dort verfügbar ist.

### 6.3 Overlay-Arten konsequent trennen

| Art | Scope | Beispiel |
|---|---|---|
| Dialog-Overlay | ein Owner-Fenster | ProjectDialog, ColorPicker |
| Busy-Overlay | Anwendung/Fenster, blockierend | Projekt laden/speichern |
| Flyout-Overlay | View/Control | Toolbox Rail, Navigation |

`MSG.UI.ShowOverlay()` darf nach der Migration nicht mehr für normale modale Edit-Dialoge zuständig sein. Es bleibt nur für einen klar getrennten Busy-/Loading-Fall oder wird entsprechend umbenannt/refaktoriert.

## 7. Ablauf von `XDialogService.ShowModal`

```text
1. Prüfen: dialog != null, UI-Thread sicherstellen
2. Owner deterministisch auflösen
3. Owner und WindowStartupLocation setzen
4. Dialog-Overlay am tatsächlichen Owner erwerben
5. Dialog Loaded/Activated sicherstellen
6. dialog.ShowDialog() ausführen
7. finally:
   - Overlay-Handle disposen
   - Dialog bei Bedarf schließen/aufräumen
   - Owner aktivieren und Fokus zurückgeben
8. DialogResult zurückgeben
```

Referenzpseudocode:

```csharp
public bool? ShowModal(Window dialog, DependencyObject? ownerSource = null,
    XDialogOptions? options = null)
{
    ArgumentNullException.ThrowIfNull(dialog);
    options ??= XDialogOptions.Default;

    return RunOnUiThread(() =>
    {
        Window? owner = ResolveOwner(dialog, ownerSource);
        ConfigureOwnerAndLocation(dialog, owner, options);

        using IDisposable? overlay = AcquireOwnerOverlay(owner, options);
        try
        {
            if (options.ActivateDialog)
                dialog.Loaded += (_, _) => dialog.Activate();

            return dialog.ShowDialog();
        }
        finally
        {
            if (options.RestoreOwnerFocus && owner?.IsVisible == true)
                owner.Activate();
        }
    });
}
```

Die tatsächliche Implementation darf den Eventhandler nicht dauerhaft anhängen; für die Aktivierung ist ein einmaliger Handler oder Dispatcher-Aufruf zu verwenden.

## 8. Dialogklassen in VIA.WPF

### Phase 1

Bestehende Dialoge dürfen weiterhin von `XWindow` bzw. der gegenwärtigen `ModalDialogWindow`-Basisklasse erben. `ModalDialogWindow` darf dabei **keine** Overlay-Logik mehr enthalten.

### Phase 2 (optional)

Wenn sich ein klarer Mehrwert zeigt, kann VIA.WPF eine optische Basis `XDialogWindow : XWindow` anbieten:

- dialoggerechte Standardgrößen und `ResizeMode`;
- optionale Header-/Footer-Styles;
- Default für Primary/Secondary-Footer;
- keine fachliche Logik;
- insbesondere kein automatisches globales Overlay.

Diese Klasse ist eine Stilbasis, nicht der Lifecycle-Manager. Der `XDialogService` bleibt der einzige Ort für Owner/Overlay/Modalität.

### 8.1 Registrierungs- und Nutzungsmodell

Der Dienst muss ohne verpflichtenden DI-Container nutzbar sein, da bestehende WPF-Anwendungen oft direkt mit Fenstern/Views arbeiten. Gleichzeitig soll er DI-freundlich sein:

```csharp
// Direkte Verwendung
var dialogs = XDialogService.Default;
bool? result = dialogs.ShowModal(dialog, this);

// oder über DI
services.AddSingleton<IXDialogService, XDialogService>();
```

`Default` darf keine globale Dialog- oder Overlay-Instanz halten; er ist nur eine stateless Service-Instanz. Owner-spezifische Overlay-Zustände liegen beim jeweiligen Owner bzw. in einer internen, per `Window` schwach referenzierten Registry.

## 9. UserFlow-Migration

### 9.1 Was in UserFlow bleibt

`Mockup/Dialogs/Service/DialogService.EditEntity<T>` bleibt fachlich zuständig:

1. `CloneProfiles.CloneForEditor(source)` aufrufen.
2. Dialog mit Clone als `DataContext` erzeugen.
3. VW-Dialogdienst aufrufen.
4. Nur bei `DialogResult == true`: `beforeApply`, Snapshot und Commit.
5. Bei Cancel/Close: Clone verwerfen, Original unverändert lassen.
6. Spezielle Screen-Commit-Logik (`Bands`, atomare Collections, Layout) bleibt unverändert.

Clones sind somit **wichtig**, aber sie gehören nicht in VIA.WPF. Sie schützen die UserFlow-Fachobjekte vor Teiländerungen bei Cancel und erlauben fachlich korrekte Snapshots vor dem Commit.

### 9.2 Zielcode im UserFlow-Service

```csharp
var dialog = createDialog(clone);
dialog.Title = title;
dialog.DataContext ??= clone;

bool? result = _xDialogService.ShowModal(dialog, ownerSource: ownerSource);
if (result != true)
    return false;

beforeApply?.Invoke();
CommitClone(source, clone);
return true;
```

`ownerSource` soll bevorzugt aus der aufrufenden View/Control kommen. Nur falls der ViewModel-Aufruf keinen Bezug zur View hat, darf der VW-Service sicher auf das aktive MainWindow zurückfallen.

### 9.3 Migrationsreihenfolge

| Schritt | Dialoge | Ziel |
|---|---|---|
| 1 | ProjectDialog | New/Edit + OK/Cancel stabilisieren |
| 2 | TemplateDialog, PopupDialog | identischer Standardfall |
| 3 | ScreenDialog | inkl. verschachtelter ColorPicker und Bands/Pages |
| 4 | XColorPickerDialog | Owner ist der aufrufende Dialog |
| 5 | ColorSchemaEditor, ImageRefDialog, ActionAreaEditor | restliche Sonderwege angleichen |
| 6 | Loading/Busy | von Edit-Dialog-Overlay trennen |

Nach jeder Zeile: bauen, manuell testen, lokal committen. Erst dann nächsten Dialogtyp migrieren.

## 10. Testmatrix

### 10.1 Grundfälle pro Dialog

Für Project, Screen, Template und Popup jeweils:

- New öffnen, OK bestätigen.
- New öffnen, Cancel.
- Edit öffnen, Werte ändern, OK.
- Edit öffnen, Werte ändern, über X schließen.
- Originalobjekt nach Cancel/X unverändert.
- Originalobjekt nach OK korrekt aktualisiert.
- Light Mode und Dark Mode.
- MainWindow normal, maximiert und nach Themewechsel.

### 10.2 Verschachtelung

- ScreenDialog → Background ColorPicker → OK.
- ScreenDialog → Header/Footer/Band ColorPicker → Cancel.
- PropertyEditor → ColorPicker → OK → Skia-Control aktualisiert.
- ColorSchemaEditor → ColorPicker → OK/Cancel.
- Nach jedem inneren Dialog bleibt der äußere Dialog sichtbar, aktiv und bedienbar.
- Nach Schließen des äußeren Dialogs ist kein Overlay mehr sichtbar und MainWindow ist bedienbar.

### 10.3 Fehler- und Aufräumfälle

- Exception beim Erzeugen eines Dialogs.
- Exception in `Loaded` oder `DataContext`-Binding.
- Dialog wird programmgesteuert geschlossen.
- Dialog ohne Owner.
- Mehrfaches schnelles Öffnen verhindern oder sauber serialisieren.
- App-Schließen während eines Dialogs.

### 10.4 Automatisierbare Tests in VIA.WPF

- Owner-Auflösung: explizit, `ownerSource`, aktives Fenster, MainWindow, kein Owner.
- Overlay-Referenzzählung pro Owner.
- Verschachtelung: MainWindow → Dialog A → Dialog B.
- `finally`-Cleanup nach simuliertem Fehler.
- Kein globaler Overlay-Zustand zwischen zwei Fenstern.

Eine kleine VW-Demo-Seite mit Hauptfenster, Dialog und verschachteltem ColorPicker ist Pflicht, bevor UserFlow umgestellt wird.

### 10.5 MessageBox und Toasts (Folgephase)

- Toast bei erfolgreichem Speichern, Export, Import oder unkritischen Hinweisen.
- `XMessageBoxWindow` nur bei Entscheidung, Warnung oder nicht rückgängig zu machendem Schritt.
- `XMessageBoxService` delegiert für die Anzeige an `IXDialogService`; es verwaltet kein eigenes Overlay.
- Light/Dark, Tastatur (Enter/Escape), DefaultButton, Accessibility und Owner-Verhalten separat testen.

## 11. Branch- und Commit-Plan

1. UserFlow: aktuellen akuten Regression-Fix separat testen und als kleinen Sicherungscommit abschließen.
2. VIA.WPF: neuer Branch, z. B. `codex/dialog-service-foundation`.
3. Implementierung nur in VIA.WPF, mit Demo und Tests; lokaler Commit.
4. VIA.WPF-Branch manuell testen, dann pushen/auf `master` übernehmen nach ausdrücklicher Bestätigung.
5. UserFlow: neuer Branch, z. B. `codex/dialog-service-userflow-migration`.
6. Dialogtypen einzeln migrieren und pro Gruppe committen.
7. Erst nach vollständigem Test alte `MSG.UI.ShowOverlay()`-Aufrufe für Edit-Dialoge entfernen.

Kein paralleles Mischen von Theme-/Designer-/Dialog-Refactoring in diese Branches.

## 12. Nicht-Ziele der ersten Version

- Keine allgemeine Dialog-ViewModel-Abstraktion erzwingen.
- Keine Clone-/Undo-/Snapshot-Infrastruktur in VIA.WPF.
- Keine Topmost-Fenster.
- Keine globale Overlay-Nachricht als Dialog-Lifecycle.
- Keine Umstellung sämtlicher UserFlow-Dialoge in einem Schritt.
- Keine asynchrone Pseudo-Modalität, solange der synchrone WPF-Fall nicht stabil ist.

## 13. Akute Sofortmaßnahme vor dem Umbau

Der aktuelle UserFlow-Branch `codex/dialog-editing-regression` enthält einen minimalen, noch zu testenden Fix: `ModalDialogWindow` ist wieder reine `XWindow`-Basis ohne eigenes Overlay. Dieser Fix muss interaktiv für New/Edit Project, Screen, Template und Popup geprüft werden.

Er ist bewusst unabhängig vom neuen VW-Dialogservice und soll erst nach erfolgreichem Test als Sicherungsstand committed werden.
