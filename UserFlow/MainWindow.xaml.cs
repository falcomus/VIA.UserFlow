using CommunityToolkit.Mvvm.Messaging;
using Mockup.Actions;
using Mockup.Helper;
using Mockup.Messages;
using Mockup.Resources;
using Mockup.Services;
using Mockup.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using VIA.WPF.Localization;

namespace UserFlow;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private SplashWindow? projectLoadingWindow;

    #region === VIEWMODEL ACCESSOR ===

    public MockupViewModel VM => (MockupViewModel)DataContext;

    #endregion

    #region === CTOR / INIT ===

    public MainWindow()
    {
        InitializeComponent();

        if (DesignModeHelper.IsInDesignMode)
            return;

        Loaded += OnLoaded;

        WeakReferenceMessenger.Default.UnregisterAll(this);

        WeakReferenceMessenger.Default.Register<ShowHideOverlayMessage>(
            this,
            (_, msg) =>
            {
                PART_Overlay.Visibility = msg.Value ? Visibility.Visible : Visibility.Collapsed;
            }
        );

        WeakReferenceMessenger.Default.Register<ShowProjectLoadingMessage>(
            this,
            (_, message) => ShowProjectLoading(message.ProjectName));

        WeakReferenceMessenger.Default.Register<HideProjectLoadingMessage>(
            this,
            (_, _) => HideProjectLoading());

        WeakReferenceMessenger.Default.Register<ActionAreaEditMessage>(
            this,
            (_, msg) =>
            {
                Dispatcher.Invoke(() => OpenActionAreaEditor(msg.Value));
            }
        );
    }

    #endregion

    #region === LOADED ===

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Maximized;
        VM.MainTabSelectedIndex = 0;
        ApplyCaptionButtonIcons();
        StateChanged += (_, _) => ApplyCaptionButtonIcons();
    }

    private void ApplyCaptionButtonIcons()
    {
        SetCaptionIcon("PART_MinimizeButton", "M 1 7.5 L 11 7.5");
        SetCaptionIcon(
            "PART_MaximizeRestoreButton",
            WindowState == WindowState.Maximized
                ? "M 3.5 1.5 H 10.5 V 8.5 H 3.5 Z M 1.5 3.5 H 8.5 V 10.5 H 1.5 Z"
                : "M 1.5 1.5 H 10.5 V 10.5 H 1.5 Z");
        SetCaptionIcon("PART_CloseButton", "M 2 2 L 10 10 M 10 2 L 2 10");
    }

    private void SetCaptionIcon(string partName, string geometry)
    {
        if (Template.FindName(partName, this) is not Button button)
            return;

        var path = new Path
        {
            Data = Geometry.Parse(geometry),
            Height = 12,
            Width = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Stretch = Stretch.None,
            StrokeThickness = 1.25,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
        };

        path.SetBinding(
            Shape.StrokeProperty,
            new Binding(nameof(Control.Foreground))
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Button), 1),
            });

        button.Content = path;
    }

    private void ShowProjectLoading(string projectName)
    {
        Dispatcher.Invoke(() =>
        {
            string heading = XLocalizationService.Current.GetString(
                UserFlowResources.ResourceManager,
                "Main.ProjectLoadingHeading",
                "Loading project");

            string status = XLocalizationService.Current.Format(
                UserFlowResources.ResourceManager,
                "Main.ProjectLoadingStatus",
                "Loading {0}...",
                projectName);

            if (projectLoadingWindow is not null)
            {
                projectLoadingWindow.UpdateStatus(status, null);
                return;
            }

            MSG.UI.ShowOverlay();

            projectLoadingWindow = new SplashWindow
            {
                Owner = this,
            };
            projectLoadingWindow.SetHeading(heading);
            projectLoadingWindow.UpdateStatus(status, null);
            projectLoadingWindow.Show();

            Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
        });
    }

    private void HideProjectLoading()
    {
        Dispatcher.Invoke(() =>
        {
            if (projectLoadingWindow is null)
                return;

            projectLoadingWindow.Close();
            projectLoadingWindow = null;
            MSG.UI.HideOverlay();
        });
    }

    #endregion

    #region === LOCALIZATION ===

    private static string Localize(string key, string fallbackText)
    {
        return XLocalizationService.Current.GetString(
            UserFlowResources.ResourceManager,
            key,
            fallbackText);
    }

    private static string FormatLocalize(string key, string fallbackText, params object[] args)
    {
        return XLocalizationService.Current.Format(
            UserFlowResources.ResourceManager,
            key,
            fallbackText,
            args);
    }

    #endregion
    #region === ACTION AREA EDITOR ===

    private void OpenActionAreaEditor(ActionArea area)
    {
        if (VM == null)
            return;

        if (VM.CurrentProject == null)
            return;

        var editorVm = new ActionAreaEditorViewModel(
            area,
            VM.CurrentProject.Screens,
            beforeApply: () => VM.PushActionAreaChangedSnapshot());

        var wnd = new ActionAreaEditor
        {
            Owner = this,
            DataContext = editorVm,
        };

        try
        {
            MSG.UI.ShowOverlay();
            bool? accepted = wnd.ShowDialog();

            if (accepted == true && editorVm.HasAppliedChanges)
            {
                VM.SaveCurrentSnapshotContext();

                MSG.UI.InvalidateDesigner();
            }
        }
        finally
        {
            MSG.UI.HideOverlay();
        }
    }

    #endregion

    #region === WINDOW EVENTS ===

    private void Window_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
    }

    #endregion

    #region === DRAG DROP OF PROJECT FILE ===

    private void Window_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effects = DragDropEffects.Copy;
        else
            e.Effects = DragDropEffects.None;

        e.Handled = true;
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effects = DragDropEffects.Copy;
        else
            e.Effects = DragDropEffects.None;

        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (string file in files)
                ProcessDroppedFile(file);
        }

        e.Handled = true;
    }

    private void ProcessDroppedFile(string filePath)
    {
        if (VM is null)
            return;

        string extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();

        switch (extension)
        {
            case ".ufp":
                VM.LoadProject(filePath);
                break;

            default:
                XNotifications.Error(
                    XLocalizationService.Current.Format(
                        UserFlowResources.ResourceManager,
                        "Main.UnsupportedFileType",
                        "File type is not supported: {0}",
                        extension));
                break;
        }
    }

    #endregion
}
