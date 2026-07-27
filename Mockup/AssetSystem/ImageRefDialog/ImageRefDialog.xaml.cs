// ======================================================================================
// FILE: Mockup/AssetSystem/ImageRefDialog/ImageRefDialog.xaml.cs
//
// PURPOSE:
//   Auswahl-Dialog für SVG/PNG-Assets mit Format-Umschaltung (SVG/PNG),
//   Suche/Filter, SkiaSharp-Preview und Dateiexport.
//   Vollständig MVVM-kompatibel (ObservableObject, RelayCommand).
//
//   Unterstützt: Add SVG/PNG → Assets\SVG / Assets\PNG
//
// AUTHOR: Claus Falkenstein / ChatGPT (XMOCKUP2 / MO27)
// VERSION: 3.1 (vollständig fehlerfrei, kompatibel mit XAML-Events)
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Mockup.Domain.Registry;
using Mockup.Messages;
using Mockup.Resources;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using VIA.WPF.Localization;

namespace Mockup.AssetSystem;

public partial class ImageRefDialog : Window
{
    public ImageRefDialogViewModel VM { get; }

    /// <summary>Gibt das ausgewählte Asset als ImageRef zurück.</summary>
    public ImageRef? SelectedImageRef =>
        VM.SelectedAsset == null
            ? null
            : new ImageRef(
                VM.SelectedAsset.Id,
                VM.SelectedAsset.Kind == AssetCatalog.AssetKind.Png
                    ? ImageFormat.Png
                    : ImageFormat.Svg);

    // ============================================================================
    // CTOR
    // ============================================================================
    public ImageRefDialog(ImageRef? existingRef = null)
    {
        VM = new ImageRefDialogViewModel();
        DataContext = VM;

        MSG.UI.ShowOverlay(true);

        InitializeComponent();

        // Format übernehmen
        if (existingRef != null)
        {
            VM.CurrentFormat = existingRef.Format == ImageFormat.Png
                ? ImageRefDialogViewModel.FormatKind.Png
                : ImageRefDialogViewModel.FormatKind.Svg;
        }

        // Auswahl auf bestehendes Asset setzen
        if (existingRef != null)
        {
            var match = AssetCatalog.AllAssets?
                .FirstOrDefault(a => a.Id == existingRef.Id &&
                    a.Kind == (existingRef.Format == ImageFormat.Png
                        ? AssetCatalog.AssetKind.Png
                        : AssetCatalog.AssetKind.Svg));
            if (match != null)
                VM.SelectedAsset = match;
        }
    }

    // ============================================================================
    // EVENT HANDLER – XAML referenziert
    // ============================================================================

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        if (sender is SKElement element &&
            element.DataContext is AssetCatalog.AssetInfo info)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var size = Math.Min(e.Info.Width, e.Info.Height);
            using var bitmap = ImageRenderer.RenderPreview(info, tint: null, targetSize: size);

            if (bitmap != null)
            {
                var x = (e.Info.Width - bitmap.Width) / 2f;
                var y = (e.Info.Height - bitmap.Height) / 2f;
                using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
                canvas.DrawBitmap(bitmap, x, y, paint);
            }
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SKElement_MouseDown(object sender, MouseButtonEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Root_Closed(object sender, EventArgs e)
        => MSG.UI.ShowOverlay(false);
}

// ============================================================================
// VIEWMODEL
// ============================================================================

public partial class ImageRefDialogViewModel : ObservableObject
{
    public enum FormatKind { Svg, Png }

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string countDisplayText = string.Empty;
    [ObservableProperty] private string addButtonText = string.Empty;
    [ObservableProperty] private FormatKind currentFormat = FormatKind.Svg;
    [ObservableProperty] private bool noResultsVisible;
    [ObservableProperty] private AssetCatalog.AssetInfo? selectedAsset;

    public ObservableCollection<AssetCatalog.AssetInfo> FilteredAssets { get; } = new();

    public ICommand SwitchFormatCommand { get; }
    public ICommand SelectAssetCommand { get; }
    public ICommand AddAssetCommand { get; }

