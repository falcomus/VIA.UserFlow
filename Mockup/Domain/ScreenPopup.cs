// ======================================================================================
// FILE: Mockup/ScreenPopup.cs
//
// ZWECK (CLEAN, FINAL):
// - Eigenständiges Popup-Modell (KEIN Screen, KEINE Inheritance)
// - Frei skalierbar (Designzeit)
// - Genau EIN Band (Custom)
// - Genau EINE Page
// - Optionaler Header mit eigener Höhe
// - Bands sind Single-Source-of-Truth für HitTest / Drag / Bounds
//
// WICHTIG:
// - Width / Height = äußere Gesamtgröße des Popups
// - HeaderHeight = Höhe der Titelzeile
// - Controls liegen im Content-Bereich
// - Page.Height = ContentHeight
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ViewModel;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Mockup;

public enum ScreenPopupPosition
{
    Left,
    Right,
    Top,
    Bottom,
    Center,
    MousePos,
}

public sealed partial class ScreenPopup : ObservableObject
{
    #region === Identity ===============================================================

    [ObservableProperty]
    private long id = IdGenerator.NewID;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string? description;

    [ObservableProperty]
    private string groupName = MockupViewModel.DEFAULT_POPUP_GROUPNAME;

    #endregion

    #region === Size / Position ========================================================

    /// <summary>
    /// Äußere Gesamtbreite des Popups.
    /// </summary>
    [ObservableProperty]
    private float width = 390;

    /// <summary>
    /// Äußere Gesamthöhe des Popups.
    /// </summary>
    [ObservableProperty]
    private float height = 640;

    /// <summary>
    /// Zielposition im Preview/Runtime-Kontext.
    /// </summary>
    [ObservableProperty]
    public ScreenPopupPosition position = ScreenPopupPosition.Center;

    /// <summary>
    /// Header ein-/ausblenden.
    /// </summary>
    [ObservableProperty]
    private bool hasHeader = true;

    /// <summary>
    /// Höhe des Popup-Headers.
    /// </summary>
    [ObservableProperty]
    private float headerHeight = 34;

    #endregion

    #region === Structure ===============================================================

    /// <summary>
    /// Popups besitzen IMMER genau EIN Band (Custom).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Band> bands = new();

    [JsonIgnore]
    public Band RootBand =>
        Bands.Count > 0
            ? Bands[0]
            : throw new InvalidOperationException("ScreenPopup has no RootBand.");

    [JsonIgnore]
    public BandPage Page =>
        RootBand.ActivePage
        ?? throw new InvalidOperationException("ScreenPopup RootBand has no ActivePage.");

    [JsonIgnore]
    public ObservableCollection<DesignControl> Controls => Page.Controls;

    /// <summary>
    /// Oberkante des Content-Bereichs innerhalb des Popups.
    /// </summary>
    [JsonIgnore]
    public float ContentTop => HasHeader ? MathF.Max(0, HeaderHeight) : 0f;

    /// <summary>
    /// Nutzbare Content-Höhe für Controls innerhalb des Popups.
    /// </summary>
    [JsonIgnore]
    public float ContentHeight
    {
        get
        {
            if (!HasHeader)
                return MathF.Max(0, Height);

            return MathF.Max(0, Height - HeaderHeight);
        }
    }

    #endregion

    #region === Construction ============================================================

    /// <summary>
    /// Ctor für JSON
    /// </summary>
    public ScreenPopup()
    {
        InitializeStructure();
    }

    /// <summary>
    /// Runtime-Ctor
    /// </summary>
    public ScreenPopup(long id, string name, float width, float height)
    {
        Id = id;
        Name = name;
        Width = width;
        Height = height;

        InitializeStructure();
    }

    private void InitializeStructure()
    {
        Bands.Clear();

        var band = new Band
        {
            BandType = BandType.Custom,
            Title = string.Empty,
            Width = Width,
            Height = Height,
            IsExpandable = false,
            IsExpanded = false,
            UniformPageHeight = true,
        };

        var page = new BandPage
        {
            Id = IdGenerator.NewID,
            Name = "PopupPage",
            Title = "PopupPage",
            Height = ContentHeight,
        };

        band.Pages.Add(page);
        band.ActivePageIndex = 0;

        Bands.Add(band);
    }

    #endregion

    #region === Sync on Resize / Header Change ==========================================

    partial void OnWidthChanged(float value)
    {
        if (Bands.Count == 0)
            return;

        float w = MathF.Round(value);
        RootBand.Width = w;
    }

    partial void OnHeightChanged(float value)
    {
        SyncPopupStructureSize();
    }

    partial void OnHasHeaderChanged(bool value)
    {
        SyncPopupStructureSize();
    }

    partial void OnHeaderHeightChanged(float value)
    {
        if (HeaderHeight < 0)
            HeaderHeight = 0;

        SyncPopupStructureSize();
    }

    private void SyncPopupStructureSize()
    {
        if (Bands.Count == 0)
            return;

        float outerHeight = MathF.Round(Height);
        float contentHeight = MathF.Round(ContentHeight);

        RootBand.Height = outerHeight;
        RootBand.ActivePage.Height = contentHeight;
        Page.Height = contentHeight;
    }

    #endregion

    #region === Clone ===================================================================

    public ScreenPopup DeepClone()
    {
        var clone = new ScreenPopup
        {
            Id = IdGenerator.NewID,
            Name = Name,
            Title = Title,
            Position = Position,
            Description = Description,
            GroupName = GroupName,
            Width = Width,
            Height = Height,
            HasHeader = HasHeader,
            HeaderHeight = HeaderHeight,
        };

        clone.Bands.Clear();

        foreach (var band in Bands)
        {
            var clonedBand = band.DeepClone();

            clonedBand.ParentBand = band;

            clone.Bands.Add(clonedBand);
        }

        return clone;
    }

    #endregion
}
