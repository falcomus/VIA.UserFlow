// ======================================================================================
// FILE: Mockup.ViewModel/MockupViewModel.Storage.cs
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.JsonConverters;
using Mockup.Messages;
using Mockup.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace Mockup.ViewModel;

public sealed record StartupProgress(string Message, double? Percent = null);

public partial class MockupViewModel : ObservableObject
{
    #region === DATA PATHS ===

    private readonly string _dataRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

    private string ProjectsFolder => Path.Combine(_dataRoot, "Projects");
    private string TemplatesFolder => Path.Combine(_dataRoot, "Templates");

    private string SettingsFilePath => Path.Combine(_dataRoot, "settings.json");
    private string TemplatesFilePath => Path.Combine(TemplatesFolder, "templates.json");

    #endregion === DATA PATHS ===

    #region === SETTINGS ===

    [ObservableProperty]
    private AppSettings settings = AppSettings.CreateDefault();

    public sealed class AppSettings
    {
        public DesignerSettings Designer { get; set; } = new();
        public UISettings UI { get; set; } = new();
        public StorageSettings Storage { get; set; } = new();

        public static AppSettings CreateDefault() => new();
    }

    public sealed class DesignerSettings
    {
        public bool AutoSaveEnabled { get; set; } = true;
        public int AutoSaveIntervalMinutes { get; set; } = 2;
    }

    public sealed class UISettings
    {
        public bool ShowToolbox { get; set; } = true;

        public int MainTabSelectedIndex { get; set; } = 0;
        public int ScreenTabSelectedIndex { get; set; } = 0;

        public ObservableCollection<string> RecentColors { get; set; } = [];
    }

    public sealed class StorageSettings
    {
        public bool AutoLoadLastProject { get; set; } = true;
        public string? LastOpenedProjectPath { get; set; }
    }

    #endregion === SETTINGS ===

    #region === AUTOSAVE ===

    private DispatcherTimer? _autoSaveTimer;
    private bool _suppressProjectFileSelectionChanged;

    #endregion === AUTOSAVE ===

    #region === LOAD / SAVE ALL ===

    public void LoadAll(IProgress<StartupProgress>? progress = null)
    {
        progress?.Report(new StartupProgress("Preparing data folder...", 5));
        EnsureDataFolders();

        progress?.Report(new StartupProgress("Loading settings...", 10));
        LoadSettings();

        progress?.Report(new StartupProgress("Initializing color scheme...", 18));
        InitializeThemeService();

        progress?.Report(new StartupProgress("Preparing autosave...", 24));
        SetupAutoSaveTimer();

        progress?.Report(new StartupProgress("Loading last project...", 32));
        AutoLoadLastProject(progress);

        progress?.Report(new StartupProgress("Loading templates...", 72));
        LoadTemplates(progress);

        progress?.Report(new StartupProgress("Preparing toolbox...", 84));
        RebuildControlGroups();

        progress?.Report(new StartupProgress("Registering messages...", 90));
        RegisterMessages();

        if (CurrentProject == null)
        {
            progress?.Report(new StartupProgress("\n\nStartup complete.", 100));
            InitializeSnapshots();
            return;
        }

        progress?.Report(new StartupProgress("Preparing startup selection...", 94));
        CurrentScreen = CurrentProject.Screens.FirstOrDefault();

        HomeScreen = CurrentProject?.Screens.FirstOrDefault(x => x.IsHomeScreen);

        if (CurrentTemplate == null && Templates.Any())
            CurrentTemplate = Templates.FirstOrDefault();

        progress?.Report(new StartupProgress("Preparing Undo/Redo...\n\n", 98));

        //Snapshot Library initialisieren
        InitializeSnapshots();

        progress?.Report(new StartupProgress("\n\nStartup complete.", 100));
    }

    public void SaveAll()
    {
        try
        {
            EnsureDataFolders();
            SaveSettings();
            SaveTemplates();
            SaveCurrentProject();
        }
        catch (Exception ex)
        {
            XNotifications.Error($"Automatic saving failed:\n{ex.Message}");
        }
        finally
        {
            XNotifications.Success("Automatic saving completed successfully.");
        }
    }

