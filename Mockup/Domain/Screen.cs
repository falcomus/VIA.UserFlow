// ======================================================================================
// FILE: Mockup/Screen.cs
//
// ZWECK:
// Repräsentiert einen Screen basierend auf einer festen Device-Breite
// und einer persistierten Screen-Höhe mit Mindesthöhe = DeviceHeight.
//
// KONZEPT (VEREINFACHT):
// - Das letzte Custom-Band ist ein Auto-Fill-Band.
// - Header und Footer bleiben eigenständige Bands.
// - ScreenHeight = max(DeviceHeight, UserHeight, Mindesthöhe aus festen Bands + Auto-Fill-Minimum).
// - Beim Vergrößern eines nicht-Auto-Fill-Bands wächst der Screen.
// - Beim Verkleinern schrumpft der Screen bis DeviceHeight, danach wächst das Auto-Fill-Band.
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.Messages;
using Mockup.ViewModel;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace Mockup;

public partial class Screen : ObservableObject
{
    #region === Identity ===

    public static readonly Color DefaultBackground = Colors.White;
    public static readonly float DefaultHeaderHeight = 35f;
    public static readonly float DefaulFooterHeight = 35f;
    public static readonly float DefaultBandHeight = 150f;
    public static readonly float DefaultBandHeaderHeight = 35f;


    [ObservableProperty]
    private long id;

    [ObservableProperty]
    private string name = "";

