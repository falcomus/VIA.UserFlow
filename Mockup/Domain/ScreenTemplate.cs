// ======================================================================================
// FILE: Mockup/ScreenTemplate.cs
//
// ZWECK (CLEAN, FINAL):
// - Eigenständiges Template-Modell (KEIN Screen, KEINE Inheritance)
// - Frei skalierbar (Designzeit)
// - Genau EIN Band (Custom)
// - Genau EINE Page
// - Kein Header, kein Footer, kein Fill, kein Scroll
// - Bands sind Single-Source-of-Truth für HitTest / Drag / Bounds
//
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ViewModel;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Mockup;

public sealed partial class ScreenTemplate : ObservableObject
{
    #region === Identity ===============================================================

    [ObservableProperty]
    private long id = IdGenerator.NewID;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string? description;

    [ObservableProperty]
    private string groupName = MockupViewModel.DEFAULT_TEMPLATE_GROUPNAME;

    partial void OnGroupNameChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            GroupName = MockupViewModel.DEFAULT_TEMPLATE_GROUPNAME;
    }

    #endregion


    #region === Size (Design-Time, frei) ================================================

    [ObservableProperty]
    private float width = 390;

    [ObservableProperty]
    private float height = 300;

    #endregion


    #region === Structure ===============================================================

    /// <summary>
    /// Templates besitzen IMMER genau EIN Band (Custom).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Band> bands = new();

    [JsonIgnore]
    public Band RootBand =>
        Bands.Count > 0
            ? Bands[0]
            : throw new InvalidOperationException("ScreenTemplate has no RootBand.");

    [JsonIgnore]
    public BandPage Page =>
        RootBand.ActivePage
        ?? throw new InvalidOperationException("ScreenTemplate RootBand has no ActivePage.");

    [JsonIgnore]
    public ObservableCollection<DesignControl> Controls => Page.Controls;

    #endregion


    #region === Construction ============================================================

    /// <summary>
    /// Ctor für JSON
    /// </summary>
    public ScreenTemplate()
    {
        InitializeStructure();
    }

    /// <summary>
    /// Runtime-Ctor
    /// </summary>
    public ScreenTemplate(long id, string name, float width, float height)
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
            Name = "TemplatePage",
            Title = "Template",
            Height = Height,
        };

        band.Pages.Add(page);
        band.ActivePageIndex = 0;

        Bands.Add(band);
    }

    #endregion


    #region === Sync on Resize ===========================================================

    partial void OnWidthChanged(float value)
    {
        if (Bands.Count == 0)
            return;

        float w = MathF.Round(value);

        RootBand.Width = w;
    }

    partial void OnHeightChanged(float value)
    {
        if (Bands.Count == 0)
            return;

        float h = MathF.Round(value);

        RootBand.Height = h;
        RootBand.ActivePage.Height = h;
        Page.Height = h;
    }

    #endregion


    #region === Clone ===================================================================

    public ScreenTemplate DeepClone()
    {
        //Name = Name + " – Copy",
        var clone = new ScreenTemplate
        {
            Id = IdGenerator.NewID,
            Name = Name,
            Description = Description,
            GroupName = GroupName,
            Width = Width,
            Height = Height,
        };

        clone.Bands.Clear();

        foreach (var band in Bands)
        {
            var clonedBand = band.DeepClone();
            clonedBand.ParentBand = clonedBand;
            clonedBand.ParentScreen = clonedBand.ParentScreen;
            clone.Bands.Add(clonedBand);
        }

        return clone;
    }

    #endregion
}
