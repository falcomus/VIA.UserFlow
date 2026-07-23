// ======================================================================================
// FILE: Mockup.Designer/BaseDesigner.Bands.cs
// MO44 – Band Accessors & Shared Helpers
// ======================================================================================

namespace Mockup.Designer;

public abstract partial class BaseDesigner
{
    #region === Band Collections ===

    protected IEnumerable<Band> CustomBands =>
        GetAllBands().Where(b => b.BandType == BandType.Custom);

    protected Band? HeaderBand => GetHeaderBand();

    protected Band? FooterBand => GetFooterBand();

    #endregion

    #region === LiveMode ===

    public bool LiveMode { get; set; }

    #endregion

    #region === Layout Helper ===

    protected void UpdateActivePageWorldBounds(Band band)
    {
        var page = band.ActivePage;
        if (page == null)
            return;

        // Zentrale Regel:
        // Die ActivePage belegt immer exakt den Content-Bereich des Bands.
        // Keine eigene Header-/Tabs-/Toggle-Sonderlogik mehr hier.
        page.WorldBounds = band.GetContentRect();
    }

    #endregion
}
