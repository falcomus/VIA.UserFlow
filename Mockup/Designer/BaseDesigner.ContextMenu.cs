// ======================================================================================
// FILE: Mockup.Designer/BaseDesigner.ContextMenu.cs
// ======================================================================================

using Mockup.UIControls;
using Mockup.ViewModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Mockup.Designer;

public partial class BaseDesigner
{
    private void ShowDesignerContextMenu(MouseButtonEventArgs e)
    {
        if (DataContext is not MockupViewModel vm)
            return;

        if (LiveMode && DesignerKind == DesignerKind.Screen)
            return;

        var pt = MouseWorldPoint;

        // 1) Control unter Maus?
        var hitCtrl = HitTestControl(pt, out var ctrlBand);
        if (hitCtrl != null)
        {
            if (!hitCtrl.IsSelected)
            {
                DeselectAllControls();
                SelectControl(hitCtrl);
                SelectedBand = ctrlBand;
                InvalidateDesigner();
            }

            vm.SetContextControls(ctrlBand, VM.SelectedControls);
            OpenContextMenu(vm, e);
            return;
        }

        // 2) Band unter Maus?
        var hitBand = HitTestBand(pt);
        if (hitBand != null)
        {
            SelectedBand = hitBand;
            vm.SetContextBand(hitBand);
            OpenContextMenu(vm, e);
            return;
        }

        // 3) Leerfläche => Root-Kontext je nach Designer
        SelectedBand = null;

        switch (DesignerKind)
        {
            case DesignerKind.Template:
                vm.SetContextTemplate(vm.CurrentTemplate);
                break;

            case DesignerKind.Popup:
                vm.SetContextPopup(vm.CurrentPopup);
                break;

            default:
                vm.SetContextScreen(Screen);
                break;
        }

        OpenContextMenu(vm, e);
    }

    private void OpenContextMenu(object vm, MouseButtonEventArgs e)
    {
        e.Handled = true;

        var placementTarget = (UIElement?)PART_Canvas ?? this;
        var p = e.GetPosition(placementTarget);

        if (vm is MockupViewModel mvm)
            mvm.ContextMenuWorldPoint = new Point(MouseWorldPoint.X, MouseWorldPoint.Y);

        var menu = new DesignerContextMenu
        {
            PlacementTarget = placementTarget,
            Placement = PlacementMode.RelativePoint,
            HorizontalOffset = p.X,
            VerticalOffset = p.Y,
            DataContext = vm,
        };

        menu.IsOpen = true;
    }
}
