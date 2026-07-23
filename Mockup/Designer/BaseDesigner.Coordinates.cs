// ======================================================================================
// FILE: Mockup.Designer/BaseDesigner.Coordinates.cs
// ======================================================================================

using SkiaSharp;

namespace Mockup.Designer;

public partial class BaseDesigner
{
    protected static SKPoint WorldToPageLocal(SKPoint world, Band band)
    {
        var page = band.ActivePage
            ?? throw new InvalidOperationException(
                "WorldToPageLocal called on Band without ActivePage.");

        return new SKPoint(
            world.X - page.WorldBounds.Left,
            world.Y - page.WorldBounds.Top);
    }
}