    [JsonIgnore]
    [ObservableProperty]
    private Project? project;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GroupSortKey))]
    private string groupName = MockupViewModel.DEFAULT_SCREEN_GROUPNAME;

    public string GroupSortKey =>
        string.Equals(
            GroupName,
            MockupViewModel.DEFAULT_SCREEN_GROUPNAME,
            StringComparison.OrdinalIgnoreCase
        )
            ? "0_HOME"
            : "1_" + (GroupName ?? "");

    [ObservableProperty]
    private string descr = string.Empty;

    [ObservableProperty]
    private Color background = DefaultBackground;

    [ObservableProperty]
    private bool isHomeScreen;

    partial void OnIsHomeScreenChanged(bool value)
    {
        if (value)
            MockupService.Mockup.HomeScreen = this;
    }

    #endregion

    #region === Device Size ===

    [JsonIgnore]
    public float Width => Project?.DeviceWidth ?? 0f;

    [JsonIgnore]
    public float DeviceHeight => Project?.DeviceHeight ?? 0f;

    /// <summary>
    /// Persistierte Screen-Höhe. Darf niemals kleiner als DeviceHeight werden.
    /// Das letzte Custom-Band füllt den verbleibenden Raum innerhalb dieser Höhe.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScreenHeight))]
    private float userHeight;

    [JsonIgnore]
    private bool _isRecalculatingLayout;

    partial void OnUserHeightChanged(float value)
    {
        if (_isRecalculatingLayout)
            return;

        float min = DeviceHeight;
        if (value < min)
        {
            try
            {
                _isRecalculatingLayout = true;
                UserHeight = min;
            }
            finally
            {
                _isRecalculatingLayout = false;
            }

            return;
        }

        if (value <= 0)
            return;

        RecalculateBandLayout();
    }

    [JsonIgnore]
    public float ScreenHeight => Math.Max(Math.Max(DeviceHeight, UserHeight), GetMinimumScreenHeightForLayout());

    #endregion

    #region === Background Image ===

    [ObservableProperty]
    private string? backgroundImageFilename;

    [ObservableProperty]
    private string? backgroundImageBase64;

    partial void OnBackgroundImageBase64Changed(string? value)
    {
        LoadBackgroundImage();
    }

    [ObservableProperty]
    [JsonIgnore]
    private SKBitmap? _backgroundImage;

    partial void OnBackgroundImageChanged(SKBitmap? oldValue, SKBitmap? newValue)
    {
        MSG.UI.InvalidateDesigner();
    }

    public void SetBackgroundImageFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        try
        {
            var bytes = File.ReadAllBytes(filePath);
            BackgroundImageBase64 = Convert.ToBase64String(bytes);

            MSG.UI.InvalidateDesigner();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to set background image for screen {Name}", Name);
        }
    }

    internal void LoadBackgroundImage()
    {
        if (string.IsNullOrWhiteSpace(BackgroundImageBase64))
        {
            BackgroundImage = null;
            return;
        }

        SKBitmap? decoded = null;

        try
        {
            var bytes = Convert.FromBase64String(BackgroundImageBase64);
            using var ms = new MemoryStream(bytes);

            decoded = SKBitmap.Decode(ms);
            BackgroundImage = decoded;
            decoded = null;
        }
        catch
        {
            decoded?.Dispose();
            BackgroundImage = null;
        }
    }

    public void ResetBackgroundImage()
    {
        BackgroundImageFilename = null;
        BackgroundImageBase64 = null;
        BackgroundImage = null;

        MSG.UI.InvalidateDesigner();
    }

    #endregion

    #region === Bands ===

    [ObservableProperty]
    private ObservableCollection<Band> bands = [];

    partial void OnBandsChanged(
        ObservableCollection<Band>? oldValue,
        ObservableCollection<Band> newValue
    )
    {
        if (oldValue != null)
            oldValue.CollectionChanged -= OnBandsCollectionChanged;

        if (newValue != null)
        {
            newValue.CollectionChanged += OnBandsCollectionChanged;

            foreach (var band in newValue)
                WireBand(band);
        }

        RecalculateBandLayout();
    }

    private void OnBandsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (Band band in e.NewItems)
                WireBand(band);
        }

        RecalculateBandLayout();
    }

    [JsonIgnore]
    public Band? HeaderBand => Bands.FirstOrDefault(b => b.BandType == BandType.Header);

    [JsonIgnore]
    public Band? FooterBand => Bands.FirstOrDefault(b => b.BandType == BandType.Footer);

    [JsonIgnore]
    public IEnumerable<Band> CustomBands => Bands.Where(b => b.BandType == BandType.Custom);

    [JsonIgnore]
    public IEnumerable<DesignControl> AllControls =>
        Bands.SelectMany(b => b.Pages ?? []).SelectMany(p => p.Controls);

    #endregion

    #region === Header Flags ===

    [ObservableProperty]
    private bool showHeader = false;

    partial void OnShowHeaderChanged(bool value)
    {
        RecalculateBandLayout();
    }

    [ObservableProperty]
    private bool showBackButton = false;

    [ObservableProperty]
    private bool showHamburgerButton = false;

    #endregion

    #region === Footer Flags ===

    [ObservableProperty]
    private bool showFooter = false;

    partial void OnShowFooterChanged(bool value)
    {
        RecalculateBandLayout();
    }

    #endregion

    #region === Derived Heights ===

    [JsonIgnore]
    public float TotalBandHeight
    {
        get
        {
            float total = 0f;

            foreach (var band in Bands)
            {
                if (!IsBandVisible(band))
                    continue;

                total += band.EffectiveHeight;
            }

            return MathF.Round(total);
        }
    }

    private float GetMinimumScreenHeightForLayout()
    {
        var fillBand = GetAutoFillCustomBand();

        float fixedHeight = GetVisibleBandHeightExcept(fillBand);
        float fillMinHeight = fillBand != null ? GetBandMinHeightFromContent(fillBand) : 0f;

        return MathF.Round(Math.Max(DeviceHeight, fixedHeight + fillMinHeight));
    }

    #endregion

    #region === Construction ===

    public Screen() { }

    public Screen(long id, string name, Project? project)
    {
        this.id = id;
        this.name = name;
        Project = project;

        UserHeight = DeviceHeight;

        EnsureDefaultBands();
        RecalculateBandLayout();
        LoadBackgroundImage();
    }

    #endregion

    #region === Reconstruction ===

    public void Reconstruct(Project project)
    {
        Project = project;

        if (UserHeight <= 0)
            UserHeight = DeviceHeight;

        foreach (var band in Bands)
        {
            band.ParentScreen = this;
            band.Width = Width;
            band.EnsureInitialPage();
            band.EnsureDefaultIdentity(this);

            foreach (var page in band.Pages)
            {
                page.ParentBand = band;

                foreach (var ctrl in page.Controls)
                {
                    ctrl.ParentBandPage = page;
                    ctrl.ParentBand = band;
                }
            }
        }

        SortBands();
        RecalculateBandLayout();
        LoadBackgroundImage();

        MSG.UI.InvalidateDesigner();
    }

    #endregion

    #region === Band Layout ===

    public void EnsureDefaultBands()
    {
        if (!Bands.Any(b => b.BandType == BandType.Header))
            Bands.Add(CreateDefaultHeader());

        if (!Bands.Any(b => b.BandType == BandType.Custom))
            Bands.Add(CreateDefaultCustom());

        if (!Bands.Any(b => b.BandType == BandType.Footer))
            Bands.Add(CreateDefaultFooter());

        foreach (var band in Bands)
        {
            WireBand(band);
            band.EnsureInitialPage();
            band.EnsureDefaultIdentity(this);
        }

        SortBands();
    }

    private void WireBand(Band band)
    {
        band.ParentScreen = this;
        band.Width = Width;
        band.EnsureInitialPage();
        band.EnsureDefaultIdentity(this);
    }

    private Band CreateDefaultHeader()
    {
        var band = new Band
        {
            BandType = BandType.Header,
            HeaderBackground = Colors.LightGray,
            FooterBackground = Colors.Lime,
            Height = DefaultHeaderHeight,
            IsExpandable = false,
            IsExpanded = false,
            ParentScreen = this,
        };

        band.AddNewPage();
        band.EnsureDefaultIdentity(this);
        return band;
    }

    private Band CreateDefaultCustom()
    {
        var band = new Band
        {
            BandType = BandType.Custom,
            HeaderBackground = Colors.LightGray,
            FooterBackground = Colors.Lime,
            Height = DefaultBandHeight,
            SavedExpandedHeight = DefaultBandHeight,
            IsExpandable = false,
            IsExpanded = false,
            ParentScreen = this,
        };

        band.AddNewPage();
        band.EnsureDefaultIdentity(this);
        return band;
    }

    private Band CreateDefaultFooter()
    {
        var band = new Band
        {
            BandType = BandType.Footer,
            HeaderBackground = Colors.LightGray,
            Height = DefaulFooterHeight,
            IsExpandable = false,
            IsExpanded = false,
            ParentScreen = this,
        };

        band.AddNewPage();
        band.EnsureDefaultIdentity(this);
        return band;
    }

    private void SortBands()
    {
        var ordered = Bands
            .OrderBy(b =>
                b.BandType == BandType.Header ? 0
                : b.BandType == BandType.Custom ? 1
                : 2
            )
            .ToList();

        if (Bands.SequenceEqual(ordered))
            return;

        Bands.Clear();
        foreach (var b in ordered)
            Bands.Add(b);
    }

    internal bool IsBandVisible(Band band)
    {
        return (band.BandType != BandType.Header || ShowHeader)
            && (band.BandType != BandType.Footer || ShowFooter);
    }

    internal Band? GetAutoFillCustomBand()
    {
        return Bands.LastOrDefault(b => b.BandType == BandType.Custom && IsBandVisible(b));
    }

    internal bool IsAutoFillCustomBand(Band? band)
    {
        return band != null && ReferenceEquals(GetAutoFillCustomBand(), band);
    }

    private void RestoreFormerAutoFillBands(Band? currentFillBand)
    {
        foreach (var band in Bands.Where(b => b.BandType == BandType.Custom))
        {
            if (currentFillBand != null && ReferenceEquals(band, currentFillBand))
                continue;

            if (!band.HeightIsAutoFilled)
                continue;

            float preferredHeight = band.SavedExpandedHeight;

            if (preferredHeight <= DefaultBandHeaderHeight + 0.5f)
                preferredHeight = DefaultBandHeight;

            float minHeight = GetBandMinHeightFromContent(band);
            preferredHeight = MathF.Round(Math.Max(minHeight, preferredHeight));

            ApplyBandHeight(band, preferredHeight);
            band.HeightIsAutoFilled = false;
        }
    }

    private float GetVisibleBandHeightExcept(Band? excludedBand)
    {
        float total = 0f;

        foreach (var band in Bands)
        {
            if (!IsBandVisible(band))
                continue;

            if (excludedBand != null && ReferenceEquals(band, excludedBand))
                continue;

            total += MathF.Round(band.EffectiveHeight);
        }

        return MathF.Round(total);
    }

    private static void ApplyBandHeight(Band band, float height, bool updateSavedExpandedHeight = false)
    {
        height = MathF.Round(height);

        if (height < 0)
            height = 0;

        if (Math.Abs(band.Height - height) > 0.5f)
            band.Height = height;

        if (band.UniformPageHeight)
        {
            foreach (var page in band.Pages)
                page.Height = height;
        }
        else if (band.ActivePage != null)
        {
            band.ActivePage.Height = height;
        }

        if (updateSavedExpandedHeight && band.IsExpandable)
            band.SavedExpandedHeight = height;
    }

    private static float GetBandMinHeightFromContent(Band band)
    {
        float Round(float v) => MathF.Round(v);

        const float PADDING_BOTTOM = 10f;

        float headerH = Round(band.HeaderHeight);
        float baseMin = headerH;

        if (band.MinHeight > 0)
            baseMin = Math.Max(baseMin, Round(band.MinHeight));

        if (band.UniformPageHeight)
        {
            if (band.Pages == null || band.Pages.Count == 0)
                return baseMin;

            float requiredAcrossPages = 0f;
            bool anyControls = false;

            foreach (var p in band.Pages)
            {
                if (p == null || p.Controls == null || p.Controls.Count == 0)
                    continue;

                anyControls = true;

                float maxBottom = 0f;

                foreach (var c in p.Controls)
                {
                    float bottom = Round(c.Y + c.Height);
                    if (bottom > maxBottom)
                        maxBottom = bottom;
                }

                float contentMin = headerH + Round(maxBottom + PADDING_BOTTOM);
                if (contentMin > requiredAcrossPages)
                    requiredAcrossPages = contentMin;
            }

            if (!anyControls)
                return baseMin;

            return Math.Max(baseMin, requiredAcrossPages);
        }

        var page = band.ActivePage;

        if (page == null || page.Controls == null || page.Controls.Count == 0)
            return baseMin;

        float maxBottomActive = 0f;

        foreach (var c in page.Controls)
        {
            float bottom = Round(c.Y + c.Height);
            if (bottom > maxBottomActive)
                maxBottomActive = bottom;
        }

        float contentMinActive = headerH + Round(maxBottomActive + PADDING_BOTTOM);

        return Math.Max(baseMin, contentMinActive);
    }

    public void ResizeScreenFromDesigner(float deltaHeight)
    {
        float targetHeight = MathF.Round(ScreenHeight + deltaHeight);
        float minHeight = GetMinimumScreenHeightForLayout();

        if (targetHeight < minHeight)
            targetHeight = minHeight;

        if (Math.Abs(ScreenHeight - targetHeight) < 0.5f)
            return;

        try
        {
            _isRecalculatingLayout = true;
            UserHeight = targetHeight;
        }
        finally
        {
            _isRecalculatingLayout = false;
        }

        RecalculateBandLayout(invalidatePreview: false);
    }

    public void ResizeBandFromDesigner(Band resizeBand, float requestedHeight)
    {
        if (resizeBand == null)
            return;

        if (IsAutoFillCustomBand(resizeBand))
            return;

        float oldHeight = MathF.Round(resizeBand.Height);
        float minHeight = GetBandMinHeightFromContent(resizeBand);
        float newHeight = MathF.Round(Math.Max(minHeight, requestedHeight));

        if (Math.Abs(oldHeight - newHeight) < 0.5f)
            return;

        float delta = newHeight - oldHeight;
        float oldScreenHeight = MathF.Round(ScreenHeight);

        ApplyBandHeight(resizeBand, newHeight);
        resizeBand.HeightIsAutoFilled = false;
        resizeBand.SavedExpandedHeight = newHeight;

        float targetScreenHeight = MathF.Round(oldScreenHeight + delta);
        float minScreenHeight = GetMinimumScreenHeightForLayout();

        if (targetScreenHeight < minScreenHeight)
            targetScreenHeight = minScreenHeight;

        try
        {
            _isRecalculatingLayout = true;
            UserHeight = targetScreenHeight;
        }
        finally
        {
            _isRecalculatingLayout = false;
        }

        RecalculateBandLayout(invalidatePreview: false);
    }

    public void RecalculateBandLayout(bool invalidatePreview = true)
    {
        float Round(float v) => MathF.Round(v);

        EnsureDefaultBands();
        SortBands();

        foreach (var band in Bands)
        {
            band.ParentScreen = this;
            band.Width = Round(Width);
            band.X = 0;
        }

        var fillBand = GetAutoFillCustomBand();

        RestoreFormerAutoFillBands(fillBand);

        float fixedVisibleHeight = GetVisibleBandHeightExcept(fillBand);
        float fillMinHeight = fillBand != null ? GetBandMinHeightFromContent(fillBand) : 0f;

        float requiredHeight = Round(Math.Max(DeviceHeight, fixedVisibleHeight + fillMinHeight));
        float screenHeight = Round(Math.Max(UserHeight, requiredHeight));

        if (UserHeight < requiredHeight || Math.Abs(UserHeight - screenHeight) > 0.5f)
        {
            try
            {
                _isRecalculatingLayout = true;
                UserHeight = screenHeight;
            }
            finally
            {
                _isRecalculatingLayout = false;
            }
        }

        screenHeight = Round(ScreenHeight);

        if (fillBand != null)
        {
            float fillHeight = Round(Math.Max(fillMinHeight, screenHeight - fixedVisibleHeight));
            ApplyBandHeight(fillBand, fillHeight);
            fillBand.HeightIsAutoFilled = true;
        }

        float y = 0f;

        var headerBand = HeaderBand;
        var footerBand = FooterBand;

        if (headerBand != null)
        {
            if (ShowHeader)
            {
                headerBand.Y = 0f;
                y = Round(headerBand.EffectiveHeight);
            }
            else
            {
                headerBand.Y = -10000;
            }
        }

        foreach (var band in Bands.Where(b => b.BandType == BandType.Custom))
        {
            if (!IsBandVisible(band))
            {
                band.Y = -10000;
                continue;
            }

            band.Y = Round(y);
            y += Round(band.EffectiveHeight);
        }

        if (footerBand != null)
        {
            if (ShowFooter)
                footerBand.Y = Round(screenHeight - footerBand.EffectiveHeight);
            else
                footerBand.Y = -10000;
        }

        OnPropertyChanged(nameof(TotalBandHeight));
        OnPropertyChanged(nameof(ScreenHeight));

        if (invalidatePreview)

        MSG.UI.InvalidateDesigner();
    }


    public void NotifyHeightChanged(bool invalidatePreview = true)
    {
        OnPropertyChanged(nameof(TotalBandHeight));
        OnPropertyChanged(nameof(ScreenHeight));

        if (invalidatePreview)

        MSG.UI.InvalidateDesigner();
    }

    #endregion

    #region === CLONE ===

    public Screen DeepClone(Project newOwner)
    {
        var clone = new Screen(IdGenerator.NewID, Name, newOwner)
        {
            Descr = Descr,
            GroupName = GroupName,
            Background = Background,
            ShowHeader = ShowHeader,
            ShowFooter = ShowFooter,
            ShowBackButton = ShowBackButton,
            ShowHamburgerButton = ShowHamburgerButton,
            UserHeight = UserHeight,
            BackgroundImageFilename = BackgroundImageFilename,
            BackgroundImageBase64 = BackgroundImageBase64,
        };

        clone.Bands.Clear();

        foreach (var band in Bands)
        {
            var clonedBand = band.DeepClone();
            clonedBand.ParentScreen = clone;
            clone.Bands.Add(clonedBand);
        }

        clone.RecalculateBandLayout();
        return clone;
    }

    #endregion  === CLONE ===
}




