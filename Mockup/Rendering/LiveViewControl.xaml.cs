// ======================================================================================
// FILE: Mockup.Designer/ScreenPreviewControl.xaml.cs
// ======================================================================================

using CommunityToolkit.Mvvm.Messaging;
using GongSolutions.Wpf.DragDrop;
using Mockup.Actions;
using Mockup.Designer;
using Mockup.Messages;
using Mockup.Services;
using Mockup.ViewModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mockup.Rendering;

public partial class LiveViewControl : UserControl, IDropTarget
{
    #region === CTOR ===

    public LiveViewControl()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    #endregion === CTOR ===

    #region === DP: Screen ===

    public Screen? Screen
    {
        get => (Screen?)GetValue(ScreenProperty);
        set => SetValue(ScreenProperty, value);
    }

    public static readonly DependencyProperty ScreenProperty = DependencyProperty.Register(
        nameof(Screen),
        typeof(Screen),
        typeof(LiveViewControl),
        new PropertyMetadata(null, OnScreenChanged)
    );

    private static void OnScreenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (LiveViewControl)d;
        ctrl.SetDesignerScreen(e.NewValue as Screen, resetScroll: true);
    }

    #endregion === DP: Screen ===

    #region === Preview Action Event (ACTION AREA) ===

    public event EventHandler<PreviewActionTriggeredEventArgs>? PreviewActionTriggered;
    public event EventHandler? PreviewScrollChanged;

    public sealed class PreviewActionTriggeredEventArgs : EventArgs
    {
        public required ActionArea Area { get; init; }
        public required ActionTrigger Trigger { get; init; }
        public required ActionDefinition? Action { get; init; }
    }

    #endregion === Preview Action Event ===

    #region === LOADED / UNLOADED ===

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (PART_Designer == null)
            return;

        if (DataContext is MockupViewModel vm)
        {
            vm.PreviewNavigateHome(); // initialisiert Trail + PreviewScreen
            SetDesignerScreen(vm.PreviewScreen, resetScroll: true);

            // Wenn PreviewScreen wechselt -> Designer.Screen nachziehen
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MockupViewModel.PreviewScreen))
                    SetDesignerScreen(vm.PreviewScreen, resetScroll: true);
            };
        }

        //// Screen setzen (falls DP vorher kam)
        //PART_Designer.Screen = Screen;

        // Preview-Mode
        PART_Designer.LiveMode = true;

        // Keine Band-Editor-Interaktionen im Preview
        PART_Designer.AllowBandInteraction = false;

        // alles abwählen (auch wenn vorher im Designer selektiert war)
        PART_Designer.DeselectAllControls();

        if (IsVisible)
        {
            PART_Designer.StartPreviewAnim();
        }

        WeakReferenceMessenger.Default.UnregisterAll(this);

        WeakReferenceMessenger.Default.Register<ActionAreaTriggerMessage>(
            this,
            static (r, m) => ((LiveViewControl)r).OnActionAreaClicked(m)
        );

    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        PART_Designer.StopPreviewAnim();
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    #endregion === LOADED / UNLOADED ===

    #region === MESSAGE HANDLING ===

    private void OnActionAreaClicked(ActionAreaTriggerMessage msg)
    {
        var (area, trigger) = msg.Value;

        var action = area.GetActionForTrigger(trigger);

        if (action != null && DataContext is MockupViewModel vm)
        {
            switch (action.Type)
            {
                case ActionType.Navigate:
                    {
                        var target = vm.CurrentProject?.Screens.FirstOrDefault(x =>
                            x.Id == action.TargetScreenId
                        );
                        vm.PreviewNavigateTo(target);
                        break;
                    }
                case ActionType.NavigateBack:
                    {
                        vm.PreviewNavigateBack();
                        break;
                    }
                case ActionType.NavigateHome:
                    {
                        vm.PreviewNavigateHome();
                        break;
                    }

                case ActionType.OpenFile:
                    {
                        // path kommt aus ActionDefinition.FilePath (mapped auf Parameters["path"])
                        var path = action.FilePath;

                        if (string.IsNullOrWhiteSpace(path))
                            break;

                        try
                        {
                            // Standard-App öffnen (ShellExecute)
                            var psi = new ProcessStartInfo { FileName = path, UseShellExecute = true };

                            Process.Start(psi);
                        }
                        catch (Exception ex)
                        {
                            // optional: Logging / UI-Hinweis
                            Serilog.Log.Error(ex, "OpenFile failed: {Path}", path);
                            XNotifications.Warning($"Cannot open file: {path}");
                        }

                        break;
                    }

                case ActionType.OpenURL:
                    {
                        var url = action.Url;

                        if (string.IsNullOrWhiteSpace(url))
                            break;

                        url = url.Trim();

                        // optional: wenn User "example.com" eingibt
                        if (!url.Contains("://", StringComparison.Ordinal))
                            url = "https://" + url;

                        try
                        {
                            Process.Start(
                                new ProcessStartInfo { FileName = url, UseShellExecute = true }
                            );
                        }
                        catch (Exception ex)
                        {
                            Serilog.Log.Error(ex, "OpenURL failed: {Url}", url);
                            XNotifications.Warning($"Cannot open URL: {url}");
                        }

                        break;
                    }

                case ActionType.ShowPopup:
                    {
                        vm.OpenPreviewPopup(
                            action.PopupId,
                            action.PopupPosition,
                            action.UseMousePos == true
                        );
                        break;
                    }
            }

            // LiveViewControl.Screen nur noch “Anzeige”, nicht Logik:
            Screen = vm.PreviewScreen;
            ClampPreviewScrollOffset(notify: true);
        }

        PreviewActionTriggered?.Invoke(
            this,
            new PreviewActionTriggeredEventArgs
            {
                Area = area,
                Trigger = trigger,
                Action = action,
            }
        );

        // Ausführung (Navigation/Popup) macht dann der Host (VM/PreviewHost).
    }

    #endregion === MESSAGE HANDLING ===

    #region === PREVIEW SCROLL ===

    private void SetDesignerScreen(Screen? screen, bool resetScroll)
    {
        if (PART_Designer == null)
            return;

        PART_Designer.Screen = screen;

        if (resetScroll)
            PART_Designer.PreviewScrollOffsetY = 0f;

        PART_Designer.InvalidateDesigner();
        PreviewScrollChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Aktualisiert die Live Preview explizit beim Betreten der LiveView.
    /// </summary>
    public void RefreshPreview()
    {
        if (PART_Designer == null)
            return;

        PART_Designer.InvalidateDesigner();
        InvalidateVisual();
        PreviewScrollChanged?.Invoke(this, EventArgs.Empty);
    }

    public double GetPreviewScrollMaximum()
    {
        if (PART_Designer == null)
            return 0d;

        double screenHeight = PART_Designer.Screen?.ScreenHeight ?? 0d;
        double deviceHeight = 0d;

        if (DataContext is MockupViewModel vm && vm.CurrentProject != null)
            deviceHeight = vm.CurrentProject.DeviceHeight;

        if (deviceHeight <= 0d)
            deviceHeight = PART_Designer.ActualHeight;

        if (screenHeight <= 0d || deviceHeight <= 0d)
            return 0d;

        return Math.Max(0d, Math.Round(screenHeight - deviceHeight));
    }

    public double GetPreviewScrollValue()
    {
        if (PART_Designer == null)
            return 0d;

        double maxScroll = GetPreviewScrollMaximum();
        double value = -PART_Designer.PreviewScrollOffsetY;

        return Math.Clamp(value, 0d, maxScroll);
    }

    public void SetPreviewScrollValue(double value, bool notify = true)
    {
        if (PART_Designer == null)
            return;

        double maxScroll = GetPreviewScrollMaximum();
        double clampedValue = Math.Clamp(value, 0d, maxScroll);

        PART_Designer.PreviewScrollOffsetY = -(float)clampedValue;
        PART_Designer.InvalidateDesigner();

        if (notify)
            PreviewScrollChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClampPreviewScrollOffset(bool notify = true)
    {
        SetPreviewScrollValue(GetPreviewScrollValue(), notify);
    }

    #endregion === PREVIEW SCROLL ===

    #region === DRAGDROP ===

    void IDropTarget.DragOver(IDropInfo dropInfo) => PART_Designer?.OnDragOver(dropInfo);

    void IDropTarget.Drop(IDropInfo dropInfo) => PART_Designer?.OnDrop(dropInfo);

    #endregion === DRAGDROP ===

    #region === MOUSE WHEEL ===

    private void LayoutRoot_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (PART_Designer == null)
            return;

        // nur wenn Preview-Scroll aktiv ist
        if (!(PART_Designer.LiveMode && PART_Designer.DesignerKind == DesignerKind.Screen))
            return;

        double maxScroll = GetPreviewScrollMaximum();
        if (maxScroll <= 0.5d)
            return;

        bool ctrlKey = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

        double step = ctrlKey ? 30d : 15d;
        double delta = e.Delta > 0 ? -step : step;

        SetPreviewScrollValue(GetPreviewScrollValue() + delta, notify: true);

        e.Handled = true;
    }

    #endregion === MOUSE WHEEL ===
}
