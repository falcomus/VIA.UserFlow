// ======================================================================================
// DATEI: Mockup.ViewModel/MockupViewModel.Grouping.cs
// ======================================================================================
// Diese Datei enthält die Gruppierungs- und Filterlogik für Screens, Templates,
// Popups und Controls. Sie ist als partielle Klasse von MockupViewModel implementiert.
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.Grouping;
using Mockup.Registry;
using Mockup.Resources;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace Mockup.ViewModel;

public partial class MockupViewModel : ObservableObject
{
    #region === INITIALISIERUNG ===

    partial void InitGrouping()
    {
        // Globale Templates-Collection initialisieren
        Templates = [];

        // Gruppierte Ansicht für Templates konfigurieren
        SetupTemplatesGroupedView();

        // Toolbox searches and Template groups
        InitToolboxFiltering();

        // DesignControl Gruppen neu aufbauen
        RebuildControlGroups();
    }

    #endregion


    #region === STANDARD-GRUPPENNAMEN ===

    public static string DEFAULT_SCREEN_GROUPNAME = "HOME";
    public static string DEFAULT_TEMPLATE_GROUPNAME = "General";
    public static string DEFAULT_POPUP_GROUPNAME = "General";

    #endregion


    #region === SCREEN GRUPPIERUNG UND FILTER (Screens) ===

    /// <summary>
    /// Liste der vorhandenen Gruppennamen für Screens (ohne "ALL").
    /// Dient als Grundlage für die Filter-Gruppenliste.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> screenGroupNames = [];

    /// <summary>
    /// Speichert den Expanded-Zustand jeder Screen-Gruppe (Dictionary).
    /// </summary>
    private readonly Dictionary<string, bool> _screenGroupExpanded = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gibt zurück, ob die angegebene Screen-Gruppe expandiert ist.
    /// </summary>
    /// <param name="key">Name der Gruppe.</param>
    /// <returns>True wenn expandiert, sonst false (Standard: true).</returns>
    public bool GetScreenGroupExpanded(string key) => _screenGroupExpanded.TryGetValue(key, out var v) ? v : true;

    /// <summary>
    /// Setzt den Expanded-Zustand einer Screen-Gruppe.
    /// </summary>
    /// <param name="key">Name der Gruppe.</param>
    /// <param name="expanded">Neuer Zustand.</param>
    public void SetScreenGroupExpanded(string key, bool expanded) => _screenGroupExpanded[key] = expanded;

