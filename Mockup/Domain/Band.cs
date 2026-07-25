// ======================================================================================
// FILE: Mockup/Band.cs
//
// MO44 – Band-Handling bereinigt
// - Zentrale Rect-/Header-/Content-Logik
// - ActivePage.WorldBounds folgt dem ContentRect
// - RenderControls / HitTests nutzen dieselbe Basis
// - Default-Band-Identity vorbereitet:
//   - Title standardmäßig "<Title>"
//   - Name automatisch "CustomBand1", "CustomBand2", ...
// - Fix:
//   - Auto-Name wird nicht mehrfach neu vergeben
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.Actions;
using Mockup.Messages;
using Mockup.Rendering;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows.Media;
using Topten.RichTextKit;

namespace Mockup;

public partial class Band : DesignControl
{
    public static string DEFAULT_NAME = "CustomBand";
    public static string DEFAULT_TITLE = "<Title>";

    #region === CTOR ===

    public Band()
    {
        Name = DEFAULT_NAME;
        Title = DEFAULT_TITLE;

        Height = Screen.DefaultBandHeaderHeight;
        SavedExpandedHeight = Height;
        MinHeight = Screen.DefaultBandHeaderHeight;
        ResizeStyle = ResizeStyles.HeightOnly;

        Pages.CollectionChanged += (_, __) => OnPagesChanged();
        PropertyChanged += OnBandPropertyChanged;
    }

    #endregion === CTOR ===

    #region === IDENTITY / PARENT ===

    [ObservableProperty]
    [JsonIgnore]
    [property: Browsable(false)]
    private Screen? parentScreen;

    partial void OnParentScreenChanged(Screen? value)
    {
        EnsureDefaultIdentity(value);
    }

    #endregion === IDENTITY / PARENT ===

    #region === TITLE, BORDERCOLOR, ISEXPANDABLE, ISEXPANDED, BANDTYPE, ROTATEPAGES ===

    [ObservableProperty]
    private bool isSticky;

    [ObservableProperty]
    [property: Mockup.Registry.ControlProp, Category("Appearance"), DisplayName("Title")]
    private string title = "<Title>";

    [ObservableProperty]
    [property:
        Mockup.Registry.ControlProp,
        Category("Appearance"),
        DisplayName("Band Background")
    ]
    private Color bandBackground = Colors.Transparent;

    [ObservableProperty]
    [property:
        Mockup.Registry.ControlProp,
        Category("Appearance"),
        DisplayName("Header Background")
    ]
    private Color headerBackground = Colors.LightGray;


    [ObservableProperty]
    [property:
        Mockup.Registry.ControlProp,
        Category("Appearance"),
        DisplayName("Footer Background")
    ]
    private Color footerBackground = Colors.Gray;



    [ObservableProperty]
    [property: Mockup.Registry.ControlProp, Category("Behavior"), DisplayName("Is Expandable")]
    private bool isExpandable = false;

    partial void OnIsExpandableChanged(bool value)
    {
        if (!value)
        {
            IsExpanded = false;
            SavedExpandedHeight = 0;
        }
    }

    [ObservableProperty]
    [property: Mockup.Registry.ControlProp, Category("Behavior"), DisplayName("Is Expanded")]
    private bool isExpanded = false;

    partial void OnIsExpandedChanged(bool value)
    {
        SavedExpandedHeight = Math.Max(SavedExpandedHeight, Screen.DefaultBandHeaderHeight);

        foreach (BandPage page in Pages)
            page.Height = Math.Max(page.Height, SavedExpandedHeight);

        UpdateActivePageWorldBounds();
        MSG.UI.InvalidateDesigner();
    }

    [ObservableProperty]
    [property: Mockup.Registry.ControlProp, Category("Behavior"), DisplayName("Band Type")]
    [property: Browsable(false)]
    private BandType bandType = BandType.Custom;

    partial void OnBandTypeChanged(BandType value)
    {
        EnsureDefaultIdentity(ParentScreen);
        UpdateActivePageWorldBounds();
    }

    [ObservableProperty]
    [property: Mockup.Registry.ControlProp, Category("Behavior"), DisplayName("Rotate Pages")]
    private bool rotatePages = false;

    #endregion === TITLE, BORDERCOLOR, ISEXPANDABLE, ISEXPANDED, BANDTYPE, ROTATEPAGES ===

    #region === MULTI-PAGE SUPPORT ===

    [ObservableProperty]
    [property: Mockup.Registry.ControlProp, Category("Appearance"), DisplayName("Pages")]
    private ObservableCollection<BandPage> pages = new();

    [JsonIgnore]
    public List<SKRect> TabRects { get; } = new();

    [JsonConverter(typeof(JsonStringEnumConverter))]
    [ObservableProperty]
    [property: Mockup.Registry.ControlProp, Category("Appearance"), DisplayName("Show Tabs")]
    public bool showTabs;

    [ObservableProperty]
    [property: Mockup.Registry.ControlProp, Category("Appearance"), DisplayName("Page Index")]
    private int activePageIndex = 0;

    partial void OnActivePageIndexChanged(int value)
    {
        if (Pages == null || Pages.Count == 0)
        {
            activePageIndex = 0;
            return;
        }

        if (ActivePage == null)
            return;

        activePageIndex = Math.Clamp(value, 0, Pages.Count - 1);

        if (!UsesScreenAutoFillHeight())
        {
            Height = ActivePage.Height;
            SavedExpandedHeight = ActivePage.Height;
        }

        if (UniformPageHeight)
            SyncPageHeights();

        UpdateActivePageWorldBounds();
    }

    [JsonIgnore]
    [Browsable(false)]
    public BandPage? ActivePage => Pages.Count == 0 ? null : Pages[ActivePageIndex];