//TODO: REMOVE


//// ======================================================================================
//// FILE: Mockup/Screen.cs
////
//// ZWECK:
//// Repräsentiert einen Screen basierend auf einer festen Device-Breite
//// und einer persistierten Screen-Höhe mit Mindesthöhe = DeviceHeight.
////
//// KONZEPT (VEREINFACHT):
//// - Das letzte Custom-Band ist ein Auto-Fill-Band.
//// - Header und Footer bleiben eigenständige Bands.
//// - ScreenHeight = max(DeviceHeight, UserHeight, Mindesthöhe aus festen Bands + Auto-Fill-Minimum).
//// - Beim Vergrößern eines nicht-Auto-Fill-Bands wächst der Screen.
//// - Beim Verkleinern schrumpft der Screen bis DeviceHeight, danach wächst das Auto-Fill-Band.
//// ======================================================================================

//using CommunityToolkit.Mvvm.ComponentModel;
//using Mockup.Messages;
//using Mockup.ViewModel;
//using SkiaSharp;
//using System.Collections.ObjectModel;
//using System.Collections.Specialized;
//using System.IO;
//using System.Text.Json.Serialization;
//using System.Windows.Media;

//namespace Mockup;

//public partial class Screen : ObservableObject
//{
//    #region === Identity ===

