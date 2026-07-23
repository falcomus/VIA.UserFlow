// ======================================================================================
// FILE: Mockup.Designer/PreviewInteractionService.cs
//
// PURPOSE:
// - Zentrale Preview-/Live-Interaktionslogik für BaseDesigner
// - Keine Designer/Edit-Logik, nur Preview-Dispatch
// - Controls entscheiden selbst über ihr Verhalten via OnPointerDown/Move/Up
// - Hint-Handling bleibt kompatibel zu ActionArea
// ======================================================================================

using Mockup.Actions;
using Mockup.Designer;
using Mockup.ViewModel;
using SkiaSharp;
using System.Windows.Input;

namespace Mockup.Services;

internal sealed class PreviewInteractionService
{
    #region === STATE =====================================================================

    private DesignControl? _activeInteractiveControl;
    private DesignControl? _hoveredInteractiveControl;

    #endregion

    #region === PUBLIC API =================================================================

    public bool HandleMouseDown(BaseDesigner designer, SKPoint pt, MouseButtonEventArgs e)
    {
        var hitControl = HitTestPreviewInteractiveControl(designer, pt, out _);

        // Offene Dropdowns außerhalb schließen, getroffenen Host ggf. offen lassen
        CloseOpenPreviewDropDownsAndInvalidateIfNeeded(designer, hitControl);

        // ---------------------------------------------------------
        // Band-Logik nur im Screen-Preview
        // ---------------------------------------------------------
        if (designer.LiveMode && designer.DesignerKind == DesignerKind.Screen)
        {
            var hitBand = designer.HitTestBand(pt);

            if (hitBand != null)
            {
                bool checkExpand = hitBand.IsExpandable;

                if (checkExpand)
                {
                    bool toggleHit = hitBand.HitTestToggle(pt);
                    if (toggleHit)
                    {
                        hitBand.IsExpanded = !hitBand.IsExpanded;
                        designer.Screen?.RecalculateBandLayout();
                        designer.InvalidateDesigner();
                        return true;
                    }
                }
            }

            if (hitBand != null)
            {
                bool checkTabs = hitBand.ShowTabs;

                if (hitBand.IsExpandable)
                    checkTabs &= hitBand.IsExpanded;

                if (checkTabs)
                {
                    int tab = hitBand.HitTestTabIndex(pt);
                    if (tab >= 0)
                    {
                        hitBand.SetActivePage(tab);
                        designer.Screen?.RecalculateBandLayout();
                        designer.InvalidateDesigner();
                        return true;
                    }
                }
            }
        }

        if (hitControl != null)
        {
            _activeInteractiveControl = hitControl;

            var ctx = designer.CreatePointerContext(pt, e.ChangedButton, e.ClickCount);
            hitControl.OnPointerDown(in ctx);

            return true;
        }

        return true;
    }

    public bool HandleMouseMove(BaseDesigner designer, SKPoint pt, MouseEventArgs e)
    {
        bool showAA = Keyboard.IsKeyDown(Key.Space);

        if (designer.DataContext is MockupViewModel vm)
        {
            if (showAA)
            {
                var hit = HitTestPreviewInteractiveControl(designer, pt, out _);

                if (!ReferenceEquals(_hoveredInteractiveControl, hit))
                {
                    _hoveredInteractiveControl = hit;

                    if (hit != null && hit.SupportsPreviewHint)
                    {
                        var hintSource = hit.GetPreviewHintSource();

                        if (hintSource is ActionArea aa)
                            vm.ShowActionAreaHint(aa);
                        else
                            vm.HideActionAreaHint();
                    }
                    else
                    {
                        vm.HideActionAreaHint();
                    }
                }
            }
            else
            {
                if (_hoveredInteractiveControl != null || vm.IsActionAreaHintVisible)
                {
                    _hoveredInteractiveControl = null;
                    vm.HideActionAreaHint();
                }
            }
        }

        if (_activeInteractiveControl != null)
        {
            var ctx = designer.CreatePointerContext(pt, null, 0);
            _activeInteractiveControl.OnPointerMove(in ctx);
            e.Handled = true;
            return true;
        }

        return false;
    }

    public bool HandleMouseUp(BaseDesigner designer, SKPoint pt, MouseButtonEventArgs e)
    {
        var control = _activeInteractiveControl;

        _activeInteractiveControl = null;
        _hoveredInteractiveControl = null;

        if (designer.DataContext is MockupViewModel vm)
            vm.HideActionAreaHint();

        if (control != null)
        {
            var ctx = designer.CreatePointerContext(pt, e.ChangedButton, e.ClickCount);
            control.OnPointerUp(in ctx);
            return true;
        }

        return false;
    }

    public void ResetHover(MockupViewModel? vm = null)
    {
        _hoveredInteractiveControl = null;
        vm?.HideActionAreaHint();
    }

    public void ResetAll(MockupViewModel? vm = null)
    {
        _activeInteractiveControl = null;
        _hoveredInteractiveControl = null;
        vm?.HideActionAreaHint();
    }

    #endregion

    #region === HITTEST ====================================================================


    private static DesignControl? HitTestPreviewInteractiveControl(
        BaseDesigner designer,
        SKPoint worldPoint,
        out Band? parentBand
    )
    {
        parentBand = null;

        var allBands = designer.GetAllBands()?.Reverse();
        if (allBands == null)
            return null;

        foreach (var band in allBands)
        {
            if (band.IsExpandable && !band.IsExpanded)
                continue;

            var page = band.ActivePage;
            if (page == null)
                continue;

            foreach (var ctrl in page.Controls
                .Where(c => c.SupportsPreviewInteraction)
                .OrderByDescending(c => c.ZIndex))
            {
                bool hit;

                if (ctrl is ActionArea aa)
                {
                    hit = designer.LiveMode
                        ? aa.HitTestLive(worldPoint)
                        : aa.HitTestBounds(worldPoint);
                }
                else
                {
                    hit = ctrl.HitTest(worldPoint);
                }

                if (hit)
                {
                    parentBand = band;
                    return ctrl;
                }
            }
        }

        return null;
    }

    #endregion

    #region === DROPDOWN CLOSE =============================================================

    private static bool CloseOpenPreviewDropDownsExcept(
        BaseDesigner designer,
        DesignControl? keepOpenControl
    )
    {
        bool changed = false;

        var allBands = designer.GetAllBands();
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

    private static void CloseOpenPreviewDropDownsAndInvalidateIfNeeded(
        BaseDesigner designer,
        DesignControl? keepOpenControl
    )
    {
        if (CloseOpenPreviewDropDownsExcept(designer, keepOpenControl))
            designer.InvalidateDesigner();
    }

    #endregion
}
