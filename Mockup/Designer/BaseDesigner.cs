// ======================================================================================
// FILE: Mockup.Designer/BaseDesigner.cs
// MO44 – BaseDesigner (ROOT / NO DUPLICATES)
//
// FIX:
// - Root enthält nur TemplateParts, Wiring, Basiseigenschaften + gemeinsame Felder
// - Bandzugriff erfolgt ausschließlich über GetBands()/GetHeaderBand()/GetFooterBand()
// - VERTICAL SCROLLBAR entfernt (neues Konzept: Designer wächst, kein internes Scrollbar-UI)
// ======================================================================================

using CommunityToolkit.Mvvm.Messaging;
using Mockup.Messages;
using Mockup.ViewModel;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Mockup.Designer;

#region === Enumeration DesignerKind ===

public enum DesignerKind
{
    Screen,
    Template,
    Popup,
}

#endregion === Enumeration DesignerKind ===

public abstract partial class BaseDesigner : System.Windows.Controls.Control
{
    #region === STATIC / CTOR / LOADED / UNLOADED ===
    MockupViewModel? VM => DataContext as MockupViewModel;
    private static WeakReference<BaseDesigner>? activeKeyboardDesigner;
    private Window? keyboardHostWindow;

    static BaseDesigner()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(BaseDesigner),
            new FrameworkPropertyMetadata(typeof(BaseDesigner))
        );
    }

    public BaseDesigner()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Focusable = true;
        KeyboardNavigation.SetIsTabStop(this, true);

        AttachKeyboardHost();
        HookMessages();

        InvalidateDesigner();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DetachKeyboardHost();

        if (IsActiveKeyboardDesigner())
            activeKeyboardDesigner = null;
    }

    public void FocusDesignerSurface()
    {
        activeKeyboardDesigner = new WeakReference<BaseDesigner>(this);
        Focus();
        Keyboard.Focus(this);
    }

    private void AttachKeyboardHost()
    {
        var host = Window.GetWindow(this);
        if (ReferenceEquals(keyboardHostWindow, host))
            return;

        DetachKeyboardHost();
        keyboardHostWindow = host;

        if (keyboardHostWindow == null)
            return;

        keyboardHostWindow.AddHandler(
            Keyboard.PreviewKeyDownEvent,
            new KeyEventHandler(OnHostPreviewKeyDown),
            handledEventsToo: true);
        keyboardHostWindow.AddHandler(
            Keyboard.PreviewKeyUpEvent,
            new KeyEventHandler(OnHostPreviewKeyUp),
            handledEventsToo: true);
        keyboardHostWindow.AddHandler(
            Mouse.PreviewMouseDownEvent,
            new MouseButtonEventHandler(OnHostPreviewMouseDown),
            handledEventsToo: true);
    }

    private void DetachKeyboardHost()
    {
        if (keyboardHostWindow == null)
            return;

        keyboardHostWindow.RemoveHandler(
            Keyboard.PreviewKeyDownEvent,
            new KeyEventHandler(OnHostPreviewKeyDown));
        keyboardHostWindow.RemoveHandler(
            Keyboard.PreviewKeyUpEvent,
            new KeyEventHandler(OnHostPreviewKeyUp));
        keyboardHostWindow.RemoveHandler(
            Mouse.PreviewMouseDownEvent,
            new MouseButtonEventHandler(OnHostPreviewMouseDown));

        keyboardHostWindow = null;
    }

    private void OnHostPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (IsActiveKeyboardDesigner())
            OnPreviewKeyDown(this, e);
    }

    private void OnHostPreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (IsActiveKeyboardDesigner())
            OnPreviewKeyUp(this, e);
    }

    private void OnHostPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (IsActiveKeyboardDesigner() && !IsMouseOver)
            activeKeyboardDesigner = null;
    }

    private bool IsActiveKeyboardDesigner()
    {
        return activeKeyboardDesigner != null
            && activeKeyboardDesigner.TryGetTarget(out var designer)
            && ReferenceEquals(designer, this);
    }

    #endregion === STATIC / CTOR / LOADED / UNLOADED ===

    #region ==== BAND ACCESS ===
    internal abstract IEnumerable<Band> GetAllBands();

    protected abstract IEnumerable<Band>? GetCustomBands();

    protected abstract Band? GetHeaderBand();

    protected abstract Band? GetFooterBand();

    #endregion ==== BAND ACCESS ===

    #region === PREVIEW ACTION AREA VISIBILITY ===

    public void StartPreviewAnim()
    {
        // Kompatibilitätsmethode für bestehende Preview-Aufrufer.
        // Es gibt bewusst keinen Timer mehr; ActionAreas werden per Space KeyDown/KeyUp neu gezeichnet.
        InvalidateDesigner();
    }

    public void StopPreviewAnim()
    {
        // Kompatibilitätsmethode für bestehende Preview-Aufrufer.
        // Kein Timer mehr vorhanden.
    }

    #endregion === PREVIEW ACTION AREA VISIBILITY ===

    #region === PREVIEW PROPS ===

    public bool IsPreviewHost { get; set; }
    public float PreviewScrollOffsetY { get; set; }

    #endregion === PREVIEW PROPS ===

    #region === DESIGNER WORLD BOUNDS ===

    protected abstract SKRect GetDesignerWorldBounds();

    #endregion === DESIGNER  WORLD BOUNDS ===

    #region === Template Parts ===

    protected SKElement PART_Canvas = null!;
    protected Canvas PART_Overlay = null!;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();


        PART_Canvas = GetTemplateChild("PART_Canvas") as SKElement ?? null!;
        PART_Overlay = GetTemplateChild("PART_Overlay") as Canvas ?? null!;

        if (PART_Canvas == null || PART_Overlay == null)
            return;

        PART_Canvas.RequestBringIntoView += (_, e) => e.Handled = true;

        Focusable = true;

        PART_Canvas.Focusable = true;
        PART_Canvas.MouseDown += (_, _) => FocusDesignerSurface();

        // Wichtig: diese Methoden sind in Partials implementiert!
        PART_Canvas.PaintSurface += OnPaintSurface;
        PART_Canvas.MouseDown += OnMouseDown;
        PART_Canvas.MouseMove += OnMouseMove;
        PART_Canvas.MouseUp += OnMouseUp;
        PART_Canvas.MouseLeave += OnMouseLeave;
        PART_Canvas.MouseWheel += OnMouseWheel;
    }

    #endregion

    #region === DPI Scaling ===
    private void DpiScale(SKCanvas canvas)
    {
        float _dpiScaleX = 1f;
        float _dpiScaleY = 1f;

        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget != null)
        {
            _dpiScaleX = (float)source.CompositionTarget.TransformToDevice.M11;
            _dpiScaleY = (float)source.CompositionTarget.TransformToDevice.M22;
        }
        else
        {
            _dpiScaleX = 1f;
            _dpiScaleY = 1f;
        }

        canvas.Scale(_dpiScaleX, _dpiScaleY);
    }

    #endregion

    #region === Screen (optional, nur für ScreenDesigner relevant) ===
    public Screen? Screen
    {
        get => (Screen?)GetValue(ScreenProperty);
        set => SetValue(ScreenProperty, value);
    }

    public static readonly DependencyProperty ScreenProperty = DependencyProperty.Register(
        nameof(Screen),
        typeof(Screen),
        typeof(BaseDesigner),
        new PropertyMetadata(null, OnScreenChangedStatic)
    );

    private static void OnScreenChangedStatic(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        var designer = (BaseDesigner)d;

        designer.OnScreenChanged((Screen?)e.OldValue, (Screen?)e.NewValue);

        designer.DeselectAllControls();

        designer.InvalidateDesigner();
    }

    protected virtual void OnScreenChanged(Screen? oldValue, Screen? newValue) { }

    #endregion

    #region === Designer Flags / State (shared, not duplicated) ===

    private DispatcherOperation? _pendingDesignerInvalidation;

    public DesignerKind DesignerKind { get; set; } = DesignerKind.Screen;

    public bool AllowBandInteraction { get; set; } = true;

    public float ScrollOffsetY { get; set; }

    public void InvalidateDesigner()
    {
        if (PART_Canvas == null)
            return;

        if (_pendingDesignerInvalidation?.Status == DispatcherOperationStatus.Pending)
            return;

        _pendingDesignerInvalidation = Dispatcher.BeginInvoke(
            DispatcherPriority.Render,
            new Action(() =>
            {
                _pendingDesignerInvalidation = null;
                PART_Canvas?.InvalidateVisual();
            }));
    }

    #endregion

    #region === Shared fields used by Partials (must exist once) ===

    protected Band? SelectedBand;
    protected Band? HoveredBand;

    #endregion

    #region === MOVE BAND ===

    protected virtual bool CanMoveBand(Band band)
    {
        if (!AllowBandInteraction)
            return false;

        if (band == null)
            return false;

        // Header/Footer bleiben fix
        if (band.BandType != BandType.Custom)
            return false;

        // Mindestens 2 Custom-Bands nötig
        var bands = GetAllBands();
        if (bands == null)
            return false;

        int customCount = bands.Count(b => b.BandType == BandType.Custom);
        return customCount > 1;
    }

    #endregion

    #region === Convenience (used across Partials) ===

    protected float DesignerWidth => PART_Canvas != null ? (float)PART_Canvas.ActualWidth : 0f;

    protected float DesignerHeight => PART_Canvas != null ? (float)PART_Canvas.ActualHeight : 0f;

    #endregion

    #region === Messaging ===

    private IEnumerable<DesignControl> GetAllDesignerControls()
    {
        var bands = GetAllBands();
        if (bands == null)
            yield break;

        foreach (var band in bands)
        {
            if (band?.Pages == null)
                continue;

            foreach (var page in band.Pages)
            {
                if (page?.Controls == null)
                    continue;

                foreach (var ctrl in page.Controls)
                {
                    if (ctrl != null)
                        yield return ctrl;
                }
            }
        }
    }

    private void ApplyExternalSelection(IReadOnlyList<long> ids)
    {
        DeselectAllControls();

        if (ids == null || ids.Count == 0)
        {
            VM.CurrentControl = null;
            return;
        }

        var all = GetAllDesignerControls().ToList();

        foreach (var id in ids)
        {
            var ctrl = all.FirstOrDefault(x => x.Id == id);
            if (ctrl == null)
                continue;

            if (!VM.SelectedControls.Contains(ctrl))
                VM.SelectedControls.Add(ctrl);

            ctrl.IsSelected = true;
        }

        VM.CurrentControl = VM.SelectedControls.LastOrDefault();

        var first = VM.SelectedControls.FirstOrDefault();
        if (first != null)
            MockupService.Mockup.SetContextControls(first.ParentBand, VM.SelectedControls);
    }

    private void HookMessages()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);

        WeakReferenceMessenger.Default.Register<InvalidateDesignerMessage>(
            this,
            (_, _) => InvalidateDesigner()
        );

        WeakReferenceMessenger.Default.Register<SelectBandMessage>(
            this,
            (_, msg) =>
            {
                if (DesignerKind != DesignerKind.Screen)
                    return;

                var screen = Screen;
                if (screen == null || screen.Id != msg.ScreenId)
                    return;

                var band = screen.Bands.FirstOrDefault(b => b.Id == msg.BandId);
                if (band == null)
                    return;

                DeselectAllControls();

                SelectedBand = band;
                HoveredBand = null;
                VM.CurrentControl = null;
                _mouseState.Reset();

                MSG.UI.InvalidateDesigner();
            }
        );

    }

    #endregion
}
