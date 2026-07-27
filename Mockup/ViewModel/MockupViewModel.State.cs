// ======================================================================================
// FILE: Mockup.ViewModel/MockupViewModel.Data.cs
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.Actions;
using Mockup.Resources;
using System.Collections.ObjectModel;
using VIA.WPF.Localization;

namespace Mockup.ViewModel;

public partial class MockupViewModel : ObservableObject
{
    #region === DEVICE SIZE HELPLINE ===

    public double DeviceHelpLineWidth =>
        CurrentProject == null ? 0 : CurrentProject.DeviceWidth + 250;

    #endregion === DEVICE SIZE HELPLINE ===

    #region === PROJECT STATE ===

    public string ScreenCountInfo => $"{CurrentProject?.Screens.Count} Screens";
    public string TemplateCountInfo => $"{Templates.Count} Templates";
    public string PopupCountInfo => $"{CurrentProject?.Popups.Count} Popups";
    public int ControlCountValue => CurrentProject?.Screens.Sum(s => s.AllControls?.Count() ?? 0) ?? 0;
    public string ControlCountInfo
    {
        get
        {
            return $"{ControlCountValue} Controls";
        }
    }

    #endregion

    #region === TEMPLATE DESIGNER STATE ===

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemplateSizeInfo))]
    [NotifyPropertyChangedFor(nameof(CurrentTemplate))]
    private double templateDesignerHeight = 400;

    partial void OnTemplateDesignerHeightChanged(double value)
    {
        if (CurrentTemplate != null)
            CurrentTemplate.Height = (float)value;
    }

    public string TemplateSizeInfo =>
        $"Size: {CurrentProject?.DeviceWidth} x {TemplateDesignerHeight}";

    #endregion === TEMPLATE DESIGNER STATE ===

    #region === POPUP DESIGNER STATE ===

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PopupSizeInfo))]
    private double popupDesignerWidth;

    partial void OnPopupDesignerWidthChanged(double value)
    {
        if (CurrentPopup != null)
            CurrentPopup.Width = (float)value;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PopupSizeInfo))]
    private double popupDesignerHeight;

    partial void OnPopupDesignerHeightChanged(double value)
    {
        if (CurrentPopup != null)
            CurrentPopup.Height = (float)value;
    }

    public string PopupSizeInfo => $"Size: {PopupDesignerWidth} x {PopupDesignerHeight}";

    #endregion === POPUP DESIGNER STATE ===

    #region === DEVICE / SCREEN INFO ===

    public string DeviceSizeInfo =>
        CurrentProject == null
            ? string.Empty
            : $"Device Size: {CurrentProject.DeviceWidth} x {CurrentProject.DeviceHeight}";

    public string ScreenSizeInfo => $"Size: {CurrentScreen?.Width} x {CurrentScreen?.ScreenHeight}";

    #endregion === DEVICE / SCREEN INFO ===

    #region === ACTION AREA HINT ===

    [ObservableProperty]
    private bool isActionAreaHintVisible;

    [ObservableProperty]
    private ObservableCollection<ActionHintRow> actionAreaHintRows = [];

    public void ShowActionAreaHint(ActionArea? aa)
    {
        ActionAreaHintRows.Clear();

        if (aa == null)
        {
            IsActionAreaHintVisible = false;
            return;
        }

        if (aa.Actions == null || aa.Actions.Count == 0)
        {
            ActionAreaHintRows.Add(new ActionHintRow { Header = ActionAreaText("Dialog.ActionArea.Hint.NoActions", "NO ACTIONS"), Target = "—" });

            IsActionAreaHintVisible = true;
            return;
        }

        var project = CurrentProject;

        foreach (var a in aa.Actions)
        {
            if (a == null)
                continue;

            string header =
                $"{LocalizeActionTrigger(a.Trigger)}: {LocalizeActionType(a.Type)}"
                    .ToUpper(XLocalizationService.Current.CurrentUICulture);

            string target = a.Type switch
            {
                ActionType.Navigate => ResolveScreenName(project, a.TargetScreenId),
                ActionType.NavigateHome => ActionAreaText("Dialog.ActionArea.Hint.Home", "HOME"),
                ActionType.NavigateBack => ActionAreaText("Dialog.ActionArea.Hint.Back", "BACK"),
                ActionType.OpenFile => ShortFileName(a.FilePath ?? ""),
                ActionType.OpenURL => NormalizeUrl(a.Url),
                ActionType.ShowPopup => ResolvePopupName(project, a.PopupId),
                _ => "",
            };

            if (string.IsNullOrWhiteSpace(target))
                target = "—";

            ActionAreaHintRows.Add(new ActionHintRow { Header = header, Target = target });
        }

        IsActionAreaHintVisible = ActionAreaHintRows.Count > 0;
    }

    public void HideActionAreaHint()
    {
        ActionAreaHintRows.Clear();
        IsActionAreaHintVisible = false;
    }

    private static string ActionAreaText(string key, string fallbackText)
    {
        return XLocalizationService.Current.GetString(
            UserFlowResources.ResourceManager,
            key,
            fallbackText);
    }

    private static string LocalizeActionTrigger(ActionTrigger trigger)
    {
        return trigger switch
        {
            ActionTrigger.Tap => ActionAreaText("Dialog.ActionArea.Trigger.Tap", "Tap"),
            ActionTrigger.DoubleTap => ActionAreaText("Dialog.ActionArea.Trigger.DoubleTap", "Double Tap"),
            ActionTrigger.LongPress => ActionAreaText("Dialog.ActionArea.Trigger.LongPress", "Long Press"),
            ActionTrigger.SwipeLeft => ActionAreaText("Dialog.ActionArea.Trigger.SwipeLeft", "Swipe Left"),
            ActionTrigger.SwipeRight => ActionAreaText("Dialog.ActionArea.Trigger.SwipeRight", "Swipe Right"),
            ActionTrigger.SwipeUp => ActionAreaText("Dialog.ActionArea.Trigger.SwipeUp", "Swipe Up"),
            ActionTrigger.SwipeDown => ActionAreaText("Dialog.ActionArea.Trigger.SwipeDown", "Swipe Down"),
            _ => trigger.ToString(),
        };
    }

    private static string LocalizeActionType(ActionType actionType)
    {
        return actionType switch
        {
            ActionType.None => ActionAreaText("Dialog.ActionArea.ActionType.None", "None"),
            ActionType.Navigate => ActionAreaText("Dialog.ActionArea.ActionType.Navigate", "Navigate"),
            ActionType.NavigateHome => ActionAreaText("Dialog.ActionArea.ActionType.NavigateHome", "Navigate Home"),
            ActionType.NavigateBack => ActionAreaText("Dialog.ActionArea.ActionType.NavigateBack", "Navigate Back"),
            ActionType.OpenFile => ActionAreaText("Dialog.ActionArea.ActionType.OpenFile", "Open File"),
            ActionType.OpenURL => ActionAreaText("Dialog.ActionArea.ActionType.OpenUrl", "Open URL"),
            ActionType.ShowPopup => ActionAreaText("Dialog.ActionArea.ActionType.ShowPopup", "Show Popup"),
            _ => actionType.ToString(),
        };
    }

    private static string ResolveScreenName(Project? project, long? id)
    {
        if (id == null)
            return "";

        if (project == null)
            return id.Value.ToString();

        var s = project.Screens.FirstOrDefault(x => x.Id == id.Value);
        return string.IsNullOrWhiteSpace(s?.Name) ? id.Value.ToString() : s!.Name;
    }

    private static string ResolvePopupName(Project? project, long? id)
    {
        if (id == null)
            return "";

        if (project == null)
            return id.Value.ToString();

        var p = project.Popups.FirstOrDefault(x => x.Id == id.Value);
        return string.IsNullOrWhiteSpace(p?.Name) ? id.Value.ToString() : p!.Name;
    }

    private static string ShortFileName(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "";

        try
        {
            var name = System.IO.Path.GetFileName(path);
            return string.IsNullOrWhiteSpace(name) ? path : name;
        }
        catch
        {
            return path;
        }
    }

    private static string NormalizeUrl(string? url)
    {
        url = (url ?? "").Trim();
        return url;
    }

    public sealed class ActionHintRow
    {
        public string Header { get; init; } = "";
        public string Target { get; init; } = "";
    }

    #endregion
}
