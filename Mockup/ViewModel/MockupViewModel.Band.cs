// ======================================================================================
// FILE: Mockup.ViewModel/MockupViewModel.Messaging.cs
// MO44 – Persistenz: Designer -> ViewModel (MoveBand)
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.Messages;
using Mockup.Snapshots;

namespace Mockup.ViewModel;

public partial class MockupViewModel : ObservableObject
{
    private void HandleMoveBand(MoveBandMessage msg)
    {
        if (msg.Band == null)
            return;

        if (TryMoveBandInCurrentScreen(msg.Band, msg.Delta))
            return;

        if (TryMoveBandInCurrentTemplate(msg.Band, msg.Delta))
            return;

        _ = TryMoveBandInCurrentPopup(msg.Band, msg.Delta);
    }


    private bool TryMoveBandInCurrentScreen(Band band, int delta)
    {
        var screen = CurrentScreen;
        if (screen == null)
            return false;

        if (screen.Bands == null)
            return false;

        if (band.BandType != BandType.Custom)
            return false;

        if (!CanReorderCustomBands(screen.Bands, band, delta))
            return false;

        PushSnapshot(SnapshotContext.Screen, SnapshotLabels.BandMoved);

        if (!ReorderCustomBands(screen.Bands, band, delta))
            return false;

        screen.RecalculateBandLayout();
        SaveCurrentProject();

        MSG.UI.InvalidateDesigner();

        return true;
    }

    private bool TryMoveBandInCurrentTemplate(Band band, int delta)
    {
        var template = CurrentTemplate;
        if (template == null)
            return false;

        if (template.Bands == null)
            return false;

        if (band.BandType != BandType.Custom)
            return false;

        if (!CanReorderCustomBands(template.Bands, band, delta))
            return false;

        PushSnapshot(SnapshotContext.Template, SnapshotLabels.BandMoved);

        if (!ReorderCustomBands(template.Bands, band, delta))
            return false;

        SaveTemplates();

        MSG.UI.InvalidateDesigner();

        return true;
    }

    private bool TryMoveBandInCurrentPopup(Band band, int delta)
    {
        var popup = CurrentPopup;
        if (popup == null)
            return false;

        if (CurrentProject == null)
            return false;

        if (popup.Bands == null)
            return false;

        if (band.BandType != BandType.Custom)
            return false;

        if (!CanReorderCustomBands(popup.Bands, band, delta))
            return false;

        PushSnapshot(SnapshotContext.Popup, SnapshotLabels.BandMoved);

        if (!ReorderCustomBands(popup.Bands, band, delta))
            return false;

        SaveCurrentProject();

        MSG.UI.InvalidateDesigner();

        return true;
    }

    private static bool CanReorderCustomBands(IList<Band> bands, Band band, int delta)
    {
        if (bands is null)
            return false;

        var customs = bands.Where(b => b.BandType == BandType.Custom).ToList();
        if (customs.Count <= 1)
            return false;

        int idx = customs.IndexOf(band);
        if (idx < 0)
            return false;

        int target = idx + delta;
        return target >= 0 && target < customs.Count;
    }

    private static bool ReorderCustomBands(IList<Band> bands, Band band, int delta)
    {
        if (bands is null)
            return false;

        if (bands.IsReadOnly)
            return false;

        var headers = bands.Where(b => b.BandType == BandType.Header).ToList();
        var customs = bands.Where(b => b.BandType == BandType.Custom).ToList();
        var footers = bands.Where(b => b.BandType == BandType.Footer).ToList();

        if (customs.Count <= 1)
            return false;

        int idx = customs.IndexOf(band);
        if (idx < 0)
            return false;

        int target = idx + delta;
        if (target < 0 || target >= customs.Count)
            return false;

        customs.RemoveAt(idx);
        customs.Insert(target, band);

        bands.Clear();
        foreach (var h in headers) bands.Add(h);
        foreach (var c in customs) bands.Add(c);
        foreach (var f in footers) bands.Add(f);

        return true;
    }
}