    [ObservableProperty]
    [property:
        Mockup.Registry.ControlProp,
        Category("Appearance"),
        DisplayName("Uniform Page Height")
    ]
    private bool uniformPageHeight = false;

    partial void OnUniformPageHeightChanged(bool value)
    {
        if (Pages.Count == 0)
            return;

        if (value)
            SyncPageHeights();

        if (ActivePage != null && !UsesScreenAutoFillHeight())
            SavedExpandedHeight = ActivePage.Height;
    }

    [ObservableProperty]
    [property: Browsable(false)]
    public float tabScrollOffsetX = 0f;

    public void ClampTabScroll()
    {
        if (!ShowsTabs)
        {
            TabScrollOffsetX = 0;
            return;
        }

        if (Pages.Count <= 1 || TabRects.Count == 0)
        {
            TabScrollOffsetX = 0;
            return;
        }

        float usableWidth = TabsClipRect.Width;

        float left = TabRects[0].Left;
        float right = TabRects[TabRects.Count - 1].Right;
        float tabsTotalWidth = right - left;

        float max = 0;
        float min = Math.Min(0, usableWidth - tabsTotalWidth);

        TabScrollOffsetX = Math.Clamp(TabScrollOffsetX, min, max);
    }

    #endregion === MULTI-PAGE SUPPORT ===

    #region === LAYOUT / HÖHE ===

    [JsonIgnore]
    [Browsable(false)]
    public float EffectiveHeight =>
        UsesScreenAutoFillHeight()
            ? Height
            : (IsExpandable && !IsExpanded) ? Screen.DefaultBandHeaderHeight : Height;

    [ObservableProperty]
    [property:
        Mockup.Registry.ControlProp,
        Category("Layout"),
        DisplayName("Saved Expanded Height")
    ]
    private float savedExpandedHeight;

    [JsonIgnore]
    internal bool HeightIsAutoFilled;

    private bool UsesScreenAutoFillHeight()
    {
        return BandType == BandType.Custom
            && (HeightIsAutoFilled || ParentScreen?.IsAutoFillCustomBand(this) == true);
    }

    #endregion === LAYOUT / HÖHE ===

    #region === DEFAULT IDENTITY ===

    public void EnsureDefaultIdentity()
    {
        EnsureDefaultIdentity(ParentScreen);
    }

    public void EnsureDefaultIdentity(Screen? screen)
    {
        if (BandType == BandType.Header)
        {
            Name = "Header";

            //if (string.IsNullOrWhiteSpace(Title) || Title == Band.DEFAULT_TITLE)
            //    Title = "Header";

            return;
        }

        if (BandType == BandType.Footer)
        {
            Name = "Footer";

            //if (string.IsNullOrWhiteSpace(Title) || Title == Band.DEFAULT_TITLE)
            //    Title = "Footer";

            return;
        }

        if (string.IsNullOrWhiteSpace(Title))
            Title = Band.DEFAULT_TITLE;

        AssignDefaultBandNameIfNeeded(screen);
    }

    private void AssignDefaultBandNameIfNeeded(Screen? screen)
    {
        if (!NeedsDefaultBandName())
            return;

        if (screen == null)
            return;

        Name = BuildNextDefaultBandName(screen);
    }

    private bool NeedsDefaultBandName()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return true;

        var trimmed = Name.Trim();

