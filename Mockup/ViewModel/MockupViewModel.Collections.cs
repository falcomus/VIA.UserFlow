// ======================================================================================
// DATEI: Mockup.ViewModel/MockupViewModel.Collections.cs
// ======================================================================================
// Diese Datei enthält die Sammlungen und gruppierten Ansichten für Projekte, Screens,
// Templates, Popups und Steuerelemente. Sie ist als partielle Klasse von
// MockupViewModel implementiert.
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mockup.Registry;
using Mockup.Snapshots;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Mockup.ViewModel;

public partial class MockupViewModel : ObservableObject
{
    #region === PROJEKT - COLLECTIONS & EIGENSCHAFTEN ===

    /// <summary>
    /// Liste aller geöffneten/verfügbaren Projekte (wird aktuell nur für die Projektverwaltung verwendet).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Project> projects = new();

    /// <summary>
    /// Das aktuell geladene und bearbeitete Projekt.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DeviceHelpLineWidth))]
    [NotifyPropertyChangedFor(nameof(DeviceSizeInfo))]
    private Project? currentProject = null;

    [ObservableProperty]
    private ObservableCollection<ProjectFileEntry> projectFiles = new();

    public ICollectionView? ProjectFilesView { get; private set; }

    [ObservableProperty]
    private ProjectFileEntry? currentProjectFile;

    [ObservableProperty]
    private ProjectBrowserSortMode projectBrowserSortMode = ProjectBrowserSortMode.LastOpenedDesc;

    partial void OnProjectBrowserSortModeChanged(ProjectBrowserSortMode value)
    {
        ApplyProjectFilesSort();
    }

    /// <summary>
    /// Wird aufgerufen, wenn sich das aktuelle Projekt ändert.
    /// Richtet die gruppierten Ansichten für Screens und Popups neu ein,
    /// setzt die aktuelle Auswahl zurück und aktualisiert die UI.
    /// </summary>
    partial void OnCurrentProjectChanged(Project? value)
    {
        SetupScreensGroupedView();
        SetupPopupsGroupedView();

        CurrentScreenFilterGroupName = "ALL";
        CurrentScreen = value?.Screens.FirstOrDefault(x => x.IsHomeScreen);
        HomeScreen = CurrentScreen;

        EnsurePreviewTrailInitialized();
        OnPropertyChanged(nameof(NavigationTrail));
        OnPropertyChanged(nameof(PreviewScreen));

        CurrentPopup = value?.Popups.FirstOrDefault();

        ClosePreviewPopup();

        UpdateScreenGroupNames();
        UpdatePopupGroupNames();

        OnPropertyChanged(nameof(ScreensGroupedView));
        OnPropertyChanged(nameof(ScreensNavigationView));
        OnPropertyChanged(nameof(PopupsGroupedView));

        OnPropertyChanged(nameof(PreviewPopupWidth));
        OnPropertyChanged(nameof(PreviewPopupHeight));

        OnPropertyChanged(nameof(PreviewPopupViewportContentWidth));
        OnPropertyChanged(nameof(PreviewPopupViewportContentHeight));
        OnPropertyChanged(nameof(PreviewPopupRequestedContentHeight));
        OnPropertyChanged(nameof(PreviewPopupVerticalScrollBarVisibility));
        OnPropertyChanged(nameof(PreviewPopupHorizontalAlignment));
        OnPropertyChanged(nameof(PreviewPopupVerticalAlignment));
        OnPropertyChanged(nameof(PreviewPopupMargin));

        SyncCurrentProjectFileFromCurrentProject();
    }


    //XXX
    //public void RefreshProjectFilesBrowser()
    //{
    //    var knownLastOpened = RecentProjects
    //        .Where(x => !string.IsNullOrWhiteSpace(x.FilePath))
    //        .GroupBy(x => x.FilePath, StringComparer.OrdinalIgnoreCase)
    //        .ToDictionary(
    //            g => g.Key,
    //            g => g.Max(x => x.LastOpenedUtc),
    //            StringComparer.OrdinalIgnoreCase);

    //    var items = new List<ProjectFileEntry>();

