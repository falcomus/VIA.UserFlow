//MockupViewModel.Settings.cs
using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.Services;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Mockup.ViewModel;

//Was man künftig tun muss, wenn neue Settings dazukommen:

//Immer diese 3 Stellen anpassen:

//AppSettings(DTO) erweitern
//Beispiel: public bool Foo { get; set; } = true;

//Mapping ergänzen:
//ApplySettingsToViewModel() (Settings -> VM)
//ApplyViewModelToSettings() (VM -> Settings)

//Defaults:
//Default-Wert im DTO setzen(oder in CreateDefault())

//##### SETTINGS #####

public partial class MockupViewModel : ObservableObject
{
    #region === INITIALISIERUNG ===

    partial void InitSettings()
    {
        // VM-Defaults (nur wenn leer)
        if (RecentColors.Count == 0)
        {
            for (int i = 0; i < 15; i++)
                RecentColors.Add(RECENT_COLOR_EMPTY);
        }
    }

    #endregion

    #region === SELECTED INDICES ===

    //SelectedIndex of MainTabControl in MainWindow
    [ObservableProperty]
    private int _mainTabSelectedIndex = 0;

    partial void OnMainTabSelectedIndexChanged(int value)
    {
        //Deselect all controls when leaving the "Design" tab
        SelectedControls.ToList().ForEach(c => c.IsSelected = false);
        SelectedControls.Clear();
        CurrentControl = null;

        //Close all open ComboBoxes to prevent them from staying open when switching tabs
        if (CurrentProject?.Screens != null)
        {
            foreach (var screen in CurrentProject.Screens)
            {
                foreach (var band in screen.Bands)
                {
                    foreach (var page in band.Pages)
                    {
                        foreach (var comboBox in page.Controls.OfType<Mockup.Controls.ComboBox>())
                        {
                            comboBox.IsDropDownOpen = false;
                        }
                    }
                }
            }
        }

        ShowHamburgerButton = value == 1 || value == 2 || value == 3;

        OnPropertyChanged(nameof(ShowHamburgerButton));

        //Wenn erste Seite verlassen wird -> Filterung wieder aus "All"
        if (value > 0)
        {
            CurrentScreenFilterGroupName = ScreenFilterGroupNames.FirstOrDefault();
        }

        if (value == 4 && PreviewScreen == null)
        {
            PreviewScreen = CurrentProject?.Screens.FirstOrDefault(x => x.IsHomeScreen);
        }

        OnPropertyChanged(nameof(IsScreenDesigner));
        OnPropertyChanged(nameof(IsTemplateDesigner));
        OnPropertyChanged(nameof(IsPopupDesigner));
        OnPropertyChanged(nameof(IsTemplateTabVisible));

        NotifyUndoRedoCommandsChanged();
    }


    //SelectedIndex of Screen Tab (ScreenView)
    [ObservableProperty]
    private int _screenTabSelectedIndex = 0;

    #endregion === SELECTED INDICES ===

    #region === UI VISIBILITY FLAGS ===

    [ObservableProperty]
    private bool _showHamburgerButton;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToolboxToggleText))]
    [NotifyPropertyChangedFor(nameof(ToolboxIcon))]
    private bool _showToolbox = true;

    partial void OnShowToolboxChanged(bool value)
    {
        ToolboxToggleText = value ? "Hide Toolbox" : "Show Toolbox";
        ToolboxIcon = value ? "EyeSlash" : "Eye";
        if (value)
        {
            XNotifications.Info("Not yet implemented");
        }
    }

    [ObservableProperty]
    public string _toolboxToggleText = "Show Toolbox";

    [ObservableProperty]
    public string _toolboxIcon = "Eye";

    #endregion === UI VISIBILITY FLAGS ===

    #region === COLOR PICKER RECENTS & METHODS ===

    [ObservableProperty]
    private ObservableCollection<string> recentColors = [];

    private void AddRecentColorIfMissing(Color color)
    {
        var hex = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

        // ignore empty
        if (string.Equals(hex, RECENT_COLOR_EMPTY, StringComparison.OrdinalIgnoreCase))
            return;

        // already present?
        if (RecentColors.Any(x => string.Equals(x, hex, StringComparison.OrdinalIgnoreCase)))
            return;

        RecentColors.Insert(0, hex);

        while (RecentColors.Count > RECENT_COLORS_COUNT)
            RecentColors.RemoveAt(RecentColors.Count - 1);

        while (RecentColors.Count < RECENT_COLORS_COUNT)
            RecentColors.Add(RECENT_COLOR_EMPTY);

        SaveSettings();
    }

    #endregion === COLOR PICKER RECENTS & METHODS ===
}