        if (trimmed.Equals(Band.DEFAULT_NAME, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private string BuildNextDefaultBandName(Screen? screen)
    {
        string prefix = Band.DEFAULT_NAME;

        if (screen?.Bands == null || screen.Bands.Count == 0)
            return $"{prefix}1";

        int maxNumber = 0;

        foreach (var band in screen.CustomBands)
        {
            if (band == null || ReferenceEquals(band, this) || string.IsNullOrWhiteSpace(band.Name))
                continue;

            var name = band.Name.Trim();

            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var suffix = name.Substring(prefix.Length);

            if (int.TryParse(suffix, out int number))
                maxNumber = Math.Max(maxNumber, number);
        }

        return $"{prefix}{maxNumber + 1}";
    }

    #endregion === DEFAULT IDENTITY ===

    #region === BAND RECT HELPERS ===

    [JsonIgnore]
    [Browsable(false)]
    public bool HasVisibleHeader
    {
        get
        {
            return BandType switch
            {
                BandType.Header => true,
                BandType.Footer => true,
                BandType.Custom => HasCustomHeaderVisuals,
                _ => false,
            };
        }
    }

    [JsonIgnore]
    [Browsable(false)]
    public bool HasMeaningfulTitle =>
        !string.IsNullOrWhiteSpace(Title)
        && !Title.Equals(DEFAULT_TITLE, StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    [Browsable(false)]
    public bool HasCustomHeaderVisuals => HasMeaningfulTitle || ShowTabs || IsExpandable;

    [JsonIgnore]
    [Browsable(false)]
    public bool ShowsTitle => HasVisibleHeader && !ShowsTabs && HasMeaningfulTitle;

    [JsonIgnore]
    [Browsable(false)]
    public bool ShowsTabs
    {
        get
        {
            if (!HasVisibleHeader)
                return false;

            if (!ShowTabs || Pages.Count <= 1)
                return false;

            if (IsExpandable && !IsExpanded)
                return false;

            return true;
        }
    }

    [JsonIgnore]
    [Browsable(false)]
    public bool ShowsToggle => IsExpandable && HasVisibleHeader;

    [JsonIgnore]
    [Browsable(false)]
    public float HeaderHeight => HasVisibleHeader ? Screen.DefaultBandHeaderHeight : 0f;

    [JsonIgnore]
    [Browsable(false)]
    public float ContentTop => WorldBounds.Top + HeaderHeight;

    [JsonIgnore]
    [Browsable(false)]
    public SKRect BandRect => WorldBounds;

    [JsonIgnore]
    [Browsable(false)]
    public SKRect HeaderRect
    {
        get
        {
            if (!HasVisibleHeader)
                return SKRect.Empty;

            return SKRect.Create(
                WorldBounds.Left,
                WorldBounds.Top,
                WorldBounds.Width,
                HeaderHeight
            );
        }
    }

    [JsonIgnore]
    [Browsable(false)]
    public SKRect ContentRect
    {
        get
        {
            float top = ContentTop;
            float bottom = WorldBounds.Bottom;

            if (bottom < top)
                bottom = top;

            return new SKRect(WorldBounds.Left, top, WorldBounds.Right, bottom);
        }
    }

    [JsonIgnore]
    [Browsable(false)]
    public SKRect TabsClipRect
    {
        get
        {
            if (!ShowsTabs)
                return SKRect.Empty;

            float rightReserve = ShowsToggle ? 24f : 0f;
            float width = Math.Max(0f, HeaderRect.Width - rightReserve);

            return SKRect.Create(HeaderRect.Left, HeaderRect.Top, width, HeaderRect.Height);
        }
    }

    [JsonIgnore]
    [Browsable(false)]
    public SKRect CalculatedTabsRect => TabsClipRect;

    [JsonIgnore]
    [Browsable(false)]
    public SKRect CalculatedToggleRect
    {
        get
        {
            if (!ShowsToggle)
                return SKRect.Empty;

            float buttonSize = 10f;
            float marginX = 10f;
            float right = HeaderRect.Right - marginX;
            float top = HeaderRect.Top + (HeaderHeight - buttonSize) / 2f + 1f;

            return SKRect.Create(right - buttonSize, top, buttonSize, buttonSize);
        }
    }

    [JsonIgnore]
    [Browsable(false)]
    public SKRect CalculatedToggleInteractiveRect
    {
        get
        {
            if (!ShowsToggle)
                return SKRect.Empty;

            float buttonSize = 10f;
            float btnWidth = 3 * buttonSize;
            float marginX = 10f;
            float right = HeaderRect.Right - marginX;

            return SKRect.Create(right - btnWidth, HeaderRect.Top, btnWidth, HeaderRect.Height);
        }
    }

    [JsonIgnore]
    [Browsable(false)]
    public SKRect CalculatedResizeRect
    {
        get
        {
            if (UsesScreenAutoFillHeight())
                return SKRect.Empty;

            if (IsExpandable && !IsExpanded)
                return SKRect.Empty;

            float triHeight = 7f;
            float spacing = 1f;

            if (BandType == BandType.Footer)
            {
                const float HIT_PAD_Y = 4f;

                float referenceY = WorldBounds.Top + 0.5f;
                float downTopY = referenceY + spacing;
                float triTop = downTopY;
                float triBottom = downTopY + triHeight;

                return SKRect.Create(
                    WorldBounds.Left,
                    triTop - HIT_PAD_Y,
                    WorldBounds.Width,
                    (triBottom - triTop) + 2 * HIT_PAD_Y
                );
            }
            else
            {
                float referenceY = WorldBounds.Bottom;
                float upBottomY = referenceY - spacing;
                float thumbAreaHeight = (triHeight * 2) + (spacing * 3);
                float thumbAreaTop = upBottomY - triHeight - spacing;

                return SKRect.Create(
                    WorldBounds.Left,
                    thumbAreaTop,
                    WorldBounds.Width,
                    thumbAreaHeight
                );
            }
        }
    }

    public SKRect GetBandRect() => BandRect;

    public SKRect GetHeaderRect() => HeaderRect;

    public SKRect GetContentRect() => ContentRect;

    public SKRect GetTabsRect() => CalculatedTabsRect;

    public SKRect GetToggleRect() => CalculatedToggleRect;

    public SKRect GetResizeRect() => CalculatedResizeRect;

    private void UpdateActivePageWorldBounds()
    {
        var page = ActivePage;
        if (page == null)
            return;

        page.WorldBounds = ContentRect;
    }

    #endregion === BAND RECT HELPERS ===

    #region === COLORS & PAINTS ===

    public readonly static Color DEFAULT_HEADER_COLOR = Color.FromRgb(240, 241, 243);
    public static readonly Color DEFAULT_FOOTER_COLOR = Color.FromRgb(200, 201, 203);

    private static readonly SKPaint borderPaint = new()
    {
        Color = SKColor.Parse("D0D0D0"),
        IsAntialias = false,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1f,
    };

    private static readonly SKPaint tabPaint = new()
    {
        IsAntialias = true,
        Color = SKColors.White,
        Style = SKPaintStyle.Fill,
    };

    private static readonly SKPaint tabBorderPaint = new()
    {
        IsAntialias = true,
        Color = SKColor.Parse("#C0C1C1"),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 0.75f,
    };

    private static readonly SKPaint tabBorderWeakPaint = new()
    {
        IsAntialias = true,
        Color = SKColor.Parse("#D0D1D1"),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 0.75f,
    };

    private static readonly SKPaint backgroundPaint = new()
    {
        Color = SKColors.Transparent,
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    private static readonly SKPaint headerPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    private static readonly SKPaint footerPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
    };

    private static readonly SKPaint selectionPaint = new()
    {
        IsAntialias = true,
        Color = SKColors.DodgerBlue,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.5f
    };

    private static readonly SKPaint selectionFillPaint = new()
    {
        IsAntialias = false,
        Color = SKColors.DodgerBlue,
        Style = SKPaintStyle.Fill,
    };

    private static readonly SKPaint thumbPaint = new()
    {
        IsAntialias = true,
        Color = SKColors.DodgerBlue,
        Style = SKPaintStyle.Fill,
    };

    private static readonly SKPaint toggleButtonPaint = new()
    {
        IsAntialias = true,
        Color = SKColors.Black.WithAlpha(200),
        Style = SKPaintStyle.Fill,
    };

    private static readonly SKPaint pageArrowPaint = new()
    {
        IsAntialias = true,
        Color = SKColors.DodgerBlue.WithAlpha(200),
        Style = SKPaintStyle.Fill,
    };

    private readonly SKImageFilter dropShadow = SKImageFilter.CreateDropShadow(
        0,
        0f,
        0,
        6,
        SKColors.Black.WithAlpha(60)
    );

    #endregion === COLORS & PAINTS ===

    #region === TABS CALCULATION ===

    private void BuildTabRects()
    {
        TabRects.Clear();

        if (!ShowsTabs)
            return;

        var clip = TabsClipRect;
        if (clip.IsEmpty)
            return;

        float x = clip.Left + 4f + TabScrollOffsetX;
        float y = clip.Top + 6f;
        float tabHeight = Math.Max(0f, HeaderHeight - 5f);

        foreach (var page in Pages)
        {
            float w = TextRenderer.MeasureTextWidth(text: page.Title ?? page.Name, fontSize: 11);
            float padding = 40f;
            float width = Math.Max(w + padding, 80f);

            var rect = SKRect.Create(x, y, width, tabHeight);
            TabRects.Add(rect);

            x += width + 4f;
        }
    }

    #endregion === TABS CALCULATION ===

    #region === HITEST HELPERS ===

    [JsonIgnore]
    public SKRect ResizeThumbRect;

    [JsonIgnore]
    public SKRect ToggleButtonRect;

    [JsonIgnore]
    public SKRect ToggleButtonInteractiveRect;

    [JsonIgnore]
    public SKRect LeftArrowRect;

    [JsonIgnore]
    public SKRect RightArrowRect;

    [JsonIgnore]
    public SKRect MoveUpRect;

    [JsonIgnore]
    public SKRect MoveDownRect;

    [JsonIgnore]
    public bool IsMouseOverBand = false;

    [JsonIgnore]
    public bool IsMouseNearLeft = false;

    [JsonIgnore]
    public bool IsMouseNearRight = false;

    public bool HitTestResize(SKPoint p)
    {
        if (UsesScreenAutoFillHeight())
            return false;

        var rect = !ResizeThumbRect.IsEmpty ? ResizeThumbRect : GetResizeRect();
        return rect.Contains(p);
    }

    public bool HitTestToggle(SKPoint p)
    {
        var rect = !ToggleButtonInteractiveRect.IsEmpty
            ? ToggleButtonInteractiveRect
            : CalculatedToggleInteractiveRect;

        return rect.Contains(p);
    }

    public bool HitTestBandHeader(SKPoint p) => HeaderRect.Contains(p);

    public bool HitTestLeftArrow(SKPoint p) => LeftArrowRect.Contains(p);

    public bool HitTestRightArrow(SKPoint p) => RightArrowRect.Contains(p);

    public bool HitTestMoveUp(SKPoint p) => MoveUpRect.Contains(p);

    public bool HitTestMoveDown(SKPoint p) => MoveDownRect.Contains(p);

    public bool HitTestPageArea(SKPoint p) => ContentRect.Contains(p);

    public int HitTestTabIndex(SKPoint p)
    {
        if (!ShowsTabs)
            return -1;

        for (int i = 0; i < TabRects.Count; i++)
            if (TabRects[i].Contains(p))
                return i;

        return -1;
    }

    public bool HitTestTabStrip(SKPoint p) => TabsClipRect.Contains(p);

    #endregion === HITEST HELPERS ===

    #region === PROPERTYCHANGED HANDLER ===

    private void OnBandPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ActivePageIndex))
        {
            if (ActivePage != null && !UsesScreenAutoFillHeight())
            {
                if (!FloatEquals(Height, ActivePage.Height))
                    Height = ActivePage.Height;

                SavedExpandedHeight = Height;
            }

            if (UniformPageHeight)
                SyncPageHeights();

            UpdateActivePageWorldBounds();
            return;
        }

        if (e.PropertyName == nameof(UniformPageHeight) && UniformPageHeight)
        {
            if (ActivePage != null && !UsesScreenAutoFillHeight() && !FloatEquals(Height, ActivePage.Height))
                Height = ActivePage.Height;

            SyncPageHeights();
            UpdateActivePageWorldBounds();
            return;
        }

        if (
            e.PropertyName == nameof(Title)
            || e.PropertyName == nameof(ShowTabs)
            || e.PropertyName == nameof(IsExpandable)
            || e.PropertyName == nameof(IsExpanded)
            || e.PropertyName == nameof(Height)
        )
        {
            UpdateActivePageWorldBounds();
        }
    }

