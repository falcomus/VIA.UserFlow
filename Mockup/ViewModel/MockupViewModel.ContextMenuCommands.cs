// ======================================================================================
// FILE: Mockup.ViewModel/MockupViewModel.ContextMenuCommands
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Mockup.Actions;
using Mockup.Dialogs;
using Mockup.Messages;
using Mockup.Services;
using Mockup.Snapshots;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using VIA.WPF.Windowing;
using MessageBox = Mockup.Services.XDialogs;

namespace Mockup.ViewModel;

public sealed partial class MockupViewModel : ObservableObject
{
    #region === INIT ===

    private readonly MockupClipboardService _clipboard = new();

    [ObservableProperty]
    private Point contextMenuWorldPoint;

    partial void InitContextMenu()
    {
        // aktuell nichts nötig (kein Messenger).
        // Kontext wird direkt vom Designer gesetzt.
    }

    #endregion

    #region === DESIGNER KIND / DESIGNER CONTENT KIND ===

    public enum ContextDesignerKind
    {
        Screen,
        Template,
        Popup,
    }

    public bool IsScreenDesigner => MainTabSelectedIndex == 1;
    public bool IsTemplateDesigner => MainTabSelectedIndex == 2;
    public bool IsPopupDesigner => MainTabSelectedIndex == 3;
    public bool IsTemplateTabVisible => IsScreenDesigner;

    #endregion

    #region === CONTEXT STATE ===

    [ObservableProperty]
    private Screen? _contextScreen;

    [ObservableProperty]
    private ScreenTemplate? _contextTemplate;

    [ObservableProperty]
    private ScreenPopup? _contextPopup;

    [ObservableProperty]
    private Band? _contextBand;

    [ObservableProperty]
    private IReadOnlyList<DesignControl>? _contextControls;

    public DesignControl? CurrentSingleControl =>
        HasSingleControlContext ? ContextControls![0] : null;

    public ActionArea? CurrentActionArea => CurrentSingleControl as ActionArea;

    public bool HasScreenContext => ContextScreen != null;
    public bool HasTemplateContext => ContextTemplate != null;
    public bool HasPopupContext => ContextPopup != null;

    public bool HasBandContext => ContextBand != null;
    public bool HasControlContext => ContextControls != null && ContextControls.Count > 0;
    public bool HasSingleControlContext => HasControlContext && ContextControls!.Count == 1;

    public bool HasSingleActionAreaContext => CurrentActionArea != null;
    public bool HasSingleNonActionAreaControlContext =>
        HasSingleControlContext && CurrentActionArea == null;
    public bool HasMultiControlContext => HasControlContext && !HasSingleControlContext;

    public bool ShowActionAreaMenu => HasSingleActionAreaContext;
    public bool ShowControlMenu => HasControlContext && !ShowActionAreaMenu;

    public bool ShowBandMenu =>
        !ShowControlMenu && !ShowActionAreaMenu && HasBandContext && IsScreenDesigner;

    public bool ShowScreenMenu =>
        !ShowControlMenu
        && !ShowActionAreaMenu
        && IsScreenDesigner
        && (ContextScreen ?? CurrentScreen) != null;

    public bool ShowTemplateMenu =>
        !ShowControlMenu
        && !ShowActionAreaMenu
        && IsTemplateDesigner
        && (ContextTemplate ?? CurrentTemplate) != null;

    public bool ShowPopupMenu =>
        !ShowControlMenu
        && !ShowActionAreaMenu
        && IsPopupDesigner
        && (ContextPopup ?? CurrentPopup) != null;

    public bool ShowSeparator_RootToBand =>
        ShowBandMenu && (ShowScreenMenu || ShowTemplateMenu || ShowPopupMenu);

    public bool HasClipboardControls =>
        _clipboard.HasControls;

    public bool CanPasteControls =>
        HasClipboardControls
        && (
            (IsScreenDesigner && CurrentScreen != null)
            || (IsTemplateDesigner && CurrentTemplate != null)
            || (IsPopupDesigner && CurrentPopup != null)
        );

    partial void OnContextScreenChanged(Screen? value) => RaiseContextFlagsChanged();

    partial void OnContextTemplateChanged(ScreenTemplate? value) => RaiseContextFlagsChanged();

    partial void OnContextPopupChanged(ScreenPopup? value) => RaiseContextFlagsChanged();

    partial void OnContextBandChanged(Band? value) => RaiseContextFlagsChanged();

    partial void OnContextControlsChanged(IReadOnlyList<DesignControl>? value) =>
        RaiseContextFlagsChanged();

    private void RaiseContextFlagsChanged()
    {
        OnPropertyChanged(nameof(CurrentSingleControl));
        OnPropertyChanged(nameof(CurrentActionArea));

        OnPropertyChanged(nameof(HasScreenContext));
        OnPropertyChanged(nameof(HasTemplateContext));
        OnPropertyChanged(nameof(HasPopupContext));
        OnPropertyChanged(nameof(HasBandContext));
        OnPropertyChanged(nameof(HasControlContext));
        OnPropertyChanged(nameof(HasSingleControlContext));
        OnPropertyChanged(nameof(HasSingleActionAreaContext));
        OnPropertyChanged(nameof(HasSingleNonActionAreaControlContext));
        OnPropertyChanged(nameof(HasMultiControlContext));

        OnPropertyChanged(nameof(ShowActionAreaMenu));
        OnPropertyChanged(nameof(ShowControlMenu));
        OnPropertyChanged(nameof(ShowBandMenu));
        OnPropertyChanged(nameof(ShowScreenMenu));
        OnPropertyChanged(nameof(ShowTemplateMenu));
        OnPropertyChanged(nameof(ShowPopupMenu));
        OnPropertyChanged(nameof(ShowSeparator_RootToBand));

        OnPropertyChanged(nameof(HasClipboardControls));
        OnPropertyChanged(nameof(CanPasteControls));
    }

    private SnapshotContext? GetActiveContextMenuSnapshotContext()
        => GetCurrentSnapshotContext();

    private bool TryPushActiveContextMenuSnapshot(string label)
    {
        var context = GetActiveContextMenuSnapshotContext();
        if (context == null)
            return false;

        PushSnapshot(context.Value, label);
        return true;
    }

    private void SaveActiveContextMenuSnapshotContext()
    {
        var context = GetActiveContextMenuSnapshotContext();
        if (context != null)
        {
            SaveCurrentSnapshotContext(context.Value);
            return;
        }

        SaveCurrentProject();
    }

    public void SetContextScreen(Screen? screen)
    {
        ContextScreen = screen ?? CurrentScreen;
        ContextTemplate = null;
        ContextPopup = null;

        ContextBand = null;
        ContextControls = null;
    }

    public void SetContextTemplate(ScreenTemplate? template)
    {
        ContextScreen = null;
        ContextTemplate = template ?? CurrentTemplate;
        ContextPopup = null;

        ContextBand = null;
        ContextControls = null;
    }

    public void SetContextPopup(ScreenPopup? popup)
    {
        ContextScreen = null;
        ContextTemplate = null;
        ContextPopup = popup ?? CurrentPopup;

        ContextBand = null;
        ContextControls = null;
    }

    public void SetContextBand(Band band)
    {
        ContextScreen = IsScreenDesigner ? (CurrentScreen ?? ContextScreen) : null;
        ContextTemplate = IsTemplateDesigner ? (CurrentTemplate ?? ContextTemplate) : null;
        ContextPopup = IsPopupDesigner ? (CurrentPopup ?? ContextPopup) : null;

        ContextBand = band;
        ContextControls = null;
    }

    public void SetContextControls(Band? band, IReadOnlyList<DesignControl>? controls)
    {
        if (controls == null)
            return;

        ContextScreen = IsScreenDesigner ? (CurrentScreen ?? ContextScreen) : null;
        ContextTemplate = IsTemplateDesigner ? (CurrentTemplate ?? ContextTemplate) : null;
        ContextPopup = IsPopupDesigner ? (CurrentPopup ?? ContextPopup) : null;

        ContextBand = band;
        ContextControls = controls;
    }

    #endregion

    #region === PROJEKT COMMANDS ===

    private bool CurrentProjectNotNull() { return CurrentProject != null; }

    partial void OnCurrentProjectFileChanged(ProjectFileEntry? value)
    {
        if (_suppressProjectFileSelectionChanged)
            return;

        if (value == null || string.IsNullOrWhiteSpace(value.FullPath))
            return;

        if (!File.Exists(value.FullPath))
        {
            RefreshProjectFiles();
            return;
        }

        if (CurrentProject != null
            && !string.IsNullOrWhiteSpace(CurrentProject.FilePath)
            && string.Equals(
                Path.GetFullPath(CurrentProject.FilePath),
                Path.GetFullPath(value.FullPath),
                StringComparison.OrdinalIgnoreCase))
            return;

        LoadProject(value.FullPath);
        RefreshProjectUiInfo();
    }