    #endregion === LOAD / SAVE ALL ===

    #region === SETTINGS IO ===

    public const int RECENT_COLORS_COUNT = 15;
    public const string RECENT_COLOR_EMPTY = "#00000000";

    private void LoadSettings()
    {
        if (!File.Exists(SettingsFilePath))
        {
            Settings = AppSettings.CreateDefault();
            ApplySettingsToViewModel();
            EnsureRecentColorsCount();
            SaveSettings();
            return;
        }

        try
        {
            Settings = JsonSerializer.Deserialize<AppSettings>(
                File.ReadAllText(SettingsFilePath),
                JsonOptions
            ) ?? AppSettings.CreateDefault();

            ApplySettingsToViewModel();
            EnsureRecentColorsCount();
        }
        catch
        {
            Settings = AppSettings.CreateDefault();
            ApplySettingsToViewModel();
            EnsureRecentColorsCount();
        }
    }

    private void SaveSettings()
    {
        try
        {
            EnsureRecentColorsCount();
            ApplyViewModelToSettings();

            File.WriteAllText(
                SettingsFilePath,
                JsonSerializer.Serialize(Settings, JsonOptions));
        }
        catch
        {
        }
    }

    private void ApplySettingsToViewModel()
    {
        AutoSaveEnabled = Settings.Designer.AutoSaveEnabled;
        AutoSaveIntervalMinutes = Settings.Designer.AutoSaveIntervalMinutes;
        ShowToolbox = Settings.UI.ShowToolbox;
        MainTabSelectedIndex = Settings.UI.MainTabSelectedIndex;
        ScreenTabSelectedIndex = Settings.UI.ScreenTabSelectedIndex;
        OpenLastProjectOnStartup = Settings.Storage.AutoLoadLastProject;

        RecentColors.Clear();
        foreach (var hex in Settings.UI.RecentColors ?? new ObservableCollection<string>())
        {
            if (!string.IsNullOrWhiteSpace(hex))
                RecentColors.Add(hex.Trim());
        }

        EnsureRecentColorsCount();
    }

