using CommunityToolkit.Mvvm.ComponentModel;
using Mockup.ColorSystem;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Mockup.Dialogs;


[ObservableObject]
public partial class ProjectDialog : ModalDialogWindow
{
    public IReadOnlyList<ColorSchema> ColorSchemas { get; } = ThemeService.GetAllSchemas().ToList();

    private void PART_ScreenSizes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is Project project)
        {
            if (PART_ScreenSizes.SelectedItem is ScreenSize screenSize)
            {
                project.DeviceWidth = screenSize.Width;
                project.DeviceHeight = screenSize.Height;

            }
        }
    }



    //public ProjectDialog(object clone)
    //{
    //    InitializeComponent();

    //    DataContext = clone;

    //    PART_ScreenSizes.ItemsSource = new ScreenSizeCollection();
    //}

    public ProjectDialog()
    {
        InitializeComponent();

        PART_ScreenSizes.ItemsSource = new ScreenSizeCollection();
    }


    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

}


#region DEVICES

public class ScreenSize
{
    public string DeviceName { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string DisplayText => $"{DeviceName} ({Width} × {Height})";
    public double AspectRatio => Math.Round((double)Width / Height, 2);
    public bool IsMostCommon { get; set; }
}

public class ScreenSizeCollection : ObservableCollection<ScreenSize>
{
    public ScreenSizeCollection()
    {
        // Logical design dimensions (points / viewport), not physical display pixels.
        // Named profiles make target-device selection understandable in a design workflow.
        AddMostCommon(new ScreenSize { DeviceName = "iPhone 17 Pro", Width = 402, Height = 874 });
        AddMostCommon(new ScreenSize { DeviceName = "iPhone 17 Pro Max", Width = 440, Height = 956 });
        AddMostCommon(new ScreenSize { DeviceName = "iPhone 16", Width = 393, Height = 852 });
        AddMostCommon(new ScreenSize { DeviceName = "Samsung Galaxy S25", Width = 360, Height = 780 });
        AddMostCommon(new ScreenSize { DeviceName = "Google Pixel 9", Width = 412, Height = 915 });

        // Apple phones
        Add(new ScreenSize { DeviceName = "iPhone 16 Pro", Width = 402, Height = 874 });
        Add(new ScreenSize { DeviceName = "iPhone 16 Pro Max", Width = 440, Height = 956 });
        Add(new ScreenSize { DeviceName = "iPhone 15 / 15 Pro", Width = 393, Height = 852 });
        Add(new ScreenSize { DeviceName = "iPhone SE", Width = 375, Height = 667 });

        // Android phones
        Add(new ScreenSize { DeviceName = "Samsung Galaxy S25+", Width = 384, Height = 854 });
        Add(new ScreenSize { DeviceName = "Samsung Galaxy S25 Ultra", Width = 412, Height = 892 });
        Add(new ScreenSize { DeviceName = "Google Pixel 9 Pro", Width = 412, Height = 915 });
        Add(new ScreenSize { DeviceName = "Google Pixel 9 Pro XL", Width = 480, Height = 1000 });

        // Tablets and desktop canvases
        Add(new ScreenSize { DeviceName = "iPad mini", Width = 744, Height = 1133 });
        Add(new ScreenSize { DeviceName = "iPad Air 11\"", Width = 820, Height = 1180 });
        Add(new ScreenSize { DeviceName = "iPad Pro 13\"", Width = 1032, Height = 1376 });
        Add(new ScreenSize { DeviceName = "Desktop HD", Width = 1280, Height = 720 });
        Add(new ScreenSize { DeviceName = "Desktop Full HD", Width = 1920, Height = 1080 });
        Add(new ScreenSize { DeviceName = "Desktop QHD", Width = 2560, Height = 1440 });

        // Sortiere die Liste nach MostCommon und Gerätename
        var sortedItems = this.OrderBy(x => x.IsMostCommon ? 0 : 1)
                             .ThenBy(x => x.DeviceName)
                             .ToList();

        Clear();
        foreach (var item in sortedItems)
        {
            Add(item);
        }
    }

    private void AddMostCommon(ScreenSize screenSize)
    {
        screenSize.IsMostCommon = true;
        Add(screenSize);
    }
}

public class BoolToFontWeightConverter : IValueConverter
{
    public static BoolToFontWeightConverter Instance { get; } = new BoolToFontWeightConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value is bool boolValue && boolValue) ? FontWeights.Bold : FontWeights.Medium;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

// Converter für die Sichtbarkeit von Custom-Text
public class ZeroToVisibilityConverter : IValueConverter
{
    public static ZeroToVisibilityConverter Instance { get; } = new ZeroToVisibilityConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            bool invert = parameter as string == "invert";
            bool isZero = intValue == 0;

            if (invert)
                return isZero ? Visibility.Visible : Visibility.Collapsed;
            else
                return isZero ? Visibility.Collapsed : Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

#endregion

