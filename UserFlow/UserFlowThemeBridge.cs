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
        ("BorderBrush", XBrushKeys.BorderSubtle),
        ("DarkBorderBrush", XBrushKeys.BorderSubtle),
        ("PanelBorder", XBrushKeys.BorderSubtle),
        ("SubHeaderBrush", XBrushKeys.SurfaceLight),
        ("DesignerBackgroundBrush", XBrushKeys.SurfaceRaised),
        ("TabHeaderBrush", XBrushKeys.NavigationPanelBackground),
        ("SearchBoxBrush", XBrushKeys.SurfaceLight),
        ("ListBoxBrush", XBrushKeys.Background),
        ("ToolBarElemBorderBrush", XBrushKeys.BorderSubtle),
        ("ToolBarElemBGBrush", XBrushKeys.SurfaceLight),
        ("NoDataBackground", XBrushKeys.Background),
        ("PrimaryBrush", XBrushKeys.Primary),
        ("DarkPrimaryBrush", XBrushKeys.PrimaryDark),
        ("LightPrimaryBrush", XBrushKeys.PrimaryLight),
        ("DangerBrush", XBrushKeys.Danger),
        ("LightDangerBrush", XBrushKeys.DangerLight),
        ("SelectionBrush", XBrushKeys.SelectionBackground),
        ("SelectionBorder", XBrushKeys.SelectionBorder),
        ("TextPrimaryBrush", XBrushKeys.TextPrimary),
        ("TextSecondaryBrush", XBrushKeys.TextSecondary),
        ("TextMutedBrush", XBrushKeys.TextTertiary),
        ("HeaderBackgroundBrush", XBrushKeys.Surface),
        ("SubHeaderBackgroundBrush", XBrushKeys.SurfaceDark),
        ("ExpanderHeaderBrush", XBrushKeys.SurfaceLight),
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
            if (targetKey is "BorderBrush" or "DarkBorderBrush" or "ToolBarElemBorderBrush")
            {
                application.Resources[targetKey] = new SolidColorBrush(
                    theme.PanelBorder.GetColor(XThemeManager.Current.CurrentMode));
                continue;
            }

            if (application.TryFindResource(sourceKey) is SolidColorBrush source)
            {
                application.Resources[targetKey] = new SolidColorBrush(source.Color);
            }
        }
    }
}
