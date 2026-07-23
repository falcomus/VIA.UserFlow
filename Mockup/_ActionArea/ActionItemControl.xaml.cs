using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Mockup.Actions;

[ObservableObject]
public partial class ActionItemControl : UserControl
{
    #region === DependencyProperty Row ===

    public static readonly DependencyProperty RowProperty =
        DependencyProperty.Register(
            nameof(Row),
            typeof(ActionRow),
            typeof(ActionItemControl),
            new FrameworkPropertyMetadata(null, OnRowChanged));

    public ActionRow Row
    {
        get => (ActionRow)GetValue(RowProperty);
        set => SetValue(RowProperty, value);
    }

    private static void OnRowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ActionItemControl ctrl)
            ctrl.UpdateFromRow();
    }

    #endregion

    #region === Observable Properties (vom Control selbst) ===

    [ObservableProperty]
    private ActionType actionType;

    partial void OnActionTypeChanged(ActionType value)
    {
        SetDisplayText();
        UpdateVisibilities();
        if (!_updatingFromRow && Row != null)
            Row.Type = value;
    }

    [ObservableProperty]
    private string displayText = "No target...";

    [ObservableProperty]
    private string trigger = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Screen> screens = null!;

    [ObservableProperty]
    private Screen selectedScreen = null!;

    partial void OnSelectedScreenChanged(Screen value)
    {
        SetDisplayText();
        if (!_updatingFromRow && Row != null)
            Row.TargetScreenId = value?.Id;
    }

    [ObservableProperty]
    private string filename = "Select filename...";

    partial void OnFilenameChanged(string value)
    {
        SetDisplayText();
        if (!_updatingFromRow && Row != null && ActionType == ActionType.OpenFile)
            Row.Path = value;
    }

    [ObservableProperty]
    private string url = "Enter URL...";

    partial void OnUrlChanged(string value)
    {
        SetDisplayText();
        if (!_updatingFromRow && Row != null && ActionType == ActionType.OpenURL)
            Row.Url = value;
    }

    [ObservableProperty]
    private ObservableCollection<ScreenPopup> popups = null!;

    [ObservableProperty]
    private ScreenPopup selectedPopup = null!;

    partial void OnSelectedPopupChanged(ScreenPopup value)
    {
        SetDisplayText();
        if (!_updatingFromRow && Row != null && ActionType == ActionType.ShowPopup)
            Row.PopupId = value?.Id;
    }

    // --- Visibility Steuerung (statt DataTrigger) ---
    [ObservableProperty]
    private Visibility screenListVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility popupListVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility filePickerVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility urlPickerVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility dropDownTargetVisibility = Visibility.Visible;

    [ObservableProperty]
    private Visibility fileTargetVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility urlTargetVisibility = Visibility.Collapsed;

    #endregion

    #region === Ctor & Loaded ===

    public ActionItemControl()
    {
        InitializeComponent();

        DataContext = this;

        Loaded += ActionItemControl_Loaded;
    }

    private void ActionItemControl_Loaded(object sender, RoutedEventArgs e)
    {
        Screens = new ObservableCollection<Screen>(
            MockupService.Mockup.CurrentProject.Screens.OrderBy(x => x.Name));
        Popups = new ObservableCollection<ScreenPopup>(
            MockupService.Mockup.CurrentProject.Popups.OrderBy(x => x.Name));

        UpdateFromRow(); // nachträglich Row auswerten, falls schon gesetzt
    }

    #endregion

    #region === Row Synchronisation ===

    private bool _updatingFromRow;

    private void UpdateFromRow()
    {
        if (Row == null || _updatingFromRow) return;

        try
        {
            _updatingFromRow = true;

            Trigger = Row.Trigger.ToString().ToUpperInvariant();
            ActionType = Row.Type;

            // Navigate – Screen
            if (Row.TargetScreenId.HasValue && Screens != null)
            {
                SelectedScreen = Screens.FirstOrDefault(s => s.Id == Row.TargetScreenId.Value) ?? null!;
            }
            else
            {
                SelectedScreen = null;
            }

            // ShowPopup – Popup
            if (Row.PopupId.HasValue && Popups != null)
            {
                SelectedPopup = Popups.FirstOrDefault(p => p.Id == Row.PopupId.Value) ?? null!;
            }
            else
            {
                SelectedPopup = null;
            }

            Filename = Row.Path ?? "Select filename...";
            Url = Row.Url ?? "Enter URL...";
        }
        finally
        {
            _updatingFromRow = false;
        }
    }

    private void SetDisplayText()
    {
        switch (ActionType)
        {
            case ActionType.None:
                DisplayText = "No target";
                break;
            case ActionType.Navigate:
                DisplayText = SelectedScreen == null ? "Select screen..." : SelectedScreen.Name;
                break;
            case ActionType.OpenFile:
                DisplayText = string.IsNullOrWhiteSpace(Filename) ? "Select file..." : Filename;
                break;

            case ActionType.OpenURL:
                DisplayText = string.IsNullOrWhiteSpace(Url) ? "Enter URL..." : Url;
                break;
            case ActionType.ShowPopup:
                DisplayText = SelectedPopup == null ? "Select popup..." : SelectedPopup.Name;
                break;
        }

        // Hintergrundfarbe der ComboBox (optional)
        if (PART_ActionCombo != null)
        {
            PART_ActionCombo.Background = new SolidColorBrush
            {
                Color = ActionType == ActionType.None ? Colors.White : Color.FromRgb(180, 220, 255)
            };
        }
    }

    private void UpdateVisibilities()
    {
        // Listen im DropDown
        ScreenListVisibility = ActionType == ActionType.Navigate ? Visibility.Visible : Visibility.Collapsed;
        PopupListVisibility = ActionType == ActionType.ShowPopup ? Visibility.Visible : Visibility.Collapsed;

        // Neuer Target-Host:
        DropDownTargetVisibility =
            (ActionType == ActionType.Navigate || ActionType == ActionType.ShowPopup)
                ? Visibility.Visible
                : Visibility.Collapsed;

        FileTargetVisibility =
            (ActionType == ActionType.OpenFile)
                ? Visibility.Visible
                : Visibility.Collapsed;

        UrlTargetVisibility =
            (ActionType == ActionType.OpenURL)
                ? Visibility.Visible
                : Visibility.Collapsed;

        // Alte DropDown-Picker (falls du sie drin lässt):
        FilePickerVisibility = Visibility.Collapsed;
        UrlPickerVisibility = Visibility.Collapsed;
    }

    #endregion

    #region === UI Helpers (unverändert) ===

    private void ListBox_MouseUp(object sender, MouseButtonEventArgs e)
        => PART_TargetButton.IsDropDownOpen = false;

    private void PART_ListBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not ListBox lb || lb.SelectedItem == null)
            return;

        lb.Dispatcher.BeginInvoke(new Action(() =>
        {
            CenterSelectedItem(lb);
            lb.Focus();
        }), DispatcherPriority.Loaded);
    }

    private static void CenterSelectedItem(ListBox lb)
    {
        if (lb.SelectedItem == null) return;

        lb.ScrollIntoView(lb.SelectedItem);
        lb.UpdateLayout();

        var item = (ListBoxItem?)lb.ItemContainerGenerator.ContainerFromItem(lb.SelectedItem);
        if (item == null)
        {
            lb.Dispatcher.BeginInvoke(new Action(() => CenterSelectedItem(lb)), DispatcherPriority.Background);
            return;
        }

        var sv = FindVisualChild<ScrollViewer>(lb);
        if (sv == null) return;

        var presenter = FindVisualChild<ScrollContentPresenter>(sv);
        if (presenter == null) return;

        var p = item.TransformToAncestor(presenter).Transform(new Point(0, 0));
        var itemCenterY = p.Y + item.ActualHeight / 2.0;

        var target = sv.VerticalOffset + (itemCenterY - sv.ViewportHeight / 2.0);
        target = Math.Max(0, Math.Min(target, sv.ExtentHeight - sv.ViewportHeight));

        sv.ScrollToVerticalOffset(target);

        lb.Dispatcher.BeginInvoke(new Action(() =>
        {
            lb.UpdateLayout();
            var p2 = item.TransformToAncestor(presenter).Transform(new Point(0, 0));
            var itemCenterY2 = p2.Y + item.ActualHeight / 2.0;
            var target2 = sv.VerticalOffset + (itemCenterY2 - sv.ViewportHeight / 2.0);
            target2 = Math.Max(0, Math.Min(target2, sv.ExtentHeight - sv.ViewportHeight));
            sv.ScrollToVerticalOffset(target2);
        }), DispatcherPriority.Background);
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t) return t;
            var found = FindVisualChild<T>(child);
            if (found != null) return found;
        }
        return null;
    }

    private void BrowseFile_Click(object sender, RoutedEventArgs e)
    {
        // Optional: nur wenn ActionType passt
        if (ActionType != ActionType.OpenFile)
            return;

        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select file",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dlg.ShowDialog() == true)
        {
            Filename = dlg.FileName; // triggert OnFilenameChanged -> Row.Path
            SetDisplayText();
        }
    }

    #endregion
}