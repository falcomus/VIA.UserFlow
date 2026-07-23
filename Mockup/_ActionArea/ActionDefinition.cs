// ======================================================================================
// FILE: Mockup.Actions/ActionDefinition.cs
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;

namespace Mockup.Actions;

/// <summary>
/// Art der Aktion: Navigation, Datei, URL – und neu: Popup anzeigen.
/// </summary>
public enum ActionType
{
    None = 0,
    Navigate,
    NavigateBack,
    NavigateHome,
    OpenFile,
    OpenURL,
    ShowPopup
}

/// <summary>
/// Auslöser der Aktion.
/// </summary>
public enum ActionTrigger
{
    None,
    Tap,
    LongPress,
    DoubleTap,
    SwipeLeft,
    SwipeRight,
    SwipeUp,
    SwipeDown
}

/// <summary>
/// Eine konkrete Aktion, die z. B. an eine ActionArea gebunden ist.
/// - Source of truth bleibt Parameters (stabil für JSON).
/// - Zusätzlich ObservableProperties für XAML/Editor-Komfort, die in Parameters gespiegelt werden.
/// </summary>
public sealed partial class ActionDefinition : ObservableObject
{
    #region === Core ===

    [ObservableProperty]
    private ActionType type = ActionType.None;

    [ObservableProperty]
    private ActionTrigger trigger = ActionTrigger.Tap;

    [ObservableProperty]
    private Dictionary<string, string> parameters = new();

    #endregion

    #region === Bindable Convenience Properties (für XAML) ===

    // NOTE: Nicht "Path" nennen (WPF/Binder-Konflikte). Daher FilePath.

    [ObservableProperty]
    private long? targetScreenId;

    [ObservableProperty]
    private string? url;

    [ObservableProperty]
    private string? filePath;

    [ObservableProperty]
    private long? popupId;

    [ObservableProperty]
    private ScreenPopupPosition? popupPosition;

    [ObservableProperty]
    private bool? useMousePos;

    #endregion

    #region === Sync (Parameters <-> Convenience) ===

    private bool _syncing;

    public ActionDefinition()
    {
        // falls jemand direkt mit den Convenience-Props arbeitet:
        SyncFromParameters();
    }

    partial void OnParametersChanged(Dictionary<string, string> value)
        => SyncFromParameters();

    private void SyncFromParameters()
    {
        if (_syncing)
            return;

        try
        {
            _syncing = true;

            TargetScreenId = GetLong("screenId");
            Url = GetString("url");
            FilePath = GetString("path");

            PopupId = GetLong("popupId");
            PopupPosition = GetEnum<ScreenPopupPosition>("popupPos");
            UseMousePos = GetBool("mousePos");
        }
        finally
        {
            _syncing = false;
        }
    }

    partial void OnTargetScreenIdChanged(long? value)
    {
        if (_syncing) return;
        SetLong("screenId", value);
    }

    partial void OnUrlChanged(string? value)
    {
        if (_syncing) return;
        SetString("url", value);
    }

    partial void OnFilePathChanged(string? value)
    {
        if (_syncing) return;
        SetString("path", value);
    }

    partial void OnPopupIdChanged(long? value)
    {
        if (_syncing) return;
        SetLong("popupId", value);
    }

    partial void OnPopupPositionChanged(ScreenPopupPosition? value)
    {
        if (_syncing) return;
        SetEnum("popupPos", value);
    }

    partial void OnUseMousePosChanged(bool? value)
    {
        if (_syncing) return;
        SetBool("mousePos", value);
    }

    #endregion

    #region === Helpers ===

    private string? GetString(string key)
        => Parameters.TryGetValue(key, out var v) ? v : null;

    private void SetString(string key, string? value)
        => SetParam(key, value);

    private long? GetLong(string key)
    {
        if (!Parameters.TryGetValue(key, out var v))
            return null;

        return long.TryParse(v, out var id) ? id : null;
    }

    private void SetLong(string key, long? value)
        => SetParam(key, value?.ToString());

    private bool? GetBool(string key)
    {
        if (!Parameters.TryGetValue(key, out var v))
            return null;

        return bool.TryParse(v, out var b) ? b : null;
    }

    private void SetBool(string key, bool? value)
        => SetParam(key, value?.ToString());

    private TEnum? GetEnum<TEnum>(string key) where TEnum : struct, Enum
    {
        if (!Parameters.TryGetValue(key, out var v))
            return null;

        return Enum.TryParse<TEnum>(v, ignoreCase: true, out var e) ? e : null;
    }

    private void SetEnum<TEnum>(string key, TEnum? value) where TEnum : struct, Enum
        => SetParam(key, value?.ToString());

    private void SetParam(string key, string? value)
    {
        // remove
        if (string.IsNullOrWhiteSpace(value))
        {
            if (Parameters.Remove(key))
                OnPropertyChanged(nameof(Parameters));

            return;
        }

        // set/update
        if (!Parameters.TryGetValue(key, out var old) ||
            !string.Equals(old, value, StringComparison.Ordinal))
        {
            Parameters[key] = value!;
            OnPropertyChanged(nameof(Parameters));
        }
    }

    #endregion
}