    //    if (Directory.Exists(ProjectsFolder))
    //    {
    //        foreach (var file in Directory.EnumerateFiles(ProjectsFolder, "*.ufp", SearchOption.TopDirectoryOnly))
    //        {
    //            var fullPath = Path.GetFullPath(file);
    //            var fileInfo = new FileInfo(fullPath);
    //            var displayName = Path.GetFileNameWithoutExtension(fullPath);

    //            items.Add(new ProjectFileEntry
    //            {
    //                FullPath = fullPath,
    //                DisplayName = displayName,
    //                FileName = fileInfo.Name,
    //                LastWriteTime = fileInfo.LastWriteTime,
    //                LastOpenedUtc = knownLastOpened.TryGetValue(fullPath, out var lastOpened) ? lastOpened : null,
    //            });
    //        }
    //    }

    //    ProjectFiles = new ObservableCollection<ProjectFileEntry>(items);
    //    SetupProjectFilesView();
    //    SyncCurrentProjectFileFromCurrentProject();
    //}

    private void SetupProjectFilesView()
    {
        ProjectFilesView = CollectionViewSource.GetDefaultView(ProjectFiles);
        ApplyProjectFilesSort();
        OnPropertyChanged(nameof(ProjectFilesView));
    }

    private void ApplyProjectFilesSort()
    {
        if (ProjectFilesView == null)
            return;

        using (ProjectFilesView.DeferRefresh())
        {
            ProjectFilesView.SortDescriptions.Clear();

            switch (ProjectBrowserSortMode)
            {
                case ProjectBrowserSortMode.NameAsc:
                    ProjectFilesView.SortDescriptions.Add(new SortDescription(nameof(ProjectFileEntry.DisplayName), ListSortDirection.Ascending));
                    break;

                case ProjectBrowserSortMode.ModifiedDesc:
                    ProjectFilesView.SortDescriptions.Add(new SortDescription(nameof(ProjectFileEntry.LastWriteTime), ListSortDirection.Descending));
                    ProjectFilesView.SortDescriptions.Add(new SortDescription(nameof(ProjectFileEntry.DisplayName), ListSortDirection.Ascending));
                    break;

                default:
                    ProjectFilesView.SortDescriptions.Add(new SortDescription(nameof(ProjectFileEntry.LastOpenedSortValue), ListSortDirection.Descending));
                    ProjectFilesView.SortDescriptions.Add(new SortDescription(nameof(ProjectFileEntry.LastWriteTime), ListSortDirection.Descending));
                    ProjectFilesView.SortDescriptions.Add(new SortDescription(nameof(ProjectFileEntry.DisplayName), ListSortDirection.Ascending));
                    break;
            }
        }
    }

