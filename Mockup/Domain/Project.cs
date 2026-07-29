// ======================================================================================
// FILE: Mockup/Project.cs
//
// ZWECK:
// Repräsentiert ein Mockup-Projekt mit:
// - Projekt-Metadaten
// - ColorScheme
// - Device-Konfiguration
// - Screens
// - Popups (projektlokal)
//
// VERSION: 3.1 – Device-basiertes Screen-Modell
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using Mockup.Messages;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Mockup;

public sealed partial class Project : ObservableObject
{
    #region === PROJECT IDENTITY & METADATA ===

    [ObservableProperty]
    [property: System.ComponentModel.Category("Identity")]
    [property: System.ComponentModel.DisplayName("Id")]
    [property: System.ComponentModel.Browsable(false)]
    private long id;

    [ObservableProperty]
    [property: System.ComponentModel.Browsable(true)]
    [property: System.ComponentModel.Category("Project")]
    [property: System.ComponentModel.DisplayName("Name")]
    private string name = "Project";

    [ObservableProperty]
    [property: System.ComponentModel.Browsable(true)]
    [property: System.ComponentModel.Category("Project")]
    private string? description = string.Empty;

    #endregion

    #region === FILE PATH (NOT SERIALIZED) ===

    [JsonIgnore]
    [ObservableProperty]
    [property: System.ComponentModel.Browsable(false)]
    private string filePath = string.Empty;

    [ObservableProperty]
    public DateTime lastOpenedUtc = DateTime.UtcNow;

    #endregion

    #region === SHARING / PERMISSIONS ===

    #region === DESIGNER GUIDES ===

    [ObservableProperty]
    private bool showAlignmentGuidelines = true;

    partial void OnShowAlignmentGuidelinesChanged(bool value) => MSG.UI.InvalidateDesigner();

    [ObservableProperty]
    private bool showDesignerInteractionHints;

    partial void OnShowDesignerInteractionHintsChanged(bool value) => MSG.UI.InvalidateDesigner();

    #endregion === DESIGNER GUIDES ===

    [ObservableProperty]
    [property: System.ComponentModel.Browsable(false)]
    private bool isShared;

    [ObservableProperty]
    [property: System.ComponentModel.Browsable(false)]
    private bool isSharedReadonly;

    #endregion

    #region === COLOR SYSTEM ===

    [ObservableProperty]
    [property: System.ComponentModel.Browsable(true)]
    [property: System.ComponentModel.Category("Theme")]
    [property: System.ComponentModel.DisplayName("Color Scheme")]
    private string colorSchemaKey = "Default";

    partial void OnColorSchemaKeyChanged(string value)
    {
        ThemeService.SetSchema(value);
        ActiveColorSchema = ThemeService.Current.Clone();
        MSG.UI.InvalidateDesigner();
    }

    [JsonIgnore]
    [ObservableProperty]
    [property: System.ComponentModel.Browsable(false)]
    private ColorSchema activeColorSchema = ColorSchema.CreateDefault();

    #endregion

    #region === DEVICE CONFIGURATION ===

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DeviceInfo))]
    [property: System.ComponentModel.Browsable(true)]
    [property: System.ComponentModel.Category("Device")]
    [property: System.ComponentModel.DisplayName("Device Width")]
    private float deviceWidth = 410;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DeviceInfo))]
    [property: System.ComponentModel.Browsable(true)]
    [property: System.ComponentModel.Category("Device")]
    [property: System.ComponentModel.DisplayName("Device Height")]
    private float deviceHeight = 590;

    partial void OnDeviceHeightChanged(float value)
    {
        //XXX 
        //foreach (Screen screen in Screens)
        //{
        //    screen.UserHeight = value;
        //}
    }

    [JsonIgnore]
    public string DeviceInfo => $"{DeviceWidth} x {DeviceHeight} px";

    #endregion

    #region === ZOOM (UI STATE) ===

    [ObservableProperty]
    [property: System.ComponentModel.Browsable(false)]
    private double screenZoomPercent = 80;

    [ObservableProperty]
    [property: System.ComponentModel.Browsable(false)]
    private double templateZoomPercent = 80;

    [ObservableProperty]
    [property: System.ComponentModel.Browsable(false)]
    private double popupZoomPercent = 80;

    [ObservableProperty]
    [property: System.ComponentModel.Browsable(false)]
    private double previewZoomPercent = 80;

    [ObservableProperty]
    [property: System.ComponentModel.Browsable(false)]
    private double projectZoomPercent = 80;

    #endregion

    #region === SCREENS COLLECTION ===

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasScreens))]
    [NotifyPropertyChangedFor(nameof(ScreenCountInfo))]
    [property: System.ComponentModel.Browsable(false)]
    private ObservableCollection<Screen> screens = [];

    [JsonIgnore]
    public bool HasScreens => Screens.Count > 0;

    [JsonIgnore]
    public string ScreenCountInfo =>
        Screens.Count switch
        {
            0 => "No Screens",
            1 => "1 Screen",
            _ => $"{Screens.Count} Screens",
        };

    #endregion

    #region === POPUPS COLLECTION ===

    [ObservableProperty]
    [property: System.ComponentModel.Browsable(false)]
    private ObservableCollection<ScreenPopup> popups = [];

    public ScreenPopup? GetPopupById(long id) =>
        Popups.FirstOrDefault(p => p.Id == id);

    #endregion

    #region === CONSTRUCTOR & INITIALIZATION ===

    public Project()
    {
        if (string.IsNullOrEmpty(ColorSchemaKey))
            ColorSchemaKey = "Default";
    }

    public void InitializeTheme()
    {
        ThemeService.SetSchema(ColorSchemaKey);

        ActiveColorSchema = ThemeService.Current.Clone();

        //if (!ThemeService.TrySetSchema(ColorSchemaKey, out var appliedSchema))
        //    ActiveColorSchema = appliedSchema ?? ThemeService.Current.Clone();
        //else
        //    ActiveColorSchema = ThemeService.Current.Clone();
    }

    #endregion

    #region === DEEP CLONE SUPPORT ===

    public Project DeepClone()
    {
        var clone = new Project
        {
            Id = IdGenerator.NewID,
            Name = this.Name + " – Copy",
            Description = this.Description,
            DeviceWidth = this.DeviceWidth,
            DeviceHeight = this.DeviceHeight,
            ShowAlignmentGuidelines = this.ShowAlignmentGuidelines,
            ShowDesignerInteractionHints = this.ShowDesignerInteractionHints,
            ScreenZoomPercent = this.ScreenZoomPercent,
            PreviewZoomPercent = this.PreviewZoomPercent,
            TemplateZoomPercent = this.TemplateZoomPercent,
            PopupZoomPercent = this.PopupZoomPercent,
            ProjectZoomPercent = this.ProjectZoomPercent,
        };

        foreach (var s in Screens)
        {
            Screen screen = s.DeepClone(clone);

            screen.Project = this;

            clone.Screens.Add(screen);
        }

        foreach (var p in Popups)
        {
            clone.Popups.Add(p.DeepClone());
        }

        return clone;
    }

    #endregion
}