    #endregion === PROPERTYCHANGED HANDLER ===

    #region === ENSURE INITIALPAGE ===

    public void EnsureInitialPage()
    {
        EnsureDefaultIdentity();

        Pages ??= new ObservableCollection<BandPage>();

        if (Pages.Count == 0)
        {
            float pageHeight =
                Height > 0 ? Height
                : SavedExpandedHeight > 0 ? SavedExpandedHeight
                : 120f;

            var page = new BandPage
            {
                Id = IdGenerator.NewID,
                ParentBand = this,
                Height = pageHeight,
                Title = "Page 1",
                Name = "Page1",
            };

            Pages.Add(page);

            ActivePageIndex = 0;
            SavedExpandedHeight = pageHeight;
        }

        if (ActivePage != null)
        {
            float resolvedHeight =
                ActivePage.Height > 0 ? ActivePage.Height
                : Height > 0 ? Height
                : Screen.DefaultBandHeaderHeight;

            if (!UsesScreenAutoFillHeight())
            {
                if (!FloatEquals(Height, resolvedHeight))
                    Height = resolvedHeight;

                if (!FloatEquals(ActivePage.Height, resolvedHeight))
                    ActivePage.Height = resolvedHeight;

                SavedExpandedHeight = resolvedHeight;
            }
            else if (!FloatEquals(ActivePage.Height, Height))
            {
                ActivePage.Height = Height;
            }
        }

        UpdateActivePageWorldBounds();
    }

