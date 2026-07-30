using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using VIA.WPF.Themes;

namespace Mockup.Views;

/// <summary>
/// Interaction logic for OptionsView.xaml
/// </summary>
public partial class OptionsView : UserControl
{
    private bool isSynchronizingThemeMode;
    private bool isThemeManagerSubscribed;

    public OptionsView()
    {
        InitializeComponent();

        Loaded += OptionsView_Loaded;
        Unloaded += OptionsView_Unloaded;
    }

    private void OptionsView_Loaded(object sender, RoutedEventArgs e)
    {
        SynchronizeThemeMode();

        if (isThemeManagerSubscribed)
            return;

        XThemeManager.Current.PropertyChanged += ThemeManager_PropertyChanged;
        isThemeManagerSubscribed = true;
    }

    private void OptionsView_Unloaded(object sender, RoutedEventArgs e)
    {
        if (!isThemeManagerSubscribed)
            return;

        XThemeManager.Current.PropertyChanged -= ThemeManager_PropertyChanged;
        isThemeManagerSubscribed = false;
    }

    private void ThemeManager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(XThemeManager.CurrentMode))
            return;

        if (Dispatcher.CheckAccess())
            SynchronizeThemeMode();
        else
            Dispatcher.Invoke(SynchronizeThemeMode);
    }

    private void SynchronizeThemeMode()
    {
        isSynchronizingThemeMode = true;
        PART_DarkModeToggle.IsChecked = XThemeManager.Current.CurrentMode == XThemeMode.Dark;
        isSynchronizingThemeMode = false;
    }

    private void DarkModeToggle_Checked(object sender, RoutedEventArgs e)
    {
        if (!isSynchronizingThemeMode)
            XThemeManager.Current.SetMode(XThemeMode.Dark);
    }

    private void DarkModeToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        if (!isSynchronizingThemeMode)
            XThemeManager.Current.SetMode(XThemeMode.Light);
    }
}
