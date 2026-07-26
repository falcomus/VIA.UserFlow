using Mockup;
using Mockup.Resources;
using Mockup.ViewModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using VIA.WPF.Localization;
using VIA.WPF.Themes;


using System.Globalization;
using System.Text.Json;
namespace UserFlow;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private string? _logFilePath;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        XThemeService.Initialize();
        UserFlowThemeBridge.Initialize();


        ApplyStartupLanguage();
        InitializeCrashLogging();
        WriteLog("Application startup.");

        try
        {
            // Schalte lästige, ungefährliche Binding Fehlermeldungen ab!
            // PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Critical;

            SplashWindow? splash = null;

            try
            {
                splash = new SplashWindow();
                splash.Show();
                UpdateSplash(splash, Localize("Splash.StatusStarting", "UserFlow is starting..."), 2);

                // 1) EIN globales ViewModel erzeugen
                UpdateSplash(splash, Localize("Splash.StatusPreparingViewModel", "Preparing ViewModel..."), 4);
                MockupService.Mockup = new MockupViewModel();
                WriteLog("MockupViewModel created.");

                var progress = new InlineProgress<StartupProgress>(p =>
                {
                    UpdateSplash(splash, p.Message, p.Percent);
                    WriteLog($"Startup progress: {p.Message} ({p.Percent:0.#}%)");
                });

                // 2) Storage laden
                MockupService.Mockup.LoadAll(progress);

                XLocalizationService.Current.LanguageChanged -= StoreSelectedLanguage;
                XLocalizationService.Current.LanguageChanged += StoreSelectedLanguage;
                WriteLog("LoadAll finished.");

                // 3) MainWindow erstellen
                UpdateSplash(splash, Localize("Splash.StatusPreparingMainWindow", "Preparing main window..."), 99);
                var main = new MainWindow
                {
                    DataContext = MockupService.Mockup,
                };

                main.Show();
                WriteLog("MainWindow shown.");

                splash.Close();
            }
            finally
            {
                splash?.Close();
            }
        }
        catch (Exception ex)
        {
            LogException("Exception during OnStartup", ex);
            throw;
        }
    }


    private static string Localize(string key, string fallbackText)
    {
        return XLocalizationService.Current.GetString(
            UserFlowResources.ResourceManager,
            key,
            fallbackText);
    }


    private static void ApplyStartupLanguage()
    {
        string? configuredCultureName = ReadConfiguredLanguageCultureName();

        CultureInfo culture = CultureInfo.CurrentUICulture;

        if (!string.IsNullOrWhiteSpace(configuredCultureName))
        {
            try
            {
                culture = CultureInfo.GetCultureInfo(configuredCultureName);
            }
            catch (CultureNotFoundException)
            {
                culture = CultureInfo.CurrentUICulture;
            }
        }

        XLocalizationService.Current.SetCulture(culture, applyFormattingCulture: true);
    }

    private static string? ReadConfiguredLanguageCultureName()
    {
        try
        {
            string settingsFilePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Data",
                "settings.json");

            if (!File.Exists(settingsFilePath))
                return null;

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(settingsFilePath));

            if (!document.RootElement.TryGetProperty("UI", out JsonElement uiElement))
                return null;

            if (!uiElement.TryGetProperty("LanguageCultureName", out JsonElement cultureElement))
                return null;

            return cultureElement.ValueKind == JsonValueKind.String
                ? cultureElement.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private void StoreSelectedLanguage(object? sender, XLanguageChangedEventArgs e)
    {
        try
        {
            if (MockupService.Mockup is null)
                return;

            MockupService.Mockup.SetConfiguredLanguageCultureName(e.CurrentCulture.Name);
            WriteLog($"Language selected: {e.CurrentCulture.Name}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Store selected language failed: {ex}");
        }
    }

    private static void UpdateSplash(SplashWindow splash, string message, double? percent)
    {
        splash.UpdateStatus(message, percent);
        splash.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);
    }


    private sealed class InlineProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        WriteLog("Application exit started.");

        try
        {
            MockupService.Mockup.Shutdown(); // AutoSave Timer freigeben
            WriteLog("Shutdown finished.");
        }
        catch (Exception ex)
        {
            LogException("Shutdown failed", ex);
        }

        // 4) Alles Speichern beim Exit
        try
        {
            MockupService.Mockup.SaveAll();
            WriteLog("SaveAll finished.");
        }
        catch (Exception ex)
        {
            LogException("SaveAll on exit failed", ex);
            Debug.WriteLine("SaveAll on exit failed: " + ex);
        }

        WriteLog("Application exit completed.");
        base.OnExit(e);
    }

    #region === Crash Logging ===

    private void InitializeCrashLogging()
    {
        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string logDir = Path.Combine(baseDir, "Logs");
            Directory.CreateDirectory(logDir);

            _logFilePath = Path.Combine(
                logDir,
                $"crash_{DateTime.Now:yyyyMMdd}.log");

            WriteLog("Crash logging initialized.");
            WriteLog($"BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}");
            WriteLog($"Process: {Environment.ProcessPath}");
            WriteLog($"OS: {Environment.OSVersion}");
            WriteLog($".NET: {Environment.Version}");
            WriteLog($"Machine: {Environment.MachineName}");
            WriteLog($"User: {Environment.UserName}");

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("InitializeCrashLogging failed: " + ex);
        }
    }

    private void App_DispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        LogException("DispatcherUnhandledException", e.Exception);

        // Aktuell nur loggen, nicht unterdrücken.
        e.Handled = false;
    }

    private void CurrentDomain_UnhandledException(
        object? sender,
        UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogException(
                    $"AppDomain.CurrentDomain.UnhandledException (IsTerminating={e.IsTerminating})",
                    ex);
            }
            else
            {
                WriteLog(
                    $"[FATAL] AppDomain.CurrentDomain.UnhandledException with non-Exception object. IsTerminating={e.IsTerminating}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("CurrentDomain_UnhandledException logging failed: " + ex);
        }
    }

    private void TaskScheduler_UnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs e)
    {
        LogException("TaskScheduler.UnobservedTaskException", e.Exception);

        // Nur loggen; optional könnte man hier e.SetObserved() setzen.
    }

    private void LogException(string context, Exception ex)
    {
        try
        {
            var sb = new StringBuilder();

            sb.AppendLine(new string('=', 100));
            sb.AppendLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {context}");
            sb.AppendLine(new string('-', 100));

            AppendException(sb, ex, 0);

            sb.AppendLine(new string('=', 100));
            sb.AppendLine();

            WriteRaw(sb.ToString());
            Debug.WriteLine(sb.ToString());
        }
        catch (Exception logEx)
        {
            Debug.WriteLine("LogException failed: " + logEx);
            Debug.WriteLine("Original exception: " + ex);
        }
    }

    private static void AppendException(StringBuilder sb, Exception ex, int level)
    {
        string indent = new string(' ', level * 2);

        sb.AppendLine($"{indent}Type: {ex.GetType().FullName}");
        sb.AppendLine($"{indent}Message: {ex.Message}");
        sb.AppendLine($"{indent}Source: {ex.Source}");
        sb.AppendLine($"{indent}TargetSite: {ex.TargetSite}");
        sb.AppendLine($"{indent}StackTrace:");
        sb.AppendLine($"{indent}{ex.StackTrace}");

        if (ex.Data.Count > 0)
        {
            sb.AppendLine($"{indent}Data:");
            foreach (var key in ex.Data.Keys)
                sb.AppendLine($"{indent}  {key}: {ex.Data[key]}");
        }

        if (ex.InnerException != null)
        {
            sb.AppendLine();
            sb.AppendLine($"{indent}InnerException:");
            AppendException(sb, ex.InnerException, level + 1);
        }
    }

    private void WriteLog(string message)
    {
        string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {message}{Environment.NewLine}";
        WriteRaw(line);
        Debug.WriteLine(line);
    }

    private void WriteRaw(string text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_logFilePath))
                return;

            File.AppendAllText(_logFilePath, text, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("WriteRaw failed: " + ex);
        }
    }
}

    #endregion