    /// <summary>
    /// Aktualisiert die Liste der Screen-Gruppennamen anhand der aktuell geladenen Screens.
    /// Sorgt dafür, dass die Gruppe "HOME" immer an erster Stelle steht.
    /// </summary>
    private void UpdateScreenGroupNames()
    {
        var newNames = new List<string>();

        if (CurrentProject != null)
        {
            var names = CurrentProject.Screens
                .Select(s => s.GroupName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // "HOME" immer an die erste Stelle, falls vorhanden
            if (names.Contains(DEFAULT_SCREEN_GROUPNAME, StringComparer.OrdinalIgnoreCase))
            {
                newNames.Add(DEFAULT_SCREEN_GROUPNAME);
                newNames.AddRange(names.Where(n => !string.Equals(n, DEFAULT_SCREEN_GROUPNAME, StringComparison.OrdinalIgnoreCase)));
            }
            else
            {
                newNames = names;
            }
        }

        ReplaceCollection(ScreenGroupNames, newNames);
        // Nach Aktualisierung der Gruppennamen auch die Filter-Liste neu aufbauen
        UpdateScreenFilterGroupNames();
        RebuildScreenNavigationGroups();
    }

    /// <summary>
    /// Categories displayed in the left master column of the Screen view.
    /// A persisted key is never used directly as UI text.
    /// </summary>
    public ObservableCollection<ScreenNavigationGroup> ScreenNavigationGroups { get; } = [];

    [ObservableProperty]
    private ScreenNavigationGroup? currentScreenNavigationGroup;

    partial void OnCurrentScreenNavigationGroupChanged(ScreenNavigationGroup? value)
    {
        if (ScreensNavigationView == null)
            return;

        ScreensNavigationView.Filter = value == null || value.IsAll
            ? null
            : item => item is Screen screen
                && string.Equals(screen.GroupName?.Trim(), value.Key, StringComparison.OrdinalIgnoreCase);

        ScreensNavigationView.Refresh();

        if (CurrentScreen != null && ScreensNavigationView.Cast<Screen>().Contains(CurrentScreen))
            return;

        CurrentScreen = ScreensNavigationView.Cast<Screen>().FirstOrDefault();
    }

    private void RebuildScreenNavigationGroups()
    {
        string? selectedKey = CurrentScreenNavigationGroup?.Key;
        var projectScreens = CurrentProject?.Screens ?? new ObservableCollection<Screen>();
        var allDisplayName = UserFlowResources.ResourceManager.GetString(
            "Screen.All",
            CultureInfo.CurrentUICulture) ?? "All";

        var groups = projectScreens
            .Where(screen => !string.IsNullOrWhiteSpace(screen.GroupName))
            .GroupBy(screen => screen.GroupName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new ScreenNavigationGroup(
                group.Key,
                GetScreenNavigationDisplayName(group.Key),
                group.Count()))
            .OrderBy(group => group.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var allGroup = new ScreenNavigationGroup("ALL", allDisplayName, projectScreens.Count, true);
        ReplaceCollection(ScreenNavigationGroups, new[] { allGroup }.Concat(groups));

        CurrentScreenNavigationGroup = !string.IsNullOrWhiteSpace(selectedKey)
            ? ScreenNavigationGroups.FirstOrDefault(group =>
                string.Equals(group.Key, selectedKey, StringComparison.OrdinalIgnoreCase))
            : null;
        CurrentScreenNavigationGroup ??= allGroup;
    }

    private static string GetScreenNavigationDisplayName(string key)
    {
        var displayName = key.Trim();

        // Persisted project data must never leak a namespace, a fully qualified type name,
        // or a default object representation into the navigation UI.
        if (displayName.Contains('.')
            && displayName.Split('.').All(segment =>
                !string.IsNullOrWhiteSpace(segment)
                && segment.All(character => char.IsLetterOrDigit(character) || character == '_')))
        {
            displayName = displayName[(displayName.LastIndexOf('.') + 1)..];
        }

        displayName = displayName.Replace('_', ' ').Trim();
        return string.IsNullOrWhiteSpace(displayName) ? "General" : displayName;
    }

    /// <summary>
    /// Liste der Gruppennamen für den Filter (inkl. "ALL" an erster Stelle).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> screenFilterGroupNames = [];

    /// <summary>
    /// Aktuell ausgewählter Gruppenname im Filter (inkl. "ALL").
    /// </summary>
    [ObservableProperty]
    private string? currentScreenFilterGroupName;

    /// <summary>
    /// Wird bei Änderung des ausgewählten Filters aufgerufen.
    /// Setzt den Filter auf <see cref="ScreensGroupedView"/> entsprechend.
    /// </summary>
    partial void OnCurrentScreenFilterGroupNameChanged(string? value)
    {
        if (ScreensGroupedView == null)
            return;

        Mouse.OverrideCursor = Cursors.Wait;

        // Filter sofort setzen (billige Operation)
        if (string.IsNullOrEmpty(value) || value == "ALL")
        {
            ScreensGroupedView.Filter = null;
            CurrentScreen = CurrentProject.Screens.FirstOrDefault();
        }
        else
        {
            ScreensGroupedView.Filter = item =>
            {
                if (item is Screen screen)
                    return string.Equals(screen.GroupName, value, StringComparison.OrdinalIgnoreCase);
                return false;
            };
            CurrentScreen = CurrentProject.Screens.FirstOrDefault(x => x.GroupName == value);
        }


        // Refresh asynchron über den UI-Dispatcher ausführen
        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            ScreensGroupedView.Refresh();
            Mouse.OverrideCursor = null;
        }), DispatcherPriority.Background);
    }

    /// <summary>
    /// Baut die Filter-Gruppenliste aus den vorhandenen Screen-Gruppennamen auf.
    /// "ALL" wird immer als erster Eintrag eingefügt.
    /// </summary>
    private void UpdateScreenFilterGroupNames()
    {
        var list = new List<string> { "ALL" };
        list.AddRange(ScreenGroupNames);
        ReplaceCollection(ScreenFilterGroupNames, list);

        // Standardmäßig "ALL" auswählen (falls noch nichts ausgewählt)
        if (CurrentScreenFilterGroupName == null || !ScreenFilterGroupNames.Contains(CurrentScreenFilterGroupName))
            CurrentScreenFilterGroupName = "ALL";
    }

    #endregion


    #region === TEMPLATE-GRUPPIERUNG ===

    /// <summary>
    /// Liste der vorhandenen Gruppennamen für Templates.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> templateGroupNames = [];

    /// <summary>
    /// Dictionary für den Expanded-Zustand der Template-Gruppen.
    /// </summary>
    private readonly Dictionary<string, bool> _templateGroupExpanded = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gibt zurück, ob die angegebene Template-Gruppe expandiert ist.
    /// </summary>
    public bool GetTemplateGroupExpanded(string key) => _templateGroupExpanded.TryGetValue(key, out var v) ? v : true;

    /// <summary>
    /// Setzt den Expanded-Zustand einer Template-Gruppe.
    /// </summary>
    public void SetTemplateGroupExpanded(string key, bool expanded) => _templateGroupExpanded[key] = expanded;

    /// <summary>
    /// Aktualisiert die Liste der Template-Gruppennamen basierend auf der Templates-Collection.
    /// </summary>
    private void UpdateTemplateGroupNames()
    {
        var names = Templates
            .Select(t => t.GroupName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        ReplaceCollection(TemplateGroupNames, names);
    }

    /// <summary>
    /// Stellt sicher, dass ein bestimmter Gruppenname in der Template-Picklist existiert.
    /// Wird verwendet, wenn ein neues Template mit einer neuen Gruppe erstellt wird.
    /// </summary>
    /// <param name="groupName">Name der Gruppe.</param>
    public void EnsureTemplateGroupExists(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            return;

        if (!TemplateGroupNames.Contains(groupName, StringComparer.OrdinalIgnoreCase))
            TemplateGroupNames.Add(groupName);
    }

    #endregion


    #region === POPUP-GRUPPIERUNG ===

    /// <summary>
    /// Liste der vorhandenen Gruppennamen für Popups.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> popupGroupNames = [];

    /// <summary>
    /// Dictionary für den Expanded-Zustand der Popup-Gruppen.
    /// </summary>
    private readonly Dictionary<string, bool> _popupGroupExpanded = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gibt zurück, ob die angegebene Popup-Gruppe expandiert ist.
    /// </summary>
    public bool GetPopupGroupExpanded(string key) => _popupGroupExpanded.TryGetValue(key, out var v) ? v : true;

    /// <summary>
    /// Setzt den Expanded-Zustand einer Popup-Gruppe.
    /// </summary>
    public void SetPopupGroupExpanded(string key, bool expanded) => _popupGroupExpanded[key] = expanded;

    /// <summary>
    /// Aktualisiert die Liste der Popup-Gruppennamen basierend auf den Popups des aktuellen Projekts.
    /// </summary>
    private void UpdatePopupGroupNames()
    {
        var names = new List<string>();

        if (CurrentProject != null)
        {
            names = CurrentProject.Popups
                .Select(p => p.GroupName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        ReplaceCollection(PopupGroupNames, names);
    }

    #endregion


    #region === STEUERELEMENT-GRUPPIERUNG (Controls) ===

    private static readonly IReadOnlyDictionary<string, int> _controlGroupOrder =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Actions"] = 0,
            ["Buttons"] = 1,
            ["Icon Buttons"] = 2,
            ["Social Login"] = 3,
            ["Input Fields"] = 4,
            ["Selection"] = 5,
            ["Pickers & Sliders"] = 6,
            ["Content"] = 7,
            ["Indicators"] = 8,
            ["Charts"] = 9,
            ["Navigation"] = 10,
            ["Layout"] = 11,
        };

    /// <summary>
    /// Sammlung von Steuerelement-Gruppen, die in der Toolbox angezeigt werden.
    /// </summary>
    public ObservableCollection<DesignControlGroup> ControlGroups { get; } = [];

    /// <summary>
    /// Aktuell ausgewählte Steuerelement-Gruppe.
    /// </summary>
    [ObservableProperty]
    private DesignControlGroup? currentControlGroup;

    /// <summary>
    /// Aktuell ausgewählter Steuerelement-Deskriptor.
    /// </summary>
    [ObservableProperty]
    private ControlDescriptor? currentControlDescriptor;

    /// <summary>
    /// Gruppierte Ansicht aller Steuerelemente für die Toolbox.
    /// </summary>
    public ListCollectionView? ControlGroupsCV { get; private set; }

    /// <summary>
    /// Baut die Steuerelement-Gruppen aus dem globalen ControlRegistry neu auf.
    /// Wird beim Start und nach Änderungen an der Registry aufgerufen.
    /// </summary>
    //public void RebuildControlGroups()
    //{
    //    var prev = CurrentControlGroup;

    //    // Gruppen aus allen Deskriptoren bilden, die eine Group-Angabe haben
    //    var groups = ControlRegistry.AllDescriptors
    //        .Where(d => !string.IsNullOrWhiteSpace(d.Group))
    //        .GroupBy(d => d.Group!.Trim())
    //        .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
    //        .Select(g => new DesignControlGroup(g.Key)
    //        {
    //            Controls = new ObservableCollection<ControlDescriptor>(
    //                g.OrderBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase))
    //        })
    //        .ToList();

    //    // ControlGroups ersetzen (nur Gruppen mit mindestens einem Control)
    //    ReplaceCollection(
    //        ControlGroups,
    //        groups.Where(x =>
    //            !string.IsNullOrWhiteSpace(x.GroupName) &&
    //            x.Controls.Any()));

    // Flache Liste aller Steuerelemente für alternative Ansichten
    //var flat = ControlRegistry.AllDescriptors
    //    .Where(x => !string.IsNullOrWhiteSpace(x.Group))
    //    .OrderBy(d => d.Group, StringComparer.OrdinalIgnoreCase)
    //    .ThenBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase)
    //    .ToList();

    //    ReplaceCollection(AllControls, flat);

    //    // Gruppierte CollectionView für die UI erstellen
    //    var view = (ListCollectionView)CollectionViewSource.GetDefaultView(AllControls);

    //    view.GroupDescriptions.Clear();
    //    view.SortDescriptions.Clear();

    //    view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ControlDescriptor.Group)));
    //    view.SortDescriptions.Add(new SortDescription(nameof(ControlDescriptor.DisplayName), ListSortDirection.Ascending));

    //    view.IsLiveGrouping = true;
    //    view.IsLiveSorting = true;

    //    ControlGroupsCV = view;

    //    // Vorherige Auswahl wiederherstellen, falls möglich
    //    if (prev != null)
    //        CurrentControlGroup = ControlGroups.FirstOrDefault(x =>
    //            string.Equals(x.GroupName, prev.GroupName, StringComparison.OrdinalIgnoreCase));

    //    CurrentControlGroup ??= ControlGroups.FirstOrDefault();

    //    OnPropertyChanged(nameof(ControlGroupsCV));
    //}

    public void RebuildControlGroups()
    {
        string? previousGroupName = CurrentControlGroup?.GroupName;

        var flat = ControlRegistry.AllDescriptors
            .Where(descriptor =>
                !string.IsNullOrWhiteSpace(descriptor.Group))
            .OrderBy(
                descriptor => descriptor.DisplayName,
                StringComparer.OrdinalIgnoreCase)
            .ToList();

        var alphabeticalGroups = flat
            .GroupBy(
                descriptor => descriptor.Group.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(
                group => group.Key,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => new DesignControlGroup(group.Key)
            {
                Controls = new ObservableCollection<ControlDescriptor>(
                    group.OrderBy(
                        descriptor => descriptor.DisplayName,
                        StringComparer.OrdinalIgnoreCase))
            })
            .Where(group =>
                !string.IsNullOrWhiteSpace(group.GroupName) &&
                group.Controls.Any())
            .ToList();

        var allGroup = new DesignControlGroup("All")
        {
            Controls =
                new ObservableCollection<ControlDescriptor>(flat)
        };

        ReplaceCollection(
            ControlGroups,
            new[] { allGroup }.Concat(alphabeticalGroups));

        ReplaceCollection(AllControls, flat);

        var view =
            (ListCollectionView)CollectionViewSource.GetDefaultView(
                AllControls);

        view.GroupDescriptions.Clear();
        view.SortDescriptions.Clear();

        view.GroupDescriptions.Add(
            new PropertyGroupDescription(
                nameof(ControlDescriptor.Group)));

        view.SortDescriptions.Add(
            new SortDescription(
                nameof(ControlDescriptor.Group),
                ListSortDirection.Ascending));

        view.SortDescriptions.Add(
            new SortDescription(
                nameof(ControlDescriptor.DisplayName),
                ListSortDirection.Ascending));

        view.IsLiveGrouping = true;
        view.IsLiveSorting = true;

        ControlGroupsCV = view;

        CurrentControlGroup =
            !string.IsNullOrWhiteSpace(previousGroupName)
                ? ControlGroups.FirstOrDefault(group =>
                    string.Equals(
                        group.GroupName,
                        previousGroupName,
                        StringComparison.OrdinalIgnoreCase))
                : null;

        CurrentControlGroup ??= allGroup;

        RefreshControlToolboxItems();

        OnPropertyChanged(nameof(ControlGroupsCV));
    }

    private static int GetControlGroupOrder(string? groupName)
    {
        if (!string.IsNullOrWhiteSpace(groupName)
            && _controlGroupOrder.TryGetValue(groupName.Trim(), out int order))
        {
            return order;
        }

        return int.MaxValue;
    }

    #endregion


    #region === HILFSMETHODEN ===

    /// <summary>
    /// Ersetzt den gesamten Inhalt einer ObservableCollection durch die Elemente einer Quell-Enumerable.
    /// </summary>
    /// <typeparam name="T">Typ der Elemente.</typeparam>
    /// <param name="target">Zu aktualisierende ObservableCollection.</param>
    /// <param name="source">Quelle der neuen Elemente.</param>
    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
            target.Add(item);
    }

    /// <summary>
    /// Aktualisiert die von der UI verwendeten Designer-Höhen-Informationen.
    /// Wird aufgerufen, wenn sich die Höhe des aktuellen Screens, Templates oder Popups ändert.
    /// </summary>
    private void UpdateDesignerHeights()
    {
        OnPropertyChanged(nameof(TemplateDesignerHeight));
        OnPropertyChanged(nameof(PopupDesignerHeight));
        OnPropertyChanged(nameof(ScreenSizeInfo));
        OnPropertyChanged(nameof(TemplateSizeInfo));
        OnPropertyChanged(nameof(PopupSizeInfo));
    }

    #endregion
}