    [RelayCommand]
    private void NewProject()
    {
        try
        {
            var project = new Project
            {
                Id = IdGenerator.NewID,
                Name = "",
                Description = "",
                DeviceWidth = 390,
                DeviceHeight = 844,
                Screens = new ObservableCollection<Screen>(),
            };

            bool accepted = DialogService.EditEntity(
                project,
                _ => new ProjectDialog(),
                "NEW PROJECT",
                XDialogService.Default
            );

            if (!accepted)
                return;

            project.FilePath = Path.Combine(ProjectsFolder, project.Name + ".ufp");

            var screen = new Screen(IdGenerator.NewID, "Main Screen", project);

            //XXX screen.UserHeight = project.DeviceHeight;

            project.Screens.Add(screen);

            CurrentProject = project;
            CurrentScreen = screen;

            SaveProject(project);
            RefreshProjectUiInfo();
        }
        finally
        {
            MSG.UI.InvalidateDesigner();
        }
    }

    [RelayCommand]
    private void EditProject(ProjectFileEntry? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.FullPath))
            return;

        if (!File.Exists(item.FullPath))
            return;

        if (
            CurrentProject == null
            || string.IsNullOrWhiteSpace(CurrentProject.FilePath)
            || !string.Equals(
                Path.GetFullPath(CurrentProject.FilePath),
                Path.GetFullPath(item.FullPath),
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            LoadProject(item.FullPath);
        }

        if (CurrentProject == null)
            return;

        try
        {
            bool accepted = DialogService.EditEntity(
                CurrentProject,
                _ => new ProjectDialog(),
                "EDIT PROJECT",
                XDialogService.Default
            );

            if (!accepted)
                return;

            var pf = ProjectFiles.FirstOrDefault(x => x.FullPath == CurrentProject.FilePath);
            if (pf != null)
                pf.DisplayName = CurrentProject.Name;

            SaveProject(CurrentProject);
            RefreshProjectFiles();
            RefreshProjectUiInfo();
        }
        finally
        {
            MSG.UI.InvalidateDesigner();
        }
    }

    [RelayCommand]
    private void CopyProject(ProjectFileEntry? item)
    {
        if (item == null)
            return;

        CopySelectedProjectFile(item);
    }

    [RelayCommand(CanExecute = nameof(CurrentProjectNotNull))]
    private void DeleteProject(ProjectFileEntry? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.FullPath))
            return;

        var projectName = !string.IsNullOrWhiteSpace(item.DisplayName)
            ? item.DisplayName
            : Path.GetFileNameWithoutExtension(item.FullPath);

        var result = MessageBox.Show(
            $"Delete project '{projectName}'?\nThis will permanently delete the project file.",
            "Delete Project",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result != MessageBoxResult.Yes)
            return;

        MSG.UI.ShowOverlay();

        try
        {
            var fullPath = item.FullPath;

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            ClearLastOpenedProjectIfMatches(fullPath);

            var projectToRemove = Projects.FirstOrDefault(p =>
                !string.IsNullOrWhiteSpace(p.FilePath) &&
                string.Equals(
                    Path.GetFullPath(p.FilePath),
                    Path.GetFullPath(fullPath),
                    StringComparison.OrdinalIgnoreCase));

            if (projectToRemove != null)
                Projects.Remove(projectToRemove);

            if (CurrentProject != null
                && !string.IsNullOrWhiteSpace(CurrentProject.FilePath)
                && string.Equals(
                    Path.GetFullPath(CurrentProject.FilePath),
                    Path.GetFullPath(fullPath),
                    StringComparison.OrdinalIgnoreCase))
            {
                CurrentProject = null;
                CurrentScreen = null;
                HomeScreen = null;
                CurrentPopup = null;
            }

            RefreshProjectFiles();

            var nextProject = ProjectFiles.FirstOrDefault();
            if (nextProject != null && File.Exists(nextProject.FullPath))
            {
                LoadProject(nextProject.FullPath);
            }

            RefreshProjectUiInfo();
        }
        finally
        {
            MSG.UI.InvalidateDesigner();
            MSG.UI.HideOverlay();
        }
    }

    private void RefreshProjectFiles()
    {
        Directory.CreateDirectory(ProjectsFolder);

        var files = Directory
            .GetFiles(ProjectsFolder, "*.ufp", SearchOption.TopDirectoryOnly)
            .Select(path => new ProjectFileEntry
            {
                FullPath = path,
                DisplayName = ReadProjectDisplayNameFromFile(path),
                LastWriteTime = File.GetLastWriteTime(path),
            })
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.LastWriteTime)
            .ToList();

        ProjectFiles = new ObservableCollection<ProjectFileEntry>(files);

        if (CurrentProject != null && !string.IsNullOrWhiteSpace(CurrentProject.FilePath))
        {
            _suppressProjectFileSelectionChanged = true;
            try
            {
                CurrentProjectFile = ProjectFiles.FirstOrDefault(x =>
                    string.Equals(
                        Path.GetFullPath(x.FullPath),
                        Path.GetFullPath(CurrentProject.FilePath),
                        StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                _suppressProjectFileSelectionChanged = false;
            }
        }
        else
        {
            _suppressProjectFileSelectionChanged = true;
            try
            {
                CurrentProjectFile = null;
            }
            finally
            {
                _suppressProjectFileSelectionChanged = false;
            }
        }
    }

    private string ReadProjectDisplayNameFromFile(string path)
    {
        try
        {
            if (!File.Exists(path))
                return Path.GetFileNameWithoutExtension(path);

            var project = JsonSerializer.Deserialize<Project>(
                File.ReadAllText(path),
                JsonOptions);

            if (project != null && !string.IsNullOrWhiteSpace(project.Name))
                return project.Name;
        }
        catch
        {
        }

        return Path.GetFileNameWithoutExtension(path);
    }

    [RelayCommand]
    private async Task LoadProjectAsync()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Open Project",
            Filter = "UserFlow projects (*.ufp)|*.ufp|All files (*.*)|*.*",
            InitialDirectory = ProjectsFolder,
            Multiselect = false,
        };

        if (dlg.ShowDialog() == true)
        {
            string sourcePath = dlg.FileName;

            if (!File.Exists(sourcePath))
                return;

            Directory.CreateDirectory(ProjectsFolder);

            string fileName = Path.GetFileName(sourcePath);
            string baseName = Path.GetFileNameWithoutExtension(sourcePath);
            string ext = Path.GetExtension(sourcePath);

            string targetPath = Path.Combine(ProjectsFolder, fileName);

            if (!string.Equals(
                    Path.GetFullPath(sourcePath),
                    Path.GetFullPath(targetPath),
                    StringComparison.OrdinalIgnoreCase))
            {
                int copyIndex = 2;

                while (File.Exists(targetPath))
                {
                    targetPath = Path.Combine(ProjectsFolder, $"{baseName} - Copy {copyIndex}{ext}");
                    copyIndex++;
                }

                File.Copy(sourcePath, targetPath, overwrite: false);
            }

            LoadProject(targetPath);
            RefreshProjectUiInfo();
        }

        await Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CurrentProjectNotNull))]
    private async Task SaveAllAsync()
    {
        if (CurrentProject == null)
            return;

        if (string.IsNullOrWhiteSpace(CurrentProject.FilePath))
        {
            await SaveProjectAsAsync();
            return;
        }

        SaveAll();
        RefreshProjectUiInfo();
    }

    [RelayCommand(CanExecute = nameof(CurrentProjectNotNull))]
    private async Task SaveProjectAsync()
    {
        if (CurrentProject == null)
            return;

        if (string.IsNullOrWhiteSpace(CurrentProject.FilePath))
        {
            await SaveProjectAsAsync();
            return;
        }

        SaveProject(CurrentProject);
        RefreshProjectUiInfo();

        XNotifications.Success("Project successfully saved.");
    }

    [RelayCommand(CanExecute = nameof(CurrentProjectNotNull))]
    private async Task SaveProjectAsAsync()
    {
        if (CurrentProject == null)
            return;

        EnsureDataFolders();

        var dlg = new SaveFileDialog
        {
            Title = "Save Project As",
            Filter = "UserFlow Project (*.ufp)|*.ufp",
            InitialDirectory = ProjectsFolder,
            FileName = $"{SanitizeFileName(CurrentProject.Name)}.ufp",
            AddExtension = true,
        };

        if (dlg.ShowDialog() == true)
        {
            CurrentProject.FilePath = dlg.FileName;
            SaveProject(CurrentProject);
            RefreshProjectUiInfo();
        }

        XNotifications.Success("Project successfully saved.");

        await Task.CompletedTask;
    }

    #endregion

    #region === SCREEN COMMANDS ===

    private bool CurrentScreenNotNull() { return CurrentScreen != null; }

    [RelayCommand(CanExecute = nameof(CurrentScreenNotNull))]
    private void EditScreen()
    {
        var screen = ContextScreen ?? CurrentScreen;
        if (screen == null)
            return;

        EditScreenCore(screen);
    }

    [RelayCommand]
    private void EditScreenCore(Screen screen)
    {
        if (CurrentProject == null || screen == null)
            return;

        CurrentScreen = screen;

        MSG.UI.ShowOverlay();

        try
        {
            var oldId = screen.Id;

            bool accepted = DialogService.EditEntity(
                screen,
                _ => new ScreenDialog(),
                "EDIT SCREEN",
                beforeApply: () => PushSnapshot(SnapshotContext.Screen, SnapshotLabels.ScreenChanged)
            );

            if (!accepted)
                return;

            Debug.WriteLine("[Screen.Reconstruct] BEFORE RecalculateBandLayout");

            screen.RecalculateBandLayout();

            Debug.WriteLine("[Screen.Reconstruct] AFTER  RecalculateBandLayout");

            if (screen.IsHomeScreen)
            {
                ResetHomeScreen();
                screen.IsHomeScreen = true;
            }


            SaveCurrentProject();
            RefreshProjectUiInfo();

            CurrentScreen = CurrentProject.Screens.First(s => s.Id == oldId);
        }
        finally
        {
            MSG.UI.InvalidateDesigner();
            MSG.UI.HideOverlay();
        }
    }

    [RelayCommand]
    private void NewScreen()
    {
        if (CurrentProject == null)
            return;

        MSG.UI.ShowOverlay();

        try
        {
            var screen = new Screen(IdGenerator.NewID, "", CurrentProject)
            {
                ShowHeader = false,
                ShowFooter = false,
                ShowBackButton = true,
                Background = System.Windows.Media.Colors.White,
                //XXX UserHeight = CurrentProject.DeviceHeight,
                GroupName = MockupViewModel.DEFAULT_SCREEN_GROUPNAME,
            };

            screen.EnsureDefaultBands();
            screen.RecalculateBandLayout();

            bool accepted = DialogService.EditEntity(
                screen,
                _ => new Mockup.Dialogs.ScreenDialog(),
                "NEW SCREEN"
            );

            if (!accepted)
                return;

            AddRecentColorIfMissing(screen.Background);

            screen.Project = CurrentProject;
            PushProjectSnapshot(SnapshotLabels.ScreenAdded, screen.Id);
            CurrentProject.Screens.Add(screen);

            SaveCurrentProject();

            CurrentScreen = screen;
            RefreshProjectUiInfo();
        }
        finally
        {
            MSG.UI.InvalidateDesigner();
            MSG.UI.HideOverlay();
        }
    }

    [RelayCommand(CanExecute = nameof(CurrentScreenNotNull))]
    private void DuplicateScreen() => XNotifications.Info("DuplicateScreen (TODO)");

    [RelayCommand(CanExecute = nameof(CurrentScreenNotNull))]
    private void DeleteScreen(Screen screen)
    {
        if (CurrentProject == null)
            return;

        if (screen == null)
            return;

        CurrentScreen = screen;

        var result = MessageBox.Show(
            $"Do you really want to delete this screen?\n\n{screen.Name}",
            "Delete Screen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result != MessageBoxResult.Yes)
            return;

        PushProjectSnapshot(SnapshotLabels.ScreenDeleted, screen.Id);

        if (screen.IsHomeScreen)
        {
            var newHomeScreen = CurrentProject.Screens.FirstOrDefault(x => x.Id != screen.Id);
            if (newHomeScreen != null)
                newHomeScreen.IsHomeScreen = true;
        }

        CurrentProject.Screens.Remove(screen);
        SaveCurrentProject();
        RefreshProjectUiInfo();

        CurrentScreen = CurrentProject.Screens.OrderBy(x => x.Name).FirstOrDefault();

        MSG.UI.InvalidateDesigner();
    }

    [RelayCommand]
    private void ChangeScreenBackground()
    {
        if (CurrentScreen == null)
            return;

        var dialog = new XColorPickerDialog { SelectedColor = CurrentScreen.Background };

        bool? accepted = dialog.ShowDialog();
        if (accepted != true)
            return;

        if (CurrentScreen.Background == dialog.SelectedColor)
            return;

        PushSnapshot(SnapshotContext.Screen, SnapshotLabels.ScreenChanged);

        CurrentScreen.Background = dialog.SelectedColor;

        SaveCurrentProject();
        MSG.UI.InvalidateDesigner();
    }

    [RelayCommand]
    private void SelectScreenImage(Screen screen)
    {
        if (screen == null)
            return;

        var dlg = new OpenFileDialog
        {
            Title = "Hintergrundbild auswählen",
            Filter = "Bilddateien|*.png;*.jpg;*.jpeg;*.bmp;*.webp|Alle Dateien|*.*",
            CheckFileExists = true,
            Multiselect = false,
        };

        if (dlg.ShowDialog() != true)
            return;

        try
        {
            CurrentScreen = screen;
            PushSnapshot(SnapshotContext.Screen, SnapshotLabels.ScreenChanged);

            screen.BackgroundImageFilename = Path.GetFileName(dlg.FileName);
            screen.SetBackgroundImageFromFile(dlg.FileName);

            SaveCurrentProject();

            MSG.UI.InvalidateDesigner();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to select background image for screen {ScreenName}", screen.Name);
            XNotifications.Error("Background image could not be loaded.");
        }
    }

    [RelayCommand]
    private void ResetScreenImage(Screen screen)
    {
        if (screen == null)
            return;

        try
        {
            CurrentScreen = screen;
            PushSnapshot(SnapshotContext.Screen, SnapshotLabels.ScreenChanged);

            screen.ResetBackgroundImage();

            SaveCurrentProject();

            MSG.UI.InvalidateDesigner();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to reset background image for screen {ScreenName}", screen.Name);
            XNotifications.Error("Background image could not be removed.");
        }
    }

    [RelayCommand]
    private void ToggleShowHeader()
    {
        var screen = ContextScreen ?? CurrentScreen;
        if (screen == null)
            return;

        CurrentScreen = screen;
        PushSnapshot(SnapshotContext.Screen, SnapshotLabels.ScreenChanged);

        screen.ShowHeader = !screen.ShowHeader;
        screen.RecalculateBandLayout();

        SaveCurrentProject();
        MSG.UI.InvalidateDesigner();
    }

    [RelayCommand]
    private void ToggleShowFooter()
    {
        var screen = ContextScreen ?? CurrentScreen;
        if (screen == null)
            return;

        CurrentScreen = screen;
        PushSnapshot(SnapshotContext.Screen, SnapshotLabels.ScreenChanged);

        screen.ShowFooter = !screen.ShowFooter;
        screen.RecalculateBandLayout();

        SaveCurrentProject();
        MSG.UI.InvalidateDesigner();
    }

    [RelayCommand]
    private void SetAsHomeScreen(Screen screen)
    {
        if (CurrentProject == null || screen == null)
            return;

        if (ReferenceEquals(HomeScreen, screen) && screen.IsHomeScreen)
            return;

        foreach (Screen projectScreen in CurrentProject.Screens)
            projectScreen.IsHomeScreen = ReferenceEquals(projectScreen, screen);

        HomeScreen = screen;
        MockupService.Mockup.HomeScreen = screen;
        SaveProject(CurrentProject, refreshProjectFiles: false);
    }

    [RelayCommand]
    private void ResetHomeScreen() =>
        CurrentProject?.Screens?.ToList().ForEach(screen => screen.IsHomeScreen = false);

    [RelayCommand]
    private void GotoScreenPage(Screen screen)
    {
        CurrentScreen = screen;
        MainTabSelectedIndex = 1;
        ScreenTabSelectedIndex = 0;
    }

    #endregion

    #region === BAND COMMANDS (PLACEHOLDER) ===

    //[RelayCommand]
    //private void AddBand()
    //{
    //    if (!IsScreenDesigner)
    //        return;

    //    var screen = ContextScreen ?? CurrentScreen;
    //    if (screen == null)
    //        return;

    //    var customBands = screen.Bands.Where(b => b.BandType == BandType.Custom).ToList();
    //    if (customBands.Count == 0)
    //    {
    //        screen.EnsureDefaultBands();
    //        customBands = screen.Bands.Where(b => b.BandType == BandType.Custom).ToList();
    //        if (customBands.Count == 0)
    //            return;
    //    }

    //    var fillBand = customBands.Last();
    //    int fillIndex = screen.Bands.IndexOf(fillBand);
    //    if (fillIndex < 0)
    //        return;

    //    int insertIndex = fillIndex;

    //    var ctxBand = ContextBand;
    //    if (ctxBand != null && ctxBand.BandType == BandType.Custom)
    //    {
    //        int ctxIndex = screen.Bands.IndexOf(ctxBand);
    //        if (ctxIndex >= 0)
    //            insertIndex = Math.Min(ctxIndex + 1, fillIndex);
    //    }

    //    var band = new Band
    //    {
    //        BandType = BandType.Custom,
    //        IsExpandable = false,
    //        IsExpanded = false,
    //        UniformPageHeight = true,
    //        ActivePageIndex = 0,
    //        SavedExpandedHeight = 90,
    //        Height = 60,
    //        Width = screen.Width,
    //        X = 0,
    //        ParentScreen = screen,
    //    };

    //    band.EnsureDefaultIdentity(screen);

    //    band.AddNewPage();
    //    if (band.ActivePage != null)
    //        band.ActivePage.Height = band.Height;

    //    screen.Bands.Insert(insertIndex, band);

    //    screen.RecalculateBandLayout();

    //    MSG.SelectBand(screen.Id, band.Id);

    //    MSG.UI.InvalidateDesigner();
    //}

    [RelayCommand]
    private void AddBand()
    {
        if (!IsScreenDesigner)
            return;

        var screen = ContextScreen ?? CurrentScreen;
        if (screen == null)
            return;

        var customBands = screen.Bands.Where(b => b.BandType == BandType.Custom).ToList();
        if (customBands.Count == 0)
        {
            screen.EnsureDefaultBands();
            customBands = screen.Bands.Where(b => b.BandType == BandType.Custom).ToList();
            if (customBands.Count == 0)
                return;
        }

        var footer = screen.Bands.FirstOrDefault(b => b.BandType == BandType.Footer);
        int insertIndex = footer != null
            ? screen.Bands.IndexOf(footer)
            : screen.Bands.Count;

        var ctxBand = ContextBand;
        if (ctxBand != null && ctxBand.BandType == BandType.Custom)
        {
            int ctxIndex = screen.Bands.IndexOf(ctxBand);
            if (ctxIndex >= 0)
                insertIndex = Math.Min(ctxIndex + 1, insertIndex);
        }

        var band = new Band
        {
            BandType = BandType.Custom,
            IsExpandable = false,
            IsExpanded = false,
            UniformPageHeight = true,
            ActivePageIndex = 0,
            SavedExpandedHeight = 90,
            Height = 60,
            Width = screen.Width,
            X = 0,
            ParentScreen = screen,
        };

        band.EnsureDefaultIdentity(screen);

        band.AddNewPage();
        if (band.ActivePage != null)
            band.ActivePage.Height = band.Height;

        CurrentScreen = screen;
        PushSnapshot(SnapshotContext.Screen, SnapshotLabels.BandAdded);

        screen.Bands.Insert(insertIndex, band);

        //XXX 
        //screen.UserHeight = Math.Max(
        //    screen.DesignHeight + band.EffectiveHeight,
        //    screen.Project?.DeviceHeight ?? 0f);

        screen.RecalculateBandLayout();

        MSG.SelectBand(screen.Id, band.Id);

        SaveCurrentProject();
        MSG.UI.InvalidateDesigner();
    }


    //[RelayCommand]
    //private void AddExpandableBand()
    //{
    //    if (!IsScreenDesigner)
    //        return;

    //    var screen = ContextScreen ?? CurrentScreen;
    //    if (screen == null)
    //        return;

    //    var customBands = screen.Bands.Where(b => b.BandType == BandType.Custom).ToList();
    //    if (customBands.Count == 0)
    //    {
    //        screen.EnsureDefaultBands();
    //        customBands = screen.Bands.Where(b => b.BandType == BandType.Custom).ToList();
    //        if (customBands.Count == 0)
    //            return;
    //    }

    //    var fillBand = customBands.Last();
    //    int fillIndex = screen.Bands.IndexOf(fillBand);
    //    if (fillIndex < 0)
    //        return;

    //    int insertIndex = fillIndex;

    //    var ctxBand = ContextBand;
    //    if (ctxBand != null && ctxBand.BandType == BandType.Custom)
    //    {
    //        int ctxIndex = screen.Bands.IndexOf(ctxBand);
    //        if (ctxIndex >= 0)
    //            insertIndex = Math.Min(ctxIndex + 1, fillIndex);
    //    }

    //    var band = new Band
    //    {
    //        BandType = BandType.Custom,
    //        IsExpandable = true,
    //        IsExpanded = false,
    //        UniformPageHeight = true,
    //        ActivePageIndex = 0,
    //        SavedExpandedHeight = 90,
    //        Height = 90,
    //        Width = screen.Width,
    //        X = 0,
    //        ParentScreen = screen,
    //    };

    //    band.EnsureDefaultIdentity(screen);

    //    band.AddNewPage();
    //    if (band.ActivePage != null)
    //        band.ActivePage.Height = band.Height;

    //    screen.Bands.Insert(insertIndex, band);

    //    screen.RecalculateBandLayout();

    //    MSG.UI.InvalidateDesigner();
    //}

    [RelayCommand]
    private void AddExpandableBand()
    {
        if (!IsScreenDesigner)
            return;

        var screen = ContextScreen ?? CurrentScreen;
        if (screen == null)
            return;

        var customBands = screen.Bands.Where(b => b.BandType == BandType.Custom).ToList();
        if (customBands.Count == 0)
        {
            screen.EnsureDefaultBands();
            customBands = screen.Bands.Where(b => b.BandType == BandType.Custom).ToList();
            if (customBands.Count == 0)
                return;
        }

        var footer = screen.Bands.FirstOrDefault(b => b.BandType == BandType.Footer);
        int insertIndex = footer != null
            ? screen.Bands.IndexOf(footer)
            : screen.Bands.Count;

        var ctxBand = ContextBand;
        if (ctxBand != null && ctxBand.BandType == BandType.Custom)
        {
            int ctxIndex = screen.Bands.IndexOf(ctxBand);
            if (ctxIndex >= 0)
                insertIndex = Math.Min(ctxIndex + 1, insertIndex);
        }

        var band = new Band
        {
            BandType = BandType.Custom,
            IsExpandable = true,
            IsExpanded = false,
            UniformPageHeight = true,
            ActivePageIndex = 0,
            //XXX SavedExpandedHeight = 90,
            //XXXHeight = 90,
            SavedExpandedHeight = Screen.DefaultBandHeaderHeight,
            Height = Screen.DefaultBandHeaderHeight,
            Width = screen.Width,
            X = 0,
            ParentScreen = screen,
        };

        band.EnsureDefaultIdentity(screen);

        band.AddNewPage();
        if (band.ActivePage != null)
            band.ActivePage.Height = band.Height;

        CurrentScreen = screen;
        PushSnapshot(SnapshotContext.Screen, SnapshotLabels.BandAdded);

        screen.Bands.Insert(insertIndex, band);

        //XXX 
        //screen.UserHeight = Math.Max(
        //    screen.DesignHeight + band.EffectiveHeight,
        //    screen.Project?.DeviceHeight ?? 0f);

        screen.RecalculateBandLayout();

        SaveCurrentProject();
        MSG.UI.InvalidateDesigner();
    }


    [RelayCommand]
    private void DuplicateBand()
    {
        if (!IsScreenDesigner)
            return;

        var screen = ContextScreen ?? CurrentScreen;
        var sourceBand = ContextBand;

        if (screen == null || sourceBand == null)
            return;

        if (sourceBand.BandType != BandType.Custom)
            return;

        int sourceIndex = screen.Bands.IndexOf(sourceBand);
        if (sourceIndex < 0)
            return;

        var clone = sourceBand.DeepClone();

        clone.ParentScreen = screen;

        clone.Id = IdGenerator.NewID;
        clone.Name = Band.DEFAULT_NAME;

        if (string.IsNullOrWhiteSpace(clone.Title))
            clone.Title = Band.DEFAULT_TITLE;

        clone.EnsureDefaultIdentity(screen);

        foreach (var page in clone.Pages)
        {
            page.ParentBand = clone;

            foreach (var ctrl in page.Controls)
            {
                ctrl.ParentBand = clone;
                ctrl.ParentBandPage = page;
            }
        }

        int insertIndex = sourceIndex + 1;

        CurrentScreen = screen;
        PushSnapshot(SnapshotContext.Screen, SnapshotLabels.BandAdded);

        screen.Bands.Insert(insertIndex, clone);

        screen.RecalculateBandLayout();

        ContextScreen = screen;
        ContextBand = clone;
        ContextControls = null;

        SaveCurrentProject();
        MSG.UI.InvalidateDesigner();
    }

    //[RelayCommand]
    //private void DeleteBand(Band band)
    //{
    //    if (CurrentScreen == null)
    //        return;
    //    if (band == null)
    //        return;

    //    int customCount = CurrentScreen.Bands.Count(b => b.BandType == BandType.Custom);
    //    if (customCount <= 1)
    //    {
    //        XNotifications.Info("At least one band is required!");
    //        return;
    //    }

    //    int idx = CurrentScreen.Bands.IndexOf(band);
    //    if (idx < 0)
    //        return;

    //    CurrentScreen.Bands.RemoveAt(idx);

    //    CurrentScreen.RecalculateBandLayout();
    //}

    [RelayCommand]
    private void DeleteBand(Band band)
    {
        if (CurrentScreen == null)
            return;

        if (band == null)
            return;

        int customCount = CurrentScreen.Bands.Count(b => b.BandType == BandType.Custom);
        if (customCount <= 1)
        {
            XNotifications.Info("At least one band is required!");
            return;
        }

        int idx = CurrentScreen.Bands.IndexOf(band);
        if (idx < 0)
            return;

        float removedHeight = band.EffectiveHeight;

        PushSnapshot(SnapshotContext.Screen, SnapshotLabels.BandDeleted);

        CurrentScreen.Bands.RemoveAt(idx);

        float minHeight = CurrentScreen.Project?.DeviceHeight ?? 0f;

        //XXX
        //CurrentScreen.UserHeight = Math.Max(CurrentScreen.DesignHeight - removedHeight, minHeight);

        CurrentScreen.RecalculateBandLayout();

        SaveCurrentProject();
        MSG.UI.InvalidateDesigner();
    }



    [RelayCommand]
    private void MoveBandUp()
    {
        if (!IsScreenDesigner)
            return;

        var screen = ContextScreen ?? CurrentScreen;
        var band = ContextBand;

        if (screen == null || band == null)
            return;

        if (band.BandType != BandType.Custom)
            return;

        int idx = screen.Bands.IndexOf(band);
        if (idx <= 0)
            return;

        var header = screen.Bands.FirstOrDefault(b => b.BandType == BandType.Header);
        int minIdx = header != null ? screen.Bands.IndexOf(header) + 1 : 0;

        if (idx <= minIdx)
            return;

        CurrentScreen = screen;
        PushSnapshot(SnapshotContext.Screen, SnapshotLabels.BandMoved);

        screen.Bands.Move(idx, idx - 1);

        screen.RecalculateBandLayout();

        ContextBand = band;
        ContextScreen = screen;

        SaveCurrentProject();
        MSG.UI.InvalidateDesigner();
    }

    [RelayCommand]
    private void MoveBandDown()
    {
        if (!IsScreenDesigner)
            return;

        var screen = ContextScreen ?? CurrentScreen;
        var band = ContextBand;

        if (screen == null || band == null)
            return;

        if (band.BandType != BandType.Custom)
            return;

        int idx = screen.Bands.IndexOf(band);
        if (idx < 0 || idx >= screen.Bands.Count - 1)
            return;

        var footer = screen.Bands.FirstOrDefault(b => b.BandType == BandType.Footer);
        int maxIdx = footer != null ? screen.Bands.IndexOf(footer) - 1 : screen.Bands.Count - 1;

        if (idx >= maxIdx)
            return;

        CurrentScreen = screen;
        PushSnapshot(SnapshotContext.Screen, SnapshotLabels.BandMoved);

        screen.Bands.Move(idx, idx + 1);

        screen.RecalculateBandLayout();

        ContextBand = band;
        ContextScreen = screen;

        SaveCurrentProject();
        MSG.UI.InvalidateDesigner();
    }

    #endregion

    #region === ACTION AREA COMMANDS  ===

    [RelayCommand]
    private void EditActionArea(ActionArea? area)
    {
        area ??= CurrentActionArea;
        if (area == null)
            return;

        MSG.AA.ShowEditor(area);
    }

    [RelayCommand]
    private void EditActionAreaActions(ActionArea? area)
    {
        area ??= CurrentActionArea;
        if (area == null)
            return;

        MSG.AA.ShowEditor(area);
    }

    #endregion

    #region === CONTROL COMMANDS  ===

    [RelayCommand]
    private void DuplicateControls()
    {
        if (CurrentProject == null)
            return;

        var sel = ContextControls;
        if (sel == null || sel.Count == 0)
            return;

        var groups = sel.Where(c => c?.ParentBandPage != null)
            .GroupBy(c => c.ParentBandPage!)
            .ToList();

        if (groups.Count == 0)
            return;

        TryPushActiveContextMenuSnapshot(SnapshotLabels.ControlDuplicated);

        var created = new List<DesignControl>();

        foreach (var g in groups)
        {
            var page = g.Key;
            var list = page.Controls;
            if (list == null)
                continue;

            var orderedSel = g.OrderBy(c => c.ZIndex).ToList();
            if (orderedSel.Count == 0)
                continue;

            int maxZ = list.Count == 0 ? 0 : list.Max(c => c.ZIndex);

            foreach (var src in orderedSel)
            {
                var copy = src.DeepClone();

                copy.X = src.X + 10;
                copy.Y = src.Y + 10;

                copy.ParentBandPage = page;
                copy.ParentBand = src.ParentBand;

                copy.ZIndex = ++maxZ;

                list.Add(copy);
                created.Add(copy);
            }

            NormalizeZIndices(list);
        }

        SaveActiveContextMenuSnapshotContext();
        MSG.UI.InvalidateDesigner();
    }

    [RelayCommand]
    private void CopyControls()
    {
        var sel = ContextControls;
        if (sel == null || sel.Count == 0)
            return;

        _clipboard.SetControls(sel);

        OnPropertyChanged(nameof(HasClipboardControls));
        OnPropertyChanged(nameof(CanPasteControls));
    }

    [RelayCommand]
    private void PasteControls()
    {
        if (!_clipboard.HasControls)
            return;

        BandPage? targetPage = null;
        Band? targetBand = null;

        if (ContextBand?.ActivePage != null)
        {
            targetBand = ContextBand;
            targetPage = ContextBand.ActivePage;
        }
        else if (IsScreenDesigner)
        {
            targetBand = CurrentScreen?.Bands.FirstOrDefault(b =>
                b.BandType == BandType.Custom && b.ActivePage != null
            );
            targetPage = targetBand?.ActivePage;
        }
        else if (IsTemplateDesigner)
        {
            targetBand = CurrentTemplate?.Bands.FirstOrDefault(b => b.ActivePage != null);
            targetPage = targetBand?.ActivePage;
        }
        else if (IsPopupDesigner)
        {
            targetBand = CurrentPopup?.Bands.FirstOrDefault(b => b.ActivePage != null);
            targetPage = targetBand?.ActivePage;
        }

        if (targetPage == null || targetBand == null)
            return;

        var copies = _clipboard.CreateControlCopies();
        if (copies.Count == 0)
            return;

        var first = copies[0];
        float baseX = first.X;
        float baseY = first.Y;

        float targetX = (float)(ContextMenuWorldPoint.X - targetPage.WorldBounds.Left);
        float targetY = (float)(ContextMenuWorldPoint.Y - targetPage.WorldBounds.Top);

        int maxZ = targetPage.Controls.Count == 0 ? 0 : targetPage.Controls.Max(c => c.ZIndex);

        TryPushActiveContextMenuSnapshot(SnapshotLabels.ControlPasted);

        foreach (var ctrl in copies)
        {
            float relX = ctrl.X - baseX;
            float relY = ctrl.Y - baseY;

            ctrl.ParentBand = targetBand;
            ctrl.ParentBandPage = targetPage;

            ctrl.X = MathF.Round(targetX + relX);
            ctrl.Y = MathF.Round(targetY + relY);
            ctrl.ZIndex = ++maxZ;

            targetPage.Controls.Add(ctrl);
        }

        NormalizeZIndices(targetPage.Controls);
        ReplaceSelection(targetBand, copies);

        SaveActiveContextMenuSnapshotContext();
        MSG.UI.InvalidateDesigner();
    }

    [RelayCommand]
    private void DeleteControls()
    {
        if (CurrentProject == null)
            return;

        var sel = ContextControls;
        if (sel == null || sel.Count == 0)
            return;

        var toDelete = sel
            .Where(c => c?.ParentBandPage != null)
            .Distinct()
            .ToList();

        if (toDelete.Count == 0)
            return;

        var groups = toDelete
            .GroupBy(c => c.ParentBandPage!)
            .ToList();

        TryPushActiveContextMenuSnapshot(SnapshotLabels.ControlDeleted);

        foreach (var c in toDelete)
        {
            c.IsSelected = false;
            SelectedControls.Remove(c);
        }

        foreach (var g in groups)
        {
            var page = g.Key;
            var list = page.Controls;
            if (list == null)
                continue;

            foreach (var c in g)
                list.Remove(c);

            NormalizeZIndices(list);
        }

        ContextControls = null;
        CurrentControl = null;
        ContextBand = null;

        SaveActiveContextMenuSnapshotContext();

        MSG.UI.InvalidateDesigner();
    }

    [RelayCommand]
    private void BringToFront()
    {
        if (!IsScreenDesigner && !IsTemplateDesigner && !IsPopupDesigner)
            return;

        var sel = ContextControls;
        if (sel == null || sel.Count == 0)
            return;

        TryPushActiveContextMenuSnapshot(SnapshotLabels.ControlZOrderChanged);

        ApplyZOrderChange(sel, ZOrderAction.BringToFront);

        SaveAfterZOrderChange();
    }

    [RelayCommand]
    private void SendToBack()
    {
        if (!IsScreenDesigner && !IsTemplateDesigner && !IsPopupDesigner)
            return;

        var sel = ContextControls;
        if (sel == null || sel.Count == 0)
            return;

        TryPushActiveContextMenuSnapshot(SnapshotLabels.ControlZOrderChanged);

        ApplyZOrderChange(sel, ZOrderAction.SendToBack);

        SaveAfterZOrderChange();
    }

    [RelayCommand]
    private void BringForward()
    {
        if (!IsScreenDesigner && !IsTemplateDesigner && !IsPopupDesigner)
            return;

        var sel = ContextControls;
        if (sel == null || sel.Count == 0)
            return;

        TryPushActiveContextMenuSnapshot(SnapshotLabels.ControlZOrderChanged);

        ApplyZOrderChange(sel, ZOrderAction.BringForward);

        SaveAfterZOrderChange();
    }

    [RelayCommand]
    private void SendBackward()
    {
        if (!IsScreenDesigner && !IsTemplateDesigner && !IsPopupDesigner)
            return;

        var sel = ContextControls;
        if (sel == null || sel.Count == 0)
            return;

        TryPushActiveContextMenuSnapshot(SnapshotLabels.ControlZOrderChanged);

        ApplyZOrderChange(sel, ZOrderAction.SendBackward);

        SaveAfterZOrderChange();
    }

    private enum ZOrderAction
    {
        BringToFront,
        SendToBack,
        BringForward,
        SendBackward,
    }

    private void SaveAfterZOrderChange()
    {
        SaveActiveContextMenuSnapshotContext();
        MSG.UI.InvalidateDesigner();
    }

    private static void ApplyZOrderChange(
        IReadOnlyList<DesignControl> selection,
        ZOrderAction action
    )
    {
        var groups = selection
            .Where(c => c?.ParentBandPage != null)
            .GroupBy(c => c.ParentBandPage!)
            .ToList();

        foreach (var g in groups)
        {
            var page = g.Key;
            var sel = g.Where(c => c != null).ToList();
            if (sel.Count == 0)
                continue;

            var list = page.Controls;
            if (list == null || list.Count == 0)
                continue;

            NormalizeZIndices(list);

            switch (action)
            {
                case ZOrderAction.BringToFront:
                    BringToFrontCore(list, sel);
                    break;

                case ZOrderAction.SendToBack:
                    SendToBackCore(list, sel);
                    break;

                case ZOrderAction.BringForward:
                    BringForwardCore(list, sel);
                    break;

                case ZOrderAction.SendBackward:
                    SendBackwardCore(list, sel);
                    break;
            }

            NormalizeZIndices(list);
        }
    }

    private static void NormalizeZIndices(ObservableCollection<DesignControl> list)
    {
        var ordered = list.Select((c, i) => new { c, i })
            .OrderBy(x => x.c.ZIndex)
            .ThenBy(x => x.i)
            .Select(x => x.c)
            .ToList();

        for (int i = 0; i < ordered.Count; i++)
            ordered[i].ZIndex = i;
    }

    private static void BringToFrontCore(
        ObservableCollection<DesignControl> list,
        List<DesignControl> sel
    )
    {
        var orderedSel = sel.OrderBy(c => c.ZIndex).ToList();

        int max = list.Max(c => c.ZIndex);
        int z = max + 1;

        foreach (var c in orderedSel)
            c.ZIndex = z++;
    }

    private static void SendToBackCore(
        ObservableCollection<DesignControl> list,
        List<DesignControl> sel
    )
    {
        var orderedSel = sel.OrderBy(c => c.ZIndex).ToList();

        int min = list.Min(c => c.ZIndex);
        int z = min - orderedSel.Count;

        foreach (var c in orderedSel)
            c.ZIndex = z++;
    }

    private static void BringForwardCore(
        ObservableCollection<DesignControl> list,
        List<DesignControl> sel
    )
    {
        var set = new HashSet<DesignControl>(sel);

        var ordered = list.OrderBy(c => c.ZIndex).ThenBy(c => list.IndexOf(c)).ToList();

        for (int i = ordered.Count - 2; i >= 0; i--)
        {
            var cur = ordered[i];
            if (!set.Contains(cur))
                continue;

            var next = ordered[i + 1];
            if (set.Contains(next))
                continue;

            int tmp = cur.ZIndex;
            cur.ZIndex = next.ZIndex;
            next.ZIndex = tmp;

            ordered[i] = next;
            ordered[i + 1] = cur;
        }
    }

    private static void SendBackwardCore(
        ObservableCollection<DesignControl> list,
        List<DesignControl> sel
    )
    {
        var set = new HashSet<DesignControl>(sel);

        var ordered = list.OrderBy(c => c.ZIndex).ThenBy(c => list.IndexOf(c)).ToList();

        for (int i = 1; i < ordered.Count; i++)
        {
            var cur = ordered[i];
            if (!set.Contains(cur))
                continue;

            var prev = ordered[i - 1];
            if (set.Contains(prev))
                continue;

            int tmp = cur.ZIndex;
            cur.ZIndex = prev.ZIndex;
            prev.ZIndex = tmp;

            ordered[i] = prev;
            ordered[i - 1] = cur;
        }
    }

    #endregion

    #region === CONTROL ALIGNMENT COMMANDS ===

    [RelayCommand]
    private void AlignLeft()
    {
        var sel = GetSelectedControlsForAlignment();
        if (sel.Count < 2)
            return;

        TryPushActiveContextMenuSnapshot(SnapshotLabels.ControlsAligned);

        float left = sel.Min(c => c.X);

        foreach (var c in sel)
            c.X = left;

        SaveAfterControlAlignmentChange();
    }

    [RelayCommand]
    private void AlignCenter()
    {
        var sel = GetSelectedControlsForAlignment();
        if (sel.Count < 2)
            return;

        TryPushActiveContextMenuSnapshot(SnapshotLabels.ControlsAligned);

        float center = (float)(GetSelectionBounds(sel).Left + (GetSelectionBounds(sel).Width / 2f));

        foreach (var c in sel)
            c.X = MathF.Round(center - (c.Width / 2f));

        SaveAfterControlAlignmentChange();
    }

    [RelayCommand]
    private void AlignRight()
    {
        var sel = GetSelectedControlsForAlignment();
        if (sel.Count < 2)
            return;

        TryPushActiveContextMenuSnapshot(SnapshotLabels.ControlsAligned);

        float right = sel.Max(c => c.X + c.Width);

        foreach (var c in sel)
            c.X = MathF.Round(right - c.Width);

        SaveAfterControlAlignmentChange();
    }

    [RelayCommand]
    private void AlignTop()
    {
        var sel = GetSelectedControlsForAlignment();
        if (sel.Count < 2)
            return;

        TryPushActiveContextMenuSnapshot(SnapshotLabels.ControlsAligned);

        float top = sel.Min(c => c.Y);

        foreach (var c in sel)
            c.Y = top;

        SaveAfterControlAlignmentChange();
    }

    [RelayCommand]
    private void AlignMiddle()
    {
        var sel = GetSelectedControlsForAlignment();
        if (sel.Count < 2)
            return;

        TryPushActiveContextMenuSnapshot(SnapshotLabels.ControlsAligned);

        float middle = (float)(GetSelectionBounds(sel).Top + (GetSelectionBounds(sel).Height / 2f));

        foreach (var c in sel)
            c.Y = MathF.Round(middle - (c.Height / 2f));

        SaveAfterControlAlignmentChange();
    }

    [RelayCommand]
    private void AlignBottom()
    {
        var sel = GetSelectedControlsForAlignment();
        if (sel.Count < 2)
            return;

        TryPushActiveContextMenuSnapshot(SnapshotLabels.ControlsAligned);

        float bottom = sel.Max(c => c.Y + c.Height);

        foreach (var c in sel)
            c.Y = MathF.Round(bottom - c.Height);

        SaveAfterControlAlignmentChange();
    }

    [RelayCommand]
    private void MakeSameWidth()
    {
        var sel = GetSelectedControlsForAlignment();
        if (sel.Count < 2)
            return;

        TryPushActiveContextMenuSnapshot(SnapshotLabels.ControlsAligned);

        float width = sel[0].Width;

        foreach (var c in sel.Skip(1))
            c.Width = width;

        SaveAfterControlAlignmentChange();
    }

    [RelayCommand]
    private void MakeSameHeight()
    {
        var sel = GetSelectedControlsForAlignment();
        if (sel.Count < 2)
            return;

        TryPushActiveContextMenuSnapshot(SnapshotLabels.ControlsAligned);

        float height = sel[0].Height;

        foreach (var c in sel.Skip(1))
            c.Height = height;

        SaveAfterControlAlignmentChange();
    }

    [RelayCommand]
    private void MakeSameSize()
    {
        var sel = GetSelectedControlsForAlignment();
        if (sel.Count < 2)
            return;

        TryPushActiveContextMenuSnapshot(SnapshotLabels.ControlsAligned);

        float width = sel[0].Width;
        float height = sel[0].Height;

        foreach (var c in sel.Skip(1))
        {
            c.Width = width;
            c.Height = height;
        }

        SaveAfterControlAlignmentChange();
    }

    [RelayCommand]
    private void SpaceHorEven()
    {
        var sel = GetSelectedControlsForAlignment()
            .OrderBy(c => c.X)
            .ToList();

        if (sel.Count < 3)
            return;

        TryPushActiveContextMenuSnapshot(SnapshotLabels.ControlsAligned);

        float left = sel.First().X;
        float right = sel.Last().X + sel.Last().Width;
        float totalWidth = sel.Sum(c => c.Width);
        float gap = (right - left - totalWidth) / (sel.Count - 1);

        float x = left;
        foreach (var c in sel)
        {
            c.X = MathF.Round(x);
            x += c.Width + gap;
        }

        SaveAfterControlAlignmentChange();
    }

    [RelayCommand]
    private void SpaceVerEven()
    {
        var sel = GetSelectedControlsForAlignment()
            .OrderBy(c => c.Y)
            .ToList();

        if (sel.Count < 3)
            return;

        TryPushActiveContextMenuSnapshot(SnapshotLabels.ControlsAligned);

        float top = sel.First().Y;
        float bottom = sel.Last().Y + sel.Last().Height;
        float totalHeight = sel.Sum(c => c.Height);
        float gap = (bottom - top - totalHeight) / (sel.Count - 1);

        float y = top;
        foreach (var c in sel)
        {
            c.Y = MathF.Round(y);
            y += c.Height + gap;
        }

        SaveAfterControlAlignmentChange();
    }

    [RelayCommand]
    private void StackHor()
    {
        var sel = GetSelectedControlsForAlignment()
            .OrderBy(c => c.X)
            .ToList();

        if (sel.Count < 2)
            return;

        TryPushActiveContextMenuSnapshot(SnapshotLabels.ControlsAligned);

        float x = sel.First().X;

        foreach (var c in sel)
        {
            c.X = MathF.Round(x);
            x += c.Width;
        }

        SaveAfterControlAlignmentChange();
    }

    [RelayCommand]
    private void StackVer()
    {
        var sel = GetSelectedControlsForAlignment()
            .OrderBy(c => c.Y)
            .ToList();

        if (sel.Count < 2)
            return;

        TryPushActiveContextMenuSnapshot(SnapshotLabels.ControlsAligned);

        float y = sel.First().Y;

        foreach (var c in sel)
        {
            c.Y = MathF.Round(y);
            y += c.Height;
        }

        SaveAfterControlAlignmentChange();
    }

    private List<DesignControl> GetSelectedControlsForAlignment()
    {
        if (SelectedControls == null || SelectedControls.Count == 0)
            return [];

        return SelectedControls
            .Where(c => c != null)
            .ToList();
    }

    private static Rect GetSelectionBounds(IReadOnlyList<DesignControl> controls)
    {
        if (controls.Count == 0)
            return Rect.Empty;

        float left = controls.Min(c => c.X);
        float top = controls.Min(c => c.Y);
        float right = controls.Max(c => c.X + c.Width);
        float bottom = controls.Max(c => c.Y + c.Height);

        return new Rect(left, top, right - left, bottom - top);
    }

    private void SaveAfterControlAlignmentChange()
    {
        MSG.UI.InvalidateDesigner();
        SaveActiveContextMenuSnapshotContextAfterRender();
    }

    #endregion

    #region === TEMPLATE COMMANDS (PLACEHOLDER) ===

    private bool CurrentTemplateNotNull() { return CurrentTemplate != null; }

    private bool TemplateCommandTargetExists(ScreenTemplate? template) { return template != null || CurrentTemplate != null; }

    [RelayCommand]
    private void NewTemplate()
    {
        MSG.UI.ShowOverlay();

        try
        {
            var newTemplate = new ScreenTemplate(
                IdGenerator.NewID,
                string.Empty,
                390,
                300
            )
            {
                GroupName = DEFAULT_TEMPLATE_GROUPNAME,
            };

            bool accepted = DialogService.EditEntity(
                newTemplate,
                _ => new TemplateDialog(),
                "NEW TEMPLATE"
            );

            if (!accepted)
                return;

            PushTemplatesSnapshot(SnapshotLabels.TemplateAdded, newTemplate.Id);
            Templates.Add(newTemplate);
            SaveTemplates();

            CurrentTemplate = Templates.FirstOrDefault(x => x.Id == newTemplate.Id);
            RefreshProjectUiInfo();
        }
        finally
        {
            MSG.UI.InvalidateDesigner();
            MSG.UI.HideOverlay();
        }
    }

    [RelayCommand(CanExecute = nameof(TemplateCommandTargetExists))]
    private void EditTemplate(ScreenTemplate? template)
    {
        template ??= CurrentTemplate;

        if (template == null)
            return;

        CurrentTemplate = template;

        MSG.UI.ShowOverlay();

        var oldId = CurrentTemplate.Id;

        try
        {
            var clone = CurrentTemplate.DeepClone();

            bool accepted = DialogService.EditEntity(
                clone,
                _ => new TemplateDialog(),
                "EDIT TEMPLATE"
            );

            if (!accepted)
                return;

            PushSnapshot(SnapshotContext.Template, SnapshotLabels.TemplateChanged);

            template.Name = clone.Name;
            template.Description = clone.Description;
            template.GroupName = clone.GroupName;
            template.Width = clone.Width;
            template.Height = clone.Height;

            template.Bands.Clear();
            foreach (var band in clone.Bands)
                template.Bands.Add(band.DeepClone());

            SaveTemplates();
            RefreshProjectUiInfo();

            CurrentTemplate = Templates.First(s => s.Id == oldId);
        }
        finally
        {
            MSG.UI.InvalidateDesigner();
            MSG.UI.HideOverlay();
        }
    }

    [RelayCommand(CanExecute = nameof(TemplateCommandTargetExists))]
    private void DeleteTemplate(ScreenTemplate? template)
    {
        template ??= CurrentTemplate;

        if (template == null)
            return;

        CurrentTemplate = template;

        var result = MessageBox.Show(
            $"Do you really want to delete this template?\n\n{template.Name}",
            "Delete Template",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result != MessageBoxResult.Yes)
            return;

        PushTemplatesSnapshot(SnapshotLabels.TemplateDeleted, template.Id);

        Templates.Remove(template);
        SaveTemplates();
        RefreshProjectUiInfo();

        CurrentTemplate = Templates.OrderBy(x => x.Name).FirstOrDefault();

        MSG.UI.InvalidateDesigner();
    }

    [RelayCommand(CanExecute = nameof(CurrentTemplateNotNull))]
    private void DuplicateTemplate()
    {
        var tpl = CurrentTemplate;
        if (tpl == null)
            return;

        XNotifications.Info($"DuplicateTemplate {tpl.Name} (TODO)");
    }

    #endregion

    #region === POPUP COMMANDS (PLACEHOLDER) ===

    private bool CurrentPopupNotNull() { return CurrentPopup != null; }

    private bool PopupCommandTargetExists(ScreenPopup? popup) { return popup != null || CurrentPopup != null; }

    [RelayCommand]
    private void NewPopup()
    {
        if (CurrentProject == null)
            return;

        MSG.UI.ShowOverlay();

        try
        {
            var newPopup = new ScreenPopup(
                IdGenerator.NewID,
                string.Empty,
                320,
                300
            )
            {
                GroupName = DEFAULT_POPUP_GROUPNAME,
                Position = ScreenPopupPosition.Center,
            };

            bool accepted = DialogService.EditEntity(newPopup, _ => new PopupDialog(), "NEW POPUP");

            if (!accepted)
                return;

            ApplyPopupDefaultSizeByPosition(newPopup);

            PushProjectSnapshot(SnapshotLabels.PopupAdded, newPopup.Id);
            CurrentProject.Popups.Add(newPopup);
            SaveCurrentProject();

            CurrentPopup = CurrentProject.Popups.FirstOrDefault(x => x.Id == newPopup.Id);
            RefreshProjectUiInfo();

            PopupDesignerHeight = newPopup.Height;
        }
        finally
        {
            MSG.UI.InvalidateDesigner();
            MSG.UI.HideOverlay();
        }
    }

    private void ApplyPopupDefaultSizeByPosition(ScreenPopup popup)
    {
        if (CurrentProject == null)
            return;

        float deviceWidth = CurrentProject.DeviceWidth;
        float deviceHeight = CurrentProject.DeviceHeight;

        switch (popup.Position)
        {
            case ScreenPopupPosition.Left:
            case ScreenPopupPosition.Right:
                popup.Width = MathF.Round(deviceWidth * 0.42f);
                popup.Height = deviceHeight;
                break;

            case ScreenPopupPosition.Top:
            case ScreenPopupPosition.Bottom:
                popup.Width = deviceWidth;
                popup.Height = MathF.Round(deviceHeight * 0.28f);
                break;

            case ScreenPopupPosition.Center:
                popup.Width = MathF.Round(Math.Min(deviceWidth * 0.86f, 420f));
                popup.Height = MathF.Round(Math.Min(deviceHeight * 0.72f, 420f));
                break;

            case ScreenPopupPosition.MousePos:
                popup.Width = MathF.Round(Math.Min(deviceWidth * 0.72f, 360f));
                popup.Height = MathF.Round(Math.Min(deviceHeight * 0.45f, 260f));
                break;

            default:
                popup.Width = MathF.Round(Math.Min(deviceWidth * 0.86f, 420f));
                popup.Height = MathF.Round(Math.Min(deviceHeight * 0.72f, 420f));
                break;
        }
    }

    [RelayCommand(CanExecute = nameof(PopupCommandTargetExists))]
    private void EditPopup(ScreenPopup? popup)
    {
        popup ??= CurrentPopup;

        if (CurrentProject == null || popup == null)
            return;

        CurrentPopup = popup;

        MSG.UI.ShowOverlay();

        var saveId = popup.Id;

        try
        {
            var clone = popup.DeepClone();
            var oldPosition = popup.Position;

            bool accepted = DialogService.EditEntity(clone, _ => new PopupDialog(), "EDIT POPUP");

            if (!accepted)
                return;

            PushSnapshot(SnapshotContext.Popup, SnapshotLabels.PopupChanged);

            if (clone.Position != oldPosition)
                ApplyPopupDefaultSizeByPosition(clone);

            popup.Name = clone.Name;
            popup.Title = clone.Title;
            popup.Position = clone.Position;
            popup.Width = clone.Width;
            popup.Height = clone.Height;
            popup.Description = clone.Description;
            popup.GroupName = clone.GroupName;
            popup.HasHeader = clone.HasHeader;
            popup.HeaderHeight = clone.HeaderHeight;

            var clonedBands = new ObservableCollection<Band>();
            foreach (var band in clone.Bands)
                clonedBands.Add(band.DeepClone());

            popup.Bands = clonedBands;

            PopupDesignerWidth = popup.Width;
            PopupDesignerHeight = popup.Height;

            SaveCurrentProject();
            RefreshProjectUiInfo();

            CurrentPopup = CurrentProject.Popups.FirstOrDefault(x => x.Id == saveId);

            if (CurrentPopup != null)
            {
                PopupDesignerWidth = CurrentPopup.Width;
                PopupDesignerHeight = CurrentPopup.Height;
            }
        }
        finally
        {
            MSG.UI.InvalidateDesigner();
            MSG.UI.HideOverlay();
        }
    }

    [RelayCommand(CanExecute = nameof(PopupCommandTargetExists))]
    private void DeletePopup(ScreenPopup? popup)
    {
        popup ??= CurrentPopup;

        if (CurrentProject == null || popup == null)
            return;

        CurrentPopup = popup;

        var result = MessageBox.Show(
            $"Do you really want to delete this popup?\n\n{popup.Name}",
            "Delete Popup",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result != MessageBoxResult.Yes)
            return;

        PushProjectSnapshot(SnapshotLabels.PopupDeleted, popup.Id);

        CurrentProject.Popups.Remove(popup);
        SaveCurrentProject();
        RefreshProjectUiInfo();

        CurrentPopup = CurrentProject.Popups.OrderBy(x => x.Name).FirstOrDefault();

        MSG.UI.InvalidateDesigner();
    }

    [RelayCommand(CanExecute = nameof(CurrentPopupNotNull))]
    private void DuplicatePopup()
    {
        var popup = CurrentPopup;
        if (popup == null)
            return;

        XNotifications.Info($"DuplicatePopup {popup.Name} (TODO)");
    }

    [RelayCommand(CanExecute = nameof(CurrentPopupNotNull))]
    private void SetPopupDockLeft() => SetPopupPosition(ScreenPopupPosition.Left);

    [RelayCommand(CanExecute = nameof(CurrentPopupNotNull))]
    private void SetPopupDockRight() => SetPopupPosition(ScreenPopupPosition.Right);

    [RelayCommand(CanExecute = nameof(CurrentPopupNotNull))]
    private void SetPopupDockTop() => SetPopupPosition(ScreenPopupPosition.Top);

    [RelayCommand(CanExecute = nameof(CurrentPopupNotNull))]
    private void SetPopupDockBottom() => SetPopupPosition(ScreenPopupPosition.Bottom);

    [RelayCommand(CanExecute = nameof(CurrentPopupNotNull))]
    private void SetPopupDockCenter() => SetPopupPosition(ScreenPopupPosition.Center);

    [RelayCommand(CanExecute = nameof(CurrentPopupNotNull))]
    private void SetPopupAtMousePos() => SetPopupPosition(ScreenPopupPosition.MousePos);

    private void SetPopupPosition(ScreenPopupPosition pos)
    {
        if (CurrentPopup == null)
            return;

        if (CurrentPopup.Position == pos)
            return;

        PushSnapshot(SnapshotContext.Popup, SnapshotLabels.PopupChanged);

        CurrentPopup.Position = pos;

        OnPropertyChanged(nameof(CurrentPopup));
        OnPropertyChanged(nameof(PopupDesignerWidth));
        OnPropertyChanged(nameof(PopupDesignerHeight));

        SaveCurrentProject();

        MSG.UI.InvalidateDesigner();
    }

    #endregion

    #region === CONTROL SELECTION HELPER ===

    private void ReplaceSelection(Band? band, IReadOnlyList<DesignControl>? controls)
    {
        foreach (var c in SelectedControls)
            c.IsSelected = false;

        SelectedControls.Clear();

        if (controls != null)
        {
            foreach (var c in controls)
            {
                c.IsSelected = true;
                SelectedControls.Add(c);
            }
        }

        CurrentControl = SelectedControls.Count == 1
            ? SelectedControls[0]
            : null;

        SetContextControls(band, SelectedControls.ToList());
    }

    private void ClearSelectionState()
    {
        foreach (var c in SelectedControls)
            c.IsSelected = false;

        SelectedControls.Clear();
        CurrentControl = null;
        ContextControls = null;
        ContextBand = null;
    }

    #endregion
}
