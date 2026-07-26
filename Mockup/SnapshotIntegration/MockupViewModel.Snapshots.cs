// ======================================================================================
// FILE: Mockup/ViewModel/MockupViewModel.Snapshots.cs
//
// ZWECK:
//   Partial-Klasse des MockupViewModel für Undo/Redo.
//   Stellt PushSnapshot(), Collection-Snapshots, UndoCommand und RedoCommand bereit.
// ======================================================================================

using CommunityToolkit.Mvvm.Input;
using Mockup.Messages;
using Mockup.Resources;
using Mockup.Services;
using Mockup.Snapshots;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using VIA.WPF.Localization;

namespace Mockup.ViewModel;

public partial class MockupViewModel
{
    private bool _isApplyingSnapshotRestore;

    partial void InitLocalization()
    {
        XLocalizationService.Current.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(UndoLabel));
            OnPropertyChanged(nameof(RedoLabel));
            OnPropertyChanged(nameof(SnapshotStatusLabel));
        };
    }

    /// <summary>
    /// Lets WPF paint the changed designer before a synchronous JSON write starts. The
    /// persisted state remains identical; only the visual feedback is no longer delayed
    /// by file IO and serialization.
    /// </summary>
    private void SaveActiveContextMenuSnapshotContextAfterRender()
    {
        Application.Current?.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(SaveActiveContextMenuSnapshotContext));
    }

    private void SaveSnapshotContextAfterRender(SnapshotContext context)
    {
        Application.Current?.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => SaveCurrentSnapshotContext(context)));
    }

    // ─────────────────────────────────────────────────────────────
    //  Initialisierung
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialisiert den SnapshotManager mit dem Mockup-Serializer.
    /// Wird aus LoadAll() aufgerufen.
    /// </summary>
    private void InitializeSnapshots()
    {
        SnapshotManager.ClearAll();

        SnapshotManager.Initialize(
            serializer: new MockupSnapshotSerializer(),
            maxHistory: 50
        );

        NotifyUndoRedoCommandsChanged();
    }

    // ─────────────────────────────────────────────────────────────
    //  Push-API  (wird vom Designer und ViewModel aufgerufen)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Erzeugt einen Snapshot des aktuellen Zustands und legt ihn auf den Undo-Stack.
    /// Muss VOR jeder Mutation aufgerufen werden.
    /// </summary>
    /// <param name="context">Project, Screen, Templates, Template oder Popup.</param>
    /// <param name="label">Beschreibung der folgenden Aktion (aus SnapshotLabels).</param>
    public void PushSnapshot(SnapshotContext context, string label)
    {
        switch (context)
        {
            case SnapshotContext.Project when CurrentProject != null:
                SnapshotManager.Push(CurrentProject, context, label, CurrentProject.Id);
                break;

            case SnapshotContext.Screen when CurrentScreen != null:
                SnapshotManager.Push(CurrentScreen, context, label, CurrentScreen.Id);
                break;

            case SnapshotContext.Templates:
                SnapshotManager.Push(Templates, context, label, 0);
                break;

            case SnapshotContext.Template when CurrentTemplate != null:
                SnapshotManager.Push(CurrentTemplate, context, label, CurrentTemplate.Id);
                break;

            case SnapshotContext.Popup when CurrentPopup != null:
                SnapshotManager.Push(CurrentPopup, context, label, CurrentPopup.Id);
                break;
        }

        NotifyUndoRedoCommandsChanged();
    }

    /// <summary>
    /// Snapshot für projektweite Collection-Änderungen, z.B. Screen/Popup hinzufügen oder löschen.
    /// </summary>
    public void PushProjectSnapshot(string label, long targetId = 0)
    {
        if (CurrentProject == null)
            return;

        SnapshotManager.Push(
            CurrentProject,
            SnapshotContext.Project,
            label,
            targetId != 0 ? targetId : CurrentProject.Id);

        NotifyUndoRedoCommandsChanged();
    }

    /// <summary>
    /// Snapshot für globale Template-Collection-Änderungen, z.B. Template hinzufügen oder löschen.
    /// </summary>
    public void PushTemplatesSnapshot(string label, long targetId = 0)
    {
        SnapshotManager.Push(
            Templates,
            SnapshotContext.Templates,
            label,
            targetId);

        NotifyUndoRedoCommandsChanged();
    }

    // ─────────────────────────────────────────────────────────────
    //  Undo-Command
    // ─────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanExecuteUndo))]
    private void Undo()
    {
        var context = GetUndoSnapshotContext();
        if (context == null)
            return;

        var (currentObject, currentId) = GetCurrentObjectForContext(context.Value);
        if (currentObject == null)
            return;

        var result = SnapshotManager.Undo(
            currentObject,
            context.Value,
            "aktueller Zustand",
            currentId);

        if (!result.Success)
            return;

        ApplyRestoredObject(result);
        NotifyUndoRedoCommandsChanged();
    }

    private bool CanExecuteUndo() => GetUndoSnapshotContext() != null;

    // ─────────────────────────────────────────────────────────────
    //  Redo-Command
    // ─────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanExecuteRedo))]
    private void Redo()
    {
        var context = GetRedoSnapshotContext();
        if (context == null)
            return;

        var (currentObject, currentId) = GetCurrentObjectForContext(context.Value);
        if (currentObject == null)
            return;

        var result = SnapshotManager.Redo(
            currentObject,
            context.Value,
            "aktueller Zustand",
            currentId);

        if (!result.Success)
            return;

        ApplyRestoredObject(result);
        NotifyUndoRedoCommandsChanged();
    }

    private bool CanExecuteRedo() => GetRedoSnapshotContext() != null;

    private void NotifyUndoRedoCommandsChanged()
    {
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();

        OnPropertyChanged(nameof(UndoLabel));
        OnPropertyChanged(nameof(RedoLabel));
        OnPropertyChanged(nameof(SnapshotStatusLabel));
    }

    public string UndoLabel
    {
        get
        {
            var context = GetUndoSnapshotContext();
            string fallback = UiText("Undo.Fallback", "Undo (Ctrl+Z)");

            if (context == null)
                return fallback;

            return SnapshotManager.NextUndoLabel(context.Value) is string label
                ? UiFormat("Undo.Format", "Undo: {0}", LocalizeSnapshotLabel(label))
                : fallback;
        }
    }

    public string RedoLabel
    {
        get
        {
            var context = GetRedoSnapshotContext();
            string fallback = UiText("Redo.Fallback", "Redo (Ctrl+Y)");

            if (context == null)
                return fallback;

            return SnapshotManager.NextRedoLabel(context.Value) is string label
                ? UiFormat("Redo.Format", "Redo: {0}", LocalizeSnapshotLabel(label))
                : fallback;
        }
    }
    public string SnapshotStatusLabel
    {
        get
        {
            var context = GetUndoSnapshotContext()
                          ?? GetRedoSnapshotContext()
                          ?? GetActiveObjectSnapshotContext();

            if (context == null)
                return "History: -";

            int undoCount = SnapshotManager.UndoCount(context.Value);
            int redoCount = SnapshotManager.RedoCount(context.Value);
            string size = SnapshotManager.FormatSize(SnapshotManager.TotalUtf8Bytes(context.Value));

            return $"History: {context.Value} · Undo {undoCount} / Redo {redoCount} · {size}";
        }
    }


    private static string UiText(string key, string fallback)
    {
        return XLocalizationService.Current.GetString(UserFlowResources.ResourceManager, key, fallback);
    }

    private static string UiFormat(string key, string fallback, params object?[] arguments)
    {
        return XLocalizationService.Current.Format(UserFlowResources.ResourceManager, key, fallback, arguments);
    }

    private static string LocalizeSnapshotLabel(string label)
    {
        if (IsSnapshotLabel(label, SnapshotLabels.ControlMoved, "Control verschoben", "Steuerelement verschoben", "Control moved"))
            return UiText("Undo.Label.ControlMoved", "Control moved");

        return label;
    }

    private static bool IsSnapshotLabel(string label, params string[] candidates)
    {
        foreach (string candidate in candidates)
        {
            if (string.Equals(label, candidate, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
    // ─────────────────────────────────────────────────────────────
    //  Restored Object anwenden
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Wendet das wiederhergestellte Objekt auf das ViewModel an.
    /// </summary>
    private void ApplyRestoredObject(SnapshotResult result)
    {
        _isApplyingSnapshotRestore = true;

        try
        {
            switch (result.Context)
            {
                case SnapshotContext.Project when result.TryGetRestored<Project>(out var project):
                    RestoreProject(project!, result.Entry?.TargetId ?? 0);
                    break;

                case SnapshotContext.Screen when result.TryGetRestored<Screen>(out var screen):
                    RestoreScreen(screen!);
                    break;

                case SnapshotContext.Templates when result.TryGetRestored<ObservableCollection<ScreenTemplate>>(out var templates):
                    RestoreTemplates(templates!, result.Entry?.TargetId ?? 0);
                    break;

                case SnapshotContext.Template when result.TryGetRestored<ScreenTemplate>(out var template):
                    RestoreTemplate(template!);
                    break;

                case SnapshotContext.Popup when result.TryGetRestored<ScreenPopup>(out var popup):
                    RestorePopup(popup!);
                    break;
            }
        }
        finally
        {
            _isApplyingSnapshotRestore = false;
        }
    }

    private void RestoreProject(Project restored, long preferredTargetId)
    {
        if (CurrentProject == null)
            return;

        var filePath = CurrentProject.FilePath;
        var previousScreenId = CurrentScreen?.Id ?? 0;
        var previousPopupId = CurrentPopup?.Id ?? 0;

        restored.FilePath = filePath;
        MakeProjectCorrections(restored);

        foreach (var screen in restored.Screens)
            screen.Reconstruct(restored);

        CurrentProject = restored;
        CurrentProject.InitializeTheme();

        HomeScreen = CurrentProject.Screens.FirstOrDefault(x => x.IsHomeScreen)
                     ?? CurrentProject.Screens.FirstOrDefault();

        CurrentScreen = CurrentProject.Screens.FirstOrDefault(x => x.Id == preferredTargetId)
                        ?? CurrentProject.Screens.FirstOrDefault(x => x.Id == previousScreenId)
                        ?? HomeScreen
                        ?? CurrentProject.Screens.FirstOrDefault();

        CurrentPopup = CurrentProject.Popups.FirstOrDefault(x => x.Id == preferredTargetId)
                       ?? CurrentProject.Popups.FirstOrDefault(x => x.Id == previousPopupId)
                       ?? CurrentProject.Popups.FirstOrDefault();

        if (CurrentPopup != null)
        {
            PopupDesignerWidth = CurrentPopup.Width;
            PopupDesignerHeight = CurrentPopup.Height;
        }

        RefreshProjectUiInfo();
        MSG.UI.InvalidateDesigner();
        SaveSnapshotContextAfterRender(SnapshotContext.Project);
    }

    private void RestoreScreen(Screen restored)
    {
        if (CurrentProject == null || CurrentScreen == null)
            return;

        // Referenzen rekonstruieren (wie beim LoadProject)
        restored.Reconstruct(CurrentProject);
        restored.Project = CurrentProject;

        // Screen in der Collection ersetzen
        int idx = CurrentProject.Screens.IndexOf(CurrentScreen);
        if (idx < 0)
            return;

        CurrentProject.Screens[idx] = restored;
        CurrentScreen = restored;

        MSG.UI.InvalidateDesigner();
        SaveSnapshotContextAfterRender(SnapshotContext.Screen);
    }

    private void RestoreTemplates(ObservableCollection<ScreenTemplate> restored, long preferredTargetId)
    {
        var previousTemplateId = CurrentTemplate?.Id ?? 0;

        Templates.Clear();

        foreach (var template in restored)
        {
            NormalizeTemplateAfterRestore(template);
            Templates.Add(template);
        }

        CurrentTemplate = Templates.FirstOrDefault(x => x.Id == preferredTargetId)
                          ?? Templates.FirstOrDefault(x => x.Id == previousTemplateId)
                          ?? Templates.FirstOrDefault();

        RefreshProjectUiInfo();
        MSG.UI.InvalidateDesigner();
        SaveSnapshotContextAfterRender(SnapshotContext.Templates);
    }

    private void NormalizeTemplateAfterRestore(ScreenTemplate template)
    {
        if (CurrentProject != null)
            template.Width = CurrentProject.DeviceWidth;

        foreach (var band in template.Bands)
        {
            if (CurrentProject != null)
                band.Width = CurrentProject.DeviceWidth;

            foreach (var page in band.Pages)
            {
                foreach (var ctrl in page.Controls)
                {
                    if (string.IsNullOrWhiteSpace(ctrl.TypeKey))
                        ctrl.TypeKey = ctrl.GetType().Name;
                }
            }
        }
    }

    private void RestoreTemplate(ScreenTemplate restored)
    {
        if (CurrentTemplate == null)
            return;

        NormalizeTemplateAfterRestore(restored);

        int idx = Templates.IndexOf(CurrentTemplate);
        if (idx < 0)
            return;

        Templates[idx] = restored;
        CurrentTemplate = restored;

        MSG.UI.InvalidateDesigner();
        SaveSnapshotContextAfterRender(SnapshotContext.Template);
    }

    private void RestorePopup(ScreenPopup restored)
    {
        if (CurrentProject == null || CurrentPopup == null)
            return;

        int idx = CurrentProject.Popups.IndexOf(CurrentPopup);
        if (idx < 0)
            return;

        CurrentProject.Popups[idx] = restored;
        CurrentPopup = restored;

        PopupDesignerWidth = CurrentPopup.Width;
        PopupDesignerHeight = CurrentPopup.Height;

        MSG.UI.InvalidateDesigner();
        SaveSnapshotContextAfterRender(SnapshotContext.Popup);
    }

    // ─────────────────────────────────────────────────────────────
    //  Hilfsmethoden
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Ermittelt den aktiven Objekt-Snapshot-Kontext anhand des aktuellen Designer-Tabs.
    /// Gibt null zurück, wenn kein passender Objekt-Kontext aktiv ist.
    /// </summary>
    public SnapshotContext? GetCurrentSnapshotContext() => GetActiveObjectSnapshotContext();

    /// <summary>
    /// Speichert den aktuell geänderten Snapshot-Kontext passend zur Persistenz.
    /// Project/Screen/Popup liegen im Projekt, Templates/Template in der Template-Datei.
    /// </summary>
    public void SaveCurrentSnapshotContext(SnapshotContext context)
    {
        switch (context)
        {
            case SnapshotContext.Project:
            case SnapshotContext.Screen:
            case SnapshotContext.Popup:
                SaveCurrentProject();
                break;

            case SnapshotContext.Templates:
            case SnapshotContext.Template:
                SaveTemplates();
                break;
        }
    }

    public void SaveCurrentSnapshotContext()
    {
        var context = GetActiveObjectSnapshotContext();
        if (context != null)
            SaveCurrentSnapshotContext(context.Value);
    }

    public void PushActionAreaChangedSnapshot()
    {
        var context = GetActiveObjectSnapshotContext();
        if (context != null)
            PushSnapshot(context.Value, SnapshotLabels.ActionAreaChanged);
    }

    private SnapshotContext? GetUndoSnapshotContext()
        => SelectMostRecentSnapshotContext(GetCandidateUndoRedoContexts(), redo: false);

    private SnapshotContext? GetRedoSnapshotContext()
        => SelectMostRecentSnapshotContext(GetCandidateUndoRedoContexts(), redo: true);

    private IEnumerable<SnapshotContext> GetCandidateUndoRedoContexts()
    {
        return MainTabSelectedIndex switch
        {
            1 => new[] { SnapshotContext.Project, SnapshotContext.Screen },
            2 => new[] { SnapshotContext.Templates, SnapshotContext.Template },
            3 => new[] { SnapshotContext.Project, SnapshotContext.Popup },
            _ => new[] { SnapshotContext.Project, SnapshotContext.Templates },
        };
    }

    private static SnapshotContext? SelectMostRecentSnapshotContext(
        IEnumerable<SnapshotContext> contexts,
        bool redo)
    {
        SnapshotContext? selectedContext = null;
        DateTime selectedCreatedAt = DateTime.MinValue;

        foreach (var context in contexts)
        {
            var stack = SnapshotManager.GetStack(context);
            var entry = redo
                ? stack.RedoHistory.FirstOrDefault()
                : stack.UndoHistory.FirstOrDefault();

            if (entry == null)
                continue;

            if (selectedContext == null || entry.CreatedAt >= selectedCreatedAt)
            {
                selectedContext = context;
                selectedCreatedAt = entry.CreatedAt;
            }
        }

        return selectedContext;
    }

    private SnapshotContext? GetActiveObjectSnapshotContext()
    {
        return MainTabSelectedIndex switch
        {
            1 when CurrentScreen != null => SnapshotContext.Screen,
            2 when CurrentTemplate != null => SnapshotContext.Template,
            3 when CurrentPopup != null => SnapshotContext.Popup,
            _ => null,
        };
    }

    /// <summary>
    /// Gibt das aktuelle Objekt und seine ID für den angegebenen Kontext zurück.
    /// </summary>
    private (object? obj, long id) GetCurrentObjectForContext(SnapshotContext context)
    {
        return context switch
        {
            SnapshotContext.Project => (CurrentProject, CurrentProject?.Id ?? 0),
            SnapshotContext.Screen => (CurrentScreen, CurrentScreen?.Id ?? 0),
            SnapshotContext.Templates => (Templates, 0),
            SnapshotContext.Template => (CurrentTemplate, CurrentTemplate?.Id ?? 0),
            SnapshotContext.Popup => (CurrentPopup, CurrentPopup?.Id ?? 0),
            _ => (null, 0),
        };
    }
}