    #endregion === ENSURE INITIALPAGE ===

    #region === PAGES CHANGED ===

    private void OnPagesChanged()
    {
        if (ActivePageIndex >= Pages.Count)
            ActivePageIndex = Math.Max(0, Pages.Count - 1);

        RehookPageEvents();

        if (UniformPageHeight && Pages.Count > 0)
            SyncPageHeights();

        UpdateActivePageWorldBounds();
    }

    #endregion === PAGES CHANGED ===

    #region === PAGE MANAGEMENT ===

    public BandPage AddNewPage()
    {
        EnsureDefaultIdentity();

        float baseHeight = Height > 0 ? Height : SavedExpandedHeight;
        if (baseHeight <= 0)
            baseHeight = 120;

        var page = new BandPage
        {
            Id = IdGenerator.NewID,
            ParentBand = this,
            Name = $"Page {Pages.Count + 1}",
            Title = $"Page {Pages.Count + 1}",
            Height = baseHeight,
        };

        Pages.Add(page);

        if (Pages.Count == 1)
            ActivePageIndex = 0;

        HookPageEvents(page);

        if (UniformPageHeight)
            SyncPageHeights();

        UpdateActivePageWorldBounds();
        return page;
    }

    public void AddPage(BandPage page)
    {
        if (page == null)
            return;

        EnsureDefaultIdentity();

        page.Height = UniformPageHeight ? Height : (ActivePage?.Height ?? Height);

        Pages.Add(page);

        if (Pages.Count == 1)
            ActivePageIndex = 0;

        HookPageEvents(page);

        if (UniformPageHeight)
            SyncPageHeights();

        UpdateActivePageWorldBounds();
    }

    public bool RemovePageAt(int index)
    {
        if (index < 0 || index >= Pages.Count)
            return false;

        Pages.RemoveAt(index);

        EnsureInitialPage();
        UpdateActivePageWorldBounds();
        return true;
    }

    public void EnsureAtLeastOnePage() => EnsureInitialPage();

    public void SyncPageHeights()
    {
        if (!UniformPageHeight || Pages.Count == 0)
            return;

        bool usesAutoFillHeight = UsesScreenAutoFillHeight();
        float target = usesAutoFillHeight
            ? Height
            : ActivePage != null ? ActivePage.Height : Pages[0].Height;

        foreach (var p in Pages)
            p.Height = target;

        if (ActivePage != null && !FloatEquals(ActivePage.Height, target))
            ActivePage.Height = target;

        if (!usesAutoFillHeight && !FloatEquals(Height, target))
            Height = target;

        if (!usesAutoFillHeight)
            SavedExpandedHeight = target;

        UpdateActivePageWorldBounds();
    }

    public void SetActivePage(int index)
    {
        if (index < 0 || index >= Pages.Count)
            return;

        ActivePageIndex = index;

        if (ActivePage != null && !UsesScreenAutoFillHeight() && !FloatEquals(Height, ActivePage.Height))
            Height = ActivePage.Height;

        if (UniformPageHeight)
            SyncPageHeights();

        UpdateActivePageWorldBounds();
    }

    #endregion === PAGE MANAGEMENT ===

    #region === SWIPE DETECTION ===

    private SKPoint? _swipeStartPoint;
    private DateTime _swipeStartTime;

    public void OnPointerDown(SKPoint viewPoint)
    {
        _swipeStartPoint = viewPoint;
        _swipeStartTime = DateTime.Now;
    }

    public void OnPointerUp(SKPoint viewPoint)
    {
        if (_swipeStartPoint is null)
            return;

        var start = _swipeStartPoint.Value;
        var delta = viewPoint - start;
        var duration = (DateTime.Now - _swipeStartTime).TotalMilliseconds;

        const float SWIPE_MIN_DIST = 80f;
        const float SWIPE_MAX_OFF = 50f;
        const int SWIPE_MAX_TIME = 600;

        if (
            Math.Abs(delta.X) > SWIPE_MIN_DIST
            && Math.Abs(delta.Y) < SWIPE_MAX_OFF
            && duration < SWIPE_MAX_TIME
        )
        {
            if (delta.X < 0)
                NextPage();
            else
                PreviousPage();
        }

        _swipeStartPoint = null;
    }

    public void NextPage()
    {
        if (Pages.Count <= 1)
            return;

        int ni = ActivePageIndex + 1;
        if (ni >= Pages.Count)
            ni = RotatePages ? 0 : Pages.Count - 1;

        SetActivePage(ni);
    }

    public void PreviousPage()
    {
        if (Pages.Count <= 1)
            return;

        int ni = ActivePageIndex - 1;
        if (ni < 0)
            ni = RotatePages ? Pages.Count - 1 : 0;

        SetActivePage(ni);
    }

    #endregion === SWIPE DETECTION ===

    #region === AUTO RESIZE BAND FOR CONTROLS ===

    public void EnsureRequiredHeightForControls(bool allowAutoLayout, float margin = 6f)
    {
        if (!allowAutoLayout)
            return;

        var page = ActivePage;
        if (page == null)
            return;

        float required =
            page.Controls.Count == 0
                ? page.Height
                : page.Controls.Max(c => c.Y + c.Height) + margin;

        if (required < Screen.DefaultBandHeaderHeight)
            required = Screen.DefaultBandHeaderHeight;

        required = Math.Max(required, page.Height);

        page.Height = required;
        SavedExpandedHeight = required;
        Height = required;

        UpdateActivePageWorldBounds();
    }

