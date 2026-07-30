// ======================================================================================
// BASELINE: BaseDesigner.MouseHandler.cs
// VERSION: 2026-01-18-B (control selection stable)
// ======================================================================================
// FILE: Mockup.Designer/BaseDesigner.MouseHandler.cs
//
// MO44 – Mouse-Engine (V3 FINAL)
// Popup-Optimierung: Band-Interaktionen werden deaktiviert,
// wenn DesignerKind == Popup.
// Band-Reorder: NUR noch via Buttons (MoveUp/MoveDown). Kein Drag-Reorder.
// ======================================================================================

using Mockup.Actions;
using Mockup.Messages;
using Mockup.Services;
using Mockup.Snapshots;
using Mockup.ViewModel;
using SkiaSharp;
using System.Windows;
using System.Windows.Input;

namespace Mockup.Designer;

public partial class BaseDesigner
{
    #region === Mouse State ===

    public struct MouseState
    {
        public DesignerMouseMode Mode;
        public Band? ActiveDragBand;
        public Band? ActiveResizeBand;
        public float StartY;
        public float StartHeight;
        public float DragStartY;
        public float DragOffset;

        public void Reset()
        {
            Mode = DesignerMouseMode.None;
            ActiveDragBand = null;
            ActiveResizeBand = null;
            StartY = 0;
            StartHeight = 0;
            DragStartY = 0;
            DragOffset = 0;
        }
    }

    private MouseState _mouseState;

    #endregion

    #region === Resize Control State ===

    private DesignControl? _activeResizeControl = null!;
    private ControlResizeHandle _activeResizeHandle = ControlResizeHandle.None;
    private SKPoint _resizeStartMouseWorld = SKPoint.Empty;
    private SKRect _resizeStartRect = SKRect.Empty;

    #endregion === Resize Control State ===

    #region === CONTROL SELECTION ===

    private bool IsScreenPreview => LiveMode && DesignerKind == DesignerKind.Screen;
    private bool IsPopupPreview => LiveMode && DesignerKind == DesignerKind.Popup && IsPreviewHost;
    private bool IsPreview => IsScreenPreview || IsPopupPreview;

    private readonly PreviewInteractionService _previewInteraction = new();

    private DesignControl? _hoverControl;

    protected void SelectDroppedControl(DesignControl ctrl)
    {
        DeselectAllControls();
        SelectControl(ctrl);
        InvalidateDesigner();
    }

    protected void SelectControl(DesignControl ctrl)
    {
        if (!VM.SelectedControls.Contains(ctrl))
            VM?.SelectedControls.Add(ctrl);

        ctrl.IsSelected = true;
        VM?.CurrentControl = ctrl;
    }
    private void DeselectControl(DesignControl ctrl)
    {
        ctrl.IsSelected = false;
        VM?.SelectedControls.Remove(ctrl);

        if (VM?.CurrentControl == ctrl)
            VM?.CurrentControl = VM?.SelectedControls.LastOrDefault();
    }

    public void DeselectAllControls()
    {
        if (VM == null)
            return;

        if (VM?.SelectedControls?.Count == 0)
            return;

        foreach (var c in VM?.SelectedControls)
            c.IsSelected = false;

        VM?.SelectedControls.Clear();

        var allBands = GetAllBands();
        if (allBands != null)
        {
            foreach (var b in allBands)
            {
                if (b?.Pages == null)
                    continue;

                foreach (var p in b.Pages)
                {
                    if (p?.Controls == null)
                        continue;

                    foreach (var c in p.Controls)
                        c.IsSelected = false;
                }
            }
        }

        VM?.CurrentControl = null;
    }

    private DesignControl? HitTestControl(SKPoint worldPoint, out Band? parentBand)
    {
        parentBand = null;

        var allBands = GetAllBands()?.Reverse();
        if (allBands == null)
            return null;

        foreach (var band in allBands)
        {
            if (band.IsExpandable && !band.IsExpanded)
                continue;

            var page = band.ActivePage;
            if (page == null)
                continue;

            foreach (var ctrl in page.Controls.OrderByDescending(c => c.ZIndex))
            {
                if (ctrl.HitTest(worldPoint))
                {
                    parentBand = band;
                    return ctrl;
                }
            }
        }

        return null;
    }

    #endregion === CONTROL SELECTION ===

    #region === CONTROL DRAG STATE ===

    private bool _isDraggingControls;
    private bool _controlDragCopyRequested;
    private bool _controlDragSnapshotPushed;
    private bool _controlResizeSnapshotPushed;
    private bool _bandResizeSnapshotPushed;
    private bool _keyboardNudgeSnapshotPushed;
    private DesignControl? _pendingCtrlClickToggleControl;
    private SKPoint _controlDragStartMouseWorld;
    private string? _designerInteractionHintText;
    private SKPoint _designerInteractionHintAnchor = SKPoint.Empty;
    private float _designerInteractionHintFallbackY;

    private readonly Dictionary<DesignControl, SKPoint> _controlDragStartLocal = new();

    #endregion

    #region === RUBBERBAND SELECTION ===

    private bool _isRubberbandSelecting;
    private bool _rubberbandAdditiveSelection;
    private SKPoint _rubberbandStartMouseWorld;
    private SKPoint _rubberbandCurrentMouseWorld;
    private SKRect _rubberbandWorldRect = SKRect.Empty;

    private const float RubberbandStartThreshold = 4f;

    #endregion === RUBBERBAND SELECTION ===

    #region === MOUSE COORDINATES ===

    public SKPoint MouseViewPoint
    {
        get
        {
            var pos = Mouse.GetPosition(PART_Canvas);
            return new SKPoint((float)pos.X, (float)pos.Y);
        }
    }

    public SKPoint MouseWorldPoint => MouseViewPoint;

    #endregion === MOUSE COORDINATES ===

    #region === POPUP DESIGNER SHORTCUTS ===

    private bool IsPopup => DesignerKind == DesignerKind.Popup;

    private bool PopupSuppressAllBandActions => !AllowBandInteraction;

    #endregion

    #region === MOUSE DOWN ===
    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (PART_Canvas == null)
            return;

        FocusDesignerSurface();

        var pt = MouseWorldPoint;
        ClearDesignerInteractionHint();

        bool ctrlKey = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

        if (e.ChangedButton == MouseButton.Right && !IsPreview)
        {
            e.Handled = true;
            ShowDesignerContextMenu(e);
            return;
        }

        if (IsPreview)
        {
            DeselectAllControls();

            if (LiveMouseDown(pt, e))
            {
                e.Handled = true;
                return;
            }
        }

        if (VM?.SelectedControls.Count == 1)
        {
            var ctrl = VM?.SelectedControls[0];

            if (ctrl.HitTestResizeHandle(pt, out var handle))
            {
                handle = NormalizeResizeHandleForControl(ctrl, handle);

                if (handle != ControlResizeHandle.None)
                {
                    _activeResizeControl = ctrl;
                    _activeResizeHandle = handle;
                    _resizeStartMouseWorld = pt;
                    _resizeStartRect = ctrl.VisualRect;
                    _controlResizeSnapshotPushed = false;
                    ClearAlignmentGuidelines();

                    PART_Canvas.CaptureMouse();
                    e.Handled = true;
                    return;
                }
            }
        }

        var hitCtrl = HitTestControl(pt, out var ctrlBand);

        if (hitCtrl != null)
        {
            if (
                e.ChangedButton == MouseButton.Left
                && e.ClickCount == 2
                && hitCtrl is ActionArea aa
            )
            {
                DeselectAllControls();
                SelectControl(aa);
                SelectedBand = ctrlBand;
                InvalidateDesigner();

                MSG.AA.ShowEditor(aa);

                e.Handled = true;
                return;
            }

            _pendingCtrlClickToggleControl = null;

            if (ctrlKey)
            {
                if (hitCtrl.IsSelected)
                    _pendingCtrlClickToggleControl = hitCtrl;
                else
                    SelectControl(hitCtrl);
            }
            else
            {
                if (!hitCtrl.IsSelected)
                {
                    DeselectAllControls();
                    SelectControl(hitCtrl);
                }
            }

            SelectedBand = ctrlBand;

            _isDraggingControls = false;
            _controlDragCopyRequested = ctrlKey;
            _controlDragSnapshotPushed = false;
            _controlDragStartMouseWorld = pt;
            _controlDragStartLocal.Clear();

            foreach (var c in VM?.SelectedControls)
                _controlDragStartLocal[c] = new SKPoint(c.X, c.Y);

            PART_Canvas.CaptureMouse();
            InvalidateDesigner();

            return;
        }

