using Microsoft.Win32;
using Mockup.Resources;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using VIA.WPF.Localization;

namespace Mockup.UIControls;

public partial class XImagePicker : UserControl
{

    private static readonly string[] AllowedExtensions = [".bmp", ".png", ".jpg", ".jpeg"];

    public string ImageFilename { get; set; } = string.Empty;

    public string ImageSizeHint => $"{MockupService.Mockup.CurrentProject?.DeviceInfo}";

    public XImagePicker()
    {
        InitializeComponent();

        Loaded += ScreenEditor_Loaded;
    }

    private static string DialogText(string key, string fallbackText)
    {
        return XLocalizationService.Current.GetString(
            UserFlowResources.ResourceManager,
            key,
            fallbackText);
    }

    private void ScreenEditor_Loaded(object sender, RoutedEventArgs e)
    {
        //InputName.Focus();

        if (DataContext is not Screen screen)
            return;

        DisplayScreenBitmapInImage(screen.BackgroundImage);
    }

    private void SelectImageButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not Screen screen)
            return;

        var dlg = new OpenFileDialog
        {
            Title = DialogText("Dialog.ImagePicker.SelectBackgroundTitle", "Select Background Image"),
            Filter = DialogText("Dialog.ImagePicker.ImageFilesFilter", "Images (*.bmp;*.png;*.jpg;*.jpeg)|*.bmp;*.png;*.jpg;*.jpeg"),
            Multiselect = false
        };

        if (dlg.ShowDialog() == true)
        {
            ApplyImageFile(screen, dlg.FileName);
        }
    }

    private void RemoveImageButton_Click(object sender, RoutedEventArgs e)
    {
        ImageFilename = string.Empty;

        DisplayScreenBitmapInImage(null);
    }


    // ======================================================================================
    // DRAG & DROP (bmp, jpg, jpeg, png)
    // ======================================================================================

    private void OnDragOverImage(object sender, DragEventArgs e)
    {
        e.Handled = true;

        if (TryGetFirstValidImagePath(e.Data, out _))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
    }

    private void OnDropImage(object sender, DragEventArgs e)
    {
        e.Handled = true;

        if (DataContext is not Screen screen)
        {
            e.Effects = DragDropEffects.None;
            return;
        }

        if (!TryGetFirstValidImagePath(e.Data, out var path))
        {
            e.Effects = DragDropEffects.None;
            return;
        }

        ApplyImageFile(screen, path);
        e.Effects = DragDropEffects.Copy;
    }

    private static bool TryGetFirstValidImagePath(IDataObject data, out string path)
    {
        path = string.Empty;

        if (data == null)
            return false;

        // Standard: File Drop
        if (data.GetDataPresent(DataFormats.FileDrop))
        {
            if (data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
                return false;

            // Nimm erstes gültiges Bild
            foreach (var f in files)
            {
                if (IsSupportedImageFile(f))
                {
                    path = f;
                    return true;
                }
            }

            return false;
        }

        // Optional: Text/UnicodeText als Pfad (z.B. Drag aus Explorer Adresszeile/Tools)
        if (data.GetDataPresent(DataFormats.UnicodeText) || data.GetDataPresent(DataFormats.Text))
        {
            var text = (data.GetData(DataFormats.UnicodeText) as string) ?? (data.GetData(DataFormats.Text) as string) ?? "";
            text = text.Trim().Trim('"');

            if (IsSupportedImageFile(text))
            {
                path = text;
                return true;
            }
        }

        return false;
    }

    private static bool IsSupportedImageFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            if (!File.Exists(path))
                return false;

            var ext = Path.GetExtension(path);
            if (string.IsNullOrWhiteSpace(ext))
                return false;

            return AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private void ApplyImageFile(Screen screen, string filename)
    {
        if (!IsSupportedImageFile(filename))
            return;

        ImageFilename = filename;

        // falls SetBackgroundImageFromFile intern lädt + screen.BackgroundImage setzt:
        screen.SetBackgroundImageFromFile(ImageFilename);

        // UI aktualisieren
        DisplayScreenBitmapInImage(screen.BackgroundImage);

    }

    // ======================================================================================

    private void DisplayScreenBitmapInImage(SKBitmap? image)
    {
        if (image == null)
        {
            PART_Image.Source = null;
            return;
        }

        // Kopiert Pixel in ein WPF-BitmapSource (WriteableBitmap)
        WriteableBitmap wb = image.ToWriteableBitmap();
        PART_Image.Source = wb;
    }

}