    #endregion === AUTO RESIZE BAND FOR CONTROLS ===

    #region === EVENT HOOKS ===

    private void HookPageEvents(BandPage page)
    {
        page.Controls.CollectionChanged += (_, e) =>
        {
            if (e.NewItems != null)
                foreach (DesignControl c in e.NewItems)
                {
                    c.ParentBandPage = page;
                    c.ParentBand = this;
                    c.PropertyChanged += OnControlPropertyChanged;
                }

            if (e.OldItems != null)
                foreach (DesignControl c in e.OldItems)
                    c.PropertyChanged -= OnControlPropertyChanged;
        };

        foreach (var c in page.Controls)
            c.PropertyChanged += OnControlPropertyChanged;
    }

    private void OnControlPropertyChanged(object? sender, PropertyChangedEventArgs e) { }

    private void RehookPageEvents()
    {
        foreach (var p in Pages)
            HookPageEvents(p);
    }

    #endregion === EVENT HOOKS ===

    #region === WORLD BOUNDS ===

    [JsonIgnore]
    public float WorldX { get; private set; }

    [JsonIgnore]
    public float WorldY { get; private set; }

    [JsonIgnore]
    public SKRect WorldBounds { get; private set; }

    public void UpdateBandWorldBounds(float worldX, float worldY)
    {
        WorldX = worldX;
        WorldY = worldY;

        WorldBounds = new SKRect(WorldX, WorldY, WorldX + Width, WorldY + EffectiveHeight);
        UpdateActivePageWorldBounds();
    }

    #endregion === WORLD BOUNDS ===

    #region === RENDERING ===

    public void RenderBackground(SKCanvas canvas, RenderContext ctx)
    {
        //Render background only if color is not transparent. 
        //XXXif (BandBackground != Colors.Transparent || BandBackground != Colors.White)
        if (BandBackground != Colors.Transparent && BandBackground != Colors.White)
        {
            backgroundPaint.Color = BandBackground.ToSKColor();
            SKRect rect = SKRect.Create(
                WorldBounds.Left,
                WorldBounds.Top,
                WorldBounds.Width,
                WorldBounds.Height
            );
            canvas.DrawRect(rect, backgroundPaint);
        }

        //Render header only if background is transparent
        if (BandBackground == Colors.Transparent || BandBackground == Colors.White)
        {
            headerPaint.ImageFilter = null;

            switch (BandType)
            {
                case BandType.Custom:
                    headerPaint.Color = SKColor.Parse("#F3F4F6");
                    break;
                case BandType.Header:
                    headerPaint.Color = SKColor.Parse("#FFFFFF");
                    if (ctx.LiveMode)
                    {
                        headerPaint.ImageFilter = dropShadow;
                    }
                    break;
                case BandType.Footer:
                    headerPaint.Color = SKColor.Parse("#353639");
                    break;
            }

            if (!ctx.LiveMode && ctx.ShowBandBorders)
            {
                if (BandType == BandType.Footer)
                {
                    canvas.DrawLine(
                        WorldBounds.Left,
                        WorldBounds.Top,
                        WorldBounds.Right,
                        WorldBounds.Top,
                        borderPaint
                    );
                }
                else
                {
                    canvas.DrawLine(
                        WorldBounds.Left,
                        WorldBounds.Bottom - 0.5f,
                        WorldBounds.Right,
                        WorldBounds.Bottom - 0.5f,
                        borderPaint
                    );
                }
            }

            if (HasVisibleHeader)
                RenderHeader(canvas, ctx);
        }

        if (ShowsTabs)
            RenderTabs(canvas, ctx);

        if (this == ctx.SelectedBand)
            RenderSelectionFrameAndResizeThumb(canvas, ctx);

        if (ShowsToggle)
            RenderToggleButton(canvas, ctx);
    }

    public void RenderControls(SKCanvas canvas, RenderContext ctx)
    {
        var page = ActivePage;
        if (page == null)
            return;

        if (IsExpandable && !IsExpanded)
            return;

        var content = page.WorldBounds;

        ctx.SelectedPage = page;
        ctx.PageWorldBounds = page.WorldBounds;

        foreach (var ctrl in page.Controls.Where(c => c is not ActionArea).OrderBy(c => c.ZIndex))
        {
            ctrl.VisualRect = new SKRect(
                content.Left + ctrl.X,
                content.Top + ctrl.Y,
                content.Left + ctrl.X + ctrl.Width,
                content.Top + ctrl.Y + ctrl.Height
            );

            if (ctrl.VisualRect.Bottom <= 0)
                continue;

            ctrl.Render(canvas, ctrl.VisualRect, ctx);
        }

        foreach (var area in page.Controls.OfType<ActionArea>().OrderBy(c => c.ZIndex))
        {
            area.VisualRect = new SKRect(
                content.Left + area.X,
                content.Top + area.Y,
                content.Left + area.X + area.Width,
                content.Top + area.Y + area.Height
            );

            if (ctx.LiveMode && !ctx.ShowActionAreas)
                continue;

            if (area.VisualRect.Bottom <= 0)
                continue;

            area.Render(canvas, area.VisualRect, ctx);
            area.RenderActionCircle(canvas, area.VisualRect, ctx);
        }
    }