//using Mockup;
//using Mockup.ViewModel;
//using System.Diagnostics;
//using System.Windows;

//namespace UserFlow;

///// <summary>
///// Interaction logic for App.xaml
///// </summary>
//public partial class App : Application
//{
//    protected override void OnStartup(StartupEventArgs e)
//    {
//        base.OnStartup(e);

//        // Schalte lästige, ungefährliche Binding Fehlermeldungen ab!
//        //PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Critical;

//        // 1) EIN globales ViewModel erzeugen
//        MockupService.Mockup = new MockupViewModel();

//        // 2) Storage laden
//        MockupService.Mockup.LoadAll();

//        // 3) MainWindow erstellen
//        var main = new MainWindow
//        {
//            DataContext = MockupService.Mockup,
//        };

//        main.Show();
//    }

//
//private static void UpdateSplash(SplashWindow splash, string message, double? percent)
//{
//    splash.UpdateStatus(message, percent);
//    splash.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);
//}


//private sealed class InlineProgress<T>(Action<T> handler) : IProgress<T>
//{
//    public void Report(T value) => handler(value);
//}

//protected override void OnExit(ExitEventArgs e)
//    {
//        try
//        {
//            MockupService.Mockup.Shutdown(); // AutoSave Timer freigeben
//        }
//        catch { }

//        // 4) Alles Speichern beim Exit
//        try
//        {
//            MockupService.Mockup.SaveAll();
//        }
//        catch (Exception ex)
//        {
//            // Nicht blockieren – aber Fehler loggen
//            Debug.WriteLine("SaveAll on exit failed: " + ex);
//        }

//        base.OnExit(e);
//    }
//}
