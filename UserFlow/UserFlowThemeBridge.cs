using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using VIA.WPF.Themes;

namespace UserFlow;

/// <summary>
/// Keeps legacy UserFlow resource names synchronized with VIA.WPF theme tokens.
/// </summary>
internal static class UserFlowThemeBridge
{
    private static readonly (string Target, ComponentResourceKey Source)[] Mappings =
    [
        ("AppShellBrush", XBrushKeys.NavigationPanelBackground),
        ("AppShellAltBrush", XBrushKeys.NavigationPanelItemHoverBackground),
        ("AppShellBorderBrush", XBrushKeys.NavigationPanelBorder),
        ("AppStatusBarBrush", XBrushKeys.NavigationPanelBackground),
        ("AppStatusBarItemBrush", XBrushKeys.NavigationPanelItemHoverBackground),
        ("BorderBrush", XBrushKeys.BorderSubtle),
        ("DarkBorderBrush", XBrushKeys.BorderDefault),
        ("PanelBorder", XBrushKeys.BorderSubtle),
        ("SubHeaderBrush", XBrushKeys.SurfaceSunken),
        ("DesignerBackgroundBrush", XBrushKeys.SurfaceRaised),
        ("TabHeaderBrush", XBrushKeys.NavigationPanelBackground),
        ("SearchBoxBrush", XBrushKeys.InputBackground),
        ("ListBoxBrush", XBrushKeys.Surface),
        ("ToolBarElemBorderBrush", XBrushKeys.BorderSubtle),
        ("ToolBarElemBGBrush", XBrushKeys.SurfaceSunken),
        ("NoDataBackground", XBrushKeys.SurfaceSunken),
        ("DesignAreaBackground", XBrushKeys.ScrimSubtle),
        ("PrimaryBrush", XBrushKeys.Primary),
        ("DarkPrimaryBrush", XBrushKeys.PrimaryStrong),
        ("LightPrimaryBrush", XBrushKeys.PrimarySubtleHover),
        ("DangerBrush", XBrushKeys.StatusDanger),
        ("LightDangerBrush", XBrushKeys.StatusDangerSubtle),
        ("SelectionBrush", XBrushKeys.SelectionBackground),
        ("SelectionBorder", XBrushKeys.SelectionBorder),
        ("TextPrimaryBrush", XBrushKeys.TextPrimary),
        ("TextSecondaryBrush", XBrushKeys.TextSecondary),
        ("TextMutedBrush", XBrushKeys.TextTertiary),
        ("HeaderBackgroundBrush", XBrushKeys.Surface),
        ("SubHeaderBackgroundBrush", XBrushKeys.SurfaceSunken),
        ("ExpanderHeaderBrush", XBrushKeys.SurfaceSunken),
    ];

    public static void Initialize()
    {
        XThemeManager.Current.PropertyChanged += ThemeManager_PropertyChanged;
        Apply();
    }

    private static void ThemeManager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(XThemeManager.CurrentTheme) or nameof(XThemeManager.CurrentMode))
        {
            Application? application = Application.Current;
            if (application is null)
            {
                return;
            }

            application.Dispatcher.BeginInvoke(Apply, DispatcherPriority.ApplicationIdle);
        }
    }

    private static void Apply()
    {
        Application application = Application.Current;

        XTheme? theme = XThemeManager.Current.CurrentTheme;
        if (theme is null)
        {
            return;
        }

        foreach ((string targetKey, ComponentResourceKey sourceKey) in Mappings)
        {
            if (application.TryFindResource(sourceKey) is SolidColorBrush source)
            {
                application.Resources[targetKey] = new SolidColorBrush(source.Color);
            }
        }
    }
}