    private void SyncCurrentProjectFileFromCurrentProject()
    {
        if (CurrentProject == null || string.IsNullOrWhiteSpace(CurrentProject.FilePath))
        {
            CurrentProjectFile = null;
            return;
        }

        var normalized = Path.GetFullPath(CurrentProject.FilePath);
        CurrentProjectFile = ProjectFiles.FirstOrDefault(x => string.Equals(x.FullPath, normalized, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region === SCREENS ===

    /// <summary>
    /// Gruppierte Ansicht der Screens des aktuellen Projekts.
    /// Gruppiert nach <see cref="Screen.GroupName"/> und sortiert nach <see cref="Screen.GroupSortKey"/>.
    /// </summary>
    public ICollectionView? ScreensGroupedView { get; private set; }

    /// <summary>
    /// Flat, independently filtered screen view used exclusively by the Screen master-detail navigator.
    /// It intentionally does not share filters or grouping with <see cref="ScreensGroupedView"/>.
    /// </summary>
    public ICollectionView? ScreensNavigationView { get; private set; }

    /// <summary>
    /// Richtet die gruppierte Ansicht für Screens ein.
    /// Fügt GroupDescription und SortDescriptions hinzu und registriert Ereignishandler.
    /// </summary>
    private void SetupScreensGroupedView()
    {
        if (CurrentProject == null)
        {
            ScreensGroupedView = null;
            ScreensNavigationView = null;
            return;
        }

        ScreensGroupedView = CollectionViewSource.GetDefaultView(CurrentProject.Screens);
        ScreensGroupedView.GroupDescriptions.Clear();
        ScreensGroupedView.GroupDescriptions.Add(
            new PropertyGroupDescription(nameof(Screen.GroupName))
        );

        using (ScreensGroupedView.DeferRefresh())
        {
            ScreensGroupedView.SortDescriptions.Clear();
            ScreensGroupedView.SortDescriptions.Add(
                new SortDescription(nameof(Screen.GroupSortKey), ListSortDirection.Ascending)
            );
            ScreensGroupedView.SortDescriptions.Add(
                new SortDescription(nameof(Screen.Name), ListSortDirection.Ascending)
            );
        }

        CurrentProject.Screens.CollectionChanged += OnScreensCollectionChanged;
        foreach (var s in CurrentProject.Screens)
            s.PropertyChanged += OnScreenPropertyChanged;

        ScreensNavigationView = new ListCollectionView((IList)CurrentProject.Screens);
        using (ScreensNavigationView.DeferRefresh())
        {
            ScreensNavigationView.SortDescriptions.Clear();
            ScreensNavigationView.SortDescriptions.Add(
                new SortDescription(nameof(Screen.Name), ListSortDirection.Ascending));
        }
    }

    /// <summary>
    /// Wird aufgerufen, wenn sich die Screens-Collection ändert (Hinzufügen/Entfernen).
    /// Aktualisiert die Ereignisabhörer und die Gruppenliste.
    /// </summary>
    private void OnScreensCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (Screen s in e.OldItems)
                s.PropertyChanged -= OnScreenPropertyChanged;

        if (e.NewItems != null)
            foreach (Screen s in e.NewItems)
                s.PropertyChanged += OnScreenPropertyChanged;

        UpdateScreenGroupNames();
        ScreensGroupedView?.Refresh();
        ScreensNavigationView?.Refresh();
    }

    /// <summary>
    /// Wird aufgerufen, wenn sich eine Eigenschaft eines Screens ändert.
    /// Bei Änderung der GroupName wird die Gruppenliste und die Ansicht aktualisiert.
    /// </summary>
    private void OnScreenPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Screen.GroupName))
        {
            UpdateScreenGroupNames();
            ScreensGroupedView?.Refresh();
            ScreensNavigationView?.Refresh();
            return;
        }
    }

    /// <summary>
    /// Der aktuell ausgewählte Screen im Designer.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HomeScreen))]
    private Screen? currentScreen;

    partial void OnCurrentScreenChanged(Screen? value)
    {
        if (_isApplyingSnapshotRestore)
            return;

        //Alle Snapshots löschen
        SnapshotManager.Clear(SnapshotContext.Screen);
        NotifyUndoRedoCommandsChanged();
    }

    /// <summary>
    /// Der aktuell für die Vorschau ausgewählte Screen (kann vom Designer-Screen abweichen).
    /// </summary>
    [ObservableProperty]
    private Screen? homeScreen;

    partial void OnHomeScreenChanged(Screen? value)
    {
        EnsurePreviewTrailInitialized();
    }

    #endregion

    #region === TEMPLATES – global (projektübergreifend) ===

    /// <summary>
    /// Globale Sammlung aller verfügbaren ScreenTemplates.
    /// Wird in einer separaten templates.json gespeichert.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ScreenTemplate> templates = new();

    /// <summary>
    /// Gruppierte Ansicht der globalen Templates.
    /// Gruppiert nach <see cref="ScreenTemplate.GroupName"/>.
    /// </summary>
    public ICollectionView TemplatesGroupedView { get; private set; } = null!;

    /// <summary>
    /// Richtet die gruppierte Ansicht für Templates ein.
    /// </summary>
    private void SetupTemplatesGroupedView()
    {
        TemplatesGroupedView = CollectionViewSource.GetDefaultView(Templates);
        TemplatesGroupedView.GroupDescriptions.Clear();
        TemplatesGroupedView.GroupDescriptions.Add(
            new PropertyGroupDescription(nameof(ScreenTemplate.GroupName))
        );

        using (TemplatesGroupedView.DeferRefresh())
        {
            TemplatesGroupedView.SortDescriptions.Clear();
            TemplatesGroupedView.SortDescriptions.Add(
                new SortDescription(nameof(ScreenTemplate.GroupName), ListSortDirection.Ascending)
            );
            TemplatesGroupedView.SortDescriptions.Add(
                new SortDescription(nameof(ScreenTemplate.Name), ListSortDirection.Ascending)
            );
        }

        Templates.CollectionChanged += OnTemplatesCollectionChanged;
        foreach (var t in Templates)
            t.PropertyChanged += OnTemplatePropertyChanged;
    }

    /// <summary>
    /// Wird bei Änderungen an der Templates-Collection aufgerufen.
    /// Aktualisiert die Ereignisabhörer und die Gruppenliste.
    /// </summary>
    private void OnTemplatesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (ScreenTemplate t in e.OldItems)
                t.PropertyChanged -= OnTemplatePropertyChanged;

        if (e.NewItems != null)
            foreach (ScreenTemplate t in e.NewItems)
                t.PropertyChanged += OnTemplatePropertyChanged;

        UpdateTemplateGroupNames();
        TemplatesGroupedView.Refresh();
    }

    /// <summary>
    /// Wird bei Eigenschaftsänderungen eines Templates aufgerufen.
    /// Bei Änderung der GroupName wird die Gruppenliste und die Ansicht aktualisiert.
    /// </summary>
    private void OnTemplatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScreenTemplate.GroupName))
        {
            UpdateTemplateGroupNames();
            TemplatesGroupedView.Refresh();
        }
    }

    /// <summary>
    /// Das aktuell ausgewählte Template (für Bearbeitung oder Vorschau).
    /// </summary>
    [ObservableProperty]
    private ScreenTemplate? currentTemplate;

    partial void OnCurrentTemplateChanged(ScreenTemplate? value)
    {
        if (_isApplyingSnapshotRestore)
            return;

        //Alle Snapshots löschen
        SnapshotManager.Clear(SnapshotContext.Template);
        NotifyUndoRedoCommandsChanged();
    }

    #endregion

    #region === POPUPS – projektspezifisch ===

    /// <summary>
    /// Gruppierte Ansicht der Popups des aktuellen Projekts.
    /// Gruppiert nach <see cref="ScreenPopup.GroupName"/>.
    /// </summary>
    public ICollectionView? PopupsGroupedView { get; private set; }

    /// <summary>
    /// Richtet die gruppierte Ansicht für Popups ein.
    /// </summary>
    private void SetupPopupsGroupedView()
    {
        if (CurrentProject == null)
        {
            PopupsGroupedView = null;
            return;
        }

        PopupsGroupedView = CollectionViewSource.GetDefaultView(CurrentProject.Popups);
        PopupsGroupedView.GroupDescriptions.Clear();
        PopupsGroupedView.GroupDescriptions.Add(
            new PropertyGroupDescription(nameof(ScreenPopup.GroupName))
        );

        using (PopupsGroupedView.DeferRefresh())
        {
            PopupsGroupedView.SortDescriptions.Clear();
            PopupsGroupedView.SortDescriptions.Add(
                new SortDescription(nameof(ScreenPopup.GroupName), ListSortDirection.Ascending)
            );
            PopupsGroupedView.SortDescriptions.Add(
                new SortDescription(nameof(ScreenPopup.Name), ListSortDirection.Ascending)
            );
        }

        CurrentProject.Popups.CollectionChanged += OnPopupsCollectionChanged;
        foreach (var p in CurrentProject.Popups)
            p.PropertyChanged += OnPopupPropertyChanged;
    }

    /// <summary>
    /// Wird bei Änderungen an der Popups-Collection aufgerufen.
    /// </summary>
    private void OnPopupsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (ScreenPopup p in e.OldItems)
                p.PropertyChanged -= OnPopupPropertyChanged;

        if (e.NewItems != null)
            foreach (ScreenPopup p in e.NewItems)
                p.PropertyChanged += OnPopupPropertyChanged;

        UpdatePopupGroupNames();
        PopupsGroupedView?.Refresh();
    }

    /// <summary>
    /// Wird bei Eigenschaftsänderungen eines Popups aufgerufen.
    /// Bei Änderung der GroupName wird die Gruppenliste und die Ansicht aktualisiert.
    /// </summary>
    private void OnPopupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScreenPopup.GroupName))
        {
            UpdatePopupGroupNames();
            PopupsGroupedView?.Refresh();
        }
    }

    /// <summary>
    /// Das aktuell ausgewählte Popup (für Bearbeitung oder Vorschau).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PopupSizeInfo))]
    private ScreenPopup? currentPopup;


    /// <summary>
    /// Wird aufgerufen, wenn sich das aktuelle Popup ändert.
    /// Aktualisiert die Designer-Abmessungen und ruft <see cref="UpdateDesignerHeights"/> auf.
    /// </summary>
    partial void OnCurrentPopupChanged(ScreenPopup? value)
    {
        if (value != null)
        {
            PopupDesignerWidth = value.Width;
            PopupDesignerHeight = value.Height;
        }

        UpdateDesignerHeights();

        if (_isApplyingSnapshotRestore)
            return;

        //Alle Snapshots löschen
        SnapshotManager.Clear(SnapshotContext.Popup);
        NotifyUndoRedoCommandsChanged();
    }

    #endregion

    #region === CONTROLS ===

    /// <summary>
    /// Flache Liste aller verfügbaren Steuerelement-Deskriptoren (für Toolbox und andere Ansichten).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ControlDescriptor> allControls = [];


    /// <summary>
    /// Liste aller selektierten Controls.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DesignControl> selectedControls = [];

    /// <summary>
    /// Das aktuell ausgewählte Steuerelement im Designer (z. B. zum Bearbeiten von Eigenschaften).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMultipleControlsSelected))]
    private DesignControl? currentControl;

    public bool IsMultipleControlsSelected => SelectedControls != null && SelectedControls.Count > 1;

    #endregion

    #region === PREVIEW NAVIGATION (Breadcrumb / LiveView) ===

    [ObservableProperty]
    private ObservableCollection<Screen> navigationTrail = new();

    [ObservableProperty]
    private Screen? previewScreen;

    [RelayCommand]
    private void NavigateToTrailIndex(int index)
    {
        if (NavigationTrail == null || NavigationTrail.Count == 0)
            return;

        if (index < 0 || index >= NavigationTrail.Count)
            return;

        while (NavigationTrail.Count > index + 1)
            NavigationTrail.RemoveAt(NavigationTrail.Count - 1);

        PreviewScreen = NavigationTrail.LastOrDefault();
        ClosePreviewPopup();
    }

    public void PreviewNavigateTo(Screen? target)
    {
        if (target == null)
            return;

        if (NavigationTrail.Count == 0)
        {
            var home = HomeScreen ?? CurrentProject?.Screens.FirstOrDefault(s => s.IsHomeScreen);
            if (home != null)
                NavigationTrail.Add(home);
        }

        if (NavigationTrail.Count == 0 || NavigationTrail[^1].Id != target.Id)
            NavigationTrail.Add(target);

        PreviewScreen = target;
        ClosePreviewPopup();
    }

    public void PreviewNavigateHome()
    {
        var home = HomeScreen ?? CurrentProject?.Screens.FirstOrDefault(s => s.IsHomeScreen);
        if (home == null)
            return;

        NavigationTrail.Clear();
        NavigationTrail.Add(home);

        PreviewScreen = home;
        ClosePreviewPopup();
    }

    public void PreviewNavigateBack()
    {
        if (NavigationTrail.Count <= 1)
        {
            PreviewNavigateHome();
            return;
        }

        NavigationTrail.RemoveAt(NavigationTrail.Count - 1);
        PreviewScreen = NavigationTrail.LastOrDefault();
        ClosePreviewPopup();
    }

    private void EnsurePreviewTrailInitialized()
    {
        if (NavigationTrail.Count > 0 && PreviewScreen != null)
            return;

        var home = HomeScreen ?? CurrentProject?.Screens.FirstOrDefault(s => s.IsHomeScreen);
        if (home == null)
            return;

        NavigationTrail.Clear();
        NavigationTrail.Add(home);

        PreviewScreen = home;
    }

    #endregion

    #region === PREVIEW POPUP (LiveView Overlay) ===

    private const double PreviewPopupHorizontalInset = -1d;
    private const double PreviewPopupVerticalInset = 0d;
    private const double PreviewPopupOuterBorderThickness = 2d;
    private const double PreviewPopupScrollTolerance = 6d;
    private const double PreviewPopupCenterOffsetX = 0d;
    private const double PreviewPopupCenterOffsetY = -8d;

    [ObservableProperty]
    private bool isPreviewPopupOpen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewPopupTitle))]
    [NotifyPropertyChangedFor(nameof(PreviewPopupWidth))]
    [NotifyPropertyChangedFor(nameof(PreviewPopupHeight))]
    [NotifyPropertyChangedFor(nameof(PreviewPopupRequestedContentHeight))]
    [NotifyPropertyChangedFor(nameof(PreviewPopupViewportContentWidth))]
    [NotifyPropertyChangedFor(nameof(PreviewPopupVerticalScrollBarVisibility))]
    [NotifyPropertyChangedFor(nameof(PreviewPopupHorizontalAlignment))]
    [NotifyPropertyChangedFor(nameof(PreviewPopupVerticalAlignment))]
    [NotifyPropertyChangedFor(nameof(PreviewPopupMargin))]
    private ScreenPopup? previewPopup;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewPopupHorizontalAlignment))]
    [NotifyPropertyChangedFor(nameof(PreviewPopupVerticalAlignment))]
    [NotifyPropertyChangedFor(nameof(PreviewPopupMargin))]
    [NotifyPropertyChangedFor(nameof(PreviewPopupVerticalScrollBarVisibility))]
    private ScreenPopupPosition previewPopupPosition = ScreenPopupPosition.Center;

    public string PreviewPopupTitle =>
        string.IsNullOrWhiteSpace(PreviewPopup?.Name) ? "Popup" : PreviewPopup.Title;

    private double PreviewDeviceWidth => Math.Max(0d, CurrentProject?.DeviceWidth ?? 0d);

    private double PreviewDeviceHeight => Math.Max(0d, CurrentProject?.DeviceHeight ?? 0d);

    private double PreviewPopupRequestedWidth => Math.Max(0d, PreviewPopup?.Width ?? 0d);

    private double PreviewPopupRequestedHeight => Math.Max(0d, PreviewPopup?.Height ?? 0d);

    private double PreviewPopupHeaderHeight =>
        PreviewPopup?.HasHeader == true ? Math.Max(0d, PreviewPopup.HeaderHeight) : 0d;

    public double PreviewPopupWidth
    {
        get
        {
            var deviceWidth = PreviewDeviceWidth;
            if (deviceWidth <= 0d)
                return PreviewPopupRequestedWidth;

            var maxWidth = Math.Max(0d, deviceWidth - (PreviewPopupHorizontalInset * 2d));
            return Math.Min(PreviewPopupRequestedWidth, maxWidth);
        }
    }

    public double PreviewPopupHeight
    {
        get
        {
            var deviceHeight = PreviewDeviceHeight;
            if (deviceHeight <= 0d)
                return PreviewPopupRequestedHeight;

            var maxHeight = Math.Max(0d, deviceHeight - (PreviewPopupVerticalInset * 2d));
            return Math.Min(PreviewPopupRequestedHeight, maxHeight);
        }
    }


    public double PreviewPopupRequestedContentHeight =>
        Math.Max(0d, PreviewPopup?.ContentHeight ?? 0d);

    public double PreviewPopupViewportContentWidth
    {
        get
        {
            return Math.Max(0d, PreviewPopupWidth - PreviewPopupOuterBorderThickness);
        }
    }


    public ScrollBarVisibility PreviewPopupVerticalScrollBarVisibility
    {
        get
        {
            var requested = PreviewPopupRequestedContentHeight;
            var visible = Math.Max(
                0d,
                PreviewPopupHeight - PreviewPopupHeaderHeight - PreviewPopupOuterBorderThickness
            );

            return requested > visible + PreviewPopupScrollTolerance
                ? ScrollBarVisibility.Auto
                : ScrollBarVisibility.Disabled;
        }
    }

    public double PreviewPopupViewportContentHeight
    {
        get
        {
            var header = PreviewPopup?.HasHeader == true ? PreviewPopupHeaderHeight : 0d;
            return Math.Max(0d, PreviewPopupHeight - PreviewPopupOuterBorderThickness - header);
        }
    }

    public HorizontalAlignment PreviewPopupHorizontalAlignment =>
        PreviewPopupPosition switch
        {
            ScreenPopupPosition.Left => HorizontalAlignment.Left,
            ScreenPopupPosition.Right => HorizontalAlignment.Right,
            ScreenPopupPosition.Top => HorizontalAlignment.Center,
            ScreenPopupPosition.Bottom => HorizontalAlignment.Center,
            ScreenPopupPosition.MousePos => HorizontalAlignment.Center,
            _ => HorizontalAlignment.Center,
        };

    public VerticalAlignment PreviewPopupVerticalAlignment =>
        PreviewPopupPosition switch
        {
            ScreenPopupPosition.Top => VerticalAlignment.Top,
            ScreenPopupPosition.Bottom => VerticalAlignment.Bottom,
            ScreenPopupPosition.Left => VerticalAlignment.Center,
            ScreenPopupPosition.Right => VerticalAlignment.Center,
            ScreenPopupPosition.MousePos => VerticalAlignment.Center,
            _ => VerticalAlignment.Center,
        };

    public Thickness PreviewPopupMargin =>
        PreviewPopupPosition switch
        {
            ScreenPopupPosition.Left => new Thickness(PreviewPopupHorizontalInset, 0, 0, 0),
            ScreenPopupPosition.Right => new Thickness(0, 0, PreviewPopupHorizontalInset, 0),
            ScreenPopupPosition.Top => new Thickness(0, PreviewPopupVerticalInset, 0, 0),
            ScreenPopupPosition.Bottom => new Thickness(0, 0, 0, PreviewPopupVerticalInset),
            ScreenPopupPosition.MousePos => new Thickness(0),
            ScreenPopupPosition.Center => new Thickness(
                PreviewPopupCenterOffsetX,
                PreviewPopupCenterOffsetY,
                -PreviewPopupCenterOffsetX,
                -PreviewPopupCenterOffsetY),
            _ => new Thickness(0),
        };

    partial void OnPreviewPopupChanged(ScreenPopup? value)
    {
        OnPropertyChanged(nameof(PreviewPopupTitle));
        OnPropertyChanged(nameof(PreviewPopupWidth));
        OnPropertyChanged(nameof(PreviewPopupHeight));
        OnPropertyChanged(nameof(PreviewPopupRequestedContentHeight));
        OnPropertyChanged(nameof(PreviewPopupViewportContentWidth));
        OnPropertyChanged(nameof(PreviewPopupViewportContentHeight));
        OnPropertyChanged(nameof(PreviewPopupVerticalScrollBarVisibility));
        OnPropertyChanged(nameof(PreviewPopupHorizontalAlignment));
        OnPropertyChanged(nameof(PreviewPopupVerticalAlignment));
        OnPropertyChanged(nameof(PreviewPopupMargin));
    }

    partial void OnPreviewPopupPositionChanged(ScreenPopupPosition value)
    {
        OnPropertyChanged(nameof(PreviewPopupHorizontalAlignment));
        OnPropertyChanged(nameof(PreviewPopupVerticalAlignment));
        OnPropertyChanged(nameof(PreviewPopupMargin));
        OnPropertyChanged(nameof(PreviewPopupVerticalScrollBarVisibility));
    }

    partial void OnIsPreviewPopupOpenChanged(bool value)
    {
        if (!value && PreviewPopup != null)
            PreviewPopup = null;
    }

    public void OpenPreviewPopup(
        long? popupId,
        ScreenPopupPosition? position = null,
        bool useMousePos = false
    )
    {
        if (CurrentProject == null || popupId == null)
            return;

        var popup = CurrentProject.Popups.FirstOrDefault(x => x.Id == popupId.Value);
        if (popup == null)
            return;

        PreviewPopup = popup;
        PreviewPopupPosition = useMousePos
            ? ScreenPopupPosition.MousePos
            : (position ?? popup.Position);
        IsPreviewPopupOpen = true;
    }

    [RelayCommand]
    public void ClosePreviewPopup()
    {
        IsPreviewPopupOpen = false;
    }

    #endregion
}


public enum ProjectBrowserSortMode
{
    LastOpenedDesc,
    ModifiedDesc,
    NameAsc,
}

public sealed class ProjectFileEntry
{
    public string FullPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime LastWriteTime { get; set; }
    public DateTime? LastOpenedUtc { get; set; }

    public DateTime LastOpenedLocal => LastOpenedUtc?.ToLocalTime() ?? DateTime.MinValue;


    public DateTime LastOpenedSortValue => LastOpenedUtc ?? DateTime.MinValue;

    public string LastOpenedDisplay =>
        LastOpenedUtc.HasValue ? LastOpenedUtc.Value.ToLocalTime().ToString("g") : "-";

    public string ModifiedDisplay => LastWriteTime.ToString("g");
}