    internal void RenderHeader(SKCanvas canvas, RenderContext ctx)
    {
        SKRect rect = SKRect.Empty;

        if (BandType == BandType.Custom)
        {
            headerPaint.Color = SKColor.Parse("F5F6F8");
            rect = SKRect.Create(
                WorldBounds.Left,
                WorldBounds.Top,
                WorldBounds.Width,
                HeaderHeight
            );
            canvas.DrawRect(rect, headerPaint);
        }
        else if (BandType == BandType.Header)
        {
            headerPaint.Color = HeaderBackground.ToSKColor();
            rect = SKRect.Create(
                WorldBounds.Left,
                WorldBounds.Top,
                WorldBounds.Width,
                WorldBounds.Bottom
            );
            canvas.DrawRect(rect, headerPaint);
        }
        else if (BandType == BandType.Footer)
        {
            footerPaint.Color = FooterBackground.ToSKColor();
            rect = SKRect.Create(
                WorldBounds.Left,
                WorldBounds.Top,
                WorldBounds.Width,
                WorldBounds.Bottom
            );
            canvas.DrawRect(rect, footerPaint);
        }


        if (ShowsTitle)
            RenderTitle(canvas, ctx);

        if (BandType == BandType.Custom || BandRect.Height == Screen.DefaultBandHeaderHeight)
        {
            canvas.DrawLine(
                WorldBounds.Left,
                WorldBounds.Top + HeaderHeight,
                WorldBounds.Right,
                WorldBounds.Top + HeaderHeight,
                borderPaint
            );
        }
    }

    private void RenderToggleButton(SKCanvas canvas, RenderContext ctx)
    {
        if (!ShowsToggle)
        {
            ToggleButtonRect = SKRect.Empty;
            ToggleButtonInteractiveRect = SKRect.Empty;
            return;
        }

        ToggleButtonRect = CalculatedToggleRect;
        ToggleButtonInteractiveRect = CalculatedToggleInteractiveRect;

        if (ToggleButtonRect.IsEmpty)
            return;

        float triangleHeight = 6f;

        using var path = new SKPath();
        float topOffset = (ToggleButtonRect.Height - triangleHeight) / 2f;

        if (IsExpanded)
        {
            path.MoveTo(ToggleButtonRect.Left, ToggleButtonRect.Bottom - topOffset - 2);
            path.LineTo(ToggleButtonRect.Right, ToggleButtonRect.Bottom - topOffset - 2);
            path.LineTo(ToggleButtonRect.MidX, ToggleButtonRect.Top + topOffset - 2);
        }
        else
        {
            path.MoveTo(ToggleButtonRect.Left, ToggleButtonRect.Top + topOffset);
            path.LineTo(ToggleButtonRect.Right, ToggleButtonRect.Top + topOffset);
            path.LineTo(ToggleButtonRect.MidX, ToggleButtonRect.Bottom - topOffset);
        }

        path.Close();
        canvas.DrawPath(path, toggleButtonPaint);
    }

    private void RenderTitle(SKCanvas canvas, RenderContext ctx)
    {
        var rect = HeaderRect;
        if (rect.IsEmpty)
            return;

        SKRect rectText = SKRect.Create(rect.Left + 6, rect.Top, rect.Width, rect.Height);

        SKColor textColor = SKColors.Black;

        TextRenderer.Draw(
            canvas,
            Title,
            rectText,
            15,
            color: textColor,
            textAlignment: TextAlignment.Left
        );
    }

    public readonly List<(SKRect rect, int pageIndex)> _tabRects = new();

    private void RenderTabs(SKCanvas canvas, RenderContext ctx)
    {
        if (!ShowsTabs)
            return;

        BuildTabRects();
        ClampTabScroll();

        var clip = TabsClipRect;
        if (clip.IsEmpty)
            return;

        canvas.Save();
        canvas.ClipRect(clip);

        for (int i = 0; i < TabRects.Count; i++)
        {
            var tabRect = TabRects[i];
            var textRect = SKRect.Create(tabRect.Left, tabRect.Top, tabRect.Width, tabRect.Height);

            var roundedRect = new SKRoundRect();

            var radii = new SKPoint[4];
            radii[0] = new SKPoint(3, 3);
            radii[1] = new SKPoint(5, 5);
            radii[2] = new SKPoint(0, 0);
            radii[3] = new SKPoint(0, 0);

            roundedRect.SetRectRadii(tabRect, radii);
            roundedRect.Inflate(0, 1);

            if (i == ActivePageIndex)
            {
                canvas.DrawRoundRect(roundedRect, tabPaint);
                canvas.DrawRoundRect(roundedRect, tabBorderPaint);
            }

            TextRenderer.Draw(
                canvas,
                text: Pages[i].Title ?? Pages[i].Name,
                bounds: textRect,
                fontSize: 14,
                color: SKColors.Black,
                textAlignment: TextAlignment.Center
            );
        }

        canvas.Restore();
    }

    private void RenderTabArrows(SKCanvas canvas, RenderContext ctx)
    {
        if (!IsMouseOverBand || Pages.Count <= 1)
        {
            LeftArrowRect = SKRect.Empty;
            RightArrowRect = SKRect.Empty;
            return;
        }

        float size = 16f;
        float y = HeaderRect.IsEmpty ? WorldBounds.Top : HeaderRect.Bottom - size - 4f;

        if (IsMouseNearLeft && ActivePageIndex > 0)
        {
            LeftArrowRect = SKRect.Create(WorldBounds.Left + 4, y, size, size);

            using (var path = new SKPath())
            {
                float midY = LeftArrowRect.MidY;

                path.MoveTo(LeftArrowRect.Left + 3, midY);
                path.LineTo(LeftArrowRect.Right - 2, LeftArrowRect.Top + 2);
                path.LineTo(LeftArrowRect.Right - 2, LeftArrowRect.Bottom - 2);
                path.Close();
                canvas.DrawPath(path, pageArrowPaint);
            }
        }
        else
        {
            LeftArrowRect = SKRect.Empty;
        }

        if (IsMouseNearRight && ActivePageIndex < Pages.Count - 1)
        {
            RightArrowRect = SKRect.Create(WorldBounds.Right - size - 6, y, size, size);

            using (var path = new SKPath())
            {
                float midY = RightArrowRect.MidY;

                path.MoveTo(RightArrowRect.Right - 3, midY);
                path.LineTo(RightArrowRect.Left + 2, RightArrowRect.Top + 2);
                path.LineTo(RightArrowRect.Left + 2, RightArrowRect.Bottom - 2);
                path.Close();
                canvas.DrawPath(path, pageArrowPaint);
            }
        }
        else
        {
            RightArrowRect = SKRect.Empty;
        }
    }