    public ImageRefDialogViewModel()
    {
        SwitchFormatCommand = new RelayCommand<string>(OnSwitchFormat);
        SelectAssetCommand = new RelayCommand<AssetCatalog.AssetInfo>(OnSelectAsset);
        AddAssetCommand = new RelayCommand(OnAddAsset);
        CountDisplayText = DialogText("Common.Loading", "Loading...");
        UpdateAddButtonText();
        ApplyFilter();
    }

    private static string DialogText(string key, string fallbackText)
    {
        return XLocalizationService.Current.GetString(
            UserFlowResources.ResourceManager,
            key,
            fallbackText);
    }

    private static string DialogFormat(string key, string fallbackText, params object?[] arguments)
    {
        return XLocalizationService.Current.Format(
            UserFlowResources.ResourceManager,
            key,
            fallbackText,
            arguments);
    }

    private void UpdateAddButtonText()
    {
        AddButtonText = CurrentFormat == FormatKind.Svg
            ? DialogText("Dialog.AssetPicker.AddSvg", "Add SVG...")
            : DialogText("Dialog.AssetPicker.AddPng", "Add PNG...");
    }

    // ============================================================================
    // SWITCH / SEARCH
    // ============================================================================

    private void OnSwitchFormat(string? format)
    {
        CurrentFormat = format == "Png" ? FormatKind.Png : FormatKind.Svg;
        ApplyFilter();
        UpdateAddButtonText();
    }

