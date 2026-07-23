using SkiaSharp;

namespace Mockup.Designer.DragDrop;

internal sealed class DropContext
{
    public object Data { get; init; } = default!;
    public required Band TargetBand { get; init; }
    public required BandPage TargetPage { get; init; }
    public required SKPoint WorldPosition { get; init; }
}

