// ======================================================================================
// FILE: Mockup/Actions/ActionAreaEditorViewModel.cs
//
// ZWECK (CLEAN):
// - Editiert die Actions einer ActionArea (Trigger → ActionType → Parameter)
// - Keine IEditorDialog-Abhängigkeit
// - OK schreibt direkt in ActionArea.Actions zurück
// - Cancel verwirft
// - Window schließt über RequestClose (DialogResult)
//
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Mockup.Actions;

#region === ActionRow ====================================================================

public sealed partial class ActionRow : ObservableObject
{
    #region === Identity ===

    public ActionTrigger Trigger { get; init; }

    #endregion

    #region === Editable Fields ===

    [ObservableProperty] private ActionType type = ActionType.None;

    // Parameter (je nach Type)
    [ObservableProperty] private long? targetScreenId;   // Navigate
    [ObservableProperty] private long? popupId;          // Popup
    [ObservableProperty] private string? path;           // OpenFile
    [ObservableProperty] private string? url;            // OpenURL

    #endregion

    #region === Helpers ===

    public ActionDefinition ToDefinition()
    {
        var def = new ActionDefinition
        {
            Trigger = Trigger,
            Type = Type
        };
        def.Parameters ??= new Dictionary<string, string>();

        switch (Type)
        {
            case ActionType.Navigate:
                def.TargetScreenId = TargetScreenId;
                def.Url = null;
                def.FilePath = null;
                def.PopupId = null;
                break;

            case ActionType.OpenFile:
                def.FilePath = Path;
                def.TargetScreenId = null;
                def.Url = null;
                def.PopupId = null;
                break;

            case ActionType.OpenURL:
                def.Url = Url;
                def.TargetScreenId = null;
                def.FilePath = null;
                def.PopupId = null;
                break;

            case ActionType.ShowPopup:
                def.PopupId = PopupId;
                def.TargetScreenId = null;
                def.Url = null;
                def.FilePath = null;
                break;

            default:
                def.TargetScreenId = null;
                def.Url = null;
                def.FilePath = null;
                def.PopupId = null;
                break;
        }
        return def;
    }

    public void LoadFrom(ActionDefinition def)
    {
        Type = def.Type;
        switch (def.Type)
        {
            case ActionType.Navigate:
                TargetScreenId = def.TargetScreenId;
                Path = null;
                Url = null;
                PopupId = null;
                break;

            case ActionType.OpenFile:
                Path = def.FilePath;
                TargetScreenId = null;
                Url = null;
                PopupId = null;
                break;

            case ActionType.OpenURL:
                Url = def.Url;
                TargetScreenId = null;
                Path = null;
                PopupId = null;
                break;

            case ActionType.ShowPopup:
                PopupId = def.PopupId;
                TargetScreenId = null;
                Path = null;
                Url = null;
                break;

            default:
                TargetScreenId = null;
                Path = null;
                Url = null;
                PopupId = null;
                break;
        }
    }

    #endregion
}

#endregion

#region === ViewModel ====================================================================

public sealed partial class ActionAreaEditorViewModel : ObservableObject
{
    #region === Events ===

    /// <summary>
    /// RequestClose(true) => OK / Save, RequestClose(false) => Cancel
    /// </summary>
    public event Action<bool>? RequestClose;

    #endregion

    #region === Ctor ===

    private readonly ActionArea _area;
    private readonly Action? _beforeApply;
    private bool _constructed;

    public bool HasAppliedChanges { get; private set; }

    public ActionAreaEditorViewModel(
        ActionArea area,
        IEnumerable<Screen> screens,
        Action? beforeApply = null)
    {
        _area = area ?? throw new ArgumentNullException(nameof(area));
        _beforeApply = beforeApply;

        Screens = new ObservableCollection<Screen>(
            (screens ?? Enumerable.Empty<Screen>())
                .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase));

        ActionTypes =
        [
            ActionType.None,
            ActionType.NavigateBack,
            ActionType.NavigateHome,
            ActionType.Navigate,
            ActionType.OpenFile,
            ActionType.ShowPopup
        ];

        Triggers =
        [
            new ActionRow { Trigger = ActionTrigger.Tap },
            new ActionRow { Trigger = ActionTrigger.DoubleTap },
            new ActionRow { Trigger = ActionTrigger.LongPress },
            new ActionRow { Trigger = ActionTrigger.SwipeLeft },
            new ActionRow { Trigger = ActionTrigger.SwipeRight },
            new ActionRow { Trigger = ActionTrigger.SwipeUp },
            new ActionRow { Trigger = ActionTrigger.SwipeDown }
        ];

        foreach (var row in Triggers)
            HookRow(row);