    private void RenderSelectionFrameAndResizeThumb(SKCanvas canvas, RenderContext ctx)
    {
        SKRect rect = SKRect.Empty;

        rect = SKRect.Create(
            WorldBounds.Left,
            WorldBounds.Top,
            WorldBounds.Width,
            WorldBounds.Height
        );

        rect.Inflate(-1f, -1f);

        canvas.DrawRect(rect, selectionPaint);

        if (UsesScreenAutoFillHeight())
        {
            ResizeThumbRect = SKRect.Empty;
            return;
        }

        if (IsExpandable && !IsExpanded)
        {
            ResizeThumbRect = SKRect.Empty;
            return;
        }

        float triWidth = 10f;
        float triHeight = 7f;
        float spacing = 1f;

        float centerX = WorldBounds.MidX;
        float trianglesLeft = centerX - triWidth / 2f;

        if (BandType == BandType.Footer)
        {
            float referenceY = WorldBounds.Top;
            float upBottomY = referenceY;

            using (var pathUp = new SKPath())
            {
                pathUp.MoveTo(trianglesLeft, upBottomY);
                pathUp.LineTo(trianglesLeft + triWidth, upBottomY);
                pathUp.LineTo(trianglesLeft + triWidth / 2f, upBottomY + triHeight);
                pathUp.Close();
                canvas.DrawPath(pathUp, thumbPaint);
            }

            ResizeThumbRect = CalculatedResizeRect;
        }
        else
        {
            float referenceY = WorldBounds.Bottom;
            float upBottomY = referenceY - spacing;

            using (var pathUp = new SKPath())
            {
                pathUp.MoveTo(trianglesLeft, upBottomY);
                pathUp.LineTo(trianglesLeft + triWidth, upBottomY);
                pathUp.LineTo(trianglesLeft + triWidth / 2f, upBottomY - triHeight);
                pathUp.Close();
                canvas.DrawPath(pathUp, thumbPaint);
            }

            ResizeThumbRect = CalculatedResizeRect;
        }
    }

    #endregion === RENDERING ===

    #region === HELPER ===

    private static bool FloatEquals(float a, float b) => Math.Abs(a - b) < 0.0001f;

    public Band CloneShallow()
    {
        return new Band
        {
            Id = this.Id,
            BandType = this.BandType,
            BandBackground = this.BandBackground,
            HeaderBackground = this.HeaderBackground,
            FooterBackground = this.FooterBackground,
            Title = this.Title,
            IsExpandable = this.IsExpandable,
            IsExpanded = this.IsExpanded,
            SavedExpandedHeight = this.SavedExpandedHeight,
            Height = this.Height,
            UniformPageHeight = this.UniformPageHeight,
            ShowTabs = this.ShowTabs,
            ActivePageIndex = this.ActivePageIndex,
            Name = this.Name,
        };
    }

    public override Band DeepClone()
    {
        int desiredActiveIndex = ActivePageIndex;
        bool sourceUsesAutoFillHeight = UsesScreenAutoFillHeight();
        float sourceSavedExpandedHeight = SavedExpandedHeight;

        var clone = new Band
        {
            Id = IdGenerator.NewID,

            IsSticky = IsSticky,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,

            MinWidth = MinWidth,
            MinHeight = MinHeight,
            MaxWidth = MaxWidth,
            MaxHeight = MaxHeight,

            ZIndex = ZIndex,
            ResizeStyle = ResizeStyle,
            Title = Title,
            BandBackground = BandBackground,
            HeaderBackground = HeaderBackground,
            FooterBackground = FooterBackground,
            IsExpandable = IsExpandable,
            IsExpanded = IsExpanded,
            SavedExpandedHeight = SavedExpandedHeight,
            UniformPageHeight = UniformPageHeight,
            BandType = BandType,
            ShowTabs = ShowTabs,
            Name = Name,
        };

        clone.Pages.Clear();

        foreach (var page in Pages)
        {
            var newPage = page.DeepClone();
            newPage.ParentBand = clone;

            foreach (var ctrl in newPage.Controls)
            {
                ctrl.ParentBand = clone;
                ctrl.ParentBandPage = newPage;
            }

            clone.Pages.Add(newPage);
        }

        if (clone.Pages.Count > 0)
        {
            clone.ActivePageIndex = Math.Clamp(desiredActiveIndex, 0, clone.Pages.Count - 1);

            if (clone.ActivePage != null)
            {
                float resolvedHeight = clone.Height > 0 ? clone.Height : clone.ActivePage.Height;

                if (!FloatEquals(clone.Height, resolvedHeight))
                    clone.Height = resolvedHeight;

                if (!FloatEquals(clone.ActivePage.Height, resolvedHeight))
                    clone.ActivePage.Height = resolvedHeight;

                if (!sourceUsesAutoFillHeight)
                    clone.SavedExpandedHeight = resolvedHeight;
            }

            if (clone.UniformPageHeight)
                clone.SyncPageHeights();

            if (sourceUsesAutoFillHeight)
                clone.SavedExpandedHeight = sourceSavedExpandedHeight;

            clone.UpdateActivePageWorldBounds();
        }

        return clone;
    }

    #endregion === HELPER ===
}