    private void OnSelectAsset(AssetCatalog.AssetInfo? asset)
    {
        if (asset != null)
            SelectedAsset = asset;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnCurrentFormatChanged(FormatKind value) => ApplyFilter();

    // ============================================================================
    // FILTERING
    // ============================================================================

    public void ApplyFilter()
    {
        var all = AssetCatalog.AllAssets;
        if (all == null) return;

        var query = CurrentFormat == FormatKind.Svg
            ? all.Where(a => a.Kind == AssetCatalog.AssetKind.Svg)
            : all.Where(a => a.Kind == AssetCatalog.AssetKind.Png);

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(a => a.Id.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var results = query.OrderBy(a => a.Id).ToList();

        FilteredAssets.Clear();
        foreach (var asset in results)
            FilteredAssets.Add(asset);

        CountDisplayText = results.Count == 1
            ? DialogFormat("Dialog.AssetPicker.Count.One", "{0} icon", results.Count)
            : DialogFormat("Dialog.AssetPicker.Count.Many", "{0} icons", results.Count);
        NoResultsVisible = results.Count == 0;

        if (SelectedAsset != null && !results.Contains(SelectedAsset))
            SelectedAsset = null;
    }

    // ============================================================================
    // ADD COMMAND (Dateiimport)
    // ============================================================================

    private void OnAddAsset()
    {
        try
        {
            string filter = CurrentFormat == FormatKind.Svg
                ? DialogText("Dialog.AssetPicker.SvgFilesFilter", "SVG files (*.svg)|*.svg")
                : DialogText("Dialog.AssetPicker.PngFilesFilter", "PNG files (*.png)|*.png");

            var dlg = new OpenFileDialog
            {
                Title = CurrentFormat == FormatKind.Svg
                    ? DialogText("Dialog.AssetPicker.AddSvgTitle", "Add SVG Icon")
                    : DialogText("Dialog.AssetPicker.AddPngTitle", "Add PNG Icon"),
                Filter = filter,
                CheckFileExists = true,
                Multiselect = false
            };

            if (dlg.ShowDialog() != true)
                return;

            string baseDir = Path.Combine(AppContext.BaseDirectory, "Assets");
            string targetDir = Path.Combine(baseDir, CurrentFormat == FormatKind.Svg ? "SVG" : "PNG");
            Directory.CreateDirectory(targetDir);

            string fileName = Path.GetFileName(dlg.FileName);
            string destFile = Path.Combine(targetDir, fileName);

            File.Copy(dlg.FileName, destFile, true);

            // Re-Scan und Update
            AssetCatalog.Refresh();
            ApplyFilter();

            var newId = Path.GetFileNameWithoutExtension(fileName);
            SelectedAsset = FilteredAssets.FirstOrDefault(a => a.Id.Equals(newId, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ AddAsset failed: {ex}");
        }
    }
}

// ============================================================================
// Converter for selection highlighting
// ============================================================================
public class AssetSelectionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is AssetCatalog.AssetInfo asset && parameter is string param)
        {
            var window = Application.Current.Windows.OfType<ImageRefDialog>().FirstOrDefault();
            var isSelected = window?.VM.SelectedAsset == asset;

            return param switch
            {
                "Background" => isSelected ? new SolidColorBrush(Colors.LightBlue) : Brushes.Transparent,
                "BorderBrush" => isSelected ? new SolidColorBrush(Colors.RoyalBlue) : new SolidColorBrush(Colors.LightGray),
                "BorderThickness" => isSelected ? new Thickness(2) : new Thickness(1),
                _ => Brushes.Transparent
            };
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}



//using CommunityToolkit.Mvvm.ComponentModel;
//using CommunityToolkit.Mvvm.Input;
//using CommunityToolkit.Mvvm.Messaging;
//using Mockup.AssetSystem;
//using Mockup;
//using Mockup.Messages;
//using SkiaSharp;
//using SkiaSharp.Views.Desktop;
//using SkiaSharp.Views.WPF;
//using SVGImage.SVG;
//using System.Collections.ObjectModel;
//using System.Linq;
//using System.Windows;
//using System.Windows.Data;
//using System.Windows.Input;
//using System.Windows.Media;

//namespace Mockup.AssetSystem;

//public partial class ImageRefDialog : Window
//{
//    public ImageRefDialogViewModel VM { get; }

//    public ImageRef? SelectedImageRef => VM.SelectedAsset == null ? null :
//        new ImageRef(VM.SelectedAsset.Id,
//            VM.SelectedAsset.Kind == AssetCatalog.AssetKind.Png ? ImageFormat.Png : ImageFormat.Svg);

//    public ImageRefDialog(ImageRef? existingRef = null)
//    {
//        VM = new ImageRefDialogViewModel();


//        DataContext = VM;

//        Messenenger.UI.ShowOverlay(true);

//        InitializeComponent();

//        if (existingRef != null)
//        {
//            VM.CurrentFormat = existingRef.Format == ImageFormat.Png
//                ? ImageRefDialogViewModel.FormatKind.Png
//                : ImageRefDialogViewModel.FormatKind.Svg;
//        }

//        // Set initial selection based on existing reference
//        if (existingRef != null)
//        {
//            var matchingAsset = AssetCatalog.AllAssets?
//                .FirstOrDefault(a => a.Id == existingRef.Id &&
//                    a.Kind == (existingRef.Format == ImageFormat.Png ?
//                        AssetCatalog.AssetKind.Png : AssetCatalog.AssetKind.Svg));
//            if (matchingAsset != null)
//            {
//                VM.SelectedAsset = matchingAsset;
//            }
//        }
//    }

//    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
//    {
//        if (sender is SKElement element &&
//            element.DataContext is AssetCatalog.AssetInfo info)
//        {
//            var canvas = e.Surface.Canvas;
//            canvas.Clear(SKColors.Transparent);

//            var size = Math.Min(e.Info.Width, e.Info.Height);
//            using var bitmap = ImageRenderer.RenderPreview(info, tint: null, targetSize: size);

//            if (bitmap != null)
//            {
//                var x = (e.Info.Width - bitmap.Width) / 2f;
//                var y = (e.Info.Height - bitmap.Height) / 2f;

//                using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
//                canvas.DrawBitmap(bitmap, x, y, paint);
//            }
//        }
//    }

//    private void OkButton_Click(object sender, RoutedEventArgs e)
//    {
//        DialogResult = true;
//        Close();
//    }

//    private void CancelButton_Click(object sender, RoutedEventArgs e)
//    {
//        DialogResult = false;
//        Close();
//    }


//    private void SKElement_MouseDown(object sender, MouseButtonEventArgs e)
//    {
//        DialogResult = true;
//        Close();
//    }

//    private void Root_Closed(object sender, EventArgs e)
//        => Messenenger.UI.ShowOverlay(false);

//}


//public partial class ImageRefDialogViewModel : ObservableObject
//{
//    public enum FormatKind { Svg, Png }

//    [ObservableProperty]
//    private string searchText = string.Empty;

//    [ObservableProperty]
//    private string countDisplayText = "Loading...";

//    [ObservableProperty]
//    private string addButtonText = "Add SVG ..";

//    [ObservableProperty]
//    private FormatKind currentFormat = FormatKind.Svg;

//    [ObservableProperty]
//    private bool noResultsVisible;

//    [ObservableProperty]
//    private AssetCatalog.AssetInfo? selectedAsset;

//    public ObservableCollection<AssetCatalog.AssetInfo> FilteredAssets { get; } = new();

//    public ICommand SwitchFormatCommand { get; }
//    public ICommand SelectAssetCommand { get; }

//    public ImageRefDialogViewModel()
//    {
//        SwitchFormatCommand = new RelayCommand<string>(OnSwitchFormat);
//        SelectAssetCommand = new RelayCommand<AssetCatalog.AssetInfo>(OnSelectAsset);
//        ApplyFilter();
//    }

//    private void OnSwitchFormat(string? format)
//    {
//        CurrentFormat = format == "Png" ? FormatKind.Png : FormatKind.Svg;
//        ApplyFilter();

//        AddButtonText = CurrentFormat == FormatKind.Svg ? "Add SVG.." : "Add PNG ..";
//        OnPropertyChanged(nameof(AddButtonText));
//    }

//    private void OnSelectAsset(AssetCatalog.AssetInfo? asset)
//    {
//        if (asset != null)
//        {
//            SelectedAsset = asset;
//        }
//    }

//    partial void OnSearchTextChanged(string value)
//    {
//        ApplyFilter();
//    }

//    partial void OnCurrentFormatChanged(FormatKind value)
//    {
//        ApplyFilter();
//    }

//    public void ApplyFilter()
//    {
//        var all = AssetCatalog.AllAssets;
//        if (all == null) return;

//        var query = CurrentFormat == FormatKind.Svg
//            ? all.Where(a => a.Kind == AssetCatalog.AssetKind.Svg)
//            : all.Where(a => a.Kind == AssetCatalog.AssetKind.Png);

//        if (!string.IsNullOrWhiteSpace(SearchText))
//        {
//            var term = SearchText.Trim();
//            query = query.Where(a => a.Id.Contains(term, StringComparison.OrdinalIgnoreCase));
//        }

//        var results = query.OrderBy(a => a.Id).ToList();

//        FilteredAssets.Clear();
//        foreach (var asset in results)
//        {
//            FilteredAssets.Add(asset);
//        }

//        CountDisplayText = $"{results.Count} icon{(results.Count == 1 ? "" : "s")}";
//        NoResultsVisible = results.Count == 0;

//        // Clear selection if current selection is not in filtered results
//        if (SelectedAsset != null && !results.Contains(SelectedAsset))
//            SelectedAsset = null;
//    }
//}


//// Converter for selection highlighting
//public class AssetSelectionConverter : IValueConverter
//{
//    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
//    {
//        if (value is AssetCatalog.AssetInfo asset && parameter is string param)
//        {
//            var window = Application.Current.Windows.OfType<ImageRefDialog>().FirstOrDefault();
//            var isSelected = window?.VM.SelectedAsset == asset;

//            switch (param)
//            {
//                case "Background":
//                    return isSelected ? new SolidColorBrush(Colors.LightBlue) : Brushes.Transparent;
//                case "BorderBrush":
//                    return isSelected ? new SolidColorBrush(Colors.RoyalBlue) : new SolidColorBrush(Colors.LightGray);
//                case "BorderThickness":
//                    return isSelected ? new Thickness(2) : new Thickness(1);
//            }
//        }
//        return Brushes.Transparent;
//    }

//    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
//    {
//        throw new NotImplementedException();
//    }
//}