        // Load from current ActionArea
        if (_area.Actions != null)
        {
            foreach (var def in _area.Actions)
            {
                var row = Triggers.FirstOrDefault(t => t.Trigger == def.Trigger);
                row?.LoadFrom(def);
            }
        }

        SelectedTriggerRow =
            Triggers.FirstOrDefault(t => t.Type != ActionType.None)
            ?? Triggers.FirstOrDefault();

        OkCommand = new RelayCommand(Ok);
        CancelCommand = new RelayCommand(Cancel);

        _constructed = true;
    }

    #endregion

    #region === Bindings ===

    public ObservableCollection<Screen> Screens { get; }

    public ObservableCollection<ActionType> ActionTypes { get; }

    public ObservableCollection<ActionRow> Triggers { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UserInfo))]
    private ActionRow? selectedTriggerRow;

    public string UserInfo => SelectedTriggerRow?.Type switch
    {
        ActionType.Navigate => "Select target Screen",
        ActionType.OpenFile => "Select target File",
        ActionType.OpenURL => "Enter target URL",
        _ => string.Empty,
    };

    #endregion

    #region === Commands ===

    public IRelayCommand<ActionType> SetActionTypeCommand =>
        new RelayCommand<ActionType>(t =>
        {
            if (SelectedTriggerRow != null)
                SelectedTriggerRow.Type = t;
        });

    public IRelayCommand OkCommand { get; }

    public IRelayCommand CancelCommand { get; }

    private void Ok()
    {
        var defs = Triggers
            .Where(t => t.Type != ActionType.None)
            .Select(t => t.ToDefinition())
            .ToList();

        if (AreSameActionDefinitions(_area.Actions, defs))
        {
            RequestClose?.Invoke(true);
            return;
        }

        _beforeApply?.Invoke();

        // zurück ins Modell
        _area.Actions.Clear();
        foreach (var d in defs)
            _area.Actions.Add(d);

        HasAppliedChanges = true;
        RequestClose?.Invoke(true);
    }

    private void Cancel()
        => RequestClose?.Invoke(false);

    private static bool AreSameActionDefinitions(
        IReadOnlyList<ActionDefinition>? current,
        IReadOnlyList<ActionDefinition> edited)
    {
        var left = (current ?? Array.Empty<ActionDefinition>())
            .OrderBy(x => x.Trigger)
            .ThenBy(x => x.Type)
            .ToList();

        var right = edited
            .OrderBy(x => x.Trigger)
            .ThenBy(x => x.Type)
            .ToList();

        if (left.Count != right.Count)
            return false;

        for (int i = 0; i < left.Count; i++)
        {
            if (!AreSameActionDefinition(left[i], right[i]))
                return false;
        }

        return true;
    }

    private static bool AreSameActionDefinition(ActionDefinition left, ActionDefinition right)
    {
        return left.Trigger == right.Trigger
            && left.Type == right.Type
            && left.TargetScreenId == right.TargetScreenId
            && left.PopupId == right.PopupId
            && string.Equals(left.FilePath, right.FilePath, StringComparison.Ordinal)
            && string.Equals(left.Url, right.Url, StringComparison.Ordinal)
            && AreSameParameters(left.Parameters, right.Parameters);
    }

    private static bool AreSameParameters(
        IReadOnlyDictionary<string, string>? left,
        IReadOnlyDictionary<string, string>? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left == null || right == null)
            return false;

        if (left.Count != right.Count)
            return false;

        foreach (var pair in left)
        {
            if (!right.TryGetValue(pair.Key, out var value))
                return false;

            if (!string.Equals(pair.Value, value, StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    #endregion

    #region === Row Change Handling ===

    private void HookRow(ActionRow row)
        => row.PropertyChanged += OnRowPropertyChanged;

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_constructed)
            return;

        if (sender is not ActionRow row)
            return;

        if (e.PropertyName == nameof(ActionRow.Type))
        {
            // Typwechsel: andere Felder bereinigen
            switch (row.Type)
            {
                case ActionType.Navigate:
                    row.Path = null;
                    row.Url = null;
                    break;

                case ActionType.OpenFile:
                    row.TargetScreenId = null;
                    row.Url = null;
                    break;

                case ActionType.OpenURL:
                    row.TargetScreenId = null;
                    row.Path = null;
                    break;

                case ActionType.ShowPopup:
                    row.TargetScreenId = null;
                    row.Path = null;
                    row.Url = null;
                    break;

                default:
                    row.TargetScreenId = null;
                    row.Path = null;
                    row.Url = null;
                    break;
            }

            OnPropertyChanged(nameof(UserInfo));
        }
    }

    #endregion
}

#endregion
