// ======================================================================================
// FILE: Mockup.Services/MockupClipboardService.cs
// ======================================================================================

namespace Mockup.Services;

public sealed class MockupClipboardService
{
    private readonly List<DesignControl> _controls = new();

    public bool HasControls => _controls.Count > 0;

    public IReadOnlyList<DesignControl> Controls => _controls;

    public void Clear()
    {
        _controls.Clear();
    }

    public void SetControls(IEnumerable<DesignControl> controls)
    {
        _controls.Clear();

        if (controls == null)
            return;

        foreach (var ctrl in controls)
        {
            if (ctrl == null)
                continue;

            var copy = ctrl.DeepClone();
            copy.ParentBand = ctrl.ParentBand;
            copy.ParentBandPage = ctrl.ParentBandPage;

            _controls.Add(copy);
        }
    }

    public List<DesignControl> CreateControlCopies()
    {
        var result = new List<DesignControl>();

        foreach (var ctrl in _controls)
        {
            if (ctrl == null)
                continue;

            var copy = ctrl.DeepClone();
            copy.ParentBand = ctrl.ParentBand;
            copy.ParentBandPage = ctrl.ParentBandPage;

            result.Add(copy);
        }

        return result;
    }
}