    private void ApplyViewModelToSettings()
    {
        Settings.Designer.AutoSaveEnabled = AutoSaveEnabled;
        Settings.Designer.AutoSaveIntervalMinutes = AutoSaveIntervalMinutes;
        Settings.UI.ShowToolbox = ShowToolbox;
        Settings.UI.MainTabSelectedIndex = MainTabSelectedIndex;
        Settings.UI.ScreenTabSelectedIndex = ScreenTabSelectedIndex;
        Settings.Storage.AutoLoadLastProject = OpenLastProjectOnStartup;

        Settings.UI.RecentColors = new ObservableCollection<string>(
            RecentColors
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim()));
    }

    private void EnsureRecentColorsCount()
    {
        if (RecentColors.Count == 0)
        {
            for (int i = 0; i < RECENT_COLORS_COUNT; i++)
                RecentColors.Add(RECENT_COLOR_EMPTY);
            return;
        }

        while (RecentColors.Count < RECENT_COLORS_COUNT)
            RecentColors.Add(RECENT_COLOR_EMPTY);

        while (RecentColors.Count > RECENT_COLORS_COUNT)
            RecentColors.RemoveAt(RecentColors.Count - 1);
    }

    #endregion === SETTINGS IO ===

    #region === PROJECT IO ===

    public void LoadProject(string filePath, IProgress<StartupProgress>? progress = null)
    {
        if (!File.Exists(filePath))
            return;

        bool showProjectLoading = progress == null;
        string projectName = Path.GetFileNameWithoutExtension(filePath);

        try
        {
            if (showProjectLoading)
                MSG.UI.ShowProjectLoading(projectName);

            progress?.Report(new StartupProgress($"Read project file: {projectName} ...", 36));
            string json = File.ReadAllText(filePath);

            progress?.Report(new StartupProgress($"Deserialize project: {projectName} ...", 46));
            var project = JsonSerializer.Deserialize<Project>(
                json,
                JsonOptions);

            if (project == null)
                return;

            progress?.Report(new StartupProgress("Check project structure ...", 54));
            MakeProjectCorrections(project);

            project.FilePath = filePath;

            int screenCount = Math.Max(project.Screens.Count, 1);
            int screenIndex = 0;

            foreach (var screen in project.Screens)
            {
                screenIndex++;
                double percent = 56 + (screenIndex / (double)screenCount * 10);
                progress?.Report(new StartupProgress($"Reconstruct Screens ({screenIndex}/{screenCount}) ...", percent));
                screen.Reconstruct(project);
            }

            progress?.Report(new StartupProgress("Apply project ...", 67));
            CurrentProject = project;

            progress?.Report(new StartupProgress("Apply project theme ...", 69));
            CurrentProject.InitializeTheme();

            CurrentScreen = project.Screens.FirstOrDefault();

            HomeScreen = CurrentProject?.Screens.FirstOrDefault(x => x.IsHomeScreen);

            if (HomeScreen == null)
            {
                HomeScreen = project.Screens.FirstOrDefault();
                if (HomeScreen != null)
                    SetAsHomeScreen(HomeScreen);
            }

            progress?.Report(new StartupProgress("Update project list ...", 70));
            RememberLastOpenedProject(filePath);
            RefreshProjectFiles();

            _suppressProjectFileSelectionChanged = true;
            try
            {
                CurrentProjectFile = ProjectFiles.FirstOrDefault(x =>
                    string.Equals(
                        Path.GetFullPath(x.FullPath),
                        Path.GetFullPath(filePath),
                        StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                _suppressProjectFileSelectionChanged = false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadProject failed: {ex}");
        }
        finally
        {
            if (showProjectLoading)
                MSG.UI.HideProjectLoading();
        }
    }

    public void SaveCurrentProject()
    {
        if (CurrentProject == null || string.IsNullOrWhiteSpace(CurrentProject.FilePath))
            return;

        SaveProject(CurrentProject);
    }

    public void SaveProject(Project project, bool refreshProjectFiles = true)
    {
        MakeProjectCorrections(project);

        try
        {
            File.WriteAllText(
                project.FilePath!,
                JsonSerializer.Serialize(project, JsonOptions));

            RememberLastOpenedProject(project.FilePath);
            if (!refreshProjectFiles)
                return;

            RefreshProjectFiles();

            _suppressProjectFileSelectionChanged = true;
            try
            {
                CurrentProjectFile = ProjectFiles.FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(project.FilePath)
                    && string.Equals(
                        Path.GetFullPath(x.FullPath),
                        Path.GetFullPath(project.FilePath),
                        StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                _suppressProjectFileSelectionChanged = false;
            }
        }
        catch
        {
        }
    }

    //////////////////////
    // EINIGE KORREKTUREN
    //////////////////////
    private void MakeProjectCorrections(Project project)
    {
        if (project == null)
            return;

        Screen? homeScreen = project.Screens.FirstOrDefault(screen => screen.IsHomeScreen);

        foreach (var screen in project.Screens)
        {
            screen.IsHomeScreen = ReferenceEquals(screen, homeScreen);

            foreach (Band band in screen.Bands)
            {
                if (!band.IsExpandable)
                {
                    band.IsExpanded = false;
                }
                else
                {
                    band.SavedExpandedHeight = Math.Max(band.SavedExpandedHeight, Screen.DefaultBandHeaderHeight);
                    foreach (BandPage page in band.Pages)
                    {
                        page.Height = Math.Max(page.Height, band.SavedExpandedHeight);
                    }
                }
            }
        }

        if (homeScreen == null)
        {
            homeScreen = project.Screens.FirstOrDefault(s => s.GroupName == MockupViewModel.DEFAULT_SCREEN_GROUPNAME)
                         ?? project.Screens.FirstOrDefault();

            if (homeScreen != null)
                homeScreen.IsHomeScreen = true;
        }
    }

    private void AutoLoadLastProject(IProgress<StartupProgress>? progress = null)
    {
        #region DIESE VERSION NUTZEN, FALLS COMBOBOX MIT PROJEKTEN WIEDER ANGEZEIGT WERDEN SOLL
        //try
        //{
        //    if (!Settings.Storage.AutoLoadLastProject)
        //        return;

        //    var path = ResolveProjectPath(Settings.Storage.LastOpenedProjectPath);
        //    if (string.IsNullOrWhiteSpace(path))
        //        return;

        //    if (!File.Exists(path))
        //    {
        //        Settings.Storage.LastOpenedProjectPath = null;
        //        SaveSettings();
        //        return;
        //    }

        //    var entry = ProjectFiles.FirstOrDefault(x =>
        //        string.Equals(
        //            Path.GetFullPath(x.FullPath),
        //            Path.GetFullPath(path),
        //            StringComparison.OrdinalIgnoreCase));

        //    if (entry == null)
        //        return;

        //    _suppressProjectFileSelectionChanged = true;
        //    try
        //    {
        //        CurrentProjectFile = entry;
        //    }
        //    finally
        //    {
        //        _suppressProjectFileSelectionChanged = false;
        //    }

        //    LoadProject(entry.FullPath, progress);
        //    RefreshProjectUiInfo();
        //}
        //catch (Exception ex)
        //{
        //    Debug.WriteLine($"AutoLoadLastProject failed: {ex}");
        //}
        #endregion

        try
        {
            if (!Settings.Storage.AutoLoadLastProject)
                return;

            var path = ResolveProjectPath(Settings.Storage.LastOpenedProjectPath);

            if (string.IsNullOrWhiteSpace(path))
                return;

            progress?.Report(new StartupProgress($"Open last project: {Path.GetFileNameWithoutExtension(path)} ...", 34));
            LoadProject(path, progress);

            progress?.Report(new StartupProgress("Update project UI ...", 71));
            RefreshProjectUiInfo();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AutoLoadLastProject failed: {ex}");
        }
    }

    private string? MakeRelativeToDataRoot(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        var fullPath = Path.GetFullPath(filePath);
        var fullDataRoot = Path.GetFullPath(_dataRoot);

        if (!fullPath.StartsWith(fullDataRoot, StringComparison.OrdinalIgnoreCase))
            return fullPath;

        return Path.GetRelativePath(fullDataRoot, fullPath);
    }

    private string? ResolveProjectPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);

        return Path.GetFullPath(Path.Combine(_dataRoot, path));
    }


    private void RememberLastOpenedProject(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        Settings.Storage.LastOpenedProjectPath = MakeRelativeToDataRoot(filePath);
        SaveSettings();
    }

    private void ClearLastOpenedProjectIfMatches(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        if (string.IsNullOrWhiteSpace(Settings.Storage.LastOpenedProjectPath))
            return;

        if (!string.Equals(
                Path.GetFullPath(Settings.Storage.LastOpenedProjectPath),
                Path.GetFullPath(filePath),
                StringComparison.OrdinalIgnoreCase))
            return;

        Settings.Storage.LastOpenedProjectPath = null;
        SaveSettings();
    }

    #endregion === PROJECT IO ===

    #region === TEMPLATES IO (CLEAN & ISOLATED) ===

    private void LoadTemplates(IProgress<StartupProgress>? progress = null)
    {
        Templates.Clear();

        if (!File.Exists(TemplatesFilePath))
            return;

        try
        {
            progress?.Report(new StartupProgress("Reading Templates ...", 73));
            var templates = JsonSerializer.Deserialize<ScreenTemplate[]>(
                File.ReadAllText(TemplatesFilePath),
                JsonOptions) ?? Array.Empty<ScreenTemplate>();

            int templateCount = Math.Max(templates.Length, 1);
            int templateIndex = 0;

            foreach (var template in templates)
            {
                templateIndex++;
                double percent = 74 + (templateIndex / (double)templateCount * 6);
                progress?.Report(new StartupProgress($"Preparing Templates ({templateIndex}/{templateCount}) ...", percent));

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

                Templates.Add(template);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadTemplates failed: {ex}");
        }
    }

    private void SaveTemplates()
    {
        try
        {
            foreach (var template in Templates)
            {
                foreach (var band in template.Bands)
                {
                    foreach (var page in band.Pages)
                    {
                        foreach (var ctrl in page.Controls)
                            ctrl.TypeKey = ctrl.GetType().Name;
                    }
                }
            }

            File.WriteAllText(
                TemplatesFilePath,
                JsonSerializer.Serialize(Templates.ToArray(), JsonOptions));
        }
        catch
        {
        }
    }

    #endregion === TEMPLATES IO ===

    #region === HELPERS ===

    private void EnsureDataFolders()
    {
        Directory.CreateDirectory(_dataRoot);
        Directory.CreateDirectory(ProjectsFolder);
        Directory.CreateDirectory(TemplatesFolder);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "Project" : safe;
    }

    public void OpenSelectedProjectFile()
    {
        if (CurrentProjectFile == null)
            return;

        if (!File.Exists(CurrentProjectFile.FullPath))
        {
            XNotifications.Warning($"The project file does not exist anymore: {CurrentProjectFile.FullPath}");
            RefreshProjectFiles();
            return;
        }

        LoadProject(CurrentProjectFile.FullPath);
        RefreshProjectUiInfo();
    }

    public void CopySelectedProjectFile(ProjectFileEntry item)
    {
        if (item == null || !File.Exists(item.FullPath))
            return;

        string sourcePath = item.FullPath;
        string sourceName = Path.GetFileNameWithoutExtension(sourcePath);
        string extension = Path.GetExtension(sourcePath);
        string copyPath = BuildUniqueProjectCopyPath(sourceName, extension);

        File.Copy(sourcePath, copyPath, overwrite: false);
        RefreshProjectFiles();
    }

    public void DeleteSelectedProjectFile()
    {
        if (CurrentProjectFile == null)
            return;

        var result = XDialogs.Show(
            $"Delete project '{CurrentProjectFile.DisplayName}'? This will permanently delete the project file.",
            "Delete Project",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        var fullPath = CurrentProjectFile.FullPath;

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        ClearLastOpenedProjectIfMatches(fullPath);

        if (CurrentProject != null
            && !string.IsNullOrWhiteSpace(CurrentProject.FilePath)
            && string.Equals(Path.GetFullPath(CurrentProject.FilePath), Path.GetFullPath(fullPath), StringComparison.OrdinalIgnoreCase))
        {
            CurrentProject = null;
            CurrentScreen = null;
            HomeScreen = null;
            CurrentPopup = null;
        }

        RefreshProjectFiles();
        RefreshProjectUiInfo();
    }

    private string BuildUniqueProjectCopyPath(string baseName, string extension)
    {
        string candidate = Path.Combine(ProjectsFolder, $"{SanitizeFileName(baseName)} - Copy{extension}");
        int index = 2;

        while (File.Exists(candidate))
        {
            candidate = Path.Combine(ProjectsFolder, $"{SanitizeFileName(baseName)} - Copy {index}{extension}");
            index++;
        }

        return candidate;
    }

    private void InitializeThemeService()
    {
        try
        {
            ThemeService.InitializeCatalog(
                Path.Combine(_dataRoot, "colorSchemas.json"));
        }
        catch
        {
        }
    }

    private void SetupAutoSaveTimer()
    {
        _autoSaveTimer?.Stop();

        if (!Settings.Designer.AutoSaveEnabled)
            return;

        _autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(Settings.Designer.AutoSaveIntervalMinutes)
        };

        _autoSaveTimer.Tick += (_, _) => SaveAll();
        _autoSaveTimer.Start();
    }

    public void Shutdown()
    {
        try
        {
            _autoSaveTimer?.Stop();
            _autoSaveTimer = null;
            SaveAll();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Shutdown failed: {ex}");
        }
    }

    #endregion === HELPERS ===

    #region === JSON OPTIONS ===

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true,
        };

        options.Converters.Add(new ProjectConverter());
        options.Converters.Add(new ScreenConverter());
        options.Converters.Add(new TemplateConverter());
        options.Converters.Add(new BandConverter());
        options.Converters.Add(new BandPageConverter());
        options.Converters.Add(new DesignControlConverter());
        options.Converters.Add(new ColorJsonConverter());

        return options;
    }

    private static readonly JsonSerializerOptions _jsonOptions = CreateJsonOptions();
    public static JsonSerializerOptions JsonOptions => _jsonOptions;

    #endregion === JSON OPTIONS ===
}