//    public static readonly Color DefaultBackground = Colors.White;
//    public static readonly float DefaultHeaderHeight = 35f;
//    public static readonly float DefaulFooterHeight = 35f;
//    public static readonly float DefaultBandHeight = 150f;
//    public static readonly float DefaultBandHeaderHeight = 35f;


//    [ObservableProperty]
//    private long id;

//    [ObservableProperty]
//    private string name = "";

//    [JsonIgnore]
//    [ObservableProperty]
//    private Project? project;

//    [ObservableProperty]
//    [NotifyPropertyChangedFor(nameof(GroupSortKey))]
//    private string groupName = MockupViewModel.DEFAULT_SCREEN_GROUPNAME;

//    public string GroupSortKey =>
//        string.Equals(
//            GroupName,
//            MockupViewModel.DEFAULT_SCREEN_GROUPNAME,
//            StringComparison.OrdinalIgnoreCase
//        )
//            ? "0_HOME"
//            : "1_" + (GroupName ?? "");

//    [ObservableProperty]
//    private string descr = string.Empty;

//    [ObservableProperty]
//    private Color background = DefaultBackground;

//    [ObservableProperty]
//    private bool isHomeScreen;

//    partial void OnIsHomeScreenChanged(bool value)
//    {
//        if (value)
//            MockupService.Mockup.HomeScreen = this;
//    }