        HoveredBand = HitTestBand(pt);

        if (PopupSuppressAllBandActions)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (!ctrlKey)
                    DeselectAllControls();

                SelectedBand = null;
                _mouseState.Reset();
                StartRubberbandSelection(pt, ctrlKey);
                PART_Canvas.CaptureMouse();
                InvalidateDesigner();
            }

            return;
        }

        if (HoveredBand != null)
        {
            if (HoveredBand.HitTestMoveUp(pt))
            {
                MSG.MoveBand(HoveredBand, -1);
                return;
            }

            if (HoveredBand.HitTestMoveDown(pt))
            {
                MSG.MoveBand(HoveredBand, +1);
                return;
            }

            if (HoveredBand.ShowTabs && HoveredBand.HitTestLeftArrow(pt))
            {
                HoveredBand.PreviousPage();
                Screen?.RecalculateBandLayout();
                InvalidateDesigner();
                return;
            }

            if (HoveredBand.ShowTabs && HoveredBand.HitTestRightArrow(pt))
            {
                HoveredBand.NextPage();
                Screen?.RecalculateBandLayout();
                InvalidateDesigner();
                return;
            }

            bool checkTabs = HoveredBand.ShowTabs;
            if (HoveredBand.IsExpandable)
                checkTabs &= HoveredBand.IsExpanded;

            if (checkTabs)
            {
                int tab = HoveredBand.HitTestTabIndex(pt);
                if (tab >= 0)
                {
                    HoveredBand.SetActivePage(tab);
                    Screen?.RecalculateBandLayout();
                    SelectedBand = HoveredBand;
                    PART_Canvas.CaptureMouse();
                    InvalidateDesigner();
                    return;
                }
            }

            if (HoveredBand.HitTestToggle(pt))
            {
                var snapshotContext = GetSnapshotContextForDesigner();
                if (snapshotContext != null)
                    MockupService.Mockup.PushSnapshot(snapshotContext.Value, SnapshotLabels.BandToggled);

                _mouseState.Mode = DesignerMouseMode.TogglingBand;
                HoveredBand.IsExpanded = !HoveredBand.IsExpanded;
                Screen?.RecalculateBandLayout();
                SelectedBand = HoveredBand;
                PART_Canvas.CaptureMouse();
                InvalidateDesigner();
                return;
            }

            if (HoveredBand.HitTestResize(pt))
            {
                _mouseState.Mode = DesignerMouseMode.ResizingBand;
                _mouseState.ActiveResizeBand = HoveredBand;
                _bandResizeSnapshotPushed = false;

                _mouseState.StartHeight = GetBandResizeStartHeight(HoveredBand);
                _mouseState.StartY = pt.Y;

                SelectedBand = HoveredBand;
                PART_Canvas.CaptureMouse();
                return;
            }
        }

        if (e.ChangedButton == MouseButton.Left)
        {
            if (!ctrlKey)
                DeselectAllControls();

            SelectedBand = HoveredBand;
            _mouseState.Reset();
            StartRubberbandSelection(pt, ctrlKey);
            PART_Canvas.CaptureMouse();
            InvalidateDesigner();
            return;
        }

        _mouseState.Mode = DesignerMouseMode.SelectingBand;
        SelectedBand = HoveredBand;
        PART_Canvas.CaptureMouse();
        InvalidateDesigner();
    }

    #endregion === MOUSE DOWN ===

    #region === MOUSE MOVE ===
    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (PART_Canvas == null)
            return;

        var pt = MouseWorldPoint;

        if (IsScreenPreview)
        {
            Band? hoverBand = HitTestBand(pt);

            if (HandleBandResizeAndToggleHover(pt, hoverBand))
            {
                Cursor = Cursors.Hand;
                e.Handled = true;
                return;
            }

            if (CheckIfPointerisOverBandPageTabs(pt, hoverBand))
            {
                Cursor = Cursors.Hand;
                e.Handled = true;
                return;
            }

            if (LiveMouseMove(pt, e))
            {
                e.Handled = true;
                return;
            }
        }
        else if (IsPopupPreview)
        {
            if (LiveMouseMove(pt, e))
            {
                e.Handled = true;
                return;
            }
        }

        if (_isRubberbandSelecting)
        {
            UpdateRubberbandSelection(pt);
            Cursor = Cursors.Cross;
            InvalidateDesigner();
            return;
        }

        if (_activeResizeControl == null && !_isDraggingControls && !PART_Canvas.IsMouseCaptured)
        {
            Band? hoverBand;
            var hoverCtrl = HitTestControl(pt, out hoverBand);

            if (!ReferenceEquals(_hoverControl, hoverCtrl))
            {
                _hoverControl?.OnPointerLeave();
                _hoverControl = hoverCtrl;
            }

            if (hoverCtrl != null)
            {
                var ctx = CreatePointerContext(pt, null, 0);
                hoverCtrl.OnPointerMove(in ctx);
            }
        }

        if (
            _activeResizeControl == null
            && !_isDraggingControls
            && !PART_Canvas.IsMouseCaptured
            && VM?.SelectedControls.Count == 1
        )
        {
            var ctrl = VM?.SelectedControls[0];

            if (ctrl.HitTestResizeHandle(pt, out var handle))
            {
                handle = NormalizeResizeHandleForControl(ctrl, handle);

                if (handle != ControlResizeHandle.None)
                {
                    Mouse.OverrideCursor = handle switch
                    {
                        ControlResizeHandle.Left or ControlResizeHandle.Right => Cursors.SizeWE,
                        ControlResizeHandle.Top or ControlResizeHandle.Bottom => Cursors.SizeNS,
                        ControlResizeHandle.TopLeft or ControlResizeHandle.BottomRight =>
                            Cursors.SizeNWSE,
                        ControlResizeHandle.TopRight or ControlResizeHandle.BottomLeft =>
                            Cursors.SizeNESW,
                        _ => Cursors.Hand,
                    };
                    return;
                }
            }

            Mouse.OverrideCursor = null!;
        }

        if (_activeResizeControl != null)
        {
            var dx = pt.X - _resizeStartMouseWorld.X;
            var dy = pt.Y - _resizeStartMouseWorld.Y;

            bool geometryChanged = ApplyControlResize(
                _activeResizeControl,
                _activeResizeHandle,
                dx,
                dy,
                _resizeStartRect,
                () =>
                {
                    if (_controlResizeSnapshotPushed
                        || (Math.Abs(dx) <= 1 && Math.Abs(dy) <= 1))
                    {
                        return;
                    }

                    var snapshotContext = GetSnapshotContextForDesigner();
                    if (snapshotContext != null)
                    {
                        MockupService.Mockup.PushSnapshot(
                            snapshotContext.Value,
                            SnapshotLabels.ControlResized);
                        _controlResizeSnapshotPushed = true;
                    }
                });

            if (!geometryChanged)
                return;

            UpdateAlignmentGuidelinesDuringControlResize(
                _activeResizeControl,
                _activeResizeHandle);

            UpdateControlResizeInteractionHint(pt, _activeResizeControl);

            InvalidateDesigner();
            return;
        }

        if (HandleControlDrag(pt))
            return;

        if (PopupSuppressAllBandActions)
        {
            HoveredBand = null;
            Cursor = Cursors.Arrow;
            return;
        }

        if (
            _mouseState.Mode != DesignerMouseMode.ResizingBand
            && FooterBand != null
            && !FooterBand.ResizeThumbRect.IsEmpty
        )
        {
            var r = FooterBand.ResizeThumbRect;

            if (r.Contains(pt))
            {
                Cursor = Cursors.SizeNS;
                return;
            }
        }

        if (HandleBandResize(pt))
            return;

        ResetBandHoverState();

        var band = HitTestBand(pt);
        HoveredBand = band;

        if (UpdateBandHoverAndCursor(pt, band))
            return;

        if (HandleBandResizeAndToggleHover(pt, band))
            return;

        if (CheckIfPointerisOverBandPageTabs(pt, band))
        {
            Cursor = Cursors.Hand;
            return;
        }

        Cursor = Cursors.Arrow;
    }

    #endregion === MOUSE MOVE ===

    #region === MOUSE UP ===

    private const float BandBottomSnapTolerance = 20f;

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        var pt = MouseWorldPoint;

        Band? upBand;

        var upCtrl = HitTestControl(pt, out upBand);

        if (!ReferenceEquals(_hoverControl, upCtrl))
        {
            _hoverControl?.OnPointerLeave();
            _hoverControl = upCtrl;
        }

        Mouse.OverrideCursor = null!;

        if (IsPreview)
        {
            if (LiveMouseUp(pt, e))
            {
                e.Handled = true;
                if (PART_Canvas.IsMouseCaptured)
                    PART_Canvas.ReleaseMouseCapture();

                InvalidateDesigner();

                return;
            }
        }

        if (!_isDraggingControls && _activeResizeControl == null && upCtrl != null)
        {
            var ctx = CreatePointerContext(pt, e.ChangedButton, e.ClickCount);
            upCtrl.OnPointerUp(in ctx);
        }

        if (!_isDraggingControls
            && _activeResizeControl == null
            && _pendingCtrlClickToggleControl != null)
        {
            DeselectControl(_pendingCtrlClickToggleControl);
            ResetControlDragModifierState();
            ClearDesignerInteractionHint();

            if (PART_Canvas.IsMouseCaptured)
                PART_Canvas.ReleaseMouseCapture();

            Cursor = Cursors.Arrow;
            InvalidateDesigner();
            return;
        }

        if (_isDraggingControls)
        {
            _isDraggingControls = false;

            if (VM?.SelectedControls != null)
            {
                ResolveControlParentAfterDrag(VM.SelectedControls);
            }

            TryApplyAlignmentGuidelineSnapAfterControlDrag();
            TryPromoteDraggedControlAboveCoveringSurface(VM?.SelectedControls);
            NormalizeAllPagesZOrder();

            _controlDragStartLocal.Clear();
            ClearAlignmentGuidelines();

            if (PART_Canvas.IsMouseCaptured)
                PART_Canvas.ReleaseMouseCapture();

            _controlDragSnapshotPushed = false;
            ResetControlDragModifierState();
            ClearDesignerInteractionHint();

            Cursor = Cursors.Arrow;

            InvalidateDesigner();

            return;
        }

        ResetControlDragModifierState();
        ClearDesignerInteractionHint();

        if (PopupSuppressAllBandActions)
        {
            _mouseState.Reset();

            if (PART_Canvas.IsMouseCaptured)
                PART_Canvas.ReleaseMouseCapture();

            Cursor = Cursors.Arrow;

            InvalidateDesigner();

            return;
        }

        if (SelectedBand != null && SelectedBand.HitTestPageArea(pt))
        {
            SelectedBand.OnPointerUp(pt);
        }

        if (_activeResizeControl != null)
        {
            TryApplyAlignmentGuidelineSnapAfterControlResize();
            ClearAlignmentGuidelines();

            _activeResizeControl = null;
            _activeResizeHandle = ControlResizeHandle.None;
            _controlResizeSnapshotPushed = false;
            ClearDesignerInteractionHint();

            if (PART_Canvas.IsMouseCaptured)
                PART_Canvas.ReleaseMouseCapture();

            InvalidateDesigner();

            return;
        }

        if (
            _mouseState.Mode == DesignerMouseMode.ResizingBand
            && _mouseState.ActiveResizeBand != null
        )
        {
            var resizeBand = _mouseState.ActiveResizeBand;

            ApplyBandResizeHeight(resizeBand, resizeBand.Height);
            UpdateActivePageWorldBounds(resizeBand);

            if (Screen != null)
            {
                bool IsBandVisible(Band b) =>
                    (b.BandType != BandType.Header || Screen.ShowHeader)
                    && (b.BandType != BandType.Footer || Screen.ShowFooter);

                var lastVisibleCustomBand = Screen.Bands
                    .Where(b => b.BandType == BandType.Custom)
                    .Where(IsBandVisible)
                    .LastOrDefault();

                if (ReferenceEquals(lastVisibleCustomBand, resizeBand))
                {
                    float bandTop = 0f;

                    foreach (var band in Screen.Bands)
                    {
                        if (!IsBandVisible(band))
                            continue;

                        if (ReferenceEquals(band, resizeBand))
                            break;

                        bandTop += band.EffectiveHeight;
                    }

                    float bandBottom = MathF.Round(bandTop + resizeBand.EffectiveHeight);

                    float snapBottom = MathF.Round(
                        Screen.ScreenHeight
                        - (
                            Screen.ShowFooter && Screen.FooterBand != null
                                ? Screen.FooterBand.EffectiveHeight
                                : 0f
                        )
                    );

                    float deltaToBottom = snapBottom - bandBottom;

                    if (Math.Abs(deltaToBottom) <= BandBottomSnapTolerance)
                    {
                        float minHeight = GetBandMinHeightForCurrentDesigner(resizeBand);
                        float snappedHeight = MathF.Round(
                            MathF.Max(minHeight, resizeBand.Height + deltaToBottom)
                        );

                        if (Math.Abs(resizeBand.Height - snappedHeight) > 0.5f)
                        {
                            ApplyBandResizeHeight(resizeBand, snappedHeight);
                            Screen.RecalculateBandLayout(invalidatePreview: false);
                            UpdateActivePageWorldBounds(resizeBand);
                        }
                    }
                }
            }

            _bandResizeSnapshotPushed = false;
        }

        if (_isRubberbandSelecting)
        {
            FinishRubberbandSelection();

            if (PART_Canvas.IsMouseCaptured)
                PART_Canvas.ReleaseMouseCapture();

            Cursor = Cursors.Arrow;

            InvalidateDesigner();

            return;
        }

        _mouseState.Reset();

        if (PART_Canvas.IsMouseCaptured)
            PART_Canvas.ReleaseMouseCapture();

        Cursor = Cursors.Arrow;

        InvalidateDesigner();
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        _hoverControl?.OnPointerLeave();
        _hoverControl = null;

        if (_isRubberbandSelecting)
            CancelRubberbandSelection();

        ClearAlignmentGuidelines();
        ClearDesignerInteractionHint();

        Cursor = Cursors.Arrow;
        Mouse.OverrideCursor = null!;

        InvalidateDesigner();
    }

    #endregion === MOUSE UP ===

    #region === LIVE MOUSE HANDLING ===
    private bool CloseOpenPreviewDropDownsExcept(DesignControl? keepOpenControl)
    {
        bool changed = false;

        var allBands = GetAllBands();
        if (allBands == null)
            return false;

        foreach (var band in allBands)
        {
            if (band.IsExpandable && !band.IsExpanded)
                continue;

            var page = band.ActivePage;
            if (page == null)
                continue;

            foreach (var ctrl in page.Controls)
            {
                if (ReferenceEquals(ctrl, keepOpenControl))
                    continue;

                if (ctrl is Mockup.Controls.ComboBox combo && combo.IsDropDownOpen)
                {
                    combo.IsDropDownOpen = false;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private void CloseOpenPreviewDropDownsAndInvalidateIfNeeded(DesignControl? keepOpenControl)
    {
        if (CloseOpenPreviewDropDownsExcept(keepOpenControl))
            InvalidateDesigner();
    }

    private bool LiveMouseDown(SKPoint pt, MouseButtonEventArgs e)
    {
        return _previewInteraction.HandleMouseDown(this, pt, e);
    }

    private bool LiveMouseMove(SKPoint pt, MouseEventArgs e)
    {
        return _previewInteraction.HandleMouseMove(this, pt, e);
    }

    private bool LiveMouseUp(SKPoint pt, MouseButtonEventArgs e)
    {
        return _previewInteraction.HandleMouseUp(this, pt, e);
    }

    #endregion === LIVE MOUSE HANDLING ===

    #region === BAND RESIZE ===


    private float GetBandResizeStartHeight(Band band)
    {
        if (DesignerKind == DesignerKind.Template
            && this is TemplateDesigner templateDesigner
            && templateDesigner.ScreenTemplate != null)
        {
            return MathF.Round(templateDesigner.ScreenTemplate.Height);
        }

        if (DesignerKind == DesignerKind.Popup
            && this is PopupDesigner popupDesigner
            && popupDesigner.ScreenPopup != null)
        {
            return MathF.Round(popupDesigner.ScreenPopup.Height);
        }

        return MathF.Round(band.ActivePage?.Height ?? band.Height);
    }

    private bool HandleBandResize(SKPoint pt)
    {
        if (
            _mouseState.Mode != DesignerMouseMode.ResizingBand
            || _mouseState.ActiveResizeBand == null
        )
            return false;

        var resizeBand = _mouseState.ActiveResizeBand;
        float dy = pt.Y - _mouseState.StartY;

        float minH = GetBandMinHeightForCurrentDesigner(resizeBand);

        float newH =
            resizeBand.BandType == BandType.Footer
                ? Math.Max(minH, _mouseState.StartHeight - dy)
                : Math.Max(minH, _mouseState.StartHeight + dy);

        newH = MathF.Round(newH);

        if (!_bandResizeSnapshotPushed && Math.Abs(newH - _mouseState.StartHeight) > 0.5f)
        {
            var snapshotContext = GetSnapshotContextForDesigner();
            if (snapshotContext != null)
            {
                MockupService.Mockup.PushSnapshot(snapshotContext.Value, SnapshotLabels.BandResized);
                _bandResizeSnapshotPushed = true;
            }
        }

        ApplyBandResizeHeight(resizeBand, newH);
        UpdateActivePageWorldBounds(resizeBand);
        InvalidateDesigner();

        return true;
    }

    private float GetBandMinHeightForCurrentDesigner(Band band)
    {
        if (DesignerKind == DesignerKind.Popup
            && this is PopupDesigner popupDesigner
            && popupDesigner.ScreenPopup != null)
        {
            float popupHeaderHeight =
                popupDesigner.ScreenPopup.HasHeader
                    ? MathF.Round(MathF.Max(0f, popupDesigner.ScreenPopup.HeaderHeight))
                    : 0f;

            float contentMinHeight = GetBandContentMinHeightFromControls(band);

            return MathF.Round(MathF.Max(MathF.Max(30f, popupHeaderHeight), popupHeaderHeight + contentMinHeight));
        }

        return GetBandMinHeightFromContent(band);
    }

    private void ApplyBandResizeHeight(Band resizeBand, float requestedHeight)
    {
        float minHeight = GetBandMinHeightForCurrentDesigner(resizeBand);
        float newHeight = MathF.Round(MathF.Max(minHeight, requestedHeight));

        if (Screen != null)
        {
            Screen.ResizeBandFromDesigner(resizeBand, newHeight);
            return;
        }

        if (DesignerKind == DesignerKind.Template
            && this is TemplateDesigner templateDesigner
            && templateDesigner.ScreenTemplate != null)
        {
            templateDesigner.ScreenTemplate.Height = newHeight;

            if (DataContext is MockupViewModel vm)
                vm.TemplateDesignerHeight = newHeight;

            templateDesigner.Height = newHeight;
            return;
        }

        if (DesignerKind == DesignerKind.Popup
            && this is PopupDesigner popupDesigner
            && popupDesigner.ScreenPopup != null)
        {
            popupDesigner.ScreenPopup.Height = newHeight;

            if (DataContext is MockupViewModel vm)
                vm.PopupDesignerHeight = newHeight;

            popupDesigner.Height = newHeight;
            return;
        }

        resizeBand.Height = newHeight;

        if (resizeBand.IsExpandable)
            resizeBand.SavedExpandedHeight = Math.Max(resizeBand.SavedExpandedHeight, newHeight);

        if (resizeBand.ActivePage != null)
            resizeBand.ActivePage.Height = newHeight;

        if (resizeBand.UniformPageHeight)
            resizeBand.SyncPageHeights();
    }

    private static float GetBandContentMinHeightFromControls(Band band)
    {
        float Round(float v) => MathF.Round(v);

        const float PADDING_BOTTOM = 10f;

        float GetRequiredHeight(BandPage? page)
        {
            if (page == null || page.Controls == null || page.Controls.Count == 0)
                return 0f;

            float maxBottom = 0f;

            foreach (var c in page.Controls)
            {
                float bottom = Round(c.Y + c.Height);
                if (bottom > maxBottom)
                    maxBottom = bottom;
            }

            return Round(maxBottom + PADDING_BOTTOM);
        }

        if (band.UniformPageHeight)
        {
            if (band.Pages == null || band.Pages.Count == 0)
                return 0f;

            float requiredAcrossPages = 0f;

            foreach (var p in band.Pages)
            {
                float required = GetRequiredHeight(p);
                if (required > requiredAcrossPages)
                    requiredAcrossPages = required;
            }

            return requiredAcrossPages;
        }

        return GetRequiredHeight(band.ActivePage);
    }

    private static float GetBandMinHeightFromContent(Band band)
    {
        float Round(float v) => MathF.Round(v);

        const float PADDING_BOTTOM = 10f;

        float headerH = Round(band.HeaderHeight);
        float baseMin = headerH;

        if (band.MinHeight > 0)
            baseMin = Math.Max(baseMin, Round(band.MinHeight));

        if (band.UniformPageHeight)
        {
            if (band.Pages == null || band.Pages.Count == 0)
                return baseMin;

            float requiredAcrossPages = 0f;
            bool anyControls = false;

            foreach (var p in band.Pages)
            {
                if (p == null || p.Controls == null || p.Controls.Count == 0)
                    continue;

                anyControls = true;

                float maxBottom = 0f;

                foreach (var c in p.Controls)
                {
                    float bottom = Round(c.Y + c.Height);
                    if (bottom > maxBottom)
                        maxBottom = bottom;
                }

                float contentMin = headerH + Round(maxBottom + PADDING_BOTTOM);
                if (contentMin > requiredAcrossPages)
                    requiredAcrossPages = contentMin;
            }

            if (!anyControls)
                return baseMin;

            return Math.Max(baseMin, requiredAcrossPages);
        }

        var page = band.ActivePage;

        if (page == null || page.Controls == null || page.Controls.Count == 0)
            return baseMin;

        float maxBottomActive = 0f;

        foreach (var c in page.Controls)
        {
            float bottom = Round(c.Y + c.Height);
            if (bottom > maxBottomActive)
                maxBottomActive = bottom;
        }

        float contentMinActive = headerH + Round(maxBottomActive + PADDING_BOTTOM);

        return Math.Max(baseMin, contentMinActive);
    }

    #endregion === BAND RESIZE ===

    #region === BAND HOVER ===

    private void ResetBandHoverState()
    {
        foreach (var b in CustomBands)
        {
            b.IsMouseOverBand = false;
            b.IsMouseNearLeft = false;
            b.IsMouseNearRight = false;
        }

        if (HeaderBand != null)
            HeaderBand.IsMouseOverBand = false;

        if (FooterBand != null)
            FooterBand.IsMouseOverBand = false;
    }

    private bool UpdateBandHoverAndCursor(SKPoint pt, Band? band)
    {
        if (band == null)
            return false;

        band.IsMouseOverBand = true;

        float localX = pt.X - band.WorldBounds.Left;

        band.IsMouseNearLeft = localX < 40 && pt.Y > band.WorldBounds.Bottom - 20;

        band.IsMouseNearRight =
            localX > band.WorldBounds.Width - 40 && pt.Y > band.WorldBounds.Bottom - 20;

        return false;
    }

    private bool HandleBandResizeAndToggleHover(SKPoint pt, Band? band)
    {
        if (band == null)
            return false;

        if (band.HitTestResize(pt))
        {
            Cursor = Cursors.SizeNS;
            return true;
        }

        if (band.HitTestToggle(pt))
        {
            Cursor = Cursors.Hand;
            return true;
        }

        return false;
    }

    private bool CheckIfPointerisOverBandPageTabs(SKPoint pt, Band? band)
    {
        if (band == null)
            return false;

        bool checkTabs = band.ShowTabs;
        if (band.IsExpandable)
            checkTabs &= band.IsExpanded;

        if (checkTabs)
        {
            int tab = band.HitTestTabIndex(pt);
            if (tab >= 0)
            {
                return true;
            }
        }
        return false;
    }

    #endregion === BAND HOVER / BUTTONS ===

    #region === HANDLE CONTROL DRAG ===

    private bool HandleControlDrag(SKPoint pt)
    {
        if (VM?.SelectedControls.Count == 0 || !PART_Canvas.IsMouseCaptured)
            return false;

        float dx = pt.X - _controlDragStartMouseWorld.X;
        float dy = pt.Y - _controlDragStartMouseWorld.Y;

        if (Math.Abs(dx) <= 1 && Math.Abs(dy) <= 1)
        {
            ClearAlignmentGuidelines();
            return false;
        }

        _isDraggingControls = true;

        ClampGroupDeltaToBoundsFromStart(_controlDragStartLocal, ref dx, ref dy);

        bool geometryChanged = false;

        foreach (var kv in _controlDragStartLocal)
        {
            var ctrl = kv.Key;
            float targetX = MathF.Round(kv.Value.X + dx);
            float targetY = MathF.Round(kv.Value.Y + dy);

            if (ctrl.X != targetX || ctrl.Y != targetY)
            {
                geometryChanged = true;
                break;
            }
        }

        Cursor = Cursors.Hand;

        if (geometryChanged)
        {
            if (_controlDragCopyRequested)
                StartControlCopyDrag();

            if (!_controlDragSnapshotPushed)
            {
                var snapshotContext = GetSnapshotContextForDesigner();
                if (snapshotContext != null)
                {
                    MockupService.Mockup.PushSnapshot(snapshotContext.Value, SnapshotLabels.ControlMoved);
                    _controlDragSnapshotPushed = true;
                }
            }

            foreach (var kv in _controlDragStartLocal)
            {
                var ctrl = kv.Key;
                float targetX = MathF.Round(kv.Value.X + dx);
                float targetY = MathF.Round(kv.Value.Y + dy);

                if (ctrl.X != targetX)
                    ctrl.X = targetX;

                if (ctrl.Y != targetY)
                    ctrl.Y = targetY;
            }
        }

        UpdateAlignmentGuidelinesDuringControlDrag();
        UpdateControlDragInteractionHint(pt, dx, dy);

        InvalidateDesigner();
        return true;
    }

    private void UpdateControlDragInteractionHint(SKPoint pointer, float dx, float dy)
    {
        _ = pointer;
        _ = dx;
        _ = dy;

        var selectedControls = VM?.SelectedControls;
        if (selectedControls == null || selectedControls.Count == 0)
        {
            ClearDesignerInteractionHint();
            return;
        }

        if (selectedControls.Count == 1)
        {
            var ctrl = selectedControls[0];

            _designerInteractionHintText =
                $"X {MathF.Round(ctrl.X):0}   Y {MathF.Round(ctrl.Y):0}";
        }
        else
        {
            var groupPosition = GetCurrentSelectionPosition(selectedControls);
            if (groupPosition == null)
            {
                ClearDesignerInteractionHint();
                return;
            }

            _designerInteractionHintText =
                $"X {MathF.Round(groupPosition.Value.X):0}"
                + $"   Y {MathF.Round(groupPosition.Value.Y):0}";
        }

        UpdateDesignerInteractionHintAnchor(selectedControls);
    }

    private static SKPoint? GetCurrentSelectionPosition(
        IEnumerable<DesignControl> controls)
    {
        var selectedControls = controls
            .Where(ctrl => ctrl?.ParentBandPage != null)
            .Distinct()
            .ToList();

        if (selectedControls.Count == 0)
            return null;

        var firstPage = selectedControls[0].ParentBandPage;

        bool samePage = selectedControls.All(
            ctrl => ReferenceEquals(ctrl.ParentBandPage, firstPage));

        if (samePage)
        {
            return new SKPoint(
                selectedControls.Min(ctrl => ctrl.X),
                selectedControls.Min(ctrl => ctrl.Y));
        }

        float left = float.MaxValue;
        float top = float.MaxValue;

        foreach (var ctrl in selectedControls)
        {
            var page = ctrl.ParentBandPage;
            if (page == null)
                continue;

            left = Math.Min(left, page.WorldBounds.Left + ctrl.X);
            top = Math.Min(top, page.WorldBounds.Top + ctrl.Y);
        }

        return left == float.MaxValue
            ? null
            : new SKPoint(left, top);
    }

    private void UpdateControlResizeInteractionHint(SKPoint pointer, DesignControl ctrl)
    {
        _ = pointer;

        _designerInteractionHintText =
            $"W {MathF.Round(ctrl.Width):0}   H {MathF.Round(ctrl.Height):0}";

        UpdateDesignerInteractionHintAnchor([ctrl]);
    }

    private void UpdateDesignerInteractionHintAnchor(
        IEnumerable<DesignControl> controls)
    {
        float left = float.MaxValue;
        float top = float.MaxValue;
        float right = float.MinValue;
        float bottom = float.MinValue;

        foreach (var ctrl in controls)
        {
            var page = ctrl.ParentBandPage;
            if (page == null)
                continue;

            float controlLeft = page.WorldBounds.Left + ctrl.X;
            float controlTop = page.WorldBounds.Top + ctrl.Y;
            float controlRight = controlLeft + ctrl.Width;
            float controlBottom = controlTop + ctrl.Height;

            left = Math.Min(left, controlLeft);
            top = Math.Min(top, controlTop);
            right = Math.Max(right, controlRight);
            bottom = Math.Max(bottom, controlBottom);
        }

        if (left == float.MaxValue)
        {
            _designerInteractionHintAnchor = SKPoint.Empty;
            _designerInteractionHintFallbackY = 0f;
            return;
        }

        _designerInteractionHintAnchor = new SKPoint(
            MathF.Round((left + right) / 2f),
            MathF.Round(top));

        _designerInteractionHintFallbackY = MathF.Round(bottom);
    }

    private void ClearDesignerInteractionHint()
    {
        _designerInteractionHintText = null;
        _designerInteractionHintAnchor = SKPoint.Empty;
        _designerInteractionHintFallbackY = 0f;
    }

    private static string FormatSignedDesignerValue(float value)
    {
        return value > 0f
            ? $"+{value:0}"
            : $"{value:0}";
    }

    private void StartControlCopyDrag()
    {
        _controlDragCopyRequested = false;
        _pendingCtrlClickToggleControl = null;

        if (VM?.SelectedControls == null || VM.SelectedControls.Count == 0)
            return;

        var sourceGroups = VM.SelectedControls
            .Where(ctrl => ctrl?.ParentBandPage != null)
            .Distinct()
            .GroupBy(ctrl => ctrl.ParentBandPage!)
            .ToList();

        if (sourceGroups.Count == 0)
            return;

        var snapshotContext = GetSnapshotContextForDesigner();
        if (snapshotContext != null)
            MockupService.Mockup.PushSnapshot(
                snapshotContext.Value,
                SnapshotLabels.ControlDuplicated);

        _controlDragSnapshotPushed = true;

        var copies = new List<DesignControl>();

        foreach (var sourceGroup in sourceGroups)
        {
            var page = sourceGroup.Key;
            int nextZ = page.Controls.Count == 0
                ? 0
                : page.Controls.Max(ctrl => ctrl.ZIndex) + 1;

            foreach (var source in sourceGroup.OrderBy(ctrl => ctrl.ZIndex))
            {
                var copy = source.DeepClone();

                copy.ParentBand = source.ParentBand;
                copy.ParentBandPage = page;
                copy.ZIndex = nextZ++;

                page.Controls.Add(copy);
                copies.Add(copy);
            }

            NormalizeZOrder(page);
        }

        if (copies.Count == 0)
            return;

        DeselectAllControls();

        _controlDragStartLocal.Clear();

        foreach (var copy in copies)
        {
            SelectControl(copy);
            _controlDragStartLocal[copy] = new SKPoint(copy.X, copy.Y);
        }
    }

    private void ResetControlDragModifierState()
    {
        _controlDragCopyRequested = false;
        _pendingCtrlClickToggleControl = null;
    }

    protected void ClampGroupDeltaToBoundsFromStart(
        IReadOnlyDictionary<DesignControl, SKPoint> startLocal,
        ref float dx,
        ref float dy
    )
    {
        if (startLocal.Count == 0)
            return;

        var bounds = GetDesignerWorldBounds();

        float minWorldX = float.MaxValue;
        float minWorldY = float.MaxValue;
        float maxWorldX = float.MinValue;
        float maxWorldY = float.MinValue;

        foreach (var kv in startLocal)
        {
            var ctrl = kv.Key;
            var start = kv.Value;

            var page = ctrl.ParentBandPage;
            if (page == null)
                continue;

            float left = page.WorldBounds.Left + start.X;
            float top = page.WorldBounds.Top + start.Y;

            float right = left + ctrl.Width;
            float bottom = top + ctrl.Height;

            minWorldX = Math.Min(minWorldX, left);
            minWorldY = Math.Min(minWorldY, top);
            maxWorldX = Math.Max(maxWorldX, right);
            maxWorldY = Math.Max(maxWorldY, bottom);
        }

        if (minWorldX == float.MaxValue)
            return;

        if (minWorldX + dx < bounds.Left)
            dx = bounds.Left - minWorldX;

        if (minWorldY + dy < bounds.Top)
            dy = bounds.Top - minWorldY;

        if (maxWorldX + dx > bounds.Right)
            dx = bounds.Right - maxWorldX;

        if (maxWorldY + dy > bounds.Bottom)
            dy = bounds.Bottom - maxWorldY;
    }

    private void ResolveControlParentAfterDrag(IEnumerable<DesignControl> controls)
    {
        foreach (var ctrl in controls)
        {
            var oldPage = ctrl.ParentBandPage;
            var oldBand = ctrl.ParentBand;

            if (oldPage == null || oldBand == null)
                continue;

            float midY = ctrl.VisualRect.MidY;

            Band? targetBand = null;

            foreach (var band in GetAllBands())
            {
                if (band.BandType == BandType.Header && !Screen.ShowHeader)
                    continue;

                if (band.BandType == BandType.Footer && !Screen.ShowFooter)
                    continue;

                if (band.IsExpandable && !band.IsExpanded)
                    continue;

                if (band.WorldBounds.Top <= midY && band.WorldBounds.Bottom >= midY)
                {
                    targetBand = band;
                    break;
                }
            }

            if (targetBand == null)
                continue;

            var targetPage = targetBand.ActivePage;
            if (targetPage == null)
                continue;

            if (targetPage == oldPage)
                continue;

            oldPage.Controls.Remove(ctrl);
            NormalizeZOrder(oldPage);

            float newX = ctrl.VisualRect.Left - targetBand.ContentRect.Left;
            float newY = ctrl.VisualRect.Top - targetBand.ContentRect.Top;

            ctrl.X = newX;
            ctrl.Y = newY;

            ctrl.ParentBand = targetBand;
            ctrl.ParentBandPage = targetPage;

            ctrl.ZIndex = GetNextTopZ(targetPage);
            targetPage.Controls.Add(ctrl);

            NormalizeZOrder(targetPage);
        }
    }

    #endregion === HANDLE CONTROL DRAG ===

    #region === CONTROL RESIZE ===
    private static ControlResizeHandle NormalizeResizeHandleForControl(
        DesignControl ctrl,
        ControlResizeHandle handle
    )
    {
        if (ctrl is not ActionArea)
            return handle;

        return handle switch
        {
            ControlResizeHandle.Top => ControlResizeHandle.None,
            _ => handle,
        };
    }

    private static bool IsResizeHandleAllowed(DesignControl ctrl, ControlResizeHandle handle)
    {
        return NormalizeResizeHandleForControl(ctrl, handle) != ControlResizeHandle.None;
    }
    private bool ApplyControlResize(
        DesignControl ctrl,
        ControlResizeHandle handle,
        float dx,
        float dy,
        SKRect startRect,
        Action beforeApply
    )
    {
        handle = NormalizeResizeHandleForControl(ctrl, handle);
        if (handle == ControlResizeHandle.None)
            return false;

        float x = startRect.Left;
        float y = startRect.Top;
        float w = startRect.Width;
        float h = startRect.Height;

        bool allowWidth =
            ctrl.ResizeStyle == ResizeStyles.ResizeAll
            || ctrl.ResizeStyle == ResizeStyles.WidthOnly
            || ctrl.ResizeStyle == ResizeStyles.KeepRatio;

        bool allowHeight =
            ctrl.ResizeStyle == ResizeStyles.ResizeAll
            || ctrl.ResizeStyle == ResizeStyles.HeightOnly
            || ctrl.ResizeStyle == ResizeStyles.KeepRatio;

        if (ctrl.ResizeStyle == ResizeStyles.KeepRatio && handle != ControlResizeHandle.BottomRight)
            return false;

        switch (handle)
        {
            case ControlResizeHandle.Right:
                if (allowWidth)
                    w += dx;
                break;

            case ControlResizeHandle.Left:
                if (allowWidth)
                {
                    x += dx;
                    w -= dx;
                }
                break;

            case ControlResizeHandle.Bottom:
                if (allowHeight)
                    h += dy;
                break;

            case ControlResizeHandle.Top:
                if (allowHeight)
                {
                    y += dy;
                    h -= dy;
                }
                break;

            case ControlResizeHandle.BottomRight:
                if (allowWidth)
                    w += dx;
                if (allowHeight)
                    h += dy;
                break;

            case ControlResizeHandle.BottomLeft:
                if (allowWidth)
                {
                    x += dx;
                    w -= dx;
                }
                if (allowHeight)
                    h += dy;
                break;

            case ControlResizeHandle.TopRight:
                if (allowWidth)
                    w += dx;
                if (allowHeight)
                {
                    y += dy;
                    h -= dy;
                }
                break;

            case ControlResizeHandle.TopLeft:
                if (allowWidth)
                {
                    x += dx;
                    w -= dx;
                }
                if (allowHeight)
                {
                    y += dy;
                    h -= dy;
                }
                break;
        }

        if (ctrl.ResizeStyle == ResizeStyles.KeepRatio)
        {
            float ratio = startRect.Height <= 0.0001f ? 1f : startRect.Width / startRect.Height;

            if (Math.Abs(dx) > Math.Abs(dy))
                h = w / ratio;
            else
                w = h * ratio;
        }

        w = Math.Clamp(w, ctrl.MinWidth, ctrl.MaxWidth);
        h = Math.Clamp(h, ctrl.MinHeight, ctrl.MaxHeight);

        w = Math.Clamp(w, ctrl.MinWidth, ctrl.MaxWidth);
        h = Math.Clamp(h, ctrl.MinHeight, ctrl.MaxHeight);

        if (
            handle
            is ControlResizeHandle.Left
                or ControlResizeHandle.TopLeft
                or ControlResizeHandle.BottomLeft
        )
        {
            x = startRect.Right - w;
        }

        if (
            handle
            is ControlResizeHandle.Top
                or ControlResizeHandle.TopRight
                or ControlResizeHandle.TopLeft
        )
        {
            y = startRect.Bottom - h;
        }

        var page = ctrl.ParentBandPage;
        if (page == null)
            return false;

        float maxWidthInPage = IsLeftResizeHandle(handle)
            ? startRect.Right - page.WorldBounds.Left
            : page.WorldBounds.Right - startRect.Left;
        float maxHeightInPage = IsTopResizeHandle(handle)
            ? startRect.Bottom - page.WorldBounds.Top
            : page.WorldBounds.Bottom - startRect.Top;

        maxWidthInPage = Math.Max(0f, maxWidthInPage);
        maxHeightInPage = Math.Max(0f, maxHeightInPage);

        if (ctrl.ResizeStyle == ResizeStyles.KeepRatio)
        {
            float ratio = startRect.Height <= 0.0001f ? 1f : startRect.Width / startRect.Height;
            float maxWidth = Math.Min(maxWidthInPage, maxHeightInPage * ratio);

            w = Math.Min(w, maxWidth);
            h = ratio <= 0.0001f ? h : w / ratio;
        }
        else
        {
            w = Math.Min(w, maxWidthInPage);
            h = Math.Min(h, maxHeightInPage);
        }

        if (IsLeftResizeHandle(handle))
            x = startRect.Right - w;

        if (IsTopResizeHandle(handle))
            y = startRect.Bottom - h;

        float localX = x - page.WorldBounds.Left;
        float localY = y - page.WorldBounds.Top;

        float maxX = Math.Max(0f, page.WorldBounds.Width - w);
        float maxY = Math.Max(0f, page.WorldBounds.Height - h);

        localX = Math.Clamp(localX, 0f, maxX);
        localY = Math.Clamp(localY, 0f, maxY);

        float targetX = MathF.Round(localX);
        float targetY = MathF.Round(localY);
        float targetWidth = MathF.Round(w);
        float targetHeight = MathF.Round(h);

        if (ctrl.X == targetX
            && ctrl.Y == targetY
            && ctrl.Width == targetWidth
            && ctrl.Height == targetHeight)
        {
            return false;
        }

        beforeApply();

        if (ctrl.X != targetX)
            ctrl.X = targetX;

        if (ctrl.Y != targetY)
            ctrl.Y = targetY;

        if (ctrl.Width != targetWidth)
            ctrl.Width = targetWidth;

        if (ctrl.Height != targetHeight)
            ctrl.Height = targetHeight;

        return true;
    }

    private static bool IsLeftResizeHandle(ControlResizeHandle handle)
    {
        return handle
            is ControlResizeHandle.Left
                or ControlResizeHandle.TopLeft
                or ControlResizeHandle.BottomLeft;
    }

    private static bool IsRightResizeHandle(ControlResizeHandle handle)
    {
        return handle
            is ControlResizeHandle.Right
                or ControlResizeHandle.TopRight
                or ControlResizeHandle.BottomRight;
    }

    private static bool IsTopResizeHandle(ControlResizeHandle handle)
    {
        return handle
            is ControlResizeHandle.Top
                or ControlResizeHandle.TopLeft
                or ControlResizeHandle.TopRight;
    }

    private static bool IsBottomResizeHandle(ControlResizeHandle handle)
    {
        return handle
            is ControlResizeHandle.Bottom
                or ControlResizeHandle.BottomLeft
                or ControlResizeHandle.BottomRight;
    }

    #endregion === CONTROL RESIZE ===

    #region === MOUSE WHEEL / SCROLL ===

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (IsPopup)
        {
            ScrollOffsetY += e.Delta > 0 ? 40 : -40;
            InvalidateDesigner();
            return;
        }

        if (_mouseState.Mode == DesignerMouseMode.ResizingBand)
            return;

        if (!AllowBandInteraction)
            return;

        var pt = MouseWorldPoint;
        var band = HitTestBand(pt);

        if (band != null && band.ShowTabs && band.HitTestTabStrip(pt))
        {
            float step = 20;
            float delta = e.Delta > 0 ? step : -step;

            band.TabScrollOffsetX += delta;
            band.ClampTabScroll();
            InvalidateDesigner();
            return;
        }

        ScrollOffsetY += e.Delta > 0 ? 50 : -50;
        InvalidateDesigner();
    }

    #endregion === MOUSE WHEEL / SCROLL ===

    #region === EXTERNAL SCROLLBAR ===

    private void OnScrollBarChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_mouseState.Mode == DesignerMouseMode.ResizingBand)
            return;

        if (!AllowBandInteraction)
            return;

        ScrollOffsetY = (float)-e.NewValue;
        InvalidateDesigner();
    }

    #endregion === EXTERNAL SCROLLBAR ===

    #region === HITTEST BAND / PAGE ===

    internal Band? HitTestBand(SKPoint p)
    {
        bool HitBandBody(Band b) => b.WorldBounds.Contains(p);

        if (HeaderBand != null && HitBandBody(HeaderBand))
            return HeaderBand;

        if (FooterBand != null)
        {
            if (HitBandBody(FooterBand))
                return FooterBand;

            if (!FooterBand.ResizeThumbRect.IsEmpty && FooterBand.ResizeThumbRect.Contains(p))
                return FooterBand;
        }

        foreach (var b in CustomBands.Reverse())
        {
            if (HitBandBody(b))
                return b;
        }

        return null;
    }

    internal BandPage? HitTestBandPage(SKPoint p, out Band? band)
    {
        band = HitTestBand(p);

        if (band == null)
            return null;

        if (!band.ContentRect.Contains(p))
            return null;

        return band.ActivePage;
    }

    #endregion === HITTEST BAND / PAGE ===

    #region === HELPERS ===
    private static SKRect GetSelectionWorldBounds(IEnumerable<DesignControl> controls)
    {
        float left = float.MaxValue;
        float top = float.MaxValue;
        float right = float.MinValue;
        float bottom = float.MinValue;

        foreach (var c in controls)
        {
            var r = c.VisualRect;
            if (r.IsEmpty)
                continue;

            left = Math.Min(left, r.Left);
            top = Math.Min(top, r.Top);
            right = Math.Max(right, r.Right);
            bottom = Math.Max(bottom, r.Bottom);
        }

        return left == float.MaxValue ? SKRect.Empty : new SKRect(left, top, right, bottom);
    }

    private static int GetNextTopZ(BandPage page)
    {
        if (page.Controls == null || page.Controls.Count == 0)
            return 0;

        return page.Controls.Max(c => c.ZIndex) + 1;
    }

    private static void NormalizeZOrder(BandPage page)
    {
        if (page.Controls == null || page.Controls.Count <= 1)
        {
            if (page.Controls != null && page.Controls.Count == 1)
                page.Controls[0].ZIndex = 0;

            return;
        }

        var ordered = page.Controls
            .Select((ctrl, index) => new { Ctrl = ctrl, Index = index })
            .OrderBy(x => x.Ctrl.ZIndex)
            .ThenBy(x => x.Index)
            .Select(x => x.Ctrl)
            .ToList();

        for (int i = 0; i < ordered.Count; i++)
            ordered[i].ZIndex = i;
    }

    internal DesignControl.PointerContext CreatePointerContext(
        SKPoint pt,
        MouseButton? button,
        int clickCount
    )
    {
        bool ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

        bool shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

        bool alt = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);

        return new DesignControl.PointerContext(pt, button, clickCount, LiveMode, ctrl, shift, alt);
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key == Key.Space)
        {
            InvalidateDesigner();
            e.Handled = true;
            return;
        }

        if (IsPreview)
            return;

        if (VM == null)
            return;

        if (HandleSnapshotShortcut(e))
            return;

        if (HandleClipboardShortcut(e))
            return;

        if (HandleEditDesignerShortcut(e))
            return;

        if (HandleDeleteShortcut(e))
            return;

        if (VM.SelectedControls.Count == 0)
            return;

        float step = GetKeyboardNudgeStep(e);

        float dx = 0f;
        float dy = 0f;

        switch (key)
        {
            case Key.Left:
                dx = -step;
                break;

            case Key.Right:
                dx = step;
                break;

            case Key.Up:
                dy = -step;
                break;

            case Key.Down:
                dy = step;
                break;

            default:
                return;
        }

        NudgeSelectedControls(dx, dy);
        e.Handled = true;
    }

    private void OnPreviewKeyUp(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key is Key.Left or Key.Right or Key.Up or Key.Down)
        {
            _keyboardNudgeSnapshotPushed = false;
            e.Handled = true;
            return;
        }

        if (key != Key.Space)
            return;

        InvalidateDesigner();
        e.Handled = true;
    }

    private SnapshotContext? GetSnapshotContextForDesigner()
    {
        return DesignerKind switch
        {
            DesignerKind.Screen => SnapshotContext.Screen,
            DesignerKind.Template => SnapshotContext.Template,
            DesignerKind.Popup => SnapshotContext.Popup,
            _ => null,
        };
    }

    private bool HandleSnapshotShortcut(KeyEventArgs e)
    {
        bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

        if (ctrl && e.Key == Key.Z)
        {
            VM?.UndoCommand.Execute(null);
            e.Handled = true;
            return true;
        }

        if (ctrl && e.Key == Key.Y)
        {
            VM?.RedoCommand.Execute(null);
            e.Handled = true;
            return true;
        }

        return false;
    }


    private bool HandleClipboardShortcut(KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
            return false;

        if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            return false;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        switch (key)
        {
            case Key.C:
                if (VM.SelectedControls.Count == 0)
                    return false;

                var first = VM.SelectedControls.FirstOrDefault();
                VM.SetContextControls(first?.ParentBand, VM.SelectedControls.ToList());
                VM.CopyControlsCommand.Execute(null);

                e.Handled = true;
                return true;

            case Key.V:
                if (!VM.CanPasteControls)
                    return false;

                VM.PasteControlsCommand.Execute(null);

                e.Handled = true;
                return true;

            default:
                return false;
        }
    }

    private bool HandleEditDesignerShortcut(KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) != ModifierKeys.None)
            return false;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key != Key.Enter)
            return false;

        switch (DesignerKind)
        {
            case DesignerKind.Screen when VM.CurrentScreen != null:
                VM.SetContextScreen(VM.CurrentScreen);
                if (!VM.EditScreenCommand.CanExecute(null))
                    return false;

                VM.EditScreenCommand.Execute(null);
                break;

            case DesignerKind.Template when VM.CurrentTemplate != null:
                VM.SetContextTemplate(VM.CurrentTemplate);
                if (!VM.EditTemplateCommand.CanExecute(VM.CurrentTemplate))
                    return false;

                VM.EditTemplateCommand.Execute(VM.CurrentTemplate);
                break;

            case DesignerKind.Popup when VM.CurrentPopup != null:
                VM.SetContextPopup(VM.CurrentPopup);
                if (!VM.EditPopupCommand.CanExecute(VM.CurrentPopup))
                    return false;

                VM.EditPopupCommand.Execute(VM.CurrentPopup);
                break;

            default:
                return false;
        }

        FocusDesignerSurface();
        e.Handled = true;
        return true;
    }

    private bool HandleDeleteShortcut(KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) != ModifierKeys.None)
            return false;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key != Key.Delete)
            return false;

        if (VM.SelectedControls.Count == 0)
            return false;

        var controls = VM.SelectedControls.ToList();
        var first = controls.FirstOrDefault();
        VM.SetContextControls(first?.ParentBand, controls);
        VM.DeleteControlsCommand.Execute(null);

        FocusDesignerSurface();
        e.Handled = true;
        return true;
    }

    private float GetKeyboardNudgeStep(KeyEventArgs e)
    {
        bool shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        bool ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

        if (shift)
            return 5f;

        if (!ctrl)
            return 1f;

        return 10f;
    }

    private void NudgeSelectedControls(float dx, float dy)
    {
        if (VM.SelectedControls.Count == 0)
            return;

        var startLocal = new Dictionary<DesignControl, SKPoint>();

        foreach (var ctrl in VM.SelectedControls)
            startLocal[ctrl] = new SKPoint(ctrl.X, ctrl.Y);

        ClampGroupDeltaToBoundsFromStart(startLocal, ref dx, ref dy);

        if (Math.Abs(dx) < 0.0001f && Math.Abs(dy) < 0.0001f)
            return;

        var snapshotContext = GetSnapshotContextForDesigner();
        if (snapshotContext != null && !_keyboardNudgeSnapshotPushed)
        {
            MockupService.Mockup.PushSnapshot(snapshotContext.Value, SnapshotLabels.ControlMoved);
            _keyboardNudgeSnapshotPushed = true;
        }

        foreach (var kv in startLocal)
        {
            kv.Key.X = MathF.Round(kv.Value.X + dx);
            kv.Key.Y = MathF.Round(kv.Value.Y + dy);
        }

        InvalidateDesigner();
    }

    private void NormalizeAllPagesZOrder()
    {
        var allBands = GetAllBands();
        if (allBands == null)
            return;

        foreach (var band in allBands)
        {
            if (band?.Pages == null)
                continue;

            foreach (var page in band.Pages)
            {
                if (page == null)
                    continue;

                NormalizeZOrder(page);
            }
        }
    }

    private void TryPromoteDraggedControlAboveCoveringSurface(IEnumerable<DesignControl>? controls)
    {
        if (controls == null)
            return;

        var dragged = controls
            .Where(c => c != null)
            .Distinct()
            .ToList();

        if (dragged.Count == 0)
            return;

        var page = dragged[0].ParentBandPage;
        if (page == null || page.Controls == null || page.Controls.Count <= 1)
            return;

        if (dragged.Any(c => c.ParentBandPage != page))
            return;

        float left = float.MaxValue;
        float top = float.MaxValue;
        float right = float.MinValue;
        float bottom = float.MinValue;

        float draggedAreaSum = 0f;
        int draggedTopZ = int.MinValue;

        foreach (var ctrl in dragged)
        {
            var rect = new SKRect(
                page.WorldBounds.Left + ctrl.X,
                page.WorldBounds.Top + ctrl.Y,
                page.WorldBounds.Left + ctrl.X + ctrl.Width,
                page.WorldBounds.Top + ctrl.Y + ctrl.Height
            );

            if (rect.IsEmpty)
                continue;

            left = Math.Min(left, rect.Left);
            top = Math.Min(top, rect.Top);
            right = Math.Max(right, rect.Right);
            bottom = Math.Max(bottom, rect.Bottom);

            draggedAreaSum += Math.Max(0f, ctrl.Width) * Math.Max(0f, ctrl.Height);
            draggedTopZ = Math.Max(draggedTopZ, ctrl.ZIndex);
        }

        if (left == float.MaxValue || draggedAreaSum <= 0f)
            return;

        var groupRect = new SKRect(left, top, right, bottom);

        DesignControl? coveringCtrl = null;
        int highestCoveringZ = int.MinValue;

        foreach (var other in page.Controls)
        {
            if (other == null || dragged.Contains(other))
                continue;

            if (other.ZIndex <= draggedTopZ)
                continue;

            float otherArea = Math.Max(0f, other.Width) * Math.Max(0f, other.Height);
            if (otherArea <= draggedAreaSum)
                continue;

            var otherRect = new SKRect(
                page.WorldBounds.Left + other.X,
                page.WorldBounds.Top + other.Y,
                page.WorldBounds.Left + other.X + other.Width,
                page.WorldBounds.Top + other.Y + other.Height
            );

            if (otherRect.IsEmpty)
                continue;

            if (!groupRect.IntersectsWith(otherRect))
                continue;

            if (other.ZIndex > highestCoveringZ)
            {
                highestCoveringZ = other.ZIndex;
                coveringCtrl = other;
            }
        }

        if (coveringCtrl == null)
            return;

        var orderedDragged = dragged
            .OrderBy(c => c.ZIndex)
            .ToList();

        int baseZ = coveringCtrl.ZIndex + 1;

        for (int i = 0; i < orderedDragged.Count; i++)
            orderedDragged[i].ZIndex = baseZ + i;

        NormalizeZOrder(page);
    }

    #endregion === HELPER ===

    #region === RUBBERBAND HELPERS ===

    private void StartRubberbandSelection(SKPoint pt, bool additive)
    {
        _isRubberbandSelecting = true;
        _rubberbandAdditiveSelection = additive;
        _rubberbandStartMouseWorld = pt;
        _rubberbandCurrentMouseWorld = pt;
        _rubberbandWorldRect = SKRect.Create(pt.X, pt.Y, 0, 0);
    }

    private void UpdateRubberbandSelection(SKPoint pt)
    {
        _rubberbandCurrentMouseWorld = pt;
        _rubberbandWorldRect = CreateNormalizedRubberbandRect(
            _rubberbandStartMouseWorld,
            _rubberbandCurrentMouseWorld
        );
    }

    private void FinishRubberbandSelection()
    {
        var start = _rubberbandStartMouseWorld;
        var current = _rubberbandCurrentMouseWorld;
        var rect = CreateNormalizedRubberbandRect(start, current);
        bool additive = _rubberbandAdditiveSelection;

        bool hadDrag =
            Math.Abs(current.X - start.X) >= RubberbandStartThreshold
            || Math.Abs(current.Y - start.Y) >= RubberbandStartThreshold;

        CancelRubberbandSelection();

        if (!additive)
            DeselectAllControls();

        if (!hadDrag)
            return;

        LayoutPrepass();

        var allBands = GetAllBands();
        if (allBands == null)
            return;

        foreach (var band in allBands)
        {
            if (!IsBandVisibleForRubberbandSelection(band))
                continue;

            var page = band.ActivePage;
            if (page == null || page.Controls == null || page.Controls.Count == 0)
                continue;

            foreach (var ctrl in page.Controls)
            {
                UpdateControlVisualRectForRubberband(ctrl);

                if (IntersectsRubberband(rect, ctrl.VisualRect))
                {
                    SelectControl(ctrl);
                    SelectedBand = band;
                }
            }
        }
    }

    private void CancelRubberbandSelection()
    {
        _isRubberbandSelecting = false;
        _rubberbandAdditiveSelection = false;
        _rubberbandWorldRect = SKRect.Empty;
    }

    private bool IsBandVisibleForRubberbandSelection(Band band)
    {
        if (band.IsExpandable && !band.IsExpanded)
            return false;

        if (Screen != null)
        {
            if (band.BandType == BandType.Header && !Screen.ShowHeader)
                return false;

            if (band.BandType == BandType.Footer && !Screen.ShowFooter)
                return false;
        }

        return true;
    }

    private static SKRect CreateNormalizedRubberbandRect(SKPoint a, SKPoint b)
    {
        float left = Math.Min(a.X, b.X);
        float top = Math.Min(a.Y, b.Y);
        float right = Math.Max(a.X, b.X);
        float bottom = Math.Max(a.Y, b.Y);

        return new SKRect(left, top, right, bottom);
    }

    private void UpdateControlVisualRectForRubberband(DesignControl ctrl)
    {
        var page = ctrl.ParentBandPage;
        if (page == null)
            return;

        var content = page.WorldBounds;

        ctrl.VisualRect = new SKRect(
            content.Left + ctrl.X,
            content.Top + ctrl.Y,
            content.Left + ctrl.X + ctrl.Width,
            content.Top + ctrl.Y + ctrl.Height
        );
    }

    private static bool IntersectsRubberband(SKRect selection, SKRect controlRect)
    {
        if (selection.IsEmpty || controlRect.IsEmpty)
            return false;

        return controlRect.Left >= selection.Left
            && controlRect.Top >= selection.Top
            && controlRect.Right <= selection.Right
            && controlRect.Bottom <= selection.Bottom;
    }

    #endregion === RUBBERBAND HELPERS ===
}
