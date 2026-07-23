// ============================================================================
// FILE: Mockup.AssetSystem/AssetExtension.cs
// PURPOSE:
//   MarkupExtension zur direkten XAML-Nutzung von Assets:
//     <Image Source="{asset:Asset Id=icon_home}" />
//   Optional: Size=32, Tint="#FF0066CC"
//
// AUTHOR: ChatGPT (XMOCKUP2 / MO27)
// VERSION: 1.0
//
// Verwendungsbeispiel:
// xmlns: asset = "clr-namespace:Mockup.AssetSystem"...
// <Image Width = "24" Height = "24" Source = "{asset:Asset Id=icon_home}"/>
// <Image Width = "32" Height = "32" Source = "{asset:Asset Id=icon_settings, Size=32, Tint=#FF0066CC}"/>
//
// ============================================================================

using System.Windows.Markup;
using System.Windows.Media;

namespace Mockup.AssetSystem;

[MarkupExtensionReturnType(typeof(ImageSource))]
public sealed class AssetExtension : MarkupExtension
{
    public string Id { get; set; } = string.Empty;
    public int Size { get; set; } = AssetCatalog.DefaultPreviewSize;
    public Color? Tint { get; set; }

    public override object? ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrWhiteSpace(Id))
            return null;

        // Katalog bei Bedarf initialisieren
        var img = AssetCatalog.GetPreview(Id, Tint, Size);
        return img;
    }
}