//    #endregion

//    #region === Device Size ===

//    [JsonIgnore]
//    public float Width => Project?.DeviceWidth ?? 0f;

//    [JsonIgnore]
//    public float DeviceHeight => Project?.DeviceHeight ?? 0f;

//    /// <summary>
//    /// Persistierte Screen-Höhe. Darf niemals kleiner als DeviceHeight werden.
//    /// Das letzte Custom-Band füllt den verbleibenden Raum innerhalb dieser Höhe.
//    /// </summary>
//    [ObservableProperty]
//    [NotifyPropertyChangedFor(nameof(ScreenHeight))]
//    private float userHeight;

//    [JsonIgnore]
//    private bool _isRecalculatingLayout;

//    partial void OnUserHeightChanged(float value)
//    {
//        if (_isRecalculatingLayout)
//            return;

//        float min = DeviceHeight;
//        if (value < min)
//        {
//            try
//            {
//                _isRecalculatingLayout = true;
//                UserHeight = min;
//            }
//            finally
//            {
//                _isRecalculatingLayout = false;
//            }

//            return;
//        }

//        if (value <= 0)
//            return;

//        RecalculateBandLayout();
//    }

//    [JsonIgnore]
//    public float ScreenHeight => Math.Max(Math.Max(DeviceHeight, UserHeight), GetMinimumScreenHeightForLayout());

//    #endregion

//    #region === Background Image ===

//    [ObservableProperty]
//    private string? backgroundImageFilename;

//    [ObservableProperty]
//    private string? backgroundImageBase64;

//    partial void OnBackgroundImageBase64Changed(string? value)
//    {
//        LoadBackgroundImage();
//    }

//    [ObservableProperty]
//    [JsonIgnore]
//    private SKBitmap? _backgroundImage;

//    partial void OnBackgroundImageChanged(SKBitmap? oldValue, SKBitmap? newValue)
//    {
//        MSG.UI.InvalidateDesigner();
//    }

//    public void SetBackgroundImageFromFile(string filePath)
//    {
//        if (!File.Exists(filePath))
//            return;

//        try
//        {
//            var bytes = File.ReadAllBytes(filePath);
//            BackgroundImageBase64 = Convert.ToBase64String(bytes);

//            MSG.UI.InvalidateDesigner();
//        }
//        catch (Exception ex)
//        {
//            Serilog.Log.Error(ex, "Failed to set background image for screen {Name}", Name);
//        }
//    }

//    internal void LoadBackgroundImage()
//    {
//        if (string.IsNullOrWhiteSpace(BackgroundImageBase64))
//        {
//            BackgroundImage = null;
//            return;
//        }

//        SKBitmap? decoded = null;

//        try
//        {
//            var bytes = Convert.FromBase64String(BackgroundImageBase64);
//            using var ms = new MemoryStream(bytes);

//            decoded = SKBitmap.Decode(ms);
//            BackgroundImage = decoded;
//            decoded = null;
//        }
//        catch
//        {
//            decoded?.Dispose();
//            BackgroundImage = null;
//        }
//    }

//    public void ResetBackgroundImage()
//    {
//        BackgroundImageFilename = null;
//        BackgroundImageBase64 = null;
//        BackgroundImage = null;

//        MSG.UI.InvalidateDesigner();
//    }

//    #endregion

//    #region === Bands ===

//    [ObservableProperty]
//    private ObservableCollection<Band> bands = [];

//    partial void OnBandsChanged(
//        ObservableCollection<Band>? oldValue,
//        ObservableCollection<Band> newValue
//    )
//    {
//        if (oldValue != null)
//            oldValue.CollectionChanged -= OnBandsCollectionChanged;

//        if (newValue != null)
//        {
//            newValue.CollectionChanged += OnBandsCollectionChanged;

