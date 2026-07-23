// ============================================================================
// FILE: Mockup.AssetSystem/AssetToImageSourceConverter.cs
// PURPOSE:
//   ValueConverter für dynamische Bindings:
//     <Image Source="{Binding IconId, Converter={StaticResource AssetIconConv}}" />
//   Wobei der Converter als Resource konfiguriert ist (Size/Tint einstellbar).
//
// AUTHOR: ChatGPT (XMOCKUP2 / MO27)
// VERSION: 1.0
//
// <Window.Resources>
//  <asset:AssetToImageSourceConverter x:Key = "AssetIconConv" Size = "20"/>
// </Window.Resources >
// ...
// <Image Source = "{Binding IconId, Converter={StaticResource AssetIconConv}}"/>
//
// ============================================================================
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Mockup.AssetSystem;

public sealed class AssetToImageSourceConverter : IValueConverter
{
    /// <summary>Previewgröße in Pixeln (Default 24).</summary>
    public int Size { get; set; } = AssetCatalog.DefaultPreviewSize;

    /// <summary>Optionaler Tint nur für SVG.</summary>
    public Color? Tint { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // value: AssetId (string)
        if (value is not string id || string.IsNullOrWhiteSpace(id))
            return null;

        return AssetCatalog.GetPreview(id, Tint, Size);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
