using GongSolutions.Wpf.DragDrop;
using Mockup.Designer.DragDrop;
using Mockup.Registry;
using Mockup.Rendering;
using Mockup.Snapshots;
using SkiaSharp;
using System.Windows;
using System.Windows.Input;

namespace Mockup.Designer;

public partial class TemplateDesigner : BaseDesigner
{
    private DropContext? _pendingDropContext;
    private DropPreviewData? _dropPreview;

    private sealed class DropPreviewItem
    {
        public required DesignControl Control { get; init; }
        public required SKRect Rect { get; init; }
    }

    private sealed class DropPreviewData
    {
        public required SKRect GroupRect { get; init; }
        public required List<DropPreviewItem> Items { get; init; }
    }

    // ============================================================
    // DragOver
    // ============================================================

    public void OnDragOver(IDropInfo dropInfo)
    {
        if (dropInfo.Data is not ControlDescriptor descriptor)
        {
            ClearDropPreview();
            return;
        }

        var worldPos = new SKPoint((float)dropInfo.DropPosition.X, (float)dropInfo.DropPosition.Y);

        var band = HitTestBand(worldPos);
        if (band?.ActivePage == null)
        {
            ClearDropPreview();
            return;
        }

        dropInfo.Effects = DragDropEffects.Copy;
        dropInfo.DropTargetAdorner = null;

        _pendingDropContext = new DropContext
        {
            TargetBand = band,
            TargetPage = band.ActivePage,
            WorldPosition = worldPos,
        };

        BuildControlDropPreview(descriptor, _pendingDropContext);

        InvalidateDesigner();
    }

    // ============================================================
    // Drop
    // ============================================================

    public void OnDrop(IDropInfo dropInfo)
    {
        var ctx = _pendingDropContext;
        _pendingDropContext = null;
        _dropPreview = null;

        if (dropInfo.Data is not ControlDescriptor descriptor)
        {
            ClearAlignmentGuidelines();
            InvalidateDesigner();
            return;
        }

        if (ctx?.TargetBand?.ActivePage == null)
        {
            ClearAlignmentGuidelines();
            InvalidateDesigner();
            return;
        }

        DropControl(descriptor, ctx.TargetBand, ctx.TargetPage!, ctx.WorldPosition);

        ClearAlignmentGuidelines();

        FocusDesignerSurface();

        InvalidateDesigner();
    }

    // ============================================================
    // DESIGNER OVERLAY
    // ============================================================

    protected override void RenderDesignerOverlay(SKCanvas canvas, RenderContext ctx)
    {
        base.RenderDesignerOverlay(canvas, ctx);

        if (_dropPreview == null)
            return;

        using var fillPaint = new SKPaint
        {
            Color = SKColors.DodgerBlue.WithAlpha(18),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };

        using var borderPaint = new SKPaint
        {
            Color = SKColors.DodgerBlue,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f,
            PathEffect = SKPathEffect.CreateDash([6, 4], 0),
        };

        canvas.DrawRect(_dropPreview.GroupRect, fillPaint);

        foreach (var item in _dropPreview.Items)
            item.Control.Render(canvas, item.Rect, ctx);

        canvas.DrawRect(_dropPreview.GroupRect, borderPaint);
    }

    private void ClearDropPreview()
    {
        _dropPreview = null;
        _pendingDropContext = null;
        ClearAlignmentGuidelines();
        InvalidateDesigner();
    }

    // ============================================================
    // Drop implementations
    // ============================================================

    private void DropControl(
        ControlDescriptor descriptor,
        Band targetBand,
        BandPage targetPage,
        SKPoint worldPosition
    )
    {
        global::Mockup.MockupService.Mockup.PushSnapshot(SnapshotContext.Template, SnapshotLabels.ControlDropped);

        var ctrl = ControlFactory.Create(descriptor.TypeKey);

        // Parent setzen (WICHTIG)
        ctrl.ParentBand = targetBand;
        ctrl.ParentBandPage = targetPage;

        // World → Page-lokal (einzige Wahrheit)
        var local = WorldToPageLocal(worldPosition, targetBand);

        ctrl.X = MathF.Round(local.X);
        ctrl.Y = MathF.Round(local.Y);

        TryApplyAlignmentGuidelineSnapToToolboxControlDrop(ctrl);

        ctrl.ZIndex = GetNextTopZ(targetPage);

        targetPage.Controls.Add(ctrl);

        SelectDroppedControl(ctrl);
    }

    // ============================================================
    // Preview builders
    // ============================================================

    private void BuildControlDropPreview(ControlDescriptor descriptor, DropContext ctx)
    {
        if (ctx.TargetBand == null || ctx.TargetPage == null)
        {
            _dropPreview = null;
            return;
        }

        var previewCtrl = ControlFactory.Create(descriptor.TypeKey);

        previewCtrl.ParentBand = ctx.TargetBand;
        previewCtrl.ParentBandPage = ctx.TargetPage;

        var local = WorldToPageLocal(ctx.WorldPosition, ctx.TargetBand);

        previewCtrl.X = MathF.Round(local.X);
        previewCtrl.Y = MathF.Round(local.Y);

        UpdateAlignmentGuidelinesDuringToolboxControlDrop(previewCtrl);

        float left = ctx.TargetPage.WorldBounds.Left + previewCtrl.X;
        float top = ctx.TargetPage.WorldBounds.Top + previewCtrl.Y;

        var rect = new SKRect(left, top, left + previewCtrl.Width, top + previewCtrl.Height);

        _dropPreview = new DropPreviewData
        {
            GroupRect = rect,
            Items =
            [
                new DropPreviewItem
                {
                    Control = previewCtrl,
                    Rect = rect,
                }
            ],
        };
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static int GetNextTopZ(BandPage page)
    {
        return page.Controls.Count == 0
            ? 0
            : page.Controls.Max(c => c.ZIndex) + 1;
    }

}