//            foreach (var band in newValue)
//                WireBand(band);
//        }

//        RecalculateBandLayout();
//    }

//    private void OnBandsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
//    {
//        if (e.NewItems != null)
//        {
//            foreach (Band band in e.NewItems)
//                WireBand(band);
//        }

//        RecalculateBandLayout();
//    }

//    [JsonIgnore]
//    public Band? HeaderBand => Bands.FirstOrDefault(b => b.BandType == BandType.Header);

//    [JsonIgnore]
//    public Band? FooterBand => Bands.FirstOrDefault(b => b.BandType == BandType.Footer);

//    [JsonIgnore]
//    public IEnumerable<Band> CustomBands => Bands.Where(b => b.BandType == BandType.Custom);

//    [JsonIgnore]
//    public IEnumerable<DesignControl> AllControls =>
//        Bands.SelectMany(b => b.Pages ?? []).SelectMany(p => p.Controls);

//    #endregion

//    #region === Header Flags ===

//    [ObservableProperty]
//    private bool showHeader = false;

//    partial void OnShowHeaderChanged(bool value)
//    {
//        RecalculateBandLayout();
//    }

//    [ObservableProperty]
//    private bool showBackButton = false;

//    [ObservableProperty]
//    private bool showHamburgerButton = false;

//    #endregion

//    #region === Footer Flags ===

//    [ObservableProperty]
//    private bool showFooter = false;

//    partial void OnShowFooterChanged(bool value)
//    {
//        RecalculateBandLayout();
//    }

//    #endregion

//    #region === Derived Heights ===

//    [JsonIgnore]
//    public float TotalBandHeight
//    {
//        get
//        {
//            float total = 0f;

//            foreach (var band in Bands)
//            {
//                if (!IsBandVisible(band))
//                    continue;

//                total += band.EffectiveHeight;
//            }

//            return MathF.Round(total);
//        }
//    }

//    private float GetMinimumScreenHeightForLayout()
//    {
//        var fillBand = GetAutoFillCustomBand();

//        float fixedHeight = GetVisibleBandHeightExcept(fillBand);
//        float fillMinHeight = fillBand != null ? GetBandMinHeightFromContent(fillBand) : 0f;

//        return MathF.Round(Math.Max(DeviceHeight, fixedHeight + fillMinHeight));
//    }

//    #endregion

//    #region === Construction ===

//    public Screen() { }

//    public Screen(long id, string name, Project? project)
//    {
//        this.id = id;
//        this.name = name;
//        Project = project;

//        UserHeight = DeviceHeight;

//        EnsureDefaultBands();
//        RecalculateBandLayout();
//        LoadBackgroundImage();
//    }

//    #endregion

//    #region === Reconstruction ===

//    public void Reconstruct(Project project)
//    {
//        Project = project;

//        if (UserHeight <= 0)
//            UserHeight = DeviceHeight;

//        foreach (var band in Bands)
//        {
//            band.ParentScreen = this;
//            band.Width = Width;
//            band.EnsureInitialPage();
//            band.EnsureDefaultIdentity(this);

//            foreach (var page in band.Pages)
//            {
//                page.ParentBand = band;

//                foreach (var ctrl in page.Controls)
//                {
//                    ctrl.ParentBandPage = page;
//                    ctrl.ParentBand = band;
//                }
//            }
//        }

//        SortBands();
//        RecalculateBandLayout();
//        LoadBackgroundImage();

//        MSG.UI.InvalidateDesigner();
//    }

//    #endregion

//    #region === Band Layout ===

//    public void EnsureDefaultBands()
//    {
//        if (!Bands.Any(b => b.BandType == BandType.Header))
//            Bands.Add(CreateDefaultHeader());

//        if (!Bands.Any(b => b.BandType == BandType.Custom))
//            Bands.Add(CreateDefaultCustom());

//        if (!Bands.Any(b => b.BandType == BandType.Footer))
//            Bands.Add(CreateDefaultFooter());

//        foreach (var band in Bands)
//        {
//            WireBand(band);
//            band.EnsureInitialPage();
//            band.EnsureDefaultIdentity(this);
//        }

//        SortBands();
//    }

//    private void WireBand(Band band)
//    {
//        band.ParentScreen = this;
//        band.Width = Width;
//        band.EnsureInitialPage();
//        band.EnsureDefaultIdentity(this);
//    }

//    private Band CreateDefaultHeader()
//    {
//        var band = new Band
//        {
//            BandType = BandType.Header,
//            HeaderBackground = Colors.LightGray,
//            FooterBackground = Colors.Lime,
//            Height = DefaultHeaderHeight,
//            IsExpandable = false,
//            IsExpanded = false,
//            ParentScreen = this,
//        };

//        band.AddNewPage();
//        band.EnsureDefaultIdentity(this);
//        return band;
//    }

//    private Band CreateDefaultCustom()
//    {
//        var band = new Band
//        {
//            BandType = BandType.Custom,
//            HeaderBackground = Colors.LightGray,
//            FooterBackground = Colors.Lime,
//            Height = DefaultBandHeight,
//            SavedExpandedHeight = DefaultBandHeight,
//            IsExpandable = false,
//            IsExpanded = false,
//            ParentScreen = this,
//        };

//        band.AddNewPage();
//        band.EnsureDefaultIdentity(this);
//        return band;
//    }

