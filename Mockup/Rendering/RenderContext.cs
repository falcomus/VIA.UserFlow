// FILE: Mockup.Rendering/RenderContext.cs

using SkiaSharp;


namespace Mockup.Rendering;

public class RenderContext
{
    public bool LiveMode { get; set; }
    public DesignerMouseMode MouseMode { get; set; }

    public Screen? SelectedScreen { get; set; }
    public Band? SelectedBand { get; set; }
    public BandPage? SelectedPage { get; set; }

    public IReadOnlyList<DesignControl>? SelectedControls { get; set; }

    public SKRect PageWorldBounds { get; set; }

    public bool CanMoveBands { get; set; }

    public static readonly RenderContext Default = new();

    public bool ShowActionAreas { get; set; }

    public bool ShowBandBorders { get; set; } = true;
}
