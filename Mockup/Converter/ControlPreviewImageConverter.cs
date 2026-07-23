using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Mockup.Converter;

/// <summary>
/// Lädt die festen PNG-Vorschaubilder der Control-Toolbox.
/// Die Bilder werden einmal decodiert, eingefroren und anschließend aus einem
/// gemeinsamen Cache wiederverwendet.
/// </summary>
public sealed class ControlPreviewImageConverter : IValueConverter
{
    private const int DecodePixelWidth = 384;

    private static readonly ConcurrentDictionary<string, Lazy<ImageSource?>> Cache =
        new(StringComparer.OrdinalIgnoreCase);

    public object? Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return Resolve(value as string);
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return Binding.DoNothing;
    }

    internal static ImageSource? Resolve(string? previewImage)
    {
        string? normalizedPath = NormalizePngPath(previewImage);

        if (normalizedPath is null)
            return null;

        return Cache.GetOrAdd(
            normalizedPath,
            static path => new Lazy<ImageSource?>(
                () => LoadBitmap(path),
                LazyThreadSafetyMode.ExecutionAndPublication))
            .Value;
    }

    private static string? NormalizePngPath(string? previewImage)
    {
        if (string.IsNullOrWhiteSpace(previewImage))
            return null;

        string normalizedPath = previewImage
            .Trim()
            .TrimStart('/', '\\')
            .Replace('\\', '/');

        if (string.IsNullOrWhiteSpace(normalizedPath))
            return null;

        string extension = Path.GetExtension(normalizedPath);

        if (string.IsNullOrWhiteSpace(extension))
            return normalizedPath + ".png";

        return string.Equals(
            extension,
            ".png",
            StringComparison.OrdinalIgnoreCase)
                ? normalizedPath
                : Path.ChangeExtension(normalizedPath, ".png")
                    .Replace('\\', '/');
    }

    private static ImageSource? LoadBitmap(string normalizedPath)
    {
        try
        {
            Uri uri = new(
                $"pack://application:,,,/VIA.Mockup;component/Resources/ControlPreviews/{normalizedPath}",
                UriKind.Absolute);

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            image.DecodePixelWidth = DecodePixelWidth;
            image.UriSource = uri;
            image.EndInit();
            image.Freeze();

            return image;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Steuert die Sichtbarkeit von Preview und "No Image"-Fallback anhand desselben
/// gecachten Ladeergebnisses wie <see cref="ControlPreviewImageConverter"/>.
/// </summary>
public sealed class ControlPreviewVisibilityConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        bool hasImage =
            ControlPreviewImageConverter.Resolve(value as string) is not null;

        bool showMissing = string.Equals(
            parameter as string,
            "Missing",
            StringComparison.OrdinalIgnoreCase);

        bool isVisible = showMissing
            ? !hasImage
            : hasImage;

        return isVisible
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