//    private Band CreateDefaultFooter()
//    {
//        var band = new Band
//        {
//            BandType = BandType.Footer,
//            HeaderBackground = Colors.LightGray,
//            Height = DefaulFooterHeight,
//            IsExpandable = false,
//            IsExpanded = false,
//            ParentScreen = this,
//        };

//        band.AddNewPage();
//        band.EnsureDefaultIdentity(this);
//        return band;
//    }

//    private void SortBands()
//    {
//        var ordered = Bands
//            .OrderBy(b =>
//                b.BandType == BandType.Header ? 0
//                : b.BandType == BandType.Custom ? 1
//                : 2
//            )
//            .ToList();

//        if (Bands.SequenceEqual(ordered))
//            return;

//        Bands.Clear();
//        foreach (var b in ordered)
//            Bands.Add(b);
//    }

//    internal bool IsBandVisible(Band band)
//    {
//        return (band.BandType != BandType.Header || ShowHeader)
//            && (band.BandType != BandType.Footer || ShowFooter);
//    }

//    internal Band? GetAutoFillCustomBand()
//    {
//        return Bands.LastOrDefault(b => b.BandType == BandType.Custom && IsBandVisible(b));
//    }

//    internal bool IsAutoFillCustomBand(Band? band)
//    {
//        return band != null && ReferenceEquals(GetAutoFillCustomBand(), band);
//    }

//    private void RestoreFormerAutoFillBands(Band? currentFillBand)
//    {
//        foreach (var band in Bands.Where(b => b.BandType == BandType.Custom))
//        {
//            if (currentFillBand != null && ReferenceEquals(band, currentFillBand))
//                continue;

//            if (!band.HeightIsAutoFilled)
//                continue;

//            float preferredHeight = band.SavedExpandedHeight;

//            if (preferredHeight <= DefaultBandHeaderHeight + 0.5f)
//                preferredHeight = DefaultBandHeight;

//            float minHeight = GetBandMinHeightFromContent(band);
//            preferredHeight = MathF.Round(Math.Max(minHeight, preferredHeight));

//            ApplyBandHeight(band, preferredHeight);
//            band.HeightIsAutoFilled = false;
//        }
//    }

//    private float GetVisibleBandHeightExcept(Band? excludedBand)
//    {
//        float total = 0f;

//        foreach (var band in Bands)
//        {
//            if (!IsBandVisible(band))
//                continue;

//            if (excludedBand != null && ReferenceEquals(band, excludedBand))
//                continue;

//            total += MathF.Round(band.EffectiveHeight);
//        }

//        return MathF.Round(total);
//    }

//    private static void ApplyBandHeight(Band band, float height, bool updateSavedExpandedHeight = false)
//    {
//        height = MathF.Round(height);

//        if (height < 0)
//            height = 0;

//        if (Math.Abs(band.Height - height) > 0.5f)
//            band.Height = height;

//        if (band.UniformPageHeight)
//        {
//            foreach (var page in band.Pages)
//                page.Height = height;
//        }
//        else if (band.ActivePage != null)
//        {
//            band.ActivePage.Height = height;
//        }

//        if (updateSavedExpandedHeight && band.IsExpandable)
//            band.SavedExpandedHeight = height;
//    }

//    private static float GetBandMinHeightFromContent(Band band)
//    {
//        float Round(float v) => MathF.Round(v);

//        const float PADDING_BOTTOM = 10f;

//        float headerH = Round(band.HeaderHeight);
//        float baseMin = headerH;

//        if (band.MinHeight > 0)
//            baseMin = Math.Max(baseMin, Round(band.MinHeight));

//        if (band.UniformPageHeight)
//        {
//            if (band.Pages == null || band.Pages.Count == 0)
//                return baseMin;

//            float requiredAcrossPages = 0f;
//            bool anyControls = false;

//            foreach (var p in band.Pages)
//            {
//                if (p == null || p.Controls == null || p.Controls.Count == 0)
//                    continue;

//                anyControls = true;

//                float maxBottom = 0f;

//                foreach (var c in p.Controls)
//                {
//                    float bottom = Round(c.Y + c.Height);
//                    if (bottom > maxBottom)
//                        maxBottom = bottom;
//                }

//                float contentMin = headerH + Round(maxBottom + PADDING_BOTTOM);
//                if (contentMin > requiredAcrossPages)
//                    requiredAcrossPages = contentMin;
//            }

//            if (!anyControls)
//                return baseMin;

//            return Math.Max(baseMin, requiredAcrossPages);
//        }

//        var page = band.ActivePage;

//        if (page == null || page.Controls == null || page.Controls.Count == 0)
//            return baseMin;

//        float maxBottomActive = 0f;

//        foreach (var c in page.Controls)
//        {
//            float bottom = Round(c.Y + c.Height);
//            if (bottom > maxBottomActive)
//                maxBottomActive = bottom;
//        }

//        float contentMinActive = headerH + Round(maxBottomActive + PADDING_BOTTOM);

//        return Math.Max(baseMin, contentMinActive);
//    }

//    public void ResizeScreenFromDesigner(float deltaHeight)
//    {
//        float targetHeight = MathF.Round(ScreenHeight + deltaHeight);
//        float minHeight = GetMinimumScreenHeightForLayout();

//        if (targetHeight < minHeight)
//            targetHeight = minHeight;

