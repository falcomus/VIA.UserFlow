using GongSolutions.Wpf.DragDrop;
using Mockup.Designer.DragDrop;
using Mockup.Messages;
using Mockup.Registry;
using Mockup.Rendering;
using Mockup.Snapshots;
using SkiaSharp;
using System.Windows;
using System.Windows.Input;

namespace Mockup.Designer;

public partial class ScreenDesigner : BaseDesigner
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
        public bool WillGrowBand { get; init; }
    }

    // ============================================================
    // DragOver
    // ============================================================

    public void OnDragOver(IDropInfo dropInfo)
    {
        if (dropInfo.Data == null)
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

        switch (dropInfo.Data)
        {
            case ControlDescriptor descriptor:
                BuildControlDropPreview(descriptor, _pendingDropContext);
                break;

            case ScreenTemplate template:
                BuildTemplateDropPreview(template, _pendingDropContext);
                break;

            default:
                ClearDropPreview();
                break;
        }

        InvalidateDesigner();
        MSG.UI.InvalidateDesigner();
    }

    // ============================================================
    // Drop
    // ============================================================

    public void OnDrop(IDropInfo dropInfo)
    {
        var ctx = _pendingDropContext;
        _pendingDropContext = null;
        _dropPreview = null;

        if (ctx == null || ctx.TargetPage == null)
        {
            ClearAlignmentGuidelines();
            InvalidateDesigner();
            MSG.UI.InvalidateDesigner();
            return;
        }

        switch (dropInfo.Data)
        {
            case ControlDescriptor descriptor:
                DropControl(descriptor, ctx);
                break;

            case ScreenTemplate template:
                DropTemplate(template, ctx);
                break;
        }

        ClearAlignmentGuidelines();

        FocusDesignerSurface();

        InvalidateDesigner();

        MSG.UI.InvalidateDesigner();
    }

    // ============================================================
    // OVERLAY
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

        if (_dropPreview.WillGrowBand)
        {
            var labelRect = new SKRect(
                _dropPreview.GroupRect.Left,
                Math.Max(0, _dropPreview.GroupRect.Top - 18),
                _dropPreview.GroupRect.Left + 88,
                Math.Max(0, _dropPreview.GroupRect.Top - 2)
            );

            using var labelFill = new SKPaint
            {
                Color = SKColors.Orange.WithAlpha(230),
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
            };

            canvas.DrawRoundRect(labelRect, 3, 3, labelFill);

            TextRenderer.Draw(
                canvas,
                "Band grows",
                labelRect,
                10,
                SKColors.Black,
                padding: 0f
            );
        }
    }

    private void ClearDropPreview()
    {
        _dropPreview = null;
        _pendingDropContext = null;
        ClearAlignmentGuidelines();
        InvalidateDesigner();
        MSG.UI.InvalidateDesigner();
    }

    // ============================================================
    // Drop implementations
    // ============================================================

    private void DropControl(ControlDescriptor descriptor, DropContext ctx)
    {
        MockupService.Mockup.PushSnapshot(SnapshotContext.Screen, SnapshotLabels.ControlDropped);

        var ctrl = ControlFactory.Create(descriptor.TypeKey);

        ctrl.ParentBandPage = ctx.TargetPage;
        ctrl.ParentBand = ctx.TargetBand;

        var local = WorldToPageLocal(ctx.WorldPosition, ctx.TargetBand);

        ctrl.X = MathF.Round(local.X);
        ctrl.Y = MathF.Round(local.Y);

        TryApplyAlignmentGuidelineSnapToToolboxControlDrop(ctrl);

        ctrl.ZIndex = GetNextTopZ(ctx.TargetPage);

        ctx.TargetPage.Controls.Add(ctrl);

        SelectDroppedControl(ctrl);
    }

    private void DropTemplate(ScreenTemplate template, DropContext ctx)
    {
        if (template == null)
            return;

        MockupService.Mockup.PushSnapshot(SnapshotContext.Screen, SnapshotLabels.ControlDropped);

        var sourceControls = template.Controls?
            .Where(c => c != null)
            .OrderBy(c => c.ZIndex)
            .ToList();

        if (sourceControls == null || sourceControls.Count == 0)
            return;

        var targetBand = ctx.TargetBand;
        var targetPage = ctx.TargetPage;

        if (targetBand == null || targetPage == null)
            return;

        GetTemplatePlacement(
            template,
            ctx,
            out var targetX,
            out var targetY,
            out var groupHeight,
            out _
        );

        float minX = sourceControls.Min(c => c.X);
        float minY = sourceControls.Min(c => c.Y);

        int nextZ = GetNextTopZ(targetPage);
        var created = new List<DesignControl>();

        foreach (var src in sourceControls)
        {
            var copy = src.DeepClone();

            float relX = src.X - minX;
            float relY = src.Y - minY;

            copy.ParentBand = targetBand;
            copy.ParentBandPage = targetPage;

            copy.X = MathF.Round(targetX + relX);
            copy.Y = MathF.Round(targetY + relY);
            copy.ZIndex = nextZ++;

            targetPage.Controls.Add(copy);
            created.Add(copy);
        }

        float requiredBottom = targetY + groupHeight + 10f;

        if (requiredBottom > targetPage.Height)
        {
            float newHeight = MathF.Round(requiredBottom);
            targetPage.Height = newHeight;
            targetPage.InvalidateMinHeight();

            if (targetBand.ActivePage == targetPage)
                targetBand.Height = newHeight;

            if (Screen != null)
                Screen.RecalculateBandLayout();
        }

        NormalizePageZOrder(targetPage);
        SelectDroppedControls(targetBand, created);
    }

    // ============================================================
    // PREVIEW BUILDERS
    // ============================================================

    private void BuildControlDropPreview(ControlDescriptor descriptor, DropContext ctx)
    {
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
            WillGrowBand = false,
        };
    }

    private void BuildTemplateDropPreview(ScreenTemplate template, DropContext ctx)
    {
        ClearAlignmentGuidelines();

        if (template == null || ctx.TargetBand == null || ctx.TargetPage == null)
        {
            _dropPreview = null;
            return;
        }

        var controls = template.Controls?
            .Where(c => c != null)
            .OrderBy(c => c.ZIndex)
            .ToList();

        if (controls == null || controls.Count == 0)
        {
            _dropPreview = null;
            return;
        }

        GetTemplatePlacement(
            template,
            ctx,
            out var targetX,
            out var targetY,
            out var groupHeight,
            out var groupRectLocal
        );

        float minX = controls.Min(c => c.X);
        float minY = controls.Min(c => c.Y);

        var items = new List<DropPreviewItem>();

        foreach (var src in controls)
        {
            var previewCtrl = src.DeepClone();
            previewCtrl.ParentBand = ctx.TargetBand;
            previewCtrl.ParentBandPage = ctx.TargetPage;

            float relX = src.X - minX;
            float relY = src.Y - minY;

            float worldX = ctx.TargetPage.WorldBounds.Left + targetX + relX;
            float worldY = ctx.TargetPage.WorldBounds.Top + targetY + relY;

            var rect = new SKRect(worldX, worldY, worldX + src.Width, worldY + src.Height);

            items.Add(new DropPreviewItem
            {
                Control = previewCtrl,
                Rect = rect,
            });
        }

        var groupRect = new SKRect(
            ctx.TargetPage.WorldBounds.Left + groupRectLocal.Left,
            ctx.TargetPage.WorldBounds.Top + groupRectLocal.Top,
            ctx.TargetPage.WorldBounds.Left + groupRectLocal.Right,
            ctx.TargetPage.WorldBounds.Top + groupRectLocal.Bottom
        );

        float requiredBottom = targetY + groupHeight + 10f;
        bool willGrow = requiredBottom > ctx.TargetPage.Height;

        _dropPreview = new DropPreviewData
        {
            GroupRect = groupRect,
            Items = items,
            WillGrowBand = willGrow,
        };
    }

    private void GetTemplatePlacement(
        ScreenTemplate template,
        DropContext ctx,
        out float targetX,
        out float targetY,
        out float groupHeight,
        out SKRect groupRectLocal
    )
    {
        var sourceControls = template.Controls
            .Where(c => c != null)
            .OrderBy(c => c.ZIndex)
            .ToList();

        float minX = sourceControls.Min(c => c.X);
        float minY = sourceControls.Min(c => c.Y);
        float maxRight = sourceControls.Max(c => c.X + c.Width);
        float maxBottom = sourceControls.Max(c => c.Y + c.Height);

        float groupWidth = MathF.Max(0f, maxRight - minX);
        groupHeight = MathF.Max(0f, maxBottom - minY);

        var localDrop = WorldToPageLocal(ctx.WorldPosition, ctx.TargetBand);

        targetX = MathF.Round(localDrop.X);
        targetY = MathF.Round(localDrop.Y);

        float availableWidth = MathF.Max(0f, ctx.TargetBand.ContentRect.Width);

        if (targetX < 0)
            targetX = 0;

        if (groupWidth <= availableWidth)
        {
            float maxStartX = MathF.Max(0f, availableWidth - groupWidth);
            if (targetX > maxStartX)
                targetX = maxStartX;
        }
        else
        {
            targetX = 0f;
        }

        if (targetY < 0)
            targetY = 0;

        groupRectLocal = new SKRect(
            targetX,
            targetY,
            targetX + groupWidth,
            targetY + groupHeight
        );
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private static int GetNextTopZ(BandPage page)
    {
        return page.Controls.Count == 0
            ? 0
            : page.Controls.Max(c => c.ZIndex) + 1;
    }

    private static void NormalizePageZOrder(BandPage page)
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

    private void SelectDroppedControls(Band targetBand, IReadOnlyList<DesignControl> controls)
    {
        if (controls == null || controls.Count == 0)
            return;

        DeselectAllControls();

        foreach (var ctrl in controls)
            SelectControl(ctrl);

        SelectedBand = targetBand;
        InvalidateDesigner();
    }

}