//        if (Math.Abs(ScreenHeight - targetHeight) < 0.5f)
//            return;

//        try
//        {
//            _isRecalculatingLayout = true;
//            UserHeight = targetHeight;
//        }
//        finally
//        {
//            _isRecalculatingLayout = false;
//        }

//        RecalculateBandLayout();
//    }

//    public void ResizeBandFromDesigner(Band resizeBand, float requestedHeight)
//    {
//        if (resizeBand == null)
//            return;

//        if (IsAutoFillCustomBand(resizeBand))
//            return;

//        float oldHeight = MathF.Round(resizeBand.Height);
//        float minHeight = GetBandMinHeightFromContent(resizeBand);
//        float newHeight = MathF.Round(Math.Max(minHeight, requestedHeight));

//        if (Math.Abs(oldHeight - newHeight) < 0.5f)
//            return;

//        float delta = newHeight - oldHeight;
//        float oldScreenHeight = MathF.Round(ScreenHeight);

//        ApplyBandHeight(resizeBand, newHeight);
//        resizeBand.HeightIsAutoFilled = false;
//        resizeBand.SavedExpandedHeight = newHeight;

//        float targetScreenHeight = MathF.Round(oldScreenHeight + delta);
//        float minScreenHeight = GetMinimumScreenHeightForLayout();

//        if (targetScreenHeight < minScreenHeight)
//            targetScreenHeight = minScreenHeight;

//        try
//        {
//            _isRecalculatingLayout = true;
//            UserHeight = targetScreenHeight;
//        }
//        finally
//        {
//            _isRecalculatingLayout = false;
//        }

//        RecalculateBandLayout();
//    }

//    public void RecalculateBandLayout()
//    {
//        float Round(float v) => MathF.Round(v);

//        EnsureDefaultBands();
//        SortBands();

//        foreach (var band in Bands)
//        {
//            band.ParentScreen = this;
//            band.Width = Round(Width);
//            band.X = 0;
//        }

//        var fillBand = GetAutoFillCustomBand();

//        RestoreFormerAutoFillBands(fillBand);

//        float fixedVisibleHeight = GetVisibleBandHeightExcept(fillBand);
//        float fillMinHeight = fillBand != null ? GetBandMinHeightFromContent(fillBand) : 0f;

//        float requiredHeight = Round(Math.Max(DeviceHeight, fixedVisibleHeight + fillMinHeight));
//        float screenHeight = Round(Math.Max(UserHeight, requiredHeight));

//        if (UserHeight < requiredHeight || Math.Abs(UserHeight - screenHeight) > 0.5f)
//        {
//            try
//            {
//                _isRecalculatingLayout = true;
//                UserHeight = screenHeight;
//            }
//            finally
//            {
//                _isRecalculatingLayout = false;
//            }
//        }

//        screenHeight = Round(ScreenHeight);

//        if (fillBand != null)
//        {
//            float fillHeight = Round(Math.Max(fillMinHeight, screenHeight - fixedVisibleHeight));
//            ApplyBandHeight(fillBand, fillHeight);
//            fillBand.HeightIsAutoFilled = true;
//        }

//        float y = 0f;

//        var headerBand = HeaderBand;
//        var footerBand = FooterBand;

//        if (headerBand != null)
//        {
//            if (ShowHeader)
//            {
//                headerBand.Y = 0f;
//                y = Round(headerBand.EffectiveHeight);
//            }
//            else
//            {
//                headerBand.Y = -10000;
//            }
//        }

//        foreach (var band in Bands.Where(b => b.BandType == BandType.Custom))
//        {
//            if (!IsBandVisible(band))
//            {
//                band.Y = -10000;
//                continue;
//            }

//            band.Y = Round(y);
//            y += Round(band.EffectiveHeight);
//        }

//        if (footerBand != null)
//        {
//            if (ShowFooter)
//                footerBand.Y = Round(screenHeight - footerBand.EffectiveHeight);
//            else
//                footerBand.Y = -10000;
//        }

//        OnPropertyChanged(nameof(TotalBandHeight));
//        OnPropertyChanged(nameof(ScreenHeight));

//        MSG.UI.InvalidateDesigner();
//    }


//    public void NotifyHeightChanged()
//    {
//        OnPropertyChanged(nameof(TotalBandHeight));
//        OnPropertyChanged(nameof(ScreenHeight));

//        MSG.UI.InvalidateDesigner();
//    }

//    #endregion

//    #region === CLONE ===

//    public Screen DeepClone(Project newOwner)
//    {
//        var clone = new Screen(IdGenerator.NewID, Name, newOwner)
//        {
//            Descr = Descr,
//            GroupName = GroupName,
//            Background = Background,
//            ShowHeader = ShowHeader,
//            ShowFooter = ShowFooter,
//            ShowBackButton = ShowBackButton,
//            ShowHamburgerButton = ShowHamburgerButton,
//            UserHeight = UserHeight,
//            BackgroundImageFilename = BackgroundImageFilename,
//            BackgroundImageBase64 = BackgroundImageBase64,
//        };

//        clone.Bands.Clear();

//        foreach (var band in Bands)
//        {
//            var clonedBand = band.DeepClone();
//            clonedBand.ParentScreen = clone;
//            clone.Bands.Add(clonedBand);
//        }

//        clone.RecalculateBandLayout();
//        return clone;
//    }

//    #endregion  === CLONE ===
//}
